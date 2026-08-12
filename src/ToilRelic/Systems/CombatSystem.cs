using System.Text;
using ToilRelic.Models;
using ToilRelic.Util;

namespace ToilRelic.Systems;

public sealed class CombatSystem
{
    private readonly Random _random;

    public CombatSystem(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public CombatResult Fight(Player player, Enemy enemy)
    {
        var log = new StringBuilder();
        var resolvedTurnIndex = 0;
        var playerFled = false;

        void AppendLog(string message)
        {
            log.AppendLine(message);
            Console.WriteLine(message);
        }

        while (player.IsAlive && enemy.IsAlive)
        {
            var intent = TacticalCombatRules.GetIntent(enemy.Id, resolvedTurnIndex);
            ConsoleUI.Section(
                "Enemy intent",
                $"{intent.DisplayLabel} | {intent.Cue}\n{player.Name} HP {player.Hp} vs {enemy.Name} HP {enemy.Hp}");
            ConsoleUI.Menu("Combat action", new Dictionary<int, string>
            {
                { 1, "Attack" },
                { 2, "Defend" },
                { 3, "Use potion" },
                { 4, "Flee" }
            });

            var action = ConsoleUI.ReadInt("Select", 1, 4);
            var playerDefending = false;
            switch (action)
            {
                case 1:
                    var rolledPlayerDamage = _random.Next(4, 9) + player.AttackBonus;
                    var playerDamage = TacticalCombatRules.ApplyPlayerAttack(rolledPlayerDamage, intent);
                    enemy.TakeDamage(playerDamage);
                    AppendLog($"You hit {enemy.Name} for {playerDamage}.");
                    break;

                case 2:
                    playerDefending = true;
                    AppendLog("You brace for the revealed intent.");
                    break;

                case 3:
                    if (player.Hp >= player.MaxHp)
                    {
                        AppendLog("HP is already full.");
                        continue;
                    }

                    if (!player.Consume(ItemType.HealingPotion, 1))
                    {
                        AppendLog("No healing potion in inventory.");
                        continue;
                    }

                    var hpBeforeHeal = player.Hp;
                    player.Heal(12);
                    AppendLog($"You used a healing potion and recovered {player.Hp - hpBeforeHeal} HP.");
                    break;

                case 4:
                    if (_random.NextDouble() < 0.55d)
                    {
                        AppendLog("Escape successful.");
                        playerFled = true;
                        break;
                    }

                    AppendLog("Escape failed.");
                    break;
            }

            if (playerFled || !enemy.IsAlive)
            {
                break;
            }

            var rolledEnemyDamage = enemy.Attack + _random.Next(0, 3);
            var enemyAttack = TacticalCombatRules.ApplyEnemyAttack(rolledEnemyDamage, intent, playerDefending);
            player.TakeDamage(enemyAttack);
            AppendLog($"{enemy.Name} hits {player.Name} for {enemyAttack}.");

            if (!player.IsAlive)
            {
                AppendLog("You collapsed.");
                break;
            }

            resolvedTurnIndex++;
        }

        var playerWon = player.IsAlive && !enemy.IsAlive;
        return new CombatResult(playerWon, playerFled, false, log.ToString());
    }
}

public sealed record CombatResult(bool PlayerWon, bool PlayerFled, bool TimeExpired, string Log);

public interface IHuntRuntime
{
    CombatResult Fight(Player player, Enemy enemy);
    Loot RollLoot();
    double RollProfile();
}

public sealed class ProductionHuntRuntime : IHuntRuntime
{
    private readonly CombatSystem _combat = new();
    private readonly LootSystem _loot = new();

    public CombatResult Fight(Player player, Enemy enemy) => _combat.Fight(player, enemy);
    public Loot RollLoot() => _loot.RollLoot();
    public double RollProfile() => Random.Shared.NextDouble();
}
