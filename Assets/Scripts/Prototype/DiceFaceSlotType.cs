namespace DiceRevolver.Prototype
{
    public enum DiceFaceSlotType
    {
        Base = 0,
        OnFire = 1,
        OnHit = 2,
        OnFireEnd = 3,
        Passive = 4
    }

    public static class DiceFaceSlotTypeLabels
    {
        public static string ToChineseLabel(this DiceFaceSlotType slotType)
        {
            return slotType switch
            {
                DiceFaceSlotType.Base => "基础",
                DiceFaceSlotType.OnFire => "开火",
                DiceFaceSlotType.OnHit => "命中",
                DiceFaceSlotType.OnFireEnd => "开火后",
                _ => "未知"
            };
        }
    }
}
