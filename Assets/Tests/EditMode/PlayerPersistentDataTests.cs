using NUnit.Framework;
using TurnBasedRpg.Game;
using UnityEngine;

namespace TurnBasedRpg.Tests
{
    public class PlayerPersistentDataTests
    {
        GameObject prefab;
        PlayerPersistentData data;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("Hero");
            prefab.AddComponent<Character>().MaxHP = 50;
            data = ScriptableObject.CreateInstance<PlayerPersistentData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void ResetCharacters_RestoresFullHealthAndRevives()
        {
            var hero = new PlayerPersistentCharacter { CharacterPrefab = prefab, Health = 3, IsDead = true };
            data.Characters = new[] { hero };

            data.ResetCharacters();

            Assert.That(hero.Health, Is.EqualTo(50));
            Assert.That(hero.IsDead, Is.False);
        }

        [Test]
        public void ResetCharacters_SkipsSlotsWithoutAPrefab()
        {
            // The inspector adds empty slots, and OnValidate resets on every edit.
            var empty = new PlayerPersistentCharacter { Health = 7, IsDead = true };
            var hero = new PlayerPersistentCharacter { CharacterPrefab = prefab, Health = 3, IsDead = true };
            data.Characters = new[] { empty, hero };

            Assert.DoesNotThrow(() => data.ResetCharacters());
            Assert.That(empty.Health, Is.EqualTo(7));
            Assert.That(empty.IsDead, Is.True);
            Assert.That(hero.Health, Is.EqualTo(50));
            Assert.That(hero.IsDead, Is.False);
        }

        [Test]
        public void ResetCharacters_WithoutCharacters_DoesNothing()
        {
            data.Characters = null;

            Assert.DoesNotThrow(() => data.ResetCharacters());
            Assert.That(data.Characters, Is.Null);
        }
    }
}
