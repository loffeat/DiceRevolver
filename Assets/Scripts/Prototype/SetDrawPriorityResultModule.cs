using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/被动/设置抽取优先级")]
    public sealed class SetDrawPriorityResultModule : EventResultModule
    {
        [SerializeField, InspectorName("抽取优先级")]
        private int priority;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (context.Services == null)
            {
                return Skipped("缺少抽面优先级服务。");
            }

            context.Services.SetDrawPriority(priority);
            return Success($"抽取优先级设为 {priority}。");
        }
    }
}
