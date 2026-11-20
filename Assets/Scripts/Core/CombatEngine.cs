using System;
using System.Collections.Generic;
using System.Linq;

namespace Tarot.Core
{
    public static class CombatEngine
    {
        public static CombatState CreateCombat(RunState run, int levelIndex)
        {
            var seed = run.Seed ^ (levelIndex * 7919) ^ 0x5f3759df;
            var rng = new Rng(seed);
            var defs = run.DeckIds.Select(CardCatalog.Get).Where(d => d != null).ToList();
            var state = new CombatState
            {
                Player = new Combatant(run.MaxHp),
                Deck = new DeckState(rng),
                Sequence = new SequenceState(),
                LevelIndex = levelIndex,
                IsBoss = levelIndex >= 3,
                Rng = rng,
                Turn = 1
            };
            state.Player.Hp = Math.Max(1, Math.Min(run.Hp, run.MaxHp));
            state.Deck.ResetForLevel(defs, run.CardStates);

            foreach (var id in CardCatalog.Encounter(levelIndex))
            {
                var def = CardCatalog.Get(id);
                if (def == null) continue;
                var card = new CardInstance
                {
                    Def = def,
                    Owner = Owner.Enemy,
                    BaseReversed = rng.Chance(50),
                    Zone = Zone.Sequence
                };
                state.Enemies.Add(new Enemy
                {
                    Card = card,
                    Stats = new Combatant(def.EnemyHp)
                });
            }

            Log(state, "Level " + (levelIndex + 1) + " begins.");
            state.Deck.Draw(6);
            ApplyTurnStartTransforms(state);
            InsertEnemies(state);
            state.Phase = CombatPhase.Decision;
            return state;
        }

        public static void StartTurn(CombatState s)
        {
            if (s.Phase == CombatPhase.Lost || s.Phase == CombatPhase.Won) return;
            s.Turn++;
            s.AntiDefyUsed = false;
            s.AntiDefyTargeting = false;
            s.PlayerBackward = false;
            s.PendingBuffs.Clear();
            s.PendingRepeat = 0;
            RestoreTransforms(s);
            ClearSequence(s);
            ClearShields(s);
            ClearTurnFlips(s);
            s.Deck.Draw(4);
            ApplyTurnStartTransforms(s);
            InsertEnemies(s);
            s.Phase = CombatPhase.Decision;
            Log(s, "Turn " + s.Turn + " begins.");
        }

        public static void EndTurn(CombatState s)
        {
            foreach (var slot in s.Sequence.Slots)
            {
                if (slot.PlayerCard != null)
                {
                    slot.PlayerCard.Zone = Zone.Discard;
                    s.Deck.Discard.Add(slot.PlayerCard);
                    slot.PlayerCard = null;
                }
                slot.Enemy = null;
                slot.IsPlayerSlot = true;
            }
            foreach (var e in s.Enemies) e.SlotIndex = -1;
            RestoreTransforms(s);
            ClearShields(s);
            ClearTurnFlips(s);
            s.PendingBuffs.Clear();
            s.PendingRepeat = 0;
            s.AntiDefyTargeting = false;
            s.PlayerBackward = false;
        }

        public static bool PlaceCard(CombatState s, CardInstance card, int slotIndex, out string error)
        {
            error = null;
            if (s.Phase != CombatPhase.Decision) { error = "Not in decision phase."; return false; }
            if (card == null || card.Owner != Owner.Player || card.Zone != Zone.Hand) { error = "Card is not in hand."; return false; }
            var slot = GetSlot(s, slotIndex);
            if (slot == null || !slot.IsPlayerSlot) { error = "Not a player slot."; return false; }
            if (slot.PlayerCard != null) { error = "Slot is occupied."; return false; }
            if (!s.Deck.RemoveFromHand(card)) { error = "Card not found in hand."; return false; }
            slot.PlayerCard = card;
            card.Zone = Zone.Sequence;
            Log(s, card.Def.Name + " placed in slot " + (slotIndex + 1) + ".");
            return true;
        }

