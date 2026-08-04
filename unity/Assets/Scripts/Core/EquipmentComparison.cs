using System;
using System.Collections.Generic;
using System.Linq;

namespace ToilRelic.Unity.Core
{
    public enum EquipmentComparisonReason
    {
        None,
        SameItem,
        UnknownCandidate,
        CandidateNotOwned,
        IncompatibleSlot,
        CandidateEquippedElsewhere
    }

    public enum EquipmentStat
    {
        Attack,
        Defense,
        DamageReduction,
        MaxHp
    }

    public sealed class EquipmentStatDelta
    {
        public EquipmentStat Stat { get; }
        public int CurrentValue { get; }
        public int CandidateValue { get; }
        public int Delta => CandidateValue - CurrentValue;

        public EquipmentStatDelta(EquipmentStat stat, int currentValue, int candidateValue)
        {
            Stat = stat;
            CurrentValue = currentValue;
            CandidateValue = candidateValue;
        }
    }

    public sealed class EquipmentComparisonResult
    {
        public EquipmentSlot DestinationSlot { get; }
        public EquipmentDefinition Current { get; }
        public EquipmentDefinition Candidate { get; }
        public EquipmentComparisonReason Reason { get; }
        public IReadOnlyList<EquipmentStatDelta> StatDeltas { get; }
        public int ProjectedAttackBonus { get; }
        public int ProjectedDefenseBonus { get; }
        public int ProjectedDamageReductionBonus { get; }
        public int ProjectedEquipmentMaxHpBonus { get; }
        public int ProjectedMaxHp { get; }
        public bool IsValid => Reason == EquipmentComparisonReason.None || Reason == EquipmentComparisonReason.SameItem;
        public bool CanCommit => Reason == EquipmentComparisonReason.None;

        internal EquipmentComparisonResult(
            EquipmentSlot destinationSlot,
            EquipmentDefinition current,
            EquipmentDefinition candidate,
            EquipmentComparisonReason reason,
            IReadOnlyList<EquipmentStatDelta> statDeltas,
            int projectedAttackBonus,
            int projectedDefenseBonus,
            int projectedDamageReductionBonus,
            int projectedEquipmentMaxHpBonus,
            int projectedMaxHp)
        {
            DestinationSlot = destinationSlot;
            Current = current;
            Candidate = candidate;
            Reason = reason;
            StatDeltas = statDeltas;
            ProjectedAttackBonus = projectedAttackBonus;
            ProjectedDefenseBonus = projectedDefenseBonus;
            ProjectedDamageReductionBonus = projectedDamageReductionBonus;
            ProjectedEquipmentMaxHpBonus = projectedEquipmentMaxHpBonus;
            ProjectedMaxHp = projectedMaxHp;
        }
    }

    public enum UnequipEligibilityStatus
    {
        OccupiedOptional,
        EmptySlot,
        MandatoryPrimaryWeapon
    }

    public sealed class UnequipEligibilityResult
    {
        public EquipmentSlot DestinationSlot { get; }
        public EquipmentDefinition Current { get; }
        public UnequipEligibilityStatus Status { get; }
        public bool IsOccupied => Current != null;
        public bool IsMandatory => DestinationSlot == EquipmentSlot.PrimaryWeapon;
        public bool CanCommit => Status == UnequipEligibilityStatus.OccupiedOptional;

        internal UnequipEligibilityResult(
            EquipmentSlot destinationSlot,
            EquipmentDefinition current,
            UnequipEligibilityStatus status)
        {
            DestinationSlot = destinationSlot;
            Current = current;
            Status = status;
        }
    }

    public static class EquipmentComparisonEvaluator
    {
        private static readonly IReadOnlyList<EquipmentStatDelta> NoStatDeltas = Array.Empty<EquipmentStatDelta>();

        public static bool IsCandidateAvailable(PlayerState player, EquipmentSlot destinationSlot, string candidateId)
        {
            var reason = EvaluateCandidate(player, destinationSlot, candidateId, out _, out _);
            return reason == EquipmentComparisonReason.None || reason == EquipmentComparisonReason.SameItem;
        }

