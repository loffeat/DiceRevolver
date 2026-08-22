using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    public enum CounterComparisonOperator
    {
        [InspectorName("等于")] Equal,
        [InspectorName("不等于")] NotEqual,
        [InspectorName("小于")] LessThan,
        [InspectorName("小于等于")] LessThanOrEqual,
        [InspectorName("大于")] GreaterThan,
        [InspectorName("大于等于")] GreaterThanOrEqual
    }
}
