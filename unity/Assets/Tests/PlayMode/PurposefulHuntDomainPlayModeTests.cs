using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    public sealed class PurposefulHuntDomainPlayModeTests
    {
        private readonly List<UnityEngine.Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in created)
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            created.Clear();
        }

        [Test]
        public void ContentVectors_MatchNativeUnityGraphAndRejectForbiddenRewardWeapon()
        {
            var fixture = PurposefulHuntContractFixture.Load().content;
            var enemyDatabase = Create("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            foreach (var quarry in fixture.quarries)
            {
                var enemy = Create("ToilRelic.Unity.Data.EnemyData");
                SetField(enemy, "id", quarry.enemyId);
                SetField(enemy, "displayName", quarry.id == "mine-vermin" ? "Mine Vermin" :
                    quarry.id == "rust-golem" ? "Rust Golem" : "Ruin Wraith");
                SetField(enemy, "equipmentDropProfileId", quarry.profileId);
                enemyList.Add(enemy);
            }

            var profileDatabase = Create("ToilRelic.Unity.Data.EquipmentDropProfileDatabase");
            var profileList = CreateList(profileDatabase, "profiles");
            foreach (var expected in fixture.profiles)
            {
                var profile = Create("ToilRelic.Unity.Data.EquipmentDropProfileData");
                SetField(profile, "id", expected.id);
                SetField(profile, "equipmentId", expected.equipmentId);
                SetField(profile, "chance", expected.chance);
                profileList.Add(profile);
            }

            var contract = Create("ToilRelic.Unity.Data.HuntContractData");
            SetField(contract, "projectId", fixture.projectId);
            SetField(contract, "displayName", "First Relic Project");
            SetField(contract, "relicEquipmentId", fixture.relicEquipmentId);
            var quarryList = CreateList(contract, "quarries");
            var quarryType = FindType("ToilRelic.Unity.Data.HuntQuarryData");
            var dangerType = FindType("ToilRelic.Unity.Data.HuntDanger");
            foreach (var expected in fixture.quarries)
            {
                var quarry = Activator.CreateInstance(quarryType);
                SetField(quarry, "id", expected.id);
                SetField(quarry, "enemyId", expected.enemyId);
                SetField(quarry, "danger", Enum.Parse(dangerType, expected.danger));
                SetField(quarry, "contributionId", expected.contributionId);
                SetField(quarry, "contributionDisplayName", expected.contributionName);
                SetField(quarry, "profileId", expected.profileId);
                quarryList.Add(quarry);
            }

            var validate = contract.GetType().GetMethod("Validate");
            var result = validate.Invoke(contract, new[] { enemyDatabase, profileDatabase });
            Assert.That(GetProperty(result, "IsAvailable"), Is.EqualTo(true));
            Assert.That(profileList.Cast<object>().Select(profile => (float)GetField(profile, "chance")),
                Is.All.EqualTo(0.35f).Within(0.0001f));

            var catalog = FindType("ToilRelic.Unity.Core.EquipmentCatalog");
            var all = (IEnumerable)catalog.GetProperty("All").GetValue(null);
            Assert.That(all.Cast<object>().Count(), Is.EqualTo(6));
            foreach (var equipment in fixture.equipment)
            {
                var arguments = new object[] { equipment.id, null };
                Assert.That(catalog.GetMethod("TryGet").Invoke(null, arguments), Is.EqualTo(true));
                Assert.That(GetProperty(arguments[1], "DisplayName"), Is.EqualTo(equipment.displayName));
                Assert.That(GetProperty(arguments[1], "Category").ToString(), Is.EqualTo(equipment.category));
            }

            SetField(profileList[0], "equipmentId", "reward-weapon");
            result = validate.Invoke(contract, new[] { enemyDatabase, profileDatabase });
            Assert.That(GetProperty(result, "Issue").ToString(), Is.EqualTo("ForbiddenProfileEquipment"));
        }

        [Test]
        public void EnemyDatabase_UsesStableIdWithoutRandomFallback()
        {
            var enemyDatabase = Create("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            var enemy = Create("ToilRelic.Unity.Data.EnemyData");
            SetField(enemy, "id", "rust-golem");
            SetField(enemy, "displayName", "Rust Golem");
            SetField(enemy, "equipmentDropProfileId", "profile-rust-golem");
            enemyList.Add(enemy);

            var tryGet = enemyDatabase.GetType().GetMethod("TryGet");
            var found = new object[] { "rust-golem", null };
            Assert.That(tryGet.Invoke(enemyDatabase, found), Is.EqualTo(true));
            Assert.That(GetField(found[1], "displayName"), Is.EqualTo("Rust Golem"));
            var missing = new object[] { "unknown", null };
            Assert.That(tryGet.Invoke(enemyDatabase, missing), Is.EqualTo(false));
            Assert.That(missing[1], Is.Null);
        }

        private ScriptableObject Create(string typeName)
        {
            var instance = ScriptableObject.CreateInstance(FindType(typeName));
            created.Add(instance);
            return instance;
        }

        private static IList CreateList(object owner, string fieldName)
        {
            var field = owner.GetType().GetField(fieldName);
            var list = (IList)Activator.CreateInstance(field.FieldType);
            field.SetValue(owner, list);
            return list;
        }

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException($"Type not found: {fullName}");

        private static void SetField(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(instance, value);

        private static object GetField(object instance, string name) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);

        private static object GetProperty(object instance, string name) =>
            instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);
    }
}
