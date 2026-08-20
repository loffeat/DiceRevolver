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
        [SerializeField, InspectorName("被动事件")] private DiceFaceEntry passiveEntry;

        public bool Equip(DiceFaceEntry entry)
        {
            if (entry == null)
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
                DiceFaceSlotType.Passive => passiveEntry,
                _ => null
            };
        }

        public DiceFaceConfigurationSnapshot CreateSnapshot(BulletEventEffect legacyBaseEffect = null)
        {
            return new DiceFaceConfigurationSnapshot(
                baseEntry,
                onFireEntry,
                onHitEntry,
                onFireEndEntry,
                passiveEntry,
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
                case DiceFaceSlotType.Passive:
                    passiveEntry = entry;
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
        private readonly DiceFaceEntry passiveEntry;
        private readonly BulletEventEffect legacyBaseEffect;

        public DiceFaceConfigurationSnapshot(
            DiceFaceEntry baseEntry,
            DiceFaceEntry onFireEntry,
            DiceFaceEntry onHitEntry,
            DiceFaceEntry onFireEndEntry,
            BulletEventEffect legacyBaseEffect = null)
            : this(
                baseEntry,
                onFireEntry,
                onHitEntry,
                onFireEndEntry,
                null,
                legacyBaseEffect)
        {
        }

        public DiceFaceConfigurationSnapshot(
            DiceFaceEntry baseEntry,
            DiceFaceEntry onFireEntry,
            DiceFaceEntry onHitEntry,
            DiceFaceEntry onFireEndEntry,
            DiceFaceEntry passiveEntry,
            BulletEventEffect legacyBaseEffect = null)
        {
            this.baseEntry = baseEntry;
            this.onFireEntry = onFireEntry;
            this.onHitEntry = onHitEntry;
            this.onFireEndEntry = onFireEndEntry;
            this.passiveEntry = passiveEntry;
            this.legacyBaseEffect = legacyBaseEffect;
        }

        public DiceFaceEntry GetEntry(DiceFaceSlotType slotType)
        {
            return slotType switch
            {
                DiceFaceSlotType.Base => baseEntry,
                DiceFaceSlotType.OnFire => onFireEntry,
                DiceFaceSlotType.OnHit => onHitEntry,
                DiceFaceSlotType.OnFireEnd => onFireEndEntry,
                DiceFaceSlotType.Passive => passiveEntry,
                _ => null
            };
        }

        public BulletEventEffect GetEffect(DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = GetEntry(slotType);
            if (entry != null && entry.Effect != null)
            {
                return entry.Effect;
            }

            return slotType == DiceFaceSlotType.Base ? legacyBaseEffect : null;
        }

        public PassiveEventEffect GetPassiveEffect()
        {
            return passiveEntry != null ? passiveEntry.PassiveEffect : null;
        }

        public DiceFaceEntry FirstEntry =>
            baseEntry != null ? baseEntry :
            onFireEntry != null ? onFireEntry :
            onHitEntry != null ? onHitEntry :
            onFireEndEntry != null ? onFireEndEntry :
            passiveEntry;

        public bool HasAnyEffect =>
            GetEffect(DiceFaceSlotType.Base) != null ||
            GetEffect(DiceFaceSlotType.OnFire) != null ||
            GetEffect(DiceFaceSlotType.OnHit) != null ||
            GetEffect(DiceFaceSlotType.OnFireEnd) != null ||
            GetPassiveEffect() != null;

        public bool HasAnyEntry => FirstEntry != null;

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
                DiceFaceSlotType.Passive => new DiceFaceConfigurationSnapshot(
                    null,
                    null,
                    null,
                    null,
                    entry),
                _ => default
            };
        }
    }
}
