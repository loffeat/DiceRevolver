using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    /// <summary>
    /// 从候选弹丸中不重复地挑选闪电链连接目标。
    /// 纯静态辅助，不依赖场景对象或具体 Effect 类型。
    /// </summary>
    public static class LightningChainTargetSelector
    {
        /// <summary>
        /// 从候选列表中挑选最多 <paramref name="maximumConnections"/> 个存活弹丸，
        /// 每个弹丸最多被选中一次；随机下标由 <paramref name="randomIndex"/> 提供，
        /// 以便测试注入确定性序列，生产环境使用 <see cref="UnityEngine.Random"/>。
        /// </summary>
        public static IReadOnlyList<ProjectileHandle> Select(
            IReadOnlyList<ProjectileHandle> candidates,
            int maximumConnections,
            Func<int, int> randomIndex)
        {
            List<ProjectileHandle> pool = new List<ProjectileHandle>();
            if (candidates != null)
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    if (candidates[index].IsAlive)
                    {
                        pool.Add(candidates[index]);
                    }
                }
            }

            List<ProjectileHandle> selected = new List<ProjectileHandle>();
            int count = Mathf.Min(Mathf.Max(0, maximumConnections), pool.Count);
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = randomIndex != null
                    ? Mathf.Clamp(randomIndex(pool.Count), 0, pool.Count - 1)
                    : 0;
                selected.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }

            return selected;
        }
    }
}
