using AutoMapper;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSConfig.Configuration;
using SMSDAL.Context;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General;
using SMSServiceModels.v1.General.AzureAI;
using SMSServiceModels.v1.General.HuggingFace;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace SMSBAL.Projects.AzureAI
{
    public class AzureAIProcess : SMSBalBase
    {
        #region Properties
        private readonly APIConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly HttpClient _httpClient;
        private readonly string _computerVisonConfigurationKey;
        private readonly string _computerVisonConfigurationEndpoint;
        private readonly string _textAnalyticsConfigurationKey;
        private readonly string _textAnalyticsConfigurationEndpoint;
        private readonly string _textTranslatorConfigurationKey;
        private readonly string _textTranslatorConfigurationEndpoint;
        private readonly string _textTranslatorConfigurationLocation;
        private readonly bool _isAzureTestingMode;
        private readonly bool _isHuggingFaceTestingMode;
        private readonly string _summarizeModel;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        #endregion Properties

        #region Constructor
        public AzureAIProcess(IMapper mapper, ApiDbContext apiDbContext, APIConfiguration configuration,
            IHttpClientFactory httpClientFactory, TextAnalyticsClient textAnalyticsClient, HttpClient httpClient
            ): base(mapper, apiDbContext)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _computerVisonConfigurationKey = configuration.ExternalIntegrations.AzureConfiguration.ComputerVisionConfiguration.ApiKey;
            _computerVisonConfigurationEndpoint = configuration.ExternalIntegrations.AzureConfiguration.ComputerVisionConfiguration.EndPoint;
            _textAnalyticsConfigurationKey = configuration.ExternalIntegrations.AzureConfiguration.TextAnalyticsConfiguration.ApiKey;
            _textAnalyticsConfigurationEndpoint = configuration.ExternalIntegrations.AzureConfiguration.TextAnalyticsConfiguration.EndPoint;
            _textTranslatorConfigurationKey = configuration.ExternalIntegrations.AzureConfiguration.TextTranslatorConfiguration.ApiKey;
            _textTranslatorConfigurationEndpoint = configuration.ExternalIntegrations.AzureConfiguration.TextTranslatorConfiguration.EndPoint;
            _textTranslatorConfigurationLocation = configuration.ExternalIntegrations.AzureConfiguration.TextTranslatorConfiguration.Location;
            _isAzureTestingMode = configuration.ExternalIntegrations.AzureConfiguration.IsTestingMode;
            _isHuggingFaceTestingMode = configuration.ExternalIntegrations.HuggingFaceConfiguration.IsTestingMode;
            _summarizeModel = configuration.ExternalIntegrations.HuggingFaceConfiguration.SummarizeModel;
            _textAnalyticsClient = textAnalyticsClient;
            _httpClient = httpClient;
            _baseUrl = configuration.ExternalIntegrations.HuggingFaceConfiguration.BaseUrl;
            _apiKey = configuration.ExternalIntegrations.HuggingFaceConfiguration.ApiKey;
        }
        
        #endregion Constructor

        #region Text Translation
        /// <summary>
        /// Translates the provided text into the specified language using the Text Translator API.
        /// </summary>
        /// <param name="objSM">An object containing the text to be translated and the target language.</param>
        /// <returns>
        /// A <see cref="AzureAIResponseSM"/> object containing the translated text.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>The specified language is invalid or unsupported for translation.</description></item>
        ///   <item><description>The translation service fails to respond after multiple retry attempts.</description></item>
        /// </list>
        /// </exception>
        public async Task<AzureAIResponseSM> TranslateTextAsync(TranslationRequestSM objSM)
        {
            if (_isAzureTestingMode)
            {
                return GetDummyResponse();
            }
            var languageCode = GetTranslationLanguageCodeFromName(objSM.Language);
            if (languageCode.IsNullOrEmpty() || languageCode == "unk")
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Invalid Language Code", "The provided language is not supported for translation. Please verify and provide a valid language.");
            }

            string route = $"translate?api-version=3.0&to={languageCode}";
            var body = new object[] { new { objSM.Text } };
            var jsonRequestBody = JsonConvert.SerializeObject(body);

            int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                using (var client = _httpClientFactory.CreateClient())
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(_textTranslatorConfigurationEndpoint + route);
                    request.Content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", _textTranslatorConfigurationKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", _textTranslatorConfigurationLocation);

                    try
                    {
                        attempt++;

                        HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                        response.EnsureSuccessStatusCode();

                        string result = await response.Content.ReadAsStringAsync();

                        var translationResult = JsonConvert.DeserializeObject<dynamic>(result);
                        var translatedText = translationResult[0]?.translations[0]?.text;
                        var res = new AzureAIResponseSM()
                        {
                            TextResponse = translatedText,
                        };
                        return res;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        // Log the retry attempt or perform any action
                        continue; // Retry the operation
                    }
                }
            }

            throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Failed to translate text after multiple retries", "Failed to translate text after multiple retries");
        }
        
        /// <summary>
        /// Retrieves a list of supported languages for text translation.
        /// </summary>
        /// <returns>
        /// A list of <see cref="LanguageInfoSM"/> objects, where each object contains the language name and corresponding language code.
        /// </returns>
        public async Task<List<LanguageInfoSM>> GetSupportedLanguagesForTextTranslation()
        {
            var response = new List<LanguageInfoSM>();
            foreach (var pair in TranslationLanguageMapping)
            {
                var res = new LanguageInfoSM()
                {
                    LanguageName = pair.Value.ToString(),
                    LanguageCode = pair.Key
                };
                response.Add(res);
            }
            return response;
        }

        /// <summary>
        /// Retrieves a list of supported languages for text extraction.
        /// </summary>
        /// <returns>
        /// A list of <see cref="LanguageInfoSM"/> objects, where each object contains the language name and the corresponding language code.
        /// </returns>
        public async Task<List<LanguageInfoSM>> GetSupportedLanguagesForTextExtraction()
        {
            var response = new List<LanguageInfoSM>();
            foreach (var pair in ExtractionLanguageMapping)
            {
                var res = new LanguageInfoSM()
                {
                    LanguageName = pair.Value.ToString(),
                    LanguageCode = pair.Key
                };
                response.Add(res);
            }
            return response;
        }

        #endregion Text Translation

        #region Text Summerization
        /// <summary>
        /// Summarizes the provided text using an abstractive summarization model.
        /// </summary>
        /// <param name="objSM">
        /// A <see cref="AzureAIRequestSM"/> object containing the text input to be summarized.
        /// </param>
        /// <returns>
        /// A <see cref="AzureAIResponseSM"/> object containing the summarized text.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>Text summarization fails after the maximum number of retry attempts.</description></item>
        ///   <item><description>Other unexpected errors occur during the summarization process.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// This method uses an abstractive summarization model provided by <see cref="_textAnalyticsClient"/>. 
        /// The summarization process attempts retries in case of transient failures, up to a maximum of three attempts. 
        /// </remarks>
        public async Task<AzureAIResponseSM> TextSummarizeAsync(AzureAIRequestSM objSM)
        {

            if (_isAzureTestingMode)
            {
                return GetDummyResponse();
            }
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;

                    var operation = await _textAnalyticsClient.AbstractiveSummarizeAsync(
                        WaitUntil.Completed,
                        new[] { objSM.TextInput }
                    );

                    var summary = new StringBuilder();

                    await foreach (var doc in operation.Value)
                    {
                        var data = doc[0];
                        foreach (var sentence in data.Summaries)
                        {
                            summary.AppendLine(sentence.Text);
                        }
                    }
                    //Todo: Can translate summary into different language
                    var response = new AzureAIResponseSM()
                    {
                        TextResponse = summary.ToString().Trim()
                    };
                    return response;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        throw new SMSException(
                            ApiErrorTypeSM.Fatal_Log,
                            "An error occurred while summarizing the text after multiple attempts.",
                            $"{ex.Message}"
                        );
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Summarizes the provided text using an extractive summarization approach.
        /// </summary>
        /// <param name="objSM">
        /// A <see cref="AzureAIRequestSM"/> object containing the text input to be summarized.
        /// </param>
        /// <returns>
        /// A <see cref="AzureAIResponseSM"/> object containing the extractive summary of the input text.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>Text summarization fails after the maximum number of retry attempts.</description></item>
        ///   <item><description>Other unexpected errors occur during the summarization process.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// This method employs an extractive summarization model provided by <see cref="_textAnalyticsClient"/> to extract key sentences from the input text.
        /// <para>It uses the <see cref="ExtractiveSummarizeOptions"/> class to limit the number of sentences in the summary to 10.</para>
        /// <para>The summarization process includes a retry mechanism to handle transient failures, with up to three attempts.</para>
        /// </remarks>
        public async Task<AzureAIResponseSM> ExtensiveSummarizeAsync(AzureAIRequestSM objSM)
        {
            if (_isAzureTestingMode)
            {
                return GetDummyResponse();
            }
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;

                    var options = new ExtractiveSummarizeOptions
                    {
                        MaxSentenceCount = 20,

                    };
                    var operation = await _textAnalyticsClient.ExtractiveSummarizeAsync(
                    WaitUntil.Completed,
                    new[] { objSM.TextInput },
                    options: options
                    );

                    var summary = new StringBuilder();

                    await foreach (var doc in operation.Value)
                    {
                        var data = doc[0];
                        foreach (var sentence in data.Sentences)
                        {
                            summary.AppendLine(sentence.Text);
                        }
                    }
                    var response = new AzureAIResponseSM()
                    {
                        TextResponse = summary.ToString().Trim()
                    };
                    return response;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        throw new SMSException(
                            ApiErrorTypeSM.Fatal_Log,
                            "An error occurred while summarizing the text after multiple attempts.",
                            $"{ex.Message}"
                        );
                    }
                }
            }

            return null;
        }

        #region Other Methods
        #endregion Other Methods


        #endregion Text Summerization

        #region Text Extraction From Images
        /// <summary>
        /// Extracts text from an image provided as a Base64-encoded string using a Computer Vision API.
        /// </summary>
        /// <param name="objSM">An object containing the Base64-encoded image data.</param>
        /// <returns>A <see cref="SummafyAIResponseSM"/> object containing the extracted text and additional details.</returns>
        /// <exception cref="SMSException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>The input image data is null or empty.</description></item>
        ///   <item><description>The API call fails or returns an unsuccessful response.</description></item>
        ///   <item><description>Text extraction fails after the maximum number of retries.</description></item>
        /// </list>
        /// </exception>

        public async Task<AzureAIResponseSM> ExtractTextFromBase64ImageAsync(ImageDataSM objSM)
        {
            if (_isAzureTestingMode)
            {
                return GetDummyResponse();
            }

            if (objSM.Base64Image.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Image data cannot be empty", "The image data is empty. Please provide a valid image to extract text.");
            }

            string apiUrl = $"{_computerVisonConfigurationEndpoint}vision/v3.2/ocr";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _computerVisonConfigurationKey);

            // Prepare the image content
            var imageBytes = Convert.FromBase64String(objSM.Base64Image);
            using var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            int retryCount = 0;
            const int maxRetries = 3;
            const int initialDelay = 1000;

            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"{await response.Content.ReadAsStringAsync()}", $"{await response.Content.ReadAsStringAsync()}");
                        //throw new Exception($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();

                    return ExtractTextFromJson(responseContent);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(initialDelay * (int)Math.Pow(2, retryCount - 1));
                    }
                    else
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Extracting text from the image... Please try Again", $"Something went wrong while Extracting text from the image... Please try Again");

                    }
                }
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Failed to extract text from image after multiple retries.", $"Failed to extract text from image after multiple retries.");

        }
        /// <summary>
        /// Extracts text from the provided image data using OCR (Optical Character Recognition).
        /// The method makes up to three attempts to send the image to an external OCR API for text extraction.
        /// If the attempts fail, it returns an empty string.
        /// </summary>
        /// <param name="imageData">A byte array containing the image data for OCR processing.</param>
        /// <returns>
        /// A string containing the extracted text from the image. If OCR fails, returns an empty string.
        /// </returns>

        private async Task<string> ExtractTextFromImageAsync(byte[] imageData)
        {
            const int maxAttempts = 3;
            int attempt = 0;

            if (_isAzureTestingMode)
            {
                var res = GetDummyResponse();
                return res.TextResponse;
            }

            string apiUrl = $"{_computerVisonConfigurationEndpoint}vision/v3.2/ocr";

            while (attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _computerVisonConfigurationKey);

                    using var content = new ByteArrayContent(imageData);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    var response = await httpClient.PostAsync(apiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        return ExtractTextFromJson(jsonResponse)?.TextResponse ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log the exception if needed
                    Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                }
            }

            // If all attempts fail, return an empty string
            return string.Empty;
        }


        #region Extract Text from Json Data

        /// <summary>
        /// Extracts text from a JSON response returned by the Computer Vision API.
        /// </summary>
        /// <param name="jsonResponse">The JSON response string containing OCR data.</param>
        /// <returns>
        /// A <see cref="SummafyAIResponseSM"/> object containing the extracted text, 
        /// or <c>null</c> if no text is found in the response.
        /// </returns>
        private AzureAIResponseSM ExtractTextFromJson(string jsonResponse)
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;

            var extractedText = new StringBuilder();
            foreach (var region in root.GetProperty("regions").EnumerateArray())
            {
                foreach (var line in region.GetProperty("lines").EnumerateArray())
                {
                    foreach (var word in line.GetProperty("words").EnumerateArray())
                    {
                        extractedText.Append(word.GetProperty("text").GetString() + " ");
                    }
                }
            }
            if (extractedText.Length > 0)
            {
                var response = new AzureAIResponseSM()
                {
                    TextResponse = extractedText.ToString().Trim()
                };
                return response;
            }
            return null;
        }

        #endregion Extract Text from Json Data

        #endregion Text Extraction From Images

        #region Text Extraction With Translation
        /* /// <summary>
         /// Extracts text from a base64-encoded image and translates it into the specified language.
         /// </summary>
         /// <param name="objSM">
         /// A <see cref="ImageTranslationSM"/> object containing the base64-encoded image and the target language for translation.
         /// </param>
         /// <returns>
         /// A <see cref="AzureAIResponseSM"/> object containing the extracted and translated text.
         /// </returns>
         /// <exception cref="CoreVisionException">
         /// Thrown when:
         /// <list type="bullet">
         ///   <item><description>The base64-encoded image is null or empty.</description></item>
         ///   <item><description>Text extraction from the image fails.</description></item>
         ///   <item><description>The text could not be translated.</description></item>
         /// </list>
         /// </exception>
         /// <remarks>
         /// This method first extracts text from the provided base64-encoded image using OCR (Optical Character Recognition).
         /// Then, it translates the extracted text into the language specified in the input.
         /// <para>If the text extraction fails or if the image is empty, a <see cref="CoreVisionException"/> is thrown.</para>
         /// <para>If the text extraction is successful, the method proceeds to translate the text and returns the translated result.</para>
         /// </remarks>
         public async Task<AzureAIResponseSM> ExtractTextWithTranslation(ImageTranslationSM objSM)
         {

             if (objSM.Base64Image.IsNullOrEmpty())
             {
                 return null;
             }
             var input = new ImageDataSM()
             {
                 Base64Image = objSM.Base64Image,
             };
             var text = await BaseMethodForTextExtraction(input);
             if (text.TextResponse.IsNullOrEmpty())
             {
                 throw new CoreVisionException(ApiErrorTypeSM.Fatal_Log, "Text is not Extracted Successfully from picture...Try again ", "Text is not Extracted Successfully from picture...Try again ");

             }
             var extraxtionInput = new TextTranslateSM()
             {
                 Text = text.TextResponse,
                 Language = objSM.Language,
             };
             return await BaseMethodForTextTranslation(extraxtionInput);

         }*/

        #endregion Text Extraction With Translation

        #region Langugage to Language Codes

        #region Text Translation Language Mapper (Dictionary)


        private static readonly Dictionary<string, TextTranslationLanguageSupportSM> TranslationLanguageMapping = new()
        {
            { "af",         TextTranslationLanguageSupportSM.Afrikaans},
            { "am",         TextTranslationLanguageSupportSM.Amharic},
            { "ar",         TextTranslationLanguageSupportSM.Arabic },
            { "as",         TextTranslationLanguageSupportSM.Assamese },
            { "az",         TextTranslationLanguageSupportSM.Azerbaijani},
            { "ba",         TextTranslationLanguageSupportSM.Bashkir },
            { "bg",         TextTranslationLanguageSupportSM.Bulgarian },
            { "bh",         TextTranslationLanguageSupportSM.Bhojpuri },
            { "bn",         TextTranslationLanguageSupportSM.Bangla},
            { "bo",         TextTranslationLanguageSupportSM.Tibetan },
            { "brx",        TextTranslationLanguageSupportSM.Bodo },
            { "bs",         TextTranslationLanguageSupportSM.Bosnian },
            { "ca",         TextTranslationLanguageSupportSM.Catalan },
            { "cs",         TextTranslationLanguageSupportSM.Czech },
            { "cy",         TextTranslationLanguageSupportSM.Welsh },
            { "da",         TextTranslationLanguageSupportSM.Danish },
            { "de",         TextTranslationLanguageSupportSM.German },
            { "doi",        TextTranslationLanguageSupportSM.Dogri },
            { "dsb",        TextTranslationLanguageSupportSM.LowerSorbian },
            { "dv",         TextTranslationLanguageSupportSM.Divehi },
            { "el",         TextTranslationLanguageSupportSM.Greek },
            { "en",         TextTranslationLanguageSupportSM.English },
            { "es",         TextTranslationLanguageSupportSM.Spanish },
            { "et",         TextTranslationLanguageSupportSM.Estonian },
            { "eu",         TextTranslationLanguageSupportSM.Basque },
            { "fa",         TextTranslationLanguageSupportSM.Persian },
            { "fi",         TextTranslationLanguageSupportSM.Finnish },
            { "fil",        TextTranslationLanguageSupportSM.Filipino },
            { "fj",         TextTranslationLanguageSupportSM.Fijian },
            { "fo",         TextTranslationLanguageSupportSM.Faroese },
            { "fr",         TextTranslationLanguageSupportSM.French },
            { "fr-CA",      TextTranslationLanguageSupportSM.French_Canada },
            { "ga",         TextTranslationLanguageSupportSM.Irish },
            { "gl",         TextTranslationLanguageSupportSM.Galician },
            { "kok",        TextTranslationLanguageSupportSM.Konkani },
            { "gu",         TextTranslationLanguageSupportSM.Gujarati },
            { "ha",         TextTranslationLanguageSupportSM.Hausa },
            { "he",         TextTranslationLanguageSupportSM.Hebrew },
            { "hi",         TextTranslationLanguageSupportSM.Hindi },
            { "hne",        TextTranslationLanguageSupportSM.Chhattisgarhi },
            { "hr",         TextTranslationLanguageSupportSM.Croatian },
            { "hsb",        TextTranslationLanguageSupportSM.UpperSorbian },
            { "ht",         TextTranslationLanguageSupportSM.HaitianCreole },
            { "hu",         TextTranslationLanguageSupportSM.Hungarian },
            { "hy",         TextTranslationLanguageSupportSM.Armenian },
            { "id",         TextTranslationLanguageSupportSM.Indonesian },
            { "ig",         TextTranslationLanguageSupportSM.Igbo },
            { "ikt",        TextTranslationLanguageSupportSM.Inuinnaqtun },
            { "is",         TextTranslationLanguageSupportSM.Icelandic },
            { "it",         TextTranslationLanguageSupportSM.Italian },
            { "iu",         TextTranslationLanguageSupportSM.Inuktitut },
            { "iu-Latn",    TextTranslationLanguageSupportSM.Inuktitut_Latin },
            { "ja",         TextTranslationLanguageSupportSM.Japanese },
            { "ka",         TextTranslationLanguageSupportSM.Georgian },
            { "kk",         TextTranslationLanguageSupportSM.Kazakh },
            { "km",         TextTranslationLanguageSupportSM.Khmer },
            { "kmr",        TextTranslationLanguageSupportSM.Kurdish_Northern },
            { "kn",         TextTranslationLanguageSupportSM.Kannada },
            { "ko",         TextTranslationLanguageSupportSM.Korean },
            { "ks",         TextTranslationLanguageSupportSM.Kashmiri },
            { "ku",         TextTranslationLanguageSupportSM.Kurdish_Central },
            { "ky",         TextTranslationLanguageSupportSM.Kyrgyz },
            { "ln",         TextTranslationLanguageSupportSM.Lingala },
            { "lo",         TextTranslationLanguageSupportSM.Lao },
            { "lt",         TextTranslationLanguageSupportSM.Lithuanian },
            { "lg",         TextTranslationLanguageSupportSM.Ganda },
            { "lv",         TextTranslationLanguageSupportSM.Latvian },
            { "lzh",        TextTranslationLanguageSupportSM.Chinese_Literary },
            { "mai",        TextTranslationLanguageSupportSM.Maithili },
            { "mg",         TextTranslationLanguageSupportSM.Malagasy },
            { "mi",         TextTranslationLanguageSupportSM.Maori },
            { "mk",         TextTranslationLanguageSupportSM.Macedonian },
            { "ml",         TextTranslationLanguageSupportSM.Malayalam },
            { "mn",         TextTranslationLanguageSupportSM.Mongolian_Cyrillic },
            { "mn-Mong",    TextTranslationLanguageSupportSM.Mongolian_Traditional },
            { "mni",        TextTranslationLanguageSupportSM.Manipuri },
            { "mr",         TextTranslationLanguageSupportSM.Marathi },
            { "ms",         TextTranslationLanguageSupportSM.Malay},
            { "mt",         TextTranslationLanguageSupportSM.Maltese },
            { "mww",        TextTranslationLanguageSupportSM.HmongDaw },
            { "my",         TextTranslationLanguageSupportSM.Myanmar_Burmese},
            { "nb",         TextTranslationLanguageSupportSM.Norwegian },
            { "ne",         TextTranslationLanguageSupportSM.Nepali },
            { "nl",         TextTranslationLanguageSupportSM.Dutch },
            { "nso",        TextTranslationLanguageSupportSM.Sesotho_sa_Leboa },
            { "ny",         TextTranslationLanguageSupportSM.Nyanja },
            { "or",         TextTranslationLanguageSupportSM.Odia },
            { "otq",        TextTranslationLanguageSupportSM.QueretaroOtomi },
            { "pa",         TextTranslationLanguageSupportSM.Punjabi },
            { "pl",         TextTranslationLanguageSupportSM.Polish },
            { "prs",        TextTranslationLanguageSupportSM.Dari },
            { "ps",         TextTranslationLanguageSupportSM.Pashto },
            { "pt-BR",      TextTranslationLanguageSupportSM.Portuguese_Brazil },
            { "pt-PT",      TextTranslationLanguageSupportSM.Portuguese_Brazil },
            { "ro",         TextTranslationLanguageSupportSM.Romanian },
            { "ru",         TextTranslationLanguageSupportSM.Russian },
            { "rn",         TextTranslationLanguageSupportSM.Rundi },
            { "rw",         TextTranslationLanguageSupportSM.Kinyarwanda },
            { "sd",         TextTranslationLanguageSupportSM.Sindhi },
            { "si",         TextTranslationLanguageSupportSM.Sinhala },
            { "sk",         TextTranslationLanguageSupportSM.Slovak },
            { "sl",         TextTranslationLanguageSupportSM.Slovenian },
            { "sm",         TextTranslationLanguageSupportSM.Samoan },
            { "sn",         TextTranslationLanguageSupportSM.Shona },
            { "so",         TextTranslationLanguageSupportSM.Somali },
            { "sq",         TextTranslationLanguageSupportSM.Albanian },
            { "sr-Cyrl",    TextTranslationLanguageSupportSM.Serbian_Cyrillic },
            { "sr-Latn",    TextTranslationLanguageSupportSM.Serbian_Latin },
            { "st",         TextTranslationLanguageSupportSM.Sesotho },
            { "sv",         TextTranslationLanguageSupportSM.Swedish },
            { "sw",         TextTranslationLanguageSupportSM.Swahili },
            { "ta",         TextTranslationLanguageSupportSM.Tamil },
            { "te",         TextTranslationLanguageSupportSM.Telugu },
            { "th",         TextTranslationLanguageSupportSM.Thai },
            { "ti",         TextTranslationLanguageSupportSM.Tigrinya },
            { "tk",         TextTranslationLanguageSupportSM.Turkmen },
            { "tlh-Latn",   TextTranslationLanguageSupportSM.Klingon_Latin },
            { "tlh-pIqaD",  TextTranslationLanguageSupportSM.Klingon_pIqaD },
            { "tn",         TextTranslationLanguageSupportSM.Setswana },
            { "to",         TextTranslationLanguageSupportSM.Tongan },
            { "tr",         TextTranslationLanguageSupportSM.Turkish },
            { "tt",         TextTranslationLanguageSupportSM.Tatar },
            { "ty",         TextTranslationLanguageSupportSM.Tahitian },
            { "ug",         TextTranslationLanguageSupportSM.Uyghur },
            { "uk",         TextTranslationLanguageSupportSM.Ukrainian },
            { "ur",         TextTranslationLanguageSupportSM.Urdu },
            { "uz",         TextTranslationLanguageSupportSM.Uzbek_Latin },
            { "vi",         TextTranslationLanguageSupportSM.Vietnamese }
        };

        #endregion Text Translation Language Mapper (Dictionary)

        #region Text Extraction Language Mapper (Dictionary)

        private static readonly Dictionary<string, TextExtractionLanguageSupportSM> ExtractionLanguageMapping = new()
        {
            { "ar",      TextExtractionLanguageSupportSM.Arabic },
            { "cs",      TextExtractionLanguageSupportSM.Czech},
            { "da",      TextExtractionLanguageSupportSM.Danish},
            { "de",      TextExtractionLanguageSupportSM.German},
            { "el",      TextExtractionLanguageSupportSM.Greek},
            { "en",      TextExtractionLanguageSupportSM.English},
            { "es",      TextExtractionLanguageSupportSM.Spanish },
            { "fi",      TextExtractionLanguageSupportSM.Finnish },
            { "fr",      TextExtractionLanguageSupportSM.French },
            { "hu",      TextExtractionLanguageSupportSM.Hungarian },
            { "it",      TextExtractionLanguageSupportSM.Italian},
            { "ja",      TextExtractionLanguageSupportSM.Japanese },
            { "ko",      TextExtractionLanguageSupportSM.Korean},
            { "nb",      TextExtractionLanguageSupportSM.NorwegianBokmal },
            { "nl",      TextExtractionLanguageSupportSM.Dutch},
            { "pl",      TextExtractionLanguageSupportSM.Polish },
            { "pt",      TextExtractionLanguageSupportSM.Portuguese },
            { "ro",      TextExtractionLanguageSupportSM.Romanian},
            { "ru",      TextExtractionLanguageSupportSM.Russian },
            { "sk",      TextExtractionLanguageSupportSM.Slovak },
            { "sr_Cyrl", TextExtractionLanguageSupportSM.SerbianCyrillic },
            { "sr_Latn", TextExtractionLanguageSupportSM.SerbianLatin },
            { "sv",      TextExtractionLanguageSupportSM.Swedish},
            { "tr",      TextExtractionLanguageSupportSM.Turkish },
            { "unk",     TextExtractionLanguageSupportSM.Unknown },
            { "zh_Hans", TextExtractionLanguageSupportSM.SimplifiedChinese },
            { "zh_Hant", TextExtractionLanguageSupportSM.TraditionalChinese}
        };

        #endregion Text Extraction Language Mapper (Dictionary)

        #region Get Respective Language Codes

        public string GetTranslationLanguageCodeFromName(TextTranslationLanguageSupportSM languageName)
        {

            foreach (var pair in TranslationLanguageMapping)
            {
                if (pair.Value == languageName)
                {
                    return pair.Key;
                }
            }
            return "unk";
        }

        public static string GetExtractionLanguageCodeFromName(TextExtractionLanguageSupportSM languageName)
        {

            foreach (var pair in ExtractionLanguageMapping)
            {
                if (pair.Value == languageName)
                {
                    return pair.Key;
                }
            }
            return "unk";
        }

        #endregion Get Respective Language Codes

        #endregion Langugage to Language Codes

        /*#region Pdf Process

        #region Extract Text and Images from Pdf        

        /// <summary>
        /// Extracts text and images from a PDF file represented as a Base64-encoded string. 
        /// It decodes the PDF, extracts text from each page, and uses OCR to extract text from images embedded in the PDF.
        /// </summary>
        /// <param name="objSM">An object containing the Base64-encoded PDF data to be processed.</param>
        /// <returns>
        /// A <see cref="AzureAIResponseSM"/> object containing the extracted text from the PDF and images.
        /// </returns>



        public async Task<AzureAIResponseSM> ExtractTextAndImagesFromPdfAsync(PdfBase64RequestSM objSM)
        {
            var extractedText = new StringBuilder();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); // Temporary folder for images
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // Convert base64 PDF to byte array
                byte[] pdfBytes;
                try
                {
                    pdfBytes = Convert.FromBase64String(objSM.Base64Pdf);
                }
                catch (FormatException)
                {
                    throw new CoreVisionException(ApiErrorTypeSM.Success_NoLog,
                        "Invalid base64 PDF format. Please provide a valid base64-encoded PDF.",
                        "Invalid base64 PDF format.");
                }

                using (var pdfStream = new MemoryStream(pdfBytes))
                using (var reader = new PdfReader(pdfStream))
                using (var pdfDoc = new PdfDocument(reader))
                {
                    // Iterate through each page in the PDF
                    for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
                    {
                        PdfPage page = pdfDoc.GetPage(pageNum);

                        // Extract text from the page
                        string pageText = PdfTextExtractor.GetTextFromPage(page);
                        extractedText.AppendLine(pageText);

                        // Extract images from the page
                        var images = ExtractImagesFromPage(page);

                        int imageIndex = 1;
                        foreach (var imageData in images)
                        {
                            // Save the image to a temporary file (optional)
                            string imagePath = Path.Combine(tempDirectory, $"Page-{pageNum}_Image-{imageIndex}.jpg");
                            System.IO.File.WriteAllBytes(imagePath, imageData);

                            // Convert the image to base64
                            string imageBase64 = Convert.ToBase64String(imageData);

                            if (!string.IsNullOrEmpty(imageBase64))
                            {
                                // Create an object to pass the base64 image for text extraction
                                var input = new SummafyAIImageDataSM()
                                {
                                    Base64Image = imageBase64
                                };

                                // Extract text from the image
                                var textExtractionResponse = await BaseMethodForTextExtraction(input);
                                if (!string.IsNullOrEmpty(textExtractionResponse.TextResponse))
                                {
                                    extractedText.AppendLine(textExtractionResponse.TextResponse);
                                }
                            }

                            imageIndex++;
                        }
                    }
                }

                // Return the extracted text
                return new AzureAIResponseSM
                {
                    TextResponse = extractedText.ToString().Trim()
                };
            }
            catch (PdfException ex)
            {
                // Handle PDF-specific exceptions (e.g., encrypted or damaged PDF)
                throw new CoreVisionException(ApiErrorTypeSM.Success_NoLog,
                    "The PDF file seems to be either encrypted, damaged, or in an unsupported format. Please check the file and try again.",
                    ex.Message);
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions
                throw new CoreVisionException(ApiErrorTypeSM.Fatal_Log,
                    "An unexpected error occurred while processing the PDF. Please try again later.",
                    ex.Message);
            }
            finally
            {
                // Clean up the temporary directory
                if (Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (IOException ex)
                    {
                        // Log the cleanup error (optional)
                        Console.WriteLine($"Failed to delete temporary directory: {ex.Message}");
                    }
                }
            }
        }
        #endregion Extract Text and Images from Pdf

        #region Extract Images from Pdf


        /// <summary>
        /// Extracts images from the specified PDF page.
        /// This method processes the page content and retrieves any images embedded in the page.
        /// </summary>
        /// <param name="page">The PDF page from which images are to be extracted.</param>
        /// <returns>
        /// A list of byte arrays, where each byte array represents an image extracted from the page.
        /// </returns>
        private List<byte[]> ExtractImagesFromPage(PdfPage page)
        {
            var images = new List<byte[]>();
            var strategy = new SimpleEventListener();

            PdfCanvasProcessor processor = new PdfCanvasProcessor(strategy);
            processor.ProcessPageContent(page);

            foreach (var renderInfo in strategy.Images)
            {
                try
                {
                    var imageBytes = renderInfo.GetImage().GetImageBytes();
                    if (imageBytes != null)
                    {
                        images.Add(imageBytes);
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }

            return images;
        }
        #endregion Extract Images from Pdf

        #region Simple Event Listener

        private class SimpleEventListener : IEventListener
        {
            public List<ImageRenderInfo> Images { get; } = new List<ImageRenderInfo>();

            public void EventOccurred(IEventData data, EventType type)
            {
                if (type == EventType.RENDER_IMAGE)
                {
                    var renderInfo = (ImageRenderInfo)data;
                    Images.Add(renderInfo);
                }
            }

            public ICollection<EventType> GetSupportedEvents()
            {
                return null;
            }
        }

        #endregion Simple Event Listener


        #endregion Pdf Process*/

        #region Dummy Response 

        public AzureAIResponseSM GetDummyResponse()
        {
            var res = new AzureAIResponseSM()
            {
                TextResponse = "This is a dummy response for your input, designed to facilitate the integration of text extraction, summarization, and translation functionalities. " +
                               "It serves as a placeholder while the actual Azure AI integration is being tested, ensuring the system functions smoothly. " +
                               "This response will allow you to test the flow and connectivity of the integration without depending on real API calls or data."
            };
            return res;
        }


        #endregion Dummy Response 
    }
}
