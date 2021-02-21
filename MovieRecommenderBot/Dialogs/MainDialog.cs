using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public MainDialog(UserState userState,
            RecommendationRecognizer luisRecognizer,
            IRecommenderService recommenderService,
            DataProcessor dataProcessor,
            IMoviePosterService moviePosterService)
            : base(nameof(MainDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);

            _luisRecognizer = luisRecognizer;
            _recommenderService = recommenderService;
            _userNameStateProperty = userState.CreateProperty<string>("UserName");

            _dataProcessor = dataProcessor;
            _moviePosterService = moviePosterService;
        }

        private async Task<DialogTurnResult> IntroStepAsync(
            WaterfallStepContext stepContext,
           CancellationToken cancellationToken)
        {
            var userName = await _userNameStateProperty.GetAsync(stepContext.Context, () => default, cancellationToken);
            
            if (!_recommenderService.Login(userName))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Loading your initial recommendations...",
                    InputHints.IgnoringInput), cancellationToken);

                await SendMoviesAsync(_dataProcessor.BestMovies, stepContext, cancellationToken);
                await PrintRateAsync(stepContext, cancellationToken);

                //_recommenderService.CreateUserRatings(userName, GetRatingsFromUser());

                //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = reply });
            }
        

            if (!_luisRecognizer.IsConfigured)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var messageText = stepContext.Options?.ToString();
            messageText ??= "What can I help? Say something like \"recommend movies\"";

            var promptMessage = MessageFactory.Text(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = promptMessage
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(
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
                    var userName = await _userNameStateProperty.GetAsync(stepContext.Context, () => default, cancellationToken);
                    var recommendations = _recommenderService.GetRecommendations(userName);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Loading your recommendations...",
                        InputHints.IgnoringInput), cancellationToken);

                    await SendRecommendationsAsync(recommendations, stepContext, cancellationToken);
                    await PrintRateAsync(stepContext, cancellationToken);

                    //Save ratings

                    return await stepContext.NextAsync(null, cancellationToken);

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
            //TODO: print recommendations
                //var messageText = "Your link for booking ticket";
                //messageText = language == TranslationSettings.defaultLanguage ? messageText :
                //(await translationService.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(messageText) }, language))[0];
                //var reply = MessageFactory.Attachment(new List<Attachment>());
                //var heroCard = new HeroCard()
                //{
                //    Title = messageText,
                //    Tap = new CardAction()
                //    {
                //        Type = ActionTypes.OpenUrl,
                //        Value = cinemaService.GetOrderTicketUri(result.Movie.Code, result.Show.Id)
                //    }
                //};
                //reply.Attachments.Add(heroCard.ToAttachment());
                //await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            
            var promptMessage = "What else can I do?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        private static async Task PrintRateAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var rateRecommendations = "Please, rate recommendations from 1 to 5 " +
                                      "for more personalized recommendations in future.";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(rateRecommendations,
                InputHints.IgnoringInput), cancellationToken);
        }

        private async Task SendMoviesAsync(
            IEnumerable<Movie> movies,
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var reply = stepContext.Context.Activity.CreateReply();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            foreach (var movie in movies)
            {
                var heroCard = new HeroCard
                {
                    Title = _dataProcessor.GetMovieTitle(movie.Title),
                    Subtitle = string.Join(" | ", movie.Genres),
                    Images = new List<CardImage>
                    {
                        new CardImage
                        {
                            Url = await _moviePosterService.GetPosterLinkAsync((int)movie.Id)
                        }
                    }
                };
                reply.Attachments.Add(heroCard.ToAttachment());
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

            foreach (var recommendation in recommendations)
            {
                var heroCard = new HeroCard
                {
                    Title = _dataProcessor.GetMovieTitle(recommendation.Movie.Title),
                    Subtitle = string.Join(" | ", recommendation.Movie.Genres),
                    Text = $"Probability {recommendation.Prediction.Probability * 100: 0.##}%",
                    Images = new List<CardImage>
                    {
                        new CardImage
                        {
                            Url = await _moviePosterService.GetPosterLinkAsync((int) recommendation.Movie.Id)
                        }
                    }
                };
                reply.Attachments.Add(heroCard.ToAttachment());
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
        }
    }
}