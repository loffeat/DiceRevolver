using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/被动/触发相邻骰面")]
    public sealed class TriggerAdjacentFacesResultModule : EventResultModule
    {
        private const string DefaultCounterKey = "adjacentActivationTriggers";

        [SerializeField, Min(1), InspectorName("每轮最大触发次数")]
        private int maximumTriggers = 1;
        [SerializeField, InspectorName("触发次数计数键")]
        private string counterKey = DefaultCounterKey;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (maximumTriggers < 1 ||
                !HasKey(counterKey) ||
                context.State == null ||
                context.Services == null ||
                context.Signal.EventBudget == null ||
                context.Signal.Activation == null)
            {
                return Skipped("相邻触发参数无效，或缺少共享预算、状态与服务。");
            }

            int used = Mathf.Max(0, context.State.GetInt(counterKey));
            if (used >= maximumTriggers)
            {
                return Skipped("相邻触发已达到本轮最大触发次数。");
            }

            IReadOnlyList<int> adjacent = DiceFaceAdjacency.AdjacentFaces(context.Signal.EquippedFace);
            if (adjacent.Count == 0)
            {
                return Skipped("装备骰面没有相邻骰面。");
            }

            int reserved = used + 1;
            context.State.SetInt(counterKey, reserved);
            int accepted = 0;
            try
            {
                for (int index = 0; index < adjacent.Count; index++)
                {
                    if (context.Services.RequestBonusActivation(
                        adjacent[index],
                        0f,
                        0f,
                        context.SourceRule))
                    {
                        accepted++;
                    }
                }
            }
            catch
            {
                RollBackReservation(context.State, used, reserved);
                throw;
            }

            if (accepted == 0)
            {
                RollBackReservation(context.State, used, reserved);
                return Skipped("相邻骰面奖励激活请求均未被接受。");
            }

            return Success($"已触发 {accepted} 个相邻骰面。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-adjacent-counter-key", "相邻触发结果缺少触发次数计数键。", this);
            if (maximumTriggers < 1)
            {
                AddError(issues, "invalid-adjacent-trigger-limit", "每轮最大触发次数必须至少为 1。", this);
            }
        }

        private void RollBackReservation(
            EventRuleStateStore state,
            int previous,
            int reserved)
        {
            if (state.GetInt(counterKey) == reserved)
            {
                state.SetInt(counterKey, previous);
            }
        }
    }
}
