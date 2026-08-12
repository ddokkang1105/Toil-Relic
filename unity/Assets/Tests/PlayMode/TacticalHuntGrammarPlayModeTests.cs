using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    public sealed class TacticalHuntGrammarPlayModeTests
    {
        private const string CategoryName = "TacticalHuntGrammar";

        [Test]
        [Category(CategoryName)]
        public void SharedFixture_DescribesCanonicalIntentsAndProfiles()
        {
            var fixture = LoadFixture();
            var rulesType = FindType("ToilRelic.Unity.Systems.TacticalCombatRules");
            var kindType = FindType("ToilRelic.Unity.Systems.EnemyIntentKind");
            var describe = rulesType.GetMethod("Describe", BindingFlags.Public | BindingFlags.Static);
            var getIntent = rulesType.GetMethod("GetIntent", BindingFlags.Public | BindingFlags.Static);
            var applyPlayer = rulesType.GetMethod("ApplyPlayerAttack", BindingFlags.Public | BindingFlags.Static);
            var applyEnemy = rulesType.GetMethod("ApplyEnemyAttack", BindingFlags.Public | BindingFlags.Static);

            Assert.That(fixture.schemaVersion, Is.EqualTo(1));
            foreach (var expected in fixture.intents)
            {
                var kind = Enum.Parse(kindType, expected.kind);
                var actual = describe.Invoke(null, new[] { kind });
                Assert.That(GetProperty(actual, "Label"), Is.EqualTo(expected.label));
                Assert.That(GetProperty(actual, "Marker"), Is.EqualTo(expected.marker));
                Assert.That(GetProperty(actual, "Cue"), Is.EqualTo(expected.cue));
                Assert.That((int)applyPlayer.Invoke(null, new[] { (object)10, actual }) - 10,
                    Is.EqualTo(expected.playerAttackBonus));
                Assert.That((int)applyEnemy.Invoke(null, new[] { (object)10, actual, false }) - 10,
                    Is.EqualTo(expected.enemyDamageBonus));
                Assert.That(
                    (int)applyEnemy.Invoke(null, new[] { (object)10, actual, false }) -
                    (int)applyEnemy.Invoke(null, new[] { (object)10, actual, true }),
                    Is.EqualTo(expected.defendReduction));
            }

            foreach (var profile in fixture.profiles)
            {
                for (var turn = 0; turn < profile.sequence.Length; turn++)
                {
                    var intent = getIntent.Invoke(null, new object[] { profile.enemyId, turn });
                    Assert.That(GetProperty(intent, "Kind").ToString(), Is.EqualTo(profile.sequence[turn]));
                }
            }
        }

        private static TacticalHuntGrammarFixture LoadFixture()
        {
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures", "TacticalHuntGrammarContracts.json");
            return JsonUtility.FromJson<TacticalHuntGrammarFixture>(File.ReadAllText(path));
        }

        private static object GetProperty(object instance, string name) => instance.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(instance);

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName)).First(type => type != null);

        [Serializable]
        private sealed class TacticalHuntGrammarFixture
        {
            public int schemaVersion;
            public TacticalIntentFixture[] intents;
            public TacticalProfileFixture[] profiles;
        }

        [Serializable]
        private sealed class TacticalIntentFixture
        {
            public string kind;
            public string label;
            public string marker;
            public string cue;
            public int playerAttackBonus;
            public int enemyDamageBonus;
            public int defendReduction;
        }

        [Serializable]
        private sealed class TacticalProfileFixture
        {
            public string enemyId;
            public string[] sequence;
        }
    }
}
