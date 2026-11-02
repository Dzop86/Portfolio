# Maquette de la page Éléments : l'Atelier des fusions

Graphiste, 2 novembre 2026, version 2. Demande de Charles : la page actuelle n'est pas assez belle. Couleurs : `src/assets/tokens.css`. Thème : `godot/OsmoseTheme.cs`. Aucun code n'est modifié par ce document.

**Règle appliquée (décision de Charles, 2 novembre), qui remplace `elements-specialisations.md` §v3.2 et §v3.3 sur ces points :**

- Créer un composé **consomme ses parents** : leurs sorts quittent le livre et ces éléments ne peuvent plus servir à une autre fusion, tant qu'on ne dissout pas le composé.
- Un héros a donc **au plus 2 paires disjointes, ou 1 trio**. Avec un trio, l'élément restant reste de base.
- C'est gratuit et réversible, hors combat.
- Une paire donne 8 sorts et un trio 12 : **le livre reste à 24 sorts** (8 neutres et 16 d'éléments).
- Un sort d'un composé pose son empreinte, et le sort suivant du même composé déclenche sa réaction. Les réactions entre éléments encore libres restent possibles.

## 1. La recommandation

**On retire la roue et on la remplace par un atelier qui se lit de gauche à droite : les 4 éléments, des fils de couleur, les 6 paires, les 4 trios, puis un panneau de lecture.** La frise devient l'en-tête de ces colonnes.

La forme tient toujours avec la nouvelle règle, et elle la sert mieux que la roue :

- **La rareté se voit.** Un composé créé allume sa tuile, éteint ses parents et bloque toutes les tuiles qui en dépendent. Le joueur voit d'un coup ce qu'il a pris et ce qu'il s'est fermé.
- **Les paires compatibles sont côte à côte.** Les 6 paires forment exactement 3 couples disjoints : Magma et Glace, Boue et Foudre, Sable et Vapeur. La colonne les range ainsi, avec une accolade : la deuxième paire possible est juste à côté de la première.
- **Lequel bloque est écrit.** Sur la tuile bloquée, sur l'élément consommé et dans le panneau.
- **Unité avec la page Voie.** Son arbre se lit aussi de gauche à droite, avec une racine puis les colonnes 30, 50 et 100 (§v3.4). La frise est le même composant sur les deux pages.

**Écart à signaler :** §v3.3 parle d'une « roue des fusions ». Le fond est gardé (parents, paires, trios, réaction, signature, Créer ou Dissoudre) ; la forme change. À noter dans `DECISIONS.md` à l'implémentation.

**Choix écarté :** relier les paires aux trios. Cela ferait croire qu'une paire est l'étape vers un trio, alors que les deux s'excluent.

## 2. Le schéma

### 2.1 La fenêtre (1140 × 620, inchangée hors de la page)

```
+------------------------------------------------------------------------------------------+
| [portrait 72x84]  Ondine                  [Caractéristiques][Sorts][Éléments][Talents]     |  84
|                   Niveau 52 · Mage                                                         |
|                                                                                            |  10
|  PAGE ÉLÉMENTS : 1108 x 407, voir 2.2                                                      | 407
|                                                                                            |  10
|  Créer consomme les parents : au plus 2 paires sans élément commun, ou 1 trio. ...         |  23
|                                                                                            |  10
|                                                          [ Enregistrer ]  [ Fermer ]       |  44
+------------------------------------------------------------------------------------------+
```

### 2.2 La page (exemple : niveau 52 ; Foudre créée ; Magma, bloqué, affiché dans le panneau)

La Foudre a pris le Feu et l'Air. Il reste la Boue comme seconde paire ; toutes les autres tuiles sont bloquées, trios compris.

Une colonne de caractères vaut environ 9 px ; la hauteur du schéma n'est pas à l'échelle, les vraies valeurs sont au §4. Bords : `-` disponible, `#` créé, `.` bloqué ou consommé, `=` affiché dans le panneau. Sceaux : `(o)` normal, `(v)` créé avec sa coche, `(x)` bloqué ou consommé. `}` marque l'accolade d'un couple.

```
[ Mes éléments  Libres : 2/4 >(30) L'Éveil              >(50) L'Affirmation        >(100) L'Accomplissement               ]
[ Seuil de rendement : 100   >     Jusqu'à 2 paires...  >     Ou 1 trio, le 4e...  >      Dernière évolution, page Voie   ]

+-------------------+       +========================+  +........................+  +-------------------------------------+
| (o) Terre         |..     =(x) Magma  ×8[Bloqué   ]=} .(x) Éther ×12[Bloqué   ].  |                                     |
|    80 / 100 · +80 | ~ .   =    Foudre              =} .    Dispersé            .  | (O)  Magma                          |
|    [########--]   |  ~ .  +========================+} .    Foudre              .  | (O)  Terre + Feu                    |
+-------------------+   ~ . +........................+} +........................+  | ----------------------------------- |
                         ~  .(x) Glace  ×8[Bloqué   ].}                             | RÉACTION                            |
+...................+    ~  .    Foudre              .}                             | Fêlé                                |
| (x) Feu        120|#    ~ +........................+}                             | −15 % de résistance à tous les      |
|    Dans la Foudre | #   ~                               +........................+  | éléments pendant 2 tours.           |
|    [##########]   |  #  ~                               .(x) Sel   ×12[Bloqué   ].  | 1er sort : l'empreinte ; 2e : la    |
+...................+   # ~                               .    Desséché            .  | réaction.                           |
                        #~  +------------------------+}   .    Foudre              .  |                                     |
+-------------------+   #~~~| (o) Boue   ×8[Créer    ]|}  +........................+  | SIGNATURE DE SES SORTS              |
| (o) Eau           |~~~#~~ |     Enlisé             |}                              | Fonte, −8 % de résistance pendant   |
|    40 / 100 · +40 |   #   +------------------------+}                              | 1 tour.                             |
|    [####------]   |   ##  +########################+}                              |                                     |
+-------------------+    ###(v) Foudre ×8[Dissoudre]#}  +........................+  |                                     |
                       ##   #    Électrisé           #}  .(x) Cendre×12[Bloqué   ].  |                                     |
+...................+ ##    #                        #}  .    Enseveli            .  |                                     |
| (x) Air         60|#      +########################+}  .    Foudre              .  |                                     |
|    Dans la Foudre |                                    +........................+  |                                     |
|    [######----]   |                                                                |                                     |
+...................+       +........................+}                              |                                     |
                            .(x) Sable  ×8[Bloqué   ].}                              |                                     |
+-------------------+       .    Foudre              .}                              |                                     |
|                   |       +........................+}  +........................+  |                                     |
|                   |       +........................+}  .(x) Sève  ×12[Bloqué   ].  | ----------------------------------- |
|Répartir mes points|       .(x) Vapeur ×8[Bloqué   ].}  .    Enraciné            .  | Bloqué : Feu dans la Foudre.        |
|                   |       .    Foudre              .}  .    Foudre              .  | Dissoudre rend les éléments.        |
+-------------------+       +........................+}  +........................+  +-------------------------------------+
```

Les fils du schéma sont indicatifs : leur tracé exact est au §5.3.

## 3. La hiérarchie

1. **Point focal : les tuiles créées et leurs fils.** Fond vert, contour pistache, fils épais. Il n'y en a qu'une ou deux : la page est plus calme, et le choix pèse plus.
2. **Ce qui reste possible :** les tuiles disponibles, seules à garder un relief et un bouton actif.
3. **Le panneau de lecture**, le seul bloc de texte : il répond à « que fait ce composé ? » et « qu'est-ce qui le bloque ? ».
4. **Les éléments**, en appui à gauche : consommés, ils s'enfoncent et disent où ils sont partis.
5. **La frise et la ligne d'aide**, en repères.

Un héros qui n'a rien créé voit toutes les tuiles ouvertes par son niveau et leurs boutons ; le pistache n'apparaît sur un bouton « Créer » qu'au survol, pour que six boutons ne se disputent pas le regard.

## 4. Les dimensions

Budget vertical inchangé : 620 − 32 de marges = 588 ; 588 − 84 (en-tête) − 23 (message) − 44 (boutons) − 30 (3 espaces de 10) = **407 px**. Valeur calculée, non mesurée : l'autotest `ElementsFit` la confirmera.

| Zone | x | y | Taille |
|---|---|---|---|
| Frise | 0 | 0 | 1108 × 44 |
| Colonne des éléments | 0 | 52 | 188 × 352 |
| Gouttière des fils | 188 | 52 | 64 × 352 |
| Colonne des paires | 252 | 52 | 236 × 352 |
| Accolades | 488 | 52 | 16 × 352 |
| Colonne des trios | 504 | 52 | 236 × 352 |
| Panneau de lecture | 756 | 52 | 352 × 352 |

Il reste 3 px de marge sous la page. Les hauteurs tombent juste, ce qui aligne le haut et le bas des trois colonnes :

| Pile | Calcul |
|---|---|
| Éléments | 4 × 68 + 3 × 8 + 12 + bouton de 44 = 352 |
| Paires | 3 couples de (52 + 4 + 52) + 2 × 14 = 352 |
| Trios | 4 × 82 + 3 × 8 = 352 |

L'écart de 14 entre les couples sort des multiples de 4 : c'est ce qu'il faut pour aligner le bas sur les autres colonnes.

**Ordre des colonnes** (une liste dans `Rpg.Client`, testée) :

- Éléments : Terre, Feu, Eau, Air (l'ordre actuel de `Bars`).
- Paires, par couples disjoints : Magma et Glace ; Boue et Foudre ; Sable et Vapeur.
- Trios, dans l'ordre de l'élément absent : Éther (sans Terre), Sel (sans Feu), Cendre (sans Eau), Sève (sans Air).

**Cellules de la frise :** racine x 0 à 252, palier 30 de 252 à 504, palier 50 de 504 à 756, palier 100 de 756 à 1108. Le palier 30 s'aligne sur les paires et le palier 50 sur les trios.

**Plus petite fenêtre :** à 1280 × 720, tout tient sans défilement. Sous ce minimum, le `ScrollContainer` existant de `Scrolled` prend le relais ; pas de seconde mise en page.

## 5. Les composants

### 5.1 La frise, `TierFrieze` (commune avec la page Voie)

Un `Control` dessiné en `_Draw` : des chevrons en `DrawPolygon`, avec des couleurs par sommet pour le dégradé vertical. Encoche et pointe de 10 px, 2 px entre les cellules.

- **Paramètres :** titre, valeur à droite et ligne de la racine, puis pour chaque palier son x et sa ligne. Sur la page Voie, la racine porte la classe (`relief/class-<id>.svg`) et les x suivent ses colonnes.
- **Une cellule :** rond de 32 px avec le niveau (14 gras) ; ligne 1, le nom du palier en 14 gras ; ligne 2, ce qu'il ouvre, en 14.
- **Le compteur est unique, sur la racine :** `Libres : 2 / 4`, en `--pistache`. Compter les éléments libres dit le vrai budget, ce que des compteurs par palier ne pouvaient pas faire (un trio laisserait afficher « paires 0 / 2 »).
- **Infobulle :** la phrase complète actuelle (`elements.tier.30`, etc.), conservée.
- **Non cliquable,** donc pas de cible de 44 px à respecter ; elle fait quand même 44 de haut.

| État du palier | Corps | Rond | Texte |
|---|---|---|---|
| Racine | `--osmose-ui-lit-top` → `--osmose-ui-lit-bottom` | aucun | `--osmose-ui-text-strong`, `--osmose-ui-text` |
| Atteint | idem | plein `--pistache`, chiffre `--chocolat` | idem |
| Le prochain | `--osmose-ui-well`, bord 1 px `--chocolat-clair` | creux, anneau 2 px `--pistache`, chiffre `--osmose-ui-text-strong` | `--osmose-ui-text-strong`, `--osmose-ui-text-dim` |
| Plus tard | `--osmose-ui-well`, sans bord | creux, anneau 1 px `--osmose-locked`, chiffre `--osmose-locked` | `--osmose-ui-text-dim` |

### 5.2 Une ligne d'élément (188 × 68)

Bouton à bascule : le clic affiche l'élément dans le panneau. Sceau de 40 à x 8, centré ; texte à x 56, sur 124 de large.

| | Libre | Consommé |
|---|---|---|
| **Variation** | `CompoundOpen` | `ElementConsumed` : creux `--osmose-ui-well`, pas d'ombre |
| **Bord gauche** | aucun | barre de 4 px de la couleur du composé |
| **Sceau** | icône en couleur, anneau de sa couleur | icône `-locked`, anneau `--osmose-out-of-level`, **sceau du composé de 20 px en bas à droite** |
| **Ligne 1** | nom en 16 gras `--osmose-ui-text-strong` | nom en 16 gras `--osmose-ui-text-dim`, total en 14 à droite |
| **Ligne 2** | `elements.bar` en 14 `--osmose-ui-text` | **`Dans la Foudre`** en 14 `--osmose-ui-text-strong` |
| **Jauge** (124 × 8) | couleur de l'élément, reflet blanc à 25 % sur le pixel du haut | la même, à `Modulate` 60 % |

- **La jauge reste sur un élément consommé,** parce que ses points comptent toujours si la règle de §v3.2 tient : un composé frappe sur la plus haute caractéristique de ses parents. Le détail des valeurs passe en infobulle et au panneau.
- **Trop-plein (`Over`) :** valeurs ou total en `--osmose-losing`, jauge pleine, butée de 3 px `--osmose-losing` à droite.
- **Infobulle :** `elements.bar.note`.
- **Sous les 4 lignes :** le bouton `Répartir mes points` (188 × 44, secondaire du thème).

### 5.3 Les fils, `ElementStrands`

Un `Control` en `MouseFilter = Ignore`, de 64 × 352, qui dessine une courbe de Bézier cubique par couple parent-paire, soit 12 fils. En coordonnées locales à la gouttière, de (0, ya) à (64, yb), points de contrôle (32, ya) et (32, yb), 24 segments en `DrawPolyline` lissé.

| Départ (`ya`) : Éléments | Centre | Fils à −12, 0, +12 vers |
|---|---|---|
| Terre | 34 | Magma, Boue, Sable |
| Feu | 110 | Magma, Foudre, Vapeur |
| Eau | 186 | Glace, Boue, Vapeur |
| Air | 262 | Glace, Foudre, Sable |

**Arrivée (`yb`) :** centres des paires Magma 26, Glace 82, Boue 148, Foudre 204, Sable 270, Vapeur 326. Le premier parent, dans l'ordre des lignes, arrive à −7, le second à +7 : les deux couleurs entrent côte à côte dans la tuile. Point de 6 px de la couleur du fil à chaque bout.

| État de la paire | Fil |
|---|---|
| Verrouillée ou bloquée | 1 px `--osmose-out-of-level` |
| Disponible | 1,5 px, couleur du parent à 35 % |
| Survolée ou affichée, non créée | 2 px, couleur du parent à 90 % |
| Créée | sous-couche 5 px `--osmose-ui-edge`, puis 3 px, couleur du parent à 100 % |

Ordre de dessin : gris, disponibles, créés, puis le fil survolé. Comme toute paire bloquée passe au gris, un élément consommé n'a plus qu'un seul fil vivant : celui qui part vers son composé.

Les trios n'ont pas de fils, qui devraient traverser la colonne des paires. Quand un trio est créé, trois lignes d'éléments portent `Dans l'Éther` avec la barre de sa couleur, et toutes les paires sont grises : le lien reste lisible sans fil.

### 5.4 Les accolades

Une par couple, dans la gouttière de 16 px à droite des paires : un crochet de 2 px, 6 de profondeur, sur la hauteur du couple (108), en `_Draw`.

- **Au repos :** `--osmose-ui-frame`.
- **Si une paire du couple est créée :** `--pistache`, ce qui désigne l'autre paire comme la seconde possible.

### 5.5 Le sceau, `ElementSigil`

Un `Control` en `_Draw`, de 20 (composé sur un élément consommé), 36 (paire), 40 (élément, trio) ou 52 px (panneau) :

1. Disque `--osmose-ui-well`.
2. **Anneau de 3 px aux couleurs des parents :** une couleur pour un élément, 2 moitiés pour une paire, 3 tiers pour un trio, en `DrawArc`, en partant du haut, avec 4° d'écart entre les arcs. Le sceau de 20 px n'a qu'un anneau de 2 px à sa propre couleur.
3. Silhouette à 64 % du diamètre.
4. **Si le composé est créé :** halo de sa couleur à 18 %, rayon + 4 ; `relief/check.svg` de 16 px en bas à droite. La coche évite de ne dire l'état que par la couleur.
5. **Si le composé est bloqué :** silhouette à `Modulate` 50 % ; **l'arc du parent pris prend la couleur du composé qui le tient,** l'arc du parent libre garde la sienne.

**Les icônes.** Aucune peinture et aucun téléchargement : `tools/ui_art.py` les génère depuis des silhouettes déjà dans `godot/assets/icons/bar/shapes/`, sauf une exception signalée. Entrées `element-<id>` dans `relief/palette.json` et `relief/credits.json`, et préfixe `element-` à ajouter à la liste des variantes `-locked`. Soit 14 fichiers en couleur et 14 en gris, car un élément de base peut désormais être consommé.

| Id | Silhouette (CC BY 3.0) | `main` |
|---|---|---|
| earth | lorc/stone-sphere | `--osmose-earth` |
| fire | lorc/fire-ring | `--osmose-fire` |
| water | lorc/drop | `--osmose-water` |
| air | lorc/whirlwind | `--osmose-air` |
| magma | sbed/lava | `--osmose-magma` |
| ice | lorc/frozen-orb | `--osmose-ice` |
| mud | delapouite/swamp | `--osmose-mud` |
| lightning | lorc/power-lightning | `--osmose-lightning` |
| sand | lorc/sands-of-time | `--osmose-sand` |
| steam | delapouite/steam | `--osmose-steam` |
| ether | lorc/aura | `--osmose-ether` |
| salt | lorc/crystal-growth | `--osmose-salt` |
| ash | lorc/burning-embers | `--osmose-ash` |
| sap | delapouite/tree-roots | `--osmose-sap` |

- Les 4 éléments de base reprennent les silhouettes des Cœurs (`s163-icones-arbre.md`).
- `light` = `mix(main, #ffffff, 0.45)`, `dark` = `mix(main, #000000, 0.45)`, `outline` = `mix(main, #000000, 0.85)`, à la manière des autres entrées.
- **Exception, le Sel :** `lorc/crystal-bars` est déjà l'icône des Éclats, donc on prend `crystal-growth`. Son tracé est dans `items/crystal-growth.svg`, il faut le recopier en `bar/shapes/lorc_crystal-growth.path`.
- **À juger sur la planche :** `sands-of-time` est un sablier, qui peut évoquer le temps plus que le sable.

### 5.6 Les tuiles de composé

**Paire, 236 × 52 :**

```
| 8 |sceau 36| 8 |texte 96              | 4 |bouton 76 x 44| 8 |
                  Foudre            ×8
                  Électrisé
```

**Trio, 236 × 82 :**

```
| 8 |sceau 40| 8 |texte 92              | 4 |bouton 76 x 44| 8 |
                  Éther            ×12
                  Dispersé
                  sans Terre
```

- **Nom** en 16 gras. **`×8` ou `×12`**, le nombre de sorts, en 14, aligné à droite sur la ligne du nom, avec `elements.spells.tip` en infobulle ; caché sur une tuile verrouillée. Le `+8` disparaît : il n'y a plus d'ajout, mais un échange.
- **Réaction** (nom) et **`sans …`** en 14. Sur un trio, `sans Terre` dit aussi quel élément reste libre si on le crée.
- La **signature** et le texte complet de la réaction restent au panneau.
- **Bouton** centré verticalement dans la tuile, 76 de large avec des marges de 6 : « Dissoudre » tient à 14 px. Une pression crée ou dissout, et affiche aussi ce composé dans le panneau.

**Thème, 7 nouvelles variations** à ajouter à `OsmoseTheme.Variations` :

| Variation | Type | Styles |
|---|---|---|
| `CompoundOpen` | Button | normal `Flat(Tab, Edge, 1, 8)` avec ombre ; survol `Flat(Hover, ChocolateLight, 1, 8)` avec ombre ; affiché `Flat(Hover, ChocolateLight, 2, 8)` |
| `CompoundCreated` | Button | normal `Flat(Selected, Pistachio, 1, 8)` avec ombre ; survol `Flat(LitTop, Pistachio, 1, 8)` avec ombre ; affiché `Flat(Selected, ChocolateLight, 2, 8)` |
| `CompoundLocked` | Button | normal `Flat(out-of-level-fill, #ffffff0f, 1, 8)` ; survol `Flat(locked-fill, #ffffff2e, 1, 8)` ; affiché `Flat(locked-fill, ChocolateLight, 2, 8)` |
| `CompoundBlocked` | Button | normal `Flat(locked-fill, Edge, 1, 8)`, sans ombre ; survol `Flat(Tab, #ffffff2e, 1, 8)` ; affiché `Flat(Tab, ChocolateLight, 2, 8)` |
| `ElementConsumed` | Button | normal `Flat(Well, Edge, 1, 8)` ; survol `Flat(locked-fill, #ffffff2e, 1, 8)` ; affiché `Flat(locked-fill, ChocolateLight, 2, 8)` |
| `CompoundCreate` | Button | normal `secondary`, survol `primary-hover`, pressé `primary-pressed`, marges de 6 ; texte `TextStrong`, `Chocolate` au survol et pressé ; **désactivé `Flat(Well, #ffffff14, 1, 6)`, texte `--osmose-locked`** |
| `CompoundDissolve` | Button | normal transparent, bord 1 px `#ffffff2e`, rayon 6 ; survol `Hover` ; texte `Text`, blanc au survol |

On ne réutilise pas `AchievementCard`, dont l'état « choisi » vert se confondrait ici avec « créé ». **Règle simple : le contour clair de 2 px signifie « affiché dans le panneau », quel que soit l'état.**

### 5.7 Le panneau de lecture (352 × 352)

`DetailPanel` du thème, marges de 16, soit 320 de large pour le texte.

| Bloc | Contenu | Hauteur |
|---|---|---|
| Tête | sceau 52 ; nom en 22 gras ; parents en 14 (`Feu + Air`, ou `Feu + Eau + Air, sans Terre`) | 52 |
| Filet | 1 px `#ffffff0f`, 8 de marge | 9 |
| Réaction | `SectionLabel`, nom en 14 gras `--osmose-ui-text-strong`, description en 14 `--osmose-ui-text`, déclencheur en 14 `--osmose-ui-text-dim` | 138 au pire |
| Signature | `SectionLabel`, texte en 14 | 58 |
| Pied, ancré en bas | filet ; 2 lignes, voir ci-dessous | 45 |

Le total au pire est de 350 sur 352, **à condition que les descriptions de `data/reactions.json` soient réécrites** (§10). **C'est juste : l'autotest doit passer les 10 composés dans leurs états, en français et en anglais,** et échouer si le panneau dépasse. Un trio bloqué par deux paires peut prendre 3 lignes de pied, mais ses descriptions plus courtes compensent.

**Le pied selon l'état :**

| État | Ligne 1 | Ligne 2 |
|---|---|---|
| Disponible | `Change 8 sorts de Feu + Air en Foudre.` | `Livre : 24 sorts, quoi que tu crées.` |
| Créé | `8 sorts de Foudre à la place de Feu + Air.` | `Livre : 24 sorts, quoi que tu crées.` |
| Bloqué | `Bloqué : Feu dans la Foudre.` | `Dissoudre rend les éléments.` |
| Verrouillé | `Ouvert au niveau 30.` | `Livre : 24 sorts, quoi que tu crées.` |

La ligne « Bloqué » est en `--osmose-cost`. Avec deux responsables : `Bloqué : Terre dans la Boue, Feu dans la Foudre.` Pour un trio : `Bloqué : Feu + Air dans la Foudre, Eau dans la Boue.`

**Contenu du panneau :** il suit le survol, puis revient au composé cliqué quand la souris part. À l'ouverture, il montre le premier composé créé dans l'ordre d'affichage, sinon le premier disponible, sinon Magma.

**Sur un élément de base :**

- nom, valeurs, `elements.bar.note` ;
- libre : `elements.reaction.base` et `Se retrouve dans : Foudre, Magma, Vapeur, Éther, Cendre, Sève` ;
- consommé : `Dans la Foudre` et `elements.base.consumed`.

**Pas de bouton d'action dans le panneau :** Créer et Dissoudre n'existent qu'à un seul endroit, sur la tuile.

### 5.8 La ligne d'aide

Le `_message` existant affiche `elements.hint` en `--osmose-ui-text-dim` tant qu'aucune action n'a produit de message, puis les messages de création et de dissolution. Elle remplace `elements.free`. En combat : `elements.hint.fight`.

## 6. Les couleurs

**Aucun jeton nouveau.**

| Usage | Jeton |
|---|---|
| Accent, créé, progrès | `--pistache`, `--osmose-taken-fill`, `--osmose-ui-lit-top`, `--osmose-ui-lit-bottom` |
| Cadres, titres de section, affiché, accolades | `--chocolat-clair`, `--osmose-ui-frame`, `--osmose-ui-frame-dark` |
| Surfaces | `--osmose-ui-window`, `--osmose-ui-tab`, `--osmose-ui-hover`, `--osmose-ui-well`, `--osmose-ui-edge` |
| Verrouillé, bloqué | `--osmose-out-of-level`, `--osmose-out-of-level-fill`, `--osmose-locked-fill`, `--osmose-locked` |
| Ce qui bloque | `--osmose-cost` (ce vers quoi aller, comme dans les quêtes) |
| Texte | `--osmose-ui-text-strong`, `--osmose-ui-text`, `--osmose-ui-text-dim` |
| Trop-plein | `--osmose-losing` |
| Éléments | les 14 `--osmose-<élément>`, dans les fils, les sceaux, les jauges et les barres |

**Règle nouvelle pour cette page : la couleur d'un élément n'écrit jamais de texte.** Sur `--osmose-ui-tab`, plusieurs passent sous le seuil AA de 4,5:1 : Terre 4,2, Feu 4,2, Boue 3,6. Les noms sont donc en gris clair et la couleur vit dans le dessin.

Contrastes calculés à la main, à recontrôler à l'implémentation :

| Texte | Fond | Rapport |
|---|---|---|
| `--osmose-ui-text-dim` | `--osmose-ui-tab` | 5,6 |
| `--osmose-ui-text-dim` | `--osmose-locked-fill` | 6,0 |
| `--osmose-ui-text-dim` | `--osmose-ui-well` | 6,8 |
| `--osmose-ui-text-dim` | `--osmose-taken-fill` | **4,0, refusé** : sur une tuile créée, la réaction passe en `--osmose-ui-text` |
| `--osmose-ui-text` | `--osmose-taken-fill` | 6,7 |
| `--pistache` | `--osmose-taken-fill` | 8,4 |
| `--osmose-cost` | `--osmose-locked-fill` | 11 |
| `--osmose-locked` | `--osmose-out-of-level-fill` | 4,9 |
| `--osmose-locked` | `--osmose-ui-well` | 5,3 |
| `--chocolat` | `--pistache` | 7,9 |
| `--chocolat-clair` | `--osmose-ui-tab` | 6,5 |

Le pistache n'écrit que sur des fonds sombres ; le jeu n'a pas de thème clair.

## 7. Les états d'un composé

L'état se calcule dans cet ordre : créé, verrouillé par le niveau, bloqué par un parent pris, sinon disponible. Le niveau passe avant : un composé hors de portée n'a pas à parler de ses parents.

| | Verrouillé (niveau) | Disponible | Bloqué (élément pris) | Créé |
|---|---|---|---|---|
| **Tuile** | `CompoundLocked` | `CompoundOpen` | `CompoundBlocked` | `CompoundCreated` |
| **Sceau** | icône `-locked`, anneau 2 px `--osmose-out-of-level` | icône en couleur, anneau aux couleurs des parents | icône à 50 %, arc du parent pris à la couleur de celui qui le tient | en couleur, halo et coche |
| **Nom** | `--osmose-locked` | `--osmose-ui-text-strong` | `--osmose-ui-text-dim` | blanc |
| **Ligne de réaction** | nom de réaction, `--osmose-locked` | nom de réaction, `--osmose-ui-text-dim` | **paire : le ou les responsables, `--osmose-cost`** ; trio : nom de réaction, `--osmose-ui-text-dim` | nom de réaction, `--osmose-ui-text` |
| **Ligne `sans …` (trio)** | `--osmose-locked` | `--osmose-ui-text-dim` | **le ou les responsables, `--osmose-cost`** | `--osmose-ui-text` |
| **`×8`** | caché | `--osmose-ui-text-dim` | `--osmose-ui-text-dim` | `--pistache` |
| **À droite** | `Niveau 30` en 14 `--osmose-locked`, sans bouton | `Créer` (`CompoundCreate`) | **`Bloqué`, bouton désactivé** | `Dissoudre` (`CompoundDissolve`) |
| **Fils** (§5.3) | gris fin | fins, couleur à 35 % | gris fin | épais, couleur à 100 % |

**Quand Créer est bloqué, le bouton reste présent, à la même place et à la même taille, désactivé.**

- **Il dit `Bloqué`,** et son infobulle `Dissous d'abord la Foudre.` nomme l'action qui le débloque.
- **Il reste,** parce que le blocage se lève d'un geste ailleurs sur la page, contrairement au niveau. Le niveau, lui, ne s'obtient pas ici : d'où un simple texte, sans bouton.
- **Il ne fait pas de remplacement automatique.** Un bouton qui dissoudrait la Foudre pour créer le Magma changerait 2 ou 4 éléments et le deck d'un seul clic, parfois avec deux composés à dissoudre : le joueur garde les deux gestes séparés.
- **Technique :** il passe en `MouseFilter.Pass`, pour qu'un clic dessus affiche quand même la tuile dans le panneau. Il faut vérifier que l'infobulle s'affiche bien sur un bouton désactivé.

**Dire lequel bloque, à trois endroits :** sur la tuile (`Foudre`, ou `Foudre, Boue`), au survol (§ ci-dessous) et au panneau (`Bloqué : Feu dans la Foudre.`). Le nom seul suffit sur la tuile, parce que le bouton `Bloqué` est juste à côté.

**Montrer qu'un élément est consommé, à trois endroits :** sur sa ligne (creux, barre et petit sceau du composé, `Dans la Foudre`), par ses fils (un seul reste vivant) et dans la frise (`Libres : 2 / 4`).

Une tuile verrouillée ou bloquée reste cliquable : on peut lire ce qu'elle offre.

**Survol, sur tous les états :**

- le fond et le bord suivent la variation (§5.6) ;
- le panneau montre le composé ;
- les fils de la paire montent à 2 px, s'ils ne sont ni épais ni bloqués ;
- **les lignes de ses parents reçoivent un contour de 2 px de leur couleur** ;
- **sur une tuile bloquée, la ou les tuiles responsables reçoivent un contour de 2 px `--osmose-cost`,** de même que les lignes d'éléments qu'elles tiennent.

**Pressé :** le bouton suit le thème, dégradé inversé et contenu un pixel plus bas. La tuile prend le contour de l'état « affiché ».

**Au moment de créer :** en 200 ms, la tuile monte à `Modulate` 1,25 et redescend, ses fils passent de 5 à 3 px, les lignes des parents passent à l'état consommé et les tuiles concernées à l'état bloqué. **Dissoudre** fait le trajet inverse, sans éclat : les parents reviennent en 200 ms.

**En combat (`ReadOnly`) :** aucun bouton, pas même `Bloqué` ; tuiles lisibles et cliquables, `elements.hint.fight` en bas.

**Niveau inférieur à 30 :** toutes les tuiles sont verrouillées, les 4 éléments sont libres, et le palier 30 est marqué comme le prochain.

## 8. Les textes

Nouveaux, à relire par le scénariste :

| Clé | FR | EN |
|---|---|---|
| `tier.30.name` | L'Éveil | The Awakening |
| `tier.50.name` | L'Affirmation | The Affirmation |
| `tier.100.name` | L'Accomplissement | The Fulfilment |
| `elements.tier.30.short` | Jusqu'à 2 paires disjointes | Up to 2 separate pairs |
| `elements.tier.50.short` | Ou 1 trio, le 4e reste libre | Or 1 trio, the 4th stays free |
| `elements.tier.100.short` | Dernière évolution, page Voie | Last evolution, Path page |
| `elements.threshold` | Seuil de rendement : {0} | Yield threshold: {0} |
| `elements.free-count` | Libres : {0} / {1} | Free: {0} / {1} |
| `elements.amount` | ×{0} | ×{0} |
| `elements.without` | sans {0} | no {0} |
| `elements.parents.trio` | {0}, sans {1} | {0}, no {1} |
| `elements.section.reaction` | RÉACTION | REACTION |
| `elements.section.signature` | SIGNATURE DE SES SORTS | ITS SPELLS' SIGNATURE |
| `elements.reaction.trigger` | 1er sort : l'empreinte ; 2e : la réaction. | 1st spell: the mark; 2nd: the reaction. |
| `elements.spells.tip` | {0} sorts de {1} à la place de ceux de ses parents. | {0} {1} spells in place of its parents' ones. |
| `elements.spells.open` | Change {0} sorts de {1} en {2}. | Turns {0} {1} spells into {2}. |
| `elements.spells.open.one` | Change 1 sort de {0} en {1}. | Turns 1 {0} spell into {1}. |
| `elements.spells.created` | {0} sorts de {1} à la place de {2}. | {0} {1} spells in place of {2}. |
| `elements.spells.created.one` | 1 sort de {0} à la place de {1}. | 1 {0} spell in place of {1}. |
| `elements.spells.none` | Aucun sort pour ta classe à ce niveau. | No spell for your class at this level. |
| `elements.spells.locked` | Ouvert au niveau {0}. | Opens at level {0}. |
| `elements.book` | Livre : {0} sorts, quoi que tu crées. | Book: {0} spells, whatever you create. |
| `elements.blocked` | Bloqué | Blocked |
| `elements.blocked.by` | Bloqué : {0}. | Blocked: {0}. |
| `elements.blocked.part` | {0} dans {1} | {0} in {1} |
| `elements.blocked.how` | Dissoudre rend les éléments. | Dissolving gives the elements back. |
| `elements.blocked.tip` | Dissous d'abord {0}. | Dissolve {0} first. |
| `elements.consumed` | Dans {0} | In {0} |
| `elements.base.consumed` | Ses sorts ont quitté le livre tant que ce composé existe. Tes points comptent toujours : un composé frappe sur la plus haute caractéristique de ses parents. | Its spells left the book while this compound exists. Your points still count: a compound strikes with the highest characteristic of its parents. |
| `elements.found-in` | Se retrouve dans : {0} | Found in: {0} |
| `elements.hint` | Créer consomme les parents : au plus 2 paires sans élément commun, ou 1 trio. Gratuit, réversible, hors combat. Pense à enregistrer. | Creating consumes the parents: at most 2 pairs with no element in common, or 1 trio. Free, reversible, out of a fight. Remember to save. |
| `elements.hint.fight` | En combat, cette page se lit seulement. | In a fight, this page is read-only. |
| `elements.created.deck` | Fusion créée : {0}. Sorts retirés du paquet : {1}. Enregistre pour la garder. | {0} created. Spells off your deck: {1}. Save to keep it. |

**Le nom avec son article,** 10 clés `fusion.<id>.the` pour `Dans la Foudre`, `Bloqué : Feu dans la Foudre` et `Dissous d'abord la Foudre` :

| Id | FR | EN |
|---|---|---|
| lightning | la Foudre | Lightning |
| ice | la Glace | Ice |
| mud | la Boue | Mud |
| magma | le Magma | Magma |
| steam | la Vapeur | Steam |
| sand | le Sable | Sand |
| ether | l'Éther | Ether |
| ash | la Cendre | Ash |
| sap | la Sève | Sap |
| salt | le Sel | Salt |

**Valeurs modifiées :**

| Clé | FR | EN |
|---|---|---|
| `elements.to-stats` | Répartir mes points | Spend my points |
| `elements.created` | Fusion créée : {0}. Ses sorts remplacent ceux de ses parents ; enregistre pour la garder. | {0} created: its spells replace its parents'. Save to keep it. |
| `elements.dissolved` | Fusion dissoute : {0}. Ses parents reviennent ; enregistre pour confirmer. | {0} dissolved: its parents are back. Save to confirm. |
| `elements.reaction.base` | Dès le niveau 30, frapper dans un élément libre une cible marquée par un autre élément libre déclenche la réaction de leur paire. | From level 30, striking in a free element a target marked by another free element sets off the reaction of their pair. |

- **Gardées telles quelles :** `elements.mine`, `elements.bar`, `elements.bar.note`, `elements.create`, `elements.dissolve`, `elements.locked`, `elements.dissolved.deck`, `elements.tier.30/50/100` (infobulles, à relire car elles parlent des « 6 paires »), `fusion.*.name`, `fusion.*.signature`.
- **Retirées, plus d'usage ou devenues fausses :** `elements.signature`, `elements.reaction`, `elements.free`, `elements.spells` (« rien n'en est retiré »).

## 9. Ce qui change par rapport à la page actuelle

| Aujourd'hui | Demain |
|---|---|
| Roue de 14 boutons de texte, avec éléments et composés mêlés | Colonnes éléments, paires par couples disjoints, trios, de gauche à droite |
| Traits gris ou pistache | 12 fils aux couleurs des parents, un seul vivant par élément consommé |
| Sélectionner, puis Créer dans une carte | Créer et Dissoudre directement sur chaque tuile |
| Texte « s'ajoutent à ton grimoire » | `×8` sur la tuile, échange expliqué au pied du panneau, livre à 24 sorts |
| Pas de notion d'élément pris | Nouvel état `Bloqué`, nom du responsable, élément consommé visible |
| Bandeaux de phrases pour la frise | Frise en chevrons, en-tête des colonnes, compteur des éléments libres ; composant commun avec la Voie |
| Noms colorés et barres plates | Sceaux, jauges en relief, noms en gris clair (AA) |
| Carte sans cadre, styles posés à la main (`Flat`, `StyleBase`, `StyleFusion`) | `DetailPanel` et 7 variations de thème |
| Note du seuil toujours affichée à gauche | Seuil dans la frise, règle en infobulle et au panneau |
| Note « gratuit » sous le bouton | Ligne d'aide unique, qui donne aussi la règle de consommation |

**Conséquences à l'implémentation :**

- **`ElementsPage.cs`.**
  - `FusionState` gagne `Blocked`, et `Fusion` la liste de ses responsables.
  - `ElementBar` gagne le composé qui consomme l'élément (nul s'il est libre).
  - `ElementsWheel` disparaît au profit de l'ordre des colonnes et de la géométrie du §4.
  - Un ajout : le nombre d'éléments libres.
  - **`SpellsOf` doit être redéfini.** Il mesure aujourd'hui une différence de taille du grimoire, qui vaudra environ 0 si la création échange les sorts. Il doit compter les sorts de classe débloqués dont l'élément est un parent et qui ont un jumeau dans ce composé, quel que soit l'état de la tuile.
