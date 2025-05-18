using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSConfig.Configuration;
using SMSDAL.Context;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.StoryAI;
using System.Net.Http.Headers;
using System.Text;

namespace SMSBAL.Projects.StoryAI
{
    public class StoryProcess : SMSBalBase
    {
        private readonly HttpClient _httpClient;
        private readonly APIConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _storyAIModel;
        public StoryProcess(IMapper mapper, ApiDbContext apiDbContext, APIConfiguration configuration, HttpClient httpClient)
            : base(mapper, apiDbContext)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = configuration.ExternalIntegrations.HuggingFaceConfiguration.BaseUrl;
            _apiKey = configuration.ExternalIntegrations.HuggingFaceConfiguration.ApiKey;
            _storyAIModel = configuration.ExternalIntegrations.HuggingFaceConfiguration.StoryAIModel;
        }

        #region Generate Content (Story, Conversation, Dialogue etc)

        public async Task<ContentGenerationResponseSM> GenerateStory(ContentGenerationRequestSM data)
        {
            if (data == null)
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "No input data provided.",
                    "Please provide valid content data to generate the story."
                );
            }

            var prompt = GeneratePrompt(data);
            if (prompt.IsNullOrEmpty())
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "Failed to generate prompt.",
                    "We encountered an issue while creating the prompt. Please try again later."
                );
            }

            var requestContent = new StringContent(
                JsonConvert.SerializeObject(new { inputs = prompt }),
                Encoding.UTF8,
                "application/json"
            );
            var url = _baseUrl + _storyAIModel;

            // Retry mechanism for API calls
            int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // Create a new HttpRequestMessage for each request
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = requestContent
                    })
                    {
                        // Set the authorization header
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                        // Make the POST request
                        var response = await _httpClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            // Parse the response to get the story content
                            var responseContent = await response.Content.ReadAsStringAsync();
                            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                            string story = jsonResponse[0]["generated_text"].ToString();
                            if (!string.IsNullOrEmpty(story))
                            {
                                int delimiterIndex = story.IndexOf("---");

                                string dataAfterDelimiter = string.Empty;

                                // Check if the delimiter exists
                                if (delimiterIndex != -1 && delimiterIndex + 3 < story.Length)
                                {
                                    // Get everything after the delimiter
                                    story = story.Substring(delimiterIndex + 3).Trim();
                                }
                                return new ContentGenerationResponseSM()
                                {
                                    ContentResponse = story
                                };
                            }

                            // Return null if no story is generated
                            return null;
                        }
                        else
                        {
                            var errorDetails = await response.Content.ReadAsStringAsync();
                            throw new SMSException(
                                ApiErrorTypeSM.Fatal_Log,
                                "Story generation failed.",
                                "We could not generate the requested story at the moment. Please try again later."
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and retry if retries are remaining
                    if (retry == maxRetries - 1)
                    {
                        throw new SMSException(
                            ApiErrorTypeSM.Fatal_Log,
                            "Multiple attempts to generate the story have failed.",
                            "We encountered repeated issues while processing your request. Please try again later."
                        );
                    }
                }
            }

            return null; // Return null if the request ultimately fails
        }


        #region Generate Prompt

        public string GeneratePrompt(ContentGenerationRequestSM request)
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine($"Create a {request.Genre.ToString()} {request.ContentType.ToString()} with the theme '{request.Theme}'.");
            promptBuilder.AppendLine($"The {request.ContentType.ToString()} should be suitable for the age group: {GetAgeGroupDescription(request.AgeGroup)}.");
            promptBuilder.AppendLine("Here are the characters:");

            foreach (var character in request.FictionalCharacters)
            {
                promptBuilder.AppendLine($"- {character.Name}: {character.Role}");
            }

            return promptBuilder.ToString().Trim();
        }

        private string GetAgeGroupDescription(AgeGroupSM ageGroup)
        {
            return ageGroup switch
            {
                AgeGroupSM.Child => "0-12 years",
                AgeGroupSM.Teen => "13-18 years",
                AgeGroupSM.Adult => "19-59 years",
                AgeGroupSM.Senior => "60+ years",
                _ => "Unknown age group"
            };
        }

        #endregion Generate Prompt

        #endregion Generate Content
    }
}
