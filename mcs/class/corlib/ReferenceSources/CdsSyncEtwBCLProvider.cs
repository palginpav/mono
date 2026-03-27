// CdsSyncEtwBCLProvider — ETW events for CDS sync primitives
// (SpinLock, SpinWait, Barrier). Shared between mscorlib and System.dll.
//
// The referencesource version is gated behind #if !FEATURE_PAL which
// excludes it from the wine-mono corlib build. This file provides the
// same class without the FEATURE_PAL guard so that Windows System.dll
// Barrier can resolve CdsSyncEtwBCLProvider.Log from mscorlib.

using System;
using System.Diagnostics.Tracing;
using System.Security;

namespace System.Threading
{
    [System.Runtime.CompilerServices.FriendAccessAllowed]
    [EventSource(
        Name = "System.Threading.SynchronizationEventSource",
        Guid = "EC631D38-466B-4290-9306-834971BA0217")]
    internal sealed class CdsSyncEtwBCLProvider : EventSource
    {
        public static CdsSyncEtwBCLProvider Log = new CdsSyncEtwBCLProvider();
        private CdsSyncEtwBCLProvider() { }

        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        private const int SPINLOCK_FASTPATHFAILED_ID = 1;
        private const int SPINWAIT_NEXTSPINWILLYIELD_ID = 2;
        private const int BARRIER_PHASEFINISHED_ID = 3;

        [Event(SPINLOCK_FASTPATHFAILED_ID, Level = EventLevel.Warning)]
        public void SpinLock_FastPathFailed(int ownerID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                WriteEvent(SPINLOCK_FASTPATHFAILED_ID, ownerID);
            }
        }

        [Event(SPINWAIT_NEXTSPINWILLYIELD_ID, Level = EventLevel.Informational)]
        public void SpinWait_NextSpinWillYield()
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                WriteEvent(SPINWAIT_NEXTSPINWILLYIELD_ID);
            }
        }

        [SecuritySafeCritical]
        [Event(BARRIER_PHASEFINISHED_ID, Level = EventLevel.Verbose, Version = 1)]
        public void Barrier_PhaseFinished(bool currentSense, long phaseNum)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[2];

                    Int32 senseAsInt32 = currentSense ? 1 : 0;
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&senseAsInt32));
                    eventPayload[1].Size = sizeof(long);
                    eventPayload[1].DataPointer = ((IntPtr)(&phaseNum));

                    WriteEventCore(BARRIER_PHASEFINISHED_ID, 2, eventPayload);
                }
            }
        }
    }
}
