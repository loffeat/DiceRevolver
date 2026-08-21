using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceEventBudget
    {
        private bool warningIssued;

        public DiceEventBudget(int amount)
        {
            Remaining = Mathf.Max(1, amount);
        }

        public int Remaining { get; private set; }

        public bool TryConsume(int amount, Action exhaustedWarning = null)
        {
            int required = Mathf.Max(1, amount);
            if (Remaining >= required)
            {
                Remaining -= required;
                return true;
            }

            if (!warningIssued)
            {
                warningIssued = true;
                exhaustedWarning?.Invoke();
            }

            return false;
        }

        public bool TryConsume(Action exhaustedWarning = null) =>
            TryConsume(1, exhaustedWarning);
    }
}
