using System;
using System.Collections.Generic;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;

namespace DiceRevolver.Editor
{
    internal readonly struct EventRuleDebugSnapshot
    {
        internal EventRuleDebugSnapshot(
            IReadOnlyList<CombatDebugRecord> records,
            string message)
        {
            Records = records ?? Array.Empty<CombatDebugRecord>();
            Message = message ?? string.Empty;
        }

        internal IReadOnlyList<CombatDebugRecord> Records { get; }
        internal string Message { get; }
    }

    internal sealed class EventRuleDebugSource
    {
        private readonly Func<bool> isPlaying;

        internal EventRuleDebugSource()
            : this(() => EditorApplication.isPlaying)
        {
        }

        internal EventRuleDebugSource(Func<bool> isPlaying)
        {
            this.isPlaying = isPlaying ?? throw new ArgumentNullException(nameof(isPlaying));
        }

        internal EventRuleDebugSnapshot ReadSelected()
        {
            if (!isPlaying.Invoke())
            {
                return new EventRuleDebugSnapshot(
                    Array.Empty<CombatDebugRecord>(),
                    "Play Mode 中可查看所选 Gun 的规则记录。");
            }

            DiceRevolverGun gun = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<DiceRevolverGun>()
                : null;
            if (gun == null)
            {
                return new EventRuleDebugSnapshot(
                    Array.Empty<CombatDebugRecord>(),
                    "请选择场景中的 DiceRevolverGun / Gun。");
            }

            CombatDebugRecord[] records = gun.DebugTrace.Records.ToArray();
            return new EventRuleDebugSnapshot(Array.AsReadOnly(records), string.Empty);
        }
    }
}
