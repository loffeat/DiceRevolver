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
            yield return Port<DiceRevolverGun>("eventBudgetPerActivation", "单次骰面事件预算");
            yield return Port<DiceRevolverGun>("reloadBlinkSpeed", "换弹闪烁速度");

            yield return Port<DiceFaceEntry>("displayName", "显示名称");
            yield return Port<DiceFaceEntry>("slotType", "槽位类型");
            yield return Port<DiceFaceEntry>("effect", "事件效果");
            yield return Port<DiceFaceEntry>("passiveEffect", "被动效果");

            yield return Port<ProjectileDefinition>("projectilePrefab", "弹丸 Prefab");
            yield return Port<ProjectileDefinition>("projectileType", "弹幕类型");
            yield return Port<ProjectileDefinition>("projectileTag", "弹幕标签");
            yield return Port<ProjectileDefinition>("projectileTypeDefinition", "弹幕类型资源");
            yield return Port<ProjectileDefinition>("projectileTags", "弹幕标签资源");
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
            yield return Port<AreaExplosionProjectile>("radius", "爆炸半径");
            yield return Port<AreaExplosionProjectile>("visualDuration", "视觉持续时间（秒）");
            yield return Port<AreaExplosionProjectile>("ringColor", "圆环颜色");
            yield return Port<AreaExplosionProjectile>("ringWidth", "圆环宽度");
            yield return Port<AreaExplosionProjectile>("targetLayers", "受击图层");
            yield return Port<DiceFaceLibrary>("entries", "骰面词条库");
            yield return Port<BulletEventLibrary>("effects", "子弹事件库");
            yield return Port<ProjectileDefinitionLibrary>("definitions", "弹丸定义库");
            yield return Port<ProjectileTypeLibrary>("types", "弹幕类型库");
            yield return Port<ProjectileTagLibrary>("tags", "弹幕标签库");
            yield return Port<TeslaPassiveEffect>("lightningTag", "雷电标签");
            yield return Port<TeslaPassiveEffect>("damagePerStack", "每层伤害提升比例");
            yield return Port<EchoSynergyPassiveEffect>("maximumTriggersPerChamber", "每轮最大呼应次数");
            yield return Port<EchoSynergyPassiveEffect>("maximumSpreadAngle", "最大自然散布角度");
            yield return Port<EchoSynergyPassiveEffect>("minimumSpreadSeparation", "同帧最小角度间隔");
            yield return Port<ElectromagneticResonanceEffect>("searchRadius", "共鸣搜索半径");
            yield return Port<ElectromagneticResonanceEffect>("maximumConnections", "最大连接数量");
            yield return Port<LightningChainDefinition>("damage", "闪电链伤害");
            yield return Port<LightningChainDefinition>("chainWidth", "闪电链宽度");
            yield return Port<DiceFaceLoadout>("faceConfigurations", "六面五槽位配置");
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

        [Test]
        public void GunInspectorUsesFixedFaceRulesAndBoundedActivationBudget()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Assert.That(typeof(DiceRevolverGun).GetField("faceCount", flags), Is.Null);
            Assert.That(typeof(DiceRevolverGun).GetField("reloadDropDistance", flags), Is.Null);

            FieldInfo budget = typeof(DiceRevolverGun).GetField("eventBudgetPerActivation", flags);
            Assert.That(budget, Is.Not.Null);
            Assert.That(budget.GetCustomAttribute<SerializeField>(), Is.Not.Null);
            MinAttribute minimum = budget.GetCustomAttribute<MinAttribute>();
            Assert.That(minimum, Is.Not.Null);
            Assert.That(minimum.min, Is.EqualTo(1f));
            Assert.That(
                budget.GetCustomAttribute<InspectorNameAttribute>()?.displayName,
                Is.EqualTo("单次骰面事件预算"));
        }

        private static TestCaseData Port<T>(string propertyName, string displayName)
            where T : UnityEngine.Object
        {
            return new TestCaseData(typeof(T), propertyName, displayName)
                .SetName($"{typeof(T).Name}_{propertyName}_UsesChineseInspectorName");
        }
    }
}