- **`PointsPanel.Elements.cs`.** Réécrit, avec `TierFrieze`, `ElementStrands` et `ElementSigil` à part. `FusionButton` et `WheelNodes` laissent place à un accès par tuile.
- **Messages.** Créer peut désormais retirer des sorts du deck : `PressPicked` doit aussi comparer `DeckChanges` à la création (`elements.created.deck`).
- **Tests.**
  - `ElementsPageTests` : `TheWheel_FitsItsArea_…` et `AWheelNode_SitsBetweenItsParents_…` sont remplacés par des tests d'ordre (chaque couple est disjoint), de géométrie (chaque pile vaut 352, aucune cible sous 44), d'états et de responsables.
  - Cas à couvrir : Foudre créée, seule la Boue reste disponible ; Foudre et Boue créées, plus aucune tuile disponible, 0 élément libre ; un trio créé, 1 élément libre, tout le reste bloqué ; dissoudre rend tout.
  - `CheckElements` dans `Main.Town.cs` : mêmes scénarios, passés par le bouton de la tuile, avec en plus le test du panneau du §5.7. Son scénario actuel reste valable pour Créer puis Dissoudre.
  - La capture de `Main.Town.cs` (vers la ligne 4020) doit montrer deux paires créées, plus un élément et une tuile bloquée.
