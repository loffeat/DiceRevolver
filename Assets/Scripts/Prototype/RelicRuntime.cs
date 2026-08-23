using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class RelicRuntime
    {
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
