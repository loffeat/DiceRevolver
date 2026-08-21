using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/骰面/填充并强制骰面")]
    public sealed class ForceFaceResultModule : EventResultModule
    {
        [SerializeField, Range(1, DiceRevolverRules.FaceCount), InspectorName("指定骰面")]
        private int face = 4;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (context.Services == null)
            {
                return Skipped("缺少填充并强制骰面服务。");
            }

            bool requested = context.Services.RequestRefillAndForceNextFace(face);
            return requested
                ? new EventResult(EventResultStatus.Success, $"已请求填充并强制骰面 {face}。")
                : Skipped($"填充并强制骰面 {face} 的请求未被接受。");
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }
}
