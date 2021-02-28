using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using MovieRecommender.DataModels;

namespace MovieRecommenderBot.Dialogs
{
    public class PickRatingDialog : ComponentDialog
    {
        public PickRatingDialog()
            : base(nameof(PickRatingDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                WaitingRatingStepAsync,
                PickRatingStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> WaitingRatingStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new DialogTurnResult(DialogTurnStatus.Waiting));
        }

        private async Task<DialogTurnResult> PickRatingStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var ratings = stepContext.Options as List<Rating> ?? new List<Rating>();

            if(ratings.Count >= 5)
            {
                return await stepContext.EndDialogAsync(ratings, cancellationToken);
            }

            var result = stepContext.State.Values.First().ToString();

            var activity = JsonDocument.Parse(result)
                .RootElement
                .GetProperty("activity");

            if (activity.TryGetProperty("value", out var value))
            {
                ratings.Add(JsonSerializer.Deserialize<Rating>(value.GetRawText()));
                return await stepContext.ReplaceDialogAsync(nameof(PickRatingDialog), ratings, cancellationToken);
            }

            return await stepContext.EndDialogAsync(ratings, cancellationToken);
        }
    }
}