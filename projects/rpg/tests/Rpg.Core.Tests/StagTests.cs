using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The sylvan stags (sprint 132, docs/familles-foret.md §3, words of docs/familles-foret-textes.md): the Forest of the
/// Ancestors' first family, whose lesson is reading the ground: glyphs that burn and weigh, blows that push onto them.
/// </summary>
public class StagTests
{
    private static MonsterFamily Stags => Real.Families["stag"];

    [Fact]
    public void TheStags_LiveInTheForest_FiveRoles_TheCommonOnesOf31To34_TheirChiefOf45()
    {
        Assert.Equal(("ancestors", "ancestors-0-4", "ancestors-trunks"), (Stags.Zone, Stags.BossMap, Stags.Battle));
        Assert.Equal([31, 32, 33, 34], Stags.Monsters.Where(m => m.Role != MonsterRole.Boss).Select(m => m.Level).Order());
        Assert.Equal(45, Stags[MonsterRole.Boss].Level);
        // The chief's map is at the forest's far end.
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns[Stags.BossMap!], [.. Real.Towns.Values]));
        Assert.Contains(Real.Groups.Values, g => g.Family == "stag" && g.Map == Stags.BossMap && g.Monsters.Contains("elder-stag"));
        // Each, its role's template at its level (T96): its initiative; its hit points, the common ones 17 % above (T41
        // measured at 31 with the craftsmen's pieces of 30, sprint 132), the chief's as they are.
        foreach (MonsterSpec m in Stags.Monsters)
        {
            MonsterTemplate t = Templates.Monster(m.Role, m.Level);
            Assert.Equal(Templates.Initiative(m.Role, m.Level), m.Initiative);
            if (m.Role == MonsterRole.Boss)
                Assert.Equal(t.Hp, m.Hp);
            else
                Assert.InRange(m.Hp, t.Hp * 1.15, t.Hp * 1.2);
        }
    }

    [Fact]
    public void TheHindAndTheHart_LayGlyphs_ThatBurnOrWeigh_AndTheChiefsRingAroundItself()
    {
        GlyphEffect Glyph(string spell) => Assert.Single(Real.Spells[spell].AllEffects.OfType<GlyphEffect>());
        Assert.Contains("scrape", Real.Monsters["carving-hind"].Spells);
        Assert.Equal((Affects.Enemies, 2, 7, 9, Element.Earth), (Glyph("scrape").Affects, Glyph("scrape").Turns, Glyph("scrape").DamageMin, Glyph("scrape").DamageMax, Glyph("scrape").Element));
        Assert.Contains("heavy-ground", Real.Monsters["old-hart"].Spells);
        Assert.Equal((0, Stat.Mp, -2, 1), (Glyph("heavy-ground").DamageMax, Glyph("heavy-ground").Status, Glyph("heavy-ground").Value, Glyph("heavy-ground").StatusTurns));
        // A glyph never takes all three movement points: one is always left to step off it.
        Assert.All(Stags.Monsters.SelectMany(m => m.Spells).Distinct().SelectMany(s => Real.Spells[s].AllEffects.OfType<GlyphEffect>()),
            g => Assert.True(g.Status != Stat.Mp || g.Value > -3));
        Spell ring = Real.Spells["ancestors-ring"];
        Assert.Equal((0, 0), (ring.MinRange, ring.MaxRange));
        Assert.Equal("Brame de l'aïeul", ring.Name.Fr);
        // The brocket's head toss and the chief's hook push and pull onto them.
        Assert.Contains(Real.Spells["head-toss"].AllEffects, e => e is PushEffect);
        Assert.Contains(Real.Spells["antler-hook"].AllEffects, e => e is PullEffect);
    }

    [Fact]
    public void TheChief_CracksOpenBelowHalfItsLife_OnAScrapedOutHollow()
    {
        PassiveSpec cracked = Real.Passives["cracked-antlers"];
        Assert.Equal((Trigger.LowHp, 50), (cracked.Trigger, cracked.Percent));
        // The scriptwriter's word: scraped out (a sign erased), never carved.
        Assert.Contains("creux gratté", cracked.Description.Fr, StringComparison.Ordinal);
        Assert.DoesNotContain("gravé", cracked.Description.Fr, StringComparison.Ordinal);
        Assert.Equal("Ancestor Stag", Stags[MonsterRole.Boss].Name.En);
    }

    [Fact]
    public void TheSet_HasFourPieces_OneByEachCommonMonsterButTheSupport_AndTheChiefsStaff()
    {
        ItemSet set = Real.Sets["stag"];
        Item[] pieces = [.. Real.Items.Values.Where(i => i.Set == "stag")];
        Assert.Equal(4, pieces.Length);
        Assert.Equal([2, 3, 4], set.Bonuses.Select(b => b.Pieces));
        Assert.Equal(1, set.Bonuses[^1].Mp);
        Assert.All(set.Bonuses, b => Assert.Equal(0, b.Ap));
        Assert.Equal((ItemSlot.TwoHanded, 45), (Real.Items["elder-antler-staff"].Slot, Real.Items["elder-antler-staff"].Level));
        foreach (MonsterSpec m in Stags.Monsters)
        {
            string[] drops = [.. (m.Loot ?? []).Select(l => l.Item).Where(i => Real.Items[i].Set == "stag")];
            int expected = m.Role switch { MonsterRole.Boss => 4, MonsterRole.Support => 0, _ => 1 };
            Assert.Equal(expected, drops.Length);
        }
        // The family's two resources, left by every monster.
        Assert.All(Stags.Monsters, m => Assert.Contains(m.Loot!, l => l.Item is "stone-antler" or "stag-velvet"));
    }

    [Fact]
    public void AFightAgainstStags_LaysGlyphs_AndReplaysIdentically_ThroughItsRecord()
    {
        // A group with the hind and the hart, played out by the AI against a hero of 31.
        MonsterGroup group = Real.Groups.Values.Where(g => g.Family == "stag" && g.Monsters.Contains("carving-hind") && g.Monsters.Contains("old-hart"))
            .OrderBy(g => g.Monsters.Count).ThenBy(g => g.Id, StringComparer.Ordinal).First();
        Hero hero = Progression.Built(new Hero("Essai", "female-d", "guard", Level: 31), Real);
        bool laid = false;
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var f = new Fight(Real, group.Scenario, seed, hero);
            Ai.PlayOut(f);
            laid |= f.Events.OfType<GlyphPlaced>().Any();
            Fight again = FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(Real);
            Assert.Equal(Fingerprint(f), Fingerprint(again));
        }
        Assert.True(laid, "no glyph laid in ten fights");
    }
}
