using System;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal interface IIrcMiddleware<in TInitParam> : IDisposable
    {
        public Task<bool> InitializeAsync(ITwitchChatStreamNotifications prev, TInitParam param);
        Task ShutdownAsync();
        Task WriteMessageAsync(IrcMessage message);
        Task HandleMessageAsync(IrcMessage message);
        Task HandleErrorAsync(Exception exception);
        bool Healthy { get; }
    }
}