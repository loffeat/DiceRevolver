using System;
using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    /// <summary>骰面相邻关系（按构筑 UI 布局 DiceBuildPageUI.FacePositions 的 8 向邻接）。</summary>
    public static class DiceFaceAdjacency
    {
        private static readonly IReadOnlyDictionary<int, int[]> Table =
            new Dictionary<int, int[]>
            {
                { 1, new[] { 2, 3, 4 } },
                { 2, new[] { 1, 3, 6 } },
                { 3, new[] { 1, 2, 4, 6 } },
                { 4, new[] { 1, 3, 5, 6 } },
                { 5, new[] { 4 } },
                { 6, new[] { 2, 3, 4 } }
            };

        public static IReadOnlyList<int> AdjacentFaces(int face)
        {
            return Table.TryGetValue(face, out int[] value)
                ? value
                : Array.Empty<int>();
        }
    }
}
