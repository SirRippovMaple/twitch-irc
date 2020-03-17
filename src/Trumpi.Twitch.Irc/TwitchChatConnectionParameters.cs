namespace Trumpi.Twitch.Irc
{
    public class TwitchChatConnectionParameters
    {
        public TwitchChatConnectionParameters(string hostName, int port, bool ssl, string user, string password)
        {
            HostName = hostName;
            Port = port;
            Ssl = ssl;
            User = user;
            Password = password;
        }

        public string HostName { get; }
        public int Port { get; }
        public bool Ssl { get; }
        public string User { get; }
        public string Password { get; }
    }
}