        public static bool ReturnCard(CombatState s, int slotIndex, out string error)
        {
            error = null;
            if (s.Phase != CombatPhase.Decision) { error = "Not in decision phase."; return false; }
            var slot = GetSlot(s, slotIndex);
            if (slot == null || slot.PlayerCard == null) { error = "No card to return."; return false; }
            var card = slot.PlayerCard;
            slot.PlayerCard = null;
            card.Zone = Zone.Hand;
            s.Deck.Hand.Add(card);
            Log(s, card.Def.Name + " returned to hand.");
            return true;
        }

        public static bool TryBeginAntiDefy(CombatState s, CardInstance card, out string error)
        {
            error = null;
            if (s.Phase != CombatPhase.Decision) { error = "Anti-defy is only usable in the decision phase."; return false; }
            if (s.AntiDefyUsed) { error = "Anti-defy already used this turn."; return false; }
            if (s.AntiDefyTargeting) { error = "Choose an anti-defy target first."; return false; }
            if (card == null || card.Owner != Owner.Player || card.Zone != Zone.Hand) { error = "Drag a hand card to the discard zone."; return false; }
            if (!s.Deck.Hand.Contains(card)) { error = "Card is not in hand."; return false; }
            bool hasEnemy = s.Enemies.Any(e => e.Alive);
            bool hasOtherHand = s.Deck.Hand.Count > 1;
            if (!hasEnemy && !hasOtherHand) { error = "No legal anti-defy target."; return false; }
            s.Deck.RemoveFromHand(card);
            card.Zone = Zone.Discard;
            s.Deck.Discard.Add(card);
            s.AntiDefyUsed = true;
            s.AntiDefyTargeting = true;
            Log(s, "Anti-defy: discarded " + card.Def.Name + ".");
            return true;
        }

        public static bool ApplyAntiDefyAll(CombatState s, out string error)
        {
            error = null;
            if (!s.AntiDefyTargeting) { error = "No anti-defy pending."; return false; }
            foreach (var e in s.Enemies.Where(e => e.Alive))
            {
                ApplyFlip(s, e.Card, FlipType.Permanent);
            }
            s.AntiDefyTargeting = false;
            Log(s, "Anti-defy: all living enemies permanently flipped.");
            return true;
        }

        public static bool ApplyAntiDefyHand(CombatState s, CardInstance card, out string error)
        {
            error = null;
            if (!s.AntiDefyTargeting) { error = "No anti-defy pending."; return false; }
            if (card == null || card.Owner != Owner.Player || card.Zone != Zone.Hand) { error = "Choose a hand card."; return false; }
            ApplyFlip(s, card, FlipType.Level);
            s.AntiDefyTargeting = false;
            Log(s, "Anti-defy: " + card.Def.Name + " level-flipped.");
            return true;
        }

        public static ResolutionPlan BuildPlan(CombatState s)
        {
            var plan = new ResolutionPlan();
            var occupied = s.Sequence.Slots.Where(x => x.Card != null).Select(x => x.Index).ToList();
            plan.Forward.AddRange(occupied);

            bool playerFoolUpright = s.Sequence.Slots.Any(x => x.PlayerCard != null && x.PlayerCard.Def.Id == "Major_0" && !x.PlayerCard.IsReversed);
            bool wheelUpright = s.Sequence.Slots.Any(x => x.Card != null && x.Card.Def.Id == "Major_10" && !x.Card.IsReversed);
            bool playerWheelReversed = s.Sequence.Slots.Any(x => x.PlayerCard != null && x.PlayerCard.Def.Id == "Major_10" && x.PlayerCard.IsReversed);
            bool enemyWheelReversed = s.Sequence.Slots.Any(x => x.Enemy != null && x.Enemy.Card.Def.Id == "Major_10" && x.Enemy.Card.IsReversed);

            if (wheelUpright)
            {
                plan.UseBoth = true;
                plan.DirectionLabel = "Both directions";
            }
            else if (playerWheelReversed)
            {
                plan.UseBackward = s.PlayerBackward;
                plan.DirectionLabel = s.PlayerBackward ? "Backward" : "Forward";
            }
            else if (enemyWheelReversed)
            {
                plan.UseBackward = s.Rng.Chance(50);
                plan.DirectionLabel = plan.UseBackward ? "Backward (random)" : "Forward (random)";
            }
            else
            {
                plan.DirectionLabel = "Forward";
            }

            if (plan.UseBackward) plan.Forward.Reverse();
            plan.Backward.AddRange(Enumerable.Reverse(occupied));

            if (playerFoolUpright && occupied.Count > 0)
            {
                plan.Skip.Add(occupied[0]);
                plan.Skip.Add(occupied[occupied.Count - 1]);
            }
            return plan;
        }

