using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReferenceBot
{
    /// <summary>
    /// Contains state property accessors for the bot.
    /// </summary>
    public class StateAccessors
    {
        /// <summary>
        /// The identifier for the state property accessor for the book-a-table dialog.
        /// </summary>
        public const string key = "EchoBotStateKey";

        /// <summary>
        /// Gets or sets the state property accessor for the book-a-table dialog.
        /// </summary>
        public IStatePropertyAccessor<EchoState> PropertyAccessor { get; set; }
    }
}
