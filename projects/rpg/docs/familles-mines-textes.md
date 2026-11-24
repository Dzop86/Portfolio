# Osmose : relecture des textes des familles des Mines de Sous-Racine

> Relecture du scénariste, 21 novembre 2026, sur `docs/familles-mines.md` (§3 et §6.2). Seuls les textes affichés changent : **aucun id ne bouge**.
> Sources croisées : `docs/bible.md`, la bible de Charles (§2, §8, §14, §15, §19), `docs/chapitre-2-pistes.md` §5, `docs/familles-foret-textes.md`, `data/quests.json` (`the-tree-town`, `the-unmapped-passage`, `mossy-keepers`, `roots-of-the-mother-tree`), `data/dialogues/gaspard.json`, `data/dialogues/berold.json`, `data/monsters.json`, `data/passives.json`, `data/spells.json`, `data/summons.json`, `data/items.json`, `data/sets.json`.
> Cette version couvre les **deux familles** : les **Fouille-rocs** (S225, §1, remarques au §2) et les **Vers de roche** (S226, §3, relus le 24 novembre 2026 sur `docs/familles-mines.md` §4, §5.2 et §6.2).

## 1. Les Fouille-rocs (`delver`), sprint 225

Les lignes marquées **(changé)** diffèrent de la proposition ; les raisons sont au §1.9. Tout le reste est repris tel quel.

### 1.1 La famille

| id | fr | en |
|---|---|---|
| `delver` (nom de famille) | Fouille-rocs **(changé)** | Rockdelvers |

### 1.2 Les monstres

| id | fr | en |
|---|---|---|
| `shotfirer` | Boutefeu | Shotfirer |
| `cinder-flinger` | Jeteur d'escarbilles | Cinder Flinger |
| `timberman` | Étançonneur | Timberman |
| `lamp-bearer` | Porte-lanterne | Lantern Bearer **(changé)** |
| `stonelistener` | Écoute-roche | Stonelistener |

### 1.3 Les sorts

| id | fr | en |
|---|---|---|
| `pick-blow` | Coup de pic | Pick Strike **(changé)** |
| `powder-charge` | Tonnelet de poudre **(changé)** | Powder Keg **(changé)** |
| `cinder-toss` | Escarbilles | Cinders |
| `cinder-spray` | Gerbe d'escarbilles | Cinder Spray |
| `prop-ram` | Coup d'étai | Prop Ram |
| `wedging` | Calage | Wedging |
| `swig` | Lampée | Swig |
| `lamp-swing` | Coup de lanterne | Lantern Swing **(changé)** |
| `great-pick` | Grand pic | Great Pick |
| `powder-barrel` | Baril de poudre | Powder Barrel |
| `lamp-hurl` | Lanterne lancée | Hurled Lantern |

### 1.4 Les passifs

| id | Nom fr | Nom en | Description fr | Description en |
|---|---|---|---|---|
| `dogged` | Acharné | Dogged | S'il n'a pas marché à son tour précédent, ou s'il n'a pas encore joué : +20 % de dégâts ce tour-ci. **(changé)** | If it did not walk on its last turn, or has not played yet: +20% damage this turn. **(changé)** |
| `ringing-stone` | La roche qui sonne | The Ringing Rock | Sous la moitié de ses PV, il colle l'oreille à la paroi, et la roche sonne. Il allume toutes ses mèches : deux tonnelets à ses pieds, +2 PA et −10 % de résistances jusqu'à la fin du combat. **(changé)** | Below half its HP, it presses its ear to the wall, and the rock rings. It lights every fuse it carries: two kegs at its feet, +2 AP and −10% resistances until the end of the fight. **(changé)** |
| `short-fuse` | Mèche courte **(changé)** | Short Fuse **(changé)** | La mèche brûle : perd 1 PV au début de chacun de ses tours (il en a 2). **(changé)** | The fuse is burning: loses 1 HP at the start of each of its turns (it has 2). **(changé)** |
| `keg-blast` | Coup de mine | Blast | À sa mort, saute : 20 à 26 dégâts Feu à 2 cases, sur tout le monde, Fouille-rocs compris. **(changé)** | At its death, it blows up: 20 to 26 Fire damage 2 cells around, to everyone, Rockdelvers included. **(changé)** |
| `barrel-blast` | Grand coup de mine | Great Blast | À sa mort, saute : 34 à 42 dégâts Feu à 2 cases, sur tout le monde, Fouille-rocs compris. **(changé)** | At its death, it blows up: 34 to 42 Fire damage 2 cells around, to everyone, Rockdelvers included. **(changé)** |

