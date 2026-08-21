using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/被动/请求奖励骰面激活")]
    public sealed class RequestBonusActivationResultModule : EventResultModule
    {
        private const string DefaultCounterKey = "bonusActivationTriggers";

        [SerializeField, Min(1), InspectorName("每轮最大触发次数")]
        private int maximumTriggers = 1;
        [SerializeField, Min(0f), InspectorName("最大散布角度")]
        private float maximumSpreadAngle;
        [SerializeField, Min(0f), InspectorName("最小散布间隔")]
        private float minimumSpreadSeparation;
        [SerializeField, InspectorName("触发次数计数键")]
        private string counterKey = DefaultCounterKey;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (maximumTriggers < 1 || maximumSpreadAngle < 0f || minimumSpreadSeparation < 0f ||
                float.IsNaN(maximumSpreadAngle) || float.IsInfinity(maximumSpreadAngle) ||
                float.IsNaN(minimumSpreadSeparation) || float.IsInfinity(minimumSpreadSeparation) ||
                minimumSpreadSeparation > maximumSpreadAngle ||
                !HasKey(counterKey) || context.State == null || context.Services == null ||
                context.Signal.EventBudget == null || context.Signal.Activation == null)
            {
                return Skipped("奖励激活参数无效，或缺少来源激活、共享预算、状态与服务。");
            }

            int used = Mathf.Max(0, context.State.GetInt(counterKey));
            if (used >= maximumTriggers)
            {
                return Skipped("奖励激活已达到本轮最大触发次数。");
            }

            int reserved = used + 1;
            context.State.SetInt(counterKey, reserved);
            bool accepted;
            try
            {
                accepted = context.Services.RequestBonusActivation(
                    context.Signal.EquippedFace,
                    maximumSpreadAngle,
                    minimumSpreadSeparation,
                    null);
            }
            catch
            {
                RollBackReservationWithoutNestedProgress(context.State, used, reserved);
                throw;
            }

            if (!accepted)
            {
                RollBackReservationWithoutNestedProgress(context.State, used, reserved);
                return Skipped("奖励骰面激活请求未被接受。");
            }

            return Success("已请求奖励骰面激活。");
        }

        private void RollBackReservationWithoutNestedProgress(
            EventRuleStateStore state,
            int previous,
            int reserved)
        {
            if (state.GetInt(counterKey) == reserved)
            {
                state.SetInt(counterKey, previous);
            }
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-bonus-counter-key", "奖励激活结果缺少触发次数计数键。", this);
            if (maximumTriggers < 1)
            {
                AddError(issues, "invalid-bonus-trigger-limit", "每轮最大触发次数必须至少为 1。", this);
            }

            if (maximumSpreadAngle < 0f || minimumSpreadSeparation < 0f ||
                float.IsNaN(maximumSpreadAngle) || float.IsInfinity(maximumSpreadAngle) ||
                float.IsNaN(minimumSpreadSeparation) || float.IsInfinity(minimumSpreadSeparation) ||
                minimumSpreadSeparation > maximumSpreadAngle)
            {
                AddError(issues, "invalid-bonus-spread", "奖励激活散布必须是有限非负数，且最小间隔不能大于最大角度。", this);
            }
        }
    }
}
