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
    using Microsoft.Bot.Builder.Ai.QnA;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json.Linq;

    /// <summary>Defines the Contoso Cafe bot.</summary>
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// Gets a template help message.
        /// </summary>
        private static IActivity HelpResponse { get; }
            = MessageFactory.Text("Type `book a table` to make a reservation.");

        /// <summary>
        /// Gets a template "I don't understand" message.
        /// </summary>
        private static IActivity DontKnowResponse { get; }
            = MessageFactory.Text("I'm sorry, I don't understand.\n\n" +
                "Type `book a table` to make a reservation.");

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
        /// Gets the Contoso Cafe QnA service and knowledgebase.
        /// </summary>
        private QnAMaker QnA { get; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContosoCafeBot"/> class.
        /// </summary>
        /// <param name="accessors">State property accessors.</param>
        /// <param name="qna">The Contoso Cafe QnA service and knowledgebase.</param>
        public ContosoCafeBot(StateAccessors accessors, QnAMaker qna)
        {
            this.ConvAccessor = accessors?.ConvData ?? throw new ArgumentNullException(nameof(accessors));
            this.QnA = qna ?? throw new ArgumentNullException(nameof(qna));
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
                await this.HandleSystemActivity(context);
            }
            else
            {
                // Get conversation state and establish a dialog context.
                var data = await this.ConvAccessor.GetAsync(context, () => new ConversationData());
                var dc = this.TableDialog.CreateContext(context, data.DialogState);

                // Capture any input text, and check for "cancel" before continuing any active dialog.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

                if (CancelPhrases.Contains(text))
                {
                    // If there's no active dialog, this is a no-op.
                    dc.EndAll();

                    // Send a cancellation completed message and finish turn.
                    await context.SendActivityAsync("Sure.. Let's start over");
                    return;
                }

                // If there's no active dialog, this is a no-op.
                await dc.ContinueAsync();

                // If there were an active dialog, then it should have replied to the user.
                if (!context.Responded)
                {
                    switch (text)
                    {
                        case "help":

                            // Provide some guidance to the user.
                            await context.SendActivityAsync("Type `book a table` to make a reservation.");
                            break;

                        case "book":
                        case "table":
                        case "book table":
                        case "book a table":

                            // Start the "book a table" dialog.
                            await dc.BeginAsync(nameof(BookATable), data.DialogState);
                            break;

                        default:
                            break;
                    }
                }

                if (!context.Responded)
                {
                    // Field any questions the user has asked.
                    var answers = await this.QnA.GetAnswersAsync(context);
                    if (answers is null)
                    {
                        // Output trace information to the Emulator.
                        // This does not generate a response to the user.
                        await context.TraceActivityAsync("Call to the QnA Maker service failed.");
                    }
                    else if (answers.Any())
                    {
                        // If the service produced one or more answers, send the first one.
                        await context.SendActivityAsync(answers[0].Answer);
                    }
                }

                if (!context.Responded)
                {
                    // Provide a default response for anything we don't understand.
                    await context.SendActivityAsync(DontKnowResponse);
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
                                MessageFactory.Text("How can I help you? (Type `book a table` to set up a table reservation.)"),
                            });
                    }

                    break;
            }
        }
    }
}
