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

---

# v2 : Électrisation décomposée, les héros nus, les trios (sprint 152, D83, T119)

> `dotnet run --project src/Rpg.Sim -c Release -- --talents --simulate 300 --scenario hordes:ancestors --lang fr`, puis la même ligne avec `--no-braid`, puis `--talents --trio --no-braid --simulate 100`. Depuis S152, le glyphe neuf épargne à son premier tour celui qui était déjà dessus. La règle des 5 % devient celle des 20 % des builds qui visent la branche du nœud (les portes, paires et Cœurs, sont imposées par la mesure et laissées de côté).

## Contre les hordes, sans aucune tresse (la réaction seule)

| Classe | Paire | Victoires | Intervalle à 95 % |
|---|---|---|---|
| sentinel | bogging | 28.0 % | 23.2 – 33.3 % |
| sentinel | chilling | 28.7 % | 23.8 – 34.0 % |
| sentinel | cracking | 25.7 % | 21.1 – 30.9 % |
| sentinel | electrifying | 24.0 % | 19.5 – 29.1 % |
| sentinel | fogging | 23.3 % | 18.9 – 28.4 % |
| sentinel | blinding | 26.0 % | 21.4 – 31.2 % |
| guard | bogging | 14.7 % | 11.1 – 19.1 % |
| guard | chilling | 16.3 % | 12.6 – 20.9 % |
| guard | cracking | 16.0 % | 12.3 – 20.6 % |
| guard | electrifying | 14.7 % | 11.1 – 19.1 % |
| guard | fogging | 14.7 % | 11.1 – 19.1 % |
| guard | blinding | 16.0 % | 12.3 – 20.6 % |
| mage | bogging | 20.3 % | 16.2 – 25.2 % |
| mage | chilling | 20.3 % | 16.2 – 25.2 % |
| mage | cracking | 22.3 % | 18.0 – 27.4 % |
| mage | electrifying | 23.3 % | 18.9 – 28.4 % |
| mage | fogging | 20.7 % | 16.5 – 25.6 % |
| mage | blinding | 23.7 % | 19.2 – 28.8 % |
| sentinel | nu, mixte | 20.7 % | 16.5 – 25.6 % |
| sentinel | nu, Terre seul | 62.0 % | 56.4 – 67.3 % |
| sentinel | nu, Feu seul | 89.0 % | 85.0 – 92.1 % |
| sentinel | nu, Eau seul | 75.3 % | 70.2 – 79.9 % |
| sentinel | nu, Air seul | 86.3 % | 82.0 – 89.8 % |
| guard | nu, mixte | 2.7 % | 1.4 – 5.2 % |
| guard | nu, Terre seul | 53.0 % | 47.3 – 58.6 % |
| guard | nu, Feu seul | 62.7 % | 57.1 – 67.9 % |
| guard | nu, Eau seul | 58.0 % | 52.3 – 63.4 % |
| guard | nu, Air seul | 74.0 % | 68.8 – 78.6 % |
| mage | nu, mixte | 18.0 % | 14.1 – 22.7 % |
| mage | nu, Terre seul | 80.0 % | 75.1 – 84.1 % |
| mage | nu, Feu seul | 56.0 % | 50.3 – 61.5 % |
| mage | nu, Eau seul | 70.0 % | 64.6 – 74.9 % |
| mage | nu, Air seul | 67.7 % | 62.2 – 72.7 % |

Talents pris par moins de 20 % des builds qui visent leur branche, sur 5400 builds tirés (une fausse décision à revoir) :
- convert-lightning : 0.0 %
- echo : 13.2 %
- long-breath : 17.4 %
- nomad : 15.7 %
- one-gesture : 16.9 %
- prism : 0.0 %
- scorched-earth : 13.8 %
- shared-mark : 9.4 %
- strong-signature : 0.0 %
- thrift : 18.5 %
- transmute-air-lightning : 0.0 %
- transmute-fire-lightning : 0.0 %

## Contre les hordes, avec les tresses

