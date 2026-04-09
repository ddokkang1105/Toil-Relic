using ToilRelic.Unity.Data;
using ToilRelic.Unity.Save;
using ToilRelic.Unity.Systems;
using UnityEngine;

namespace ToilRelic.Unity.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyDatabase enemyDatabase;
        [SerializeField] private DropTableData dropTable;

        [Header("State")]
        [SerializeField] private PlayerState player = new();

        private readonly CombatSystem combat = new();
        private readonly LootSystem loot = new();
        private readonly CraftingSystem crafting = new();

        private EnemyRuntime currentEnemy;
        private GameState state = GameState.Title;

        private void Start()
        {
            if (!SaveService.TryLoad(out var loaded))
            {
                player.InitDefaults();
            }
            else
            {
                player = loaded;
                player.InitDefaults();
            }

            ChangeState(GameState.Camp);
            PublishPlayer();
            GameEvents.RaiseBattleLog("Ready. Hunt, craft, and survive.");
        }

        public void StartHunt()
        {
            if (state != GameState.Camp)
            {
                return;
            }

            var enemyData = enemyDatabase != null ? enemyDatabase.GetRandom() : null;
            if (enemyData == null)
            {
                GameEvents.RaiseBattleLog("No enemy data found. Create EnemyDatabase and assign entries.");
                return;
            }

            currentEnemy = new EnemyRuntime(enemyData);
            ChangeState(GameState.Battle);
            PublishEnemy();
            GameEvents.RaiseBattleLog($"A wild {currentEnemy.Name} appears.");
        }

        public void Attack()
        {
            if (state != GameState.Battle || currentEnemy == null)
            {
                return;
            }

            var playerDamage = combat.RollPlayerAttack();
            currentEnemy.TakeDamage(playerDamage);
            GameEvents.RaiseBattleLog($"You hit {currentEnemy.Name} for {playerDamage}.");
            PublishEnemy();

            if (!currentEnemy.IsAlive)
            {
                ResolveVictory();
                return;
            }

            ResolveEnemyTurn(playerDefending: false);
        }

        public void Defend()
        {
            if (state != GameState.Battle || currentEnemy == null)
            {
                return;
            }

            GameEvents.RaiseBattleLog("You brace for impact.");
            ResolveEnemyTurn(playerDefending: true);
        }

        public void Flee()
        {
            if (state != GameState.Battle)
            {
                return;
            }

            if (combat.TryFlee())
            {
                GameEvents.RaiseBattleLog("Escape successful.");
                currentEnemy = null;
                ChangeState(GameState.Camp);
                return;
            }

            GameEvents.RaiseBattleLog("Escape failed.");
            ResolveEnemyTurn(playerDefending: false);
        }

        public void UsePotion()
        {
            if (state != GameState.Battle || currentEnemy == null)
            {
                return;
            }

            if (player.Hp >= player.MaxHp)
            {
                GameEvents.RaiseBattleLog("HP is already full.");
                return;
            }

            if (!player.Consume(ItemType.HealingPotion, 1))
            {
                GameEvents.RaiseBattleLog("No healing potion in inventory.");
                return;
            }

            var hpBeforeHeal = player.Hp;
            player.Heal(12);
            var healed = player.Hp - hpBeforeHeal;
            GameEvents.RaiseBattleLog($"You used a healing potion and recovered {healed} HP.");
            PublishPlayer();
            ResolveEnemyTurn(playerDefending: false);
        }

        public void Rest()
        {
            if (state != GameState.Camp)
            {
                return;
            }

            player.HealAll();
            PublishPlayer();
            SaveService.Save(player);
            GameEvents.RaiseBattleLog("You rest and recover to full HP.");
        }

        public void CraftTreasure()
        {
            if (state != GameState.Camp)
            {
                return;
            }

            var result = crafting.TryCraftTreasure(player);
            GameEvents.RaiseBattleLog(result.Message);
            PublishPlayer();
            SaveService.Save(player);
        }

        private void ResolveEnemyTurn(bool playerDefending)
        {
            var enemyDamage = combat.RollEnemyAttack(currentEnemy, playerDefending);
            player.TakeDamage(enemyDamage);
            GameEvents.RaiseBattleLog($"{currentEnemy.Name} hits you for {enemyDamage}.");
            PublishPlayer();

            if (!player.IsAlive)
            {
                GameEvents.RaiseBattleLog("You collapsed. Auto-rest and return to camp.");
                player.HealAll();
                currentEnemy = null;
                ChangeState(GameState.Camp);
                PublishPlayer();
                SaveService.Save(player);
            }
        }

        private void ResolveVictory()
        {
            var expReward = currentEnemy.ExpReward;
            var rolled = loot.Roll(dropTable);
            player.Add(ItemType.Junk, rolled.Junk);
            player.Add(ItemType.RelicPart, rolled.RelicPart);
            player.Add(ItemType.HealingPotion, rolled.HealingPotion);
            var levelResult = player.GainExperience(expReward);

            GameEvents.RaiseBattleLog($"Win. Loot: junk +{rolled.Junk}, relic part +{rolled.RelicPart}, healing potion +{rolled.HealingPotion}, EXP +{expReward}.");
            if (levelResult.LeveledUp)
            {
                GameEvents.RaiseBattleLog($"Level up! +{levelResult.LevelsGained} -> Lv.{levelResult.NewLevel}. HP fully restored.");
            }
            currentEnemy = null;
            ChangeState(GameState.Camp);
            PublishPlayer();
            SaveService.Save(player);
        }

        private void PublishPlayer()
        {
            GameEvents.RaisePlayerChanged(player);
        }

        private void PublishEnemy()
        {
            if (currentEnemy == null)
            {
                GameEvents.RaiseEnemyChanged("None", 0, 0);
                return;
            }

            GameEvents.RaiseEnemyChanged(currentEnemy.Name, currentEnemy.Hp, currentEnemy.MaxHp);
        }

        private void ChangeState(GameState next)
        {
            state = next;
            GameEvents.RaiseStateChanged(state);
        }
    }
}
