using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Bosses.Fenrir;

namespace ValorChronicle.Tests.EditMode.Bosses.Fenrir
{
    public sealed class FenrirCombatConfigTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Fenrir/fenrir_combat_config.asset";

        [Test]
        public void ContentConfigContainsConfirmedValuesAndValidates()
        {
            FenrirCombatConfig config =
                AssetDatabase.LoadAssetAtPath<FenrirCombatConfig>(ConfigPath);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.SavageRendCoefficient, Is.EqualTo(0.8d));
            Assert.That(config.RampagingClawCoefficient, Is.EqualTo(1.2d));
            Assert.That(config.FrenzyAttackIncreaseRate, Is.EqualTo(0.6d));
            Assert.That(
                config.FrenzyTakenDamageReductionRate,
                Is.EqualTo(0.2d));
            Assert.That(config.WoundThreshold, Is.EqualTo(3));
            Assert.That(config.FrenzyRequiredCriticalHits, Is.EqualTo(3));
            Assert.That(config.TryValidate(out string errorMessage), Is.True);
            Assert.That(errorMessage, Is.Empty);
        }
    }
}
