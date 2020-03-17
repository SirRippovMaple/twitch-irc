using System;

namespace Trumpi.Twitch.Irc.Internal
{
    internal static class SpanExtensions
    {
        public static int IndexOf(this ReadOnlySpan<char> source, char c, int startIndex)
        {
            var index = source.Slice(startIndex).IndexOf(c);
            if (index == -1) return -1;
            return index + startIndex;
        }
    }
}