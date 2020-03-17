using System.IO;
using System.Text;

namespace Trumpi.Twitch.Irc.Internal
{
    // Reads lines from text sources safely; non-terminated lines are not returned.
    internal class SafeLineReader
    {
        // Reads characters from text source.
        private readonly TextReader _textReader;

        // Current incomplete line;
        private string _currentLine;

        public SafeLineReader(TextReader textReader)
        {
            this._textReader = textReader;
            this._currentLine = string.Empty;
        }

        public TextReader TextReader => _textReader;

        // Reads line from source, ensuring that line is not returned unless it terminates with line break.
        public string ReadLine()
        {
            var lineBuilder = new StringBuilder();

            while (true)
            {
                // Check whether to stop reading characters.
                var nextChar = this._textReader.Peek();
                if (nextChar == -1)
                {
                    this._currentLine = lineBuilder.ToString();
                    break;
                }
                else if (nextChar == '\r' || nextChar == '\n')
                {
                    this._textReader.Read();
                    if (this._textReader.Peek() == '\n')
                        this._textReader.Read();

                    var line = this._currentLine + lineBuilder;
                    this._currentLine = string.Empty;
                    return line;
                }

                // Append next character to line.
                lineBuilder.Append((char)this._textReader.Read());
            }

            return null;
        }
    }
}
