using System;
using System.IO;

namespace Trumpi.Twitch.Irc.Internal
{
    // Allows reading and writing to circular buffer as stream.
    // Note: Stream is non-blocking and non-thread-safe.
    internal class CircularBufferStream : Stream
    {
        // Buffer for storing data.

        // Current index within buffer for writing and reading.
        private long _writePosition;
        private long _readPosition;

        public CircularBufferStream(int length)
            : this(new byte[length])
        {
        }

        public CircularBufferStream(byte[] buffer)
        {
            Buffer = buffer;
            _writePosition = 0;
            _readPosition = 0;
        }

        public byte[] Buffer { get; }

        public long WritePosition
        {
            get => _writePosition;
            set => _writePosition = value % Buffer.Length;
        }

        public override long Position
        {
            get => _readPosition;
            set => _readPosition = value % Buffer.Length;
        }

        public override long Length
        {
            get
            {
                var length = _writePosition - _readPosition;
                return length < 0 ? Buffer.Length + length : length;
            }
        }

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override bool CanRead => true;

        public override void Flush()
        {
            //
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _readPosition = offset % Buffer.Length;
                    break;
                case SeekOrigin.End:
                    _readPosition = (Buffer.Length - offset) % Buffer.Length;
                    break;
                case SeekOrigin.Current:
                    _readPosition = (_readPosition + offset) % Buffer.Length;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return _readPosition;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Write block of bytes from given buffer into circular buffer, wrapping around when necessary.
            int writeCount;
            while ((writeCount = Math.Min(count, (int)(Buffer.Length - _writePosition))) > 0)
            {
                var oldWritePosition = _writePosition;
                var newWritePosition = (_writePosition + writeCount) % Buffer.Length;
                if (newWritePosition > _readPosition && oldWritePosition < _readPosition)
                {
#if !SILVERLIGHT
                    throw new InternalBufferOverflowException("The CircularBuffer was overflowed!");
#else
                    throw new IOException("The CircularBuffer was overflowed!");
#endif
                }
                System.Buffer.BlockCopy(buffer, offset, Buffer, (int)_writePosition, writeCount);
                _writePosition = newWritePosition;

                offset += writeCount;
                count -= writeCount; //writeCount <= count => now is count >=0
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Read block of bytes from circular buffer, wrapping around when necessary.
            int totalReadCount = 0;
            int readCount;
            count = Math.Min(buffer.Length - offset, count);
            while ((readCount = Math.Min(count, (int)(Length))) > 0)
            {
                if (readCount > Buffer.Length - _readPosition)
                {
                    readCount = (int)(Buffer.Length - _readPosition);
                }
                System.Buffer.BlockCopy(Buffer, (int)_readPosition, buffer, offset, readCount);
                _readPosition = (_readPosition + readCount) % Buffer.Length;
                offset += readCount;
                count = Math.Min(buffer.Length - offset, count);
                totalReadCount += readCount;
            }
            return totalReadCount;
        }
    }
}
