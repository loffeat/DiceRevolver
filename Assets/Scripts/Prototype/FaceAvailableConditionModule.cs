using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/骰面/指定骰面可检索")]
    public sealed class FaceAvailableConditionModule : EventConditionModule
    {
        [SerializeField, Range(1, DiceRevolverRules.FaceCount), InspectorName("指定骰面")]
        private int face = 4;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            IReadOnlyList<int> remainingFaces = context.Signal.RemainingFaces;
            if (remainingFaces != null)
            {
                for (int index = 0; index < remainingFaces.Count; index++)
                {
                    if (remainingFaces[index] == face)
                    {
                        return new EventConditionResult(
                            false,
                            $"骰面 {face} 仍在剩余骰面中。",
                            $"骰面 {face} 仍在剩余骰面中。");
                    }
                }
            }

            return new EventConditionResult(true, $"骰面 {face} 可通过填充进行检索。");
        }
    }
}
