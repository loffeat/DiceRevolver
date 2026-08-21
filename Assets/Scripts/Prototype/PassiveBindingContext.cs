namespace DiceRevolver.Prototype
{
    public readonly struct BonusDiceActivationRequest
    {
        public BonusDiceActivationRequest(
            int face,
            DiceEventBudget eventBudget,
            long suppressedPassiveInstanceId,
            float maximumSpreadAngle,
            float minimumSpreadSeparation)
            : this(
                face,
                eventBudget,
                suppressedPassiveInstanceId,
                maximumSpreadAngle,
                minimumSpreadSeparation,
                null)
        {
        }

        public BonusDiceActivationRequest(
            int face,
            DiceEventBudget eventBudget,
            long suppressedPassiveInstanceId,
            float maximumSpreadAngle,
            float minimumSpreadSeparation,
            DiceFaceActivation sourceActivation)
            : this(
                face,
                eventBudget,
                suppressedPassiveInstanceId,
                maximumSpreadAngle,
                minimumSpreadSeparation,
                sourceActivation,
                null)
        {
        }

        public BonusDiceActivationRequest(
            int face,
            DiceEventBudget eventBudget,
            long suppressedPassiveInstanceId,
            float maximumSpreadAngle,
            float minimumSpreadSeparation,
            DiceFaceActivation sourceActivation,
            EventRuleDefinition sourceRule)
        {
            Face = face;
            EventBudget = eventBudget;
            SuppressedPassiveInstanceId = suppressedPassiveInstanceId;
            MaximumSpreadAngle = maximumSpreadAngle;
            MinimumSpreadSeparation = minimumSpreadSeparation;
            SourceActivation = sourceActivation;
            SourceRule = sourceRule;
        }

        public int Face { get; }
        public DiceEventBudget EventBudget { get; }
        public long SuppressedPassiveInstanceId { get; }
        public float MaximumSpreadAngle { get; }
        public float MinimumSpreadSeparation { get; }
        public DiceFaceActivation SourceActivation { get; }
        public EventRuleDefinition SourceRule { get; }
    }

    public readonly struct PassiveBindingContext
    {
        public PassiveBindingContext(int face)
            : this(face, null)
        {
        }

        public PassiveBindingContext(
            int face,
            System.Func<int, ProjectileTypeDefinition> baseProjectileTypeResolver)
            : this(face, 0, baseProjectileTypeResolver, null)
        {
        }

        public PassiveBindingContext(
            int face,
            long instanceId,
            System.Func<int, ProjectileTypeDefinition> baseProjectileTypeResolver,
            System.Func<BonusDiceActivationRequest, bool> bonusActivationRequest)
        {
            Face = face;
            InstanceId = instanceId;
            this.baseProjectileTypeResolver = baseProjectileTypeResolver;
            this.bonusActivationRequest = bonusActivationRequest;
        }

        private readonly System.Func<int, ProjectileTypeDefinition> baseProjectileTypeResolver;
        private readonly System.Func<BonusDiceActivationRequest, bool> bonusActivationRequest;
        public int Face { get; }
        public long InstanceId { get; }
        public ProjectileTypeDefinition BaseProjectileType =>
            baseProjectileTypeResolver?.Invoke(Face);

        public bool RequestBonusActivation(
            DiceEventBudget eventBudget,
            float maximumSpreadAngle,
            float minimumSpreadSeparation)
        {
            return RequestBonusActivation(
                eventBudget,
                maximumSpreadAngle,
                minimumSpreadSeparation,
                null);
        }

        public bool RequestBonusActivation(
            DiceEventBudget eventBudget,
            float maximumSpreadAngle,
            float minimumSpreadSeparation,
            DiceFaceActivation sourceActivation)
        {
            return eventBudget != null &&
                bonusActivationRequest != null &&
                bonusActivationRequest.Invoke(new BonusDiceActivationRequest(
                    Face,
                    eventBudget,
                    InstanceId,
                    maximumSpreadAngle,
                    minimumSpreadSeparation,
                    sourceActivation));
        }
    }
}
