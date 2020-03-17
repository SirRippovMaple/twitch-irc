using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc
{
    public interface ITwitchChatConnection
    {
        Task<bool> ConnectAsync(TwitchChatConnectionParameters connectionParameters);
        Task DisconnectAsync();
        Task WriteMessage(IrcMessage message);
        bool Connected { get; }
    }
}