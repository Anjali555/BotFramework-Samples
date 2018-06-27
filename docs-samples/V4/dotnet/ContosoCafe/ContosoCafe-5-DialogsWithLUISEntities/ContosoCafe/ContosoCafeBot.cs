using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Ai.QnA;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cognitive.LUIS;

namespace ContosoCafe
{
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// A template help message.
        /// </summary>
        private static IActivity HelpResponse { get; }
            = MessageFactory.Text("Type `book a table` to make a reservation.");

        /// <summary>
        /// A template "I don't understand" message.
        /// </summary>
        private static IActivity DontKnowResponse { get; }
            = MessageFactory.Text("I'm sorry, I don't understand.\n\n" +
                "Type `book a table` to make a reservation.");

        /// <summary>
        /// The set of phrases we recognize as the user cancelling a multi-step
        /// conversation flow.
        /// </summary>
        private static IEnumerable<string> CancelPhrases { get; }
            = new HashSet<string> { "cancel", "start over", "stop" };

        /// <summary>
        /// Singleton reference to the Contoso Cafe QnA service and knowledgebase.
        /// </summary>
        private QnAMaker QnA { get; } = null;

        /// <summary>
        /// Singleton reference to the Contoso Cafe LUIS app and model.
        /// </summary>
        private LuisRecognizer Recognizer { get; } = null;

        /// <summary>
        /// A bot constructor that takes a configuration object.
        /// </summary>
        /// <param name="configuration">A configuration object containing information from our appsettings.json file.</param>
        public ContosoCafeBot(IConfiguration configuration)
        {
            // Create the QnA Maker instance.
            var hostname = configuration["QnAHostname"];
            var endpointKey = configuration["QnAEndpoint"];
            var knowledgebaseId = configuration["QnAKnowledgebaseId"];
            var scoreThreshold = float.Parse(configuration["QnAScoreThreshold"]);
            QnA = new QnAMaker(
                new QnAMakerEndpoint
                {
                    EndpointKey = endpointKey,
                    Host = hostname,
                    KnowledgeBaseId = knowledgebaseId
                },
                new QnAMakerOptions { ScoreThreshold = scoreThreshold }
            );

            // Create the LUIS recognizer for our model.
            var luisRecognizerOptions = new LuisRecognizerOptions { Verbose = true };
            var luisModel = new LuisModel(
                configuration["LuisModel"],
                configuration["LuisSubscriptionKey"],
                new Uri(configuration["LuisUriBase"]),
                LuisApiVersion.V2);
            Recognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, null);
        }

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
                    // Use LUIS to extract intent from the user's input text.
                    var result = await Recognizer.Recognize<CafeLuisModel>(text, new CancellationToken());

                    // Handle the user's input based on the extracted intent.
                    switch (result.TopIntent().intent)
                    {
                        case CafeLuisModel.Intent.Reservation:
                            // Start the "book a table" dialog. Pass in any entities that our LUIS model captured.
                            var dialogArgs = new Dictionary<string, object>();
                            if (result != null)
                            {
                                dialogArgs.Add(BookATable.Keys.LuisArgs, result.Entities);
                            }
                            await dc.Begin(nameof(BookATable), dialogArgs);
                            break;

                        case CafeLuisModel.Intent.Help:
                            // Provide some guidance to the user.
                            await context.SendActivity(HelpResponse);
                            break;

                        case CafeLuisModel.Intent.None:
                        default:
                            break;
                    }
                }

                if (!context.Responded)
                {
                    // Field any questions the user has asked.
                    var answers = await QnA.GetAnswers(text);
                    if (answers is null)
                    {
                        // Output trace information to the Emulator.
                        // This does not generate a response to the user.
                        await context.TraceActivity("Call to the QnA Maker service failed.");
                    }
                    else if (answers.Any())
                    {
                        // If the service produced one or more answers, send the first one.
                        await context.SendActivity(answers[0].Answer);
                    }
                }

                if (!context.Responded)
                {
                    // Provide a default response for anything we don't understand.
                    await context.SendActivity(DontKnowResponse);
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