        public static bool ResolveSlot(CombatState s, ResolutionPlan plan, int slotIndex)
        {
            if (s.Phase == CombatPhase.Lost || s.Phase == CombatPhase.Won) return false;
            if (plan.Skip.Contains(slotIndex))
            {
                Log(s, "Slot " + (slotIndex + 1) + " skipped by The Fool.");
                return true;
            }
            var slot = GetSlot(s, slotIndex);
            if (slot == null || slot.Card == null) return true;
            var card = slot.Card;

            int repetitions = 1 + s.PendingRepeat;
            s.PendingRepeat = 0;
            var buffs = new List<PendingBuff>(s.PendingBuffs);
            s.PendingBuffs.Clear();

            for (int i = 0; i < repetitions; i++)
            {
                ResolveCard(s, card, buffs);
                if (s.Phase == CombatPhase.Lost) return false;
            }
            CheckEnemyDeaths(s);
            return s.Phase != CombatPhase.Lost;
        }

        public static void SaveToRun(CombatState s, RunState run)
        {
            run.Hp = Math.Max(0, s.Player.Hp);
            foreach (var card in AllPlayerCards(s))
            {
                if (card.IsTransformed) card.Restore();
                if (!run.CardStates.TryGetValue(card.Def.Id, out var st))
                {
                    st = new CardState();
                    run.CardStates[card.Def.Id] = st;
                }
                st.BaseReversed = card.BaseReversed;
                st.M = card.M;
            }
        }

        public static bool HasReversedWheel(CombatState s)
        {
            return s.Deck.Hand.Any(c => c.Def.Id == "Major_10" && c.IsReversed)
                || s.Sequence.Slots.Any(x => x.PlayerCard != null && x.PlayerCard.Def.Id == "Major_10" && x.PlayerCard.IsReversed);
        }

        public static string CardText(CardInstance card)
        {
            if (card == null) return "";
            if (card.Owner == Owner.Enemy && !card.Def.IsMajor)
                return "Deal " + card.Def.EnemyDamage;
            return card.IsReversed ? card.Def.TextR : card.Def.TextU;
        }

        public static List<Enemy> AliveEnemies(CombatState s) => s.Enemies.Where(e => e.Alive).ToList();

        public static Slot GetSlot(CombatState s, int index)
        {
            return index >= 0 && index < s.Sequence.Slots.Length ? s.Sequence.Slots[index] : null;
        }

        static void ResolveCard(CombatState s, CardInstance card, List<PendingBuff> buffs)
        {
            if (card.Owner == Owner.Enemy && !card.Def.IsMajor)
            {
                int amount = card.Def.EnemyDamage;
                int percent = buffs.Where(b => b.Kind == BuffKind.Damage).Sum(b => b.Percent);
                if (percent != 0) amount = amount * (100 + percent) / 100;
                ApplyDamage(s, s.Player, Math.Max(0, amount), card.Def.Name);
                return;
            }

            var effects = card.IsReversed ? card.Def.Reversed : card.Def.Upright;
            foreach (var effect in effects)
            {
                ResolveEffect(s, card, effect, buffs);
                if (s.Phase == CombatPhase.Lost) return;
            }
        }

