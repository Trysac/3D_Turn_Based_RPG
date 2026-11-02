using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnBasedRpg.Core
{
    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat,
    }

    public enum ActionKind
    {
        Skill,
        Defend,
        Item,
    }

    /// <summary>What a fighter does on its turn. Area skills and self skills need no target.</summary>
    public sealed class BattleAction
    {
        BattleAction(ActionKind kind, SkillDef skill, ItemDef item, Combatant target)
        {
            Kind = kind;
            Skill = skill;
            Item = item;
            Target = target;
        }

        public ActionKind Kind { get; }
        public SkillDef Skill { get; }
        public ItemDef Item { get; }
        public Combatant Target { get; }

        public TargetKind TargetKind => Kind == ActionKind.Skill ? Skill.Target : Kind == ActionKind.Item ? Item.Target : TargetKind.Self;
        public string Name => Kind == ActionKind.Skill ? Skill.Name : Kind == ActionKind.Item ? Item.Name : "Defend";

        public static BattleAction UseSkill(SkillDef skill, Combatant target = null) => new BattleAction(ActionKind.Skill, skill, null, target);
        public static BattleAction Defend() => new BattleAction(ActionKind.Defend, null, null, null);
        public static BattleAction UseItem(ItemId item, Combatant target = null) => new BattleAction(ActionKind.Item, null, Catalog.Item(item), target);
    }

    /// <summary>One effect on one fighter, for the view to animate.</summary>
    public sealed class Hit
    {
        public Hit(Combatant target)
        {
            Target = target;
        }

        public Combatant Target { get; }
        public int Damage { get; internal set; }
        public int Healed { get; internal set; }
        public int MpRestored { get; internal set; }
        public bool Critical { get; internal set; }
        public StatusKind? StatusApplied { get; internal set; }
        public bool StatusResisted { get; internal set; }
        public bool Cured { get; internal set; }
        public bool Revived { get; internal set; }
        public bool Defeated { get; internal set; }
    }

    public sealed class ActionReport
    {
        internal ActionReport(Combatant actor, BattleAction action)
        {
            Actor = actor;
            Action = action;
        }

        public Combatant Actor { get; }
        public BattleAction Action { get; }
        public List<Hit> Hits { get; } = new List<Hit>();
    }

    public sealed class TurnStart
    {
        internal TurnStart(Combatant actor)
        {
            Actor = actor;
        }

        public Combatant Actor { get; }
        /// <summary>Poison and regen applied as the turn began.</summary>
        public List<Hit> Ticks { get; } = new List<Hit>();
        /// <summary>The actor was stunned, or fell to poison: its turn is already over.</summary>
        public bool Skipped { get; internal set; }
        public bool Stunned { get; internal set; }
    }

    /// <summary>
    /// One fight between the heroes and an enemy group. Every round, the living fighters act in
    /// order of speed (ties: heroes first, then by slot). Call <see cref="BeginTurn"/> for the
    /// fighter in <see cref="Current"/>, then <see cref="Execute"/> its action unless the turn was skipped.
    /// </summary>
    public sealed class Battle
    {
        readonly List<Combatant> heroes;
        readonly List<Combatant> enemies;
        readonly List<Combatant> queue = new List<Combatant>();
        readonly Rng rng;
        bool turnStarted;

        public Battle(IEnumerable<Combatant> heroes, IEnumerable<Combatant> enemies, Inventory inventory, Rng rng)
        {
            this.heroes = heroes.ToList();
            this.enemies = enemies.ToList();
            if (this.heroes.Count == 0 || this.enemies.Count == 0)
            {
                throw new ArgumentException("A battle needs at least one hero and one enemy.");
            }
            Inventory = inventory ?? new Inventory();
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            UpdateOutcome();
            if (Outcome == BattleOutcome.Ongoing)
            {
                StartRound();
            }
        }

        public IReadOnlyList<Combatant> Heroes => heroes;
        public IReadOnlyList<Combatant> Enemies => enemies;
        public IEnumerable<Combatant> All => heroes.Concat(enemies);
        public Inventory Inventory { get; }
        public int Round { get; private set; }
        /// <summary>Turns that ended so far, skipped ones included.</summary>
        public int TurnsTaken { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public bool IsOver => Outcome != BattleOutcome.Ongoing;
        public bool TurnStarted => turnStarted;

        /// <summary>The fighter whose turn it is, or null when the battle is over.</summary>
        public Combatant Current => IsOver ? null : queue[0];

        /// <summary>The fighters still to act this round, the current one first.</summary>
        public IReadOnlyList<Combatant> TurnQueue => queue;

        public IReadOnlyList<Combatant> AlliesOf(Combatant unit) => unit.Team == Team.Heroes ? heroes : enemies;
        public IReadOnlyList<Combatant> FoesOf(Combatant unit) => unit.Team == Team.Heroes ? enemies : heroes;

        /// <summary>Speed order for a round: fastest first, heroes before enemies on ties, then by slot.</summary>
        public static List<Combatant> OrderBySpeed(IEnumerable<Combatant> fighters) => fighters
            .Where(f => f.IsAlive)
            .OrderByDescending(f => f.Stats.Speed)
            .ThenBy(f => f.Team == Team.Heroes ? 0 : 1)
            .ThenBy(f => f.Slot)
            .ToList();

        /// <summary>Applies start-of-turn effects to <see cref="Current"/>. A stunned or poisoned-out fighter's turn ends here.</summary>
        public TurnStart BeginTurn()
        {
            if (IsOver)
            {
                throw new InvalidOperationException("The battle is over.");
            }
            if (turnStarted)
            {
                throw new InvalidOperationException("The turn has already begun.");
            }
            Combatant actor = queue[0];
            var start = new TurnStart(actor);
            actor.Remove(StatusKind.Defending);

            if (actor.Has(StatusKind.Poison))
            {
                var tick = new Hit(actor) { Damage = Formulas.PercentOf(actor.Stats.MaxHp, Formulas.PoisonPercent) };
                actor.Hp -= tick.Damage;
                tick.Defeated = !actor.IsAlive;
                start.Ticks.Add(tick);
            }
            if (actor.IsAlive && actor.Has(StatusKind.Regen))
            {
                int before = actor.Hp;
                actor.Hp += Formulas.PercentOf(actor.Stats.MaxHp, Formulas.RegenPercent);
                start.Ticks.Add(new Hit(actor) { Healed = actor.Hp - before });
            }

            if (!actor.IsAlive)
            {
                actor.ClearStatuses();
                start.Skipped = true;
                UpdateOutcome();
                FinishTurn(actor);
                return start;
            }
            if (actor.Has(StatusKind.Stun))
            {
                start.Skipped = true;
                start.Stunned = true;
                FinishTurn(actor);
                return start;
            }
            turnStarted = true;
            return start;
        }

        /// <summary>True if the current fighter has the MP for the skill and it is off cooldown.</summary>
        public bool CanUse(Combatant actor, SkillDef skill) =>
            actor.Mp >= skill.MpCost && actor.CooldownOf(skill) == 0 && ValidTargets(actor, skill.Target).Count > 0;

        public bool CanUse(Combatant actor, ItemId item) =>
            actor.Team == Team.Heroes && Inventory.Count(item) > 0 && ValidTargets(actor, Catalog.Item(item).Target).Count > 0;

        /// <summary>The fighters an action of this kind may be aimed at (for area kinds: everyone it hits).</summary>
        public IReadOnlyList<Combatant> ValidTargets(Combatant actor, TargetKind kind)
        {
            switch (kind)
            {
                case TargetKind.Foe:
                case TargetKind.AllFoes:
                    return FoesOf(actor).Where(f => f.IsAlive).ToList();
                case TargetKind.Ally:
                case TargetKind.AllAllies:
                    return AlliesOf(actor).Where(f => f.IsAlive).ToList();
                case TargetKind.FallenAlly:
                    return AlliesOf(actor).Where(f => !f.IsAlive).ToList();
                default:
                    return new[] { actor };
            }
        }

        /// <summary>Performs the current fighter's action and ends its turn.</summary>
        public ActionReport Execute(BattleAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (IsOver)
            {
                throw new InvalidOperationException("The battle is over.");
            }
            if (!turnStarted)
            {
                throw new InvalidOperationException("Call BeginTurn first.");
            }
            Combatant actor = queue[0];
            List<Combatant> targets = ResolveTargets(actor, action);
            var report = new ActionReport(actor, action);

            switch (action.Kind)
            {
                case ActionKind.Defend:
                    actor.Apply(StatusKind.Defending, 1);
                    int before = actor.Mp;
                    actor.Mp += Formulas.PercentOf(actor.Stats.MaxMp, Formulas.DefendMpPercent);
                    report.Hits.Add(new Hit(actor) { StatusApplied = StatusKind.Defending, MpRestored = actor.Mp - before });
                    break;
                case ActionKind.Skill:
                    actor.Mp -= action.Skill.MpCost;
                    actor.StartCooldown(action.Skill);
                    foreach (Combatant target in targets)
                    {
                        report.Hits.Add(ApplySkill(actor, target, action.Skill));
                    }
                    break;
                case ActionKind.Item:
                    Inventory.Take(action.Item.Id);
                    foreach (Combatant target in targets)
                    {
                        report.Hits.Add(ApplyItem(target, action.Item));
                    }
                    break;
            }

            UpdateOutcome();
            FinishTurn(actor);
            return report;
        }

        List<Combatant> ResolveTargets(Combatant actor, BattleAction action)
        {
            if (action.Kind == ActionKind.Skill && !CanUse(actor, action.Skill))
            {
                throw new InvalidOperationException($"{actor.Name} cannot use {action.Skill.Name} now.");
            }
            if (action.Kind == ActionKind.Item && !CanUse(actor, action.Item.Id))
            {
                throw new InvalidOperationException($"{actor.Name} cannot use {action.Item.Name} now.");
            }
            IReadOnlyList<Combatant> valid = ValidTargets(actor, action.TargetKind);
            switch (action.TargetKind)
            {
                case TargetKind.AllFoes:
                case TargetKind.AllAllies:
                case TargetKind.Self:
                    return valid.ToList();
                default:
                    if (action.Target == null || !valid.Contains(action.Target))
                    {
                        throw new InvalidOperationException($"{action.Target?.Name ?? "Nobody"} is not a valid target for {action.Name}.");
                    }
                    return new List<Combatant> { action.Target };
            }
        }

        Hit ApplySkill(Combatant actor, Combatant target, SkillDef skill)
        {
            var hit = new Hit(target);
            switch (skill.Effect)
            {
                case SkillEffect.Physical:
                case SkillEffect.Magical:
                    hit.Damage = Formulas.RollDamage(actor, target, skill, rng, out bool critical);
                    hit.Critical = critical;
                    target.Hp -= hit.Damage;
                    break;
                case SkillEffect.Heal:
                    int before = target.Hp;
                    target.Hp += Formulas.RollHeal(actor, skill, rng);
                    hit.Healed = target.Hp - before;
                    break;
            }
            if (!target.IsAlive)
            {
                hit.Defeated = true;
                target.ClearStatuses();
                return hit;
            }
            if (skill.Status.HasValue)
            {
                if (rng.Chance(skill.StatusChance))
                {
                    // A status cast on yourself would otherwise lose a turn at the end of this one.
                    target.Apply(skill.Status.Value, target == actor ? skill.StatusTurns + 1 : skill.StatusTurns);
                    hit.StatusApplied = skill.Status.Value;
                }
                else
                {
                    hit.StatusResisted = true;
                }
            }
            return hit;
        }

        Hit ApplyItem(Combatant target, ItemDef item)
        {
            var hit = new Hit(target);
            switch (item.Id)
            {
                case ItemId.Potion:
                    int hpBefore = target.Hp;
                    target.Hp += item.Amount;
                    hit.Healed = target.Hp - hpBefore;
                    break;
                case ItemId.Ether:
                    int mpBefore = target.Mp;
                    target.Mp += item.Amount;
                    hit.MpRestored = target.Mp - mpBefore;
                    break;
                case ItemId.Remedy:
                    bool cured = target.Remove(StatusKind.Poison);
                    cured |= target.Remove(StatusKind.Stun);
                    cured |= target.Remove(StatusKind.Weakness);
                    hit.Cured = cured;
                    break;
                case ItemId.Bomb:
                    hit.Damage = target.Has(StatusKind.Defending) ? item.Amount / 2 : item.Amount;
                    target.Hp -= hit.Damage;
                    if (!target.IsAlive)
                    {
                        hit.Defeated = true;
                        target.ClearStatuses();
                    }
                    break;
                case ItemId.Feather:
                    target.Hp = Formulas.PercentOf(target.Stats.MaxHp, item.Amount);
                    hit.Healed = target.Hp;
                    hit.Revived = true;
                    break;
            }
            return hit;
        }

        void FinishTurn(Combatant actor)
        {
            turnStarted = false;
            TurnsTaken++;
            if (actor.IsAlive)
            {
                actor.TickEndOfTurn();
            }
            queue.RemoveAt(0);
            if (IsOver)
            {
                return;
            }
            queue.RemoveAll(f => !f.IsAlive);
            if (queue.Count == 0)
            {
                StartRound();
            }
        }

        void StartRound()
        {
            Round++;
            queue.Clear();
            queue.AddRange(OrderBySpeed(All));
        }

        void UpdateOutcome()
        {
            if (enemies.All(e => !e.IsAlive))
            {
                Outcome = BattleOutcome.Victory;
            }
            else if (heroes.All(h => !h.IsAlive))
            {
                Outcome = BattleOutcome.Defeat;
            }
        }
    }
}
