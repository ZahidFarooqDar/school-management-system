using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSBAL.Projects.AzureAI;
using SMSBAL.Projects.HuggingFace;
using SMSConfig.Configuration;
using SMSDAL.Context;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.AzureAI;
using SMSServiceModels.v1.General.HuggingFace;

namespace SMSBAL.Projects.BaseAIProcess
{
    public class BaseAIProcess : SMSBalBase
    {
        private readonly APIConfiguration _configuration;
        private readonly HuggingfaceProcess _huggingfaceProcess;
        private readonly AzureAIProcess _azureAIProcess;
        private readonly string _huggingfaceProcessingModel;
        private readonly string _azureProcessingModel;
        public BaseAIProcess(IMapper mapper, ApiDbContext apiDbContext, APIConfiguration configuration, HuggingfaceProcess huggingfaceProcess, AzureAIProcess azureAIProcess)
            :base(mapper, apiDbContext)
        {
            _configuration = configuration;
            _azureAIProcess = azureAIProcess;
            _huggingfaceProcess = huggingfaceProcess;
            _azureProcessingModel = configuration.AzureProcessingModel;
            _huggingfaceProcessingModel = configuration.HuggingFaceProcessingModel;
        }

        #region AzureAI Base Methods        

        #region Text Extraxtion Base Model

        public async Task<AzureAIResponseSM> BaseMethodForTextExtraction(ImageDataSM objSM)
        {
            if (string.IsNullOrWhiteSpace(_azureProcessingModel))
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "The AI model used for text extraction is not specified.",
                    "Please specify a valid AI model for text extraction."
                );
            }