### 1.5 Les invocations

| id | fr | en |
|---|---|---|
| `powder-keg` | Tonnelet de poudre **(changé)** | Powder Keg |
| `powder-barrel` | Baril de poudre | Powder Barrel |

### 1.6 Les ressources

| id | fr | en |
|---|---|---|
| `fuse-cord` | Bout de mèche **(changé)** | Fuse Cord |
| `lode-chip` | Morceau de filon **(changé)** | Lode Chip |

### 1.7 La panoplie et ses pièces

| id | fr | en |
|---|---|---|
| `delver` (panoplie) | Panoplie du fouille-roc | Rockdelver set |
| `gallery-boots` | Bottes cloutées **(changé)** | Hobnailed Boots **(changé)** |
| `lantern-helmet` | Casque à lanterne | Lantern Helmet |
| `prop-pauldrons` | Épaulières d'étai | Prop Pauldrons |
| `listener-pick` | Pic de l'Écoute-roche | Stonelistener's Pick |

Dans `tools/sets_gen.py`, la ligne de `gallery-boots` devient : `piece("gallery-boots", "Bottes cloutées", "Hobnailed Boots", "Boots", 46, "cinder-flinger", {"fire": 20, "vitality": 16}, 30)`.

### 1.8 La carte de combat

| id | fr | en |
|---|---|---|
| `underroot-gallery` | Galerie étayée | Timbered Gallery **(changé)** |

### 1.9 Le bestiaire (`lore`)

| id | fr | en |
|---|---|---|
| `delver` (famille) | Les Fouille-rocs creusaient sous l'Arbre bien avant les mineurs d'Ormeval, et ils ne remontent jamais. Petits, trapus, une lanterne au front, ils suivent un son qu'ils étaient seuls à entendre : la roche qui sonne. Quand une paroi leur résiste, ils la font sauter, et tant pis pour qui se trouve à côté, même l'un des leurs. **(changé)** | The Rockdelvers were digging beneath the Tree long before Ormeval's miners, and they never come back up. Small and stocky, a lantern on the brow, they follow a sound only they used to hear: the ringing of the rock. When a wall won't give, they blow it up, and too bad for whoever is standing by, even one of their own. **(changé)** |
| `shotfirer` | Il ne regarde jamais la mèche : il écoute la roche. | It never watches the fuse: it listens to the rock. |
| `cinder-flinger` | Il garde dans un sac les braises de sa lanterne, et vous les lance encore chaudes. **(changé)** | It keeps its lantern's embers in a sack and throws them at you still hot. **(changé)** |
| `timberman` | Il cale tout ce qui bouge, la roche comme vous. | It wedges anything that moves, the rock and you alike. **(changé)** |
| `lamp-bearer` | Autour de sa lanterne, les autres creusent plus fort. | Around its lantern, the others dig harder. |
| `stonelistener` | Il colle l'oreille à la paroi et sourit : là-dessous, quelque chose sonne. | He presses his ear to the wall and smiles: something down there is ringing. **(changé)** |

### 1.10 Les changements et leurs raisons (17)

