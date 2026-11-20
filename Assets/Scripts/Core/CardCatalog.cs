using System;
using System.Collections.Generic;
using System.Linq;

namespace Tarot.Core
{
    public static class CardCatalog
    {
        static readonly Dictionary<string, CardDefinition> s_Defs = new Dictionary<string, CardDefinition>();
        static readonly List<CardDefinition> s_All = new List<CardDefinition>();
        static readonly string[] s_Roman = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        static CardCatalog()
        {
            BuildMinor(Suit.Wands);
            BuildMinor(Suit.Swords);
            BuildMinor(Suit.Cups);
            BuildMajors();
        }

        public static IReadOnlyList<CardDefinition> All => s_All;
        public static IEnumerable<CardDefinition> ImplementedMajors => s_All.Where(d => d.IsMajor && d.IsImplemented);
        public static IEnumerable<CardDefinition> ImplementedMinors => s_All.Where(d => !d.IsMajor && d.IsImplemented);

        public static CardDefinition Get(string id)
        {
            return id != null && s_Defs.TryGetValue(id, out var def) ? def : null;
        }

        public static string Id(Suit suit, int rank) => suit + "_" + rank;

        public static List<string> StartingDeck()
        {
            return new List<string>
            {
                Id(Suit.Wands, 1), Id(Suit.Wands, 3), Id(Suit.Wands, 5), Id(Suit.Wands, 7), Id(Suit.Wands, 10),
                Id(Suit.Swords, 1), Id(Suit.Swords, 2), Id(Suit.Swords, 4), Id(Suit.Swords, 6), Id(Suit.Swords, 10),
                Id(Suit.Cups, 1), Id(Suit.Cups, 2), Id(Suit.Cups, 3), Id(Suit.Cups, 5), Id(Suit.Cups, 8), Id(Suit.Cups, 10),
                "Major_8"
            };
        }

        public static List<string> Encounter(int levelIndex)
        {
            switch (levelIndex)
            {
                case 0: return new List<string> { Id(Suit.Swords, 2), Id(Suit.Cups, 3), Id(Suit.Wands, 4) };
                case 1: return new List<string> { Id(Suit.Swords, 5), Id(Suit.Cups, 6), Id(Suit.Wands, 7) };
                case 2: return new List<string> { Id(Suit.Swords, 8), Id(Suit.Cups, 9), Id(Suit.Wands, 10) };
                default: return new List<string> { "Major_0", "Major_1", "Major_10" };
            }
        }

        static void BuildMinor(Suit suit)
        {
            for (int rank = 1; rank <= 10; rank++)
            {
                string id = Id(suit, rank);
                string name = suit + " " + s_Roman[rank];
                var def = new CardDefinition
                {
                    Id = id,
                    Name = name,
                    Arcana = Arcana.Minor,
                    Suit = suit,
                    Rank = rank,
                    IsImplemented = true,
                    EnemyHp = 20,
                    EnemyDamage = rank
                };

                switch (suit)
                {
                    case Suit.Wands:
                        def.Upright.Add(Buff(BuffKind.Damage, rank == 1 ? 10 : 0, rank == 1 ? 0 : 10, rank == 1 ? 10 : 0));
                        def.Reversed.Add(Buff(BuffKind.Heal, rank == 1 ? 10 : 0, rank == 1 ? 0 : 10, rank == 1 ? 10 : 0));
                        def.TextU = rank == 1 ? "Next DMG +10% (+10%/flip)" : "Next DMG +" + (rank * 10) + "%";
                        def.TextR = rank == 1 ? "Next HEAL +10% (+10%/flip)" : "Next HEAL +" + (rank * 10) + "%";
                        break;

                    case Suit.Swords:
                        def.Upright.Add(rank == 1 ? Dmg(1, 0, 1) : Dmg(0, 1));
                        def.Reversed.Add(rank == 1 ? Dmg(5, 0, 1, 2) : Dmg(5));
                        def.TextU = rank == 1 ? "Deal 1+flips" : "Deal " + rank;
                        def.TextR = rank == 1 ? "Deal 5+flips/2" : "Deal 5";
                        break;

                    case Suit.Cups:
                        def.Upright.Add(rank == 1 ? Heal(1, 0, 1) : Heal(0, 1));
                        def.Reversed.Add(rank == 1 ? Shield(5, 0, 1) : Shield(0, 1));
                        def.TextU = rank == 1 ? "Heal 1+flips" : "Heal " + rank;
                        def.TextR = rank == 1 ? "Shield 5+flips" : "Shield " + rank;
                        break;
                }

                Add(def);
            }
        }

