using System;
using System.Collections.Generic;
using System.Linq;

namespace Tarot.Core
{
    public enum Arcana { Minor, Major }
    public enum Suit { None, Wands, Swords, Cups, Pentacles }
    public enum Owner { Player, Enemy }
    public enum Zone { Draw, Hand, Discard, Sequence, Removed }
    public enum FlipType { Turn, Level, Permanent }
    public enum Primitive { Damage, Heal, Shield, BuffNext, Flip, SkipResolve, RepeatNext, Transform, SetDirection, PlacementRule, SelfDamage, Draw }
    public enum TargetSelector { Self, LockedEnemy, Player, Adjacent, SequenceEnds, AllEnemies, HandCard, NextCard }
    public enum BuffKind { Damage, Heal }
    public enum DirectionMode { Forward, Backward, ForwardAndBackward, PlayerChoice, Random }
    public enum PlacementRule { None, NoEnds, NextIsEnemy }
    public enum CombatPhase { Decision, Resolving, Won, Lost }
    public enum TransformPool { None, ImplementedMajors, ImplementedMinors }

    [Serializable]
    public sealed class CardDefinition
    {
        public string Id;
        public string Name;
        public Arcana Arcana;
        public Suit Suit;
        public int Rank;
        public bool IsImplemented = true;
        public List<EffectDef> Upright = new List<EffectDef>();
        public List<EffectDef> Reversed = new List<EffectDef>();
        public string TextU;
        public string TextR;
        public int EnemyHp = 20;
        public int EnemyDamage;
        public bool IsMajor => Arcana == Arcana.Major;
    }

    [Serializable]
    public sealed class EffectDef
    {
        public Primitive Primitive;
        public TargetSelector Target = TargetSelector.LockedEnemy;
        public int Flat;
        public int N;
        public int M;
        public int MDiv = 1;
        public int PercentFlat;
        public int PercentN;
        public int PercentM;
        public BuffKind BuffKind;
        public FlipType FlipType;
        public DirectionMode Direction;
        public PlacementRule Placement;
        public int Count = 1;
        public TransformPool Pool;

        public int Amount(CardInstance c)
        {
            int amount = Flat + N * c.N;
            if (M != 0) amount += (M * c.M) / Math.Max(1, MDiv);
            return amount;
        }

        public int Percent(CardInstance c) => PercentFlat + PercentN * c.N + PercentM * c.M;
    }

    public sealed class CardInstance
    {
        public CardDefinition Def;
        public Owner Owner;
        public bool BaseReversed;
        public bool TurnFlip;
        public bool LevelFlip;
        public int M;
        public Zone Zone = Zone.Draw;
        public CardDefinition OriginalDef;
        public bool OriginalBaseReversed;
        public int OriginalM;

        public bool IsTransformed => OriginalDef != null;
        public int N => Def.Rank;
        public bool IsReversed => BaseReversed ^ TurnFlip ^ LevelFlip;
        public string Id => Def.Id;

        public void TransformTo(CardDefinition def)
        {
            if (OriginalDef == null)
            {
                OriginalDef = Def;
                OriginalBaseReversed = BaseReversed;
                OriginalM = M;
            }
            Def = def;
        }

        public void Restore()
        {
            if (OriginalDef == null) return;
            Def = OriginalDef;
            BaseReversed = OriginalBaseReversed;
            M = OriginalM;
            OriginalDef = null;
        }
    }

    public sealed class Combatant
    {
        public int MaxHp;
        public int Hp;
        public int Shield;
        public bool Alive => Hp > 0;

        public Combatant(int maxHp)
        {
            MaxHp = maxHp;
            Hp = maxHp;
        }
    }

    public sealed class Enemy
    {
        public CardInstance Card;
        public Combatant Stats;
        public PlacementRule Placement;
        public int SlotIndex = -1;
        public bool Alive => Stats != null && Stats.Hp > 0;
    }

