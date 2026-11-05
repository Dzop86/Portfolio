# Sprint 193 : Osmose : le cadrage des dix classes, le banc de mesure

**Objectif :** Dix classes remplacent la Mage, la Garde et la Sentinelle : 220 classes en tout, 3 choix à 30, 2 à 50, 2 à 100, livrées par vagues de 4, 7 et 10 classes (D96).

**Goal:** Ten classes replace the Mage, the Guard and the Sentinel: 220 classes in all, 3 choices at 30, 2 at 50, 2 at 100, delivered in waves of 4, 7 and 10 classes (D96).

| Story | Points | État |
|---|---|---|
| En tant que Charles, le tableau des chiffres des 10 classes, les gabarits du rang 4 et un canon des classes réécrit par le scénariste, où mes noms l'emportent, sont écrits et relus (rpg) ; documents relus, scan de confidentialité. | 2 | Fait : canon à relire par Charles, sauf son §5 tranché |
| En tant que développeur, un budget de combats fixe en CI (20 000 au plus), un banc hors CI aux jalons (`Rpg.Sim --matrix`), la couverture des sorts et des paquets de référence par classe (rpg) ; xUnit, mesure hors CI. | 3 | Fait en partie : le budget est de 240 000 combats, pas de 20 000 (T153) |

**Ce qui est fait.** `classes-conception.md` (chiffres §1.2, gabarits §2.3), `classes-canon.md` écrit par le scénariste, Malandrin à la place de Roublard (5.2) ; `Bench`, `SyntheticFamily`, `Rpg.Sim --matrix --decks`, `tools/bench-decks.json`, `FightBudget`, couverture des sorts ; première mesure dans `docs/mesures-classes.md`.

**Ce qui n'est pas fait.** Le budget de 20 000 combats : un run en joue 233 875, la limite est à 240 000 et il reste 6 125 combats de marge. Les niveaux N0, les 12 compositions de N1 et la sortie des duels de la CI ne sont pas faits non plus.

**Ce qui a bougé.** 7 lignes de voies sur 24 sont concluantes : les ±8 de la Garde tiennent surtout parce que l'IA ne joue pas ses sorts de voie. Le paquet pèse autant que la voie, sans cause établie : à comprendre en S194.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
