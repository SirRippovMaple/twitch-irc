using System;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal abstract class IrcMiddleware<TInitParam, TNextParam> : IIrcMiddleware<TInitParam>, ITwitchChatStreamNotifications
    {
        private ITwitchChatStreamNotifications _prev;
        private readonly IIrcMiddleware<TNextParam> _next;

        protected IrcMiddleware(IIrcMiddleware<TNextParam> next)
        {
            _next = next;
        }

        public Task<bool> InitializeAsync(ITwitchChatStreamNotifications prev, TInitParam param)
        {
            _prev = prev;
            return ReInitializeAsync(param);
        }

        protected async Task<bool> ReInitializeAsync(TInitParam param)
        {
            var nextParam = await InitializeImplAsync(param);
            if (nextParam == null)
            {
                return false;
            }

            if (await _next.InitializeAsync(this, nextParam))
            {
                return await PostInitializeAsync(param);
            }

            return false;
        }
        
        protected virtual Task<bool> PostInitializeAsync(TInitParam param)
        {
            return Task.FromResult(true);
        }
        
        protected abstract Task<TNextParam> InitializeImplAsync(TInitParam param);

        public virtual Task ShutdownAsync()
        {
            return _next.ShutdownAsync();
        }

        async Task ITwitchChatStreamNotifications.OnMessageAsync(IrcMessage message)
        {
            if (!await OnMessageImplAsync(message))
            {
                await _prev.OnMessageAsync(message);
            }
        }

        protected virtual Task<bool> OnMessageImplAsync(IrcMessage message)
        {
            return Task.FromResult(false);
        }

        async Task ITwitchChatStreamNotifications.OnErrorAsync(Exception exception)
        {
            if (!await OnErrorImplAsync(exception))
            {
                await _prev.OnErrorAsync(exception);
            }
        }

        protected virtual Task<bool> OnErrorImplAsync(Exception exception)
        {
            return Task.FromResult(false);
        }

        public virtual void Dispose()
        {
            _next.Dispose();
        }

        public virtual Task WriteMessageAsync(IrcMessage message)
        {
            return _next.WriteMessageAsync(message);
        }

        public Task HandleMessageAsync(IrcMessage message)
        {
            return _next.HandleMessageAsync(message);
        }

        public Task HandleErrorAsync(Exception exception)
        {
            return _next.HandleErrorAsync(exception);
        }

        public bool Healthy => IsHealthy() && _next.Healthy;

        protected virtual bool IsHealthy()
        {
            return true;
        }
    }

    internal abstract class IrcMiddleware<TInitParam> : IrcMiddleware<TInitParam, TInitParam>
    {
        protected IrcMiddleware(IIrcMiddleware<TInitParam> next) : base(next)
        {
        }

        protected override Task<TInitParam> InitializeImplAsync(TInitParam param)
        {
            return Task.FromResult(param);
        }
    }
}
    