| # | id | Proposé fr / en | Retenu fr / en | Raison |
|---|---|---|---|---|
| 1 | `delver` (famille) | Fouille-roc / Rockdelvers | **Fouille-rocs** / Rockdelvers | Le nom de famille est au pluriel, comme « Gardiens moussus » et « Sylvecerfs ». Pour un composé verbe et nom, le nom prend la marque du pluriel (« des tire-bouchons »). Au singulier, dans la panoplie, on garde « fouille-roc ». Majuscule pour le nom inventé, comme « les Sylvecerfs ». |
| 2 | `lamp-bearer` | Porte-lanterne / Lamp Bearer | Porte-lanterne / **Lantern Bearer** | Le français dit « lanterne » partout ; l'anglais mélangeait *lamp* et *lantern* (*Lantern Helmet*, *Hurled Lantern*). Une seule lanterne, un seul mot. |
| 3 | `lamp-swing` | Coup de lanterne / Lamp Swing | Coup de lanterne / **Lantern Swing** | Suit le 2. |
| 4 | `pick-blow` | Coup de pic / Pick Blow | Coup de pic / **Pick Strike** | *Pick Blow* ne se dit pas. *Strike* suit aussi le dialogue de Gaspard (« *strike it* »). |
| 5 | `powder-charge` (sort) | Charge de poudre / Powder Charge | **Tonnelet de poudre / Powder Keg** | Le Guerrier a déjà un sort « Charge / Charge » (`spells.json`) : le journal de combat mélangerait les deux. L'objet posé est dessiné comme un tonnelet (`Keg`). Le sort porte le nom de ce qu'il pose, comme « Faucon » ou « Image ». |
| 6 | `powder-keg` (invocation) | Charge de poudre / Powder Keg | **Tonnelet de poudre** / Powder Keg | Suit le 5. Le français et l'anglais disent enfin la même chose, et « tonnelet » répond à « baril » : le petit et le grand. |
| 7 | `ringing-stone` (description) | « … Il allume tout ce qu'il porte : +2 PA, −10 % de résistances jusqu'à la fin du combat, et deux charges à ses pieds. » | « … Il allume toutes ses mèches : deux tonnelets à ses pieds, +2 PA et −10 % de résistances jusqu'à la fin du combat. » (en : *It lights every fuse it carries: two kegs…*) | « Tout ce qu'il porte » laisse croire qu'il prend feu. Suit le 5. La fin reprend la forme de `cracked-antlers` (« +2 PA et −10 % de résistances jusqu'à la fin du combat »). |
| 8 | `dogged` (description) | « S'il n'a pas marché à son tour précédent, il frappe à +20 % ce tour-ci. » | « S'il n'a pas marché à son tour précédent, ou s'il n'a pas encore joué : +20 % de dégâts ce tour-ci. » | Le passif se déclenche dès le premier tour (`familles-mines.md` §0.1, point 7). Sans le dire, la première volée à +20 % paraît une erreur au joueur. « +20 % de dégâts » est la forme des autres passifs. |
| 9 | `short-fuse` (nom) | Mèche / Fuse | **Mèche courte / Short Fuse** | Le nom flotte au-dessus du tonnelet à chaque tour : « Mèche courte » dit le danger mieux que « Mèche ». C'est l'expression courante en anglais, et c'est l'id. |
| 10 | `short-fuse` (description) | « La mèche brûle : perd 1 PV au début de chacun de ses tours. » | « … (il en a 2). » | Le joueur compte lui-même : il voit qu'il a un tour. On ne dit pas « au deuxième tour », qui serait faux si le Porte-lanterne le soigne. |
| 11 | `keg-blast`, `barrel-blast` (descriptions) | « En sautant : 20 à 26 dégâts de Feu à 2 cases, sur tout le monde, Fouille-roc compris. » | « À sa mort, saute : 20 à 26 dégâts Feu à 2 cases, sur tout le monde, Fouille-rocs compris. » | La forme suit `explosion` (« À sa mort, explose : 10 à 14 dégâts Terre à 2 cases, alliés compris. »). « À sa mort » dit aussi qu'il saute quand on le frappe. |
| 12 | `fuse-cord` | Cordeau / Fuse Cord | **Bout de mèche** / Fuse Cord | Seul, « cordeau » se lit comme la ficelle du jardinier. « Mèche » rappelle le passif du tonnelet : la ressource s'explique toute seule. |
| 13 | `lode-chip` | Éclat de filon / Lode Chip | **Morceau de filon** / Lode Chip | La monnaie s'appelle les **Éclats**. « 3 Éclats de filon » dans le butin, à côté des Éclats gagnés, prêterait à confusion. Le passif des sangliers s'appelle déjà « Éclat gravé ». « Fragment » est exclu : c'est le mot des Fragments des Primordiaux. |
| 14 | `gallery-boots` | Bottes de galerie / Gallery Boots | **Bottes cloutées / Hobnailed Boots** | La panoplie des taupes s'appelle « Panoplie des galeries » (`sets.json`), et leur donjon les « Galeries » : un joueur rangerait ces bottes chez les taupes. Les Vers auront aussi un sort « Galerie » (S226). Les bottes cloutées sont celles des mineurs, et tout le monde les voit. |
| 15 | `underroot-gallery` (en) | Propped Gallery | **Timbered Gallery** | *Propped* ne se dit pas d'une galerie ; *timbered* est le mot des mines britanniques pour une galerie étayée de bois. Le français est bon. |
| 16 | `delver` (bestiaire) | « … ils suivent un son que personne d'autre n'entend : la roche qui sonne. … » | « … ils suivent un son qu'ils étaient seuls à entendre … » | **Erreur de canon :** Gaspard l'entend, à Clairval (« Depuis peu, la carrière sonne quand on la frappe », `gaspard.json`), et `moved-mountains` dira « ici, elle sonne aussi ». L'imparfait garde le mystère et fait le lien sans le dire : ils l'entendaient avant tout le monde. J'ai aussi repris « ils ne remontent jamais » du Narratif (§3), retiré « vissée » (une lanterne ne se visse pas au front) et remplacé « eux compris » par « même l'un des leurs », plus naturel après « qui se trouve à côté ». |
| 17 | `cinder-flinger`, `timberman`, `stonelistener` (bestiaire) | « Il jette ce qui reste au fond de sa lanterne. » ; *the rock as well as you* ; *It presses its ear… beneath, something is ringing.* | voir §1.9 | Le Jeteur porte un sac d'escarbilles (allure `delver-ranged`) et une petite lanterne au front : il ne peut pas la vider à la main, il garde ses braises dans le sac. En anglais, *the rock and you alike* se dit mieux ; les chefs sont *he* dans le bestiaire (« *His fur is the colour of bark* », « *When his antlers split* ») ; *beneath, something is ringing* n'est pas naturel. |

