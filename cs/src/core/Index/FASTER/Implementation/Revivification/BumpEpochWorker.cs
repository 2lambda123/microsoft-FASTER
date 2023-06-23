﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace FASTER.core
{
    internal class BumpEpochWorker<Key, Value>
    {
        // These are balanced to decrease the wait interval as the count of records grows until we hit 16ms, which is the
        // timer resolution on Windows. If we have more than MaxCountForBump record needing a bump, we'll bump immediately.
        // Otherwise we shift DefaultBumpIntervalMs by BumpMsShift multiples to determine sleep time (see ScanForBump()).
        internal const int DefaultBumpIntervalMs = 1024;
        const int MaxCountForBump = 32;
        const int BumpMsShift = 2;

        // State control variables.
        long state;
        const long ScanOrQuiescent = 0;     // Any threads in the worker are scanning or quiescent, so a thread may claim BumpOrSleep
        const long BumpOrSleep = 1;         // A thread is either bumping or sleeping before bumping, and after bumping will recheck for more work

        readonly FreeRecordPool<Key, Value> recordPool;

        internal BumpEpochWorker(FreeRecordPool<Key, Value> recordPool) => this.recordPool = recordPool;

        internal void Start(bool fromAdd)
        {
            if (!fromAdd || (state == ScanOrQuiescent && Interlocked.CompareExchange(ref state, BumpOrSleep, ScanOrQuiescent) == ScanOrQuiescent))
                Task.Run(() => LaunchWorker(fromAdd));
        }

        // Return whether another thread has been launched while we were scanning.
        internal bool YieldToAnotherThread() => this.state == BumpOrSleep;

        private void LaunchWorker(bool fromAdd)
        {
            ulong startMs;
            while (true)
            {
                // Do the bump if we just added a free record. If this is the first time for this worker we may have only one record
                // (the one that triggered the worker run), or possibly more that happened at about the same time. Otherwise we've
                // looped up from below and already slept if needed. If not fromAdd, then we are here to update HasSafeRecords.
                if (fromAdd)
                    recordPool.fkv.epoch.BumpCurrentEpoch();
                startMs = Native32.GetTickCount64();

                // See if more entries were added following the bump.
                this.state = ScanOrQuiescent;
                if (!ScanForBumpOrEmpty(startMs, out int waitMs) || !fromAdd)
                {
                    // No records needing Bump(), or another thread has taken bumpWorkerState, or we're here from Take and just wanted
                    // to update HasSafeRecords. We're done with this invocation of the worker.
                    return;
                }

                // We need another bump. If another thread has already claimed the BumpOrSleep state, exit.
                if (Interlocked.CompareExchange(ref state, BumpOrSleep, ScanOrQuiescent) != ScanOrQuiescent)
                    return;

                // If we don't have many entries, sleep a bit so we don't thrash epoch increments.
                if (waitMs > 0)
                    Thread.Sleep(waitMs);
            }
        }

        bool ScanForBumpOrEmpty(ulong startMs, out int waitMs)
        {
            waitMs = 0;
            if (!this.recordPool.ScanForBumpOrEmpty(MaxCountForBump, out int count))
                return false;   // Pool is empty, no bump needed, or we are yielding to another thread

            if (count > 0)
            {
                if (count < MaxCountForBump)
                {
                    var elapsedMs = Native32.GetTickCount64() - startMs;

                    // Determine sleep interval based on count...           // These are the current sleep times at count intervals, for illustration
                    if (count > MaxCountForBump / 2)                        // 16-31 records
                        waitMs = DefaultBumpIntervalMs >> BumpMsShift * 3;  // 16 ms
                    else if (count > MaxCountForBump / 4)                   // 8-15 records
                        waitMs = DefaultBumpIntervalMs >> BumpMsShift * 2;  // 64 ms
                    else if (count > MaxCountForBump / 8)                   // 4-7 records
                        waitMs = DefaultBumpIntervalMs >> BumpMsShift;      // 256 ms
                    else                                                    // 1-4 records
                        waitMs = DefaultBumpIntervalMs;                     // 1024 ms

                    // If more time has already elapsed than we just decided to wait, we'll Bump immediately.
                    if (elapsedMs >= (ulong)waitMs)
                        waitMs = 0;
                }
            }

            return this.state == ScanOrQuiescent;
        }

        internal void Dispose()
        {
            // Any in-progress thread will stop when it sees this, thinking another thread is taking over.
            this.state = BumpOrSleep;
        }

        /// <inheritdoc/>
        public override string ToString() => $"state: {(this.state == BumpOrSleep ? "BumpOrSleep" : "ScanOrQuiescent")}";
    }
}
