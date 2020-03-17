namespace Trumpi.Twitch.Irc
{
    public interface IFloodPreventer
    {
        /// <summary>
        /// Gets the time delay before which the client may currently send the next message.
        /// </summary>
        /// <param name="peek"></param>
        /// <returns>The time delay before the next message may be sent, in milliseconds.</returns>
        int GetSendDelay(IrcMessage peek);

        /// <summary>
        /// Notifies the flood preventer that a message has just been send by the client.
        /// </summary>
        /// <param name="message"></param>
        void HandleMessageSent(IrcMessage message);
    }
}