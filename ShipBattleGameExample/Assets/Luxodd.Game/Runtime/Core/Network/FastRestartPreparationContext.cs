using System;
using System.Threading;

namespace Luxodd.Game.Scripts.Network
{
    /// <summary>Game-owned preparation of Session A before the host starts Fast Restart.</summary>
    public sealed class FastRestartPreparationContext
    {
        private Action<FastRestartPreparationContext, bool> _onCompleted;

        internal string RequestId { get; }

        internal FastRestartPreparationContext(string requestId,
            Action<FastRestartPreparationContext, bool> onCompleted)
        {
            RequestId = requestId;
            _onCompleted = onCompleted;
        }

        /// <summary>Confirm preparation succeeded. Call on Unity's main thread after the command response.</summary>
        public void Complete()
        {
            Interlocked.Exchange(ref _onCompleted, null)?.Invoke(this, true);
        }

        /// <summary>Stop preparation without acknowledging Restart. Call on Unity's main thread.</summary>
        public void Fail()
        {
            Interlocked.Exchange(ref _onCompleted, null)?.Invoke(this, false);
        }

        internal void Invalidate()
        {
            Interlocked.Exchange(ref _onCompleted, null);
        }
    }
}