**Repris tel quel :** les 5 monstres en français, 4 sur 5 en anglais ; 10 sorts sur 11 en français, 8 sur 11 en anglais ; les noms de `dogged`, `ringing-stone`, `keg-blast`, `barrel-blast` ; `powder-barrel` ; 3 pièces sur 4 ; le nom de la panoplie ; « Galerie étayée » ; 4 phrases du bestiaire sur 6 en français.

## 2. Remarques de canon (rien n'est corrigé dans le document du game designer)

1. **La roche qui sonne est récente.** Gaspard dit « depuis peu » et « cinquante ans de mine, et je n'avais jamais entendu ça ». Le Narratif du §3 (« un son que personne d'autre n'entend ») le contredisait ; le changement 16 le corrige dans le bestiaire. Le §3 de `familles-mines.md` garde l'ancienne phrase : à aligner si on la recopie ailleurs.
2. **Un seul indice par chef.** Le §3.3 propose d'ajouter « une coupe de roche à l'envers » derrière l'Écoute-roche. Je le déconseille : c'est le signe du Ronge-montagne (`strata`, §4.3) et le cœur de `moved-mountains`. Deux chefs de la même zone montreraient la même chose, et la quête de niveau 50 perdrait sa découverte (même remarque que pour les cerfs et les gardiens, `familles-foret-textes.md` §3, point 2). Le partage proposé : **l'Écoute-roche porte le son** (l'écho de Gaspard), **le Ronge-montagne porte les couches** (l'écho de `moved-mountains`).
3. **Ce ne sont pas les mineurs de Berthe.** Le bestiaire les place « bien avant les mineurs d'Ormeval ». Ça tient avec les quêtes : Berthe ne parle que des pierres qui marchent (`the-tree-town`, `mossy-keepers`), car son équipe n'a pas encore descendu sous l'Arbre. Ça tient aussi avec Bérold, « Je sais tout ce qui se passe sous cet Arbre » (`roots-of-the-mother-tree`) : il se trompe, et le joueur le verra. C'est voulu (bible §14 : des personnages qui se trompent).
   - **[P] pour la fiche de `keep-or-dig` ou `moved-mountains` :** une réplique de Berthe sur eux aiderait le joueur à les situer, par exemple « Les petits à lanterne ? Ils étaient là avant nous. Ils ne volent rien, ils creusent, et ils écoutent. » / "The little ones with lanterns? They were here before us. They don't steal, they dig, and they listen." Ce n'est qu'une proposition.
4. **`moved-mountains` et `keep-or-dig` ne sont pas encore dans `data/quests.json`** : ils n'existent que dans `chapitre-2-pistes.md` §5. Le bestiaire ne s'y appuie donc que par le son, déjà établi par Gaspard.
5. **Du Feu dans la région de la Terre.** Les Fouille-rocs sont de Feu et frappent en Terre au pic. C'est cohérent avec la bible (§2 : « certaines créatures manifestent plusieurs affinités élémentaires », §6 : une culture n'est jamais réduite à un trait) et aucun texte ne l'explique. Il faut que ça le reste.
6. **Rien ne dépasse la Saga I.** Aucun texte ne cite l'Accord, un Primordial, la Disjonction, un enfant de Primordial ni le mot « cinquième ». « Là-dessous, quelque chose sonne » reste au niveau 1 (un phénomène réel, sans cause). La réplique du gardien de la Chambre sans nom n'apparaît pas.
7. **Le mot « charge » dans les documents.** Le game designer et les tests parlent de « charge » (`APowderKeg_…`, la leçon du §3) : c'est sans effet, ce sont des ids et du texte interne. Seul le texte affiché devient « tonnelet ».
8. **Pour S226, le mot « galerie »** reviendra quatre fois : le donjon et la panoplie des taupes, la carte « Galerie étayée » et le sort des Vers « Galerie / Tunnel » (avec le Ver de galerie). Je le traiterai à la relecture des Vers ; le changement 14 en retire déjà un. **Traité au §3.10** (changements 1 à 3 et 12) : les Vers n'ont plus aucune « galerie ».
9. **Ankama (D54).** Je ne connais aucune reprise : « boutefeu », « étançonneur », « escarbille », « lampée », « calage », « coup de mine » et « bottes cloutées » sont des mots communs du français, souvent du vocabulaire des mines. « Coup d'étai » est un jeu de mots sur « coup d'État », gardé pour l'humour. Je ne peux pas le garantir pour « Fouille-roc », « Porte-lanterne » et « Écoute-roche » : Charles peut faire une recherche rapide.
10. **Les ids ne posent pas de problème.** `powder-charge`, `gallery-boots` et `lamp-bearer` ne correspondent plus tout à fait à leurs textes. Ce sont des ids internes, stables par règle (bible §16, §19) : on les garde.

