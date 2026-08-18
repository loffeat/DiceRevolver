using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, Vector3 hitPosition, GameObject source)
        {
            Amount = amount;
            HitPosition = hitPosition;
            Source = source;
        }

        public float Amount { get; }
        public Vector3 HitPosition { get; }
        public GameObject Source { get; }
    }
}
