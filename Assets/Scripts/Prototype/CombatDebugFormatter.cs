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
            builder.Append(record.Sequence.ToString("D4"));
            if (record.Face > 0)
            {
                builder.Append(" [骰面");
                builder.Append(record.Face);
                builder.Append(']');
            }

            if (!string.IsNullOrEmpty(record.Phase))
            {
                builder.Append(" [");
                builder.Append(record.Phase);
                builder.Append(']');
            }

            if (!string.IsNullOrEmpty(record.Name))
            {
                builder.Append(' ');
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
