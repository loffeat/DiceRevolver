using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/弹丸/攻击特效")]
    public sealed class AttackEffectConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("期望触发攻击特效")]
        private bool expectedCanTriggerHitEffects = true;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            DiceRevolverShotContext shot = context.Signal.Shot;
            if (shot == null)
            {
                return new EventConditionResult(false, "缺少射击上下文。", "缺少射击上下文。");
            }

            bool passed = shot.CanTriggerHitEffects == expectedCanTriggerHitEffects;
            return passed
                ? new EventConditionResult(true, "攻击特效判定匹配。")
                : new EventConditionResult(false, "攻击特效判定不匹配。", "攻击特效判定不匹配。");
        }
    }
}
