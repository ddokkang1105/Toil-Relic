using ToilRelic.Models;

namespace ToilRelic.Util;

public static class ConsoleUI
{
    public static void Header(string title, string subtitle)
    {
        Console.WriteLine("====================================");
        Console.WriteLine($"{title}");
        Console.WriteLine(subtitle);
        Console.WriteLine("====================================");
        Console.WriteLine();
    }

    public static void Footer(string title, string message)
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine(title);
        Console.WriteLine(message);
        Console.WriteLine("====================================");
    }

    public static void Section(string title, string message)
    {
        Console.WriteLine($"[{title}]");
        Console.WriteLine(message);
        Console.WriteLine();
    }

    public static void Status(Player player)
    {
        Console.WriteLine(
            $"{player.Name} | HP {player.Hp}/{player.MaxHp} | 보물 {player.TreasureCount} | 레벨 {player.LevelProgress:F2}");
    }

    public static void Menu(string title, Dictionary<int, string> options)
    {
        Console.WriteLine($"[{title}]");
        foreach (var kv in options)
        {
            Console.WriteLine($"{kv.Key}. {kv.Value}");
        }
    }

    public static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} ({min}-{max}): ");
            var input = Console.ReadLine();
            if (int.TryParse(input, out var value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine("잘못된 입력.");
        }
    }

    public static void Inventory(Player player)
    {
        Console.WriteLine("[인벤토리]");
        foreach (var kv in player.Inventory)
        {
            Console.WriteLine($"- {ItemLabel(kv.Key)}: {kv.Value}");
        }
        Console.WriteLine();
        Pause();
    }

    public static void Pause()
    {
        Console.Write("계속하려면 Enter...");
        Console.ReadLine();
    }

    private static string ItemLabel(ItemType type)
    {
        return type switch
        {
            ItemType.Junk => "잡템",
            ItemType.RelicPart => "보물 재료",
            ItemType.Treasure => "보물",
            ItemType.HealingPotion => "HP 물약",
            _ => type.ToString()
        };
    }
}
