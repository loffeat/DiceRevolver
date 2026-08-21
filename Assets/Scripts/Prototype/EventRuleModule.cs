using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public abstract class EventTriggerModule : ScriptableObject
    {
        public abstract bool Matches(EventSignal signal);
        public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
    }

    public abstract class EventConditionModule : ScriptableObject
    {
        public abstract EventConditionResult Evaluate(EventEvaluationContext context);
        public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
    }

    public abstract class EventResultModule : ScriptableObject
    {
        public abstract EventResult Execute(EventExecutionContext context);
        public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
    }
}