        static void ResolveEffect(CombatState s, CardInstance source, EffectDef effect, List<PendingBuff> buffs)
        {
            if (source.Owner == Owner.Enemy && (effect.Primitive == Primitive.SkipResolve || effect.Primitive == Primitive.RepeatNext || effect.Primitive == Primitive.SetDirection))
                return;

            switch (effect.Primitive)
            {
                case Primitive.Damage:
                {
                    Combatant target = source.Owner == Owner.Player
                        ? s.LockedTarget()?.Stats
                        : s.Player;
                    if (target == null) { Log(s, source.Def.Name + " has no target."); return; }
                    int amount = effect.Amount(source);
                    int percent = buffs.Where(b => b.Kind == BuffKind.Damage).Sum(b => b.Percent);
                    if (percent != 0) amount = amount * (100 + percent) / 100;
                    ApplyDamage(s, target, Math.Max(0, amount), source.Def.Name);
                    break;
                }
                case Primitive.Heal:
                {
                    var target = OwnerCombatant(s, source);
                    if (target == null) return;
                    int amount = effect.Amount(source);
                    int percent = buffs.Where(b => b.Kind == BuffKind.Heal).Sum(b => b.Percent);
                    if (percent != 0) amount = amount * (100 + percent) / 100;
                    int before = target.Hp;
                    target.Hp = Math.Min(target.MaxHp, target.Hp + Math.Max(0, amount));
                    Log(s, source.Def.Name + " heals " + (target.Hp - before) + ".");
                    break;
                }
                case Primitive.Shield:
                {
                    var target = OwnerCombatant(s, source);
                    if (target == null) return;
                    int amount = effect.Amount(source);
                    target.Shield += Math.Max(0, amount);
                    Log(s, source.Def.Name + " gains " + amount + " shield.");
                    break;
                }
                case Primitive.BuffNext:
                    s.PendingBuffs.Add(new PendingBuff { Kind = effect.BuffKind, Percent = effect.Percent(source) });
                    Log(s, source.Def.Name + " buffs the next card by +" + effect.Percent(source) + "%.");
                    break;
                case Primitive.RepeatNext:
                    s.PendingRepeat = Math.Max(s.PendingRepeat, Math.Max(0, effect.Count - 1));
                    Log(s, source.Def.Name + " repeats the next card.");
                    break;
                case Primitive.Flip:
                {
                    foreach (var target in FlipTargets(s, source, effect.Target))
                    {
                        ApplyFlip(s, target, effect.FlipType);
                    }
                    break;
                }
                case Primitive.SelfDamage:
                {
                    var target = OwnerCombatant(s, source);
                    if (target != null) ApplyDamage(s, target, Math.Max(0, effect.Flat), source.Def.Name + " recoil");
                    break;
                }
                case Primitive.Draw:
                    s.Deck.Draw(Math.Max(0, effect.Count));
                    break;
            }
        }

        static void ApplyDamage(CombatState s, Combatant target, int amount, string source)
        {
            if (target == null || amount <= 0) return;
            int remaining = amount;
            if (target.Shield > 0)
            {
                int absorbed = Math.Min(target.Shield, remaining);
                target.Shield -= absorbed;
                remaining -= absorbed;
            }
            if (remaining > 0) target.Hp = Math.Max(0, target.Hp - remaining);
            Log(s, source + " deals " + amount + " damage.");

            if (target == s.Player && target.Hp <= 0)
            {
                s.Phase = CombatPhase.Lost;
                Log(s, "Player defeated.");
            }
            CheckEnemyDeaths(s);
        }

        static void CheckEnemyDeaths(CombatState s)
        {
            foreach (var e in s.Enemies)
            {
                if (e.Alive || e.SlotIndex < 0) continue;
                var slot = GetSlot(s, e.SlotIndex);
                if (slot != null && slot.Enemy == e) slot.Enemy = null;
                e.SlotIndex = -1;
                e.Card.Zone = Zone.Removed;
                Log(s, e.Card.Def.Name + " is defeated.");
            }
        }

