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
    public class ReadLoopMiddlewareTests
    {
        [Test]
        public async Task CanReadMessage()
        {
            var notifier = new FakeNotifier();
            using var subject = CreateSubject();
            var pipe = new System.IO.Pipelines.Pipe();
            await subject.InitializeAsync(notifier, pipe.Reader.AsStream());
            await pipe.Writer.WriteAsync(new Memory<byte>(Encoding.UTF8.GetBytes(":tmi.twitch.tv 001 drangrybot :Welcome, GLHF!\r\n")));
            var message = notifier.WaitForMessages(1).First();

            message.Command.ShouldBe("001");
            message.Parameters.Count.ShouldBe(2);
            message.Parameters[0].ShouldBe("drangrybot");
            message.Parameters[1].ShouldBe("Welcome, GLHF!");
            notifier.ExceptionList.Count.ShouldBe(0);
        }

        [Test]
        public async Task CanShutdown()
        {
            var notifier = new FakeNotifier();
            using var subject = CreateSubject();
            var pipe = new System.IO.Pipelines.Pipe();
            await subject.InitializeAsync(notifier, pipe.Reader.AsStream());
            await subject.ShutdownAsync();
        }
        
        [Test]
        public async Task ReadLoopCanShutdownWithoutInitialization()
        {
            using var subject = CreateSubject();
            await subject.ShutdownAsync();
        }

        private static ReadLoopMiddleware CreateSubject()
        {
            return new ReadLoopMiddleware(new TerminalMiddleware<Stream>());
        }
    }
}