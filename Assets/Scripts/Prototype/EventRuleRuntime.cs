using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct EventRuleInvocationResult
    {
        public EventRuleInvocationResult(EventResultStatus status, string description)
        {
            Status = status;
            Description = description;
        }

        public EventResultStatus Status { get; }
        public string Description { get; }
    }

    public sealed class EventRuleRuntime
    {
        private readonly EventRuleDefinition rule;
        private readonly EventRuleStateStore state = new EventRuleStateStore();
        private int activeInvocationCount;

        public EventRuleRuntime(EventRuleDefinition rule, int equippedFace, DiceFaceSlotType slot)
        {
            this.rule = rule;
        }

        public EventRuleInvocationResult TryHandle(EventSignal signal, IEventRuleServices services)
        {
            if (rule == null || rule.Trigger == null)
            {
                return Failed("Rule definition is incomplete.");
            }

            if (rule.RecursionPolicy == EventRuleRecursionPolicy.IgnoreBonusActivation && signal.IsBonusActivation)
            {
                RecordDebug(services, "rule", "Rule ignores bonus activations.", EventResultStatus.Skipped);
                return Skipped("Rule ignores bonus activations.");
            }

            if (rule.RecursionPolicy == EventRuleRecursionPolicy.DenyReentry && activeInvocationCount > 0)
            {
                RecordDebug(services, "rule", "Rule reentry is denied.", EventResultStatus.Skipped);
                return Skipped("Rule reentry is denied.");
            }

            activeInvocationCount++;
            try
            {
                EventRuleInvocationResult triggerResult = MatchTrigger(signal, services);
                if (triggerResult.Status != EventResultStatus.Success)
                {
                    return triggerResult;
                }

                DiceEventBudget budget = signal.EventBudget;
                if (budget != null && !budget.TryConsume(Mathf.Max(1, rule.EventBudgetCost)))
                {
                    RecordDebug(services, "budget", "Event budget is exhausted.", EventResultStatus.Skipped);
                    return Skipped("Event budget is exhausted.");
                }

                EventEvaluationContext evaluationContext = new EventEvaluationContext(signal, state, services);
                EventRuleInvocationResult conditionResult = EvaluateConditions(
                    rule.Conditions,
                    evaluationContext,
                    "rule-condition");
                if (conditionResult.Status != EventResultStatus.Success)
                {
                    return conditionResult;
                }

                return ExecuteEntries(signal, services, rule.Results);
            }
            finally
            {
                activeInvocationCount--;
            }
        }

        private EventRuleInvocationResult ExecuteEntries(
            EventSignal signal,
            IEventRuleServices services,
            IReadOnlyList<EventResultEntry> entries)
        {
            if (entries == null)
            {
                return Failed("Rule results are missing.");
            }

            EventEvaluationContext evaluationContext = new EventEvaluationContext(signal, state, services);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                EventResultEntry entry = entries[entryIndex];
                if (entry == null || entry.Result == null)
                {
                    RecordDebug(services, "result", "Rule result entry is incomplete.", EventResultStatus.Failed);
                    return Failed("Rule result entry is incomplete.");
                }

                EventRuleInvocationResult localConditionResult = EvaluateConditions(
                    entry.Conditions,
                    evaluationContext,
                    "result-condition");
                if (localConditionResult.Status == EventResultStatus.Failed)
                {
                    return localConditionResult;
                }

                if (localConditionResult.Status == EventResultStatus.Skipped)
                {
                    continue;
                }

                EventExecutionContext executionContext = new EventExecutionContext(
                    signal,
                    state,
                    services,
                    (delaySeconds, scheduledEntries) => ScheduleEntries(
                        signal, services, delaySeconds, scheduledEntries),
                    rule);
                EventResult result;
                try
                {
                    result = entry.Result.Execute(executionContext);
                }
                catch (Exception exception)
                {
                    ReportException(services, exception, entry.Result);
                    RecordDebug(services, "result", "Result threw an exception.", EventResultStatus.Failed);
                    return Failed("Result threw an exception.");
                }

                RecordDebug(services, "result", result.Description, result.Status);

                if (result.Status == EventResultStatus.Failed)
                {
                    return Failed(result.Description);
                }
            }

            return Succeeded();
        }

        private EventRuleInvocationResult EvaluateConditions(
            IReadOnlyList<EventConditionModule> conditions,
            EventEvaluationContext context,
            string stage)
        {
            if (conditions == null)
            {
                return Succeeded();
            }

            for (int conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
            {
                EventConditionModule condition = conditions[conditionIndex];
                if (condition == null)
                {
                    return Failed("Rule condition is missing.");
                }

                EventConditionResult result;
                try
                {
                    result = condition.Evaluate(context);
                }
                catch (Exception exception)
                {
                    ReportException(context.Services, exception, condition);
                    RecordDebug(context.Services, "condition", "Condition threw an exception.", EventResultStatus.Failed);
                    return Failed("Condition threw an exception.");
                }

                if (result.Passed)
                {
                    RecordDebug(context.Services, stage, result.Description, EventResultStatus.Success);
                }
                else
                {
                    RecordDebug(context.Services, stage, result.FailureReason ?? result.Description, EventResultStatus.Skipped);
                    return Skipped(result.FailureReason ?? result.Description);
                }
            }

            return Succeeded();
        }

        private EventRuleInvocationResult MatchTrigger(EventSignal signal, IEventRuleServices services)
        {
            try
            {
                if (rule.Trigger.Matches(signal))
                {
                    RecordDebug(services, "trigger", null, EventResultStatus.Success);
                    return Succeeded();
                }

                RecordDebug(services, "trigger", "Trigger did not match.", EventResultStatus.Skipped);
                return Skipped("Trigger did not match.");
            }
            catch (Exception exception)
            {
                ReportException(services, exception, rule.Trigger);
                RecordDebug(services, "trigger", "Trigger threw an exception.", EventResultStatus.Failed);
                return Failed("Trigger threw an exception.");
            }
        }

        private bool ScheduleEntries(
            EventSignal signal,
            IEventRuleServices services,
            float delaySeconds,
            IReadOnlyList<EventResultEntry> entries)
        {
            if (services == null || entries == null)
            {
                return false;
            }

            return services.Schedule(delaySeconds, () => ExecuteEntries(signal, services, entries));
        }

        private static void ReportException(
            IEventRuleServices services,
            Exception exception,
            ScriptableObject module)
        {
            try
            {
                services?.ReportException(exception, module);
            }
            catch (Exception)
            {
                // Exception reporting must not escape a rule boundary.
            }
        }

        private void RecordDebug(
            IEventRuleServices services,
            string stage,
            string description,
            EventResultStatus status)
        {
            try
            {
                services?.RecordRuleDebug(rule, stage, description, status);
            }
            catch (Exception)
            {
                // Debug reporting must not escape a rule boundary.
            }
        }

        private static EventRuleInvocationResult Succeeded() =>
            new EventRuleInvocationResult(EventResultStatus.Success, null);

        private static EventRuleInvocationResult Skipped(string description) =>
            new EventRuleInvocationResult(EventResultStatus.Skipped, description);

        private static EventRuleInvocationResult Failed(string description) =>
            new EventRuleInvocationResult(EventResultStatus.Failed, description);
    }
}
