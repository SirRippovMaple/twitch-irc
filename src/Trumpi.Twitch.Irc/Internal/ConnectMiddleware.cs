using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class ConnectMiddleware : IrcMiddleware<TwitchChatConnectionParameters, Stream>
    {
        private TcpClient _client;
        private TwitchChatConnectionParameters _connectionParameters;
        private readonly ManualResetEvent _registrationEvent = new ManualResetEvent(false);
        private Stream _stream;

        public ConnectMiddleware(IIrcMiddleware<Stream> next) : base(next)
        {
        }

        protected override async Task<Stream> InitializeImplAsync(TwitchChatConnectionParameters param)
        {
            if (_client != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _connectionParameters = param;
            _registrationEvent.Reset();
            _client = new TcpClient();
            await _client.ConnectAsync(_connectionParameters.HostName, _connectionParameters.Port);
            _stream = _client.GetStream();
            if (_connectionParameters.Ssl)
            {
                var sslStream = new SslStream(_stream, false);
                await sslStream.AuthenticateAsClientAsync(_connectionParameters.HostName);
                _stream = sslStream;
            }

            return _stream;
       }

        protected override async Task<bool> PostInitializeAsync(TwitchChatConnectionParameters param)
        {
            await WriteMessageAsync(new IrcMessage("PASS", _connectionParameters.Password));
            await WriteMessageAsync(new IrcMessage("NICK", _connectionParameters.User));

            if (!_registrationEvent.WaitOne(TimeSpan.FromMilliseconds(3000)))
            {
                await ShutdownAsync();
                return false;
            }

            return true;
        }

        public override async Task ShutdownAsync()
        {
            await base.ShutdownAsync();
            _stream?.Close();
            _stream = null;
            _client?.Close();
            _client = null;
        }

        protected override bool IsHealthy()
        {
            return _stream != null &&
                   _client.Connected &&
                   _registrationEvent.WaitOne(TimeSpan.FromMilliseconds(1));
        }

        protected override Task<bool> OnMessageImplAsync(IrcMessage message)
        {
            if (message.Command == "001")
            {
                _registrationEvent.Set();
            }
            return base.OnMessageImplAsync(message);
        }
    }
}