| Classe | Paire | Victoires | Intervalle à 95 % |
|---|---|---|---|
| sentinel | bogging | 28.0 % | 23.2 – 33.3 % |
| sentinel | chilling | 28.7 % | 23.8 – 34.0 % |
| sentinel | cracking | 25.7 % | 21.1 – 30.9 % |
| sentinel | electrifying | 36.0 % | 30.8 – 41.6 % |
| sentinel | fogging | 23.3 % | 18.9 – 28.4 % |
| sentinel | blinding | 26.0 % | 21.4 – 31.2 % |
| guard | bogging | 14.7 % | 11.1 – 19.1 % |
| guard | chilling | 16.3 % | 12.6 – 20.9 % |
| guard | cracking | 16.0 % | 12.3 – 20.6 % |
| guard | electrifying | 25.7 % | 21.1 – 30.9 % |
| guard | fogging | 14.7 % | 11.1 – 19.1 % |
| guard | blinding | 16.0 % | 12.3 – 20.6 % |
| mage | bogging | 20.7 % | 16.5 – 25.6 % |
| mage | chilling | 20.3 % | 16.2 – 25.2 % |
| mage | cracking | 22.3 % | 18.0 – 27.4 % |
| mage | electrifying | 29.3 % | 24.5 – 34.7 % |
| mage | fogging | 20.7 % | 16.5 – 25.6 % |
| mage | blinding | 23.7 % | 19.2 – 28.8 % |
| sentinel | nu, mixte | 20.7 % | 16.5 – 25.6 % |
| sentinel | nu, Terre seul | 62.0 % | 56.4 – 67.3 % |
| sentinel | nu, Feu seul | 89.0 % | 85.0 – 92.1 % |
| sentinel | nu, Eau seul | 75.3 % | 70.2 – 79.9 % |
| sentinel | nu, Air seul | 86.3 % | 82.0 – 89.8 % |
| guard | nu, mixte | 2.7 % | 1.4 – 5.2 % |
| guard | nu, Terre seul | 53.0 % | 47.3 – 58.6 % |
| guard | nu, Feu seul | 62.7 % | 57.1 – 67.9 % |
| guard | nu, Eau seul | 58.0 % | 52.3 – 63.4 % |
| guard | nu, Air seul | 74.0 % | 68.8 – 78.6 % |
| mage | nu, mixte | 18.0 % | 14.1 – 22.7 % |
| mage | nu, Terre seul | 80.0 % | 75.1 – 84.1 % |
| mage | nu, Feu seul | 56.0 % | 50.3 – 61.5 % |
| mage | nu, Eau seul | 70.0 % | 64.6 – 74.9 % |
| mage | nu, Air seul | 67.7 % | 62.2 – 72.7 % |

Talents pris par moins de 20 % des builds qui visent leur branche, sur 5400 builds tirés (une fausse décision à revoir) :
- echo : 12.6 %
- long-breath : 16.9 %
- nomad : 15.4 %
- one-gesture : 16.4 %
- scorched-earth : 13.7 %
- shared-mark : 9.3 %
- thrift : 18.3 %
- transmute-air-lightning : 17.2 %
- transmute-fire-lightning : 10.9 %

## Trios : un héros de chaque classe, l'un avec la paire, les deux autres avec une autre paire tirée (jamais Électrisation), 100 combats par paire

| Paire du premier | Victoires | Intervalle à 95 % |
|---|---|---|
| bogging | 100.0 % | 96.3 – 100.0 % |
| chilling | 100.0 % | 96.3 – 100.0 % |
| cracking | 100.0 % | 96.3 – 100.0 % |
| electrifying | 100.0 % | 96.3 – 100.0 % |
| fogging | 100.0 % | 96.3 – 100.0 % |
| blinding | 100.0 % | 96.3 – 100.0 % |

## Ce que ça montre, et la règle appliquée (§12.3 de `suite-apres-d69.md`)

- **La réaction seule ne dépasse pas les autres paires.** Sans tresse, Électrisation gagne 24,0, 14,7 et 23,3 % des hordes (Sentinelle, Garde, Mage) contre des médianes de 26,0, 16,0 et 20,7 % pour les cinq autres paires : −2,0, −1,3 et +2,6 points, soit −0,2 point les trois classes réunies, bien sous le seuil de +8. **Électrisé reste à 35 %.** Toute l'avance de S151 (36,0, 25,7 et 29,3 %) vient de la tresse : elle se refermera quand les autres paires auront la leur (la Glace en S161), et l'on remesure alors.
- **Les trios sont saturés** : trois héros de 45 gagnent toutes les hordes de la Forêt, quelle que soit la paire du premier. L'écart d'un trio de Foudre (0 point) est sous le seuil de +10 : **pas de plafond de sauts.** La mesure à trois ne dira quelque chose que contre des hordes du niveau des héros (le socle 45-60, S157).
- **Un constat à part : à 45, un héros nu d'un seul élément bat les hordes de la Forêt** (53 à 89 %), loin devant les builds tirés (15 à 36 %) et le héros nu mixte (3 à 21 %). Les monstres de la Forêt sont de niveau 31 à 34 : à 45, un héros qui met tous ses points dans un élément les surclasse, alors que la mesure répartit les points d'un build sur les éléments de son grimoire (`Progression.Built`). Ni l'arbre ni Électrisé n'y sont pour rien ; la comparaison au « meilleur héros nu » (T110) n'a de sens qu'au niveau des monstres. À reprendre avec le socle 45-60 (S157), où les hordes seront du niveau des héros.
- **Les nœuds rares, par branche :** la moitié d'un choix (les deux Transmutations, 10,9 et 17,2 %) l'est par construction ; restent `shared-mark` (9 %), `echo` et `scorched-earth` (13 %), `nomad`, `one-gesture`, `long-breath` (15 à 17 %) et `thrift` (18 %). Rien n'y est sous 5 % : ce sont des nœuds de fin de branche que le tirage atteint quand il lui reste des points, pas des fausses décisions évidentes. À regarder par le game designer avec les spécialisations.

