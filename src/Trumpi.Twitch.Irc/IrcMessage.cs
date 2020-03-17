using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trumpi.Twitch.Irc.Internal;

namespace Trumpi.Twitch.Irc
{
    /// <summary>
    /// Represents a raw IRC message that is sent/received.
    /// A message contains a prefix (representing the source), a command name (a word or three-digit number),
    /// and any number of parameters (up to a maximum of 15).
    /// </summary>
    [DebuggerDisplay("{ToString(), nq}")]
    public struct IrcMessage
    {
        private const int MaxParamsCount = 15;
        private readonly string _raw;
        public IDictionary<string, string> Tags;

        /// <summary>
        /// The message prefix.
        /// </summary>
        public string Prefix;

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Command;

        /// <summary>
        /// A list of the parameters to the message.
        /// </summary>
        public IList<string> Parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcMessage"/> structure.
        /// </summary>
        /// <param name="prefix">The message prefix that represents the source of the message.</param>
        /// <param name="command">The command name; either an alphabetic word or 3-digit number.</param>
        /// <param name="parameters">A list of the parameters to the message. Can contain a maximum of 15 items.
        /// </param>
        /// <param name="tags"></param>
        public IrcMessage(string raw, IDictionary<string, string> tags, string prefix, string command, IList<string> parameters)
        {
            _raw = raw;
            this.Tags = tags;
            this.Prefix = prefix;
            this.Command = command;
            this.Parameters = parameters;
        }

        public IrcMessage(string command, params string[] parameters)
        {
            this._raw = null;
            this.Tags = new Dictionary<string, string>();
            this.Prefix = null;
            this.Command = command;
            this.Parameters = parameters.ToList();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>A string that represents this instance.</returns>
        public override string ToString()
        {
            if (_raw != null)
            {
                return _raw;
            }
            return string.Format("{0} {1}", this.Command, String.Join(" ", this.Parameters));
        }

        public static IrcMessage ParseLine(string line)
        {
            var lineSpan = line.AsSpan();
            IDictionary<string, string> tags = new Dictionary<string, string>();
            ReadOnlySpan<char> prefix = Span<char>.Empty;
            ReadOnlySpan<char> lineAfterPrefix;

            // Extract prefix from message lineSpan, if it contains one.
            if (lineSpan[0] == ':')
            {
                var firstSpaceIndex = lineSpan.IndexOf(' ');
                prefix = lineSpan.Slice(1, firstSpaceIndex - 1);
                lineAfterPrefix = lineSpan.Slice(firstSpaceIndex + 1);
            }
            else
            {
                var firstSpaceIndex = lineSpan.IndexOf(' ');
                if (lineSpan[0] == '@' && firstSpaceIndex > 0)
                {
                    tags = ParseTags(lineSpan.Slice(1, firstSpaceIndex - 1).Trim());
                    var firstSpaceIndex2 = lineSpan.IndexOf(' ', firstSpaceIndex + 1);
                    prefix = lineSpan.Slice(firstSpaceIndex + 2, firstSpaceIndex2 - firstSpaceIndex - 2);
                    lineAfterPrefix = lineSpan.Slice(firstSpaceIndex2 + 1);
                }
                else
                {
                    lineAfterPrefix = lineSpan;
                }
            }

            // Extract command from message.
            var spaceIndex = lineAfterPrefix.IndexOf(' ');
            var command = spaceIndex == -1 ? lineAfterPrefix : lineAfterPrefix.Slice(0, spaceIndex);
            var parameters = new string[MaxParamsCount];
            if (spaceIndex != -1)
            {
                var paramsLine = lineAfterPrefix.Slice(command.Length + 1);

                // Extract parameters from message.
                // Each parameter is separated by single space, except last one, which may contain spaces if it
                // is prefixed by colon.
                int paramEndIndex = -1;
                int lineColonIndex = paramsLine.IndexOf(" :", StringComparison.Ordinal);
                if (lineColonIndex == -1 && !paramsLine.StartsWith(":"))
                    lineColonIndex = paramsLine.Length;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramStartIndex = paramEndIndex + 1;
                    paramEndIndex = paramsLine.IndexOf(' ', paramStartIndex);
                    if (paramEndIndex == -1)
                        paramEndIndex = paramsLine.Length;
                    if (paramEndIndex > lineColonIndex)
                    {
                        paramStartIndex++;
                        paramEndIndex = paramsLine.Length;
                    }

                    parameters[i] = paramsLine.Slice(paramStartIndex, paramEndIndex - paramStartIndex).ToString();
                    if (paramEndIndex == paramsLine.Length)
                        break;
                }
            }
            
            var message = new IrcMessage(
                line,
                tags,
                prefix.ToString(),
                command.ToString(),
                parameters.Where(x => x != null).ToArray()
            );
            return message;
        }

        private static IDictionary<string, string> ParseTags(ReadOnlySpan<char> preambleSpan)
        {
            Dictionary<string ,string> preamble = new Dictionary<string, string>();
            int start = 0;
            while (start < preambleSpan.Length)
            {
                int mid = preambleSpan.IndexOf('=', start);
                int end = preambleSpan.IndexOf(';', start);
                if (end == -1) end = preambleSpan.Length;
                if (mid != -1)
                {
                    preamble.Add(
                        preambleSpan.Slice(start, mid - start).ToString(),
                        preambleSpan.Slice(mid + 1, end - mid - 1).ToString()
                    );
                }

                start = end + 1;
            }

            return preamble;
        }
    }
}
