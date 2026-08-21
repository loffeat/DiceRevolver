using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/弹丸/与装备骰面主弹同类型")]
    public sealed class SameProjectileTypeConditionModule : EventConditionModule
    {
        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            ProjectileTypeDefinition equippedType = context.Signal.EquippedBaseProjectileType;
            if (equippedType == null)
            {
                return Failed("装备骰面没有可解析的基础弹丸类型。");
            }

            return context.Signal.CurrentStats.ProjectileTypeDefinition == equippedType
                ? Passed("弹丸类型与装备骰面主弹相同。")
                : Failed("弹丸类型与装备骰面主弹不同。");
        }
    }
}