## 3. Les Vers de roche (`borer`), sprint 226

Les lignes marquées **(changé)** diffèrent de la proposition (`familles-mines.md` §4, §5.2 et §6.2) ; les raisons sont au §3.10. Tout le reste est repris tel quel. Le dev a déjà recopié la proposition dans `tools/spells_gen.py` et `data/` : les lignes à reprendre sont au §3.11.

### 3.1 La famille

| id | fr | en |
|---|---|---|
| `borer` (nom de famille) | Vers de roche | Rock Worms |

### 3.2 Les monstres

| id | fr | en |
|---|---|---|
| `stonegnawer` | Ronge-pierre | Stonegnawer |
| `grit-spitter` | Cracheur de gravier | Grit Spitter |
| `millstone-worm` | Ver-meule | Millstone Worm |
| `tunnel-worm` | Ver tunnelier **(changé)** | Tunnel Worm |
| `mountain-gnawer` | Ronge-montagne | Mountain Gnawer |

### 3.3 Les sorts

| id | fr | en |
|---|---|---|
| `stone-bite` | Morsure de pierre | Stone Bite |
| `burrow-dive` | Plongée | Plunge **(changé)** |
| `grit-spit` | Crachat de gravier | Grit Spit |
| `millstone-crush` | Écrasement de meule **(changé)** | Millstone Crush |
| `snatch` | Happer **(changé)** | Snatch |
| `rock-slime` | Bave de roche | Rock Slime |
| `tunnel-swap` | Tunnel **(changé)** | Tunnel |
| `tunnel-nip` | Coup de dent **(changé)** | Snap **(changé)** |
| `mountain-maw` | Gueule de montagne | Mountain Maw |
| `engulf` | Engloutir | Engulf |
| `deep-dive` | Plongée profonde | Deep Plunge **(changé)** |

### 3.4 Les passifs

| id | Nom fr | Nom en | Description fr | Description en |
|---|---|---|---|---|
| `rock-hide` | Cuir de roche | Rock Hide | Au début de son tour, son cuir se referme : +20 % de résistances. Quand il frappe, il s'ouvre : −20 % jusqu'à son tour suivant. | At the start of its turn, its hide closes: +20% resistances. When it strikes, it opens: −20% until its next turn. |
| `coils` | Anneaux | Coils | Une fois par tour, un allié à 2 cases qui tomberait reste à 1 PV (jamais une invocation) : le ver l'abrite dans ses anneaux. **(changé)** | Once per turn, an ally within 2 cells who would fall stays at 1 HP (never a summon): the worm shelters it in its coils. **(changé)** |
| `strata` | Couches à l'envers | Overturned Strata **(changé)** | Sous la moitié de ses PV, son cuir se fend : dessous, des couches de roche à l'envers. Deux vermisseaux en sortent. +20 % de dégâts jusqu'à la fin du combat. **(changé)** | Below half its HP, its hide splits: beneath it, layers of rock upside down. Two wormlings crawl out. +20% damage until the end of the fight. **(changé)** |

### 3.5 L'invocation

| id | fr | en |
|---|---|---|
| `wormling` | Vermisseau | Wormling |

### 3.6 Les ressources

| id | fr | en |
|---|---|---|
| `rock-slough` | Mue de roche | Rock Slough |
| `borer-tooth` | Dent de ver de roche **(changé)** | Rock Worm Tooth **(changé)** |

### 3.7 La panoplie et ses pièces

