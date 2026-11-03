# Osmose : les textes de l'arbre des spécialisations

> **Proposition du scénariste, 3 novembre 2026, à relire par Charles.** Elle habille la conception validée : `docs/elements-specialisations.md`, section v4 (D95), avec ses retouches à 30 (§v4.2) et D94 (un composé consomme ses parents).
> **Convention :** **[P]** = absent de la bible et du jeu. La bible ne dit rien des classes : **tous les noms, titres, passifs et phrases de ce fichier sont [P]**, sauf les chiffres, qui viennent de la v4. Le canon de la bible n'est pas modifié.
> **Lu :** v4 et v3 de la conception, `docs/bible.md`, `docs/talents-canon.md`, `docs/textes-v7.md` (voix de Quatre-Chemins), `data/specs.json` (déjà passé à `base` et `forks`), `data/spells.json`, `data/passives.json`, `data/talents.json`, `data/quests.json`, `data/dialogues/anatole.json` et `josette.json`, `Texts.cs`.
> **Non lu :** `docs/textes-s163.md`. Il n'est pas dans l'arbre de travail, et je n'ai pas de shell pour lancer `git show s163-specialisations:…`. Pour Mire et Affût, je reprends `talents-canon.md` ; la phrase de la Sente est nouvelle. À comparer avec la branche (§4).

## Sommaire
1. Noms et titres (45 nœuds)
2. Sorts de voie (36)
3. Passifs, règles, descriptions
4. Phrases d'ambiance (27)
5. La quête « Une route à soi », et Anatole
6. Textes d'interface à retoucher
7. Contradictions et points à trancher

---

## 1. Noms et titres (45 nœuds) [P]

### Les trois règles que j'ai suivies