        static void ApplyFlip(CombatState s, CardInstance card, FlipType type)
        {
            if (card == null) return;
            switch (type)
            {
                case FlipType.Turn:
                    card.TurnFlip = !card.TurnFlip;
                    break;
                case FlipType.Level:
                    card.LevelFlip = !card.LevelFlip;
                    card.M++;
                    break;
                case FlipType.Permanent:
                    card.BaseReversed = !card.BaseReversed;
                    card.M++;
                    break;
            }
            Log(s, card.Def.Name + " flipped (" + type + ").");
        }

        static IEnumerable<CardInstance> FlipTargets(CombatState s, CardInstance source, TargetSelector target)
        {
            switch (target)
            {
                case TargetSelector.Self:
                    yield return source;
                    break;
                case TargetSelector.Adjacent:
                {
                    var slot = s.Sequence.Slots.FirstOrDefault(x => x.Card == source);
                    if (slot == null) yield break;
                    var left = GetSlot(s, slot.Index - 1);
                    var right = GetSlot(s, slot.Index + 1);
                    if (left?.Card != null) yield return left.Card;
                    if (right?.Card != null) yield return right.Card;
                    break;
                }
                case TargetSelector.SequenceEnds:
                {
                    var cards = s.Sequence.Slots.Where(x => x.Card != null).OrderBy(x => x.Index).ToList();
                    if (cards.Count > 0) yield return cards[0].Card;
                    if (cards.Count > 1) yield return cards[cards.Count - 1].Card;
                    break;
                }
                case TargetSelector.AllEnemies:
                    foreach (var e in s.Enemies.Where(e => e.Alive)) yield return e.Card;
                    break;
            }
        }

        static Combatant OwnerCombatant(CombatState s, CardInstance card)
        {
            if (card.Owner == Owner.Player) return s.Player;
            var enemy = s.Enemies.FirstOrDefault(e => e.Card == card);
            return enemy?.Stats;
        }

        static void ClearSequence(CombatState s)
        {
            foreach (var slot in s.Sequence.Slots)
            {
                if (slot.PlayerCard != null)
                {
                    slot.PlayerCard.Zone = Zone.Discard;
                    s.Deck.Discard.Add(slot.PlayerCard);
                }
                slot.PlayerCard = null;
                slot.Enemy = null;
                slot.IsPlayerSlot = true;
            }
        }

        static void ClearShields(CombatState s)
        {
            s.Player.Shield = 0;
            foreach (var e in s.Enemies) if (e.Stats != null) e.Stats.Shield = 0;
        }

        static void ClearTurnFlips(CombatState s)
        {
            foreach (var card in AllCards(s)) card.TurnFlip = false;
        }

        static void RestoreTransforms(CombatState s)
        {
            foreach (var card in AllCards(s))
            {
                if (card.IsTransformed) card.Restore();
            }
        }

        static IEnumerable<CardInstance> AllCards(CombatState s)
        {
            foreach (var c in s.Deck.DrawPile) yield return c;
            foreach (var c in s.Deck.Hand) yield return c;
            foreach (var c in s.Deck.Discard) yield return c;
            foreach (var slot in s.Sequence.Slots)
            {
                if (slot.PlayerCard != null) yield return slot.PlayerCard;
                if (slot.Enemy != null) yield return slot.Enemy.Card;
            }
            foreach (var e in s.Enemies) yield return e.Card;
        }

        static IEnumerable<CardInstance> AllPlayerCards(CombatState s)
        {
            foreach (var c in s.Deck.DrawPile) yield return c;
            foreach (var c in s.Deck.Hand) yield return c;
            foreach (var c in s.Deck.Discard) yield return c;
            foreach (var slot in s.Sequence.Slots)
            {
                if (slot.PlayerCard != null) yield return slot.PlayerCard;
            }
        }

