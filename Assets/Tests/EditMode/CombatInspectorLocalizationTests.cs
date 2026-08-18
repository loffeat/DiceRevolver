using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class CombatInspectorLocalizationTests
    {
        private static IEnumerable<TestCaseData> CoreCombatPorts()
        {
            yield return Port<DiceRevolverGun>("player", "玩家控制器");
            yield return Port<DiceRevolverGun>("muzzle", "枪口");
            yield return Port<DiceRevolverGun>("shotsPerSecond", "每秒射击次数");
            yield return Port<DiceRevolverGun>("reloadDuration", "换弹时间（秒）");
            yield return Port<DiceRevolverGun>("reloadBlinkSpeed", "换弹闪烁速度");

            yield return Port<DiceFaceEntry>("displayName", "显示名称");
            yield return Port<DiceFaceEntry>("onFireEffects", "开火时事件");
            yield return Port<DiceFaceEntry>("onHitEffects", "击中时事件");
            yield return Port<DiceFaceEntry>("onFireEndEffects", "结束开火时事件");

            yield return Port<ProjectileDefinition>("projectilePrefab", "弹丸 Prefab");
            yield return Port<ProjectileDefinition>("projectileType", "弹幕类型");
            yield return Port<ProjectileDefinition>("projectileTag", "弹幕标签");
            yield return Port<ProjectileDefinition>("damage", "弹幕伤害");
            yield return Port<ProjectileDefinition>("flightDistance", "飞行距离");
            yield return Port<ProjectileDefinition>("flightSpeed", "飞行速度");
            yield return Port<ProjectileDefinition>("enemyPierceCount", "敌人穿透数量");
            yield return Port<ProjectileDefinition>("extensionPorts", "扩展端口");
            yield return Port<ProjectileDefinition>("defaultAttackEffect", "默认视为攻击特效");

            yield return Port<Projectile>("speed", "默认飞行速度");
            yield return Port<Projectile>("lifetime", "默认存在时间（秒）");
            yield return Port<ExtraShotOnFireEffect>("delaySeconds", "第二发延迟（秒）");
            yield return Port<ExtraShotOnFireEffect>("attackEffectOverride", "攻击特效判定");
            yield return Port<ProjectileSpawnEffect>("projectileDefinition", "弹丸定义");
            yield return Port<ProjectileSpawnEffect>("delaySeconds", "生成延迟（秒）");
            yield return Port<ExplosionOnHitEffect>("explosionProjectileDefinition", "爆炸弹丸定义");
            yield return Port<DiceFaceLibrary>("entries", "骰面词条库");
            yield return Port<BulletEventLibrary>("effects", "子弹事件库");
            yield return Port<ProjectileDefinitionLibrary>("definitions", "弹丸定义库");
            yield return Port<DiceFaceLoadout>("entries", "六面装备");
            yield return Port<DiceFaceLoadout>("baseEffects", "六面基础事件");
        }

        [TestCaseSource(nameof(CoreCombatPorts))]
        public void CoreCombatPortUsesChineseInspectorName(
            Type targetType,
            string propertyName,
            string expectedDisplayName)
        {
            FieldInfo field = targetType.GetField(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing serialized field {targetType.Name}.{propertyName}");

            InspectorNameAttribute inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();

            Assert.That(inspectorName, Is.Not.Null);
            Assert.That(inspectorName.displayName, Is.EqualTo(expectedDisplayName));
        }

        private static TestCaseData Port<T>(string propertyName, string displayName)
            where T : UnityEngine.Object
        {
            return new TestCaseData(typeof(T), propertyName, displayName)
                .SetName($"{typeof(T).Name}_{propertyName}_UsesChineseInspectorName");
        }
    }
}
