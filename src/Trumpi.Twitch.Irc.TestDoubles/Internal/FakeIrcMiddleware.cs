using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trumpi.Twitch.Irc.Internal;

namespace Trumpi.Twitch.Irc.TestDoubles.Internal
{
    public class FakeIrcMiddleware<T> : IIrcMiddleware<T>
    {
        private ITwitchChatStreamNotifications _prev;
        private ManualResetEvent _initializeWait = new ManualResetEvent(true);

        public int InitializeCount { get; private set; } = 0;
        public int ShutdownCount { get; private set; } = 0;
        public List<IrcMessage> WrittenMessages { get; } = new List<IrcMessage>();
        
        public void Dispose()
        {
        }

        public void RegisterWaitForInitialize()
        {
            _initializeWait.Reset();
        }

        public bool WaitForInitialize(TimeSpan timeout)
        {
            return _initializeWait.WaitOne(timeout);
        }
        
        public Task<bool> InitializeAsync(ITwitchChatStreamNotifications prev, T param)
        {
            _prev = prev;
            InitializeCount += 1;
            _initializeWait.Set();    
            return Task.FromResult(true);
        }

        public Task ShutdownAsync()
        {
            ShutdownCount += 1;
            return Task.CompletedTask;
        }

        public Task WriteMessageAsync(IrcMessage message)
        {
            WrittenMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task HandleMessageAsync(IrcMessage message)
        {
            return _prev?.OnMessageAsync(message);
        }

        public Task HandleErrorAsync(Exception exception)
        {
            return _prev.OnErrorAsync(exception);
        }

        public bool Healthy => true;
    }
}