using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MovieRecommender;
using MovieRecommender.DataModels;
using MovieRecommender.Services;
using MovieRecommenderBot.CognitiveModels;
using MovieRecommenderBot.Services;

namespace MovieRecommenderBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly RecommendationRecognizer _luisRecognizer;
        private readonly IRecommenderService _recommenderService;
        private readonly IStatePropertyAccessor<string> _userNameStateProperty;
        private readonly DataProcessor _dataProcessor;
        private readonly IMoviePosterService _moviePosterService;
        private string _userName;

        public MainDialog(UserState userState,
            RecommendationRecognizer luisRecognizer,
            IRecommenderService recommenderService,
            DataProcessor dataProcessor,
            IMoviePosterService moviePosterService,
            PickRatingDialog pickRatingDialog)
            : base(nameof(MainDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(pickRatingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                LoginStepAsync,
                CreateRatingStepAsync,
                RecommendationStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);

            _luisRecognizer = luisRecognizer;
            _recommenderService = recommenderService;
            _userNameStateProperty = userState.CreateProperty<string>("UserName");

            _dataProcessor = dataProcessor;
            _moviePosterService = moviePosterService;
        }

        private async Task<DialogTurnResult> LoginStepAsync(
            WaterfallStepContext stepContext,
           CancellationToken cancellationToken)
        {
            _userName = await _userNameStateProperty.GetAsync(stepContext.Context, () => default, cancellationToken);
            
            if (!_recommenderService.Login(_userName))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Loading your initial recommendations...",
                    InputHints.IgnoringInput), cancellationToken);

                await SendMoviesAsync(_dataProcessor.BestMovies, stepContext, cancellationToken);
                await PrintRateAsync(stepContext, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(PickRatingDialog), new List<Rating>(), cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CreateRatingStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (stepContext.Result != null && stepContext.Result is List<Rating> ratings)
            {
                _recommenderService.CreateUserRatings(_userName, ratings);
            }

            var messageText = stepContext.Options?.ToString();
            messageText ??= "What can I help? Say something like \"recommend movies\"";

            var promptMessage = MessageFactory.Text(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = promptMessage
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> RecommendationStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var luisResult = await _luisRecognizer.RecognizeAsync<RecommendationLuis>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case RecommendationLuis.Intent.GetRecommendations:
                    var recommendations = _recommenderService.GetRecommendations(_userName);

                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Loading your recommendations..."),
                        cancellationToken);

                    await SendRecommendationsAsync(recommendations, stepContext, cancellationToken);
                    await PrintRateAsync(stepContext, cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(PickRatingDialog), new List<Rating>(), cancellationToken);

                case RecommendationLuis.Intent.Help:
                    var helpMessageText = "Try asking me to 'recommend movies'.";
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, helpMessageText, cancellationToken);

                case RecommendationLuis.Intent.Cancel:
                    var cancelMessageText = "Bye bye!";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(cancelMessageText,
                        InputHints.IgnoringInput), cancellationToken);
                    return await stepContext.CancelAllDialogsAsync(cancellationToken);

                default:
                    var notUnderstandMessageText = "Sorry, I didn't get that. Please try asking in a different way";
                    var notUnderstandMessage = MessageFactory.Text(notUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(notUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            _recommenderService.UpdateUserRatings(_userName, stepContext.Result as List<Rating>);
            var promptMessage = "What else can I do?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        private static async Task PrintRateAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var rateRecommendations = "Please, rate recommendations from 1 to 5 " +
                                      "for more personalized recommendations in future. " +
                                      "Enter some message, for example, 'next', to continue.";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(rateRecommendations), cancellationToken);
        }

        private async Task SendMoviesAsync(
            IEnumerable<Movie> movies,
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var reply = stepContext.Context.Activity.CreateReply();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            var paths = new[] { ".", "Resources", "movieCard.json" };
            var adaptiveCardJson = await File.ReadAllTextAsync(Path.Combine(paths), cancellationToken);
            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(adaptiveCardJson);

            foreach (var movie in movies)
            {
                var card = template.Expand(new
                {
                    Title = _dataProcessor.GetMovieTitle(movie.Title),
                    Genres = string.Join(" | ", movie.Genres),
                    MovieId = movie.Id,
                    ImageUrl = await _moviePosterService.GetPosterLinkAsync((int) movie.Id)
                });

                var adaptiveCardAttachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = AdaptiveCard.FromJson(card).Card
                };

                reply.Attachments.Add(adaptiveCardAttachment);
            }
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
        }

        private async Task SendRecommendationsAsync(
            IEnumerable<Recommendation> recommendations,
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var reply = stepContext.Context.Activity.CreateReply();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            var paths = new[] { ".", "Resources", "recommendationCard.json" };
            var adaptiveCardJson = await File.ReadAllTextAsync(Path.Combine(paths), cancellationToken);
            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(adaptiveCardJson);

            foreach (var recommendation in recommendations)
            {
                var card = template.Expand(new
                {
                    Title = _dataProcessor.GetMovieTitle(recommendation.Movie.Title),
                    MovieId = recommendation.Movie.Id,
                    Genres = string.Join(" | ", recommendation.Movie.Genres),
                    Probability = $"Probability {recommendation.Prediction.Probability * 100: 0.##}%",
                    ImageUrl = await _moviePosterService.GetPosterLinkAsync((int)recommendation.Movie.Id)
                });

                var adaptiveCardAttachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = AdaptiveCard.FromJson(card).Card
                };

                reply.Attachments.Add(adaptiveCardAttachment);
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
        }
    }
}