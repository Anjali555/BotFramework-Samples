using bot;
using Microsoft.Bot.Builder.Adapters;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace test
{
    public class MessageTests
    {
        [Fact]
        public async Task DefaultMessage()
        {
            var bot = new TalkingBot();
            var adapter = new TestAdapter();
            var input = "hi";
            var expectedOutput = "This is the wrong output.";

            // This fails to fail.
            var flow = new TestFlow(adapter, bot.OnTurn);
            flow.Test(input, expectedOutput, "variation a");
            await flow.StartTest();

           // This fails as expected.
            await new TestFlow(adapter, bot.OnTurn)
                .Test(input, expectedOutput, "variation b")
                .StartTest();
        }
    }
}
