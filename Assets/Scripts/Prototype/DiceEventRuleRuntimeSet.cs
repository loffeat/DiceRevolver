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
            if (!IsValidSlot(face, slotIndex))
            {
                return false;
            }

            return ExecuteActive(
                face,
                slot,
                definitions[face - 1, slotIndex],
                signal,
                services);
        }

        public bool ExecuteActive(
            int face,
            DiceFaceSlotType slot,
            EventRuleDefinition definition,
            EventSignal signal,
            IEventRuleServices services)
        {
            int slotIndex = (int)slot;
            if (!IsValidSlot(face, slotIndex) || definition == null)
            {
                return false;
            }

            EventRuleRuntime runtime = definitions[face - 1, slotIndex] == definition
                ? runtimes[face - 1, slotIndex]
                : new EventRuleRuntime(definition, face, slot);

            runtime.TryHandle(signal, services);
            return true;
        }

        private static bool IsValidSlot(int face, int slotIndex)
        {
            return face >= 1 && face <= DiceRevolverRules.FaceCount &&
                slotIndex >= 0 && slotIndex < SlotCount;
        }
    }
}
