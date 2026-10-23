using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The mossy keepers (sprint 133, docs/familles-foret.md §4, words of docs/familles-foret-textes.md): the Forest of the
/// Ancestors' second family, whose lesson is dislodging: glyphs that strengthen the keepers standing in them.
/// </summary>
public class GolemTests
{
    private static MonsterFamily Keepers => Real.Families["golem"];

    [Fact]
    public void TheKeepers_LiveInTheForest_TheCommonOnesOf34To37_TheirChiefOf45_OnBothMapsOfTheZone()
    {
        Assert.Equal(("ancestors", "ancestors-4-4"), (Keepers.Zone, Keepers.BossMap));
        Assert.Equal([34, 35, 36, 37], Keepers.Monsters.Where(m => m.Role != MonsterRole.Boss).Select(m => m.Level).Order());
        Assert.Equal(45, Keepers[MonsterRole.Boss].Level);
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns[Keepers.BossMap!], [.. Real.Towns.Values]));
        Assert.Contains(Real.Groups.Values, g => g.Family == "golem" && g.Map == Keepers.BossMap && g.Monsters.Contains("menhir-keeper"));
        // The stags and the keepers share the forest's two battle maps, each first on its own.
        Assert.Equal(["ancestors-stones", "ancestors-trunks"], Keepers.AllBattles);
        Assert.Equal(["ancestors-trunks", "ancestors-stones"], Real.Families["stag"].AllBattles);
        // The tank is the family's only slow one.
        Assert.Equal(2, Keepers[MonsterRole.Tank].Mp);
        foreach (MonsterSpec m in Keepers.Monsters)
            Assert.Equal(Templates.Initiative(m.Role, m.Level), m.Initiative);
    }

    [Fact]
    public void TheirGlyphs_StrengthenTheKeepers_NeverStrikeTheHeroes()
    {
        string[] laying = ["moss-circle", "standing-ring", "oath-ring"];
        foreach (string id in laying)
        {
            GlyphEffect g = Assert.Single(Real.Spells[id].AllEffects.OfType<GlyphEffect>());
            Assert.Equal((Affects.Allies, 0, 2), (g.Affects, g.DamageMax, g.StatusTurns));
            Assert.Equal(20, g.Value);
        }
        Assert.Equal(Stat.Damage, Real.Spells["standing-ring"].AllEffects.OfType<GlyphEffect>().Single().Status);
        // The scriptwriter's names: no fourth "Circle", no "raised" stones (kept for the quest of the four standing stones).
        Assert.Equal(("Mousse hérissée", "Bristling Moss"), (Real.Spells["standing-ring"].Name.Fr, Real.Spells["standing-ring"].Name.En));
    }

    [Fact]
    public void AFallenKeeper_SheltersTheOthers_AndTheMenhirsHeart_RaisesTwoStones()
    {
        PassiveSpec cairn = Real.Passives["cairn"];
        Assert.Equal((Trigger.AllyDies, 3), (cairn.Trigger, cairn.Range));
        Assert.All(Keepers.Monsters, m => Assert.Contains("cairn", m.Passives!));
        PassiveSpec heart = Real.Passives["carved-heart"];
        Assert.Equal((Trigger.LowHp, 50), (heart.Trigger, heart.Percent));
        SummonEffect raise = Assert.Single(heart.Effects!.OfType<SummonEffect>());
        Assert.Equal(("pebbling", 2), (raise.Summon, raise.Count));
        // "Caillou" alone is the mage's spell: the risen stone is "awakened".
        Assert.Equal("Caillou éveillé", Real.Summons["pebbling"].Name.Fr);
        // The heart says nothing of what is carved on it: Bérold reads it (mossy-keepers, sprint 136).
        Assert.DoesNotContain("signe", heart.Description.Fr, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSet_HasFourPieces_TheShieldAndTheChiefsMace_AgainstTheStagsTwoHandedStaff()
    {
        ItemSet set = Real.Sets["golem"];
        Assert.Equal([2, 3, 4], set.Bonuses.Select(b => b.Pieces));
        Assert.All(set.Bonuses, b => Assert.Equal((0, 0), (b.Ap, b.Mp)));
        Assert.Equal([ItemSlot.Ring, ItemSlot.Belt, ItemSlot.Shield, ItemSlot.OneHanded], Real.Items.Values.Where(i => i.Set == "golem").Select(i => i.Slot));
        foreach (MonsterSpec m in Keepers.Monsters)
        {
            int drops = (m.Loot ?? []).Count(l => Real.Items[l.Item].Set == "golem");
            Assert.Equal(m.Role switch { MonsterRole.Boss => 4, MonsterRole.Support => 0, _ => 1 }, drops);
        }
    }

    [Fact]
    public void AFightAgainstKeepers_TheirGlyphsTouchOnlyTheirSide_AndItReplaysIdentically()
    {
        MonsterGroup group = Real.Groups.Values.Where(g => g.Family == "golem" && g.Monsters.Contains("slab-keeper") && g.Monsters.Contains("moss-tender"))
            .OrderBy(g => g.Monsters.Count).ThenBy(g => g.Id, StringComparer.Ordinal).First();
        Hero hero = Progression.Built(new Hero("Essai", "female-d", "guard", Level: 35), Real);
        int strengthened = 0;
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var f = new Fight(Real, group.Scenario, seed, hero);
            Ai.PlayOut(f);
            Dictionary<int, int> casterOf = f.Events.OfType<GlyphPlaced>().GroupBy(p => p.Glyph).ToDictionary(g => g.Key, g => g.Last().Caster);
            foreach (GlyphTriggered t in f.Events.OfType<GlyphTriggered>())
            {
                Assert.Equal(f.Fighters[casterOf[t.Glyph]].Team, f.Fighters[t.Fighter].Team);
                strengthened++;
            }
            Assert.Equal(Fingerprint(f), Fingerprint(FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(Real)));
        }
        Assert.True(strengthened > 0, "no keeper strengthened by a glyph in ten fights");
    }
}
