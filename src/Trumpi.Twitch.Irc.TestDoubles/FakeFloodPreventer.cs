namespace Trumpi.Twitch.Irc.TestDoubles
{
    /// <summary>
    /// This is a flood preventer that does not throttle.
    /// </summary>
    public class FakeFloodPreventer : IFloodPreventer
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