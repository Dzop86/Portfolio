# L'arbre des talents mesuré hors CI (sprint 151, D82, J5 b, J6 a)

> Rapport produit par `Rpg.Sim`, versionné pour suivre l'arbre d'une version à l'autre. À relancer après chaque changement de l'arbre :
> `dotnet run --project src/Rpg.Sim -c Release -- --talents --simulate 100 --scenario zone:ancestors --level 45 --lang fr`
> puis la même ligne avec `--scenario hordes:ancestors`.

**Comment c'est mesuré.** Pour chacune des 18 combinaisons d'une classe et d'une paire, 100 combats au niveau 45, sans équipement d'artisan (`--kit none`). Chaque combat a son build, tiré au hasard comme le ferait un joueur : la paire, puis une branche visée parmi celles que la porte ouvre (un style, ou la tresse de Foudre après Électrisation), montée nœud après nœud, puis les points qui restent sur ce que les règles permettent (`TreeDraws.Random`). Les points de caractéristiques sont répartis à la manière simple (`Progression.Built`). Les adversaires sont les groupes de la Forêt des Aïeux, l'un après l'autre : d'abord ceux de trois monstres au plus, puis les hordes de six et plus, faites pour jouer à plusieurs.

## Contre les petits groupes (trois monstres au plus)

| Classe | Paire | Victoires | Intervalle à 95 % |
|---|---|---|---|
| sentinel | bogging | 100.0 % | 96.3 – 100.0 % |
| sentinel | chilling | 100.0 % | 96.3 – 100.0 % |
| sentinel | cracking | 100.0 % | 96.3 – 100.0 % |
| sentinel | electrifying | 100.0 % | 96.3 – 100.0 % |
| sentinel | fogging | 99.0 % | 94.6 – 99.8 % |
| sentinel | blinding | 100.0 % | 96.3 – 100.0 % |
| guard | bogging | 100.0 % | 96.3 – 100.0 % |
| guard | chilling | 99.0 % | 94.6 – 99.8 % |
| guard | cracking | 100.0 % | 96.3 – 100.0 % |
| guard | electrifying | 100.0 % | 96.3 – 100.0 % |
| guard | fogging | 99.0 % | 94.6 – 99.8 % |
| guard | blinding | 100.0 % | 96.3 – 100.0 % |
| mage | bogging | 100.0 % | 96.3 – 100.0 % |
| mage | chilling | 100.0 % | 96.3 – 100.0 % |
| mage | cracking | 100.0 % | 96.3 – 100.0 % |
| mage | electrifying | 100.0 % | 96.3 – 100.0 % |
| mage | fogging | 100.0 % | 96.3 – 100.0 % |
| mage | blinding | 100.0 % | 96.3 – 100.0 % |

Tout est saturé : un héros de 45 seul bat les petits groupes de la Forêt, quel que soit son build. C'est ce que montraient déjà les mesures de la CI (T110, T114) ; ici, la mesure ne distingue rien.

## Contre les hordes (six monstres et plus)

| Classe | Paire | Victoires | Intervalle à 95 % |
|---|---|---|---|
| sentinel | bogging | 25.0 % | 17.5 – 34.3 % |
| sentinel | chilling | 29.0 % | 21.0 – 38.5 % |
| sentinel | cracking | 23.0 % | 15.8 – 32.2 % |
| sentinel | electrifying | 37.0 % | 28.2 – 46.8 % |
| sentinel | fogging | 18.0 % | 11.7 – 26.7 % |
| sentinel | blinding | 20.0 % | 13.3 – 28.9 % |
| guard | bogging | 17.0 % | 10.9 – 25.5 % |
| guard | chilling | 14.0 % | 8.5 – 22.1 % |
| guard | cracking | 15.0 % | 9.3 – 23.3 % |
| guard | electrifying | 24.0 % | 16.7 – 33.2 % |
| guard | fogging | 13.0 % | 7.8 – 21.0 % |
| guard | blinding | 12.0 % | 7.0 – 19.8 % |
| mage | bogging | 20.0 % | 13.3 – 28.9 % |
| mage | chilling | 22.0 % | 15.0 – 31.1 % |
| mage | cracking | 28.0 % | 20.1 – 37.5 % |
| mage | electrifying | 32.0 % | 23.7 – 41.7 % |
| mage | fogging | 19.0 % | 12.5 – 27.8 % |
| mage | blinding | 24.0 % | 16.7 – 33.2 % |

**Ce que ça montre.** Les hordes restent faites pour jouer à plusieurs : de 12 à 37 % de victoires seul. **Électrisation est devant pour les trois classes** (37, 24 et 32 %, contre 18 à 29 % pour les autres paires de la Sentinelle) : Électrisé fait sauter 35 % de chaque coup sur l'ennemi le plus proche, ce qui vaut le plus quand les ennemis sont nombreux, et la paire est la seule à ouvrir sa tresse. Pour la Sentinelle, l'écart ne dépasse les intervalles à 95 % que face à Vapeur ; ailleurs, il reste dans le bruit de 100 combats. À regarder avec Charles : c'est la lecture voulue de la Foudre (« le feu qui court dans l'air ») ou un léger excès ; la règle de J3 (±1,5 victoire sur 30 contre les petits groupes) tient, elle.

## La règle des 5 % (J6 a)

Talents pris par moins de 5 % des 1800 builds tirés (une fausse décision à revoir) :
- convert-lightning : 2.8 %
- echo : 3.6 %
- heart-air : 4.6 %
- heart-earth : 3.6 %
- heart-fire : 4.6 %
- heart-water : 4.7 %
- long-breath : 4.8 %
- nomad : 4.8 %
- one-gesture : 4.7 %
- prism : 2.6 %
- scorched-earth : 3.9 %
- shared-mark : 2.5 %
- strong-signature : 2.7 %
- transmute-air-lightning : 0.5 %
- transmute-fire-lightning : 0.4 %

**Lecture.** La liste dépend beaucoup de la façon de tirer : la tresse de Foudre ne s'ouvre que d'une paire sur six, donc ses nœuds (Transmutations, Prisme, Signature appuyée, Conversion) ne peuvent pas dépasser environ un sixième des builds, et une Transmutation tirée est remplacée par la Conversion à 45 dès que les points le permettent, ce qui explique leurs 0,4 et 0,5 %. Les Cœurs ne sont pris qu'en plus d'une paire, avec les points qui restent. Restent des clés tirées un peu moins souvent que leurs voisines : Écho, Terre brûlée et Nomade (Terrain), Souffle long et Un geste par sort (Tempo), Empreinte partagée (Alchimie, qui exclut la Catalyse). Elles sont sous 5 % de peu, et elles sont 3 par style : le tirage leur donne au mieux un tiers d'un sixième. Aucune n'est un refus des règles. La vraie mesure de la « fausse décision » demande des joueurs : c'est le playtest.

## Le playtest extérieur (P4 b, P5 a)

À faire par Charles : un joueur qui ne connaît pas l'arbre l'ouvre à 30, et doit pouvoir expliquer son build en une phrase au bout de deux minutes. Le résultat (réussi ou non, et ce qui l'a bloqué) va dans `REVIEW.md`.
