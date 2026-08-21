using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/骰面/来源为装备骰面")]
    public sealed class SourceFaceConditionModule : EventConditionModule
    {
        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            return context.Signal.SourceFace == context.Signal.EquippedFace
                ? Passed("来源骰面与装备骰面相同。")
                : Failed("来源骰面与装备骰面不同。");
        }
    }
}
