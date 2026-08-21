using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/骰面/覆盖下一骰面活动槽")]
    public sealed class QueueActiveOverlayResultModule : EventResultModule
    {
        public override EventResult Execute(EventExecutionContext context)
        {
            DiceFaceActivation activation = context.Signal.Activation;
            if (activation == null)
            {
                return Skipped("缺少来源骰面激活。");
            }

            if (context.Services == null)
            {
                return Skipped("缺少下一骰面覆盖服务。");
            }

            DiceFaceActiveOverlay overlay = DiceFaceActiveOverlay.FromSnapshot(
                activation.Configuration,
                true);
            if (overlay.IsEmpty)
            {
                return Skipped("来源骰面没有可复制的非空活动槽。");
            }

            bool requested = context.Services.QueueNextShotOverlay(overlay);
            return requested
                ? new EventResult(EventResultStatus.Success, "已请求覆盖下一骰面的非空活动槽。")
                : Skipped("下一骰面覆盖请求未被接受。");
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }
}
