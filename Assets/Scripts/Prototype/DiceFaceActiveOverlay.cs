namespace DiceRevolver.Prototype
{
    public readonly struct DiceFaceActiveOverlay
    {
        public DiceFaceActiveOverlay(
            DiceFaceEntry baseEntry,
            DiceFaceEntry onFireEntry,
            DiceFaceEntry onHitEntry,
            DiceFaceEntry onFireEndEntry)
        {
            BaseEntry = baseEntry;
            OnFireEntry = onFireEntry;
            OnHitEntry = onHitEntry;
            OnFireEndEntry = onFireEndEntry;
        }

        public DiceFaceEntry BaseEntry { get; }
        public DiceFaceEntry OnFireEntry { get; }
        public DiceFaceEntry OnHitEntry { get; }
        public DiceFaceEntry OnFireEndEntry { get; }
        public bool IsEmpty =>
            BaseEntry == null &&
            OnFireEntry == null &&
            OnHitEntry == null &&
            OnFireEndEntry == null;

        public DiceFaceActiveOverlay Merge(DiceFaceActiveOverlay later)
        {
            return new DiceFaceActiveOverlay(
                later.BaseEntry != null ? later.BaseEntry : BaseEntry,
                later.OnFireEntry != null ? later.OnFireEntry : OnFireEntry,
                later.OnHitEntry != null ? later.OnHitEntry : OnHitEntry,
                later.OnFireEndEntry != null ? later.OnFireEndEntry : OnFireEndEntry);
        }

        public static DiceFaceActiveOverlay FromSnapshot(
            DiceFaceConfigurationSnapshot snapshot,
            bool excludeOnFireEnd)
        {
            return new DiceFaceActiveOverlay(
                snapshot.GetEntry(DiceFaceSlotType.Base),
                snapshot.GetEntry(DiceFaceSlotType.OnFire),
                snapshot.GetEntry(DiceFaceSlotType.OnHit),
                excludeOnFireEnd
                    ? null
                    : snapshot.GetEntry(DiceFaceSlotType.OnFireEnd));
        }
    }
}
