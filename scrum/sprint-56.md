# Sprint 56 : Osmose, les trois classes refaites

**Objectif :** La Sentinelle, la Garde et la Mage avec une vingtaine de sorts chacune, sur au moins deux éléments, équilibrées par simulation.

**Goal:** The Sentinel, the Guard and the Mage with about twenty spells each, on at least two elements, balanced by simulation.

| Story | Points | État |
|---|---|---|
| En tant que joueur, ma classe a une vraie palette de sorts (rpg) : une vingtaine de sorts par classe (environ 60, français et anglais), débloqués du niveau 1 au niveau 100, au moins deux éléments par classe, rôles distincts ; équilibre mesuré par simulation à plusieurs niveaux ; écran de création qui montre les sorts. | 3 | Fait (équilibre atteint au niveau 1 seulement) |

**Résultat :** 62 sorts, 20 par classe sur deux éléments (Sentinelle Air et Eau, Garde Terre et Feu, Mage Feu et Eau), débloqués du niveau 1 au niveau 100 et listés par niveau sur l'écran de création ; relance, érosion des PV maximum, états renouvelés ; dégâts réglés par une règle de budget par PA ; PV par niveau propres à chaque classe ; une IA qui planifie son tour ; duels de classes dans le simulateur (T27).

**Équilibre :** au niveau 1, duels de classes de 38 à 54 % (moyenne des deux ordres de jeu), entraînement de 70 à 84 %. Aux niveaux 25 à 100, les duels restent tranchés : ce sont des courses aux dégâts presque déterministes, sans rangs ni caractéristiques (sprint 58). À refaire au sprint 58 : je le propose dans `projects/rpg/REVIEW.md`.

**Tests :** 15 tests de plus (13 des règles, 1 du simulateur, 1 du client) ; 8 mutations, 7 attrapées, 1 presque équivalente. **Trouvé en route :** une IA qui gaspillait ses PA, revenait au contact après avoir tiré, comptait deux fois un poison et n'achevait pas les blessés ; des sorts de haut niveau moins forts par PA qu'un sort du niveau 1.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
