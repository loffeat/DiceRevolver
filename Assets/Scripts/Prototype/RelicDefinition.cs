using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct RelicContext
    {
        public RelicContext(
            DiceRevolverRuntime runtime,
            IReadOnlyList<int> passiveFaces,
            int faceCount)
        {
            Runtime = runtime;
            PassiveFaces = passiveFaces;
            FaceCount = faceCount;
        }

        public DiceRevolverRuntime Runtime { get; }
        public IReadOnlyList<int> PassiveFaces { get; }
        public int FaceCount { get; }
    }

    public abstract class RelicDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("显示名称")] private string displayName;
        [SerializeField, InspectorName("描述")] private string description;
        [SerializeField, InspectorName("遗物立绘")] private Sprite icon;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public abstract void ApplyAtRoundStart(RelicContext context);
    }
}
