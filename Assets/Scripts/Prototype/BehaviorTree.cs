using System;

namespace DiceRevolver.Prototype
{
    public enum BehaviorStatus
    {
        Success,
        Failure,
        Running
    }

    public interface IBehaviorNode<in TContext>
    {
        BehaviorStatus Tick(TContext context);
    }

    public sealed class BehaviorAction<TContext> : IBehaviorNode<TContext>
    {
        private readonly Func<TContext, BehaviorStatus> action;

        public BehaviorAction(Func<TContext, BehaviorStatus> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public BehaviorStatus Tick(TContext context)
        {
            return action(context);
        }
    }

    public sealed class BehaviorCondition<TContext> : IBehaviorNode<TContext>
    {
        private readonly Predicate<TContext> condition;

        public BehaviorCondition(Predicate<TContext> condition)
        {
            this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public BehaviorStatus Tick(TContext context)
        {
            return condition(context) ? BehaviorStatus.Success : BehaviorStatus.Failure;
        }
    }

    public sealed class BehaviorSequence<TContext> : IBehaviorNode<TContext>
    {
        private readonly IBehaviorNode<TContext>[] children;

        public BehaviorSequence(params IBehaviorNode<TContext>[] children)
        {
            this.children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public BehaviorStatus Tick(TContext context)
        {
            for (int i = 0; i < children.Length; i++)
            {
                BehaviorStatus status = children[i].Tick(context);
                if (status != BehaviorStatus.Success)
                {
                    return status;
                }
            }

            return BehaviorStatus.Success;
        }
    }

    public sealed class BehaviorSelector<TContext> : IBehaviorNode<TContext>
    {
        private readonly IBehaviorNode<TContext>[] children;

        public BehaviorSelector(params IBehaviorNode<TContext>[] children)
        {
            this.children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public BehaviorStatus Tick(TContext context)
        {
            for (int i = 0; i < children.Length; i++)
            {
                BehaviorStatus status = children[i].Tick(context);
                if (status != BehaviorStatus.Failure)
                {
                    return status;
                }
            }

            return BehaviorStatus.Failure;
        }
    }

    public sealed class BehaviorParallel<TContext> : IBehaviorNode<TContext>
    {
        private readonly IBehaviorNode<TContext>[] children;

        public BehaviorParallel(params IBehaviorNode<TContext>[] children)
        {
            this.children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public BehaviorStatus Tick(TContext context)
        {
            bool anyRunning = false;
            for (int i = 0; i < children.Length; i++)
            {
                BehaviorStatus status = children[i].Tick(context);
                if (status == BehaviorStatus.Failure)
                {
                    return BehaviorStatus.Failure;
                }

                anyRunning |= status == BehaviorStatus.Running;
            }

            return anyRunning ? BehaviorStatus.Running : BehaviorStatus.Success;
        }
    }
}
