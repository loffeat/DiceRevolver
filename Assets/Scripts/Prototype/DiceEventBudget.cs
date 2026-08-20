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

        public bool TryConsume(Action exhaustedWarning = null)
        {
            if (Remaining > 0)
            {
                Remaining--;
                return true;
            }

            if (!warningIssued)
            {
                warningIssued = true;
                exhaustedWarning?.Invoke();
            }

            return false;
        }
    }
}
