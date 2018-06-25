using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ContosoCafe
{
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// The set of phrases we recognize as the user cancelling a multi-step
        /// conversation flow.
        /// </summary>
        private static IEnumerable<string> CancelPhrases { get; } = new HashSet<string>
        {
            "cancel", "start over", "stop"
        };

        /// <summary>
        /// Handles incoming activities from a user's channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        public async Task OnTurn(ITurnContext context)
        {
            // Handle message and non-message activities differently.
            if (context.Activity.Type != ActivityTypes.Message)
            {
                // Handle any non-message activity.
                await HandleSystemActivity(context);
            }
            else
            {
                // Get conversation state and establish a dialog context.
                var state = ConversationState<ConversationData>.Get(context);
                var dc = BookATable.Instance.CreateContext(context, state.DialogState);

                // Capture any input text.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

                // Check for "cancel" before continuing any active dialog.
                if (CancelPhrases.Contains(text))
                {
                    // If there's no active dialog, this is a no-op.
                    dc.EndAll();

                    // Send a cancellation message and finish turn.
                    await context.SendActivity("Sure.. Let's start over");
                }

                if (!context.Responded)
                {
                    // Continue any active dialog. If there's no active dialog, this is a no-op.
                    await dc.Continue();
                }

                if (!context.Responded)
                {
                    // Handle any "command-like" input from the user.
                    switch (text)
                    {
                        case "who are you":
                        case "who are you?":
                            // Stub for answering questions.
                            await context.SendActivity("Hi, I'm the Contoso Cafe bot.");
                            break;

                        case "book table":
                        case "book a table":
                            // Start the "book a table" dialog.
                            await dc.Begin(nameof(BookATable), state.DialogState);
                            break;

                        case "help":
                            // Provide some guidance to the user.
                            await context.SendActivity("Type `book a table` to make a reservation.");
                            break;

                        default:
                            // Provide a default response for anything we didn't understand.
                            await context.SendActivity("I'm sorry; I do not understand.");
                            goto case "help";
                    }
                }
            }
        }

        /// <summary>
        /// Handle any non-message activities from the channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        private async Task HandleSystemActivity(ITurnContext context)
        {
            switch (context.Activity.Type)
            {
                // Not all channels send a ConversationUpdate activity.
                // However, both the Emulator and WebChat do.
                case ActivityTypes.ConversationUpdate:

                    // If a user is being added to the conversation, send them an initial greeting.
                    var update = context.Activity.AsConversationUpdateActivity();
                    if (update.MembersAdded.Any(member => member.Id != update.Recipient.Id))
                    {
                        await context.SendActivities(
                            new IActivity[]
                            {
                                MessageFactory.Text("Hello, I'm the Contoso Cafe bot."),
                                MessageFactory.Text("How can I help you? (Type `book a table` to set up a table reservation.)")
                            });
                    }
                    break;
            }
        }
    }
}
