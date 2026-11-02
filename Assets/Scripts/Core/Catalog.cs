using System;
using System.Collections.Generic;

namespace TurnBasedRpg.Core
{
    /// <summary>Every hero, enemy, skill and item in the game, with its tuning.</summary>
    public static class Catalog
    {
        public static readonly SkillDef Attack = new SkillDef("attack", "Attack", "A plain weapon strike.", 0, 0,
            TargetKind.Foe, SkillEffect.Physical, 1.0);

        // ---- Hero skills ----
        public static readonly SkillDef ShieldBash = new SkillDef("shield_bash", "Shield Bash", "A strike that may stun (60%).", 5, 2,
            TargetKind.Foe, SkillEffect.Physical, 1.1, StatusKind.Stun, 1, 0.6);
        public static readonly SkillDef Cleave = new SkillDef("cleave", "Cleave", "A wide swing at every foe.", 6, 2,
            TargetKind.AllFoes, SkillEffect.Physical, 0.7);
        public static readonly SkillDef WarCry = new SkillDef("war_cry", "War Cry", "The party gains Strength for 3 turns.", 8, 4,
            TargetKind.AllAllies, SkillEffect.None, 0, StatusKind.Strength, 3);
        public static readonly SkillDef Fireball = new SkillDef("fireball", "Fireball", "Fire damage to every foe.", 10, 2,
            TargetKind.AllFoes, SkillEffect.Magical, 0.85);
        public static readonly SkillDef VenomBolt = new SkillDef("venom_bolt", "Venom Bolt", "A bolt that poisons (90%).", 6, 0,
            TargetKind.Foe, SkillEffect.Magical, 0.8, StatusKind.Poison, 3, 0.9);
        public static readonly SkillDef ArcaneLance = new SkillDef("arcane_lance", "Arcane Lance", "A heavy hit that weakens defense.", 14, 3,
            TargetKind.Foe, SkillEffect.Magical, 1.9, StatusKind.Weakness, 2);
        public static readonly SkillDef Mend = new SkillDef("mend", "Mend", "Heals one ally.", 6, 0,
            TargetKind.Ally, SkillEffect.Heal, 2.2);
        public static readonly SkillDef Sanctuary = new SkillDef("sanctuary", "Sanctuary", "Heals the party and grants Regen.", 14, 4,
            TargetKind.AllAllies, SkillEffect.Heal, 0.8, StatusKind.Regen, 3);
        public static readonly SkillDef Smite = new SkillDef("smite", "Smite", "Holy light against one foe.", 5, 1,
            TargetKind.Foe, SkillEffect.Magical, 1.2);

        // ---- Enemy skills ----
        public static readonly SkillDef AcidSplash = new SkillDef("acid_splash", "Acid Splash", "Corrosive goo that poisons.", 0, 3,
            TargetKind.Foe, SkillEffect.Physical, 0.8, StatusKind.Poison, 3, 0.7);
        public static readonly SkillDef Stab = new SkillDef("stab", "Backstab", "A quick, vicious stab.", 0, 2,
            TargetKind.Foe, SkillEffect.Physical, 1.5);
        public static readonly SkillDef DarkMend = new SkillDef("dark_mend", "Dark Mend", "Heals one ally.", 8, 1,
            TargetKind.Ally, SkillEffect.Heal, 1.8);
        public static readonly SkillDef Hex = new SkillDef("hex", "Hex", "A curse that weakens defense.", 6, 2,
            TargetKind.Foe, SkillEffect.Magical, 0.6, StatusKind.Weakness, 2);
        public static readonly SkillDef Howl = new SkillDef("howl", "Howl", "The pack gains Strength.", 0, 4,
            TargetKind.AllAllies, SkillEffect.None, 0, StatusKind.Strength, 2);
        public static readonly SkillDef BoneCrush = new SkillDef("bone_crush", "Bone Crush", "A crushing blow that may stun.", 0, 3,
            TargetKind.Foe, SkillEffect.Physical, 1.5, StatusKind.Stun, 1, 0.35);
        public static readonly SkillDef ShadowBolt = new SkillDef("shadow_bolt", "Shadow Bolt", "A bolt of darkness.", 5, 0,
            TargetKind.Foe, SkillEffect.Magical, 1.1);
        public static readonly SkillDef Wail = new SkillDef("wail", "Wail", "A scream that hurts and may weaken.", 12, 3,
            TargetKind.AllFoes, SkillEffect.Magical, 0.6, StatusKind.Weakness, 2, 0.5);
        public static readonly SkillDef Spark = new SkillDef("spark", "Spark", "A small burst of flame.", 0, 0,
            TargetKind.Foe, SkillEffect.Magical, 1.0);
        public static readonly SkillDef FlameWave = new SkillDef("flame_wave", "Flame Wave", "Fire washes over every foe.", 0, 2,
            TargetKind.AllFoes, SkillEffect.Magical, 0.9);
        public static readonly SkillDef MoltenFist = new SkillDef("molten_fist", "Molten Fist", "A burning punch that may stun.", 0, 3,
            TargetKind.Foe, SkillEffect.Physical, 1.7, StatusKind.Stun, 1, 0.5);
        public static readonly SkillDef InfernoRage = new SkillDef("inferno_rage", "Inferno Rage", "Strength for the rest of the fight.", 0, 99,
            TargetKind.Self, SkillEffect.None, 0, StatusKind.Strength, 99);