- **Aucun titre ne s'accorde.** Le jeu connaît une apparence (`female-*`, `male-*`, « Femme » et « Homme » au lobby), mais aucun genre, et aucun texte n'accorde le héros (`textes-v7.md`). `LocalizedText` n'a qu'une forme par langue. Je n'ajoute donc ni forme féminine ni code : chaque titre est un mot invariable (Archimage, Thaumaturge), une chose (Fléau, Carcan, Muraille) ou un composé verbe-nom, le modèle des titres déjà en jeu (Porte-clé, Passe-chemins, Glane-tout). Sorcier, Nécromancien, Envoûteur, Moissonneur, Harponneur, Geôlier, Artilleur, Fauconnier, Trappeur, Bondisseur, Coureur disparaissent, ainsi que « Haut », « souverain » et « ancien », qui s'accordent aussi. Un titre s'affiche sans article : `{0} Te voici : {1}.`
- **Longueur.** 18 signes au plus en français (Citadelle d'épines), 16 en anglais (Grand Procession). Coupés en deux lignes à l'espace ou au trait d'union, aucun morceau ne dépasse 11 signes (Thaumaturge, Shadecaller). Non mesuré en pixels : la maquette et la police de la page ne sont pas encore fixées.
- **Le nœud et le titre sont le même mot**, sauf pour les branches de Garde et de Sentinelle à 30 : le nœud dit « Palissade », le titre reste « Garde de la Palissade », comme dans `specs.json`.

**Pour brancher :** `branche` = `name` et `base.title` ; `branche/lignée@50` et `@100` = `forks[].tiers[].title`. Les identifiants de la v4 ne changent pas, même quand le nom s'en éloigne (`reaper`, `jailer`).

### Mage

| id (v4) | FR | EN | Titre de travail remplacé | Pourquoi |
|---|---|---|---|---|
| `archmage` | Archimage | Archmage | | inchangé |
| `archmage/high-archmage@50` | Astromage | Astromage | Haut archimage | Comète et Rayon ; « Haut » s'accorde |
| `archmage/high-archmage@100` | Mage du zénith | Zenith Mage | Archimage souverain | le plus gros sort, au plus haut |
| `archmage/patient@50` | Thaumaturge | Thaumaturge | | épicène, gardé (voir §7, point 9) |
| `archmage/patient@100` | Grand œuvre | Magnum Opus | Thaumaturge ancien | ce qu'on prépare longtemps |
| `sorcerer` | Maléfice | Hex | Sorcier | Sorcier s'accorde |
| `sorcerer/plague@50` | Fléau | Plague | | une chose, gardé |
| `sorcerer/plague@100` | Pestilence | Pestilence | Grand fléau | un seul mot, plus fort |
| `sorcerer/hexer@50` | Mauvais œil | Evil Eye | Envoûteur | « je désigne qui tombe » |
| `sorcerer/hexer@100` | Augure funeste | Dire Omen | Grand envoûteur | « funeste » est épicène, et « augure » aussi |
| `necromancer` | **Lève-ombres** | **Shadecaller** | Nécromancien | tranché, voir ci-dessous |
| `necromancer/binder@50` | Mène-ombres | Shade Marshal | Lieur d'ombres | « Lien » est interdit (`talents-canon.md` §5) |
| `necromancer/binder@100` | Grand cortège | Grand Procession | Seigneur des tombes | avec Marche funèbre ; ni tombe ni seigneur |
| `necromancer/reaper@50` | Sème-ombres | Shadesower | Moissonneur | elle pose ses ombres où l'ennemi passera |
| `necromancer/reaper@100` | Moisson noire | Black Harvest | Moissonneur ancien | ce qu'on a semé saute |

**Le Nécromancien s'appelle Lève-ombres / Shadecaller.** Trois raisons. Le mot s'accorde. Il dit « relever les morts », et les morts relevés sont dans le jeu les zombies de la Crypte oubliée, un symptôme et une contrainte (`talents-canon.md` §3.3) : le héros ne doit pas faire ce qu'il combat. Enfin, les sorts disent déjà « ombre », jamais « cadavre ». Une ombre est ce qu'un mort a laissé, pas le mort : la phrase du §4 le pose. Le nom suit le sort « Lever une ombre » et le modèle Porte-clé.

### Garde

| id (v4) | FR | EN | Titre de base ou de travail remplacé | Pourquoi |
|---|---|---|---|---|
| `palisade` | Palissade | Palisade | titre : Garde de la Palissade / Palisade Guard | inchangé |
| `palisade/rampart@50` | Garde-corps | Bodyguard | Rempart | la classe a déjà un sort Rempart (`bulwark`) ; ici, on garde les corps des autres |
| `palisade/rampart@100` | Muraille | Rampart | | gardé en français |
| `palisade/bastion@50` | Bastion d'épines | Thorn Bastion | | gardé |
| `palisade/bastion@100` | Citadelle d'épines | Thorn Citadel | | gardé |
| `breach` | Brèche | Breach | titre : Garde de la Brèche / Breach Guard | inchangé |
| `breach/ram@50` | Bélier | Ram | | gardé |
| `breach/ram@100` | Brise-rangs | Rank-Breaker | | gardé |
| `breach/harpooner@50` | Croc-de-fer | Ironhook | Harponneur | s'accorde, et le sort porte déjà « Harpon » |
| `breach/harpooner@100` | Cloue-au-mur | Wallcrusher | Grand harponneur | 15 dégâts par case contre un obstacle |
| `grip` | Étau | Grip | titre : Garde de l'Étau / Grip Guard | inchangé |
| `grip/jailer@50` | Carcan | Shackle | Geôlier | s'accorde |
| `grip/jailer@100` | Oubliette | Oubliette | Geôlier souverain | la cible ne joue plus |
| `grip/pincer@50` | Tenaille | Pincer | | gardé ; c'est le sort qui change de nom (§2) |
| `grip/pincer@100` | Étau vivant | Living Grip | | gardé : l'adjectif s'accorde à « étau » |

### Sentinelle

| id (v4) | FR | EN | Titre de base ou de travail remplacé | Pourquoi |
|---|---|---|---|---|
| `sightline` | Mire | Sightline | titre : Sentinelle de la Mire / Sightline Sentinel | inchangé |
| `sightline/marksman@50` | Longue-vue | Longsight | Tireur d'élite | s'accorde |
| `sightline/marksman@100` | Dernière semonce | Final Warning | Œil sûr | la Mire prévient d'abord (`talents-canon.md` §3.1) ; « Œil sûr » se confondait avec Mauvais œil |
| `sightline/barrage@50` | Herse | Portcullis | Artilleur | elle ferme les couloirs |
| `sightline/barrage@100` | Route barrée | Road Closed | Maître artilleur | un clin d'œil aux routes de Quatre-Chemins |
| `ambush` | Affût | Ambush | titre : Sentinelle de l'Affût / Ambush Sentinel | inchangé |
| `ambush/falconer@50` | Porte-faucon | Hawkbearer | Fauconnier | s'accorde |
| `ambush/falconer@100` | Deux-Ailes | Twin Wings | Maître fauconnier | deux Faucons |
| `ambush/trapper@50` | Tend-pièges | Trapsetter | Trappeur | s'accorde |
| `ambush/trapper@100` | Chausse-trape | Deadfall | Maître piégeur | un vieux mot de chasse |
| `trail` | Sente | Trail | titre : Sentinelle de la Sente / Trail Sentinel | inchangé |
| `trail/leaper@50` | Saute-haie | Hedgehopper | Bondisseur | s'accorde |
| `trail/leaper@100` | Hors d'atteinte | Out of Reach | Ombre légère | « Ombre » se confondait avec les titres de la Mage |
| `trail/runner@50` | Court-sentes | Trail Runner | Coureur | s'accorde |
| `trail/runner@100` | Cours-toujours | Never Caught | Coureur des sentes | l'expression veut aussi dire « tu peux toujours essayer » |

**Vérifié entre classes :** aucun titre identique et aucun mot de tête commun d'une classe à l'autre. Ombre reste à la Mage, Œil au seul Mauvais œil, Grand à la seule Mage, Mur au seul Cloue-au-mur. En français, aucun titre ne reprend le nom d'un sort du jeu ; en anglais, seule la branche Grip, déjà nommée ainsi, porte le nom d'un sort de monstre (Étreinte). Le plus proche d'un titre existant est Fléau et l'achèvement « Fléau des donjons » / "Dungeon Bane", qui se lisent sans confusion.

---

## 2. Sorts de voie (36) [P]

Les identifiants sont une proposition (préfixe `path-`, retouche 10). La colonne de droite résume la v4 et n'est pas un texte du jeu. **En gras, les noms changés par rapport à la v4**, pour une raison donnée sous chaque tableau. Aucun doublon avec les 470 noms de `spells.json`, en français comme en anglais.

### Mage

| id proposé | Où | FR | EN | Ce qu'il fait (v4) |
|---|---|---|---|---|
| `path-comet` | `archmage` | Comète | Comet | 7 PA, PO 3 à 7, cercle de 1, relance 3 |
| `path-composure` | `archmage` | Recueillement | Composure | 2 PA, sur soi, +25 % pour le tour, relance 3 |
| `path-ray` | `high-archmage` | Rayon | Ray | 5 PA, PO 1 à 5 en ligne, ligne de 4 cases, sans vue, relance 2, 17 à 24 |
| `path-crescendo` | `patient` | **Point d'orgue** | **Crescendo** | 10 PA, PO 3 à 7, cercle de 2, sans vue, relance 2, 27 à 36 |
| `path-curse` | `sorcerer` | Malédiction | Curse | 3 PA, PO 1 à 6, −15 % de dégâts infligés 2 tours |
| `path-gall` | `sorcerer` | Fiel | Gall | 3 PA, poison au barème 3 tours |
| `path-miasma` | `plague` | Miasme | Miasma | 4 PA, PO 1 à 5, croix de 1, relance 2, 10 à 13 et poison 5 pendant 3 tours |
| `path-blight` | `hexer` | Flétrissure | Blight | 3 PA, PO 1 à 6, sans dégâts, −15 % de résistance 2 tours, relance 1 |
| `path-raise-shade` | `necromancer` | Lever une ombre | Raise a Shade | 4 PA, ombre de 25 % de ses PV, relance 3 |
| `path-shade-tithe` | `necromancer` | **Dîme d'ombre** | **Shade's Tithe** | 2 PA, une invocation lui cède 20 % de ses PV |
| `path-funeral-march` | `binder` | Marche funèbre | Funeral March | 3 PA, cercle de 2 autour de soi, alliés +20 % de dégâts et +1 PM 1 tour, relance 3 |
| `path-detonation` | `reaper` | **Détonation** | **Detonation** | 3 PA, PO 1 à 6, vise une de ses ombres, qui meurt, croix de 1, relance 1, 23 à 31 |

- **Point d'orgue** remplace Déferlante : le mot dit la vague, et une voie ne nomme aucun élément (§v4.1). Ce sort peut frapper en Feu.
- **Dîme d'ombre** remplace Lien funeste : « Lien » fait partie des mots réservés à l'Accord (`talents-canon.md` §5).
- **Détonation** remplace Éclat d'ombre : le mot se lit comme les jumeaux (Éclat de glace, de boue, de vapeur), donc comme un composé « Ombre » qui n'existe pas, et il rappelle la monnaie.

### Garde

| id proposé | Où | FR | EN | Ce qu'il fait (v4) |
|---|---|---|---|---|
| `path-interpose` | `palisade` | Interposition | Interpose | 2 PA, PO 1 à 3, échange de place avec un allié, bouclier sur soi, relance 2 |
| `path-stand-fast` | `palisade` | **Tenir bon** | **Stand Fast** | 2 PA, sur soi, indéplaçable et +15 % de résistance 1 tour, relance 2 |
| `path-shield-wall` | `rampart` | Mur de boucliers | Shield Wall | 4 PA, croix de 1 autour de soi, bouclier de 30 aux alliés (37 avec la règle), relance 3 |
| `path-retaliation` | `bastion` | Rétorsion | Retaliation | 3 PA, contact, relance 1, 14 à 19, plus tout son bouclier, consommé, 38 au plus |
| `path-assault` | `breach` | **Assaut** | **Assault** | 3 PA, ligne de 2 à 4, on vise la case libre où l'on atterrit, dégâts en croix de 1, relance 2 |
| `path-wrench` | `breach` | Arrachement | Wrench | 3 PA, PO 1 à 4, attire de 2, −1 PM |
| `path-breakthrough` | `ram` | Trouée | Breakthrough | 4 PA, contact, ligne de 3 cases, recul de 1, relance 1, 12 à 17 |
| `path-harpoon` | `harpooner` | Harpon | Harpoon | 3 PA, PO 2 à 6 en ligne, attire de 3 (4 avec la règle), relance 1, 10 à 13 |
| `path-hold` | `grip` | Prise | Hold | 3 PA, contact, au barème, −2 PM 2 tours |
| `path-lock` | `grip` | Verrou | Lock | 2 PA, contact, plus de déplacement volontaire 1 tour, relance 3 |
| `path-crush` | `jailer` | Écrasement | Crush | 5 PA, contact, 18 à 25, +30 % sur un ennemi à 0 PM |
| `path-iron-ring` | `pincer` | **Cercle de fer** | **Iron Ring** | 3 PA, cercle de 1 autour de soi, sans dégâts, −2 PM, relance 2 |

- **Tenir bon** remplace Ancrage : « ancrage » est un mot du Fragment de Dhoram (bible §8), et une voie ne double pas un Fragment. « Pied ferme » est pris par un talent (`firm-footing`).
- **Assaut** remplace Charge : le nom et l'anglais sont déjà ceux d'un sort de monstre, la retouche 10 ne changeait que l'identifiant.
- **Cercle de fer** remplace Tenaille : la lignée porte déjà ce titre, et « Tenaille se relance » deviendrait ambigu.

### Sentinelle

| id proposé | Où | FR | EN | Ce qu'il fait (v4) |
|---|---|---|---|---|
| `path-aimed-shot` | `sightline` | Tir visé | Aimed Shot | 5 PA, PO 4 à 9, une cible, relance 2 |
| `path-weak-spot` | `sightline` | **Point faible** | **Weak Spot** | 2 PA, PO 1 à 9, sans dégâts, la cible perd 15 % de résistance 2 tours, relance 3 |
| `path-bullseye` | `marksman` | Coup au but | Bullseye | 6 PA, PO 5 à 10, une cible, relance 3, 31 à 42 |
| `path-barrage` | `barrage` | Tir de barrage | Barrage | 4 PA, PO 3 à 8, ligne de 3 cases, −1 PM, relance 2, 11 à 16 |
| `path-jaws` | `ambush` | Mâchoires | Jaw Trap | 3 PA, glyphe de 2 tours, −3 PM et dégâts au barème, relance 2 |
| `path-dive` | `ambush` | Piqué | Dive | 3 PA, PO 1 à 7, sans vue, demande un Faucon en jeu, relance 1 |
| `path-changeover` | `falconer` | **Chassé-croisé** | **Changeover** | 2 PA, PO 1 à 6, sans vue, échange de place avec le Faucon, relance 2 |
| `path-bait` | `trapper` | Appât | Bait | 2 PA, PO 1 à 4, glyphe de 2 tours, −1 PA à qui y commence son tour, relance 3 |
| `path-vault` | `trail` | **Cabriole** | Vault | 2 PA, PO 2 à 4, saut, relance 1 |
| `path-heel-kick` | `trail` | Coup de talon | Heel Kick | 3 PA, contact, au barème, puis recul de 2 |
| `path-feint` | `leaper` | Feinte | Feint | 2 PA, sur soi, le prochain qui te frappe au contact perd 2 PM, relance 3 |
| `path-running-shot` | `runner` | Tir en course | Running Shot | 3 PA, PO 2 à 5, +2 PM pour le tour, 8 à 11 |

- **Point faible** remplace Visée : le sort ne touche plus le lanceur (retouche 4), et Visée se confondait à l'œil avec Tir visé.
- **Chassé-croisé** remplace Vol d'échange : « vol » se lit aussi comme un larcin.
- **Cabriole** remplace Bond : deux sorts de monstres s'appellent déjà Bond dans `spells.json`.

### Les invocations (la v4 les compte sans les nommer)

| id proposé | FR | EN | Note |
|---|---|---|---|
| `shade` | Ombre levée | Risen Shade | la v4 ne donne ni son sort, ni sa portée de pose, ni l'endroit où naît celle du passif |
| `shade-touch` (si son sort est nouveau) | Toucher d'ombre | Shade's Touch | à fixer par le game designer |

Le second Faucon reste « Faucon » / "Hawk" (`hawk`).

---

## 3. Passifs, règles, descriptions [P]

### 3.1 Passifs et règles, avec leur texte court

Style des fiches du jeu, chiffres de la v4. Un même nom sur plusieurs lignes veut dire un passif qui en remplace un autre (`Replaces`) : le joueur garde un seul nom. Les identifiants sont une proposition. Les règles n'ont pas de nom : le nom affiché est celui du nœud.

**Mage**

| id proposé | Où | Type | FR : nom | FR : texte | EN : nom | EN : texte |
|---|---|---|---|---|---|---|
| `spec-held-breath` | `archmage` | passif | Souffle retenu | +1 PA au tour suivant si tu n'as pas bougé. | Held Breath | +1 AP next turn if you did not move. |
| `spec-held-breath-step` | `high-archmage@50` | passif | Souffle retenu | +1 PA au tour suivant, même après un pas. | Held Breath | +1 AP next turn, even after one step. |
| `spec-grand-entrance` | `high-archmage@100` | passif | Entrée en scène | +3 PA au premier tour du combat. | Grand Entrance | +3 AP on the first turn of the fight. |
| | `archmage` | règle | | Tes sorts de 6 PA ou plus se relancent un tour plus tôt. | | Your spells of 6 AP or more come back a turn sooner. |
| | `patient@50` | règle | | Tu gardes jusqu'à 3 PA d'un tour au suivant. | | You keep up to 3 AP from one turn to the next. |
| | `patient@100` | règle | | Tu gardes jusqu'à 5 PA. Ton premier sort de dégâts du tour fait +15 %. | | You keep up to 5 AP. Your first damage spell of the turn does +15%. |
| | `sorcerer` | règle | | Tes états durent 1 tour de plus, sauf ceux des sorts de composés. 2 poisons se cumulent. | | Your states last 1 turn longer, except those of compound spells. 2 poisons stack. |
| | `plague@50`, `@100` | règle | | 3 poisons se cumulent. Puis 4. | | 3 poisons stack. Then 4. |
| | `hexer@100` | règle | | Un ennemi sous 2 états ou plus reçoit +15 % de tes coups. | | An enemy under 2 states or more takes +15% from your blows. |
| `spec-what-remains` | `necromancer` | passif | Ce qui reste | Une fois par combat, un ennemi qui tombe laisse une ombre qui se bat pour toi. | What Remains | Once per fight, a fallen enemy leaves a shade that fights for you. |
| `spec-what-remains-twice` | `binder@100` | passif | Ce qui reste | Deux fois par combat, un ennemi qui tombe laisse une ombre qui se bat pour toi. | What Remains | Twice per fight, a fallen enemy leaves a shade that fights for you. |
| | `necromancer` | règle | | 2 invocations à la fois. | | 2 summons at once. |
| | `binder@50` | règle | | Tes ombres ont +25 % de PV. | | Your shades have +25% HP. |
| | `binder@100` | règle | | 3 invocations à la fois. | | 3 summons at once. |
| `shade-short-fuse` | `reaper@100`, porté par l'ombre | passif `Death` | Mèche courte | À sa mort, explose à 2 cases, alliés compris : 30 à 40 dégâts neutres. | Short Fuse | On death, explodes within 2 cells, allies included: 30 to 40 neutral damage. |

**Garde**

| id proposé | Où | Type | FR : nom | FR : texte | EN : nom | EN : texte |
|---|---|---|---|---|---|---|
| `spec-close-ranks-9` | `palisade` | passif | Serrez les rangs | Au début de ton tour, bouclier de 9 sur toi et tes alliés à 1 case. | Close Ranks | At the start of your turn, a shield of 9 on you and allies 1 cell away. |
| `spec-close-ranks-12` | `rampart@50` | passif | Serrez les rangs | Au début de ton tour, bouclier de 12 sur toi et tes alliés à 2 cases. | Close Ranks | At the start of your turn, a shield of 12 on you and allies within 2 cells. |
| `spec-close-ranks-17` | `rampart@100` | passif | Serrez les rangs | Au début de ton tour, bouclier de 17 sur toi et tes alliés à 2 cases. | Close Ranks | At the start of your turn, a shield of 17 on you and allies within 2 cells. |
| `spec-not-today` | `rampart@100` | passif | Pas aujourd'hui | Une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV. | Not Today | Once per fight, an ally within 2 cells who would fall stays at 1 HP. |
| | `palisade` | règle | | Tes boucliers sur les alliés valent +25 %. | | Your shields on allies are worth +25%. |
| `spec-thorns` | `bastion@100` | passif | Épines | Qui te frappe au contact est empoisonné : 8 pendant 2 tours, neutre. | Thorns | Whoever strikes you in melee is poisoned: 8 for 2 turns, neutral. |
| `spec-carried-through-1` | `breach` | passif | Sur la lancée | Un ennemi achevé : +1 PM. | Carried Through | An enemy finished off: +1 MP. |
| `spec-carried-through-2` | `ram@50` | passif | Sur la lancée | Un ennemi achevé : +1 PM et +1 PA. | Carried Through | An enemy finished off: +1 MP and +1 AP. |
| `spec-carried-through-3` | `ram@100` | passif | Sur la lancée | Un ennemi achevé : +1 PM et +2 PA. | Carried Through | An enemy finished off: +1 MP and +2 AP. |
| | `breach` | règle | | Tes attirances portent d'une case de plus. | | Your pulls reach one cell farther. |
| | `harpooner@50` | règle | | +15 % de dégâts sur une cible à ton contact. | | +15% damage on a target next to you. |
| | `harpooner@100` | règle | | Tes reculs contre un obstacle font 15 dégâts par case. | | Your pushes into an obstacle do 15 damage per cell. |
| `spec-hands-off` | `grip` | passif | Qui s'y frotte | Qui te frappe au contact perd 1 PM. | Hands Off | Whoever strikes you in melee loses 1 MP. |
| `spec-hands-off-ap` | `jailer@50` | passif | Qui s'y frotte | Qui te frappe au contact perd 1 PM, et 1 PA une fois par tour. | Hands Off | Whoever strikes you in melee loses 1 MP, and 1 AP once per turn. |
| | `grip` | règle | | Tes retraits de PA et de PM durent 1 tour de plus, sauf ceux des sorts de composés. | | Your AP and MP losses last 1 turn longer, except those of compound spells. |
| `spec-no-way-out` | `pincer@100` | passif | Personne ne sort | Les ennemis à ton contact perdent 1 PM au début de leur tour. | No Way Out | Enemies next to you lose 1 MP at the start of their turn. |

**Sentinelle**

| id proposé | Où | Type | FR : nom | FR : texte | EN : nom | EN : texte |
|---|---|---|---|---|---|---|
| `spec-calm-eye` | `sightline` | passif | Œil calme | Aucun ennemi à 3 cases au début de ton tour : +15 % de dégâts pendant 1 tour. | Calm Eye | No enemy within 3 cells at the start of your turn: +15% damage for 1 turn. |
| `spec-calm-eye-near` | `barrage@50` | passif | Œil calme | Aucun ennemi à 2 cases au début de ton tour : +15 % de dégâts pendant 1 tour. | Calm Eye | No enemy within 2 cells at the start of your turn: +15% damage for 1 turn. |
| `spec-calm-eye-long` | `marksman@100` | passif | Œil calme | Aucun ennemi à 3 cases au début de ton tour : +25 % de dégâts pendant 2 tours. | Calm Eye | No enemy within 3 cells at the start of your turn: +25% damage for 2 turns. |
| | `sightline` | règle | | Tes sorts en vue portent à +1 PO. | | Your spells in sight reach +1 range. |
| | `marksman@50` | règle | | Tes sorts de 6 PO ou plus portent à +2 PO au lieu de +1. | | Your spells of 6 range or more reach +2 range instead of +1. |
| `spec-well-fed` | `ambush` | règle d'invocation | Bien nourri | Ton Faucon a 12 % de tes PV et profite de tes caractéristiques. | Well Fed | Your Hawk has 12% of your HP and draws on your characteristics. |
| `spec-well-fed-20` | `falconer@50` | règle d'invocation | Bien nourri | Ton Faucon a 20 % de tes PV et profite de tes caractéristiques. | Well Fed | Your Hawk has 20% of your HP and draws on your characteristics. |
| | `ambush` | règle | | Tes glyphes durent 1 tour de plus, sauf celles des sorts de composés. | | Your glyphs last 1 turn longer, except those of compound spells. |
| | `trapper@100` | règle | | +15 % de dégâts sur un ennemi dans une de tes glyphes. | | +15% damage on an enemy in one of your glyphs. |
| `spec-quick-heels` | `trail` | passif | Talons vifs | Si on te frappe au contact : +2 PM pendant 1 tour. | Quick Heels | If you are struck in melee: +2 MP for 1 turn. |
| `spec-quick-heels-3` | `leaper@100` | passif | Talons vifs | Si on te frappe au contact : +3 PM pendant 1 tour. | Quick Heels | If you are struck in melee: +3 MP for 1 turn. |
| `spec-saw-it-coming` | `leaper@100` | passif | Vu venir | Une fois par combat, un coup au contact te donne un bouclier de 40. | Saw It Coming | Once per fight, a melee blow gives you a shield of 40. |
| | `trail` | règle | | Tes sauts n'ont pas besoin de la vue. | | Your leaps need no sight. |
| | `runner@50` | règle | | +3 % de dégâts par case marchée depuis ton dernier sort, 15 % au plus. | | +3% damage per cell walked since your last spell, 15% at most. |
| | `runner@100` | règle | | +5 % par case, 25 % au plus. Si tu as marché 3 cases, ton dernier sort laisse un écho, à 50 % au tour suivant. | | +5% per cell, 25% at most. If you walked 3 cells, your last spell leaves an echo, at 50% on the next turn. |

### 3.2 Les `description` de chaque nœud, prêtes pour `specs.json`

Elles remplacent celles de S186 (`Frappée au contact` s'accordait, et les lignées recopiaient la même description).

**Mage**

| id (v4) | FR | EN |
|---|---|---|
| `archmage` | +1 PA au tour suivant si tu n'as pas bougé. Tes sorts de 6 PA ou plus se relancent un tour plus tôt. Sorts : Comète et Recueillement. | +1 AP next turn if you did not move. Your spells of 6 AP or more come back a turn sooner. Spells: Comet and Composure. |
| `high-archmage@50` | Le passif de +1 PA tient même après un pas. Sort : Rayon. | The +1 AP passive holds even after one step. Spell: Ray. |
| `high-archmage@100` | +3 PA au premier tour du combat. Comète laisse au sol, pendant 1 tour, une glyphe de −1 PM. | +3 AP on the first turn of the fight. Comet leaves a glyph of −1 MP on the ground for 1 turn. |
| `patient@50` | Tu gardes jusqu'à 3 PA d'un tour au suivant. Sort : Point d'orgue. | You keep up to 3 AP from one turn to the next. Spell: Crescendo. |
| `patient@100` | Tu gardes jusqu'à 5 PA. Ton premier sort de dégâts du tour fait +15 %. | You keep up to 5 AP. Your first damage spell of the turn does +15%. |
| `sorcerer` | Tes états durent 1 tour de plus, sauf ceux des sorts de composés. 2 poisons se cumulent. Sorts : Malédiction et Fiel. | Your states last 1 turn longer, except those of compound spells. 2 poisons stack. Spells: Curse and Gall. |
| `plague@50` | 3 poisons se cumulent. Sort : Miasme. | 3 poisons stack. Spell: Miasma. |
| `plague@100` | 4 poisons se cumulent. Fiel frappe en croix de 1 (dégâts × 0,8). Miasme se relance en 1 tour. | 4 poisons stack. Gall strikes in a cross of 1 (damage × 0.8). Miasma comes back in 1 turn. |
| `hexer@50` | Malédiction se relance en 1 tour. Sort : Flétrissure. | Curse comes back in 1 turn. Spell: Blight. |
| `hexer@100` | Un ennemi sous 2 états ou plus reçoit +15 % de tes coups. Malédiction touche en croix de 1. | An enemy under 2 states or more takes +15% from your blows. Curse reaches a cross of 1. |
| `necromancer` | Une fois par combat, un ennemi qui tombe laisse une ombre qui se bat pour toi. 2 invocations à la fois. Sorts : Lever une ombre et Dîme d'ombre. | Once per fight, a fallen enemy leaves a shade that fights for you. 2 summons at once. Spells: Raise a Shade and Shade's Tithe. |
| `binder@50` | Tes ombres ont +25 % de PV. Sort : Marche funèbre. | Your shades have +25% HP. Spell: Funeral March. |
| `binder@100` | 3 invocations à la fois. Le passif joue 2 fois par combat. | 3 summons at once. The passive works twice per fight. |
| `reaper@50` | Lever une ombre se relance en 2 tours. Sort : Détonation. | Raise a Shade comes back in 2 turns. Spell: Detonation. |
| `reaper@100` | Toute ombre qui meurt explose à 2 cases, alliés compris : 30 à 40 dégâts neutres. Détonation n'a plus de relance, une fois par tour. | Any shade that dies explodes within 2 cells, allies included: 30 to 40 neutral damage. Detonation has no cooldown, once per turn. |

**Garde**

| id (v4) | FR | EN |
|---|---|---|
| `palisade` | Au début de ton tour, bouclier de 9 sur toi et tes alliés à 1 case. Tes boucliers sur les alliés valent +25 %. Sorts : Interposition et Tenir bon. | At the start of your turn, a shield of 9 on you and allies 1 cell away. Your shields on allies are worth +25%. Spells: Interpose and Stand Fast. |
| `rampart@50` | Le passif vaut 12 et porte à 2 cases. Sort : Mur de boucliers. | The passive is worth 12 and reaches 2 cells. Spell: Shield Wall. |
| `rampart@100` | Une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV. Le passif vaut 17. Interposition se relance en 1 tour. | Once per fight, an ally within 2 cells who would fall stays at 1 HP. The passive is worth 17. Interpose comes back in 1 turn. |
| `bastion@50` | Tenir bon te donne aussi un bouclier de 15. Sort : Rétorsion. | Stand Fast also gives you a shield of 15. Spell: Retaliation. |
| `bastion@100` | Rétorsion frappe aussi la case derrière la cible (dégâts × 0,8). Qui te frappe au contact est empoisonné : 8 pendant 2 tours, neutre. | Retaliation also strikes the cell behind the target (damage × 0.8). Whoever strikes you in melee is poisoned: 8 for 2 turns, neutral. |
| `breach` | Un ennemi achevé : +1 PM. Tes attirances portent d'une case de plus. Sorts : Assaut et Arrachement. | An enemy finished off: +1 MP. Your pulls reach one cell farther. Spells: Assault and Wrench. |
| `ram@50` | Un ennemi achevé : +1 PM et +1 PA. Sort : Trouée. | An enemy finished off: +1 MP and +1 AP. Spell: Breakthrough. |
| `ram@100` | Un ennemi achevé : +1 PM et +2 PA. Assaut se relance en 1 tour. | An enemy finished off: +1 MP and +2 AP. Assault comes back in 1 turn. |
| `harpooner@50` | +15 % de dégâts sur une cible à ton contact. Sort : Harpon. | +15% damage on a target next to you. Spell: Harpoon. |
| `harpooner@100` | Tes reculs contre un obstacle font 15 dégâts par case. Harpon n'a plus de relance, une fois par tour. | Your pushes into an obstacle do 15 damage per cell. Harpoon has no cooldown, once per turn. |
| `grip` | Qui te frappe au contact perd 1 PM. Tes retraits de PA et de PM durent 1 tour de plus, sauf ceux des sorts de composés. Sorts : Prise et Verrou. | Whoever strikes you in melee loses 1 MP. Your AP and MP losses last 1 turn longer, except those of compound spells. Spells: Hold and Lock. |
| `jailer@50` | Le passif retire aussi 1 PA, une fois par tour. Sort : Écrasement. | The passive also takes 1 AP, once per turn. Spell: Crush. |
| `jailer@100` | Écrasement fait +50 % sur un ennemi à 0 PM, au lieu de +30 %. Verrou se relance en 2 tours. | Crush does +50% on an enemy at 0 MP, instead of +30%. Lock comes back in 2 turns. |
| `pincer@50` | Provocation porte à +2 PO. Sort : Cercle de fer. | Taunt reaches +2 range. Spell: Iron Ring. |
| `pincer@100` | Les ennemis à ton contact perdent 1 PM au début de leur tour. Cercle de fer se relance en 1 tour. | Enemies next to you lose 1 MP at the start of their turn. Iron Ring comes back in 1 turn. |

**Sentinelle**

| id (v4) | FR | EN |
|---|---|---|
| `sightline` | Aucun ennemi à 3 cases au début de ton tour : +15 % de dégâts pendant 1 tour. Tes sorts en vue portent à +1 PO. Sorts : Tir visé et Point faible. | No enemy within 3 cells at the start of your turn: +15% damage for 1 turn. Your spells in sight reach +1 range. Spells: Aimed Shot and Weak Spot. |
| `marksman@50` | Tes sorts de 6 PO ou plus portent à +2 PO au lieu de +1. Sort : Coup au but. | Your spells of 6 range or more reach +2 range instead of +1. Spell: Bullseye. |
| `marksman@100` | Le passif monte à +25 % pendant 2 tours. Tir visé touche aussi la case derrière la cible. | The passive rises to +25% for 2 turns. Aimed Shot also hits the cell behind the target. |
| `barrage@50` | Le passif joue dès qu'aucun ennemi n'est à 2 cases, au lieu de 3. Sort : Tir de barrage. | The passive works as soon as no enemy is within 2 cells, instead of 3. Spell: Barrage. |
| `barrage@100` | Tir de barrage laisse sur sa ligne une glyphe de −1 PM pendant 1 tour, et se relance en 1 tour. | Barrage leaves a glyph of −1 MP along its line for 1 turn, and comes back in 1 turn. |
| `ambush` | Ton Faucon a 12 % de tes PV et profite de tes caractéristiques. Tes glyphes durent 1 tour de plus, sauf celles des sorts de composés. Sorts : Mâchoires et Piqué. | Your Hawk has 12% of your HP and draws on your characteristics. Your glyphs last 1 turn longer, except those of compound spells. Spells: Jaw Trap and Dive. |
| `falconer@50` | Ton Faucon a 20 % de tes PV. Sort : Chassé-croisé. | Your Hawk has 20% of your HP. Spell: Changeover. |
| `falconer@100` | Deux Faucons à la fois. Piqué n'a plus de relance, une fois par tour. | Two Hawks at once. Dive has no cooldown, once per turn. |
| `trapper@50` | Mâchoires se relance en 1 tour. Sort : Appât. | Jaw Trap comes back in 1 turn. Spell: Bait. |
| `trapper@100` | Deux Mâchoires à la fois. +15 % de dégâts sur un ennemi dans une de tes glyphes. | Two Jaw Traps at once. +15% damage on an enemy in one of your glyphs. |
| `trail` | Si on te frappe au contact : +2 PM pendant 1 tour. Tes sauts n'ont pas besoin de la vue. Sorts : Cabriole et Coup de talon. | If you are struck in melee: +2 MP for 1 turn. Your leaps need no sight. Spells: Vault and Heel Kick. |
| `leaper@50` | Cabriole n'a plus de relance, une fois par tour. Sort : Feinte. | Vault has no cooldown, once per turn. Spell: Feint. |
| `leaper@100` | Le passif monte à +3 PM. Une fois par combat, un coup au contact te donne un bouclier de 40. Cabriole donne +10 % de dégâts pour le tour. | The passive rises to +3 MP. Once per fight, a melee blow gives you a shield of 40. Vault gives +10% damage for the turn. |
| `runner@50` | +3 % de dégâts par case marchée depuis ton dernier sort, 15 % au plus. Sort : Tir en course. | +3% damage per cell walked since your last spell, 15% at most. Spell: Running Shot. |
| `runner@100` | +5 % par case, 25 % au plus. Si tu as marché 3 cases, ton dernier sort laisse un écho, à 50 % au tour suivant. | +5% per cell, 25% at most. If you walked 3 cells, your last spell leaves an echo, at 50% on the next turn. |

---

## 4. Phrases d'ambiance (27) [P]

Pour le champ `Lore`. Ce sont des paroles sans nom affiché, et chacune a sa voix. Aucune ne nomme un élément, un Primordial ni un mot réservé à l'Accord ou à la Disjonction (`talents-canon.md` §5), et la Brèche reste froide : ni rage, ni furie.

**Origine :** Mire, Affût, Palissade, Brèche et Thaumaturge reprennent `talents-canon.md` (voix de Mahaut, Bertille, Gautier, Rémi, Ghislaine). Si les phrases de `textes-s163.md` pour Mire, Affût et Sente diffèrent, garde celle que tu préfères : je n'ai pas pu les lire.

**Mage**

| id (v4) | FR | EN |
|---|---|---|
| `archmage` | « Un sort, ça se prépare debout et ça se lance une fois. Le reste, c'est de la gesticulation. » | "A spell is prepared standing still and cast once. The rest is flapping about." |
| `high-archmage` | « Trouve ta place, vise, et laisse le plafond se débrouiller. » | "Find your spot, take aim, and let the ceiling look after itself." |
| `patient` | « Une meule, ça couve des jours. Puis ça part d'un coup. Celui qui ouvre trop tôt n'a que de la fumée. » | "A charcoal stack smoulders for days. Then it goes all at once. Open it too early and all you get is smoke." |
| `sorcerer` | « Je n'ai touché personne. Demande-lui donc comment il se sent, demain. » | "I never laid a finger on him. Ask him how he's feeling tomorrow." |
| `plague` | « Un coup, ça se pare. Une mauvaise fièvre prend son temps, et elle gagne. » | "A blow can be parried. A bad fever takes its time, and it wins." |
| `hexer` | « Je ne le frappe pas. Je le montre du doigt, et tes amis font le reste. » | "I don't hit him. I point, and your friends do the rest." |
| `necromancer` | « Les morts restent où ils sont. Moi, je lève ce qu'ils ont laissé sur le mur. » | "The dead stay where they are. What I raise is what they left on the wall." |
| `binder` | « Une ombre ne pèse rien. Essaie donc de passer dans un couloir qui en est plein. » | "A shade weighs nothing. Try walking down a corridor full of them." |
| `reaper` | « Pose-la où il va marcher. Et recule un peu : elle ne fait pas la différence. » | "Set it where he's going to walk. And step back a bit: it can't tell friend from foe." |

**Garde**

| id (v4) | FR | EN |
|---|---|---|
| `palisade` | « Un pieu tout seul, c'est un bâton. Dix pieux qui se tiennent, c'est une palissade. Moi, je fais le pieu du milieu : je bouge pas. » | "One stake on its own is a stick. Ten stakes holding each other up, that's a palisade. Me, I'm the middle stake: I don't move." |
| `rampart` | « Reste derrière moi. Non, plus à gauche. Voilà : maintenant, tu vivras. » | "Get behind me. No, further left. There: now you'll live." |
| `bastion` | « Vas-y, frappe encore. Plus tu m'en donnes, plus je t'en rends. » | "Go on, hit me again. The more you give me, the more you get back." |
| `breach` | « Vingt ans à enfoncer des lignes, et maintenant je déblaie des routes. C'est le même métier, sauf que les cailloux, eux, se plaignent pas. » | "Twenty years breaking lines, and now I clear roads. Same trade, except the stones don't complain." |
| `ram` | « Un de moins, et il me reste du souffle. Au suivant. » | "One down, and I've breath left. Next." |
| `harpooner` | « Pourquoi lui courir après ? J'ai une corde, et il a un mur dans le dos. » | "Why chase him? I've got a rope, and he's got a wall behind him." |
| `grip` | « Tu voulais partir ? C'est gentil de me prévenir. » | "Leaving, were you? How kind of you to say." |
| `jailer` | « Celui-là ne jouera plus. Les autres, on verra après. » | "That one's done playing. We'll see about the rest later." |
| `pincer` | « Tous autour de moi, c'est ça. Maintenant, essayez donc de sortir. » | "All round me, that's it. Now try leaving." |

**Sentinelle**

| id (v4) | FR | EN |
|---|---|---|
| `sightline` | « Tu tires la semonce d'abord. Celui qui avance quand même, il a choisi. Toi, tu as juste visé. » | "Warning shot first. Anyone who keeps coming has made their choice. You've only taken aim." |
| `marksman` | « Une flèche. Si j'ai bien compté, il n'en faudra pas d'autre. » | "One arrow. If I've counted right, there won't need to be another." |
| `barrage` | « Je n'ai pas besoin de te toucher. Il suffit que tu ne passes pas. » | "I don't need to hit you. You only need not to get through." |
| `ambush` | « Chut. Le bois te parle, si tu te tais assez longtemps. Mon faucon l'a compris avant toi. » | "Hush. The woods will talk to you, if you keep quiet long enough. My hawk worked that out before you did." |
| `falconer` | « Il voit ce que je ne vois pas, et il revient quand je l'appelle. C'est tout le secret. » | "He sees what I can't, and he comes back when I call. That's the whole secret." |
| `trapper` | « Ce combat, je l'ai gagné hier, avec une pelle. Aujourd'hui, je regarde. » | "I won this fight yesterday, with a spade. Today I'm just watching." |
| `trail` | « Une sente ne va jamais tout droit. C'est pour ça qu'on ne t'y rattrape pas. » | "A trail never runs straight. That's why nobody catches you on it." |
| `leaper` | « Tu frappes, je suis là. Tu regardes, je n'y suis plus. » | "You strike, I'm there. You look, I'm gone." |
| `runner` | « Reste planté là si tu veux viser. Moi, je tire en passant. » | "Stand still to aim if you like. I shoot in passing." |

---

## 5. La quête « Une route à soi », et Anatole

### 5.1 Ce qui sonnait faux

1. **Josette se contredit.** La quête parle de « ceux qui dorment chez elle », qui « en parlent tous ». Or son dialogue dit : « Douze lits, et onze vides depuis la dernière lune ». Il n'y a plus que ses « derniers voyageurs ».
2. **« Métier » désigne déjà autre chose.** Le jeu appelle métiers la récolte et l'artisanat (`JobBook`). « Pour chaque métier » se lit de travers.
3. **« Ma colonne » arrive sans explication.** Le joueur n'a jamais vu cette colonne. Il faut dire d'abord ce qu'est une ligne du livre.
4. **Josette ne sonnait pas comme Josette.** Elle pose des questions sans attendre les réponses et elle nourrit (`textes-v7.md`). Sa réplique commençait bien, puis n'apprenait rien : l'aller-retour n'avait pas de raison d'être.
5. **Rien ne présentait les 3 chemins**, et rien ne disait qu'une rature se paie, alors que le remerciement parle du premier choix gratuit.

Ce que je garde, parce que c'est juste : « mon livre aime mieux une rature qu'un mensonge », la case en blanc, la soupe chaude, le crayon. La rature répond au « hasard » barré du dialogue, et elle sert la philosophie du jeu sans rien révéler.

### 5.2 Texte final de la quête (`a-road-of-ones-own`)

Rien ne change hors de ces trois champs : donneur, objectif, niveau, récompenses et `after` restent tels quels.

**`text`**
- fr : « Dans mon livre, chaque voyageur a une ligne : nom, route, et façon de la prendre. Cette dernière colonne, personne ne la remplit. Demande à Josette ce qu'en disaient ceux qui dormaient chez elle, puis reviens. La tienne, je l'écrirai au crayon : une route, ça se change, et mon livre aime mieux une rature qu'un mensonge. »
- en : "In my book, every traveller has a line: name, road, and the way they mean to take it. Nobody fills in that last column. Ask Josette what the people who slept at her inn made of it, then come back. Yours I'll write in pencil: a road can be changed, and my book prefers a crossing-out to a lie."

**`objectives[0].line` (Josette)**
- fr : « Les routes d'Anatole ? Tu as mangé ? Mes derniers voyageurs en parlaient tous, à table : ceux qui tirent, ceux qui tiennent, ceux qui lancent, trois façons chacun, et pas deux du même avis. Prends celle qui te va, ou aucune pour l'instant : la soupe sera chaude quand même. Et retourne le lui dire, il s'inquiète pour sa colonne. »
- en : "Anatole's roads? Have you eaten? My last travellers all talked about them at table: those who shoot, those who stand, those who cast, three ways apiece, and no two of the same mind. Take the one that suits you, or none for now: the soup will be hot either way. And go back and tell him, he frets about his column."

**`thanks`**
- fr : « Voilà ta ligne, et la case en blanc au bout. À toi de la remplir : ouvre ta fiche (C), page « Spécialisation », et choisis. Rien ne presse : la case attend, le premier choix ne coûte rien, et seules les ratures se paient. »
- en : "There's your line, and the blank box at the end of it. It's yours to fill: open your character sheet (C), the Specialisation page, and choose. No hurry: the box will wait, the first choice costs nothing, and only crossings-out are paid for."

### 5.3 Ce qu'Anatole ajoute pour présenter les 3 chemins

**Oui, le dialogue en a besoin**, mais la page dit déjà tout : ce dialogue reste facultatif, et la quête ne le demande pas. **Sans code :** une réponse ne peut pas dépendre de la classe (`DialogueAnswer` n'a pas de condition). C'est donc le joueur qui répond, et il peut aussi lire les chemins des autres classes.

**Une seule modification à `hello` :** il a déjà 4 réponses, le maximum. « Une route à soi ? » y prend donc la place de « Pourquoi par l'est ? », qui reste proposé par `topic1` et `topic2` : `topic3` reste atteignable.

```json
"own": {
  "text": {
    "fr": "Nom, route, et façon de la prendre. J'ai ajouté cette dernière colonne l'an dernier, et personne ne la remplit. Toi, comment tu te bats ?",
    "en": "Name, road, and the way you mean to take it. I added that last column a year ago, and nobody fills it in. You, how do you fight?"
  },
  "answers": [
    { "text": { "fr": "Je tire de loin.", "en": "I shoot from afar." }, "next": "ways-sentinel" },
    { "text": { "fr": "Je tiens devant.", "en": "I stand in front." }, "next": "ways-guard" },
    { "text": { "fr": "Je lance des sorts.", "en": "I cast spells." }, "next": "ways-mage" },
    { "text": { "fr": "Une autre fois.", "en": "Another time." } }
  ]
},
"ways-sentinel": {
  "text": {
    "fr": "Qui tire a trois façons. Mire : voir loin, compter, ne rien laisser arriver. Affût : préparer le terrain, avec un piège ou un faucon. Sente : ne jamais être là où on t'attend.",
    "en": "Those who shoot have three ways. Sightline: see far, count, let nothing arrive. Ambush: ready the ground, with a trap or a hawk. Trail: never be where you're expected."
  },
  "answers": [
    { "text": { "fr": "Et plus loin ?", "en": "And further on?" }, "next": "later" },
    { "text": { "fr": "Une autre façon de se battre ?", "en": "Another way of fighting?" }, "next": "own" },
    { "text": { "fr": "Merci.", "en": "Thanks." } }
  ]
},
"ways-guard": {
  "text": {
    "fr": "Qui tient devant a trois façons. Palissade : rester debout pour ceux d'à côté. Brèche : passer en premier, pour que les autres passent. Étau : attraper, et ne plus lâcher.",
    "en": "Those who stand in front have three ways. Palisade: stay on your feet for whoever's beside you. Breach: go through first, so the others can follow. Grip: catch hold and don't let go."
  },
  "answers": [
    { "text": { "fr": "Et plus loin ?", "en": "And further on?" }, "next": "later" },
    { "text": { "fr": "Une autre façon de se battre ?", "en": "Another way of fighting?" }, "next": "own" },
    { "text": { "fr": "Merci.", "en": "Thanks." } }
  ]
},
"ways-mage": {
  "text": {
    "fr": "Qui lance a trois façons. Archimage : choisir sa place, et le plus gros sort qu'on peut payer. Maléfice : presque rien sur le moment, beaucoup le lendemain. Lève-ombres : ne plus jamais se battre seul.",
    "en": "Those who cast have three ways. Archmage: pick your spot, and the biggest spell you can pay for. Hex: next to nothing at the time, a great deal the morning after. Shadecaller: never fight alone again."
  },
  "answers": [
    { "text": { "fr": "Et plus loin ?", "en": "And further on?" }, "next": "later" },
    { "text": { "fr": "Une autre façon de se battre ?", "en": "Another way of fighting?" }, "next": "own" },
    { "text": { "fr": "Merci.", "en": "Thanks." } }
  ]
},
"later": {
  "text": {
    "fr": "Plus loin, chaque route se sépare encore en deux. Quand ta case sera ouverte, ça se choisit sur ta fiche, page « Spécialisation ». Le premier trait est gratuit ; les ratures se paient en Éclats : un crayon, ça s'use.",
    "en": "Further on, each road splits in two again. Once your box is open, you choose on your character sheet, the Specialisation page. The first stroke is free; crossings-out cost Éclats: pencils wear down."
  },
  "answers": [
    { "text": { "fr": "Une autre façon de se battre ?", "en": "Another way of fighting?" }, "next": "own" },
    { "text": { "fr": "Merci.", "en": "Thanks." } }
  ]
}
```

**Dans `hello`**, la réponse « Pourquoi par l'est ? » (`next: topic3`) est remplacée par :
```json
{ "text": { "fr": "Une route à soi ?", "en": "A road of one's own?" }, "next": "own" }
```

**Vérifié à la lecture :** les lignes nouvelles sont atteignables, chacune peut se fermer, et aucune n'a plus de 4 réponses. Anatole reste sec, il compte et il corrige. Le dialogue ne suppose ni le niveau ni l'état de la quête : il se lit de la même façon avant et après. Non exécuté : c'est à `Data.cs` et aux tests de le confirmer.

**Compatibilité avec le groupe et la reprise :** la quête reste un `Talk` par personnage, et le choix une colonne du personnage. Un joueur qui ne parle pas à Anatole de tout cela ne perd rien.

---

## 6. Textes d'interface à retoucher [P]

| Clé | Aujourd'hui | Proposition FR | Proposition EN |
|---|---|---|---|
| `path.tier.30` | correct | sans changement | sans changement |
| `path.tier.50` | ne parle que des trios, alors que la lignée s'ouvre | Niveau 50 : l'Affirmation. Les trios se créent sur la page Éléments, et les routes se séparent encore, page Spécialisation. | Level 50: the Affirmation. Trios are created on the Elements page, and the roads split again, on the Specialisation page. |
| `path.tier.100` | n'annonce rien | Niveau 100 : l'Accomplissement. Ta spécialisation atteint sa dernière évolution. | Level 100: the Fulfilment. Your specialisation reaches its last evolution. |
| `path.evolved` | convient à tous les titres : pas d'article, pas d'accord | sans changement | sans changement |
| `compounds.reset` | correct avec D94 | sans changement | sans changement |

**Le mot « lignée » : à garder hors de l'écran.** Il va très bien dans les documents, mais il dit la descendance, et la Saga III parle des enfants des Primordiaux. Personne n'en tirera rien aujourd'hui ; il vaut mieux ne pas donner de faux indice. Le joueur lit « ta route se sépare encore » et voit les deux nœuds.

Les textes de la page (§v4.5), si tu en as besoin :

| Situation | FR | EN |
|---|---|---|
| Quête non faite | Rends-toi à Quatre-Chemins : Anatole t'ouvrira une case dans son livre. | Go to Four Ways: Anatole will open a box for you in his book. |
| Branche sans second choix, à 50 ou plus | Ta route se sépare : choisis la suite. Le premier choix est gratuit. | Your road splits: choose what comes next. The first choice is free. |
| Le paquet composé à la main | Ce sort n'entre pas seul dans ton paquet : ouvre ton livre pour lui faire une place. | This spell won't join your deck by itself: open your book to make room for it. |

---

## 7. Contradictions et points à trancher

**Avec la bible :** aucune contradiction sur l'Accord, les Primordiaux ou la chronologie. Aucun texte ici ne parle du cinquième, et la phrase du gardien n'est pas touchée. Un seul vrai point de tension avec le canon :

1. **D93 et D94 contre l'indice du héros (à trancher par Charles).** La bible fait du mélange des éléments une anomalie (créatures à plusieurs affinités, §2 ; machine rituelle du Cœur, §8). Le jeu dit par Garance que le héros passe « d'un élément à l'autre sans qu'ils se marchent dessus », et c'est l'indice discret de la Saga I. Or, depuis D94, le héros **fusionne** deux éléments dès 30 et perd ses parents : c'est presque le contraire de ce que dit la réplique, et ça ressemble à ce que fait le Disjoint. Ma lecture possible, non écrite dans le jeu : le héros est le seul à fusionner sans se détruire, ce qui renforce l'indice. Rien de ce fichier n'en dépend, mais `well-equipped` risque de sonner faux. Je ne touche à rien.

**Avec mes propres documents :**

2. **`talents-canon.md` est dépassé en partie.** Six voies et six maîtres y ont cédé la place aux 9 branches, et son « ne relève jamais les morts » est tenu ici par les ombres. Sa liste de noms interdits contenait aussi nécromancien, thaumaturge, tireur d'élite. Le fichier devrait porter une ligne qui renvoie à celui-ci.
3. **« Le titre de 30 ne change pas » (§v4.4).** Sorcier et Nécromancien changent quand même, à cause de l'accord. C'est le seul écart à la v4 sur les titres.

**Dans la conception, à confirmer par le game designer :**

4. **La Palissade à 12 et 17.** La retouche 11 lie 3 passifs ; le tableau ne donne le 17 qu'au Rempart, et le 12 nulle part. J'ai écrit : Rempart à 12 puis 17, **Bastion reste à 9** à 1 case. Si le Bastion doit monter, il faut changer `bastion@50` et `@100`.
5. **La règle « sauf jumeaux ».** La retouche 5 dit « sorts neutres et sorts de voie » et aussi « `Twin` vide ». Ce n'est pas pareil : un sort de Feu de base a `Twin` vide. J'ai écrit « sauf ceux des sorts de composés », conforme à `Twin` vide. Si seuls les neutres et la voie comptent, le texte change.
6. **L'ombre.** Ni son sort, ni la portée de Lever une ombre, ni la case où naît celle du passif ne sont donnés.
7. **Le Faucon.** La retouche 1 dit « ses caractéristiques », la brique E « une part » de celles de l'invocateur. D'où « profite de tes caractéristiques », vrai dans les deux cas.
8. **Où se fait le premier choix.** Le §v4.5 dit « chez l'habitant des voies », T146 dit « un village suffit », et la quête dit « ouvre ta fiche ». J'ai gardé le texte de la quête. Il ne dit pas où l'on paie une rature : le §v4.5 demande un village.

**À ton goût ou à celui de Charles :**

9. **Thaumaturge** est un mot rare, et ma liste du 23 octobre l'écartait. Je le garde : il est épicène, déjà dans la v4, et unique dans le jeu. Sinon : **Longue-mèche / Slow Fuse**.
10. **Les autres jeux.** Je n'ai pas d'accès au web. Je ne connais aucun de ces titres chez Ankama, sans pouvoir le garantir, et Archimage, Thaumaturge, Bastion sont des mots communs à beaucoup de jeux. À vérifier (D54) pour Astromage et les composés.
11. **« Ombre » et les cinq ombres de la Chambre sans nom (bible §8).** Le mot sera aussi celui des invocations de la Mage. Aucun lien n'est suggéré, et aucun texte futur ne devra en créer un par accident.

**Trouvés en lisant, sans rapport direct, non corrigés :**

12. `spells.json` : **« Sceau de Eau »** devrait être « Sceau d'Eau ». Ces « Sceau » figurent aussi dans ma liste de mots évités.
13. `spells.json` : **« Fureur d'orage »** (jumeau de la Garde) emploie un mot que `talents-canon.md` écartait pour la Garde, et la colère est un symptôme de l'arc de Kaor.
14. `specs.json` est passé à `base` et `forks` pendant ce travail : les lignées y recopient encore la description de leur voisine. Le §3.2 corrige cela.
