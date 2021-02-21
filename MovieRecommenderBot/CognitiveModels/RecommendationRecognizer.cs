using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Options;
using MovieRecommenderBot.Options;
using LuisApplication = Microsoft.Bot.Builder.AI.Luis.LuisApplication;
using LuisPredictionOptions = Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions;
using LuisRecognizer = Microsoft.Bot.Builder.AI.Luis.LuisRecognizer;

namespace MovieRecommenderBot.CognitiveModels
{
    public class RecommendationRecognizer : IRecognizer
    {
        private readonly LuisRecognizer _recognizer;
        public RecommendationRecognizer(IOptions<LuisOptions> options)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(options.Value.AppId) 
                && !string.IsNullOrEmpty(options.Value.APIKey) 
                && !string.IsNullOrEmpty(options.Value.APIHostName);

            if (luisIsConfigured)
            {
                var luisApplication = new LuisRecognizerOptionsV3(new LuisApplication(
                    options.Value.AppId,
                    options.Value.APIKey,
                    "https://" + options.Value.APIHostName))
                {
                    PredictionOptions = new LuisPredictionOptions
                    {
                        IncludeInstanceData = true,
                    }
                };

                _recognizer = new LuisRecognizer(luisApplication);
            }
        }
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, 
            CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, 
            CancellationToken cancellationToken)  where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}