        static void BuildMajors()
        {
            Add(new CardDefinition
            {
                Id = "Major_0", Name = "The Fool", Arcana = Arcana.Major, Suit = Suit.None, Rank = 0,
                EnemyHp = 60, EnemyDamage = 15,
                TextU = "Deal 15. First and last cards don't resolve.",
                TextR = "Deal 15. Next card resolves twice.",
                Upright = { Dmg(15), Skip() },
                Reversed = { Dmg(15), Repeat(2) }
            });

            Add(new CardDefinition
            {
                Id = "Major_1", Name = "The Magician", Arcana = Arcana.Major, Suit = Suit.None, Rank = 1,
                EnemyHp = 60, EnemyDamage = 15,
                TextU = "At turn start, transform into a random major.",
                TextR = "Deal 15. At turn start, transform 2 hand cards into minors.",
                Reversed = { Dmg(15) }
            });

            Add(new CardDefinition
            {
                Id = "Major_8", Name = "Strength", Arcana = Arcana.Major, Suit = Suit.None, Rank = 8,
                EnemyHp = 60, EnemyDamage = 20,
                TextU = "Deal 20.",
                TextR = "Deal 25. Lose 10 HP after attacking.",
                Upright = { Dmg(20) },
                Reversed = { Dmg(25), SelfDmg(10) }
            });

            Add(new CardDefinition
            {
                Id = "Major_10", Name = "Wheel of Fortune", Arcana = Arcana.Major, Suit = Suit.None, Rank = 10,
                EnemyHp = 60, EnemyDamage = 15,
                TextU = "Deal 15. Sequence resolves forward and backward.",
                TextR = "Deal 15. Choose the sequence direction.",
                Upright = { Dmg(15), Direction(DirectionMode.ForwardAndBackward) },
                Reversed = { Dmg(15), Direction(DirectionMode.PlayerChoice) }
            });

            Add(new CardDefinition
            {
                Id = "Major_12", Name = "The Hanged Man", Arcana = Arcana.Major, Suit = Suit.None, Rank = 12,
                EnemyHp = 60, EnemyDamage = 15,
                TextU = "Deal 15. Turn-flip adjacent cards.",
                TextR = "Deal 15. Turn-flip first and last cards.",
                Upright = { Dmg(15), FlipDef(FlipType.Turn, TargetSelector.Adjacent) },
                Reversed = { Dmg(15), FlipDef(FlipType.Turn, TargetSelector.SequenceEnds) }
            });
        }

        static void Add(CardDefinition def)
        {
            s_Defs[def.Id] = def;
            s_All.Add(def);
        }

        static EffectDef Dmg(int flat = 0, int n = 0, int m = 0, int mDiv = 1, TargetSelector target = TargetSelector.LockedEnemy)
        {
            return new EffectDef { Primitive = Primitive.Damage, Target = target, Flat = flat, N = n, M = m, MDiv = mDiv };
        }

        static EffectDef Heal(int flat = 0, int n = 0, int m = 0)
        {
            return new EffectDef { Primitive = Primitive.Heal, Target = TargetSelector.Self, Flat = flat, N = n, M = m };
        }

        static EffectDef Shield(int flat = 0, int n = 0, int m = 0)
        {
            return new EffectDef { Primitive = Primitive.Shield, Target = TargetSelector.Self, Flat = flat, N = n, M = m };
        }

        static EffectDef Buff(BuffKind kind, int pf = 0, int pn = 0, int pm = 0)
        {
            return new EffectDef { Primitive = Primitive.BuffNext, Target = TargetSelector.NextCard, BuffKind = kind, PercentFlat = pf, PercentN = pn, PercentM = pm };
        }

        static EffectDef FlipDef(FlipType type, TargetSelector target)
        {
            return new EffectDef { Primitive = Primitive.Flip, Target = target, FlipType = type };
        }

        static EffectDef Repeat(int count) => new EffectDef { Primitive = Primitive.RepeatNext, Target = TargetSelector.NextCard, Count = count };
        static EffectDef Skip() => new EffectDef { Primitive = Primitive.SkipResolve, Target = TargetSelector.SequenceEnds };
        static EffectDef SelfDmg(int amount) => new EffectDef { Primitive = Primitive.SelfDamage, Target = TargetSelector.Self, Flat = amount };
        static EffectDef Direction(DirectionMode mode) => new EffectDef { Primitive = Primitive.SetDirection, Direction = mode };
    }
}
