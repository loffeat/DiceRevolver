using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [Serializable]
    public sealed class DiceFaceConfiguration
    {
        [SerializeField, InspectorName("基础事件")] private DiceFaceEntry baseEntry;
        [SerializeField, InspectorName("开火时事件")] private DiceFaceEntry onFireEntry;
        [SerializeField, InspectorName("命中时事件")] private DiceFaceEntry onHitEntry;
        [SerializeField, InspectorName("开火后事件")] private DiceFaceEntry onFireEndEntry;

        public bool Equip(DiceFaceEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.IsPassiveBase && entry.SlotType != DiceFaceSlotType.Base)
            {
                return false;
            }

            if (entry.Rule != null && !entry.Rule.CanEquip(entry.SlotType))
            {
                return false;
            }

            SetEntry(entry.SlotType, entry);
            return true;
        }

        public DiceFaceEntry GetEntry(DiceFaceSlotType slotType)
        {
            return slotType switch
            {
                DiceFaceSlotType.Base => baseEntry,
                DiceFaceSlotType.OnFire => onFireEntry,
                DiceFaceSlotType.OnHit => onHitEntry,
                DiceFaceSlotType.OnFireEnd => onFireEndEntry,
                _ => null
            };
        }

        public void ClearSlot(DiceFaceSlotType slotType)
        {
            SetEntry(slotType, null);
        }

        public DiceFaceConfigurationSnapshot CreateSnapshot(BulletEventEffect legacyBaseEffect = null)
        {
            return new DiceFaceConfigurationSnapshot(
                baseEntry,
                onFireEntry,
                onHitEntry,
                onFireEndEntry,
                legacyBaseEffect);
        }

        private void SetEntry(DiceFaceSlotType slotType, DiceFaceEntry entry)
        {
            switch (slotType)
            {
                case DiceFaceSlotType.Base:
                    baseEntry = entry;
                    break;
                case DiceFaceSlotType.OnFire:
                    onFireEntry = entry;
                    break;
                case DiceFaceSlotType.OnHit:
                    onHitEntry = entry;
                    break;
                case DiceFaceSlotType.OnFireEnd:
                    onFireEndEntry = entry;
                    break;
            }
        }
    }

    public readonly struct DiceFaceConfigurationSnapshot
    {
        private readonly DiceFaceEntry baseEntry;
        private readonly DiceFaceEntry onFireEntry;
        private readonly DiceFaceEntry onHitEntry;
        private readonly DiceFaceEntry onFireEndEntry;
        private readonly BulletEventEffect legacyBaseEffect;

        public DiceFaceConfigurationSnapshot(
            DiceFaceEntry baseEntry,
            DiceFaceEntry onFireEntry,
            DiceFaceEntry onHitEntry,
            DiceFaceEntry onFireEndEntry,
            BulletEventEffect legacyBaseEffect = null)
        {
            this.baseEntry = baseEntry;
            this.onFireEntry = onFireEntry;
            this.onHitEntry = onHitEntry;
            this.onFireEndEntry = onFireEndEntry;
            this.legacyBaseEffect = legacyBaseEffect;
        }

        public bool IsPassiveFace => baseEntry != null && baseEntry.IsPassiveBase;

        public DiceFaceEntry GetEntry(DiceFaceSlotType slotType)
        {
            return slotType switch
            {
                DiceFaceSlotType.Base => baseEntry,
                DiceFaceSlotType.OnFire => onFireEntry,
                DiceFaceSlotType.OnHit => onHitEntry,
                DiceFaceSlotType.OnFireEnd => onFireEndEntry,
                _ => null
            };
        }

        public BulletEventEffect GetEffect(DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = GetEntry(slotType);
            if (entry != null && entry.Rule != null)
            {
                return null;
            }

            if (entry != null && entry.Effect != null)
            {
                return entry.Effect;
            }

            return slotType == DiceFaceSlotType.Base ? legacyBaseEffect : null;
        }

        public EventRuleDefinition GetRule(DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = GetEntry(slotType);
            return entry != null ? entry.Rule : null;
        }

        public PassiveEventEffect GetPassiveEffect()
        {
            // 兼容垫片：被动槽已移除，T4 删除本方法。
            return null;
        }

        public DiceFaceEntry FirstEntry =>
            baseEntry != null ? baseEntry :
            onFireEntry != null ? onFireEntry :
            onHitEntry != null ? onHitEntry :
            onFireEndEntry;

        public bool HasAnyEffect =>
            GetRule(DiceFaceSlotType.Base) != null ||
            GetRule(DiceFaceSlotType.OnFire) != null ||
            GetRule(DiceFaceSlotType.OnHit) != null ||
            GetRule(DiceFaceSlotType.OnFireEnd) != null ||
            GetEffect(DiceFaceSlotType.Base) != null ||
            GetEffect(DiceFaceSlotType.OnFire) != null ||
            GetEffect(DiceFaceSlotType.OnHit) != null ||
            GetEffect(DiceFaceSlotType.OnFireEnd) != null;

        public bool HasAnyEntry => FirstEntry != null;

        public DiceFaceConfigurationSnapshot MergeActiveOverlay(
            DiceFaceActiveOverlay overlay)
        {
            return new DiceFaceConfigurationSnapshot(
                MergeEntry(baseEntry, overlay.BaseEntry),
                MergeEntry(onFireEntry, overlay.OnFireEntry),
                MergeEntry(onHitEntry, overlay.OnHitEntry),
                MergeEntry(onFireEndEntry, overlay.OnFireEndEntry),
                legacyBaseEffect);
        }

        private static DiceFaceEntry MergeEntry(
            DiceFaceEntry equippedEntry,
            DiceFaceEntry overlayEntry)
        {
            if (overlayEntry == null)
            {
                return equippedEntry;
            }

            return equippedEntry != null &&
                equippedEntry.Rule != null &&
                equippedEntry.Rule.PreserveWhenOverlaid
                    ? equippedEntry
                    : overlayEntry;
        }

        public static DiceFaceConfigurationSnapshot FromEntry(DiceFaceEntry entry)
        {
            if (entry == null)
            {
                return default;
            }

            return entry.SlotType switch
            {
                DiceFaceSlotType.Base => new DiceFaceConfigurationSnapshot(entry, null, null, null),
                DiceFaceSlotType.OnFire => new DiceFaceConfigurationSnapshot(null, entry, null, null),
                DiceFaceSlotType.OnHit => new DiceFaceConfigurationSnapshot(null, null, entry, null),
                DiceFaceSlotType.OnFireEnd => new DiceFaceConfigurationSnapshot(null, null, null, entry),
                _ => default
            };
        }
    }
}
