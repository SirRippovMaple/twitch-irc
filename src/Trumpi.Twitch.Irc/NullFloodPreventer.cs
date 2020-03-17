namespace Trumpi.Twitch.Irc
{
    public class NullFloodPreventer : IFloodPreventer
    {
        public int GetSendDelay(IrcMessage peek)
        {
            return 0;
        }

        public void HandleMessageSent(IrcMessage message)
        {
        }
    }
}