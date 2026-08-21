namespace DiceRevolver.Prototype
{
    public sealed class DiceEventRuleRuntimeSet
    {
        private const int SlotCount = 5;
        private readonly EventRuleDefinition[,] definitions =
            new EventRuleDefinition[DiceRevolverRules.FaceCount, SlotCount];
        private readonly EventRuleRuntime[,] runtimes =
            new EventRuleRuntime[DiceRevolverRules.FaceCount, SlotCount];

        public void RebuildFace(int face, DiceFaceConfigurationSnapshot snapshot)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return;
            }

            int faceIndex = face - 1;
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                DiceFaceSlotType slot = (DiceFaceSlotType)slotIndex;
                EventRuleDefinition definition = snapshot.GetRule(slot);
                if (definitions[faceIndex, slotIndex] == definition)
                {
                    continue;
                }

                definitions[faceIndex, slotIndex] = definition;
                runtimes[faceIndex, slotIndex] = definition != null
                    ? new EventRuleRuntime(definition, face, slot)
                    : null;
            }
        }

        public bool ExecuteActive(
            int face,
            DiceFaceSlotType slot,
            EventSignal signal,
            IEventRuleServices services)
        {
            int slotIndex = (int)slot;
            if (face < 1 || face > DiceRevolverRules.FaceCount ||
                slotIndex < 0 || slotIndex >= SlotCount)
            {
                return false;
            }

            EventRuleRuntime runtime = runtimes[face - 1, slotIndex];
            if (runtime == null)
            {
                return false;
            }

            runtime.TryHandle(signal, services);
            return true;
        }
    }
}