    public sealed class Slot
    {
        public int Index;
        public bool IsPlayerSlot;
        public CardInstance PlayerCard;
        public Enemy Enemy;
        public bool IsEmpty => PlayerCard == null && Enemy == null;
        public CardInstance Card => PlayerCard != null ? PlayerCard : (Enemy != null ? Enemy.Card : null);
    }

    public sealed class CardState
    {
        public bool BaseReversed;
        public int M;
    }

    public sealed class PendingBuff
    {
        public BuffKind Kind;
        public int Percent;
    }

    public sealed class ResolutionPlan
    {
        public readonly List<int> Forward = new List<int>();
        public readonly List<int> Backward = new List<int>();
        public readonly HashSet<int> Skip = new HashSet<int>();
        public bool UseBoth;
        public bool UseBackward;
        public string DirectionLabel = "Forward";
    }

    public sealed class DeckState
    {
        public readonly List<CardInstance> DrawPile = new List<CardInstance>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> Discard = new List<CardInstance>();
        public Rng Rng;

        public DeckState(Rng rng)
        {
            Rng = rng;
        }

        public void ResetForLevel(IEnumerable<CardDefinition> defs, Dictionary<string, CardState> states)
        {
            DrawPile.Clear();
            Hand.Clear();
            Discard.Clear();
            foreach (var def in defs)
            {
                var card = new CardInstance { Def = def, Owner = Owner.Player, Zone = Zone.Draw };
                if (states != null && states.TryGetValue(def.Id, out var st))
                {
                    card.BaseReversed = st.BaseReversed;
                    card.M = st.M;
                }
                DrawPile.Add(card);
            }
            Rng.Shuffle(DrawPile);
        }

        public int Draw(int count)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (DrawPile.Count == 0) Reshuffle();
                if (DrawPile.Count == 0) break;
                int last = DrawPile.Count - 1;
                var card = DrawPile[last];
                DrawPile.RemoveAt(last);
                card.Zone = Zone.Hand;
                Hand.Add(card);
                drawn++;
            }
            return drawn;
        }

        public void Reshuffle()
        {
            if (Discard.Count == 0) return;
            foreach (var card in Discard) card.Zone = Zone.Draw;
            DrawPile.AddRange(Discard);
            Discard.Clear();
            Rng.Shuffle(DrawPile);
        }

        public bool RemoveFromHand(CardInstance card)
        {
            return Hand.Remove(card);
        }
    }

    public sealed class RunState
    {
        public int MaxHp = 120;
        public int Hp = 120;
        public List<string> DeckIds = new List<string>();
        public Dictionary<string, CardState> CardStates = new Dictionary<string, CardState>();
        public int LevelIndex;
        public int Seed;
        public bool IsComplete => LevelIndex >= 4;

        public RunState(IEnumerable<string> deckIds, int seed)
        {
            DeckIds = deckIds.ToList();
            Seed = seed;
            foreach (var id in DeckIds)
            {
                if (!CardStates.ContainsKey(id)) CardStates[id] = new CardState();
            }
        }
    }

    public sealed class CombatState
    {
        public Combatant Player;
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public DeckState Deck;
        public SequenceState Sequence;
        public int Turn = 1;
        public CombatPhase Phase = CombatPhase.Decision;
        public bool AntiDefyUsed;
        public bool AntiDefyTargeting;
        public bool PlayerBackward;
        public readonly List<PendingBuff> PendingBuffs = new List<PendingBuff>();
        public int PendingRepeat;
        public readonly List<string> Log = new List<string>();
        public int LevelIndex;
        public bool IsBoss;
        public Rng Rng;

        public Enemy LockedTarget()
        {
            Enemy best = null;
            int bestSlot = int.MaxValue;
            foreach (var e in Enemies)
            {
                if (e.Alive && e.SlotIndex >= 0 && e.SlotIndex < bestSlot)
                {
                    best = e;
                    bestSlot = e.SlotIndex;
                }
            }
            return best;
        }

        public bool AllEnemiesDead => Enemies.All(e => !e.Alive);
    }

    public sealed class SequenceState
    {
        public readonly Slot[] Slots = new Slot[9];

        public SequenceState()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = new Slot { Index = i };
            }
        }
    }
}
