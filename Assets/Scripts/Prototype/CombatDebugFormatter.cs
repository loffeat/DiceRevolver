using System.Text;

namespace DiceRevolver.Prototype
{
    public static class CombatDebugFormatter
    {
        public static string Format(CombatDebugRecord record)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(' ', record.Depth * 2);
            builder.Append('#');
            builder.Append(record.Sequence);
            if (record.Face > 0)
            {
                builder.Append(" 骰面");
                builder.Append(record.Face);
            }

            if (!string.IsNullOrEmpty(record.Phase) || !string.IsNullOrEmpty(record.Name))
            {
                builder.Append(" · ");
                if (!string.IsNullOrEmpty(record.Phase))
                {
                    builder.Append(record.Phase);
                    if (!string.IsNullOrEmpty(record.Name))
                    {
                        builder.Append('：');
                    }
                }

                builder.Append(record.Name);
            }

            if (!string.IsNullOrEmpty(record.Detail))
            {
                builder.Append('：');
                builder.Append(record.Detail);
            }

            return builder.ToString();
        }
    }
}
