# Osmose : relecture des textes des familles des Mines de Sous-Racine

> Relecture du scénariste, 21 novembre 2026, sur `docs/familles-mines.md` (§3 et §6.2). Seuls les textes affichés changent : **aucun id ne bouge**.
> Sources croisées : `docs/bible.md`, la bible de Charles (§2, §8, §14, §15, §19), `docs/chapitre-2-pistes.md` §5, `docs/familles-foret-textes.md`, `data/quests.json` (`the-tree-town`, `the-unmapped-passage`, `mossy-keepers`, `roots-of-the-mother-tree`), `data/dialogues/gaspard.json`, `data/dialogues/berold.json`, `data/monsters.json`, `data/passives.json`, `data/spells.json`, `data/summons.json`, `data/items.json`, `data/sets.json`.
> Cette version couvre les **Fouille-rocs** (S225). Les Vers de roche (S226) viendront dans une section 2.

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
8. **Pour S226, le mot « galerie »** reviendra quatre fois : le donjon et la panoplie des taupes, la carte « Galerie étayée » et le sort des Vers « Galerie / Tunnel » (avec le Ver de galerie). Je le traiterai à la relecture des Vers ; le changement 14 en retire déjà un.
9. **Ankama (D54).** Je ne connais aucune reprise : « boutefeu », « étançonneur », « escarbille », « lampée », « calage », « coup de mine » et « bottes cloutées » sont des mots communs du français, souvent du vocabulaire des mines. « Coup d'étai » est un jeu de mots sur « coup d'État », gardé pour l'humour. Je ne peux pas le garantir pour « Fouille-roc », « Porte-lanterne » et « Écoute-roche » : Charles peut faire une recherche rapide.
10. **Les ids ne posent pas de problème.** `powder-charge`, `gallery-boots` et `lamp-bearer` ne correspondent plus tout à fait à leurs textes. Ce sont des ids internes, stables par règle (bible §16, §19) : on les garde.
