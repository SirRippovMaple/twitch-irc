using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class JoinRateLimiter<T> : LoopMiddleware<T>
    {
        private readonly Queue<IrcMessage> _queue = new Queue<IrcMessage>();
        private readonly AutoResetEvent _queueItemsAvailable = new AutoResetEvent(false);
        private DateTime _nextAvailableTime;
        
        public JoinRateLimiter(IIrcMiddleware<T> next) : base(next)
        {
        }

        protected override Task<T> InitializeImplAsync(T param)
        {
            _nextAvailableTime = DateTime.UtcNow;
            return base.InitializeImplAsync(param);
        }

        public override Task WriteMessageAsync(IrcMessage message)
        {
            if (message.Command == "JOIN" || message.Command == "PART")
            {
                _queue.Enqueue(message);
                _queueItemsAvailable.Set();
                return Task.CompletedTask;
            }
            else
            {
                return base.WriteMessageAsync(message);
            }
        }

        protected override async Task<bool> DoLoopAsync(CancellationToken cancellationToken)
        {
            var handle = WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, _queueItemsAvailable });
            if (handle == 0)
            {
                return false;
            }

            while (!cancellationToken.IsCancellationRequested && _queue.Count > 0)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var item = _queue.Dequeue();
                    if (_nextAvailableTime > now)
                    {
                        if (cancellationToken.WaitHandle.WaitOne(_nextAvailableTime.Subtract(now)))
                        {
                            return false;
                        }
                    }

                    await base.WriteMessageAsync(item);

                    _nextAvailableTime = now.AddMilliseconds(333);
                }
                catch (Exception e)
                {
                    await HandleErrorAsync(e);
                }
            }

            return true;
        }
    }
}