| id | fr | en |
|---|---|---|
| `borer` (panoplie) | Panoplie du ver de roche | Rock Worm set |
| `slough-cape` | Cape de mue | Slough Cape |
| `ringed-amulet` | Amulette annelée | Ringed Amulet |
| `millstone-shield` | Bouclier-meule | Millstone Shield |
| `gnawer-mandible` | Mandibule du Ronge-montagne | Mountain Gnawer's Mandible |

Rien à changer dans `tools/sets_gen.py`.

### 3.8 La carte de combat

| id | fr | en |
|---|---|---|
| `underroot-pillars` | Chambres et piliers | Rooms and Pillars |

« Chambres et piliers » est le nom français de la méthode d'exploitation (la mine dessinée par la carte). En anglais, le terme de métier est *room and pillar* ; le pluriel se lit mieux comme nom de lieu.

### 3.9 Le bestiaire (`lore`)

| id | fr | en |
|---|---|---|
| `borer` (famille) | Les vers de roche mangent la pierre et rendent du gravier. Leurs tunnels traversent les Mines dans tous les sens, et les mineurs s'en servent sans le dire. Leur cuir est de roche, et ne s'ouvre que lorsqu'ils attaquent. **(changé)** | Rock worms eat stone and leave gravel behind. Their tunnels cross the Mines every which way, and the miners use them without saying so. Their hide is rock, and it opens only when they attack. **(changé)** |
| `stonegnawer` | Il ne mord que ce qui bouge ; le reste, il le mange. | It bites only what moves; the rest it eats. |
| `grit-spitter` | Il crache ce qu'il n'a pas digéré, assez fort pour fendre un casque. | It spits out what it couldn't digest, hard enough to crack a helmet. **(changé)** |
| `millstone-worm` | Ses anneaux sont si épais qu'un ver blessé vient s'y abriter. **(changé)** | Its coils are so thick that a wounded worm comes to shelter in them. **(changé)** |
| `tunnel-worm` | Il connaît tous les tunnels, et y fait passer ceux qui saignent. **(changé)** | It knows every tunnel, and slips the bleeding ones through them. **(changé)** |
| `mountain-gnawer` | On dit qu'il a mangé le dessous d'une montagne : sa peau en porte les couches, à l'envers. **(changé)** | They say he ate the underside of a mountain: his hide bears its layers, upside down. **(changé)** |

### 3.10 Les changements et leurs raisons (16)

