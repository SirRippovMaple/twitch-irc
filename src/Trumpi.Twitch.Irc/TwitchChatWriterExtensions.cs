using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc
{
    public static class TwitchChatWriterExtensions
    {
        internal static Task SendPass(this TwitchChatConnection writer, string password)
        {
            return writer.WriteMessage(new IrcMessage("PASS", password));
        }

        internal static Task SendNick(this TwitchChatConnection writer, string nick)
        {
            return writer.WriteMessage(new IrcMessage("NICK", nick));
        }

        public static Task SendPing(this TwitchChatConnection writer, string pingNonce)
        {
            return writer.WriteMessage(new IrcMessage("PING", pingNonce));
        }

        public static Task Join(this TwitchChatConnection writer, string channelName)
        {
            return writer.WriteMessage(new IrcMessage("JOIN", $"#{channelName}"));
        }

        internal static Task Part(this TwitchChatConnection writer, string channelName)
        {
            return writer.WriteMessage(new IrcMessage("PART", $"#{channelName}"));
        }
        
        internal static Task SendPong(this TwitchChatConnection writer,string[] parameters)
        {
            return writer.WriteMessage(new IrcMessage("PONG", parameters));
        }

        internal static Task SendQuit(this TwitchChatConnection writer)
        {
            return writer.WriteMessage(new IrcMessage("QUIT"));
        }
    }
}