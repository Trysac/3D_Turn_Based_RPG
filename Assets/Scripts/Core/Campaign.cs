using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnBasedRpg.Core
{
    public sealed class EnemySpawn
    {
        public EnemySpawn(UnitDef def, int level)
        {
            Def = def;
            Level = level;
        }

        public UnitDef Def { get; }
        public int Level { get; }
    }

    /// <summary>One battle on the campaign map.</summary>
    public sealed class Encounter
    {
        public Encounter(string name, string blurb, bool isBoss, params EnemySpawn[] enemies)
        {
            Name = name;
            Blurb = blurb;
            IsBoss = isBoss;
            Enemies = enemies;
        }

        public string Name { get; }
        public string Blurb { get; }
        public bool IsBoss { get; }
        public IReadOnlyList<EnemySpawn> Enemies { get; }

        public int XpReward => Enemies.Sum(e => e.Def.XpReward * e.Level);
    }

    public enum CampChoice
    {
        /// <summary>Revive the fallen and restore 60% of max HP and MP to everyone.</summary>
        Rest,
        /// <summary>Two potions and an ether.</summary>
        Supplies,
        /// <summary>Two fire bombs and an ember feather.</summary>
        Arsenal,
    }

    public sealed class BattleSummary
    {
        public bool Won { get; internal set; }
        public int Xp { get; internal set; }
        public int Turns { get; internal set; }
        public List<LevelUp> LevelUps { get; } = new List<LevelUp>();
        public bool CampaignComplete { get; internal set; }
    }

    /// <summary>The road to the Cinder Throne: five battles, the last one against the boss.</summary>
    public static class Campaign
    {
        public const int CampRestPercent = 60;

        public static readonly IReadOnlyList<Encounter> Encounters = new[]
        {
            new Encounter("Mossy Meadow", "Slimes ooze across the old road.", false,
                new EnemySpawn(Catalog.Slime, 1), new EnemySpawn(Catalog.Goblin, 1), new EnemySpawn(Catalog.Slime, 1)),
            new Encounter("Goblin Ridge", "A raiding party and its shaman.", false,
                new EnemySpawn(Catalog.Goblin, 2), new EnemySpawn(Catalog.GoblinShaman, 2), new EnemySpawn(Catalog.Goblin, 2)),
            new Encounter("Howling Woods", "Dire wolves hunt in packs.", false,
                new EnemySpawn(Catalog.Wolf, 4), new EnemySpawn(Catalog.GoblinShaman, 3), new EnemySpawn(Catalog.Wolf, 4)),
            new Encounter("Sunken Crypt", "The dead guard the way down.", false,
                new EnemySpawn(Catalog.Skeleton, 5), new EnemySpawn(Catalog.Wraith, 5), new EnemySpawn(Catalog.Skeleton, 5)),
            new Encounter("Cinder Throne", "The Cinder King and his imps.", true,
                new EnemySpawn(Catalog.Imp, 5), new EnemySpawn(Catalog.CinderKing, 6), new EnemySpawn(Catalog.Imp, 5)),
        };
    }

    /// <summary>
    /// A campaign in progress: the party, its items and how far it got. A lost battle changes
    /// nothing but the attempt counter and the turn count, so it can simply be fought again.
    /// </summary>
    public sealed class Run
    {
        public Run(int seed, IEnumerable<PartyMember> party, Inventory inventory)
        {
            Seed = seed;
            Party = party.ToList();
            Inventory = inventory;
        }

        public int Seed { get; }
        public List<PartyMember> Party { get; }
        public Inventory Inventory { get; }
        /// <summary>Index of the next encounter; equals the encounter count once the boss falls.</summary>
        public int Stage { get; internal set; }
        /// <summary>Losses at the current stage, which change the battle's random seed.</summary>
        public int Attempt { get; internal set; }
        /// <summary>Turns fought over the whole run, lost battles included.</summary>
        public int TotalTurns { get; internal set; }
        /// <summary>True between a victory and the camp choice.</summary>
        public bool AwaitingCamp { get; internal set; }

        public bool IsComplete => Stage >= Campaign.Encounters.Count;
        public Encounter CurrentEncounter => IsComplete ? null : Campaign.Encounters[Stage];

        public static Run New(int seed) => new Run(seed, Catalog.Party.Select(def => new PartyMember(def)), Inventory.Starting());

        /// <summary>The seed of the battle at the current stage and attempt.</summary>
        public int BattleSeed => unchecked(Seed * 31 + Stage * 7919 + Attempt * 104729);

        /// <summary>Sets up the current encounter. Items are spent from a copy until the battle is won.</summary>
        public Battle StartBattle()
        {
            if (IsComplete)
            {
                throw new InvalidOperationException("The campaign is complete.");
            }
            if (AwaitingCamp)
            {
                throw new InvalidOperationException("Choose a camp reward first.");
            }
            IEnumerable<Combatant> heroes = Party.Select((m, i) => new Combatant(m.Def, Team.Heroes, i, m.Level, m.Hp, m.Mp));
            IEnumerable<Combatant> enemies = CurrentEncounter.Enemies.Select((e, i) => new Combatant(e.Def, Team.Enemies, i, e.Level));
            return new Battle(heroes, enemies, Inventory.Clone(), new Rng(BattleSeed));
        }

        /// <summary>
        /// Records a finished battle. A victory keeps the heroes' HP, MP and items, gives the
        /// encounter's experience to every hero who stood at the end (half to the fallen), revives
        /// the fallen with 1 HP and moves to the next stage. A defeat refills the party's HP and
        /// MP, refunds the items and counts an attempt.
        /// </summary>
        public BattleSummary Finish(Battle battle)
        {
            if (!battle.IsOver)
            {
                throw new InvalidOperationException("The battle is not over.");
            }
            var summary = new BattleSummary { Turns = battle.TurnsTaken };
            TotalTurns += battle.TurnsTaken;
            if (battle.Outcome == BattleOutcome.Defeat)
            {
                // The party regroups at full strength, so a lost battle can never lock the run.
                foreach (PartyMember member in Party)
                {
                    member.Hp = member.Stats.MaxHp;
                    member.Mp = member.Stats.MaxMp;
                }
                Attempt++;
                return summary;
            }

            summary.Won = true;
            summary.Xp = CurrentEncounter.XpReward;
            for (int i = 0; i < Party.Count; i++)
            {
                PartyMember member = Party[i];
                Combatant fighter = battle.Heroes[i];
                bool stood = fighter.IsAlive;
                member.Hp = stood ? fighter.Hp : 1;
                member.Mp = fighter.Mp;
                summary.LevelUps.AddRange(Progression.GainXp(member, stood ? summary.Xp : summary.Xp / 2));
            }
            CopyItems(battle.Inventory);
            Stage++;
            Attempt = 0;
            summary.CampaignComplete = IsComplete;
            AwaitingCamp = !IsComplete;
            return summary;
        }

        public void Camp(CampChoice choice)
        {
            if (!AwaitingCamp)
            {
                throw new InvalidOperationException("There is no camp to rest at now.");
            }
            switch (choice)
            {
                case CampChoice.Rest:
                    foreach (PartyMember member in Party)
                    {
                        Stats stats = member.Stats;
                        member.Hp = Math.Min(stats.MaxHp, member.Hp + stats.MaxHp * Campaign.CampRestPercent / 100);
                        member.Mp = Math.Min(stats.MaxMp, member.Mp + stats.MaxMp * Campaign.CampRestPercent / 100);
                    }
                    break;
                case CampChoice.Supplies:
                    Inventory.Add(ItemId.Potion, 2);
                    Inventory.Add(ItemId.Ether, 1);
                    break;
                case CampChoice.Arsenal:
                    Inventory.Add(ItemId.Bomb, 2);
                    Inventory.Add(ItemId.Feather, 1);
                    break;
            }
            AwaitingCamp = false;
        }

        void CopyItems(Inventory from)
        {
            foreach (ItemDef item in Catalog.Items)
            {
                int difference = from.Count(item.Id) - Inventory.Count(item.Id);
                for (; difference < 0; difference++)
                {
                    Inventory.Take(item.Id);
                }
                if (difference > 0)
                {
                    Inventory.Add(item.Id, difference);
                }
            }
        }

        internal void Restore(int stage, int attempt, int totalTurns, bool awaitingCamp)
        {
            Stage = Math.Max(0, Math.Min(Campaign.Encounters.Count, stage));
            Attempt = Math.Max(0, attempt);
            TotalTurns = Math.Max(0, totalTurns);
            AwaitingCamp = awaitingCamp && !IsComplete;
        }
    }

    /// <summary>The best results on this device.</summary>
    public sealed class Records
    {
        public int Clears { get; internal set; }
        /// <summary>Fewest turns of a finished campaign; 0 until one is finished.</summary>
        public int BestTurns { get; internal set; }
        /// <summary>The furthest stage reached (number of battles won in one run).</summary>
        public int FurthestStage { get; internal set; }

        /// <summary>Updates the records with a run; true when the run set a new best clear.</summary>
        public bool Register(Run run)
        {
            FurthestStage = Math.Max(FurthestStage, run.Stage);
            if (!run.IsComplete)
            {
                return false;
            }
            Clears++;
            if (BestTurns == 0 || run.TotalTurns < BestTurns)
            {
                BestTurns = run.TotalTurns;
                return true;
            }
            return false;
        }

        internal void Restore(int clears, int bestTurns, int furthest)
        {
            Clears = Math.Max(0, clears);
            BestTurns = Math.Max(0, bestTurns);
            FurthestStage = Math.Max(0, Math.Min(Campaign.Encounters.Count, furthest));
        }
    }
}
