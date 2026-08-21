using System;

namespace DiceRevolver.Prototype
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EventRuleModuleMenuAttribute : Attribute
    {
        public EventRuleModuleMenuAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
