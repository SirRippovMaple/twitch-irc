using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc
{
    public interface ITwitchChatStreamNotifications
    {
        Task OnMessageAsync(IrcMessage message);
        Task OnErrorAsync(Exception exception);
    }
}