        public static EquipmentComparisonResult Compare(PlayerState player, EquipmentSlot destinationSlot, string candidateId)
        {
            var reason = EvaluateCandidate(player, destinationSlot, candidateId, out var current, out var candidate);
            if (reason != EquipmentComparisonReason.None && reason != EquipmentComparisonReason.SameItem)
            {
                return Invalid(player, destinationSlot, current, candidate, reason);
            }

            var deltas = BuildStatDeltas(current, candidate);
            var currentAttack = current?.AttackBonus ?? 0;
            var currentDefense = current?.DefenseBonus ?? 0;
            var currentDamageReduction = current?.DamageReductionBonus ?? 0;
            var currentMaxHp = current?.MaxHpBonus ?? 0;

            return new EquipmentComparisonResult(
                destinationSlot,
                current,
                candidate,
                reason,
                deltas,
                player.AttackBonus - currentAttack + candidate.AttackBonus,
                player.DefenseBonus - currentDefense + candidate.DefenseBonus,
                player.DamageReductionBonus - currentDamageReduction + candidate.DamageReductionBonus,
                player.EquipmentMaxHpBonus - currentMaxHp + candidate.MaxHpBonus,
                player.MaxHp - currentMaxHp + candidate.MaxHpBonus);
        }

        private static EquipmentComparisonReason EvaluateCandidate(
            PlayerState player,
            EquipmentSlot destinationSlot,
            string candidateId,
            out EquipmentDefinition current,
            out EquipmentDefinition candidate)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            current = player.TryGetEquippedEquipment(destinationSlot, out var equipped) ? equipped : null;
            if (!EquipmentCatalog.TryGet(candidateId, out var foundCandidate))
            {
                candidate = null;
                return EquipmentComparisonReason.UnknownCandidate;
            }

            candidate = foundCandidate;

            if (!player.OwnedEquipmentIds.Contains(foundCandidate.Id))
            {
                return EquipmentComparisonReason.CandidateNotOwned;
            }

            if (!foundCandidate.CanEquipTo(destinationSlot))
            {
                return EquipmentComparisonReason.IncompatibleSlot;
            }

            if (player.EquippedEquipment.Any(entry =>
                    entry.equipmentId == foundCandidate.Id && entry.slot != destinationSlot))
            {
                return EquipmentComparisonReason.CandidateEquippedElsewhere;
            }

            return current?.Id == foundCandidate.Id
                ? EquipmentComparisonReason.SameItem
                : EquipmentComparisonReason.None;
        }

        public static UnequipEligibilityResult EvaluateUnequip(PlayerState player, EquipmentSlot destinationSlot)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var current = player.TryGetEquippedEquipment(destinationSlot, out var equipped) ? equipped : null;
            var status = destinationSlot == EquipmentSlot.PrimaryWeapon
                ? UnequipEligibilityStatus.MandatoryPrimaryWeapon
                : current == null
                    ? UnequipEligibilityStatus.EmptySlot
                    : UnequipEligibilityStatus.OccupiedOptional;
            return new UnequipEligibilityResult(destinationSlot, current, status);
        }

        private static EquipmentComparisonResult Invalid(
            PlayerState player,
            EquipmentSlot destinationSlot,
            EquipmentDefinition current,
            EquipmentDefinition candidate,
            EquipmentComparisonReason reason)
        {
            return new EquipmentComparisonResult(
                destinationSlot,
                current,
                candidate,
                reason,
                NoStatDeltas,
                player.AttackBonus,
                player.DefenseBonus,
                player.DamageReductionBonus,
                player.EquipmentMaxHpBonus,
                player.MaxHp);
        }

        private static IReadOnlyList<EquipmentStatDelta> BuildStatDeltas(
            EquipmentDefinition current,
            EquipmentDefinition candidate)
        {
            var deltas = new List<EquipmentStatDelta>(4);
            AddIfPresent(deltas, EquipmentStat.Attack, current?.AttackBonus ?? 0, candidate.AttackBonus);
            AddIfPresent(deltas, EquipmentStat.Defense, current?.DefenseBonus ?? 0, candidate.DefenseBonus);
            AddIfPresent(deltas, EquipmentStat.DamageReduction, current?.DamageReductionBonus ?? 0, candidate.DamageReductionBonus);
            AddIfPresent(deltas, EquipmentStat.MaxHp, current?.MaxHpBonus ?? 0, candidate.MaxHpBonus);
            return deltas;
        }

        private static void AddIfPresent(
            ICollection<EquipmentStatDelta> deltas,
            EquipmentStat stat,
            int currentValue,
            int candidateValue)
        {
            if (currentValue != 0 || candidateValue != 0)
            {
                deltas.Add(new EquipmentStatDelta(stat, currentValue, candidateValue));
            }
        }
    }
}
