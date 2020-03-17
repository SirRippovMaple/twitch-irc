using NUnit.Framework;
using Shouldly;
using Trumpi.Twitch.Irc.Internal;

namespace Trumpi.Twitch.Irc.UnitTests.Internal
{
    [TestFixture]
    public class IrcMessageParserTests
    {
        [Test]
        public static void Test()
        {
            const string line =
                "@badge-info=subscriber/3;badges=subscriber/3,premium/1;color=#0000FF;display-name=CDubTheRIPper;" +
                "emotes=;flags=;id=fb706dcf-7282-40af-9167-1304ff1ba6c8;mod=0;room-id=26490481;subscriber=1;" +
                "tmi-sent-ts=1583880341354;turbo=0;user-id=54604605;user-type=" +
                " :cdubtheripper!cdubtheripper@cdubtheripper.tmi.twitch.tv PRIVMSG #summit1g :monkaS";

            var message = IrcMessage.ParseLine(line);
            message.ShouldSatisfyAllConditions(() =>
            {
                message.Command.ShouldBe("PRIVMSG");
                message.Parameters.Count.ShouldBe(2);
                message.Parameters[0].ShouldBe("#summit1g");
                message.Parameters[1].ShouldBe("monkaS");
            });
        }
    }
}