| # | id | Proposé fr / en | Retenu fr / en | Raison |
|---|---|---|---|---|
| 1 | `tunnel-worm` | Ver de galerie / Tunnel Worm | **Ver tunnelier** / Tunnel Worm | Remarque §2, point 8. « Galerie » est le mot des taupes (« Galeries des taupes », la panoplie, la « Clef des galeries ») et de la carte des Fouille-rocs (« Galerie étayée ») : un joueur rangerait ce ver chez les taupes. « Tunnelier » est le mot de ceux qui percent des tunnels, il dit le rôle (il fait passer les autres) et rejoint l'anglais. |
| 2 | `tunnel-swap` | Galerie / Tunnel | **Tunnel** / Tunnel | Suit le 1. « Tunnel » est aussi un mot français ; le sort et le Ver tunnelier se répondent. |
| 3 | `tunnel-nip` | Morsure de galerie / Tunnel Nip | **Coup de dent / Snap** | Suit le 1. Deux autres raisons : un monstre a déjà le sort `nip` « Mordillement / Nip » (`spells.json`), et la famille a déjà sa « Morsure de pierre » : deux « Morsure » dans le même combat se confondraient dans le journal. *Snap* est le coup de dents rapide, à 1 ou 2 cases. |
| 4 | `burrow-dive` (en) | Burrow Dive | **Plunge** | L'Archer a déjà « Piqué / *Dive* » (`path-dive`, le faucon) : le journal anglais dirait *Dive* pour deux gestes différents. *Burrow* est le mot anglais des taupes (*Burrow set*, *Burrow Key*, *Burrow Belt*, le passif *Burrow Down*). *Plunge* rend bien « Plongée ». |
| 5 | `deep-dive` (en) | Deep Dive | **Deep Plunge** | Suit le 4. En anglais britannique, *a deep dive* est aussi un mot de réunion de bureau (« une analyse approfondie ») : il casserait le ton du chef. |
| 6 | `millstone-crush` (fr) | Écrasement | **Écrasement de meule** | Une voie de classe a déjà « Écrasement / *Crush* » (`path-crush`). Le même nom chez un héros et chez un monstre rendrait le journal illisible. Le complément suit « Écrasement du menhir » (`menhir-crush`) et rejoint l'anglais. |
| 7 | `snatch` (fr) | Happe | **Happer** | « Happe » ne se lit pas comme un geste : c'est un nom rare (une pièce de métal) ou un impératif. L'infinitif suit « Surgir » des taupes et « Engloutir » du chef, qui fait la même chose en plus fort : les deux sorts qui attirent se reconnaissent. |
| 8 | `strata` (nom en) | Upturned Strata | **Overturned Strata** | *Upturned* veut dire « retourné vers le haut » (un col, un visage). Des couches renversées se disent *overturned strata* en géologie, et c'est le sens du français. |
| 9 | `strata` (description) | « … son cuir se fend sur des couches de roche à l'envers ; deux vermisseaux en sortent. … » | « … son cuir se fend : dessous, des couches de roche à l'envers. Deux vermisseaux en sortent. … » | « Se fendre sur » ne se dit pas. La forme reprend celle du gardien (« sa mousse tombe et laisse voir un cœur gravé ») et de la ramure (« sa ramure se fend : entre quatre signes… ») : ce qu'on voit, puis ce qui arrive, puis les chiffres. |
| 10 | `coils` (description) | « Une fois par tour, un allié à 2 cases qui tomberait reste à 1 PV : … » ; *Once a turn* | « … reste à 1 PV (jamais une invocation) : … » ; ***Once per turn*** | Le chef porte aussi les Anneaux, et ses vermisseaux meurent quand même (`AllyFalls` exclut les invocations) : sans le dire, le joueur croirait à une erreur. *Once per turn* suit « *Once per fight* » du Paladin, la même mécanique. |
| 11 | `borer-tooth` | Dent de ver / Worm Tooth | **Dent de ver de roche / Rock Worm Tooth** | Le butin d'autres monstres contient déjà « Ver de terre / *Earthworm* » (`earthworm`, `monsters.json`). « Dent de ver » à côté se lirait comme une dent de ver de terre, ce qui n'existe pas. Le nom complet suit la panoplie (« du ver de roche »). |
| 12 | `borer` (bestiaire) | « Leurs galeries… Leur cuir est de roche ; il ne s'ouvre que lorsqu'ils mordent. » | « Leurs tunnels… Leur cuir est de roche, et ne s'ouvre que lorsqu'ils attaquent. » | Suit le 1. « Mordent » était faux pour le Cracheur, qui s'ouvre en crachant : la règle de la famille est « un ver qui frappe s'ouvre » (§4.2). En anglais, *every which way* est plus naturel que *every way*. |
| 13 | `grit-spitter` (en) | It spits what it could not digest… | It **spits out** what it **couldn't** digest… | *Spit out* est le verbe naturel avec un objet ; la contraction suit le ton parlé des autres phrases. |
| 14 | `millstone-worm` (bestiaire) | « Ses anneaux sont si lourds qu'un ver blessé s'y abrite. » | « … si **épais** qu'un ver blessé **vient** s'y abriter. » | Le poids n'explique pas l'abri ; l'épaisseur, si. « Vient » dit que c'est le blessé qui va vers lui, comme en combat (à 2 cases). |
| 15 | `tunnel-worm` (bestiaire) | « Il connaît toutes les galeries… » ; *and takes those who bleed through them* | « Il connaît tous les **tunnels**… » ; *and **slips the bleeding ones** through them* | Suit le 1. *Takes those who bleed through them* se lit d'abord comme « ceux qui saignent à travers les tunnels ». |
| 16 | `mountain-gnawer` (bestiaire) | « … ; sa peau en a les couches, à l'envers. » ; *It… its hide has the layers* | « … : sa peau en **porte** les couches, à l'envers. » ; ***He**… **his** hide **bears its** layers* | « En a les couches » est lourd ; « porte » reprend le Narratif du §4. Les chefs sont *he* dans le bestiaire anglais (changement 17 du §1.10). Les deux-points annoncent la preuve, comme pour l'Écoute-roche. |

**Repris tel quel :** le nom de famille ; 4 monstres sur 5 en français, 5 sur 5 en anglais ; 7 sorts sur 11 en français, 7 sur 11 en anglais ; les trois noms français des passifs, deux noms anglais, la description de `rock-hide` ; le vermisseau ; `rock-slough` ; la panoplie et ses 4 pièces ; la carte ; la phrase du Ronge-pierre, la phrase française du Cracheur.

### 3.11 Pour le dev

