# Maquette de la page Spécialisation : la route qui se divise

Graphiste, 3 novembre 2026, version 1, sprint 191, D95. **À valider par Charles avant le code.** Couleurs : `src/assets/tokens.css`. Thème : `godot/OsmoseTheme.cs`. Famille visuelle : `docs/maquette-elements.md` (renvoyée ici par « Éléments §x »). Noms, titres, descriptions et phrases : `docs/textes-specialisations.md`, repris sans changement. Aucun code n'est modifié par ce document. Rien n'a été rendu : toutes les valeurs sortent du calcul.

## 1. La recommandation

**Un arbre de 15 tuiles sur 4 colonnes, à gauche, et la fiche à droite, à la même place que le panneau de lecture de la page Éléments.** Seule la route du héros porte de la sève : c'est le point focal. Tout le reste s'efface, sans disparaître.

- **Même grille verticale que l'atelier.** Les 6 évolutions de 50 reprennent exactement la pile des paires (3 couples de 52 + 4 + 52, 14 entre les couples, 352 au total) : `ElementsAtelier.PairY` sert tel quel.
- **Mêmes tuiles.** Les 4 états demandés sont les 4 variations déjà au thème (`CompoundOpen`, `CompoundLocked`, `CompoundCreated`, `CompoundBlocked`). Une seule variation de plus, pour l'évolution à venir sur sa route.
- **Le bois de l'arbre des talents.** Les liaisons reprennent les trois aspects de `TreeWood` : vivant, nu, écarté.
- **La couleur d'un élément n'apparaît pas ici.** Une voie ne nomme aucun élément (§v4.1) : les sceaux sont en bois et en pistache. Ce qui distingue les routes, c'est la silhouette.
- **Le geste :** un clic lit, un bouton de la fiche agit, une seule confirmation valide. Pas de bouton sur les tuiles : avec 15 tuiles, ce serait du bruit, et le choix se fait de toute façon après lecture.

**Choix écartés :**

- Une frise de 1108 px comme sur Éléments : elle mangerait 52 px de la fiche, qui a le plus de texte de l'écran. La frise s'arrête donc au bord de l'arbre, et la fiche monte jusqu'en haut.
- Une ligne d'état écrite sur chaque tuile (`Niveau 50`, `Écartée`) : avec 128 px de texte et des titres de 18 signes, le titre et l'état ne tiennent pas ensemble. L'état se lit au sceau, et en toutes lettres dans la fiche.
- Rayer toutes les tuiles écartées : 12 traits de crayon sur 15 tuiles. On ne raye que la tête de chaque route écartée (§5.4).

## 2. Le schéma

### 2.1 La fenêtre (1140 × 620, inchangée hors de la page)

```
+------------------------------------------------------------------------------------------------+
| [portrait 72x84]  Ondine               [Caractéristiques][Sorts][Éléments][Spécialisation]       |  84
|                   Niveau 62 · Sentinelle                                                         |
|                                                                                                  |  10
|  PAGE SPÉCIALISATION : 1108 x 404, voir 2.2                                                      | 407
|                                                                                                  |  10
|  Un choix s'applique dès sa confirmation, sans Enregistrer. Le premier est gratuit ; ...         |  23
|                                                                                                  |  10
|                                                                [ Enregistrer ]  [ Fermer ]       |  44
+------------------------------------------------------------------------------------------------+
```

### 2.2 La page (exemple : Sentinelle de niveau 62 sur la route Mire puis Longue-vue, Herse affichée dans la fiche)

Une colonne de caractères vaut environ 9 px ; la hauteur du schéma n'est pas à l'échelle, les vraies valeurs sont au §4.

Bords : `#` choisi, `~` à venir sur sa route, `.` écarté, `=` écarté et affiché dans la fiche, `-` disponible. Sceaux : `(v)` coche, `(L)` cadenas, `(/)` trait de crayon, `(o)` sans marque. Liaisons : `%` bois vivant, `:` bois nu, `-` écarté.

```
[ Sentinelle  >(30) L'Éveil           >(50) L'Affirmation    >(100)L'Accomplissement +-------------------------------------+
[ Choix 2/2   >     3 routes, 1 choix >    2 suites par route>    Dernière évolution | (O)  Herse                          |
                                                                                     | (O)  Niveau 50 · Mire               |
                                      +###################+  +~~~~~~~~~~~~~~~~~~~+   | ----------------------------------- |
                                    %%#(v)Longue-vue      #::~(L)Dernière semonce~   | Le passif joue dès qu'aucun ennemi  |
              +###################+ % +###################+  +~~~~~~~~~~~~~~~~~~~+   | n'est à 2 cases, au lieu de 3.      |
            %%#(v)Mire            #%%-+===================+  +...................+   | Sort : Tir de barrage.              |
            % +###################+ --=(/)Herse           =--.(o)Route barrée    .   |                                     |
            %                         +===================+  +...................+   | « Je n'ai pas besoin de te toucher. |
            %                                                                        | Il suffit que tu ne passes pas. »   |
            %                         +...................+  +...................+   |                                     |
            %                       --.(o)Porte-faucon    .--.(o)Deux-Ailes      .   |                                     |
            % +...................+ - +...................+  +...................+   |                                     |
+--------+  % .                   . - +...................+  +...................+   |                                     |
|        |%%%-.(/)Affût           .---.(o)Tend-pièges     .--.(o)Chausse-trape   .   |                                     |
|  (S)   |  - +...................+   +...................+  +...................+   |                                     |
|        |  -                                                                        |                                     |
|Sentin. |  -                         +...................+  +...................+   |                                     |
+--------+  -                       --.(o)Saute-haie      .--.(o)Hors d'atteinte .   | ----------------------------------- |
            - +...................+ - +...................+  +...................+   | Changer de suite : 620 Éclats.      |
[ Ouvrir ]  ---.(/)Sente          .---+...................+  +...................+   | Le rang de ton sort le suit.        |
[l'arbre ]    +...................+   .(o)Court-sentes    .--.(o)Cours-toujours  .   | [        Raturer : 620 Éclats     ] |
[  (T)   ]                            +...................+  +...................+   +-------------------------------------+
```

Le dessin exact des liaisons est au §5.3.

### 2.3 La confirmation d'une rature (exemple : même héros, Longue-vue vers Herse)

```
        +-----------------------------------------------------------+
        |  Raturer Longue-vue ?                                     |
        |                                                           |
        |  TU PRENDS                                                |
        |  (o)  Herse                                               |
        |       Niveau 50 · Sort : Tir de barrage                   |
        |                                                           |
        |  Coup au but passe son rang 3 à Tir de barrage.           |
        |                                                           |
        |  Prix : 620 Éclats. Il t'en restera 9 380.                |
        |                                                           |
        |            [ Garder ma route ]  [ Raturer : 620 Éclats ]  |
        +-----------------------------------------------------------+
```

## 3. La hiérarchie

1. **Point focal : la route du héros.** Bois vivant, tuiles vertes, coches. Trois tuiles au plus, sur une ligne brisée de gauche à droite.
2. **L'action possible :** le bouton au pied de la fiche, le seul élément de la page qui puisse être pistache.
3. **La fiche :** le seul bloc de texte. Elle répond à « qu'est-ce que ça fait ? » et « qu'est-ce que ça me coûte ? ».
4. **Les autres tuiles,** lisibles et cliquables, en retrait.
5. **La frise et la ligne d'aide,** en repères.

Un héros sans route n'a aucune sève : ce sont alors les tuiles disponibles, seules en relief, qui appellent le regard.

## 4. Les dimensions

Budget vertical inchangé (Éléments §4) : **407 px** de page. Cette page en utilise 404 : 44 de frise, 8 d'espace, 352 d'arbre. Il reste 3 px.

**Page, coordonnées locales :**

| Zone | x | y | Taille |
|---|---|---|---|
| Frise | 0 | 0 | 756 × 44 |
| Arbre (`body`) | 0 | 52 | 740 × 352 |
| Fiche | 756 | 0 | 352 × 404 |

La fiche garde le x et la largeur du panneau de lecture (`PanelX`, `PanelWidth`) : passer d'un onglet à l'autre ne la fait pas bouger de côté.

**Colonnes de l'arbre :**

| Colonne | x | Largeur | Gouttière à sa droite |
|---|---|---|---|
| Racine | 0 | 92 | 36 |
| Niveau 30 | 128 | 188 | 28 |
| Niveau 50 | 344 | 188 | 20 |
| Niveau 100 | 552 | 188 | jusqu'à 756 : 16 |

Contrôle : 92 + 36 + 188 + 28 + 188 + 20 + 188 = 740.

**Tuiles de l'arbre, y local à l'arbre (ajouter 52 pour la page) :**

| Rangée | Niveau 30 (188 × 52) | Niveaux 50 et 100 (188 × 52) | Centre |
|---|---|---|---|
| Lignée 1 de la branche 1 | | y 0 | 26 |
| **Branche 1** | **y 28** | | **54** |
| Lignée 2 de la branche 1 | | y 56 | 82 |
| Lignée 1 de la branche 2 | | y 122 | 148 |
| **Branche 2** | **y 150** | | **176** |
| Lignée 2 de la branche 2 | | y 178 | 204 |
| Lignée 1 de la branche 3 | | y 244 | 270 |
| **Branche 3** | **y 272** | | **298** |
| Lignée 2 de la branche 3 | | y 300 | 326 |

| Autre élément | x | y | Taille |
|---|---|---|---|
| Tuile racine | 0 | 128 | 92 × 96 (centre 176) |
| Bouton de l'arbre des talents | 0 | 300 | 92 × 52 |

Hauteur : 3 couples de (52 + 4 + 52) + 2 × 14 = 352. **Plus petite cible : 52 px de haut,** au-dessus des 44 exigés. Aucun chevauchement : le test du §10 le vérifie sur tous les rectangles.

**Ordre des rangées :** celui de `data/specs.json`, qui est fixe. Sentinelle : Mire (Longue-vue, Herse), Affût (Porte-faucon, Tend-pièges), Sente (Saute-haie, Court-sentes).

**Une classe statique à ajouter, sur le modèle d'`ElementsAtelier` :** `SpecTree` dans `Rpg.Client`, avec ces constantes et `BranchY(i)`, `ForkY(i)` (= `PairY`), pour les tests xUnit.

**Plus petite fenêtre :** à 1280 × 720, tout tient sans défilement. Dessous, le `ScrollContainer` existant de `Scrolled` prend le relais, sans seconde mise en page.

## 5. Les composants

### 5.1 La frise, `TierFrieze`

Même composant qu'Éléments §5.1, mêmes états des paliers, même infobulle (`path.tier.30`, `.50`, `.100`, déjà au texte). Il a une largeur et des cellules différentes, donc il faut le rendre paramétrable (il lit aujourd'hui `ElementsPage` et `ElementsAtelier` en dur) et le sortir de `PointsPanel.Elements.cs`.

| Cellule | x | Largeur | Place du texte (largeur − 66) |
|---|---|---|---|
| Racine | 0 | 128 | de x 10 à x 110, soit 100 |
| Palier 30 | 128 | 216 | 150 |
| Palier 50 | 344 | 208 | 142 |
| Palier 100 | 552 | 204 | 138 |

