using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MovieRecommenderBot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(IConfiguration configuration,
            ILogger<BotFrameworkHttpAdapter> logger,
            TelemetryInitializerMiddleware telemetryInitializerMiddleware)
            : base(configuration, logger)
        {
            Use(telemetryInitializerMiddleware);
            OnTurnError = async (turnContext, exception) =>
            {
                logger.LogError($"Exception caught : {exception.Message}");
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");
            };
        }
    }
}
