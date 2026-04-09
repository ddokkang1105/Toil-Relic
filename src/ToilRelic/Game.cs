using ToilRelic.Models;
using ToilRelic.Systems;
using ToilRelic.Util;

namespace ToilRelic;

public sealed class Game
{
    private readonly Player _player = new("노역자");
    private readonly CombatSystem _combat = new();
    private readonly LootSystem _loot = new();
    private readonly CraftingSystem _crafting = new();
    private bool _running = true;

    public void Run()
    {
        ConsoleUI.Header("Toil-Relic", "노역으로 보물을 만드는 턴제 사냥 게임");

        while (_running)
        {
            ConsoleUI.Status(_player);
            ConsoleUI.Menu("행동 선택", new Dictionary<int, string>
            {
                { 1, "사냥하기" },
                { 2, "인벤토리 보기" },
                { 3, "보물 제작" },
                { 4, "휴식" },
                { 5, "종료" },
            });

            var choice = ConsoleUI.ReadInt("번호 입력", 1, 5);
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    Hunt();
                    break;
                case 2:
                    ConsoleUI.Inventory(_player);
                    break;
                case 3:
                    Craft();
                    break;
                case 4:
                    Rest();
                    break;
                case 5:
                    _running = false;
                    break;
            }
        }

        ConsoleUI.Footer(
            "게임 종료",
            $"최종 보물 수: {_player.TreasureCount}, 레벨: {_player.LevelProgress:F2}");
    }

    private void Hunt()
    {
        var enemy = Enemy.RandomEnemy();
        ConsoleUI.Section($"사냥 시작: {enemy.Name}", $"HP {enemy.Hp}");

        var result = _combat.Fight(_player, enemy);

        if (result.PlayerWon)
        {
            var loot = _loot.RollLoot();
            var levelResult = _player.GainExperience(enemy.ExpReward);
            _player.AddItem(ItemType.Junk, loot.Junk);
            _player.AddItem(ItemType.RelicPart, loot.RelicPart);
            _player.AddItem(ItemType.HealingPotion, loot.HealingPotion);

            ConsoleUI.Section("전리품", $"잡템 +{loot.Junk}, 보물 재료 +{loot.RelicPart}, HP 물약 +{loot.HealingPotion}, EXP +{enemy.ExpReward}");
            if (levelResult.LeveledUp)
            {
                ConsoleUI.Section("레벨업", $"+{levelResult.LevelsGained} 상승! 현재 레벨: {levelResult.NewLevel}");
            }
        }
        else if (result.PlayerFled)
        {
            ConsoleUI.Section("도망", "겨우 살아남았다.");
        }
        else
        {
            ConsoleUI.Section("패배", "기절했다. 휴식으로 회복 필요.");
        }

        ConsoleUI.Pause();
    }

    private void Craft()
    {
        var craftResult = _crafting.TryCraftTreasure(_player);
        ConsoleUI.Section("보물 제작", craftResult.Message);
        ConsoleUI.Pause();
    }

    private void Rest()
    {
        _player.Rest();
        ConsoleUI.Section("휴식", "체력이 회복됐다.");
        ConsoleUI.Pause();
    }
}
