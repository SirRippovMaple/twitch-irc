using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.TestDoubles
{
    public class FakeNotifier : ITwitchChatStreamNotifications
    {
        private readonly Queue<IrcMessage> _messageQueue = new Queue<IrcMessage>();
        private readonly AutoResetEvent _event = new AutoResetEvent(false);

        public List<Exception> ExceptionList { get; } = new List<Exception>();

        public IList<IrcMessage> WaitForMessages(int numberOfMessages)
        {
            while (_messageQueue.Count < numberOfMessages)
            {
                _event.WaitOne();
            }

            List<IrcMessage> returnValue = new List<IrcMessage>();
            for (int i = 0; i < numberOfMessages; i++)
            {
                returnValue.Add(_messageQueue.Dequeue());
            }

            return returnValue;
        }

        public Task OnDisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task OnMessageAsync(IrcMessage message)
        {
            _messageQueue.Enqueue(message);
            _event.Set();
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception exception)
        {
            ExceptionList.Add(exception);
            return Task.CompletedTask;
        }
    }
}