Chaque cellule commence au x de sa colonne, comme sur Éléments.

- **Racine, ligne 1 :** le nom de la classe, 14 gras, `--osmose-ui-text-strong`.
- **Racine, ligne 2 :** le compteur `Choix 1/2`, 14 gras. La racine d'Éléments place sa valeur à droite ; ici, la cellule est trop étroite.
  - Choix tous faits : `--pistache`.
  - Un choix qui attend : `--osmose-cost`.
  - Niveau inférieur à 30 : `Pas encore`, `--osmose-ui-text-dim`.
  - Le total vaut 1 sous le niveau 50, 2 ensuite.
- **`L'Accomplissement` est le plus serré :** environ 133 px en 14 gras pour 138. L'autotest doit le mesurer.

### 5.2 La tuile d'évolution (188 × 52)

Un `Button` à bascule : le clic affiche la tuile dans la fiche.

```
| 8 |sceau 36| 8 |titre 128 sur 2 lignes au plus| 8 |
```

- **Sceau :** x 8, y 8.
- **Titre :** x 52, largeur 128, 14 gras, retour à la ligne, centré verticalement, 2 lignes au plus. Le scénariste garantit 11 signes par morceau, soit environ 86 px.
- **Rien d'autre sur la tuile.** Le niveau est dans la frise, juste au-dessus de la colonne.

**La tuile racine (92 × 96) :** sceau 52 à (20, 8) avec `relief/class-<id>.svg` ; nom de la classe en dessous, 14 gras, centré, de y 64 à y 88. Variation `CompoundCreated` si le héros a une route, `CompoundOpen` sinon. Le clic affiche le résumé (§7.3).

**Le bouton de l'arbre des talents (92 × 52) :** bouton secondaire du thème, `tree.open` (`Ouvrir l'arbre (T)`), 14 px, `AutowrapMode` à `WordSmart`, marges de côté de 4 : 2 lignes de 20 + 12 de marges = 52. Voir le point à trancher Q1.

### 5.3 Les liaisons, `SpecWood`

Un `Control` en `MouseFilter = Ignore`, de 740 × 352, sous les tuiles, qui trace des lignes brisées. Les angles sont droits : c'est plus calme que 15 courbes dans des gouttières de 20 à 36 px. `DrawPolyline` n'arrondit pas les angles : on pose un disque du diamètre du trait à chaque angle.

| Tronçon | Tracé | Nombre |
|---|---|---|
| Racine vers branches | de (92, 176) à (110, 176) ; tronc vertical x = 110 de y 54 à y 298 ; de (110, c) à (128, c), c = 54, 176, 298 | 3 |
| Branche vers lignées | de (316, c) à (330, c) ; de (330, c) à (330, c ± 28) ; de (330, c ± 28) à (344, c ± 28) | 6 |
| Lignée vers son évolution de 100 | de (532, y) à (552, y), y = 26, 82, 148, 204, 270, 326 | 6 |

Le tronc se dessine par morceaux, pour que seule la partie qui mène à la route choisie soit vivante.

| Aspect | Quand | Dessin |
|---|---|---|
| **Vivant** | les deux bouts sont choisis | ombre 8 px noir à 35 %, décalée de 2 px ; `--osmose-wood` 6 px ; sève `--pistache` 3 px |
| **Ouvert** | mène à une tuile disponible | `--osmose-wood-light` 3 px |
| **Nu** | sur sa route, pas encore atteint | tirets `--osmose-wood-light`, 2 px, pas de 6 |
| **Écarté, ou en attente** | tout le reste | `--osmose-out-of-level` 1 px |
| **Survol** | de la racine à la tuile survolée, si ce n'est pas déjà vivant | sève `--pistache` à 60 %, 3 px, comme `TreeWood` |
| **Aperçu d'une rature** | de la racine à la cible, tant qu'elle est affichée | tirets `--osmose-cost`, 2 px, pas de 6 |

Ordre de dessin : écarté, nu, ouvert, vivant, survol, aperçu.

### 5.4 Le sceau, `SpecSigil`

`ElementSigil` généralisé (Éléments §5.5) : disque `--osmose-ui-well`, un anneau d'une seule couleur, silhouette à 64 % du diamètre. Tailles : 32 (confirmation), 36 (tuile), 52 (racine et fiche).

| État | Icône | Anneau | Marque en bas à droite (16 px) |
|---|---|---|---|
| Disponible | en couleur | 2 px `--osmose-wood-light` | aucune |
| Choisi, 30 ou 50 | en couleur, halo `--pistache` à 18 % | 3 px `--pistache` | `relief/check.svg` |
| **Choisi, 100** | en couleur, halo `--osmose-ui-gold` à 18 % | **3 px `--osmose-ui-gold`** | `relief/check.svg` |
| Verrouillé par le niveau | `-locked` | 2 px `--osmose-out-of-level` | `relief/padlock.svg` |
| À venir sur sa route | `-locked` | 2 px `--osmose-wood-light` | `relief/padlock.svg` |
| Écarté, tête de route | `-locked` | 2 px `--osmose-out-of-level` | **trait de crayon** |
| Écarté, plus loin | `-locked` | 2 px `--osmose-out-of-level` | aucune |
| En attente d'un choix | `-locked` | 2 px `--osmose-out-of-level` | aucune |

**Le trait de crayon :** une diagonale de 2 px `--osmose-locked`, du bas gauche au haut droit du sceau, débordant de 2 px. Dessiné en `_Draw`, sans fichier.

**Tête de route :** les 2 branches non choisies et la lignée sœur de la sienne, soit 3 traits au plus. Le mot `rature` du livre d'Anatole devient ainsi visible, sans griffonner toute la page.

**La marque évite de ne dire l'état que par la couleur.**

### 5.5 La fiche (352 × 404)

`DetailPanel` du thème, marges de 16, soit 320 de large pour le texte. Mêmes conventions que le panneau de lecture d'Éléments.

| Bloc | Contenu | Hauteur au pire |
|---|---|---|
| Tête | sceau 52 ; titre en 22 gras `--osmose-ui-text-strong`, 256 de large ; dessous en 14 `--osmose-ui-text-dim` : `Niveau 50 · Mire` | 52 |
| Filet | 1 px `#ffffff0f`, 8 de marge | 9 |
| Description | `description` du niveau, 14, `--osmose-ui-text` | 100 (5 lignes) |
| Espace | | 8 |
| Phrase d'ambiance | `lore`, 14, `--osmose-ui-text-dim`, entre guillemets. Celle de la branche au 30, celle de la lignée au 50 et au 100 | 80 (4 lignes) |
| Pied, ancré en bas | filet ; 2 lignes de 14 ; 8 d'espace ; bouton 320 × 44 | 101 |

**Total au pire : 32 de marges + 350 = 382, sur 404.** Le plus long texte connu est celui de `ambush` (162 signes, environ 4 lignes) et la plus longue phrase celle de `palisade` (132 signes, 3 ou 4 lignes). Ces cas ne s'ajoutent pas sur le même nœud, sauf dans le calcul.

**Sur une branche de la Garde ou de la Sentinelle,** la deuxième ligne de la tête porte le titre complet, qui diffère du nom : `Niveau 30 · Garde de la Palissade`. Pour une branche dont le titre est le nom (Archimage), elle reste `Niveau 30 · Mage`.

**Contenu de la fiche :** il suit le survol, puis revient à la tuile cliquée quand la souris part (Éléments §5.7). À l'ouverture, dans cet ordre :

1. Sur une route, si une lignée attend : la première lignée de sa branche.
2. Sur une route : sa dernière évolution atteinte.
3. Sinon : la première branche.

**Le pied et le bouton :** §7.

### 5.6 La confirmation

Elle se pose sur la fenêtre, au centre. Un voile noir à 50 % couvre toute la fenêtre et bloque les clics, Enregistrer et Fermer compris. `Échap` annule. `Ctrl+S` est inopérant tant qu'elle est ouverte (`_Input` de `PointsPanel` à adapter).

- **Panneau :** `WindowPanel` du thème, 560 de large, marges de 16, soit 528 pour le contenu. La hauteur suit le contenu : **394 au pire**, dans les 620 de la fenêtre. Sous 426 px de fenêtre, le corps défile et les boutons restent visibles.
- **Titre :** 22 gras, `--osmose-ui-text-strong`, 30 de haut.
- **Une ligne par évolution reçue,** 44 de haut : sceau 32 à gauche, puis 2 lignes de 14. La première en gras : le titre. La seconde : `Niveau 50 · Sort : Tir de barrage`. Elle se lit depuis `Spells` ; sans sort, `Niveau 100 · Tes sorts changent`.
- **Boutons :** 44 de haut, alignés à droite, 10 d'écart. Refuser à gauche (200 de large), accepter à droite (260 de large).

| | Premier choix | Rature |
|---|---|---|
| **Titre** | `Écrire cette route ?` | `Raturer Longue-vue ?`, ou `Raturer Mire ?` si la branche change |
| **Quitter** | | si la branche change, une ligne : `Tu quittes : Mire, Longue-vue.` |
| **`SectionLabel`** | `TU REÇOIS` | `TU PRENDS` |
| **Lignes** | de 1 à 3, tout ce que le niveau ouvre d'un coup | de 1 à 3 |
| **Rangs** | | une ligne par sort de rang 2 ou plus, 3 au plus : `Coup au but passe son rang 3 à Tir de barrage.` |
| **Prix** | `Prix : gratuit.` en `--pistache` | `Prix : 620 Éclats. Il t'en restera 9 380.` en `--osmose-cost` |
| **Ensuite** | les prix des changements suivants, selon le niveau | |
| **Paquet** | si le héros a composé son paquet : le texte du scénariste (§9, Q6) | |
| **Refuser** | `Pas encore` (bouton secondaire) | `Garder ma route` (bouton secondaire) |
| **Accepter** | `Écrire cette route`, `Primary` | `Raturer : 620 Éclats`, bouton secondaire, `--osmose-cost` |

Le pistache est réservé au premier choix, qui est gratuit. Une rature n'est pas une action à encourager : son bouton reste neutre, et son prix se voit.

**Hauteur au pire, Garde de niveau 100 qui change de branche :** 16 + 30 + 8 + 20 (quitter) + 4 + 20 (titre de section) + 132 (3 lignes) + 8 + 60 (3 rangs) + 8 + 20 (prix) + 8 + 44 + 16 = **394**.

### 5.7 Le point sur l'onglet

Quand un choix attend (route possible et non prise, ou lignée attendue au niveau 50 ou plus) : un disque de 8 px `--pistache`, contour 1 px `--osmose-ui-edge`, à 6 px du coin haut droit de l'onglet. Rien à la connexion, conformément à §v4.5.

### 5.8 La ligne d'aide

Le `_message` existant, en `--osmose-ui-text-dim`, tant qu'aucune action n'a produit de message. Une seule ligne : **125 signes au plus,** en taille 16.

## 6. Les états d'une tuile

