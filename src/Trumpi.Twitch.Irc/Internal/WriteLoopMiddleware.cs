using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class WriteLoopMiddleware : LoopMiddleware<Stream>
    {
        private readonly IFloodPreventer _floodPreventer;
        private const int MinimumSendWaitTime = 50;
        private const int MaxParamsCount = 15;
        private readonly Queue<IrcMessage> _messageSendQueue = new Queue<IrcMessage>();
        private Stream _dataStream;

        public WriteLoopMiddleware(IFloodPreventer floodPreventer, IIrcMiddleware<Stream> next) : base(next)
        {
            _floodPreventer = floodPreventer;
        }

        protected override Task<Stream> InitializeImplAsync(Stream param)
        {
            _dataStream = param;
            return base.InitializeImplAsync(param);
        }

        protected override async Task<bool> DoLoopAsync(CancellationToken cancellationToken)
        {
            // Send pending messages in queue until flood preventer indicates to stop.
            int sendDelay = 0;

            while (!cancellationToken.IsCancellationRequested && _messageSendQueue.Count > 0)
            {
                Debug.Assert(_messageSendQueue.Count < 100);
                // Check that flood preventer currently permits sending of messages.
                if (_floodPreventer != null)
                {
                    sendDelay = _floodPreventer.GetSendDelay(_messageSendQueue.Peek());
                    if (sendDelay > 0)
                        break;
                }

                // Send next message in queue.
                var message = _messageSendQueue.Dequeue();
                var line = GetWriteLine(message);
                var lineBuffer = Encoding.UTF8.GetBytes(line);
                await _dataStream.WriteAsync(lineBuffer, 0, lineBuffer.Length, cancellationToken);
                await _dataStream.FlushAsync(cancellationToken);

                // Tell flood preventer mechanism that message has just been sent.
                _floodPreventer?.HandleMessageSent(message);
            }

            // Make timer fire when next message in send queue should be written.
            await Task.Delay(Math.Max(sendDelay, MinimumSendWaitTime), cancellationToken);
            return true;
        }

        public override Task WriteMessageAsync(IrcMessage message)
        {
            _messageSendQueue.Enqueue(message);
            return Task.CompletedTask;
        }
            
        private string GetWriteLine(IrcMessage message)
        {
            if (message.Command == null)
                throw new ArgumentException();
            if (message.Parameters.Count > MaxParamsCount)
                throw new ArgumentException();

            var lineBuilder = new StringBuilder();

            // Append prefix to line, if specified.
            if (message.Prefix != null)
            {
                lineBuilder.Append($":{CheckPrefix(message.Prefix)} ");
            }

            // Append command name to line.
            lineBuilder.Append(CheckCommand(message.Command).ToUpper());

            // Append each parameter to line, adding ':' character before last parameter.
            for (int i = 0; i < message.Parameters.Count - 1; i++)
            {
                if (message.Parameters[i] != null)
                    lineBuilder.Append(" " + CheckMiddleParameter(message.Parameters[i]));
            }
            if (message.Parameters.Count > 0)
            {
                var lastParameter = message.Parameters[message.Parameters.Count - 1];
                if (lastParameter != null)
                    lineBuilder.Append(" :" + CheckTrailingParameter(lastParameter));
            }

            lineBuilder.Append("\r\n");

            // Send raw message as line of text.
            var line = lineBuilder.ToString();
            return line;
        }

        private string CheckPrefix(string value)
        {
            Debug.Assert(value != null);

            if (value.Length == 0 || value.Any(IsInvalidMessageChar))
            {
                throw new ArgumentException();
            }

            return value;
        }

        private string CheckCommand(string value)
        {
            Debug.Assert(value != null);

            if (value.Length == 0 || value.Any(IsInvalidMessageChar))
            {
                throw new ArgumentException();
            }

            return value;
        }

        private string CheckMiddleParameter(string value)
        {
            Debug.Assert(value != null);

            if (value.Length == 0 || value.Any(c => IsInvalidMessageChar(c) || c == ' ') || value[0] == ':')
            {
                throw new ArgumentException();
            }

            return value;
        }

        private string CheckTrailingParameter(string value)
        {
            Debug.Assert(value != null);

            if (value.Any(IsInvalidMessageChar))
            {
                throw new ArgumentException();
            }

            return value;
        }

        private bool IsInvalidMessageChar(char value)
        {
            return value == '\0' || value == '\r' || value == '\n';
        }
    }
}