            switch (_azureProcessingModel)
            {
                case "AzureAI":
                    return await _azureAIProcess.ExtractTextFromBase64ImageAsync(objSM);
                case "HuggingFaceAI":
                    return await _huggingfaceProcess.ExtractTextUsingLLamaHuggingFaceModel(objSM);
                case "CohereAI":
                    return await _huggingfaceProcess.ExtractTextUsingLLamaHuggingFaceModel(objSM);


                default:
                    throw new SMSException(
                        ApiErrorTypeSM.Fatal_Log,
                        "Unsupported AI model",
                        $"The AI model '{_azureProcessingModel}' is not supported."
                    );
            }
        }

        public async Task<AzureAIResponseSM> BaseMethodForTextExtractionFromPdfImages(ImageDataSM objSM)
        {
            if (string.IsNullOrWhiteSpace(_azureProcessingModel))
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "The AI model used for summarization is not specified.",
                    "Please specify a valid AI model for summarization."
                );
            }

            switch (_azureProcessingModel)
            {
                case "AzureAI":
                    return await _azureAIProcess.ExtractTextFromBase64ImageAsync(objSM);
                case "HuggingFaceAI":
                    return await _huggingfaceProcess.ExtractTextUsingLLamaHuggingFaceModel(objSM);
                case "CohereAI":
                    return await _huggingfaceProcess.ExtractTextUsingLLamaHuggingFaceModel(objSM);


                default:
                    throw new SMSException(
                        ApiErrorTypeSM.Fatal_Log,
                        "Unsupported AI model",
                        $"The AI model '{_azureProcessingModel}' is not supported."
                    );
            }
        }

        #endregion Text Extraxtion Base Model

        #region Text Translation Base Method

        public async Task<AzureAIResponseSM> BaseMethodForTextTranslation(TranslationRequestSM objSM)
        {
            if (string.IsNullOrWhiteSpace(_azureProcessingModel))
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "The AI model used for translation is not specified.",
                    "Please specify a valid AI model for translation."
                );
            }

            switch (_azureProcessingModel)
            {
                case "AzureAI":
                    return await _azureAIProcess.TranslateTextAsync(objSM);
                case "HuggingFaceAI":
                    var input = new TranslationRequestSM()
                    {
                        Text = objSM.Text,
                        Language = objSM.Language
                    };
                    var res = await _huggingfaceProcess.TranslateTextAsync(input);
                    if (res != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = res.TextResponse
                        };
                    }
                    return null;

                case "CohereAI":
                    var cohereInput = new TranslationRequestSM()
                    {
                        Text = objSM.Text,
                        Language = objSM.Language
                    };
                    var cohereRes = await _huggingfaceProcess.TranslateTextUsingCohereAsync(cohereInput);
                    if (cohereRes != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = cohereRes.TextResponse
                        };
                    }
                    return null;

                default:
                    throw new SMSException(
                        ApiErrorTypeSM.Fatal_Log,
                        "Unsupported AI model",
                        $"The AI model '{_azureProcessingModel}' is not supported."
                    );
            }
        }

        #endregion Text Translation Base Method

        #region Text Summerization Base Method

        public async Task<AzureAIResponseSM> BaseMethodForShortSummarization(AzureAIRequestSM objSM)
        {
            if (string.IsNullOrWhiteSpace(_azureProcessingModel))
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "The AI model used for summarization is not specified.",
                    "Please specify a valid AI model for summarization."
                );
            }

            switch (_azureProcessingModel)
            {
                case "AzureAI":
                    return await _azureAIProcess.TextSummarizeAsync(objSM);

                case "CohereAI":
                    var cohereInput = new HuggingFaceRequestSM()
                    {
                        InputRequest = objSM.TextInput
                    };
                    var cohereRes = await _huggingfaceProcess.GenerateSummaryUsingCohereAsync(cohereInput, true);
                    if (cohereRes != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = cohereRes.TextResponse
                        };
                    }
                    return null;

                case "HuggingFaceAI":
                    var huggingFaceInput = new HuggingFaceRequestSM()
                    {
                        InputRequest = objSM.TextInput
                    };
                    var huggingFaceRes = await _huggingfaceProcess.GenerateSummaryUsingCohereAsync(huggingFaceInput, true);
                    if (huggingFaceRes != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = huggingFaceRes.TextResponse
                        };
                    }
                    return null;

                default:
                    throw new SMSException(
                        ApiErrorTypeSM.Fatal_Log,
                        "Unsupported AI model",
                        $"The AI model '{_azureProcessingModel}' is not supported."
                    );
            }
        }


        public async Task<AzureAIResponseSM> BaseMethodForExtensiveSummarization(AzureAIRequestSM objSM)
        {
            if (string.IsNullOrWhiteSpace(_azureProcessingModel))
            {
                throw new SMSException(
                    ApiErrorTypeSM.Fatal_Log,
                    "The AI model used for summarization is not specified.",
                    "Please specify a valid AI model for summarization."
                );
            }

            switch (_azureProcessingModel)
            {
                case "AzureAI":
                    return await _azureAIProcess.ExtensiveSummarizeAsync(objSM);

                case "CohereAI":
                    var cohereInput = new HuggingFaceRequestSM()
                    {
                        InputRequest = objSM.TextInput
                    };
                    var cohereRes = await _huggingfaceProcess.GenerateSummaryUsingCohereAsync(cohereInput, false);
                    if (cohereRes != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = cohereRes.TextResponse
                        };
                    }
                    return null;
                case "HuggingFaceAI":
                    var input = new HuggingFaceRequestSM()
                    {
                        InputRequest = objSM.TextInput
                    };
                    var res = await _huggingfaceProcess.GenerateSummaryUsingCohereAsync(input, false);
                    if (res != null)
                    {
                        return new AzureAIResponseSM()
                        {
                            TextResponse = res.TextResponse
                        };
                    }
                    return null;

                default:
                    throw new SMSException(
                        ApiErrorTypeSM.Fatal_Log,
                        "Unsupported AI model",
                        $"The AI model '{_azureProcessingModel}' is not supported."
                    );
            }
        }


        #endregion Text Summerization Base Method

        #endregion AzureAI Base Methods

        #region HuggingFace AI Base Methods


        #endregion Hugging Face AI Base Methods        

        #region Audio Summary

        public async Task<AudioTranscriptionResponseSM> AudioSummerization(AudioTranscriptionRequestSM objSM)
        {
            var audioTranscription = await _huggingfaceProcess.TranscribeAudioUsingHuggingFaceAsync(objSM);
            if (audioTranscription.Response.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "The request took longer than expected and timed out. Please try again in a moment.",
                    $"The request took longer than expected and timed out. Please try again in a moment.");
            }
            var input = new AzureAIRequestSM()
            {
                TextInput = audioTranscription.Response
            };
            var response = await BaseMethodForExtensiveSummarization(input);
            if(response != null)
            {
                return new AudioTranscriptionResponseSM()
                {
                    Response = response.TextResponse
                };
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "The request took longer than expected and timed out. Please try again in a moment.",
                    $"The request took longer than expected and timed out. Please try again in a moment.");

        }

        #endregion Audio Summary
    }
}
