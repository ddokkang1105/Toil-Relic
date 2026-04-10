using System.Text;
using ToilRelic.Models;
using ToilRelic.Util;

namespace ToilRelic.Systems;

public sealed class CombatSystem
{
    private const int CombatDurationSeconds = 8;

    public CombatResult Fight(Player player, Enemy enemy)
    {
        var log = new StringBuilder();
        var startTime = DateTime.UtcNow;

        void AppendLog(string message)
        {
            log.AppendLine(message);
            Console.WriteLine(message);
        }

        while (player.IsAlive && enemy.IsAlive && (DateTime.UtcNow - startTime).TotalSeconds < CombatDurationSeconds)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;
            ConsoleUI.Section("전투", $"진행 시간 {elapsedSeconds}/{CombatDurationSeconds}초 | {player.Name} HP {player.Hp} vs {enemy.Name} HP {enemy.Hp}");

            if (ShouldUseHealingPotion(player) && player.Consume(ItemType.HealingPotion, 1))
            {
                var hpBeforeHeal = player.Hp;
                player.Heal(12);
                var healed = player.Hp - hpBeforeHeal;
                AppendLog($"HP 물약을 사용해 체력 {healed} 회복.");
            }

            var playerAttack = Random.Shared.Next(4, 9);
            enemy.TakeDamage(playerAttack);
            AppendLog($"{enemy.Name}에게 {playerAttack} 피해.");

            if (!enemy.IsAlive) break;

            var enemyAttack = enemy.Attack + Random.Shared.Next(0, 3);
            player.TakeDamage(enemyAttack);
            AppendLog($"{player.Name}가 {enemyAttack} 피해를 입었다.");

            if (!player.IsAlive)
            {
                AppendLog("기절했다.");
            }

            Thread.Sleep(1000);
        }

        var playerWon = player.IsAlive && !enemy.IsAlive;
        var timeExpired = player.IsAlive && enemy.IsAlive;
        return new CombatResult(playerWon, false, timeExpired, log.ToString());
    }

    private static bool ShouldUseHealingPotion(Player player)
    {
        if (player.Hp <= 0) return false;
        return player.Hp * 100 < player.MaxHp * 20;
    }
}

public sealed record CombatResult(bool PlayerWon, bool PlayerFled, bool TimeExpired, string Log);
