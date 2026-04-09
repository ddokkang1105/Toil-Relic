using System.Text;
using ToilRelic.Models;
using ToilRelic.Util;

namespace ToilRelic.Systems;

public sealed class CombatSystem
{
    public CombatResult Fight(Player player, Enemy enemy)
    {
        var log = new StringBuilder();
        void AppendLog(string message)
        {
            log.AppendLine(message);
            Console.WriteLine(message);
        }

        while (player.IsAlive && enemy.IsAlive)
        {
            ConsoleUI.Section("전투", $"{player.Name} HP {player.Hp} vs {enemy.Name} HP {enemy.Hp}");
            ConsoleUI.Menu("행동", new Dictionary<int, string>
            {
                { 1, "공격" },
                { 2, "방어" },
                { 3, "도망" },
                { 4, "HP 물약 사용" }
            });

            var choice = ConsoleUI.ReadInt("번호 입력", 1, 4);

            if (choice == 3)
            {
                var escaped = Random.Shared.NextDouble() < 0.55;
                if (escaped)
                {
                    AppendLog("도망에 성공했다.");
                    return new CombatResult(false, true, log.ToString());
                }

                AppendLog("도망 실패!");
            }
            else if (choice == 4)
            {
                if (player.Hp >= player.MaxHp)
                {
                    AppendLog("이미 체력이 가득하다.");
                    continue;
                }

                if (!player.Consume(ItemType.HealingPotion, 1))
                {
                    AppendLog("HP 물약이 없다.");
                    continue;
                }

                var hpBeforeHeal = player.Hp;
                player.Heal(12);
                var healed = player.Hp - hpBeforeHeal;
                AppendLog($"HP 물약을 사용해 체력 {healed} 회복.");
            }

            var playerAttack = Random.Shared.Next(4, 9);
            if (choice == 1)
            {
                enemy.TakeDamage(playerAttack);
                AppendLog($"{enemy.Name}에게 {playerAttack} 피해.");
            }
            else if (choice == 2)
            {
                AppendLog("방어 태세를 취했다.");
            }

            if (!enemy.IsAlive) break;

            var enemyAttack = enemy.Attack + Random.Shared.Next(0, 3);
            if (choice == 2)
            {
                enemyAttack = Math.Max(0, enemyAttack - 3);
            }

            player.TakeDamage(enemyAttack);
            AppendLog($"{player.Name}가 {enemyAttack} 피해를 입었다.");

            if (!player.IsAlive)
            {
                AppendLog("기절했다.");
            }
        }

        return new CombatResult(player.IsAlive, false, log.ToString());
    }
}

public sealed record CombatResult(bool PlayerWon, bool PlayerFled, string Log);
