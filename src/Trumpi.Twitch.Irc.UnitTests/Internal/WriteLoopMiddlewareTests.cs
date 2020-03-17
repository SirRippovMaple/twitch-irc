using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trumpi.Twitch.Irc.Internal;
using Trumpi.Twitch.Irc.TestDoubles;

namespace Trumpi.Twitch.Irc.UnitTests.Internal
{
    [TestFixture]
    public class WriteLoopMiddlewareTests
    {
        [Test]
        public async Task WriteLoop()
        {
            var notifier = new FakeNotifier();

            using var subject = CreateSubject();
            var pipe = new System.IO.Pipelines.Pipe();
            await subject.InitializeAsync(notifier, pipe.Writer.AsStream());
            await subject.WriteMessageAsync(new IrcMessage("TEST"));
            using var reader = new StreamReader(pipe.Reader.AsStream());
            var s = await reader.ReadLineAsync();
            s.ShouldBe("TEST");
            notifier.ExceptionList.Count.ShouldBe(0);
        }
        
        private static WriteLoopMiddleware CreateSubject()
        {
            return new WriteLoopMiddleware(new FakeFloodPreventer(), new TerminalMiddleware<Stream>());
        }
    }
}