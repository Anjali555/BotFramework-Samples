// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Schema;

    /// <summary>Defines the Contoso Cafe bot.</summary>
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// Gets the set of phrases we recognize as the user cancelling a multi-step
        /// conversation flow.
        /// </summary>
        private static IEnumerable<string> CancelPhrases { get; }
            = new HashSet<string> { "cancel", "start over", "stop" };

        /// <summary>
        /// Gets state property accessors.
        /// </summary>
        private IStatePropertyAccessor<ConversationData> ConvAccessor { get; }

        private BookATable TableDialog { get; } = new BookATable();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContosoCafeBot"/> class.
        /// </summary>
        /// <param name="accessors">State property accessors.</param>
        /// <param name="qna">The Contoso Cafe QnA service and knowledgebase.</param>
        /// <param name="luis">The Contoso Cafe LUIS app and model.</param>
        public ContosoCafeBot(StateAccessors accessors)
        {
            this.ConvAccessor = accessors?.ConvData ?? throw new ArgumentNullException(nameof(accessors));
        }

        /// <summary>
        /// Handles incoming activities from a user's channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        /// <param name="token">blah.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext context, CancellationToken token = default(CancellationToken))
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
                var data = await this.ConvAccessor.GetAsync(context, () => new ConversationData());
                var dc = this.TableDialog.CreateContext(context, data.DialogState);

                // Capture any input text.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

                // Check for "cancel" before continuing any active dialog.
                if (CancelPhrases.Contains(text))
                {
                    // If there's no active dialog, this is a no-op.
                    dc.EndAll();

                    // Send a cancellation message and finish turn.
                    await context.SendActivityAsync("Sure.. Let's start over");
                }

                if (!context.Responded)
                {
                    // Continue any active dialog. If there's no active dialog, this is a no-op.
                    await dc.ContinueAsync();
                }

                if (!context.Responded)
                {
                    // Handle any "command-like" input from the user.
                    switch (text)
                    {
                        case "who are you":
                        case "who are you?":
                            // Stub for answering questions.
                            await context.SendActivityAsync("Hi, I'm the Contoso Cafe bot.");
                            break;

                        case "book table":
                        case "book a table":
                            // Start the "book a table" dialog.
                            await dc.BeginAsync(nameof(BookATable));
                            break;

                        case "help":
                            // Provide some guidance to the user.
                            await context.SendActivityAsync("Type `book a table` to make a reservation.");
                            break;

                        default:
                            // Provide a default response for anything we didn't understand.
                            await context.SendActivityAsync("I'm sorry; I do not understand.");
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
                        await context.SendActivitiesAsync(
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
