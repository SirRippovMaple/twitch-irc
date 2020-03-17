using System.Threading.Tasks;

namespace Trumpi.Twitch.Irc.Internal
{
    internal class RequestCapMiddleware<T> : IrcMiddleware<T>
    {
        public RequestCapMiddleware(IIrcMiddleware<T> next) : base(next)
        {
        }

        protected override async Task<bool> PostInitializeAsync(T param)
        {
            await WriteMessageAsync(new IrcMessage("CAP", "REQ", "twitch.tv/commands twitch.tv/tags"));
            return true;
        }
    }
}