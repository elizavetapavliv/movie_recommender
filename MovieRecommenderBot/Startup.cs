using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ML;
using MovieRecommender;
using MovieRecommender.Services;
using MovieRecommenderBot.Bots;
using MovieRecommenderBot.CognitiveModels;
using MovieRecommenderBot.Dialogs;
using MovieRecommenderBot.Options;
using MovieRecommenderBot.Services;

namespace MovieRecommenderBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();
            services.AddSingleton<PickRatingDialog>();
            services.AddSingleton<MainDialog>();
            services.AddTransient<IBot, Bot<MainDialog>>();

            services.AddSingleton<RecommendationRecognizer>();
            services.Configure<LuisOptions>(Configuration.GetSection(LuisOptions.Luis));

            services.AddSingleton<MLContext>();
            services.AddSingleton<DataProcessor>();
            services.AddSingleton<Predictor>();
            services.AddSingleton<UserStorage>();
            services.AddSingleton<UserProfile>();
            services.AddSingleton<IRecommenderService, RecommenderService>();

            services.AddHttpClient();
            services.AddSingleton<IMoviePosterService, MoviePosterService>();
            services.Configure<UriOptions>(Configuration.GetSection(UriOptions.Uri));
            services.Configure<LoginOptions>(Configuration.GetSection(LoginOptions.Login));

            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton(sp =>
            {
                var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                var loggerMiddleware = sp.GetService<TelemetryLoggerMiddleware>();
                return new TelemetryInitializerMiddleware(httpContextAccessor, loggerMiddleware);
            });
            services.AddSingleton(sp =>
            {
                var telemetryClient = sp.GetService<IBotTelemetryClient>();
                return new TelemetryLoggerMiddleware(telemetryClient, true);
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseCors()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}
