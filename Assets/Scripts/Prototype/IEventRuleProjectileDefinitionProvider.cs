using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public interface IEventRuleProjectileDefinitionProvider
    {
        bool IsPrimaryProjectile { get; }
        ProjectileDefinition ProjectileDefinition { get; }
    }
}
