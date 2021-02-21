using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace MovieRecommenderBot
{
    public class Bot<T> : ActivityHandler where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        private readonly IStatePropertyAccessor<string> _userNameStateProperty;

        public Bot(ConversationState conversationState, UserState userState, T dialog)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            _userNameStateProperty = userState.CreateProperty<string>("UserName");
        }

        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var message = turnContext.Activity.Text;
            if (await _userNameStateProperty.GetAsync(turnContext, () => default, cancellationToken) is null)
            {
                await _userNameStateProperty.SetAsync(turnContext, message, cancellationToken);

                var reply = MessageFactory.Text($"Hi, {message}!");
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }

            await Dialog.RunAsync(turnContext,
                ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext,
            CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private async Task SendWelcomeMessageAsync(
            ITurnContext turnContext,
            CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Hello and welcome to Movie Recommender!"),
                cancellationToken);

            var reply = MessageFactory.Text("Please, enter your name to login");

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
