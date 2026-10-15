namespace Rpg.Core;

/// <summary>What a fighter did in a fight (sprint 87): damage dealt and taken, healing, shields, enemies finished off.</summary>
public sealed record FighterStats(int Fighter, int DamageDealt, int DamageTaken, int Healing, int Shielding, int Kills);

/// <summary>
/// The statistics of a fight, read from its events (sprint 87, D68): what follows a spell is its
/// caster's doing (the damage of a push included), what follows a passive its bearer's, and a summon's doing is its summoner's. Damage over
/// time, dealt at the start of a turn, is taken by its target without being dealt by anyone; a
/// shield's absorbed damage is not taken. One line per fighter who is not a summon, in the fight's order.
/// </summary>
public static class FightStats
{
    public static IReadOnlyList<FighterStats> Of(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        int Owner(int id) => fight.Fighters[id].Summoner is int s ? Owner(s) : id;
        int n = fight.Fighters.Count;
        int[] dealt = new int[n], taken = new int[n], healing = new int[n], shielding = new int[n], kills = new int[n];
        int? actor = null;
        foreach (FightEvent e in fight.Events)
        {
            switch (e)
            {
                case TurnStarted:
                    actor = null;
                    break;
                case SpellCast c:
                    actor = Owner(c.Fighter);
                    break;
                // What a passive does is its bearer's doing (sprint 90).
                case PassiveTriggered p:
                    actor = Owner(p.Fighter);
                    break;
                case Damaged d:
                    taken[Owner(d.Fighter)] += d.Amount;
                    if (actor is int by && fight.Fighters[by].Team != fight.Fighters[d.Fighter].Team)
                        dealt[by] += d.Amount;
                    break;
                case Healed h when actor is int healer:
                    healing[healer] += h.Amount;
                    break;
                case Shielded s when actor is int shielder:
                    shielding[shielder] += s.Amount;
                    break;
                case Died died when actor is int killer && !fight.Fighters[died.Fighter].IsSummon && fight.Fighters[killer].Team != fight.Fighters[died.Fighter].Team:
                    kills[killer]++;
                    break;
            }
        }
        return [.. fight.Fighters.Where(f => !f.IsSummon).Select(f => new FighterStats(f.Id, dealt[f.Id], taken[f.Id], healing[f.Id], shielding[f.Id], kills[f.Id]))];
    }
}
