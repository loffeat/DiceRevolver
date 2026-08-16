using UnityEngine;

namespace DiceRevolver.Prototype
{
    public abstract class BulletEventEffect : ScriptableObject
    {
        public abstract void Trigger(BulletEventContext context);
    }
}
