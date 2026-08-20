using UnityEngine;

namespace DiceRevolver.Prototype
{
    public abstract class PassiveEventEffect : ScriptableObject
    {
        public abstract IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context);
    }
}
