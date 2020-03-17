using System;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    public class TerminalMiddleware<T> : IIrcMiddleware<T>
    {
        private ITwitchChatStreamNotifications _prev;

        public void Dispose()
        {
        }

        public Task<bool> InitializeAsync(ITwitchChatStreamNotifications prev, T param)
        {
            _prev = prev;
            return Task.FromResult(true);
        }

        public Task ShutdownAsync()
        {
            _prev = null;
            return Task.CompletedTask;
        }

        public Task WriteMessageAsync(IrcMessage message)
        {
            return Task.CompletedTask;
        }

        public Task HandleMessageAsync(IrcMessage message)
        {
            return _prev.OnMessageAsync(message);
        }

        public Task HandleErrorAsync(Exception exception)
        {
            return _prev?.OnErrorAsync(exception);
        }

        public bool Healthy => true;
    }
}