using ToilRelic.Unity.Data;
using ToilRelic.Unity.Save;
using ToilRelic.Unity.Systems;
using UnityEngine;
using System.Collections.Generic;

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
        private BattlePhase battlePhase = BattlePhase.None;
        private bool hasSavedGame;
        private SaveLoadStatus saveLoadStatus;

        public BattlePhase CurrentBattlePhase => battlePhase;
        public GameState CurrentState => state;
        public bool HasSavedGame => hasSavedGame;
        public SaveLoadStatus CurrentSaveLoadStatus => saveLoadStatus;

        private void Awake()
        {
            var loadResult = SaveService.Load();
            saveLoadStatus = loadResult.Status;
            if (loadResult.Status == SaveLoadStatus.Loaded)
            {
                player = loadResult.Player;
                player.InitDefaults();
                hasSavedGame = true;
            }
            else
            {
                player.InitDefaults();
            }

            if (!string.IsNullOrEmpty(loadResult.Diagnostic))
            {
                Debug.LogError($"Save load failed. {loadResult.Diagnostic}");
            }
        }

        private void Start()
        {
            ChangeState(GameState.Title);
            PublishPlayer();
            GameEvents.RaiseBattleLog(GetTitleSaveMessage());
        }

        public void ContinueGame()
        {
            if (state != GameState.Title || !hasSavedGame)
            {
                return;
            }

            EnterCamp("Save loaded. Hunt, craft, and survive.");
        }

        public void StartNewGame()
        {
            if (state != GameState.Title)
            {
                return;
            }

            if (saveLoadStatus != SaveLoadStatus.Missing)
            {
                var deleteResult = SaveService.Delete();
                if (!deleteResult.Succeeded)
                {
                    Debug.LogError($"Save delete failed. {deleteResult.Diagnostic}");
                    GameEvents.RaiseBattleLog(GetTitleSaveMessage());
                    return;
                }
            }

            player = new PlayerState();
            player.InitDefaults();
            hasSavedGame = false;
            saveLoadStatus = SaveLoadStatus.Missing;
            EnterCamp("A new expedition begins. Hunt, craft, and survive.");
        }

        public void StartHunt()
        {
            if (state != GameState.Camp)
            {
                return;
            }

            var enemyData = enemyDatabase != null ? enemyDatabase.GetRandom() : null;
            if (enemyData == null || dropTable == null)
            {
                GameEvents.RaiseBattleLog("Assign an enemy database with entries and a drop table before hunting.");
                return;
            }

            currentEnemy = new EnemyRuntime(enemyData);
            ChangeState(GameState.Battle);
            ChangeBattlePhase(BattlePhase.PlayerAction);
            PublishEnemy();
            GameEvents.RaiseBattleLog($"A wild {currentEnemy.Name} appears.");
        }

        public void Attack()
        {
            if (!CanTakePlayerAction())
            {
                return;
            }

            var playerDamage = combat.RollPlayerAttack(player.AttackBonus);
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
            if (!CanTakePlayerAction())
            {
                return;
            }

            GameEvents.RaiseBattleLog("You brace for impact.");
            ResolveEnemyTurn(playerDefending: true);
        }

        public void Flee()
        {
            if (!CanTakePlayerAction())
            {
                return;
            }

            if (combat.TryFlee())
            {
                const string outcome = "Escape successful.";
                GameEvents.RaiseBattleLog(outcome);
                GameEvents.RaiseBattleOutcome(outcome);
                currentEnemy = null;
                PublishEnemy();
                ChangeState(GameState.Camp);
                ChangeBattlePhase(BattlePhase.None);
                return;
            }

            GameEvents.RaiseBattleLog("Escape failed.");
            ResolveEnemyTurn(playerDefending: false);
        }

        public void UsePotion()
        {
            if (!CanTakePlayerAction())
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
            GameEvents.RaiseBattleLog("You rest and recover to full HP.");
            SaveProgress();
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
            SaveProgress();
        }

        public void EquipStarterWeapon() => Equip(EquipmentSlot.PrimaryWeapon, EquipmentCatalog.StarterWeaponId);
        public void EquipRewardWeapon() => Equip(EquipmentSlot.PrimaryWeapon, EquipmentCatalog.RewardWeaponId);
        public void EquipEquipment(EquipmentSlot slot, string equipmentId) => Equip(slot, equipmentId);
        public void UnequipEquipment(EquipmentSlot slot)
        {
            if (state != GameState.Camp || !player.Unequip(slot)) return;
            PublishPlayer();
            SaveProgress();
        }

        private void ResolveEnemyTurn(bool playerDefending)
        {
            ChangeBattlePhase(BattlePhase.EnemyAction);
            var enemyDamage = combat.RollEnemyAttack(currentEnemy, playerDefending);
            player.TakeDamage(enemyDamage);
            GameEvents.RaiseBattleLog($"{currentEnemy.Name} hits you for {enemyDamage}.");
            PublishPlayer();

            if (!player.IsAlive)
            {
                const string outcome = "You collapsed. Auto-rest and return to camp.";
                GameEvents.RaiseBattleLog(outcome);
                GameEvents.RaiseBattleOutcome(outcome);
                player.HealAll();
                currentEnemy = null;
                PublishEnemy();
                ChangeState(GameState.Camp);
                ChangeBattlePhase(BattlePhase.None);
                PublishPlayer();
                SaveProgress();
                return;
            }

            ChangeBattlePhase(BattlePhase.PlayerAction);
        }

        private void ResolveVictory()
        {
            ChangeBattlePhase(BattlePhase.Resolving);
            var expReward = currentEnemy.ExpReward;
            var rolled = loot.Roll(dropTable);
            player.Add(ItemType.Junk, rolled.Junk);
            player.Add(ItemType.RelicPart, rolled.RelicPart);
            player.Add(ItemType.HealingPotion, rolled.HealingPotion);
            var rewardWeaponGranted = player.GrantEquipment(EquipmentCatalog.RewardWeaponId);
            var levelResult = player.GainExperience(expReward);

            var outcome = $"Win. Loot: {BuildLootLog(rolled.Junk, rolled.RelicPart, rolled.HealingPotion, expReward, rewardWeaponGranted)}.";
            GameEvents.RaiseBattleLog(outcome);
            GameEvents.RaiseBattleOutcome(outcome);
            if (levelResult.LeveledUp)
            {
                var levelUpMessage = $"Level up! +{levelResult.LevelsGained} -> Lv.{levelResult.NewLevel}. HP fully restored.";
                GameEvents.RaiseLevelUp(levelUpMessage);
            }
            currentEnemy = null;
            PublishEnemy();
            ChangeState(GameState.Camp);
            ChangeBattlePhase(BattlePhase.None);
            PublishPlayer();
            SaveProgress();
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

        private void ChangeBattlePhase(BattlePhase next)
        {
            battlePhase = next;
            GameEvents.RaiseBattlePhaseChanged(battlePhase);
        }

        private bool CanTakePlayerAction()
        {
            return state == GameState.Battle && currentEnemy != null && battlePhase == BattlePhase.PlayerAction;
        }

        private void SaveProgress()
        {
            var saveResult = SaveService.Save(player);
            if (!saveResult.Succeeded)
            {
                Debug.LogError($"Save write failed. {saveResult.Diagnostic}");
                GameEvents.RaiseSaveFailed("Save failed. Progress may not be saved.");
                return;
            }

            hasSavedGame = true;
            saveLoadStatus = SaveLoadStatus.Loaded;
        }

        private string GetTitleSaveMessage()
        {
            return saveLoadStatus switch
            {
                SaveLoadStatus.Loaded => "Save found. Continue or start a new game.",
                SaveLoadStatus.Unreadable => "Save could not be read. Start New Game to replace it.",
                _ => "Start a new game to begin."
            };
        }

        private void EnterCamp(string message)
        {
            ChangeState(GameState.Camp);
            ChangeBattlePhase(BattlePhase.None);
            PublishPlayer();
            GameEvents.RaiseBattleLog(message);
        }

        private void Equip(EquipmentSlot slot, string equipmentId)
        {
            if (state != GameState.Camp)
            {
                return;
            }

            if (!player.Equip(slot, equipmentId) || !EquipmentCatalog.TryGet(equipmentId, out var equipment))
            {
                GameEvents.RaiseBattleLog("That weapon is not owned.");
                return;
            }

            PublishPlayer();
            GameEvents.RaiseBattleLog($"Equipped {equipment.DisplayName}.");
            SaveProgress();
        }

        private static string BuildLootLog(int junk, int relicPart, int healingPotion, int expReward, bool rewardWeaponGranted)
        {
            var parts = new List<string>();

            if (junk > 0)
            {
                parts.Add($"junk +{junk}");
            }

            if (relicPart > 0)
            {
                parts.Add($"relic part +{relicPart}");
            }

            if (healingPotion > 0)
            {
                parts.Add($"healing potion +{healingPotion}");
            }

            if (expReward > 0)
            {
                parts.Add($"EXP +{expReward}");
            }

            if (rewardWeaponGranted && EquipmentCatalog.TryGet(EquipmentCatalog.RewardWeaponId, out var weapon))
            {
                parts.Add(weapon.DisplayName);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "no loot";
        }
    }
}
