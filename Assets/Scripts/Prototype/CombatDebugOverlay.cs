using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField, InspectorName("Debug 文本")] private Text debugLabel;
        [SerializeField, Min(1), InspectorName("最大显示行数")] private int maximumLines = 14;
        [SerializeField, Min(0f), InspectorName("文本停留时间（秒）")] private float lineLifetime = 10f;
        [SerializeField, Min(8), InspectorName("字体大小")] private int fontSize = 16;

        private readonly List<CombatDebugRecord> visibleRecords = new List<CombatDebugRecord>();
        private CombatDebugTrace trace;

        public void Configure(Text label, CombatDebugTrace configuredTrace, int lineCount, float lifetime, int size)
        {
            Unsubscribe();
            debugLabel = label;
            trace = configuredTrace;
            maximumLines = Mathf.Max(1, lineCount);
            lineLifetime = Mathf.Max(0f, lifetime);
            fontSize = Mathf.Max(8, size);
            visibleRecords.Clear();
            if (debugLabel != null)
            {
                debugLabel.fontSize = fontSize;
            }

            if (trace != null)
            {
                trace.RecordAdded += HandleRecordAdded;
            }

            Render();
        }

        public void Refresh(float currentTime)
        {
            if (lineLifetime <= 0f)
            {
                return;
            }

            bool removed = false;
            while (visibleRecords.Count > 0 && currentTime - visibleRecords[0].Timestamp > lineLifetime)
            {
                visibleRecords.RemoveAt(0);
                removed = true;
            }

            if (removed)
            {
                Render();
            }
        }

        private void Update()
        {
            Refresh(Time.time);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleRecordAdded(CombatDebugRecord record)
        {
            visibleRecords.Add(record);
            while (visibleRecords.Count > maximumLines)
            {
                visibleRecords.RemoveAt(0);
            }

            Render();
        }

        private void Render()
        {
            if (debugLabel == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < visibleRecords.Count; index++)
            {
                if (index > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(CombatDebugFormatter.Format(visibleRecords[index]));
            }

            debugLabel.text = builder.ToString();
        }

        private void Unsubscribe()
        {
            if (trace != null)
            {
                trace.RecordAdded -= HandleRecordAdded;
            }
        }
    }
}
