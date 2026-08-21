using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class EventRuleModuleCatalog
    {
        public static IReadOnlyList<Type> GetModules<T>() where T : ScriptableObject
        {
            Type[] copy = (Type[])ModuleCache<T>.Types.Clone();
            return Array.AsReadOnly(copy);
        }

        private static class ModuleCache<T> where T : ScriptableObject
        {
            internal static readonly Type[] Types = TypeCache.GetTypesDerivedFrom<T>()
                .Where(type =>
                    !type.IsAbstract &&
                    !type.ContainsGenericParameters &&
                    type.GetCustomAttribute<EventRuleModuleMenuAttribute>() != null)
                .OrderBy(
                    type => type.GetCustomAttribute<EventRuleModuleMenuAttribute>().Path,
                    StringComparer.Ordinal)
                .ThenBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
