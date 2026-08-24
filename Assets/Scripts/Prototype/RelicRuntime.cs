using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class RelicRuntime
    {
        public IReadOnlyList<RelicDefinition> Relics => relics;

        private readonly List<RelicDefinition> relics = new();

        public void SetRelics(IReadOnlyList<RelicDefinition> definitions)
        {
            relics.Clear();
            if (definitions != null)
            {
                for (int index = 0; index < definitions.Count; index++)
                {
                    if (definitions[index] != null)
                    {
                        relics.Add(definitions[index]);
                    }
                }
            }
        }

        public bool AddRelic(RelicDefinition relic)
        {
            if (relic == null || relics.Contains(relic))
            {
                return false;
            }

            relics.Add(relic);
            return true;
        }

        public void ApplyRoundStart(RelicContext context)
        {
            for (int index = 0; index < relics.Count; index++)
            {
                if (relics[index] != null)
                {
                    relics[index].ApplyAtRoundStart(context);
                }
            }
        }
    }
}
