using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class BulletEventTimeSchedulerTests
    {
        [Test]
        public void ScheduledCallbackRunsOnceAtDueTime()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            int executionCount = 0;

            Assert.That(scheduler.Schedule(10f, 0.25f, () => executionCount++), Is.True);

            scheduler.Tick(10.24f);
            Assert.That(executionCount, Is.Zero);
            Assert.That(scheduler.PendingCount, Is.EqualTo(1));

            scheduler.Tick(10.25f);
            scheduler.Tick(20f);

            Assert.That(executionCount, Is.EqualTo(1));
            Assert.That(scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void EqualDueTimesRunInScheduleOrder()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            List<int> executionOrder = new List<int>();

            scheduler.Schedule(2f, 1f, () => executionOrder.Add(1));
            scheduler.Schedule(2.5f, 0.5f, () => executionOrder.Add(2));
            scheduler.Schedule(1f, 2f, () => executionOrder.Add(3));

            scheduler.Tick(3f);

            Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void CallbackExceptionDoesNotBlockLaterCallbacks()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            Exception capturedException = null;
            int successfulCallbacks = 0;

            scheduler.Schedule(0f, 1f, () => throw new InvalidOperationException("expected"));
            scheduler.Schedule(0f, 1f, () => successfulCallbacks++);

            scheduler.Tick(1f, exception => capturedException = exception);

            Assert.That(capturedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(successfulCallbacks, Is.EqualTo(1));
            Assert.That(scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void CallbackScheduledDuringTickWaitsForNextTick()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            int nestedExecutions = 0;

            scheduler.Schedule(0f, 0f, () =>
            {
                scheduler.Schedule(0f, 0f, () => nestedExecutions++);
            });

            scheduler.Tick(0f);
            Assert.That(nestedExecutions, Is.Zero);
            Assert.That(scheduler.PendingCount, Is.EqualTo(1));

            scheduler.Tick(0f);
            Assert.That(nestedExecutions, Is.EqualTo(1));
            Assert.That(scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void NegativeDelayIsImmediateAndNullCallbackIsRejected()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            int executionCount = 0;

            Assert.That(scheduler.Schedule(4f, -2f, () => executionCount++), Is.True);
            Assert.That(scheduler.Schedule(4f, 1f, null), Is.False);

            scheduler.Tick(4f);

            Assert.That(executionCount, Is.EqualTo(1));
            Assert.That(scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void ClearRemovesPendingCallbacks()
        {
            BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
            int executionCount = 0;
            scheduler.Schedule(0f, 1f, () => executionCount++);

            scheduler.Clear();
            scheduler.Tick(2f);

            Assert.That(executionCount, Is.Zero);
            Assert.That(scheduler.PendingCount, Is.Zero);
        }
    }
}