        static void ApplyTurnStartTransforms(CombatState s)
        {
            var playerMagicians = s.Deck.Hand.Where(c => c.Def.Id == "Major_1").ToList();
            foreach (var magician in playerMagicians)
            {
                if (!magician.IsReversed)
                {
                    TransformCard(s, magician, TransformPool.ImplementedMajors);
                }
                else
                {
                    var targets = s.Deck.Hand.Where(c => c != magician).OrderBy(c => s.Rng.Next(1000)).Take(2).ToList();
                    foreach (var target in targets) TransformCard(s, target, TransformPool.ImplementedMinors);
                }
            }

            foreach (var enemy in s.Enemies.Where(e => e.Alive && e.Card.Def.Id == "Major_1"))
            {
                if (!enemy.Card.IsReversed)
                {
                    TransformCard(s, enemy.Card, TransformPool.ImplementedMajors);
                }
                else
                {
                    var targets = s.Deck.Hand.OrderBy(c => s.Rng.Next(1000)).Take(2).ToList();
                    foreach (var target in targets) TransformCard(s, target, TransformPool.ImplementedMinors);
                }
            }
        }

        static void TransformCard(CombatState s, CardInstance card, TransformPool poolType)
        {
            var onField = new HashSet<string>(s.Sequence.Slots.Where(x => x.Card != null).Select(x => x.Card.Def.Id));
            var pool = (poolType == TransformPool.ImplementedMajors ? CardCatalog.ImplementedMajors : CardCatalog.ImplementedMinors)
                .Where(d => d.Id != card.Def.Id && !onField.Contains(d.Id))
                .ToList();
            if (pool.Count == 0) return;
            var def = pool[s.Rng.Next(pool.Count)];
            card.TransformTo(def);
            Log(s, "The Magician transforms " + card.Def.Name + ".");
        }

        static void InsertEnemies(CombatState s)
        {
            var alive = s.Enemies.Where(e => e.Alive).ToList();
            foreach (var e in alive)
            {
                e.Placement = PlacementRule.None;
                if (e.Card.Def.Id == "Major_0")
                    e.Placement = e.Card.IsReversed ? PlacementRule.NextIsEnemy : PlacementRule.NoEnds;
                e.SlotIndex = -1;
            }

            foreach (var slot in s.Sequence.Slots)
            {
                slot.PlayerCard = null;
                slot.Enemy = null;
                slot.IsPlayerSlot = true;
            }

            if (alive.Count == 0) return;
            var chosen = new List<int>();
            bool valid = false;

            for (int attempt = 0; attempt < 300 && !valid; attempt++)
            {
                var indices = Enumerable.Range(0, 9).ToList();
                s.Rng.Shuffle(indices);
                chosen = indices.Take(alive.Count).ToList();
                valid = IsPlacementValid(alive, chosen);
            }

            if (!valid)
            {
                chosen = Enumerable.Range(0, alive.Count).ToList();
                Log(s, "Enemy placement constraints ignored.");
            }

            for (int i = 0; i < alive.Count && i < chosen.Count; i++)
            {
                int index = chosen[i];
                var enemy = alive[i];
                enemy.SlotIndex = index;
                enemy.Card.Zone = Zone.Sequence;
                s.Sequence.Slots[index].Enemy = enemy;
                s.Sequence.Slots[index].IsPlayerSlot = false;
            }
        }

        static bool IsPlacementValid(List<Enemy> enemies, List<int> chosen)
        {
            var set = new HashSet<int>(chosen);
            for (int i = 0; i < enemies.Count; i++)
            {
                int slot = chosen[i];
                if (enemies[i].Placement == PlacementRule.NoEnds && (slot == 0 || slot == 8)) return false;
                if (enemies[i].Placement == PlacementRule.NextIsEnemy)
                {
                    if (slot + 1 >= 9 || !set.Contains(slot + 1)) return false;
                }
            }
            return true;
        }

        static void Log(CombatState s, string message)
        {
            s.Log.Add(message);
            if (s.Log.Count > 60) s.Log.RemoveAt(0);
        }
    }
}
