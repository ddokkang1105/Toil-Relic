using ToilRelic.Unity.Data;
using ToilRelic.Unity.Save;
using ToilRelic.Unity.Systems;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToilRelic.Unity.Core
{
    public enum EquipmentCommandStatus
    {
        Applied,
        NotInCamp,
        ComparisonRejected,
        UnequipRejected,
        MutationRejected
    }

    public enum EquipmentCommandKind
    {
        Equip,
        Unequip
    }

    public sealed class EquipmentCommandOutcome
    {
        public EquipmentCommandKind Command { get; }
        public EquipmentCommandStatus Status { get; }
        public EquipmentCommandStatus Reason => Status;
        public bool Applied => Status == EquipmentCommandStatus.Applied;
        public EquipmentComparisonReason ComparisonReason { get; }
        public UnequipEligibilityStatus UnequipStatus { get; }
        public EquipmentComparisonResult Comparison { get; }
        public UnequipEligibilityResult UnequipEligibility { get; }

        private EquipmentCommandOutcome(
            EquipmentCommandKind command,
            EquipmentCommandStatus status,
            EquipmentComparisonReason comparisonReason,
            UnequipEligibilityStatus unequipStatus,
            EquipmentComparisonResult comparison,
            UnequipEligibilityResult unequipEligibility)
        {
            Command = command;
            Status = status;
            ComparisonReason = comparisonReason;
            UnequipStatus = unequipStatus;
            Comparison = comparison;
            UnequipEligibility = unequipEligibility;
        }

        internal static EquipmentCommandOutcome Equip(
            EquipmentCommandStatus status,
            EquipmentComparisonResult comparison = null) =>
            new(
                EquipmentCommandKind.Equip,
                status,
                comparison?.Reason ?? EquipmentComparisonReason.None,
                default,
                comparison,
                null);

        internal static EquipmentCommandOutcome Unequip(
            EquipmentCommandStatus status,
            UnequipEligibilityResult eligibility = null) =>
            new(
                EquipmentCommandKind.Unequip,
                status,
                default,
                eligibility?.Status ?? default,
                null,
                eligibility);
    }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyDatabase enemyDatabase;
        [SerializeField] private DropTableData dropTable;
        [SerializeField] private HuntContractData huntContract;
        [SerializeField] private EquipmentDropProfileDatabase equipmentDropProfiles;

        [Header("State")]
        [SerializeField] private PlayerState player = new();

        private readonly CombatSystem combat = new();
        private readonly LootSystem loot = new();
        private readonly CraftingSystem crafting = new();

        private EnemyRuntime currentEnemy;
        private ConfirmedQuarryReward confirmedQuarryReward;
        private string currentQuarryId;
        private string currentContributionName;
        private HuntContractSnapshot presentedHuntContract;
        private GameState state = GameState.Title;
        private BattlePhase battlePhase = BattlePhase.None;
        private SaveLoadStatus saveLoadStatus;
        private bool recoveryNoticePending;

        public BattlePhase CurrentBattlePhase => battlePhase;
        public GameState CurrentState => state;
        public bool HasSavedGame => saveLoadStatus is SaveLoadStatus.Loaded or SaveLoadStatus.Recovered;
        public SaveLoadStatus CurrentSaveLoadStatus => saveLoadStatus;
        public bool RecoveryNoticePending => recoveryNoticePending;
        public PlayerState Player => player;
        public string CurrentQuarryId => currentQuarryId;
        public HuntContractSnapshot PresentedHuntContract => presentedHuntContract;

        private void Awake()
        {
            var loadResult = SaveService.Load();
            saveLoadStatus = loadResult.Status;
            recoveryNoticePending = loadResult.RecoveryNoticePending;
            if (loadResult.Status is SaveLoadStatus.Loaded or SaveLoadStatus.Recovered)
            {
                player = loadResult.Player;
                player.InitDefaults();
            }
            else
            {
                player.InitDefaults();
            }

            if (loadResult.Status == SaveLoadStatus.Recovered)
            {
                Debug.Log("Recovered a previous valid save; gameplay can continue.");
            }

            if (!string.IsNullOrEmpty(loadResult.Diagnostic) && loadResult.Status == SaveLoadStatus.Recovered)
            {
                Debug.LogWarning($"Save recovery diagnostic. {loadResult.Diagnostic}");
            }
            else if (!string.IsNullOrEmpty(loadResult.Diagnostic))
            {
                Debug.LogError($"Save load failed. {loadResult.Diagnostic}");
            }
        }

        private void Start()
        {
            ChangeState(GameState.Title);
            PublishPlayer();
            GameEvents.RaiseRecoveryNoticeChanged(recoveryNoticePending);
            GameEvents.RaiseBattleLog(GetTitleSaveMessage());
        }

        public void ContinueGame()
        {
            if (state != GameState.Title || !HasSavedGame)
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

            var deleteResult = SaveService.Delete();
            if (!deleteResult.Succeeded)
            {
                Debug.LogError($"Save delete failed. {deleteResult.Diagnostic}");
                ReconcileSaveAvailabilityAfterDeleteFailure();
                ChangeState(GameState.Title);
                GameEvents.RaiseBattleLog(GetTitleSaveMessage());
                return;
            }

            if (recoveryNoticePending)
            {
                recoveryNoticePending = false;
                GameEvents.RaiseRecoveryNoticeChanged(false);
            }

            player = new PlayerState();
            player.InitDefaults();
            saveLoadStatus = SaveLoadStatus.Missing;
            EnterCamp("A new expedition begins. Hunt, craft, and survive.");
        }

        private void ReconcileSaveAvailabilityAfterDeleteFailure()
        {
            var loadResult = SaveService.Load();
            saveLoadStatus = loadResult.Status;
            if (loadResult.Status is SaveLoadStatus.Loaded or SaveLoadStatus.Recovered)
            {
                player = loadResult.Player;
                player.InitDefaults();
            }

            if (!recoveryNoticePending && loadResult.RecoveryNoticePending)
            {
                recoveryNoticePending = true;
                GameEvents.RaiseRecoveryNoticeChanged(true);
            }
        }

        public void StartHunt()
        {
            if (state != GameState.Camp)
            {
                return;
            }

            if (!TryBuildHuntSnapshot(out var snapshot, out var diagnostic))
            {
                GameEvents.RaiseBattleLog($"Hunt Contract unavailable. {diagnostic}");
                return;
            }

            presentedHuntContract = snapshot;
            GameEvents.RaiseHuntContractPresented(snapshot);
            GameEvents.RaiseBattleLog("Choose a quarry. Profile equipment is optional; first-win project progress is guaranteed.");
        }

        public bool ConfirmHunt(string quarryId, string revision)
        {
            var presentedSnapshot = presentedHuntContract;
            HuntContractSnapshot currentSnapshot = null;
            string diagnostic = null;
            if (state != GameState.Camp || string.IsNullOrEmpty(quarryId) ||
                presentedSnapshot == null ||
                !string.Equals(presentedSnapshot.Revision, revision, StringComparison.Ordinal) ||
                !presentedSnapshot.Quarries.Any(quarry =>
                    string.Equals(quarry.Id, quarryId, StringComparison.Ordinal)) ||
                !TryBuildHuntSnapshot(out currentSnapshot, out diagnostic) ||
                !string.Equals(currentSnapshot.Revision, revision, StringComparison.Ordinal) ||
                !huntContract.TryGetQuarry(quarryId, out var quarry) ||
                !enemyDatabase.TryGet(quarry.enemyId, out var enemyData) ||
                !equipmentDropProfiles.TryGet(quarry.profileId, out var profile))
            {
                GameEvents.RaiseBattleLog($"Hunt confirmation rejected. {diagnostic ?? "The Contract changed; reopen it."}");
                return false;
            }

            confirmedQuarryReward = new ConfirmedQuarryReward(
                quarry.id, quarry.contributionId, profile.equipmentId, profile.chance);
            currentQuarryId = quarry.id;
            currentContributionName = quarry.contributionDisplayName;
            currentEnemy = new EnemyRuntime(enemyData);
            presentedHuntContract = null;
            GameEvents.RaiseHuntContractClosed();
            ChangeState(GameState.Battle);
            ChangeBattlePhase(BattlePhase.PlayerAction);
            PublishEnemy();
            GameEvents.RaiseBattleLog($"Confirmed quarry: {currentEnemy.Name}. The selected target and rewards are locked for this battle.");
            return true;
        }

        public void CancelHunt()
        {
            if (state != GameState.Camp) return;
            presentedHuntContract = null;
            GameEvents.RaiseHuntContractClosed();
            GameEvents.RaiseBattleLog("Hunt Contract closed. No quarry was selected.");
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
                ClearConfirmedQuarry();
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

        public void EquipStarterWeapon() => EquipEquipment(EquipmentSlot.PrimaryWeapon, EquipmentCatalog.StarterWeaponId);

        public ForgeOutcome ForgeRelic()
        {
            if (state != GameState.Camp)
            {
                return new ForgeOutcome(ForgeStatus.Rejected, false);
            }

            var outcome = RelicForgeSystem.Forge(player, huntContract, enemyDatabase, equipmentDropProfiles);
            switch (outcome.Status)
            {
                case ForgeStatus.Forged:
                    PublishPlayer();
                    GameEvents.RaiseBattleLog("Toilbound Relic forged. It is owned and remains unequipped until confirmed.");
                    if (SaveProgress())
                    {
                        GameEvents.RaiseEquipmentFocusRequested(EquipmentSlot.Necklace, outcome.EquipmentId);
                    }
                    break;
                case ForgeStatus.NotReady:
                    GameEvents.RaiseBattleLog($"Forge unavailable: {player.RelicProject.CompletedContributionIds.Count}/3 contributions secured.");
                    break;
                case ForgeStatus.AlreadyForged:
                    GameEvents.RaiseBattleLog("Toilbound Relic was already forged; nothing was granted.");
                    break;
                default:
                    GameEvents.RaiseBattleLog("Forge rejected because project state or Hunt content is invalid.");
                    break;
            }

            return outcome;
        }

        public EquipmentCommandOutcome EquipEquipment(EquipmentSlot slot, string equipmentId)
        {
            if (state != GameState.Camp)
            {
                return EquipmentCommandOutcome.Equip(EquipmentCommandStatus.NotInCamp);
            }

            var comparison = EquipmentComparisonEvaluator.Compare(player, slot, equipmentId);
            if (!comparison.CanCommit)
            {
                return EquipmentCommandOutcome.Equip(EquipmentCommandStatus.ComparisonRejected, comparison);
            }

            if (!player.Equip(slot, equipmentId))
            {
                return EquipmentCommandOutcome.Equip(EquipmentCommandStatus.MutationRejected, comparison);
            }

            PublishPlayer();
            GameEvents.RaiseBattleLog($"Equipped {comparison.Candidate.DisplayName}.");
            SaveProgress();
            return EquipmentCommandOutcome.Equip(EquipmentCommandStatus.Applied, comparison);
        }

        public EquipmentCommandOutcome UnequipEquipment(EquipmentSlot slot)
        {
            if (state != GameState.Camp)
            {
                return EquipmentCommandOutcome.Unequip(EquipmentCommandStatus.NotInCamp);
            }

            var eligibility = EquipmentComparisonEvaluator.EvaluateUnequip(player, slot);
            if (!eligibility.CanCommit)
            {
                return EquipmentCommandOutcome.Unequip(EquipmentCommandStatus.UnequipRejected, eligibility);
            }

            if (!player.Unequip(slot))
            {
                return EquipmentCommandOutcome.Unequip(EquipmentCommandStatus.MutationRejected, eligibility);
            }

            PublishPlayer();
            GameEvents.RaiseBattleLog($"Unequipped {eligibility.Current.DisplayName}.");
            SaveProgress();
            return EquipmentCommandOutcome.Unequip(EquipmentCommandStatus.Applied, eligibility);
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
                ClearConfirmedQuarry();
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
            var reward = QuarryRewardSystem.Resolve(player, confirmedQuarryReward,
                new QuarryVictoryCommand(currentQuarryId, true, UnityEngine.Random.value, expReward,
                    rolled.Junk, rolled.RelicPart, rolled.HealingPotion));
            if (reward.Status != QuarryRewardStatus.Applied)
            {
                const string failure = "Victory reward rejected. No loot, EXP, profile equipment, or project progress changed.";
                GameEvents.RaiseBattleLog(failure);
                GameEvents.RaiseBattleOutcome(failure);
                currentEnemy = null;
                ClearConfirmedQuarry();
                PublishEnemy();
                ChangeState(GameState.Camp);
                ChangeBattlePhase(BattlePhase.None);
                return;
            }

            var outcome = $"Win. Loot: {BuildLootLog(rolled.Junk, rolled.RelicPart, rolled.HealingPotion, expReward)}. " +
                BuildRewardFacts(reward);
            string levelUpMessage = null;
            if (reward.LevelUp is { LeveledUp: true } levelResult)
            {
                levelUpMessage = $"Level up! +{levelResult.LevelsGained} -> Lv.{levelResult.NewLevel}. HP fully restored.";
            }
            currentEnemy = null;
            ClearConfirmedQuarry();
            PublishEnemy();
            ChangeState(GameState.Camp);
            ChangeBattlePhase(BattlePhase.None);
            PublishPlayer();
            GameEvents.RaiseBattleLog(outcome);
            GameEvents.RaiseBattleOutcome(outcome);
            if (levelUpMessage != null)
            {
                GameEvents.RaiseLevelUp(levelUpMessage);
            }
            SaveProgress();
        }

        private void PublishPlayer()
        {
            GameEvents.RaisePlayerChanged(player);
            GameEvents.RaiseRelicProjectChanged(new RelicProjectSnapshot(
                player.RelicProject.CompletedContributionIds.Count,
                3,
                player.RelicProject.IsReady,
                player.RelicProject.IsForged));
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

        private bool SaveProgress()
        {
            var saveResult = SaveService.Save(player);
            if (!saveResult.Succeeded)
            {
                Debug.LogError($"Save write failed. {saveResult.Diagnostic}");
                GameEvents.RaiseSaveStatusChanged(SaveFeedbackStatus.Failed);
                return false;
            }

            saveLoadStatus = SaveLoadStatus.Loaded;
            if (recoveryNoticePending)
            {
                recoveryNoticePending = false;
                GameEvents.RaiseRecoveryNoticeChanged(false);
            }
            GameEvents.RaiseSaveStatusChanged(SaveFeedbackStatus.Succeeded);
            return true;
        }

        private string GetTitleSaveMessage()
        {
            return saveLoadStatus switch
            {
                SaveLoadStatus.Loaded or SaveLoadStatus.Recovered => "Save found. Continue or start a new game.",
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

        private static string BuildLootLog(int junk, int relicPart, int healingPotion, int expReward)
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

            return parts.Count > 0 ? string.Join(", ", parts) : "no loot";
        }

        private string BuildRewardFacts(QuarryRewardOutcome reward)
        {
            EquipmentCatalog.TryGet(reward.ProfileEquipmentId, out var equipment);
            var profileFact = reward.ProfileResult switch
            {
                ProfileRewardResult.Granted => $"Profile equipment acquired: {equipment.DisplayName}.",
                ProfileRewardResult.AlreadyOwned => $"Profile equipment already owned: {equipment.DisplayName}.",
                _ => $"Profile reward missed: {equipment.DisplayName}."
            };
            var contributionFact = reward.ContributionResult == ContributionRewardResult.Granted
                ? $"Project contribution acquired: {currentContributionName}."
                : $"Replay complete: {currentContributionName} was already secured; no additional project progress.";
            return $"{profileFact} {contributionFact}{(reward.ProjectReady ? " Ready to forge." : string.Empty)}";
        }

        private bool TryBuildHuntSnapshot(out HuntContractSnapshot snapshot, out string diagnostic)
        {
            snapshot = null;
            diagnostic = null;
            if (huntContract == null || enemyDatabase == null || equipmentDropProfiles == null || dropTable == null)
            {
                diagnostic = "Assign the Contract, enemy database, reward profiles, and drop table.";
                return false;
            }

            var validation = huntContract.Validate(enemyDatabase, equipmentDropProfiles);
            if (!validation.IsAvailable)
            {
                diagnostic = $"Content issue: {validation.Issue} ({validation.ContentId ?? "unknown"}).";
                return false;
            }

            var quarries = new List<HuntQuarrySnapshot>();
            var revisionParts = new List<string> { huntContract.projectId, huntContract.relicEquipmentId };
            foreach (var quarry in huntContract.quarries)
            {
                enemyDatabase.TryGet(quarry.enemyId, out var enemy);
                equipmentDropProfiles.TryGet(quarry.profileId, out var profile);
                EquipmentCatalog.TryGet(profile.equipmentId, out var equipment);
                revisionParts.Add($"{quarry.id}:{enemy.id}:{profile.id}:{profile.equipmentId}:{profile.chance:0.####}:{quarry.contributionId}");
                quarries.Add(new HuntQuarrySnapshot(
                    quarry.id,
                    enemy.id,
                    enemy.displayName,
                    quarry.danger.ToString(),
                    profile.equipmentId,
                    equipment.DisplayName,
                    (int)Math.Round(profile.chance * 100f),
                    quarry.contributionId,
                    quarry.contributionDisplayName,
                    player.RelicProject.CompletedContributionIds.Contains(quarry.contributionId)));
            }

            snapshot = new HuntContractSnapshot(
                huntContract.projectId,
                huntContract.displayName,
                string.Join("|", revisionParts),
                quarries);
            return true;
        }

        private void ClearConfirmedQuarry()
        {
            confirmedQuarryReward = null;
            currentQuarryId = null;
            currentContributionName = null;
        }
    }
}
