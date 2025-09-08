namespace CatboxPorter
{
    public sealed class UploadProgressStream : Stream
    {
        public readonly record struct Progress(long BytesSent, long? TotalBytes, double? Percent);

        private readonly Stream _inner;
        private readonly IProgress<Progress>? _progress;
        private readonly bool _leaveInnerOpen;
        private readonly int _reportEveryBytes;
        private readonly long? _totalBytes;
        private long _sent;
        private long _sinceLastReport;

        public UploadProgressStream(
            Stream inner,
            IProgress<Progress>? progress,
            bool leaveInnerOpen = true,
            int reportEveryBytes = 64 * 1024,
            bool reportInitial = true,
            long? totalBytes = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _progress = progress;
            _leaveInnerOpen = leaveInnerOpen;
            _reportEveryBytes = Math.Max(1, reportEveryBytes);
            _totalBytes = totalBytes ?? (inner.CanSeek ? Math.Max(0, inner.Length - inner.Position) : null);

            if (reportInitial && _progress is not null)
                _progress.Report(new Progress(0, _totalBytes, _totalBytes is > 0 ? 0d : null));
        }

        public long BytesSent => _sent;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            OnRead(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            int read = _inner.Read(buffer);
            OnRead(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            OnRead(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            OnRead(read);
            return read;
        }

        private void OnRead(int read)
        {
            if (read <= 0)
            {
                if (_sinceLastReport > 0 && _progress is not null)
                {
                    _progress.Report(new Progress(_sent, _totalBytes, Percent(_sent, _totalBytes)));
                    _sinceLastReport = 0;
                }
                return;
            }

            _sent += read;
            _sinceLastReport += read;

            if (_sinceLastReport >= _reportEveryBytes && _progress is not null)
            {
                _progress.Report(new Progress(_sent, _totalBytes, Percent(_sent, _totalBytes)));
                _sinceLastReport = 0;
            }
        }

        private static double? Percent(long sent, long? total) =>
            total is > 0 ? (double)sent / total.Value * 100d : null;

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveInnerOpen)
                _inner.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_leaveInnerOpen)
                await _inner.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}