using System;
using System.IO;
using System.Threading.Tasks;
using Trumpi.Twitch.Irc.Internal;

namespace Trumpi.Twitch.Irc
{
    public class TwitchChatConnection : ITwitchChatConnection, IDisposable
    {
        private readonly ITwitchChatStreamNotifications _notificationReceiver;

        private string _connectionName;
        private readonly IIrcMiddleware<TwitchChatConnectionParameters> _impl;

        //public bool Connected => _client?.Connected ?? false;

        public TwitchChatConnection(ITwitchChatStreamNotifications notificationReceiver,
            IFloodPreventer floodPreventer = null,
            string connectionName = null)
        {
            _impl = new ReconnectMiddleware(
                new RequestCapMiddleware<TwitchChatConnectionParameters>(
                    new ConnectMiddleware(
                        new JoinRateLimiter<Stream>(
                            new PingLoopMiddleware(
                                new ReadLoopMiddleware(
                                    new WriteLoopMiddleware(
                                        floodPreventer ?? new TwitchFloodPreventer(),
                                        new TerminalMiddleware<Stream>()
                                    )
                                )
                            )
                        )
                    )
                )
            );

            _notificationReceiver = notificationReceiver;
            _connectionName = connectionName ?? Guid.NewGuid().ToString();
        }

        public async Task<bool> ConnectAsync(TwitchChatConnectionParameters connectionParameters)
        {
            return await _impl.InitializeAsync(_notificationReceiver, connectionParameters);
        }

        public async Task DisconnectAsync()
        {
            await _impl.ShutdownAsync();
        }

        public void Dispose()
        {
            _impl.Dispose();
        }

        public Task WriteMessage(IrcMessage message)
        {
            return _impl.WriteMessageAsync(message);
        }

        public bool Connected => _impl.Healthy;
    }
}