Dans `tools/spells_gen.py`, les six lignes deviennent (seuls les textes changent) :
```python
 S("burrow-dive","Plongée","Plunge",2,2,4,0,0,1,"Neutral",1,los=False,effects=[leap()],ranks=False,cd=2),
 S("millstone-crush","Écrasement de meule","Millstone Crush",3,1,1,25,33,2,"Earth",1,effects=[status("Caster","Resistance",-20,2)],ranks=False),
 S("snatch","Happer","Snatch",3,2,4,14,18,1,"Earth",1,line=True,effects=[pull(2),status("Caster","Resistance",-20,2)],ranks=False,cd=1),
 S("tunnel-swap","Tunnel","Tunnel",2,1,5,0,0,1,"Neutral",1,los=False,effects=[swap()],ranks=False,cd=2),
 S("tunnel-nip","Coup de dent","Snap",3,1,2,25,33,1,"Earth",1,effects=[status("Caster","Resistance",-20,2)],ranks=False),
 S("deep-dive","Plongée profonde","Deep Plunge",2,2,5,0,0,1,"Neutral",1,los=False,effects=[leap()],ranks=False,cd=2),
```
Les autres textes changés sont déjà dans `data/` sous leur forme proposée : `monsters.json` (le Ver tunnelier et le bestiaire), `passives.json` (`coils`, `strata`), `items.json` (`borer-tooth`, ou l'outil qui l'écrit). Aucun id ne change ; les textes de §4 et §6.2 de `familles-mines.md` restent la proposition d'origine.

### 3.12 Remarques (rien n'est corrigé ailleurs que dans ce document)

1. **Les couches à l'envers restent au chef.** Le partage du §2, point 2, est tenu : l'Écoute-roche porte le son, le Ronge-montagne porte les couches. Le passif `strata` et la phrase du bestiaire montrent les couches sans dire ce qu'il y a dessous. `moved-mountains` garde sa découverte : « on a posé une montagne sur quelque chose ».
2. **« On dit qu'il a mangé le dessous d'une montagne » est une fausse explication, et c'est voulu.** C'est ce que disent les gens des Mines devant les couches renversées. `moved-mountains` montrera trois coupes que le ver n'a pas pu faire. Un récit qui se trompe (bible §14), sans démenti écrit : le joueur fera le lien.
3. **« Les mineurs s'en servent sans le dire »** tient avec les quêtes : `the-unmapped-passage` suit « le passage des mineurs » (niveau 33), et les Gardiens moussus « chassent les mineurs depuis que le passage a été ouvert ». **[P]** Si `keep-or-dig` est écrit, Berthe pourrait l'avouer à moitié : « Le passage ? On l'a élargi. On ne l'a pas creusé. » / "The passage? We widened it. We didn't dig it." Ce n'est qu'une proposition.
4. **Une faiblesse à l'Eau dans la région de la Terre**, comme le Feu des Fouille-rocs (§2, point 5) : aucun texte ne l'explique, et il faut que ça le reste.
5. **Rien ne dépasse la Saga I.** Aucun texte ne cite l'Accord, un Primordial, la Disjonction, un enfant de Primordial ni le mot « cinquième ». Les couches à l'envers restent un phénomène (niveau 1) ; la question de ce qu'elles cachent prépare le niveau 2 dans `moved-mountains`, pas ici.
6. **Des mots proches gardés, à surveiller.** La « Mue » des crabes (`molt`, passif) et la « Mue de roche » ou la « Cape de mue » : le même mot dit la même chose (une peau perdue), sans gêne en combat. Les « Anneaux » du Ver-meule et les bagues de l'équipement (emplacement « Anneau ») : un passif et un objet ne se confondent pas dans le journal. La « Bave de roche » (sort) et la « Bave » (ressource `slime`) : idem.
7. **Incohérence signalée, non corrigée :** le passif des crabes s'écrit *Molt* et *it molts* (`passives.json`, `molt`), l'orthographe américaine. L'anglais britannique du projet écrit *Moult*, *it moults*. L'id ne change pas ; le texte est à corriger si Charles le veut. Même remarque pour *Slough* : c'est le bon mot (la peau qu'un animal perd), mais c'est aussi le nom d'une ville anglaise célèbre pour sa grisaille. Je le garde ; l'humour involontaire est mince.
8. **Ankama (D54).** « Ronge-pierre », « Ronge-montagne », « Ver-meule », « Ver tunnelier » et « Vermisseau » sont des mots communs ou des composés simples ; je ne connais aucune reprise, sans pouvoir le garantir pour les trois composés : Charles peut faire une recherche rapide.
9. **Les ids ne posent pas de problème.** `burrow-dive`, `deep-dive`, `tunnel-nip` et `tunnel-swap` ne correspondent plus tout à fait à leurs textes ; ils restent stables (bible §16, §19).
