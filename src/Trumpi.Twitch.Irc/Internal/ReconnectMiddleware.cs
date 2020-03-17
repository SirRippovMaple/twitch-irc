using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class ReconnectMiddleware : IrcMiddleware<TwitchChatConnectionParameters>
    {
        private TwitchChatConnectionParameters _parameters;
        private readonly AutoResetEvent _reconnectEvent = new AutoResetEvent(true);
        private CancellationTokenSource _cancellationTokenSource;
        private readonly HashSet<string> _channels = new HashSet<string>();

        public ReconnectMiddleware(IIrcMiddleware<TwitchChatConnectionParameters> next) : base(next)
        {
        }

        protected override Task<TwitchChatConnectionParameters> InitializeImplAsync(
            TwitchChatConnectionParameters param)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _parameters = param;
            return Task.FromResult(param);
        }

        public override Task ShutdownAsync()
        {
            // Reset the event if we aren't already in a reconnect.
            _cancellationTokenSource.Cancel();
            _reconnectEvent.WaitOne(TimeSpan.FromSeconds(0));
            return base.ShutdownAsync();
        }

        protected override async Task<bool> OnErrorImplAsync(Exception exception)
        {
            switch (exception)
            {
                case IOException ioe:
                    if (ioe.InnerException is SocketException socketException)
                    {
                        if (!_cancellationTokenSource.IsCancellationRequested && HandleSocketErrorAsync(socketException))
                        {
                            return true;
                        }

                        return await base.OnErrorImplAsync(socketException);
                    }

                    return await base.OnErrorImplAsync(exception);

                default:
                    return await base.OnErrorImplAsync(exception);
            }
        }

        public override Task WriteMessageAsync(IrcMessage message)
        {
            if (message.Command == "JOIN" && message.Parameters.Count > 0)
            {
                _channels.Add(message.Parameters[0]);
            }

            if (message.Command == "PART" && message.Parameters.Count > 0)
            {
                _channels.Remove(message.Parameters[0]);
            }
            
            return base.WriteMessageAsync(message);
        }

        private bool HandleSocketErrorAsync(SocketException exception)
        {
            switch (exception.SocketErrorCode)
            {
                case SocketError.NotConnected:
                case SocketError.ConnectionReset:
                case SocketError.ConnectionAborted:
                case SocketError.InvalidArgument:
                    // Reconnect
                    return Reconnect();
            }

            return false;
        }
        
        private bool Reconnect()
        {
            if (_reconnectEvent.WaitOne(0))
            {
                Task.Factory.StartNew(ReconnectLoopAsync);
                return true;
            }

            return true;
        }

        private async Task ReconnectLoopAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await base.ShutdownAsync();
                if (await ReInitializeAsync(_parameters))
                {
                    _reconnectEvent.Set();

                    var channelsCopy = _channels.ToArray();
                    foreach (var channel in channelsCopy)
                    {
                        await base.WriteMessageAsync(new IrcMessage("JOIN", channel));
                    }

                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), _cancellationTokenSource.Token);
            }
        }
    }
}