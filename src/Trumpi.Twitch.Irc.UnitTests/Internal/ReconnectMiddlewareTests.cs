using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trumpi.Twitch.Irc.Internal;
using Trumpi.Twitch.Irc.TestDoubles;
using Trumpi.Twitch.Irc.TestDoubles.Internal;

namespace Trumpi.Twitch.Irc.UnitTests.Internal
{
    [TestFixture]
    public class ReconnectMiddlewareTests
    {
        [Test]
        public async Task CanInitialize()
        {
            var notifier = new FakeNotifier();
            var fakeIrcMiddleware = new FakeIrcMiddleware<TwitchChatConnectionParameters>();
            using var subject = new ReconnectMiddleware(fakeIrcMiddleware);
            var result = await subject.InitializeAsync(notifier,
                new TwitchChatConnectionParameters("example.com", 6667, false, "bob", "bob"));
            result.ShouldBeTrue();
            fakeIrcMiddleware.InitializeCount.ShouldBe(1);
        }

        [Test]
        public async Task WillAttemptReconnect()
        {
            var notifier = new FakeNotifier();
            var fakeIrcMiddleware = new FakeIrcMiddleware<TwitchChatConnectionParameters>();
            using var subject = new ReconnectMiddleware(fakeIrcMiddleware);
            var result = await subject.InitializeAsync(notifier,
                new TwitchChatConnectionParameters("example.com", 6667, false, "bob", "bob"));
            
            fakeIrcMiddleware.RegisterWaitForInitialize();
            await subject.HandleErrorAsync(new IOException("Test error", new SocketException((int) SocketError.ConnectionAborted)));
            fakeIrcMiddleware.WaitForInitialize(TimeSpan.FromSeconds(1));
            fakeIrcMiddleware.ShutdownCount.ShouldBe(1);
            fakeIrcMiddleware.InitializeCount.ShouldBe(2);
        }
    }
}