using System;
using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    public sealed class BulletEventTimeScheduler
    {
        private sealed class ScheduledEvent
        {
            public float DueTime;
            public long Sequence;
            public Action Callback;
        }

        private readonly List<ScheduledEvent> pendingEvents = new List<ScheduledEvent>();
        private long nextSequence;

        public int PendingCount => pendingEvents.Count;

        public bool Schedule(float now, float delaySeconds, Action callback)
        {
            if (callback == null)
            {
                return false;
            }

            pendingEvents.Add(new ScheduledEvent
            {
                DueTime = now + Math.Max(0f, delaySeconds),
                Sequence = nextSequence++,
                Callback = callback,
            });
            pendingEvents.Sort(CompareScheduledEvents);
            return true;
        }

        public void Tick(float now, Action<Exception> exceptionHandler = null)
        {
            int dueCount = 0;
            while (dueCount < pendingEvents.Count && pendingEvents[dueCount].DueTime <= now)
            {
                dueCount++;
            }

            if (dueCount == 0)
            {
                return;
            }

            List<ScheduledEvent> dueEvents = pendingEvents.GetRange(0, dueCount);
            pendingEvents.RemoveRange(0, dueCount);

            for (int i = 0; i < dueEvents.Count; i++)
            {
                try
                {
                    dueEvents[i].Callback.Invoke();
                }
                catch (Exception exception)
                {
                    exceptionHandler?.Invoke(exception);
                }
            }
        }

        public void Clear()
        {
            pendingEvents.Clear();
        }

        private static int CompareScheduledEvents(ScheduledEvent left, ScheduledEvent right)
        {
            int dueTimeComparison = left.DueTime.CompareTo(right.DueTime);
            return dueTimeComparison != 0
                ? dueTimeComparison
                : left.Sequence.CompareTo(right.Sequence);
        }
    }
}