L'état se calcule dans cet ordre, pour une tuile de niveau N :

1. **Choisi :** sur la route du héros, et son niveau ≥ N.
2. **À venir :** sur la route du héros, et son niveau < N.
3. **Écarté :** le héros a une branche et ce n'est pas celle de la tuile, ou il a une lignée et ce n'est pas celle de la tuile. Exception : les lignées de la branche en aperçu, que le niveau ouvre, passent à la règle 5.
4. **Verrouillé :** son niveau < N.
5. **Disponible :** la quête est faite ; et la tuile est une branche, ou bien une lignée de sa branche sans lignée, ou bien une lignée de la branche en aperçu.
6. **En attente :** tout le reste (quête non faite, lignée d'une branche qu'il n'a pas encore prise).

Une lignée sous sa branche choisie, sans lignée prise, est donc **disponible** à 50 et plus, et **verrouillée** avant.

**Une tuile écartée reste écartée même si le niveau manque :** son chemin est fermé, c'est ce qui compte. Sur Éléments, le niveau passe avant, parce qu'un composé verrouillé n'est fermé par personne.

| | Verrouillé (niveau) | Disponible | Choisi | Écarté | À venir | En attente |
|---|---|---|---|---|---|---|
| **Tuile** | `CompoundLocked` | `CompoundOpen` | `CompoundCreated` | `CompoundBlocked` | **`SpecAhead`** | `CompoundLocked` |
| **Titre** | `--osmose-locked` | `--osmose-ui-text-strong` | blanc | `--osmose-ui-text-dim` | `--osmose-ui-text-dim` | `--osmose-locked` |
| **Relief** | aucun | ombre | ombre | aucun | aucun | aucun |
| **Sceau** | §5.4 | §5.4 | §5.4 | §5.4 | §5.4 | §5.4 |
| **Cliquable** | oui, on lit | oui | oui | oui | oui | oui |

**Une seule variation nouvelle,** à ajouter à `OsmoseTheme.Variations` : `SpecAhead`, type Button ; normal `Flat(out-of-level-fill, --osmose-wood-light, 1, 8)` ; survol `Flat(locked-fill, --osmose-wood-light, 1, 8)` ; affiché `Flat(locked-fill, ChocolateLight, 2, 8)`.

**La règle d'Éléments tient : le contour clair de 2 px signifie « affiché dans la fiche », quel que soit l'état.**

**Survol, sur tous les états :** le fond et le bord suivent la variation ; la fiche montre la tuile ; le chemin depuis la racine se remplit de sève à 60 %.

**Lignées à choisir, à 50 et plus :** leurs 2 tuiles respirent lentement, leur bord passant de `--osmose-wood-light` à `--pistache` en 1,6 s, aller-retour. Pas de clignotement (§11, point 3).

**En combat (`ReadOnly`) :** aucun bouton dans la fiche, tuiles cliquables, `elements.hint.fight` en bas.

## 7. Le déroulement

### 7.1 Ce que voit le héros, situation par situation

| Situation | L'arbre | Ligne d'aide |
|---|---|---|
| **Niveau < 30** | racine allumée, tout verrouillé, cadenas ; palier 30 « prochain » ; `Pas encore` | `Ta spécialisation s'ouvre au niveau 30.` |
| **≥ 30, quête non faite** | les 3 branches en attente, cadenas au-delà du niveau ; tout se lit | `Rends-toi à Quatre-Chemins : Anatole t'ouvrira une case dans son livre.` |
| **Quête faite, sans branche, 30 à 49** | 3 branches disponibles ; le reste verrouillé | l'aide normale |
| **Quête faite, sans branche, ≥ 50** | 3 branches disponibles ; les lignées en attente | l'aide normale |
| **Branche sans lignée, 30 à 49** | sa branche choisie ; ses 2 lignées verrouillées ; autres routes écartées | l'aide normale |
| **Branche sans lignée, ≥ 50** | sa branche choisie ; ses 2 lignées respirent ; point sur l'onglet | `Ta route se sépare : choisis la suite. Le premier choix est gratuit.` |
| **Branche et lignée** | route vivante jusqu'au dernier niveau atteint ; 100 à venir, ou choisi ; le reste écarté | l'aide normale |

### 7.2 Le premier choix

**Niveau 30 à 49 :**

1. Clic sur une branche : la fiche la montre.
2. Bouton `Choisir cette route` (`Primary`) au pied de la fiche.
3. La confirmation : ce qu'il reçoit, `Prix : gratuit.`, le prix des changements suivants.
4. `Écrire cette route` envoie `PUT …/spec`. Au retour, la sève court de la racine à la tuile en 300 ms, et le message dit `path.evolved`.

**Niveau 50 et plus,** une seule confirmation, conformément à §v4.5 :

1. Clic sur une branche : **aperçu**. Ses 2 lignées passent disponibles, et leurs évolutions de 100 aussi si le niveau suit ; les 2 autres branches passent en attente, mais restent cliquables. Le pied dit `Choisis aussi sa suite, dans la colonne 50.` sans bouton.
2. Clic sur une lignée, à 50 ou à 100 : la fiche la montre, avec `Prendre cette suite` (`Primary`).
3. La confirmation : tout ce qu'il reçoit d'un coup (2 ou 3 lignes), `Prix : gratuit.`
4. Clic sur la racine, ou sur une autre branche : l'aperçu change ou s'annule. L'aperçu n'est jamais envoyé ni enregistré.

**Branche déjà prise, lignée due :** clic sur une des 2 lignées, `Prendre cette suite`, confirmation. Gratuit, partout hors combat.

**Paquet composé à la main :** la confirmation le dit. Après l'envoi, le bouton du pied devient `Ouvrir mon livre`, qui montre l'onglet Sorts, jusqu'au prochain clic sur une tuile.

### 7.3 Le pied de la fiche, cas par cas

Les prix sont des exemples au niveau 62 (10 × et 30 × niveau, `Specs.ChangeCost`).

| Tuile affichée | Ligne 1 | Ligne 2 | Bouton |
|---|---|---|---|
| Quête non faite | `Rends-toi à Quatre-Chemins…` sur 2 lignes | | aucun |
| Verrouillée, hors route | `Ouvert au niveau 50.` | | aucun |
| Branche disponible, 30 à 49 | `Le premier choix est gratuit.` | `Une rature coûtera ensuite {30 × niveau} Éclats.` | `Choisir cette route` |
| Branche disponible, ≥ 50 | `Choisis aussi sa suite, dans la colonne 50.` | `Le premier choix est gratuit.` | aucun |
| Lignée disponible, gratuite | `Le premier choix est gratuit.` | `Tu reçois d'un coup ce que ton niveau ouvre.` | `Prendre cette suite` |
| **Choisie, avec lignée** | `Changer de suite : 620 Éclats.` | `Changer de route : 1 860 Éclats.` | aucun |
| Choisie, sans lignée | `Changer de route : {30 × niveau} Éclats.` | | aucun |
| **À venir** | `Dans 38 niveaux.` | `Elle viendra seule, sans nouveau choix.` | aucun |
| **Lignée écartée, de sa branche** | `Changer de suite : 620 Éclats.` | `Le rang de ton sort le suit.` | **`Raturer : 620 Éclats`** |
| **Branche écartée, sous 50** | `Changer de route : 1 050 Éclats.` | `Les rangs de tes sorts les suivent.` | **`Raturer : 1 050 Éclats`** |
| **Branche écartée, ≥ 50** | `Choisis aussi sa suite : un seul prix.` | `Changer de route : 1 860 Éclats.` | aucun ; le clic ouvre l'aperçu |
| **Lignée écartée d'une autre branche, ≥ 50** | `Changer de route : 1 860 Éclats.` | `Les rangs de tes sorts les suivent.` | **`Raturer : 1 860 Éclats`** |
| Écartée, niveau 100 pas encore atteint | `Ouvert au niveau 100.` | `Sa suite se prend dans la colonne 50.` | aucun |

**Sur la racine,** le résumé :

- **Tête :** le sceau de classe, le titre porté, ou la classe s'il n'y en a pas ; `Niveau 62 · Sentinelle`.
- **Corps :** `TA ROUTE` en `SectionLabel`, puis `Mire`, `Longue-vue`, `Dernière semonce (au niveau 100)`, une ligne chacun. Sans route : `Tu gardes ta classe de base et tes 24 sorts tant que tu ne choisis pas.`
- **Pied :** les prix, comme sur une tuile choisie.

### 7.4 La rature

Une rature, c'est quitter une lignée ou une branche contre des Éclats. **Rien n'est conservé de l'ancienne route, sinon les rangs transférés case par case** (§v4.5).

**Quand le bouton ne peut pas agir,** il reste à la même place et à la même taille, désactivé (`primary-disabled` du thème), comme `Bloqué` sur Éléments. Il dit pourquoi, et l'infobulle dit quoi faire. Par ordre de priorité :

| Cause | Texte du bouton | Infobulle |
|---|---|---|
| En combat | pas de bouton | |
| Brouillon non enregistré (`Editor.Changed`) | `Enregistre d'abord` | `Une route s'écrit tout de suite : enregistre ou annule tes changements avant.` |
| Hors d'un village | `Dans un village` | `Une rature se fait dans un village, hors combat.` |
| Trop peu d'Éclats | `Il manque 120 Éclats` | `Cette rature coûte 620 Éclats.` |

La cause « brouillon » s'applique aussi au premier choix : le retour du serveur reconstruit `Editor` et effacerait le brouillon (`Saved`).

**Changer de branche à 50 ou plus demande aussi une lignée** (le serveur refuse sinon), pour un seul prix. Deux chemins y mènent : cliquer sur la branche écartée ouvre l'aperçu, et ses lignées passent disponibles ; ou cliquer directement sur une de ses lignées.

**Pendant l'affichage de la cible :** tirets `--osmose-cost` de la racine à la cible (§5.3). L'ancienne route garde sa sève : rien n'a changé tant que la confirmation n'est pas validée.

**Après la confirmation, en 500 ms :**

1. 150 ms : le trait de crayon se trace sur l'ancienne tête de route et sa tuile s'éteint.
2. 300 ms : la sève court de la racine à la nouvelle tuile.
3. 50 ms : la coche apparaît.

Message, en une ligne : `Rature faite : 620 Éclats. {path.evolved}`.

**Refus du serveur** (solde, lieu, combat) : la confirmation se ferme, rien ne bouge, le message passe en `--osmose-losing`. Le texte du serveur est en anglais : il faut mapper ses refus sur les clés du §8 ou afficher `spec.refused`.

## 8. Les textes

Nouveaux, **à relire par le scénariste.** Le mot « lignée » reste hors de l'écran, à sa demande (textes §6) : on dit `route` pour une branche et `suite` pour une lignée. Noms et descriptions viennent de `specs.json`.

| Clé | FR | EN |
|---|---|---|
| `sheet.tab.spec` | Spécialisation | Specialisation |
| `spec.choices` | Choix {0}/{1} | Choices {0}/{1} |
| `spec.choices.none` | Pas encore | Not yet |
| `spec.tier.30.short` | 3 routes, 1 choix | 3 roads, 1 choice |
| `spec.tier.50.short` | 2 suites par route | 2 next steps each |
| `spec.tier.100.short` | Dernière évolution | Last evolution |
| `spec.hint` | Un choix s'applique dès sa confirmation, sans Enregistrer. Le premier est gratuit ; une rature se paie dans un village. | A choice applies once confirmed, with no need to save. The first is free; a crossing-out is paid for in a village. |
| `spec.hint.closed` | Ta spécialisation s'ouvre au niveau {0}. | Your specialisation opens at level {0}. |
| `spec.free` | Le premier choix est gratuit. | The first choice is free. |
| `spec.then-cost` | Une rature coûtera ensuite {0} Éclats. | A crossing-out will then cost {0} Éclats. |
| `spec.at-once` | Tu reçois d'un coup ce que ton niveau ouvre. | You get at once what your level opens. |
| `spec.needs-fork` | Choisis aussi sa suite, dans la colonne 50. | Choose its next step too, in column 50. |
| `spec.needs-fork.paid` | Choisis aussi sa suite : un seul prix. | Choose its next step too: a single price. |
| `spec.opens` | Ouvert au niveau {0}. (= `elements.spells.locked`) | Opens at level {0}. |
| `spec.pick-at-50` | Sa suite se prend dans la colonne 50. | Its next step is taken in column 50. |
| `spec.ahead` | Dans {0} niveaux. | In {0} levels. |
| `spec.ahead.one` | Au prochain niveau. | At the next level. |
| `spec.ahead.alone` | Elle viendra seule, sans nouveau choix. | It will come by itself, with no new choice. |
| `spec.price.fork` | Changer de suite : {0} Éclats. | Change next step: {0} Éclats. |
| `spec.price.branch` | Changer de route : {0} Éclats. | Change road: {0} Éclats. |
| `spec.rank.one` | Le rang de ton sort le suit. | Your spell's rank comes along. |
| `spec.rank.many` | Les rangs de tes sorts les suivent. | Your spells' ranks come along. |
| `spec.take` | Choisir cette route | Take this road |
| `spec.take.fork` | Prendre cette suite | Take this next step |
| `spec.cross` | Raturer : {0} Éclats | Cross out: {0} Éclats |
| `spec.need.save` | Enregistre d'abord | Save first |
| `spec.need.save.tip` | Une route s'écrit tout de suite : enregistre ou annule tes changements avant. | A road is written at once: save or undo your changes first. |
| `spec.need.village` | Dans un village | In a village |
| `spec.need.village.tip` | Une rature se fait dans un village, hors combat. | A crossing-out is done in a village, out of a fight. |
| `spec.need.shards` | Il manque {0} Éclats | {0} Éclats short |
| `spec.need.shards.tip` | Cette rature coûte {0} Éclats. | This crossing-out costs {0} Éclats. |
| `spec.soon` | Bientôt | Soon |
| `spec.confirm.first` | Écrire cette route ? | Write this road down? |
| `spec.confirm.cross` | Raturer {0} ? | Cross out {0}? |
| `spec.confirm.leave` | Tu quittes : {0}. | You leave: {0}. |
| `spec.confirm.get` | TU REÇOIS | YOU GET |
| `spec.confirm.take` | TU PRENDS | YOU TAKE |
| `spec.confirm.spell` | Sort : {0} | Spell: {0} |
| `spec.confirm.spells` | Sorts : {0} | Spells: {0} |
| `spec.confirm.no-spell` | Tes sorts changent | Your spells change |
| `spec.confirm.rank` | {0} passe son rang {1} à {2}. | {0} hands its rank {1} to {2}. |
| `spec.confirm.free` | Prix : gratuit. | Price: free. |
| `spec.confirm.cost` | Prix : {0} Éclats. Il t'en restera {1}. | Price: {0} Éclats. You will have {1} left. |
| `spec.confirm.later` | Ensuite : {0} Éclats pour changer de suite, {1} pour changer de route, dans un village. | After that: {0} Éclats to change next step, {1} to change road, in a village. |
| `spec.confirm.later.branch` | Ensuite : {0} Éclats pour changer de route, dans un village. | After that: {0} Éclats to change road, in a village. |
| `spec.confirm.yes` | Écrire cette route | Write this road down |
| `spec.confirm.no` | Pas encore | Not yet |
| `spec.confirm.keep` | Garder ma route | Keep my road |
| `spec.crossed` | Rature faite : {0} Éclats. {1} | Crossed out: {0} Éclats. {1} |
| `spec.refused` | Le livre refuse : rien n'a changé. | The book says no: nothing has changed. |
| `spec.open-book` | Ouvrir mon livre | Open my book |
| `spec.summary.road` | TA ROUTE | YOUR ROAD |
| `spec.summary.later` | {0} (au niveau {1}) | {0} (at level {1}) |
| `spec.summary.none` | Tu gardes ta classe de base et tes 24 sorts tant que tu ne choisis pas. | You keep your base class and your 24 spells until you choose. |

**Repris du scénariste (textes §6), sans changement :** quête non faite, branche sans second choix, paquet composé à la main. **Déjà en place :** `path.tier.*`, `path.evolved`, `tier.*.name`, `sheet.level`, `elements.hint.fight`, `tree.open`.

**Le français est le plus long.** Les plus serrés, à mesurer : `Choix 2/2` et `Choices 2/2` (100 px), `L'Accomplissement` (138), `Dernière évolution` (138), `2 suites par route` (142), `Citadelle d'épines` à 22 gras (256), `Une rature coûtera ensuite 3 000 Éclats.` et `Tu reçois d'un coup ce que ton niveau ouvre.` (320).

`Il manque 120 Éclats` tient dans le bouton de 320. `Raturer : 3 000 Éclats` aussi.

**Séparateur des milliers :** une espace insécable en français, une virgule en anglais, comme les autres prix du jeu.

## 9. Les couleurs et les icônes

**Aucun jeton nouveau.**

| Usage | Jeton |
|---|---|
| Route, choisi, progrès | `--pistache`, `--osmose-taken-fill`, `--osmose-ui-lit-top`, `--osmose-ui-lit-bottom` |
| Liaisons | `--osmose-wood`, `--osmose-wood-light`, `--osmose-out-of-level` |
| Dernière évolution atteinte | `--osmose-ui-gold` |
| Affiché, titres de section | `--chocolat-clair`, `--osmose-ui-frame-dark` |
| Surfaces | `--osmose-ui-window`, `--osmose-ui-tab`, `--osmose-ui-hover`, `--osmose-ui-well`, `--osmose-ui-edge` |
| Verrouillé, écarté | `--osmose-out-of-level`, `--osmose-out-of-level-fill`, `--osmose-locked-fill`, `--osmose-locked` |
| Prix, un choix qui attend | `--osmose-cost` |
| Refus | `--osmose-losing` |
| Texte | `--osmose-ui-text-strong`, `--osmose-ui-text`, `--osmose-ui-text-dim` |

**Contrastes,** repris d'Éléments §6, ou calculés à la main pour ceux marqués d'une étoile, à recontrôler à l'implémentation :

| Texte | Fond | Rapport |
|---|---|---|
| `--osmose-ui-text-strong` | `--osmose-ui-tab` | 11,6 * |
| `--osmose-ui-text-dim` | `--osmose-ui-tab` (fiche) | 5,6 |
| `--osmose-ui-text-dim` | `--osmose-locked-fill` (écarté) | 6,0 |
| `--osmose-locked` | `--osmose-out-of-level-fill` (verrouillé) | 4,9 |
| `--osmose-cost` | `--osmose-ui-tab` | 10,4 * |
| `--pistache` | `--osmose-ui-lit-top` (compteur, cas le pire) | 6,9 * |

**Les icônes.** Générées par `tools/ui_art.py` depuis `godot/assets/icons/bar/shapes/` (CC BY 3.0), sans peinture. Une entrée `spec-<id>` par branche et lignée dans `relief/palette.json` et `relief/credits.json`, le préfixe `spec-` à ajouter à la liste des variantes `-locked` (ligne 184), et le champ `icon` à remplir dans `data/specs.json` : `Specialisation.Icon` et `SpecFork.Icon` existent, mais il n'y a aucune valeur aujourd'hui.

- **`main` = `--chocolat-clair`** pour toutes, avec `light`, `dark` et `outline` tirés comme les autres (Éléments §5.5). Une seule teinte : la route, seule, porte le vert.
- **Une évolution de 100 reprend l'icône de sa lignée :** c'est l'anneau doré qui la distingue. 27 entrées, dont **9 pour la Sentinelle en S191**.

| Classe | Id | Silhouette | | Id | Silhouette | | Id | Silhouette |
|---|---|---|---|---|---|---|---|---|
| Sentinelle | `sightline` | delapouite/eye-target | | `marksman` | lorc/spyglass | | `barrage` | delapouite/dungeon-gate |
| | `ambush` | lorc/hood | | `falconer` | lorc/hawk-emblem | | `trapper` | lorc/mantrap |
| | `trail` | lorc/walking-boot | | `leaper` | lorc/sprint | | `runner` | delapouite/running-shoe |
| Garde (S192) | `palisade` | lorc/bordered-shield | | `rampart` | delapouite/cross-shield | | `bastion` | `items/spiked-shield.svg` |
| | `breach` | delapouite/shield-bash | | `ram` | lorc/anvil-impact | | `harpooner` | lorc/barbed-spear |
| | `grip` | lorc/grab | | `jailer` | lorc/linked-rings | | `pincer` | lorc/grapple |
| Mage (S193) | `archmage` | lorc/pointy-hat | | `high-archmage` | lorc/burning-meteor | | `patient` | lorc/hourglass |
| | `sorcerer` | lorc/candle-flame | | `plague` | sbed/poison | | `hexer` | lorc/bolt-eye |
| | `necromancer` | lorc/two-shadows | | `binder` | delapouite/three-friends | | `reaper` | delapouite/bolt-bomb |

- Toutes ces silhouettes sont présentes dans `bar/shapes/`, sauf `spiked-shield`, dans `items/`, à recopier.
- **Un seul téléchargement :** `lorc/padlock` (présence vérifiée sur game-icons.net, CC BY 3.0, Lorc), pour `relief/padlock.svg`, gris (`LOCKED`).
- **À juger sur la planche :** ces silhouettes servent peut-être déjà à des sorts de la même classe. Je ne l'ai pas contrôlé sort par sort. Si c'est le cas, l'anneau de bois suffit-il à éviter la confusion ? `three-friends` dit « groupe » plus qu'« ombres », et `candle-flame` est le moins parlant pour le Maléfice.

## 10. Conséquences à l'implémentation

- **`Rpg.Client`, nouveau `SpecPage`, sans dessin :** les 15 nœuds et leur état (§6), le pied (§7.3), l'aperçu, le compteur, les paliers. Il lui faut plus que `PointsEditor` : **`CharacterSummary`** pour les quêtes (`Quests`), les Éclats (`Shards`), le lieu (`Place`, avec `Shop.InVillage`) et le combat. Le serveur revérifie tout ; le client ne fait qu'annoncer.
- **`Rpg.Client`, nouveau `SpecTree` :** la géométrie du §4.
- **`godot/PointsPanel.Spec.cs`,** sur le modèle de `PointsPanel.Elements.cs` : page, `SpecWood`, `SpecSigil`, la fiche et la confirmation. `TierFrieze` en sort, paramétré. `Elements` n'est pas modifiée en dehors de cela.
- **`PointsPanel.cs` :**
  - `_talentsTab` affiche `sheet.tab.spec` ; `ShowTalents` devient la page Spécialisation.
  - `BuildTalents` et ses libellés disparaissent ; `_openTree` et `TreePressed` restent (Q1). Il faut aussi adapter `TalentsSummary`, lu par l'autotest, et `Main.Town.cs` vers les lignes 2485 à 2497.
  - `Refresh` n'a plus à afficher les points de talents ici : ils sont dans la fenêtre de l'arbre.
- **Envoi :** `GameServer` a déjà l'appel vers `PUT …/spec` (ligne 384). `Saved` rafraîchit l'éditeur au retour.
- **Tests xUnit client :**
  - Géométrie : aucun rectangle ne se chevauche, parmi les 15 tuiles, la racine et le bouton ; aucune cible sous 44 ; tout tient dans 740 × 352 ; chaque branche est centrée sur ses 2 lignées ; l'ordre suit `specs.json`.
  - États : les 7 situations du §7.1, sur chaque classe ; ordre de calcul ; tête de route, 3 traits au plus.
  - Pied : chaque ligne du §7.3 ; les prix égaux à `Specs.ChangeCost`.
  - Liaisons : l'aspect vivant couvre exactement la route choisie.
- **Autotest Godot `CheckSpec` :**
  - La page tient à 1280 × 720, en français et en anglais.
  - La fiche tient pour les 45 évolutions dans ses états, comme `ReadingFits` (échec si elle dépasse).
  - Aucun titre de tuile ne dépasse 2 lignes ; les cellules de frise tiennent.
  - Le scénario de la story : choisir une branche à 30, puis une évolution à 50. Plus une rature, refusée hors village.
- **Capture :** Sentinelle de niveau 62 dont la route est choisie, une lignée écartée affichée. Elle reste à relire, en particulier la lisibilité d'une page à 12 tuiles grises. Si elle est trop terne, on monte les tuiles écartées à `--osmose-ui-text` avant de toucher à la géométrie.

## 11. Mes désaccords avec la conception, à ne pas trancher ici

1. **§v4.5 : « six lignes à 68 px, nœuds de 56 px, sur 410 px ».** Le calcul oublie la frise. Il reste 352 px, soit des **tuiles de 52 px**. Cela tient au-dessus de 44, et je n'ai pas pris le chiffre du document.
2. **§v4.5 contre T146 et la quête : où se fait le premier choix** (point 8 du scénariste). §v4.5 dit « chez l'habitant des voies » ; la quête dit « ouvre ta fiche » ; le serveur l'accepte partout hors combat. J'ai dessiné **sur la page, partout hors combat**, ce qui suit le code et le texte. Q2.
3. **§v4.5 : « la colonne 50 clignote ».** Un clignotement fatigue, sur une fenêtre où l'on reste longtemps à lire. Je propose une respiration lente, sur les 2 tuiles concernées seulement. Le signal reste, l'agacement en moins.
4. **§v4.5 : point sur l'onglet, seulement pour la lignée.** Je l'étends à la première route, dès la quête faite : c'est la même attente. À retirer si Charles préfère s'en tenir au texte.
5. **§v4.5 : « sa ligne en bois vivant, la colonne 100 en "dans 40 niveaux" ».** Je garde ce texte dans la fiche et non sur la tuile, faute de place (§1).
6. **`maquette-elements.md` §5.1 : frise « identique sur les deux pages ».** C'est le même composant, mais plus la même largeur : 756 ici, 1108 là-bas. Cela s'écarte de ma propre maquette, et je le signale pour cette raison.
7. **`textes-specialisations.md` §6 : la phrase sur le paquet est au singulier** (`Ce sort n'entre pas seul…`), alors qu'un choix tardif donne 2 ou 3 sorts d'un coup. Il faut une forme au pluriel. Q6.

## 12. Les points à faire trancher par Charles

| # | Question | Ma recommandation |
|---|---|---|
| **Q1** | L'onglet Talents avait un bouton vers l'arbre. La demande dit que l'arbre reste sur T et ne fait pas partie de la page. On garde un petit bouton `Ouvrir l'arbre (T)` dans le coin de la page, ou rien ? | **Le garder** : il coûte 92 × 52 dans un coin vide, et un joueur qui cherchait « Talents » dans cette fenêtre y retrouve son chemin. Il transmet aussi le brouillon, déjà en place. Sans lui, il faut retirer `_openTree`, et la colonne racine gagne en calme |
| **Q2** | Le premier choix se fait sur la page, partout hors combat, ou seulement chez Anatole ? | **Sur la page**, comme la quête et le serveur |
| **Q3** | Le choix s'applique tout de suite, alors que le reste de la fenêtre passe par Enregistrer. On bloque avec `Enregistre d'abord` si un brouillon est en cours ? | **Oui.** C'est le plus simple et le plus sûr. Alternative : enregistrer le brouillon automatiquement avant, ce qui est plus souple mais fait deux envois en un clic |
| **Q4** | En S191, seule la Sentinelle a ses sorts. Garde et Mage voient leur arbre avec quels boutons ? `data/specs.json` n'a pas encore un seul sort | **Arbre lisible et bouton désactivé `Bientôt`** jusqu'à S192 et S193, pour qu'aucun héros n'écrive une route vide |
| **Q5** | Les mots à l'écran : `route` (branche), `suite` (lignée), `Raturer` (changer, payant). Ils viennent d'Anatole. `Raturer` reste-t-il clair ? | **Les garder** : le prix écrit dans le bouton lève le doute, et la confirmation explique. Alternative neutre : `Changer : 620 Éclats` |
| **Q6** | Texte du paquet au pluriel | **À demander au scénariste** avant le code |
| **Q7** | Le titre du héros sous son nom, dans l'en-tête de la fenêtre (`Niveau 62 · Longue-vue`) ? Il n'est aujourd'hui lisible que dans le résumé de la racine | **Non en S191 :** ce n'est pas demandé. À revoir avec le titre affiché en jeu |

## 13. Limites

- **Rien n'a été rendu.** La largeur des textes est estimée à 7 px par signe en 14, et 7,8 en 14 gras. Ces valeurs sont plus prudentes que celles d'Éléments, qui se sont révélées justes de peu : le dernier correctif de S189 l'a montré. C'est l'autotest qui tranchera.
- **La fiche ne cite pas les sorts à part :** ils sont dans la description du scénariste, et leur fiche complète reste à la page Sorts.
- **La page ne dit pas qui est le meilleur choix :** aucune note, aucun conseil, conformément à l'identité de jeu décrite en §v4.1.
- **La confirmation lit `Spells` dans `specs.json`,** vide aujourd'hui : son contenu se vérifie avec les données de la Sentinelle, pas avant.
- **Hors de cette maquette :** l'annonce au passage d'un palier (bandeau, journal), déjà prévue par §v3.5, et le titre affiché sur la carte.

## 14. Les classes finales (S196, D96)

Graphiste, 6 novembre 2026, sprint 196, D96, `docs/classes-conception.md` §2.2, §2.3 et ligne 304. **À valider par Charles avant le code.** Aucun code n'est modifié par ce document. Rien n'a été rendu. Les noms de finales du schéma sont **des exemples** : les vrais viennent du scénariste en S197. Les §1 à §13 restent valables, sauf là où cette section les remplace ; chaque remplacement est signalé.

### 14.1 La recommandation

**La colonne 100 ne montre qu'une branche, la branche ouverte : ses 2 lignées, en 2 couples de 2 tuiles.** Les colonnes racine, 30 et 50 ne changent pas : 1 + 3 + 6 tuiles, aux mêmes rectangles qu'au §4. La lecture va toujours de gauche à droite, sans défilement.

- **La branche ouverte, c'est celle de la tuile cliquée,** donc celle qu'affiche la fiche quand la souris est partie. Le survol ne la change pas. Si la colonne suivait le survol, elle changerait sous la souris pendant que le joueur traverse la colonne 50 pour l'atteindre, et ses tuiles fuiraient devant lui.
  - **À l'ouverture de la page :** la branche de la tuile d'ouverture (§5.5, complété au §14.6). C'est la branche du héros s'il en a une, sinon la première.
  - **Clic sur une tuile :** sa branche devient la branche ouverte, qu'elle soit choisie, écartée ou en attente. On peut ainsi lire les finales de toutes les routes.
  - **Clic sur la racine :** la branche du héros, s'il en a une ; sinon, rien ne change.
  - **Aperçu (§7.2) :** la branche en aperçu est la branche ouverte.
  - **Après une confirmation :** la branche du héros.
- **Les deux autres branches n'ont rien dans la colonne 100,** ni tuile, ni ligne repliée, ni moignon de liaison. La place manque : 4 finales de 52 px, plus leurs écarts, prennent 296 px des 352. Et un moignon se confondrait avec les longues liaisons qui passent dans la gouttière (§14.3). Ce qui remplace ces signes : la deuxième ligne de la frise, `2 classes par suite`, son infobulle, et les liaisons, qui ne partent que des 2 lignées de la branche ouverte.
- **Une seule règle, avec ou sans finales.** Une lignée sans finales garde son évolution unique de 100 : elle occupe une seule place de son couple (§14.2). C'est le cas de toutes les données de S196. Conséquence, dès ce sprint : **la colonne 100 passe de 6 tuiles à 2,** celles de la branche ouverte. Voir Q8 pour l'autre solution, à deux modes.
- **Même famille de tuiles,** mêmes états (§6), mêmes sceaux (anneau doré au 100) et même bois. Les tuiles de la colonne 100 sont seulement moins larges, pour laisser deux coudes dans la gouttière.

**Choix écartés :**

- **Montrer les 12 finales :** 12 × 52 + écarts = 700 px, pour 352 de place.
- **Des tuiles carrées sans nom, 2 par rangée :** les 12 tiennent, mais le joueur ne lit plus rien sans survoler. C'est le contraire du §1.
- **Ne montrer que les 2 finales de la lignée ouverte :** ce serait plus calme, mais il faudrait un clic de plus pour comparer les deux suites d'une branche, ce que fait un joueur de niveau 100 qui hésite à raturer sa suite.
- **Placer le bloc de finales en face de la branche ouverte :** pour les branches 1 et 3, le bloc sort de la page (il faut 296 px centrés sur y 54 ou y 298). Une fois ramené dans la page, il se met à bouger d'une branche à l'autre. Un bloc fixe se lit mieux : la colonne 100 est toujours au même endroit, et seules les liaisons changent.
- **Écrire le gabarit sur la tuile :** la place manque, comme pour l'état (§1). Il est écrit en tête de la fiche (§14.5).

### 14.2 Les dimensions

**Ce qui change dans la colonne 100 (remplace la dernière ligne du tableau des colonnes du §4) :**

| Colonne | x | Largeur | Gouttière à sa gauche |
|---|---|---|---|
| Niveau 100 | **572** | **168** | **40** (de 532 à 572), avec 2 coudes |

Contrôle : 344 + 188 + 40 + 168 = 740. Les tuiles de 50 finissent à x 532 et la page de l'arbre fait toujours 740 × 352.

**La tuile de la colonne 100 (168 × 52)**, finale ou évolution unique :

```
| 8 |sceau 36| 8 |titre 108 sur 2 lignes au plus| 8 |
```

- Sceau en (8, 8), titre en x 52, largeur 108, 14 gras, 2 lignes au plus. Les 11 signes par morceau garantis par le scénariste (environ 86 px) tiennent. **Nouvelle limite pour les finales : 13 signes par morceau,** soit environ 101 px.
- La frise ne bouge pas : sa cellule 100 commence toujours en x 552 et fait 204 de large. `L'Accomplissement` (environ 133 px) ne tiendrait pas dans 184.

**Places de la colonne 100, y local à l'arbre (ajouter 52 pour la page), quelle que soit la branche ouverte :**

| Place | y | Centre | Ce qui s'y pose |
|---|---|---|---|
| Couple 1, place haute | 28 | 54 | 1re finale de la lignée 1, ou son évolution unique |
| Couple 1, place basse | 84 | 110 | 2e finale de la lignée 1, sinon vide |
| Couple 2, place haute | 216 | 242 | 1re finale de la lignée 2, sinon vide |
| Couple 2, place basse | 272 | 298 | 2e finale de la lignée 2, ou son évolution unique |

- **Rythme :** 4 px entre sœurs, comme entre les 2 lignées d'une branche ; 80 px entre les couples. Le bloc va de y 28 à y 324, soit 296 px. L'écart de 80 suffit à séparer les deux suites : on lit deux couples, et non une liste de 4.
- **Une évolution unique se pose à la place extérieure de son couple** (28 en haut, 272 en bas). Ces places sont à 28 px au moins de chaque centre de lignée (26, 82, 148, 204, 270, 326). Aucune liaison n'entre donc dans la colonne 100 à la hauteur où une autre lignée en sort. Au centre d'un couple (82), elle serait au droit de la lignée 2 de la branche 1, et la liaison se lirait fausse.
- **Même règle pour les 4 finales :** les centres 54, 110, 242 et 298 sont tous à 28 px au moins de chaque centre de lignée.
- **Cas mixtes, sans exception à écrire :**
  - Une branche dont une lignée a des finales et l'autre non : un couple plein, et une seule tuile dans l'autre couple.
  - Une classe dont une branche a des finales et une autre non : la colonne 100 suit la branche ouverte. Elle montre 4, 3 ou 2 tuiles.
- **Plus petite cible : 52 px de haut,** comme au §4. Aucun chevauchement : la colonne 100 est seule entre x 572 et x 740.

**Clés des tuiles :** `{lignée}:{finale}` pour une finale, sur le modèle de `admin --spec branche:lignée:finale`. `{lignée}@100` reste la clé d'une évolution unique. `SpecPage.RoadTo` donne `[branche, lignée, clé]` dans les deux cas.

**Constantes à ajouter à `SpecTree`** (avec `LastX` remplacé) : `FinalX = 572`, `FinalWidth = 168`, `ElbowIn = 544`, `ElbowMid = 552`, `ElbowOut = 560`, `FinalY(couple, sœur) = couple == 0 ? 28 + 56 × sœur : 216 + 56 × sœur`, `SoleY(couple) = couple == 0 ? 28 : 272`.

### 14.3 Les liaisons de la colonne 100 (remplace la dernière ligne du premier tableau du §5.3)

Une liaison par tuile de la colonne 100, tracée en entier depuis sa lignée : sortie, coude, entrée. Pour une tuile de centre y, issue d'une lignée de centre c, au coude x = e :

`(532, c) → (e, c) → (e, y) → (572, y)`

Le coude dépend de la branche ouverte. Deux lignées voisines ne sont qu'à 56 px l'une de l'autre et leurs couples à 188 px. Pour les branches du haut et du bas, leurs tracés verticaux se recouvriraient sur un même x. Ils s'emboîtent donc en escalier : la lignée qui a le chemin le plus court prend le coude extérieur.

| Branche ouverte | Centres des lignées | Coude de la lignée 1 | Coude de la lignée 2 | Tracés verticaux |
|---|---|---|---|---|
| 1 (y 28) | 26, 82 | **560** (ext.) | **544** (int.) | 26 → 110 à x 560 ; 82 → 298 à x 544 |
| 2 (y 150) | 148, 204 | 552 | 552 | 54 → 148 ; 204 → 298 : disjoints |
| 3 (y 272) | 270, 326 | **544** (int.) | **560** (ext.) | 54 → 270 à x 544 ; 242 → 326 à x 560 |

**Contrôle d'absence de croisement, branche 1 :**

- La sortie de la lignée 1 (y 26, de 532 à 560) passe x 544 au-dessus du tracé de la lignée 2, qui commence à y 82.
- Les entrées de la lignée 2 (y 242 et 298, de 544 à 572) passent x 560 sous le tracé de la lignée 1, qui finit à y 110.

La branche 3 est le symétrique. Les 16 px entre les deux coudes laissent passer le bois vivant (6 px, ombre de 8 décalée de 2) sans contact.

- **Aspects :** ceux du §5.3, tronçon par tronçon, d'après la tuile d'arrivée (`WoodTo`).
  - Vivant jusqu'à la finale choisie.
  - Nu (tirets) vers les 2 sœurs de sa lignée avant 100.
  - Ouvert vers une finale disponible.
  - Écarté pour le reste.
  - La partie commune (sortie et coude jusqu'à la première sœur) prend l'aspect le plus fort des deux tronçons, puisque l'ordre de dessin la repeint.
- **Longues liaisons :** celle de la lignée 2 de la branche 1 descend à x 544, à 12 px du bord droit des lignées des branches 2 et 3. Aucune ne sort de ces lignées (§14.1), donc aucune confusion n'est possible. Écartée, elle n'a qu'un trait de 1 px. Vivante, c'est la route du héros, et elle doit se voir.
- **Survol et aperçu de rature :** comme au §5.3, de la racine à la tuile survolée ou affichée, en passant par la lignée.

### 14.4 Le schéma

**Exemple :** Archer de niveau 100 sur la route Mire, puis Longue-vue, puis Œil-de-faucon. La fiche affiche Sans-répit, la sœur écartée. Mêmes signes qu'au §2.2. La hauteur n'est pas à l'échelle : les vraies valeurs sont au §14.2. Une colonne de caractères vaut environ 9 px. Les noms de finales sont des exemples.

```
[ Archer      >(30) L'Éveil          >(50) L'Affirmation    >(100) L'Accomplissement
[ Choix 3/3   >   3 routes, 1 choix  >  2 suites par route  >   2 classes par suite

                                      +###################+
                                    %%#(v)Longue-vue      #%%%%%
              +###################+ % +###################+    %
            %%#(v)Mire            #%%                          % +#################+
            % +###################+ -                          %%#(v)Œil-de-faucon #   y 28
            %                       - +...................+    - +#################+
            %                       --.(o)Herse           .--+ - +=================+
            %                         +...................+  | --=(/)Sans-répit    =   y 84
            %                                                |   +=================+
            %                         +...................+  |
            %                       --.(o)Porte-faucon    .  |
            % +...................+ - +...................+  |
+--------+  % .                   . -                        |
|        |%%%-.(/)Affût           .--                        |
|  (A)   |  - +...................+ - +...................+  |   +.................+
|        |  -                       --.(o)Tend-pièges     .  |---.(o)Herse ardente.   y 216
| Archer |  -                         +...................+  |   +.................+
+--------+  -                                                |   +.................+
            -                         +...................+  +---.(o)Dernier mur  .   y 272
            -                       --.(o)Saute-haie      .      +.................+
            - +...................+ - +...................+
[ Ouvrir ]  ---.(/)Sente          .---+...................+
[l'arbre ]    +...................+   .(o)Court-sentes    .
[  (T)   ]                            +...................+
            x 0       x 128                 x 344           x 544/560 x 572     x 740
```

- La lignée Longue-vue (centre 26) prend le coude extérieur (x 560) vers ses 2 finales du couple 1.
- La lignée Herse (centre 82) prend le coude intérieur (x 544) et descend vers le couple 2.
- Les lignées des branches Affût et Sente n'ont aucune liaison à droite.

**La fiche, à sa place du §4 (x 756, 352 × 404) :**

```
+--------------------------------------+
| (O)  Sans-répit                      |   tête : sceau 52, 22 gras
|      Niveau 100 · Longue-vue         |   14, --osmose-ui-text-dim
| ------------------------------------ |
| SIGNATURE · PLUS VITE                |   SectionLabel
| Tir visé revient sans relance, une   |   description, 4 lignes au plus
| fois par tour.                       |
|                                      |
| « Une flèche n'attend pas l'autre. » |   lore, 4 lignes au plus
|                                      |
|                                      |
| ------------------------------------ |
| Changer de classe : 500 Éclats.      |
| Tes sorts et leurs rangs ne bougent  |
| pas.                                 |
| [      Raturer : 500 Éclats        ] |   320 × 44, bouton secondaire
+--------------------------------------+
```

### 14.5 Les états d'une tuile finale (complète le §6)

L'état se calcule dans cet ordre, pour une finale F de la lignée l, de la branche b, au niveau 100 :

1. **Choisi :** le héros a b, l et F, et son niveau est d'au moins 100.
2. **À venir :** le héros a b et l, et son niveau est inférieur à 100. **Les 2 sœurs sont à venir,** puisqu'aucune n'est encore choisie.
3. **Écarté :**
   - le héros a l et une autre finale (c'est la sœur, **tête de route** : trait de crayon) ;
   - ou il a une autre branche, ou une autre lignée (pas de trait).
   - Exception, comme au §6 : les finales de la branche en aperçu, au niveau 100, passent à la règle 5.
4. **Verrouillé :** niveau inférieur à 100.
5. **Disponible :** la quête est faite, le niveau est d'au moins 100, et l'un de ces cas :
   - le héros a l sans finale ;
   - il a b sans lignée (le choix écrit la lignée et la finale d'un coup) ;
   - b est en aperçu.
6. **En attente :** tout le reste.

Une évolution unique de 100 garde les règles du §6, inchangées.

| | Verrouillé | Disponible | Choisi | Écarté | À venir | En attente |
|---|---|---|---|---|---|---|
| **Tuile** | `CompoundLocked` | `CompoundOpen` | `CompoundCreated` | `CompoundBlocked` | `SpecAhead` | `CompoundLocked` |
| **Sceau** | cadenas | sans marque | **anneau `--osmose-ui-gold`**, coche | trait de crayon si tête, sinon aucun | cadenas, anneau `--osmose-wood-light` | aucune |

- **Têtes de route :** 4 traits au plus, au lieu de 3 (§5.4) : 2 branches, la lignée sœur, la finale sœur.
- **Finales à choisir** (héros de niveau 100 ou plus, avec une lignée à finales, sans finale) : leurs 2 tuiles respirent, comme les lignées au §6.
- **Le choix direct est permis, pas mis en avant :** un héros de niveau 100 sans lignée voit ses lignées respirer, et les finales de sa branche sont disponibles sans respirer. Seule l'étape suivante appelle le regard.
- **Icône :** `SpecFinal.Icon`, chacune la sienne : deux sœurs ne doivent pas se confondre (Q14). Faute d'icône, l'icône de la lignée, en attendant S197.
- **En combat (`ReadOnly`) :** comme au §6.

### 14.6 La fiche d'une finale (complète le §5.5)

| Bloc | Contenu | Hauteur au pire |
|---|---|---|
| Tête | sceau 52 ; titre (`Tier.Title`) en 22 gras ; dessous `Niveau 100 · {titre de la lignée au 50}` | 52 |
| Filet | | 9 |
| **Signature** | `SectionLabel` : `SIGNATURE · PLUS VITE`, le nom du gabarit en capitales | **24** (20 + 4) |
| Description | `Tier.Description`, 14, `--osmose-ui-text` | **80 (4 lignes)** |
| Espace | | 8 |
| Phrase d'ambiance | `SpecFinal.Lore`, sinon celle de la lignée | 80 (4 lignes) |
| Pied | §14.7 | 101 |

**Total au pire : 32 + 354 = 386, sur 404.**

- La place du gabarit est prise sur la description : **une finale a 160 signes de description au plus** (4 lignes de 320 px), et 130 de phrase. Ce sont des contraintes pour le scénariste en S197. Une finale n'a pas de sort à présenter, ce qui rend ces limites tenables.
- **Pourquoi le gabarit en tête :** c'est ce qui distingue les deux sœurs (§2.3, « deux gabarits différents »). Le joueur qui survole l'une puis l'autre lit d'abord `PLUS LOIN` contre `PLUS VITE`.

**Tuile d'ouverture (complète le §5.5) :** avant les règles 1 à 3, au niveau 100 ou plus :

- si le héros a une lignée à finales sans finale : la première finale de sa lignée ;
- s'il a une finale : sa finale.

**Résumé de la racine (complète le §7.3) :** sous `TA ROUTE`, la 3e ligne devient :

- le nom de la finale ;
- ou `Classe finale : à choisir`, au niveau 100 ou plus, sur une lignée à finales ;
- ou `Classe finale au choix (au niveau 100)` avant 100.

Sur une lignée sans finales, rien ne change.

### 14.7 Le pied et le bouton d'une finale (complète le §7.3)

Exemples au niveau 100 : 5 × = 500, 10 × = 1 000, 30 × = 3 000 Éclats (`Specs.ChangeCost`, avec la finale en paramètre).

| Tuile affichée | Ligne 1 | Ligne 2 | Bouton |
|---|---|---|---|
| Quête non faite | comme au §7.3 | | aucun |
| Verrouillée, hors route | `Ouvert au niveau 100.` | `À 100, sa suite s'achève : l'une des deux, au choix.` | aucun |
| **À venir** (sa lignée, avant 100) | `Dans 38 niveaux.` | `Tu choisiras alors l'une des deux.` | aucun |
| **Disponible, sur sa lignée** | `Le premier choix est gratuit.` | `Changer ensuite : 500 Éclats.` | **`Prendre cette classe`** (`Primary`) |
| Disponible, lignée prise d'un coup (sa branche sans lignée, ou aperçu, premier choix) | `Le premier choix est gratuit.` | `Tu reçois d'un coup ce que ton niveau ouvre.` | `Prendre cette classe` (`Primary`) |
| **Choisie** | `Changer de classe : 500 Éclats.` | `Changer de suite : 1 000 Éclats.` | aucun |
| **Sœur écartée** | `Changer de classe : 500 Éclats.` | `Tes sorts et leurs rangs ne bougent pas.` | **`Raturer : 500 Éclats`** |
| Finale de la lignée sœur, au niveau 100 ou plus | `Changer de suite : 1 000 Éclats.` | `Cette classe vient avec, au même prix.` | `Raturer : 1 000 Éclats` |
| Finale d'une autre branche, au niveau 100 ou plus | `Changer de route : 3 000 Éclats.` | `Sa suite et cette classe viennent avec.` | `Raturer : 3 000 Éclats` |
| Finale d'une branche en aperçu, héros sans route | comme « lignée prise d'un coup » | | |

- **Bouton qui ne peut pas agir :** mêmes causes, même priorité et mêmes textes qu'au §7.4 : combat, `Enregistre d'abord`, `Dans un village`, `Il manque {0} Éclats`, et `Bientôt` pour une branche sans contenu. Le premier choix d'une finale est gratuit partout hors combat, comme celui d'une lignée.
- **Ce qu'envoie le bouton :** `PUT …/spec` avec `Spec`, `Fork` et `Final` (`SpecChoice`), toujours les trois, pour que le serveur n'ait rien à deviner. Il le refuse au-dessous de 100 et pour une finale d'une autre lignée (`Hero.SpecProblem`). Le client ne propose pas ces cas.
- **Sur une tuile de lignée à finales, déjà choisie, au niveau 100 ou plus, sans finale :** ligne 1 `Ta suite s'achève : choisis ta classe finale, dans la colonne 100.` C'est la même phrase que la ligne d'aide, comme `spec.splits`.

### 14.8 La confirmation (complète le §5.6)

**Premier choix d'une finale seule :**

```
        +-----------------------------------------------------------+
        |  Achever ta route ?                                       |
        |                                                           |
        |  TU REÇOIS                                                |
        |  (o)  Œil-de-faucon                                       |
        |       Niveau 100 · Signature : Plus loin                  |
        |                                                           |
        |  Prix : gratuit.                                          |
        |  Ensuite : 500 Éclats pour changer de classe finale,      |
        |  dans un village.                                         |
        |                                                           |
        |                [ Pas encore ]  [ Achever ma route ]       |
        +-----------------------------------------------------------+
```

**Rature d'une finale, vers sa sœur :**

```
        +-----------------------------------------------------------+
        |  Raturer Œil-de-faucon ?                                  |
        |                                                           |
        |  TU PRENDS                                                |
        |  (o)  Sans-répit                                          |
        |       Niveau 100 · Signature : Plus vite                  |
        |                                                           |
        |  Tes sorts, leurs rangs et ton paquet ne bougent pas.     |
        |                                                           |
        |  Prix : 500 Éclats. Il t'en restera 9 500.                |
        |                                                           |
        |          [ Garder ma classe ]  [ Raturer : 500 Éclats ]   |
        +-----------------------------------------------------------+
```

| | Finale seule, premier choix | Finale seule, rature | Lignée ou branche, avec la finale |
|---|---|---|---|
| **Titre** | `Achever ta route ?` | `Raturer Œil-de-faucon ?` | comme au §5.6 (`Écrire cette route ?`, `Raturer Longue-vue ?`, `Raturer Mire ?`) |
| **Quitter** | | | si le héros avait une finale, `Tu quittes : Longue-vue, Œil-de-faucon.` ; s'il change de branche, `Tu quittes : Mire, Longue-vue, Œil-de-faucon.` |
| **Lignes** | 1, la finale | 1, la finale | 2 ou 3 ; celle de la finale dit `Niveau 100 · Signature : {gabarit}` |
| **Rangs** | | `Tes sorts, leurs rangs et ton paquet ne bougent pas.` | comme au §5.6 |
| **Prix** | `Prix : gratuit.`, `--pistache` | `Prix : 500 Éclats…`, `--osmose-cost` | comme au §5.6 |
| **Ensuite** | `Ensuite : 500 Éclats pour changer de classe finale, dans un village.` | | au premier choix : `Ensuite, dans un village : 500 Éclats pour changer de classe finale, 1 000 de suite, 3 000 de route.` |
| **Refuser** | `Pas encore` | `Garder ma classe` | comme au §5.6 |
| **Accepter** | `Achever ma route`, `Primary` | `Raturer : 500 Éclats`, secondaire, `--osmose-cost` | comme au §5.6 |

- **La ligne d'une finale ne dit pas `Tes sorts changent`** (`spec.confirm.no-spell`), qui serait faux : une finale ne touche à aucun sort. Cette clé reste celle des évolutions uniques.
- **Rature d'une lignée sans finale nommée,** pour un héros qui en avait une (clic sur la tuile de lignée, au niveau 100) : une ligne de plus, avant le prix : `Ta classe finale s'en va : tu en prendras une de ta nouvelle suite, gratuitement.` C'est ce que fait le serveur : la finale tombe, et le choix suivant est gratuit.
- **Hauteur au pire :** inchangée, 394. Un changement de branche au niveau 100 donne 3 lignes (30, 50 et la finale), puisqu'une lignée à finales n'a pas d'évolution de 100. La ligne `Tu quittes` peut passer sur 2 lignes (+20) ; elle ne s'ajoute pas à la ligne `Ta classe finale s'en va`, qui ne sert que sans changement de branche.
- **Après la confirmation :** les mêmes 500 ms qu'au §7.4 : trait de crayon sur l'ancienne finale, sève jusqu'à la nouvelle, coche dorée. Message `path.evolved` avec le titre de la finale. Refus du serveur : comme au §7.4, `spec.refused`.

### 14.9 Le compteur et le point sur l'onglet (remplace le total du §5.1 et complète le §5.7)

**`Choix x/y` :**

- **Total :**
  - 0 sous 30, 1 de 30 à 49, 2 de 50 à 99 ;
  - **3 à 100 et plus, si une finale est possible :** la lignée du héros a des finales ; ou il n'a pas de lignée et une lignée de sa branche en a ; ou il n'a pas de branche et une branche de sa classe en a. Sinon 2.
- **Choix faits :** branche + lignée + finale.
- Couleurs inchangées.
- `Choix 3/3` (environ 72 px en 14 gras) tient dans les 100 px de la cellule.

**La frise, cellule 100 :**

- **Ligne 2 :** `2 classes par suite` si la branche ouverte a au moins une lignée à finales, sinon `Dernière évolution`. `2 classes par suite` fait environ 133 px pour 138 : à mesurer à l'autotest.
- **Infobulle :** une deuxième phrase après `path.tier.100` : `La colonne 100 montre la route affichée. Clique une autre route pour voir ses classes finales.`

**Le point sur l'onglet (`Pending`) :** même disque qu'au §5.7. Une condition s'ajoute : le héros a au moins le niveau 100, sa lignée a des finales, et il n'en a aucune. **Ligne d'aide :** `Ta suite s'achève : choisis ta classe finale, dans la colonne 100. Le premier choix est gratuit.` (96 signes, sous les 125).

### 14.10 Les textes nouveaux

À relire par le scénariste. Les mots à l'écran : `classe finale` (le mot de la story), `classe` quand il est seul (`Changer de classe`, `Garder ma classe`), `signature` pour le gabarit, et `achever` pour le premier choix.

| Clé | FR | EN |
|---|---|---|
| `spec.tier.100.finals` | 2 classes par suite | 2 classes each |
| `spec.tier.100.tip` | La colonne 100 montre la route affichée. Clique une autre route pour voir ses classes finales. | Column 100 shows the road on display. Click another road to see its final classes. |
| `spec.signature` | SIGNATURE · {0} | SIGNATURE · {0} |
| `spec.final.pick` | À 100, sa suite s'achève : l'une des deux, au choix. | At 100, its next step ends in one of two, your pick. |
| `spec.final.ahead` | Tu choisiras alors l'une des deux. | You will then pick one of the two. |
| `spec.final.then-cost` | Changer ensuite : {0} Éclats. | Changing later: {0} Éclats. |
| `spec.price.final` | Changer de classe : {0} Éclats. | Change class: {0} Éclats. |
| `spec.final.same` | Tes sorts et leurs rangs ne bougent pas. | Your spells and their ranks stay as they are. |
| `spec.final.with-fork` | Cette classe vient avec, au même prix. | This class comes along, at the same price. |
| `spec.final.with-branch` | Sa suite et cette classe viennent avec. | Its next step and this class come along. |
| `spec.final.due` | Ta suite s'achève : choisis ta classe finale, dans la colonne 100. Le premier choix est gratuit. | Your next step ends: choose your final class, in column 100. The first choice is free. |
| `spec.take.final` | Prendre cette classe | Take this class |
| `spec.confirm.final` | Achever ta route ? | Complete your road? |
| `spec.confirm.final.yes` | Achever ma route | Complete my road |
| `spec.confirm.final.keep` | Garder ma classe | Keep my class |
| `spec.confirm.signature` | Signature : {0} | Signature: {0} |
| `spec.confirm.final.same` | Tes sorts, leurs rangs et ton paquet ne bougent pas. | Your spells, their ranks and your deck stay as they are. |
| `spec.confirm.final.after` | Ta classe finale s'en va : tu en prendras une de ta nouvelle suite, gratuitement. | Your final class goes: you will take one from your new next step, for free. |
| `spec.confirm.later.final` | Ensuite : {0} Éclats pour changer de classe finale, dans un village. | After that: {0} Éclats to change final class, in a village. |
| `spec.confirm.later.all` | Ensuite, dans un village : {0} Éclats pour changer de classe finale, {1} de suite, {2} de route. | After that, in a village: {0} Éclats to change final class, {1} next step, {2} road. |
| `spec.summary.final` | Classe finale au choix | Final class of your choice |
| `spec.summary.final.due` | Classe finale : à choisir | Final class: to be chosen |

`spec.summary.final` passe par `spec.summary.later` : `Classe finale au choix (au niveau 100)`. `spec.confirm.leave`, `spec.level`, `spec.opens`, `spec.ahead`, `spec.free`, `spec.at-once`, `spec.price.fork`, `spec.price.branch`, `spec.cross`, `spec.need.*`, `spec.soon` et `path.evolved` servent tels quels.

**Les 12 gabarits (`FinalTemplate`), en mots courts.** Ils sont affichés en capitales dans `spec.signature`, et tels quels dans `spec.confirm.signature`.

| Clé | Gabarit | FR | EN |
|---|---|---|---|
| `spec.template.Extends` | le sort s'étend | Plus loin | Further |
| `spec.template.Trace` | le sort laisse une trace | Trace au sol | Ground mark |
| `spec.template.Faster` | le sort revient plus vite | Plus vite | Sooner |
| `spec.template.Second` | un second exemplaire | Un second | A second one |
| `spec.template.Saves` | une fois par combat, ça sauve | Sauvetage | Rescue |
| `spec.template.LowHp` | sous un seuil de PV | À bout de PV | Low on HP |
| `spec.template.Kill` | achever rapporte | Coup de grâce | Finishing blow |
| `spec.template.FirstTurn` | premier tour | Premier tour | First turn |
| `spec.template.StayOrMove` | rester ou bouger | Rester ou bouger | Stay or move |
| `spec.template.TargetState` | cible en état | Selon la cible | By target's state |
| `spec.template.Death` | la mort paie | La mort paie | Death pays |
| `spec.template.Form` | une forme durable | Métamorphose | Transformation |

**Les plus serrés, à mesurer :**

- `2 classes par suite` (138 px) ;
- `SIGNATURE · RESTER OU BOUGER` (320 px, sans risque) ;
- `Ta suite s'achève : choisis ta classe finale, dans la colonne 100.` sur 2 lignes de pied ;
- `Raturer : 3 000 Éclats` (320 px) ;
- un nom de finale de 2 morceaux de 13 signes dans 108 px.

Le test de complétude des clés (`SpecPageTests`, ligne 392) doit couvrir les 12 gabarits dans les deux langues.

### 14.11 Couleurs

**Aucun jeton nouveau, aucune variation nouvelle :**

- les tuiles prennent les 5 variations du §6 ;
- l'anneau d'une finale choisie est `--osmose-ui-gold`, comme celui de l'évolution de 100 ;
- le gabarit est en `SectionLabel` ;
- les prix sont en `--osmose-cost` ;
- le pistache reste réservé au premier choix et à la route.

### 14.12 Conséquences à l'implémentation

- **`SpecPage` :**
  - `OpenBranch` (paramètre, comme `preview`) ;
  - les nœuds de finales (`SpecNode` avec `Final`), et leurs états dans l'ordre du §14.5 ;
  - le pied du §14.7, avec `Specs.ChangeCost(hero, spec, fork, final)` ;
  - `Confirm(spec, fork, final)` ;
  - `Choices` et `Pending` du §14.9 ;
  - `Opening` du §14.6.
- **`SpecTree` :** les constantes du §14.2, `ToFinal(branche, couple, y)` avec le coude du §14.3. `ToLast` disparaît.
- **`PointsPanel.Spec.cs` :**
  - les tuiles de la colonne 100 se reconstruisent quand la branche ouverte change (4 au plus), et non plus une fois pour toutes ;
  - la colonne ne suit pas `SetSpecHovered`.
- **Tests xUnit :** sur une `GameData` de test avec des finales, puisque les données réelles n'en ont aucune avant S197.
  - **Géométrie :** aucun chevauchement pour chacune des 3 branches ouvertes ; aucun centre d'entrée de la colonne 100 à moins de 28 px d'un centre de lignée ; aucun croisement de liaisons (segments testés deux à deux) ; tout tient dans 740 × 352.
  - **États :** les 6 règles du §14.5 ; 4 têtes au plus ; les cas mixtes (une lignée à finales et une sans, une branche à finales et une sans).
  - **Pied et prix :** chaque ligne du §14.7, avec 5 ×, 10 × et 30 × le niveau.
  - **Compteur et point :** `Choix 3/3`, le total à 2 sans finales.
- **Autotest Godot :** la fiche tient pour chaque finale (`ReadingFits`) ; aucun titre de la colonne 100 ne dépasse 2 lignes ; les cellules de frise tiennent en français et en anglais. Le scénario : Plouf à 100, choisir une finale, puis raturer vers sa sœur, refusé hors village.
- **Ce qui change dès S196, sans données :** la colonne 100 se réduit aux 2 évolutions de la branche ouverte, en x 572, aux places 28 et 272. Les tests de géométrie S191 sur `LastX` sont à réécrire.

### 14.13 Les points à faire trancher par Charles

| # | Question | Ma recommandation |
|---|---|---|
| **Q8** | Une règle (la colonne 100 montre toujours la seule branche ouverte, même sans finales) ou deux modes (la mise en page S191 à 6 tuiles tant que la branche ouverte n'a pas de finales, avec des tuiles repliées pour les branches qui en ont) ? | **Une règle.** Elle ne demande ni mode, ni tuile repliée, ni seconde géométrie à tester. Les deux modes ne serviraient que pendant la conversion des classes, de S197 à la fin. Le prix : dès S196, il faut un clic pour lire les évolutions de 100 d'une autre route |
| **Q9** | La branche ouverte suit le clic. Lire une autre route cache donc les finales du héros jusqu'au clic suivant (la racine les ramène). On accepte ? | **Oui.** Suivre le survol rendrait la colonne insaisissable, et figer la branche du héros empêcherait de comparer |
| **Q10** | Permet-on de prendre une finale d'une autre lignée ou d'une autre branche en un seul clic, au prix de la lignée ou de la branche (le serveur l'accepte déjà) ? | **Oui,** c'est la règle du §7.4 (« un seul prix »). Sans cela, il faut deux confirmations, la seconde gratuite |
| **Q11** | Point sur l'onglet pour une finale due au niveau 100 ? | **Oui,** c'est la même attente que la lignée à 50 |
| **Q12** | Contraintes de texte pour S197 : un nom de finale de 13 signes par morceau et 2 morceaux au plus, 160 signes de description, 130 de phrase | **Oui,** à transmettre au scénariste avec la commande des 120 finales |
| **Q13** | Les mots : `classe finale`, `Achever ta route ?`, `SIGNATURE · …`, `Garder ma classe`, et les 12 noms de gabarits | **À faire relire par le scénariste.** Le plus fragile : `Selon la cible`, qui dit mal « cible dans un état donné » ; une autre possibilité est `Cible entravée`, si tous les gabarits de ce type visent une entrave |
| **Q14** | Chaque finale a-t-elle sa propre silhouette (12 par classe, 120 en tout), ou reprend-elle celle de sa lignée, comme l'évolution de 100 ? | **Sa propre silhouette :** deux sœurs côte à côte, avec la même icône et le même anneau, ne se distinguent que par le nom. Planche à faire en S197, classe par classe ; d'ici là, repli sur l'icône de la lignée |
| **Q15** | La colonne 100 passe à 168 px en x 572, alors que la frise reste en x 552 : la cellule de frise ne commence plus au x de sa colonne (§5.1) | **Accepter** cet écart de 20 px : il donne aux coudes la place de ne pas se toucher, et `L'Accomplissement` ne tiendrait pas dans une cellule de 184 |

**Réponses de Charles (6 novembre 2026) :** Q8 une seule règle ; Q9, Q11, Q12 et Q15 acceptées ; Q10 oui, un seul prix ; Q14 oui, une silhouette par finale. Q13 (relecture des mots par le scénariste) reste à faire. Code : sprint 196, T159.

### 14.14 Limites

- **Rien n'a été rendu.** Les largeurs suivent la règle du §13 : 7 px par signe en 14, et 7,8 en 14 gras.
- **Je n'ai pas dessiné les 120 finales,** ni vérifié qu'aucune silhouette ne sert déjà à un sort (Q14).
- **Les liaisons en escalier** sont le point le plus incertain à l'œil, pour les branches 1 et 3. Si la capture les montre confuses, la première correction est d'éclaircir le tracé écarté (1 px `--osmose-out-of-level`, déjà très discret) avant de toucher à la géométrie.
- **Le bloc fixe laisse un vide de 80 px** au milieu de la colonne 100 quand la branche du milieu est ouverte. C'est voulu : c'est l'écart entre les deux couples.
