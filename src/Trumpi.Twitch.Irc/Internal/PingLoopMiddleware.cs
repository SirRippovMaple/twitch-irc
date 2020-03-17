using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class PingLoopMiddleware : LoopMiddleware<Stream>
    {
        private readonly TimeSpan _pingInterval;

        public PingLoopMiddleware(IIrcMiddleware<Stream> next, TimeSpan pingInterval = default) : base(next)
        {
            _pingInterval = pingInterval == default ? TimeSpan.FromSeconds(15) : pingInterval;
        }

        protected override async Task<bool> DoLoopAsync(CancellationToken cancellationToken)
        {
            var running = !cancellationToken.WaitHandle.WaitOne(_pingInterval);
            if(running)
            {
                await WriteMessageAsync(new IrcMessage("PING", Guid.NewGuid().ToString()));
            }

            return running;
        }

        protected override async Task<bool> OnMessageImplAsync(IrcMessage message)
        {
            if (message.Command == "PING")
            {
                await WriteMessageAsync(new IrcMessage("PONG"));
                return true;
            }

            return await base.OnMessageImplAsync(message);
        }
    }
}