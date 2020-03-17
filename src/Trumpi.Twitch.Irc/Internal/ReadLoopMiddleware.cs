using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class ReadLoopMiddleware : LoopMiddleware<Stream>
    {
        private const int ReceiveBufferSize = 0xFFFF;
        private CircularBufferStream _receiveStream;
        private StreamReader _dataStreamReader;
        private SafeLineReader _dataStreamLineReader;
        private Stream _dataStream;

        public ReadLoopMiddleware(IIrcMiddleware<Stream> next) : base(next)
        {
        }

        protected override Task<Stream> InitializeImplAsync(Stream param)
        {
            _dataStream = param;
            _receiveStream = new CircularBufferStream(ReceiveBufferSize);
            _dataStreamReader = new StreamReader(_receiveStream, Encoding.UTF8);
            _dataStreamLineReader = new SafeLineReader(_dataStreamReader);
            return base.InitializeImplAsync(param);
        }

        public override async Task ShutdownAsync()
        {
            await base.ShutdownAsync();
            _dataStreamLineReader = null;
            _dataStreamReader?.Dispose();
            _dataStreamReader = null;
            _receiveStream = null;
        }

        protected override async Task<bool> DoLoopAsync(CancellationToken cancellationToken)
        {
            var bytesRead = await _dataStream.ReadAsync(_receiveStream.Buffer,
                (int) _receiveStream.WritePosition,
                _receiveStream.Buffer.Length - (int) _receiveStream.WritePosition, cancellationToken);

            if (bytesRead == 0)
            {
                return true;
            }

            _receiveStream.WritePosition += bytesRead;
            _dataStreamReader.DiscardBufferedData();

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = _dataStreamLineReader.ReadLine();
                if (line == null)
                {
                    break;
                }

                if (line.Length == 0)
                {
                    Cancel();
                    throw new IOException("End of stream reached.");
                }

                await ParseMessageAsync(line);
            }

            return true;
        }

        private async Task ParseMessageAsync(string line)
        {
            try
            {
                var message = IrcMessage.ParseLine(line);
                await HandleMessageAsync(message);
            }
            catch (Exception e)
            {
                Exception ex = new Exception("Unable to parse this message: " + line, e);
                await HandleErrorAsync(ex);
            }
        }
    }
}
