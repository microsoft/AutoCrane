using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Tests
{
    internal class FakePipeWriter : PipeWriter
    {
        public override void Advance(int bytes)
        {
        }

        public override void CancelPendingFlush()
        {
        }

        public override void Complete(Exception? exception = null)
        {
            return;
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<FlushResult>(new FlushResult(false, true));
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return new Memory<byte>(new byte[sizeHint]);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return new Span<byte>(new byte[sizeHint]);
        }
    }
}
