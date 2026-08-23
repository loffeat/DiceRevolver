using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    /// <summary>每枪本轮弹丸生成统计（按弹丸定义计数，换弹时重置）。</summary>
    public sealed class RoundProjectileStatistic
    {
        private readonly Dictionary<ProjectileDefinition, int> counts = new();

        public void Increment(ProjectileDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            counts.TryGetValue(definition, out int current);
            counts[definition] = current + 1;
        }

        public int Count(ProjectileDefinition definition)
        {
            return definition != null && counts.TryGetValue(definition, out int current)
                ? current
                : 0;
        }

        public void Reset()
        {
            counts.Clear();
        }
    }
}
