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
        var weaponName = player.TryGetPrimaryWeapon(out var weapon) ? $"{weapon.DisplayName} +{weapon.AttackBonus}" : "없음";
        Console.WriteLine(
            $"{player.Name} | HP {player.Hp}/{player.MaxHp} | 보물 {player.TreasureCount} | 레벨 {player.LevelProgress:F2} | 무기 {weaponName}");
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

    public static void Equipment(Player player)
    {
        Console.WriteLine("[장비]");
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var item = player.TryGetEquippedEquipment(slot, out var equipment) ? equipment.DisplayName : "비어 있음";
            Console.WriteLine($"- {slot}: {item}");
        }
        Console.WriteLine($"합계: 공격 +{player.AttackBonus}, 방어 +{player.DefenseBonus}, 피해 감소 +{player.DamageReductionBonus}, 최대 HP +{player.EquipmentMaxHpBonus}");
        Console.WriteLine();
    }

    public static void EquipmentSlot(EquipmentSlot slot, EquipmentDefinition? current)
    {
        Console.WriteLine($"[{slot}]");
        Console.WriteLine($"Current: {current?.DisplayName ?? "Empty"}");
        Console.WriteLine();
    }

    public static string EquipmentOption(EquipmentDefinition equipment)
    {
        var modifiers = new List<string>();
        AddModifier(modifiers, "Attack", equipment.AttackBonus);
        AddModifier(modifiers, "Defense", equipment.DefenseBonus);
        AddModifier(modifiers, "Damage reduction", equipment.DamageReductionBonus);
        AddModifier(modifiers, "Max HP", equipment.MaxHpBonus);
        return modifiers.Count == 0
            ? $"{equipment.DisplayName} (no modifiers)"
            : $"{equipment.DisplayName} ({string.Join(", ", modifiers)})";
    }

    public static void EquipmentComparison(EquipmentComparisonResult comparison)
    {
        Console.WriteLine("[Equipment comparison]");
        Console.WriteLine($"Destination: {comparison.DestinationSlot}");
        Console.WriteLine($"Current: {comparison.Current?.DisplayName ?? "Empty"}");
        Console.WriteLine($"Candidate: {comparison.Candidate?.DisplayName ?? "Unavailable"}");
        if (comparison.StatDeltas.Count == 0)
        {
            Console.WriteLine("No equipment stat change (±0)");
        }

        foreach (var stat in comparison.StatDeltas)
        {
            Console.WriteLine($"{StatLabel(stat.Stat)}: {stat.CurrentValue} -> {stat.CandidateValue} ({SignedDelta(stat.Delta)})");
        }

        Console.WriteLine(
            $"Projected: Attack {SignedBonus(comparison.ProjectedAttackBonus)}, " +
            $"Defense {SignedBonus(comparison.ProjectedDefenseBonus)}, " +
            $"Damage reduction {SignedBonus(comparison.ProjectedDamageReductionBonus)}, " +
            $"Max HP {comparison.ProjectedMaxHp}");
        Console.WriteLine();
    }

    public static void UnequipPreview(UnequipEligibilityResult result)
    {
        Console.WriteLine("[Unequip preview]");
        Console.WriteLine($"Destination: {result.DestinationSlot}");
        Console.WriteLine($"Current: {result.Current?.DisplayName ?? "Empty"}");
        Console.WriteLine();
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

    private static void AddModifier(ICollection<string> modifiers, string label, int value)
    {
        if (value != 0)
        {
            modifiers.Add($"{label} {SignedBonus(value)}");
        }
    }

    private static string StatLabel(EquipmentStat stat) => stat switch
    {
        EquipmentStat.Attack => "Attack",
        EquipmentStat.Defense => "Defense",
        EquipmentStat.DamageReduction => "Damage reduction",
        EquipmentStat.MaxHp => "Max HP",
        _ => stat.ToString()
    };

    private static string SignedDelta(int value) => value switch
    {
        > 0 => $"+{value}",
        < 0 => value.ToString(),
        _ => "±0"
    };

    private static string SignedBonus(int value) => value >= 0 ? $"+{value}" : value.ToString();
}
