using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal abstract class LoopMiddleware<TInitParam> : IrcMiddleware<TInitParam, TInitParam>
    {
        private Task<Task> _loopTask;
        private CancellationTokenSource _tokenSource;

        protected LoopMiddleware(IIrcMiddleware<TInitParam> next) : base(next)
        {
        }

        protected override Task<TInitParam> InitializeImplAsync(TInitParam param)
        {
            _tokenSource = new CancellationTokenSource();
            _loopTask = Task.Factory.StartNew(() => LoopAsync(_tokenSource.Token), _tokenSource.Token);
            return Task.FromResult(param);
        }

        public override async Task ShutdownAsync()
        {
            Cancel();
            if (_loopTask != null)
            {
                await _loopTask;
            }

            _loopTask = null;
            _tokenSource = null;
            await base.ShutdownAsync();
        }

        protected void Cancel()
        {
            _tokenSource?.Cancel();
        }
        
        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!await DoLoopAsync(token))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Ignore
                    break;
                }
                catch (Exception e)
                {
                    await HandleErrorAsync(e);
                }
            }
        }

        protected abstract Task<bool> DoLoopAsync(CancellationToken cancellationToken);

        public override void Dispose()
        {
            _tokenSource?.Cancel();
            _loopTask?.GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}