- **Godot.** Une tuile est un `Button` qui contient un `Button` : entrer sur le bouton enfant peut déclencher la sortie de la tuile. Il faut brancher le survol du panneau sur les deux pour éviter un clignotement.

## 10. Limites et points à trancher

- **Les descriptions de `data/reactions.json` deviennent fausses.** Elles disent « Le Feu et l'Air mêlés dans la cible » et « Un sort d'Éther sur l'empreinte d'un de ses parents ». Le budget du panneau suppose qu'on retire ces amorces au profit de `elements.reaction.trigger`. Si elles restent, Électrisé déborde. Une paire gardant aussi sa réaction entre éléments libres, la description doit rester vraie dans les deux cas.
- **Les points d'un élément consommé** : j'ai supposé que la règle de §v3.2 tient toujours (un composé frappe sur la plus haute caractéristique de ses parents). Si elle change, la jauge de la ligne consommée et `elements.base.consumed` sont à revoir.
- **Rien n'a été rendu.** Ces valeurs sortent du calcul ; la première capture devra être relue, en particulier le croisement des 12 fils dans 64 px. S'ils sont trop denses au repos, on baisse les fils disponibles de 35 à 25 % avant de toucher à la géométrie.
- **Une page sans rien créé est la plus chargée,** une page avec une paire la plus grise : 8 tuiles sur 10 peuvent être bloquées. C'est voulu, le choix se lit, mais il faudra vérifier sur capture que le gris reste élégant.
- **Le palier 100 coiffe le panneau de lecture,** qui ne lui appartient pas. Sa ligne renvoie clairement à la Voie et le panneau a son propre cadre : je l'accepte pour garder la frise identique sur les deux pages.
- **Les largeurs de texte sont estimées** à 6,7 px par caractère en 14 px. `Lightning ×8`, `Lightning, Mud` sur un trio (92 px), `Jusqu'à 2 paires disjointes`, `Ou 1 trio, le 4e reste libre` et `Dissoudre` sont les plus serrés.
- **Sur une tuile, la réaction n'a que son nom et la signature n'y figure pas :** leur texte est au panneau. C'est le prix d'un écran sans défilement.
- **Le nombre de sorts d'une tuile verrouillée reste caché :** il serait trompeur avant que le héros ait les sorts correspondants.