        // ---- Heroes ----
        public static readonly UnitDef Knight = new UnitDef("knight", "Aldric", "Knight",
            new Stats(120, 30, 18, 14, 4, 8), new Stats(14, 3, 3, 2, 0, 1), new[] { Attack, ShieldBash, Cleave, WarCry });
        public static readonly UnitDef Mage = new UnitDef("mage", "Lyra", "Mage",
            new Stats(80, 60, 7, 8, 21, 11), new Stats(8, 7, 1, 1, 3, 1), new[] { Attack, Fireball, VenomBolt, ArcaneLance });
        public static readonly UnitDef Cleric = new UnitDef("cleric", "Bram", "Cleric",
            new Stats(88, 50, 10, 9, 15, 10), new Stats(9, 6, 1, 1, 2, 1), new[] { Attack, Mend, Sanctuary, Smite });

        // ---- Enemies ----
        public static readonly UnitDef Slime = new UnitDef("slime", "Slime", "Ooze",
            new Stats(38, 0, 11, 3, 4, 5), new Stats(6, 0, 2, 1, 1, 0), new[] { Attack, AcidSplash }, xpReward: 8);
        public static readonly UnitDef Goblin = new UnitDef("goblin", "Goblin", "Skirmisher",
            new Stats(46, 0, 14, 5, 3, 12), new Stats(7, 0, 2, 1, 0, 1), new[] { Attack, Stab }, xpReward: 10);
        public static readonly UnitDef GoblinShaman = new UnitDef("goblin_shaman", "Goblin Shaman", "Healer",
            new Stats(40, 40, 7, 4, 13, 9), new Stats(6, 5, 1, 1, 2, 1), new[] { Attack, DarkMend, Hex }, xpReward: 12);
        public static readonly UnitDef Wolf = new UnitDef("wolf", "Dire Wolf", "Beast",
            new Stats(52, 0, 16, 5, 2, 15), new Stats(7, 0, 2, 1, 0, 1), new[] { Attack, Howl }, xpReward: 11);
        public static readonly UnitDef Skeleton = new UnitDef("skeleton", "Skeleton", "Brute",
            new Stats(70, 0, 17, 12, 2, 7), new Stats(8, 0, 2, 2, 0, 0), new[] { Attack, BoneCrush }, xpReward: 13);
        public static readonly UnitDef Wraith = new UnitDef("wraith", "Wraith", "Caster",
            new Stats(60, 50, 9, 6, 18, 13), new Stats(6, 6, 1, 1, 2, 1), new[] { Attack, ShadowBolt, Wail }, xpReward: 14);
        public static readonly UnitDef Imp = new UnitDef("imp", "Ember Imp", "Minion",
            new Stats(34, 0, 12, 4, 12, 14), new Stats(5, 0, 1, 1, 2, 1), new[] { Attack, Spark }, xpReward: 9);
        public static readonly UnitDef CinderKing = new UnitDef("cinder_king", "The Cinder King", "Boss",
            new Stats(520, 0, 24, 14, 24, 10), new Stats(40, 0, 2, 1, 2, 0), new[] { Attack, FlameWave, MoltenFist, InfernoRage },
            xpReward: 60, isBoss: true);

        /// <summary>The three heroes, in party order.</summary>
        public static readonly IReadOnlyList<UnitDef> Party = new[] { Knight, Mage, Cleric };

        public static readonly IReadOnlyList<UnitDef> Units = new[]
        {
            Knight, Mage, Cleric, Slime, Goblin, GoblinShaman, Wolf, Skeleton, Wraith, Imp, CinderKing,
        };

        /// <summary>Indexed by <see cref="ItemId"/>.</summary>
        public static readonly IReadOnlyList<ItemDef> Items = new[]
        {
            new ItemDef(ItemId.Potion, "Potion", "Restores 60 HP to one ally.", TargetKind.Ally, 60),
            new ItemDef(ItemId.Ether, "Ether", "Restores 25 MP to one ally.", TargetKind.Ally, 25),
            new ItemDef(ItemId.Remedy, "Remedy", "Cures poison, stun and weakness.", TargetKind.Ally, 0),
            new ItemDef(ItemId.Bomb, "Fire Bomb", "Deals 35 damage to every foe.", TargetKind.AllFoes, 35),
            new ItemDef(ItemId.Feather, "Ember Feather", "Revives a fallen ally with 40% HP.", TargetKind.FallenAlly, 40),
        };

        public static UnitDef Unit(string id)
        {
            foreach (UnitDef unit in Units)
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }
            throw new ArgumentException($"Unknown unit '{id}'.", nameof(id));
        }

        public static ItemDef Item(ItemId id) => Items[(int)id];
    }
}
