using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;

namespace ContosoCafe
{
    public class BookATable : DialogSet
    {
        public static Lazy<BookATable> Singleton { get; } = new Lazy<BookATable>(new BookATable());

        private BookATable()
        {
            this.Add(nameof(BookATable), new WaterfallStep[]
            {
                // Finish up booking a table.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    dc.ActiveDialog.State = new Dictionary<string,object>();

                    // Pretend we have logic here, but just fall through to the end of the dialog.
                    await next();
                },
                async (dc, args, next) =>
                {
                    // Send a confirmation message.
                    var typing = Activity.CreateTypingActivity();
                    var delay = new Activity { Type = "delay", Value = 3000 };
                    await dc.Context.SendActivities(
                        new IActivity[]
                        {
                            typing, delay,
                            MessageFactory.Text("Your table is booked. Reference number: #K89HG38SZ")
                        });
                }
            });
        }
    }
}
