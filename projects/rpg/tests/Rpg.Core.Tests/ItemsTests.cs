using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Equipment, sets and loot (sprint 59).</summary>
public class ItemsTests
{
    private static Dictionary<Slot, string> Worn(params (Slot, string)[] items) => items.ToDictionary(i => i.Item1, i => i.Item2);

    [Fact]
    public void EveryOneOfTheFourteenSlots_HasSomethingToWear()
    {
        Assert.Equal(14, Enum.GetValues<Slot>().Length);
        foreach (Slot slot in Enum.GetValues<Slot>())
            Assert.Contains(Real.Items.Values, i => i.Slots.Contains(slot));
        foreach (ItemKind kind in Enum.GetValues<ItemKind>())
            Assert.Contains(Real.Items.Values, i => i.Kind == kind);
        Assert.All(Real.Items.Values, i => Assert.False(string.IsNullOrWhiteSpace(i.Name.Fr) || string.IsNullOrWhiteSpace(i.Name.En), i.Id));
    }

    [Fact]
    public void WhatIsWorn_GoesInItsSlot_AtItsLevel_ATwoHandedWeaponAlone()
    {
        Assert.Null(Equipment.Problem(Worn((Slot.Ring1, "copper-ring"), (Slot.Ring2, "copper-ring"), (Slot.OneHanded, "dagger"), (Slot.Shield, "wooden-shield")), 1, Real));
        Assert.Contains("does not go in", Equipment.Problem(Worn((Slot.Hat, "copper-ring")), 1, Real), StringComparison.Ordinal);
        Assert.Contains("needs level 8", Equipment.Problem(Worn((Slot.TwoHanded, "orc-club")), 7, Real), StringComparison.Ordinal);
        Assert.Null(Equipment.Problem(Worn((Slot.TwoHanded, "orc-club")), 8, Real));
        Assert.Contains("two-handed", Equipment.Problem(Worn((Slot.TwoHanded, "orc-club"), (Slot.Shield, "wooden-shield")), 8, Real), StringComparison.Ordinal);
        Assert.Contains("two-handed", Equipment.Problem(Worn((Slot.TwoHanded, "orc-club"), (Slot.OneHanded, "dagger")), 8, Real), StringComparison.Ordinal);
        Assert.Contains("Unknown item", Equipment.Problem(Worn((Slot.Pet, "dragon")), 1, Real), StringComparison.Ordinal);
        Assert.Contains("does not go in", Equipment.Problem(Worn((Slot.Belt, "bread")), 1, Real), StringComparison.Ordinal);
    }

    [Fact]
    public void ASet_GivesItsBestBonus_ForThePiecesWorn()
    {
        Assert.Equal((new Characteristics(Agility: 6), 0, 0), Equipment.Total(Worn((Slot.Cape, "poacher-cape")), Real));
        Assert.Equal((new Characteristics(Agility: 20), 0, 0), Equipment.Total(Worn((Slot.Cape, "poacher-cape"), (Slot.Boots, "poacher-boots")), Real));
        Assert.Equal((new Characteristics(Vitality: 8, Agility: 30), 0, 1), Equipment.Total(Worn((Slot.Cape, "poacher-cape"), (Slot.Boots, "poacher-boots"), (Slot.Hat, "poacher-hat")), Real));
        Assert.Equal((Characteristics.None, 0, 0), Equipment.Total(null, Real));
    }

    [Fact]
    public void InAFight_WhatTheHeroWearsCounts()
    {
        HeroClass guard = Real.Class("guard")!;
        var hero = new Hero("Élise", "female-c", "guard", Level: 20, Stats: new Characteristics(Vitality: 10),
            Worn: Worn((Slot.Cape, "poacher-cape"), (Slot.Boots, "poacher-boots"), (Slot.Hat, "poacher-hat"), (Slot.Mount, "donkey")));
        Assert.Null(hero.Problem(Real));
        Fighter f = new Fight(Real, "duel", 1, hero).Fighters[0];
        Assert.Equal(guard.Hp + guard.HpPerLevel * 19 + 10 + 8, f.MaxHp);
        Assert.Equal((guard.Ap, guard.Mp + 2, 30), (f.Spec.Ap, f.Spec.Mp, f.Spec.Characteristics.Agility));
        Assert.NotNull((hero with { Level = 19 }).Problem(Real));
    }

    [Fact]
    public void Loot_FollowsTheSeed_OnlyFromAWonFight_AtAboutItsChances()
    {
        var counts = new Dictionary<string, int>();
        int wins = 0;
        for (ulong seed = 0; seed < 400; seed++)
        {
            var f = new Fight(Real, "training", seed, new Hero("Élise", "female-c", "sentinel"));
            Ai.PlayOut(f);
            IReadOnlyList<ItemCount> loot = Equipment.Loot(f);
            Assert.Equal(loot, Equipment.Loot(FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(Real)));
            if (f.WinningTeam != 0)
            {
                Assert.Empty(loot);
                continue;
            }
            wins++;
            foreach (ItemCount c in loot)
                counts[c.Item] = counts.GetValueOrDefault(c.Item) + c.Count;
        }
        Assert.True(wins > 200);
        Assert.InRange(counts["orc-fang"] * 100.0 / wins, 50, 70);
        Assert.InRange(counts["orc-club"] * 100.0 / wins, 1, 10);
        Assert.Empty(Equipment.Loot(new Fight(Real, "training", 1)));
    }

    [Fact]
    public void WhatIsWorn_TravelsInTheRecord_AndComparesByContent()
    {
        var hero = new Hero("Élise", "female-c", "mage", Worn: Worn((Slot.Ring1, "copper-ring"), (Slot.Pet, "kitten")));
        Assert.Equal(hero, hero with { Worn = Worn((Slot.Pet, "kitten"), (Slot.Ring1, "copper-ring")) });
        Assert.NotEqual(hero, hero with { Worn = Worn((Slot.Ring2, "copper-ring"), (Slot.Pet, "kitten")) });
        var f = new Fight(Real, "training", 2, hero);
        Ai.PlayOut(f);
        Assert.Equal(hero, FightRecord.FromJson(FightRecord.Of(f).ToJson()).Hero);
    }

    [Fact]
    public void InconsistentItems_AreRefused()
    {
        Item ring = Real.Items["copper-ring"];
        GameData With(IEnumerable<Item> items, IEnumerable<ItemSet>? sets = null) => new(Spells, [], [], null, null, null, null, null, items, sets);
        Assert.Throws<InvalidDataException>(() => With([ring with { Slot = null }]));
        Assert.Throws<InvalidDataException>(() => With([ring with { Kind = ItemKind.Resource }]));
        Assert.Throws<InvalidDataException>(() => With([ring with { Level = 0 }]));
        Assert.Throws<InvalidDataException>(() => With([ring with { Set = "nowhere" }]));
        Assert.Throws<InvalidDataException>(() => With([ring], [new ItemSet("s", Text, [new SetBonus(1)])]));
    }
}
