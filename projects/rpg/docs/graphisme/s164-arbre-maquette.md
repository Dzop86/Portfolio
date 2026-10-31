# S164, S176-S177 : maquette visuelle de l'arbre (l'espalier)

Structure : `docs/arbre-lisible.md` (game designer, 30 oct.), qui fixe la grille, les 4 aspects, les 3 signes passagers, les 7 pièces de bois et leurs 3 vies, et l'ordre de saillance pris > ouvert > pas encore > fermé. Ce document en donne le **rendu** : couleurs (jetons), épaisseurs, tailles. Il **remplace le §3 de `s163-icones-arbre.md`** partout où les deux diffèrent (§6).
Charte : `src/assets/tokens.css`, thème S157 (`s157-maquettes.md` §4). Tout texte fait 14 px au moins ; le pistache n'est jamais du texte sur fond clair.

---

## 1. Désaccords avec le GD (un par ligne)

1. **Textes de 11 et 13 px → 14 px** (charte). Noms de nœuds sur 2 lignes de 66 px : le scénariste fournit des noms courts (`talent.short.<id>`). Le budget vertical tient, voir §3.2.
2. **Racines et ligne « Feu + Air »** : le tronc descend de 16 px (cernes à y 494, 542 et 590 au lieu de 478, 526 et 574) pour que les racines ne passent pas sous les noms des portes. Il reste 10 px sous le cerne 30.
3. **« Ouvert », fond vide → corps en relief neutre** : dans tout le thème, le relief signifie « cliquable ». L'icône reste claire, comme le GD le demande.
4. **« Pas encore », icône à 40 % → variante `-locked` grise et plate** : même saillance, sans transparence qui salit le fond.
5. **Focus clavier, cadre carré → anneau de la forme du nœud** : un carré autour d'un rond était le constat n° 6 de ma relecture.
6. **[Fermer] → bouton × du thème** (`CloseButton`, 44 × 44, infobulle « Fermer (Échap) ») : même place, moins large.
7. **Maquette B, tronc** : j'applique la règle du §2.7 (bois vivant jusqu'au dernier cerne atteint), donc le bois entre 30 et 35 est nu à 30, alors que la maquette le dessine vivant.

Tout le reste du GD est repris tel quel, notamment « fermé » sans contour, à 25 %, avec une barre oblique, et le plan en contour pointillé pistache.

---

## 2. Jetons

Jetons existants employés : `--pistache #bef374`, `--chocolat #5a3a22`, `--chocolat-clair #c9a27f`, `--osmose-ui-frame #7a5434`, `--osmose-ui-frame-dark #3a2616`, `--osmose-ground #1b1b1b`, `--osmose-ui-window #1f1f1f`, `--osmose-ui-titlebar #181818`, `--osmose-ui-well #141414`, `--osmose-ui-raised-top #3a3a3d`, `--osmose-ui-raised-bottom #262628`, `--osmose-ui-lit-top #3d4f24`, `--osmose-ui-lit-bottom #25301a`, `--osmose-taken-fill #33421f`, `--osmose-open #e0e0e0`, `--osmose-open-fill #262626`, `--osmose-locked #8a8a8a`, `--osmose-losing #f08070`, `--osmose-losing-fill #3a2424`, `--osmose-cost #f2d36b`, `--osmose-ui-gold-top`, `--osmose-ui-gold-bottom`, `--osmose-ui-gold-text`, `--osmose-ui-text*`, les couleurs d'élément et de style.

**À créer** (bloc « Osmose, l'arbre (S164, S176-S177) ») :
```css
--osmose-wood: #7a5434;        /* = --osmose-ui-frame : le bois */
--osmose-wood-light: #a07850;  /* reflet du bois ; 3,6:1 sur --osmose-ground */
--osmose-wood-dark: #3a2616;   /* = --osmose-ui-frame-dark : ombre du bois, bord du collet */
--osmose-tree-guide: #262626;  /* = --osmose-open-fill : lignes guides des étages */
```
Plus les six jetons d'élément de `s163-icones-arbre.md` §1.4 (`--osmose-neutral`, `--osmose-ice`, `--osmose-mud`, `--osmose-magma`, `--osmose-steam`, `--osmose-sand`).

**À retirer à l'étape É1** (plus aucun usage) : `--osmose-excluded`, `--osmose-excluded-fill`, `--osmose-out-of-level`, `--osmose-out-of-level-fill`, `--osmose-coming`, et les constantes correspondantes de `ElementStyle`. Le test « aucune couleur en dur » de T116 suit.

---

## 3. Les pièces

### 3.1 Cadre de l'écran (grille du GD §2.4)
| Zone | Rect | Fond | Bord |
|---|---|---|---|
| Fenêtre | 0, 0, 1280 × 720 | `--osmose-ui-window` | 2 px `--osmose-ui-frame` (`WindowPanel`) |
| Barre du haut | 0, 0, 1280 × 52 | `--osmose-ui-titlebar` | bas 2 px `--osmose-ui-frame-dark` |
| Canevas | 16, 60, 932 × 644 | `--osmose-ground`, plus une bande de sol : les 40 px du bas en dégradé vers `#231a13` (`mix(ground, chocolat, 0.15)`) | rayon 8, aucun bord |
| Panneau de droite | 960, 60, 304 × 644 | `--osmose-ui-window` | gauche 1 px `#ffffff0f` |

### 3.2 Lignes et gouttière (y local au canevas)
| Ligne | Nœuds | Noms (14 px, interligne 18) |
|---|---|---|
| Nom de branche | — | 14 px gras capitales, couleur de la branche, + « · 2/4 » `--osmose-ui-text-dim`, 18 px au-dessus du nœud le plus haut |
| Clés | 40 à 104 | 106 à 142 (2 lignes) |
| Étage 2 | 144 à 200 | 202 à 238 |
| Étage 1 | 240 à 296 | 298 à 334 |
| Couronne | collet centré (470, 352) | — |
| Portes | 368 à 432 (Cœurs : 376 à 424) | nom 434 à 452 ; « Feu + Air » 452 à 470 en `--osmose-ui-text-dim` |
| Racines | 472 à 494 | — |
| Tronc | cernes de 44 à y 494, 542 et 590 (x centre 470) | à droite du cerne, 12 px d'écart : « 40 · +5 % PV », 14 px |
| Légende | carte x 640 à 932, y 478 à 640 | §3.11 |

- **Gouttière** (x 0 à 72) : libellés 14 px `--osmose-ui-text-dim`, alignés à gauche à x 8, centrés verticalement sur leur ligne : « Clés / niv. 45 / 2 pts » (trois lignes), « Étage 2 », « Étage 1 », « Portes », « Tronc ».
- **Lignes guides** : 1 px `--osmose-tree-guide`, de x 72 à 932, au centre de chaque ligne d'étage (Clés, Étage 2, Étage 1). Toujours dessinées, sous le bois.

### 3.3 Le bois
Une épaisseur par pièce (rapports du GD §2.7, avec 1 unité ≈ 3,2 px) :

| Pièce | Épaisseur | Tracé |
|---|---|---|
| Tronc | 20 au sol → 16 en haut | vertical, x 470, du bas du cerne 30 (634) au collet de la fourche (472) |
| Racine de fourche | 6 | Bézier du haut du tronc (470, 494) au bas des noms de chaque porte (x de la porte, 472) |
| Tige | 10 | Bézier d'une porte prise (son haut) au collet de la couronne |
| Charpentière | 10 → 8 | Bézier du collet au pied de la branche (bas des noms de l'étage 1, 334), en éventail, jamais vers le bas |
| Flèche de la Fusion | 10 | droite, du haut de Foudre au bas du cadre « 1 au choix » de la Fusion |
| Axe | 8 | vertical au centre de la branche, d'un étage au suivant ; il passe dans l'écart de 10 px entre deux nœuds |
| Rameau | 4 | horizontal, de l'axe au centre du nœud, sous le nœud |

Godot : un `Line2D` par couche (`width_curve` pour le tronc et les charpentières, joints et bouts ronds, anticrénelage), Bézier échantillonnée en 16 segments (le test G5 du GD les reprend).

**Les trois vies** (une pièce de 8 px ou plus reçoit l'ombre et le reflet ; en dessous, un seul trait) :

| Vie | Couches, de bas en haut | Lecture sans couleur |
|---|---|---|
| **Vivant** | ombre : largeur + 4, `#000` à 0,35, décalage (0, 2) · bois : largeur, `--osmose-wood` · **sève** : `max(3, 50 % de la largeur)`, `--pistache` · lueur : largeur + 6, `--pistache` à 0,12 (pièces de 8 px ou plus). Rameau : 4 px `--pistache` uni | trait plein et épais, cœur clair |
| **Atteignable** | ombre comme ci-dessus · bois `--osmose-wood` · reflet : 35 % de la largeur, `--osmose-wood-light`, décalé de 20 % vers la gauche. Rameau et racine : `--osmose-wood-light` uni | trait plein |
| **Nu** | tirets de 6 px, intervalles de 6, épaisseur 2 px, `--chocolat` (1,8:1 : il s'efface, comme voulu) | pointillé |

**Signes passagers sur le bois** :
- **Chemin** (survol d'un nœud non pris) : les pièces du chemin reçoivent la sève `--pistache` à 60 % ; une pièce nue du chemin est tracée pleine le temps du survol.
- **À rendre** : la sève des pièces perdues passe en `--osmose-losing`. C'est le seul rouge de la fenêtre.

**Collet de la couronne** : disque de 20 px, dégradé vertical `--osmose-wood-light` → `--chocolat`, bord 2 px `--osmose-wood-dark`. Nourri (une porte prise) : un cœur de 8 px `--pistache`. Non cliquable.

### 3.4 Les nœuds : correspondance aspect → rendu
Formes et tailles (G8 du GD) : talent rond 56 ; clé carré arrondi 64 (rayon 14) ; paire ronde 64 ; Cœur rond 48 ; bourgeon rond 48 ; cerne rond 44. Les textures d'état sont générées par `ui_art.py` (`node-<aspect>.svg`, `key-<aspect>.svg`), selon la recette des cadres de S157.

| Aspect | Corps | Contour | Icône | Ombre et halo | Nom |
|---|---|---|---|---|---|
| **Pris** (saillance 1) | dégradé `--osmose-ui-lit-top` → `--osmose-ui-lit-bottom` + reflet (`#fff` 0,18 → 0 sur le haut) | **3 px plein** `--pistache` | **en couleur**, relief (`talents/<id>.svg`), 57 % du nœud | ombre (0, 2) `#000` 0,45 ; halo 3 px `--pistache` 0,35 à r + 4 | `#ffffff` |
| **Ouvert** (2) | dégradé `--osmose-ui-raised-top` → `--osmose-ui-raised-bottom` + reflet | **2 px plein** `--osmose-open` | **claire** : variante `<id>-open.svg`, la silhouette en relief dans les tons de la palette `menu` (`#e5e5e5` / `#cfcfcf` / `#727272` / `#292929`) | ombre (0, 2) | `--osmose-open` |
| **Pas encore** (3) | `--osmose-ui-well` uni, sans reflet | **2 px pointillé** `--osmose-locked` (arcs de 5 px, intervalles de 4) | `<id>-locked.svg` (gris plat `#7a7a7a`) | aucune (creux) | `--osmose-ui-text-dim` (6,3:1) |
| **Fermé** (4) | `--osmose-ui-well` à 25 % | **aucun** | `-locked` à 25 % | aucune | `--osmose-locked` (5,0:1, AA) |

- **Barre oblique du fermé** : de 30 % à 70 % de la diagonale (bas gauche → haut droit), 2 px `--osmose-ui-text-off` `#6a6a6a`, opaque, bouts ronds. Ni rouge ni croix.
- **Aperçu de fermeture** (survol d'un nœud qui fermerait d'autres nœuds, GD §2.7) : ces nœuds gardent leur aspect et reçoivent la barre oblique en `--chocolat-clair`, à 100 %.
- **Survol** (pris, ouvert, pas encore) : contour `--chocolat-clair` (un nœud pris garde le pistache), nœud remonté de 1 px, curseur main. Un nœud fermé ne réagit pas au survol, sauf par son infobulle.
- **Focus clavier** : anneau 2 px `#ffffff` à 4 px du bord, de la forme du nœud (rond ou carré arrondi de rayon 18).
- **Planifié** (GD, inchangé) : anneau pointillé 2 px `--pistache` à r + 4 (16 arcs). Le mode plan l'annonce aussi dans la barre du haut (§3.12).
- **À rendre** : corps `--osmose-losing-fill`, contour 3 px `--osmose-losing`, icône en couleur, nom `--osmose-losing`.
- **Respiration** : seulement sur la porte **conseillée** à 30, tant que rien n'est pris (C5 b) : halo `--pistache` dont l'opacité va de 0 à 0,35 en 1,6 s. Ailleurs, rien ne bouge. Je retire la respiration de tous les nœuds ouverts (s164 §3.4) : avec dix portes ouvertes, elle ferait du bruit.

Icônes de talents : trois variantes par talent (couleur, `-open`, `-locked`), toutes générées par `ui_art.py`. Couleur par famille (s163 §3.5) : portes et Cœurs dans leur élément ; talents de style dans la couleur du style ; tronc en or ; Fusion dans la couleur de son composé (Foudre `--osmose-lightning`).

### 3.5 Les portes
- **Paire** (64, ronde) : l'aspect s'applique, plus un **liseré intérieur** de 3 px collé au contour, en dégradé diagonal des deux parents (`main` de chacun, comme le liseré des tuiles de sorts composés), et **deux pastilles** d'élément de r 6 au bas du nœud, intérieures, centres à ±9 px de l'axe et à 10 px du bord bas, bord 1,5 px `#0d0d0d`. Pour une porte fermée, liseré et pastilles sont à 25 %, comme le reste.
- **Cœur** (48, rond) : même chose avec un liseré d'un seul élément et une seule pastille.
- **Couples** : 10 px dans un couple, 28 entre deux couples (GD) ; aucune accolade.
- **Positions** (x local) : Foudre 88, Boue 162, Glace 254, Magma 328, Vapeur 420, Sable 494 (bords gauches des disques de 64) ; Cœurs Terre 600, Feu 658, Eau 716, Air 774 (disques de 48).
- **Cadre « 1 au choix »** (Cœurs, et étage 1 de la Fusion) : rectangle à 8 px autour des nœuds et de leurs noms, 1 px `--osmose-wood`, rayon 10, sans fond. Le titre « 1 au choix » (14 px, `--chocolat-clair`) est incrusté dans le bord haut, à 12 px du coin, sur un fond `--osmose-ground` de 4 px de chaque côté. Cœurs : x 586 à 836, y 360 à 476.
- **Conseillée** (héros de 30) : pastille 20 px de haut au-dessus de la porte (y 344 à 364), fond `--pistache`, texte « conseillé » 14 px gras `--chocolat`, rayon 10, plus la respiration.

### 3.6 Le tronc et ses cernes
- Cerne de 44 : **atteint** (pris) = corps en dégradé `--osmose-wood-light` → `--chocolat`, contour 2 px `--pistache`, icône du tronc en or (lorc/broadsword, lorc/bordered-shield, lorc/heart-organ, palette des succès), ombre (0, 2) ; **pas encore** = aspect « pas encore » (creux, pointillé `--osmose-locked`, icône `-locked`).
- Texte à droite : le niveau en 14 px gras `--osmose-open`, « · », puis l'effet en 14 px `--osmose-ui-text` ; en `--osmose-ui-text-dim` si le cerne n'est pas atteint.
- Les cernes ne sont pas cliquables ; le clavier les atteint (G12) et leur infobulle les explique.

### 3.7 Branches, nœuds de style et clés
- **Nom de branche** : capitales 14 px gras, dans la couleur de la branche (contrastes sur `--osmose-ground` : Mêlée 8,2, Distance 8,0, Soutien 7,6, Terrain 7,5, Tempo 6,6, Alchimie 9,6, Fusion-Foudre 12,1) ; compteur « · 2/4 » 14 px `--osmose-ui-text-dim`.
- **Clé** (64, carré arrondi) : aspect du tableau, plus une **gemme** de la couleur du style, losange de 12 px à cheval sur le bord haut, centré, bord 1,5 px `#0d0d0d`. La clé Conversion de la Fusion porte la gemme `--osmose-lightning`.
- **Badge de coût 2** (clés) : disque de 20 px, coin haut droit, débordant de 4 px, dégradé `--osmose-ui-gold-top` → `--osmose-ui-gold-bottom`, bord 2 px `--chocolat`, « 2 » en 14 px gras `--osmose-ui-gold-text`.
- **Badge de niveau** (« pas encore » pour cause de niveau) : pastille 28 × 20, coin haut gauche, débordant de 6 px, fond `--osmose-ui-well`, bord 1 px `--osmose-locked`, rayon 10, « 45 » en 14 px gras `--osmose-ui-text-dim`. En haut et non en bas : en bas, il heurterait le nom.
- **Badge d'équipe** (Soutien et Empreinte partagée) : disque de 20 px, coin bas droit, la silhouette delapouite/three-friends en relief (palette `contacts`) sur fond `--osmose-ui-well`, bord 1 px `#0d0d0d`. Il remplace les deux têtes dessinées à la main.

### 3.8 Les bourgeons
- Rond de 48 ; **aspect = meilleur aspect de la branche** (GD) ; un anneau intérieur de 2 px dans la couleur de la branche (à 25 % si la branche est fermée) ; icône de la branche dans la variante de son aspect.
- Icônes de branche : celles d'un talent emblématique, qui ne s'affiche jamais en même temps que le bourgeon :

| Branche | Silhouette |
|---|---|
| Fusion | lorc/lightning-tree (= Conversion) |
| Alchimie | lorc/fizzing-flask (= Catalyse) |
| Mêlée | lorc/crossed-swords (= Corps à corps) |
| Distance | lorc/spyglass (= Longue vue) |
| Tempo | lorc/stopwatch (= Premier geste) |
| Terrain | lorc/rune-stone (= Sceau) |
| Soutien | delapouite/hand-of-god (= Coup de main) |

- Au-dessus : le nom de la branche (§3.7) ; en dessous, la phrase de la branche (« au contact », 14 px `--osmose-ui-text-dim`, une ligne) puis le compteur « 0/4 », 14 px `--osmose-ui-text-dim`.
- **Bourgeon compact « +n »** : rond de 48, aspect le meilleur des nœuds cachés, « +3 » en 16 px gras `--osmose-open` à la place de l'icône.

### 3.9 Infobulles
Infobulles : `TooltipPanel` du thème (fond `#181818f7`, bord 1 px `--chocolat-clair`). Le titre est le nom complet (les noms courts ne servent que sous les nœuds).

### 3.10 Le tutoriel
Bulle : `TooltipPanel` avec un bord de 2 px `--pistache`, une flèche de 12 px vers sa cible, largeur 400, texte 16 px `--osmose-ui-text-strong`, « 1/3 » en 14 px `--osmose-ui-text-dim`, bouton [Suivant] `ButtonPrimary`. Elle est ancrée dans la couronne vide (y 160 à 330) et ne couvre jamais les portes (GD §2.9).

### 3.11 La légende permanente
Carte x 640 à 932, y 478 à 640 (local), fond `--osmose-ui-well`, bord 1 px `#ffffff0f`, rayon 8, marges 12.
- Titre « LÉGENDE » : 14 px gras capitales `--chocolat-clair`.
- Quatre lignes de 28 px : un nœud miniature de 24 px dessiné par `TreeShapes` (le vrai rendu, barre oblique comprise), puis le texte 14 px `--osmose-ui-text` : « pris : à toi », « ouvert : un clic », « pas encore : il manque un nœud ou un niveau », « fermé : survole pour savoir pourquoi ».
- Dessous, 14 px `--osmose-ui-text-dim`, sur deux lignes : « Flèches : se déplacer · Entrée : prendre ou rendre » / « Ctrl+S : enregistrer ».

### 3.12 La barre du haut (52 px)
```
x16                                                                                                          x1264
| ARBRE DES TALENTS · Sentinelle niv. 45 |  (2) points à placer · 7 au total  |  [Outils ▾] [?] [Annuler] [Enregistrer] [×] |
```
- Titre : « ARBRE DES TALENTS » 18 px gras `#ffffff`, puis « · Sentinelle niv. 45 » 14 px `--osmose-ui-text-dim`, centré verticalement.
- **Compteur, point focal** : badge de 28 px de haut (largeur au contenu, au moins 28, rayon 14) en relief `--osmose-ui-primary-top` → `--osmose-ui-primary-bottom`, bord 1 px `--osmose-ui-primary-edge`, chiffre 18 px gras `--chocolat`, puis « points à placer · 7 au total » 16 px `--osmose-ui-text-strong`. **À 0** : badge creux `--osmose-ui-well`, bord 1 px `--osmose-locked`, « 0 » `--osmose-ui-text-dim`, et le texte « prochain point au niveau 48 ». **En mode plan** : badge au contour pointillé `--pistache`, texte « Plan au niveau 60 · rien ne s'enregistre ».
- Boutons, 44 px de haut, 8 d'écart : [Outils ▾] `ButtonSecondary` (menu : Build suggéré, Planifier au niveau 60, Copier le code, Charger un code…) ; [?] 44 × 44 secondaire, à bascule (allumé = `tile-lit`) ; [Annuler] secondaire, désactivé sans brouillon ; [Enregistrer] **`ButtonPrimary`**, désactivé sans brouillon ; [×] `CloseButton`.

### 3.13 Le panneau de droite (304 × 644, marges 16)
Trois blocs ; chaque en-tête est en 14 px gras capitales `--chocolat-clair`, avec une séparation de 1 px `#ffffff0f` et 12 px au-dessus.
1. **Mon build** (hauteur au contenu, environ 190) : titre du build 18 px gras `#ffffff` ; « Hors base : 1 sur 2 » et « Grande bascule : prise » 14 px `--osmose-ui-text` ; « Ce que tu paies » 14 px gras, puis 3 puces au plus (puce 6 px `--chocolat-clair`, texte 14 px `--osmose-ui-text`), « et 2 autres » en lien `--chocolat-clair` ; « Meilleur coup : +55 %, plafonné à +35 % », les valeurs en `--osmose-cost`. Sans brouillon ni point (héros de 30) : « 2 POINTS À PLACER », puis le conseil de porte.
2. **Survol** (défile si besoin) : icône de 48 dans son aspect, nom 16 px gras `#ffffff`, puis l'aspect en toutes lettres (14 px : « à toi » `--pistache` ; « ouvert » `--osmose-open` ; « pas encore » et « fermé » `--osmose-ui-text-dim`), puis la fiche T116 et T117 en 14 px `--osmose-ui-text`, avec le coût en `--osmose-cost` et les nœuds qu'il fermerait sous « Ferme : » en `--osmose-ui-text-strong`.
3. **À enregistrer** (seulement avec un brouillon) : lignes « + Pied ferme » (le « + » en `--pistache`), « − Prisme » (le « − » et le nom en `--osmose-losing`).

---

## 4. Maquette A : héros de 45, Foudre convertie et Corps à corps (0 point)

Une colonne de caractères vaut environ 10 px. Signes : `(##)` pris · `(  )` ouvert · `(..)` pas encore · `( /)` fermé · `[ ]` clé · bois `║` vivant · `│` atteignable · `┊` nu · `◆` collet.

```
 ARBRE DES TALENTS · Sentinelle niv. 45   (0) prochain point au niveau 48        [Outils ▾] [?] [Annuler] [Enregistrer] [×]
════════════════════════════════════════════════════════════════════════════════════════════╤══════════════════════════════
       ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  MÊLÉE · 4/4 ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │ MON BUILD
Clés                                         ◇[####]2                                       │ Foudre convertie · Mêlée
niv. 45                                      Corps à                                        │ Hors base : 1 sur 2
2 pts                                         corps                                         │ Grande bascule : prise
                FUSION · 3/5                    ║                                           │ CE QUE TU PAIES
Étage 2 ─ ─ ─ ─ ( )─┬─( ) ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ (####) ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │ • plus aucun sort Feu ni Air
              Signature Prisme              Poussée                                         │ • portée 2 au plus
                appuyée  │                   lourde        DISTANCE  TEMPO  TERRAIN SOUTIEN │ • −5 % si tu as bougé
        ┌─ 1 au choix ───┼──────┐ ALCHIMIE      ║                                           │ Meilleur coup : +55 %,
Étage 1 │( /)  ( /)  ◇[####]2   │  (  )   (####)╬──(  )   ( /)    (  )    (  )    (  )      │ plafonné à +35 %
        │ Feu   Air  Convers.   │ réactions Pied   Au plus  de loin rythme glyphes alliés    │──────────────────────────────
        └────────║──────────────┘  0/6    ferme   près      0/4    0/9    0/10    0/8      │ SURVOL
                 ║                  │         ║               ┊       │      │       │      │ [##] Corps à corps
                 ║                    ╲       ║        ┊          ╱      ╱      ╱           │ à toi · clé de Mêlée
                 ║                       ╲    ║     ┊      ╱  ╱   ╱                         │ +25 % ; portée 2 au plus.
                 ║                           ╲║ ┊ ╱ ╱  ╱                                    │ Ferme : Longue vue,
                 ║                            ◆ (nourri)                                    │ Artillerie.
                 ╚════════════════════════════╝                                            │
Portes  (#Fo#) (Boue)   (Gl/) (Ma/)   (Va/) (Sa/)  ┌─ 1 au choix ─────────┐                │
        Foudre  Boue    Glace Magma   Vapeur Sable │ (Te) (Fe) (Ea) (Ai)  │                │
       Feu+Air Ter+Eau  Eau+Air ...                │ Terre Feu  Eau  Air  │                │
           ║      │     ┊   ┊        ┊   ┊        └──────────────────────┘                │
             ╲    ╲      ╲  ╲  ║  ╱   ╱   │ │ │ │        ┌ LÉGENDE ─────────────────────┐   │
Tronc                         (##) 40 · +5 % PV          │ (##) pris : à toi             │   │
                               ║                         │ (  ) ouvert : un clic         │   │
                              (##) 35 · +3 % résist.     │ (..) pas encore : il manque…  │   │
                               ║                         │ ( /) fermé : survole…         │   │
                              (##) 30 · +3 % dégâts      │ Flèches · Entrée · Ctrl+S     │   │
▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔║▔▔▔▔▔▔▔▔ sol ▔▔▔▔▔▔▔▔▔▔ └───────────────────────────────┘   │
```

Rendu pièce par pièce :
- **Colonnes** (x local) : Fusion 86 à 282 · Alchimie (bourgeon) 296 à 384 · Mêlée 398 à 520 · Distance 534 à 622 · Tempo 636 à 724 · Terrain 738 à 826 · Soutien 840 à 928.
- **Tronc** : les 3 cernes atteints, bois vivant sur toute la hauteur (sève 8 à 10 px).
- **Racines** : Foudre vivante ; Boue et les 4 Cœurs atteignables (`--osmose-wood-light`, 6 px) ; Glace, Magma, Vapeur et Sable nues (pointillés `--chocolat`).
- **Tige** Foudre → collet : vivante, 10 px. Le collet est nourri (cœur pistache).
- **Charpentières** : vers la Mêlée, vivante ; vers l'Alchimie, le Tempo, le Terrain et le Soutien, atteignables ; vers la Distance, nue.
- **Flèche de la Fusion** : vivante de Foudre jusqu'au cadre. Dans la Fusion, l'axe est vivant jusqu'à la Conversion, puis atteignable jusqu'à Signature et Prisme.
- **Mêlée** : axe vivant jusqu'à Corps à corps ; rameaux de Pied ferme, Poussée lourde et Corps à corps vivants (4 px pistache) ; rameau d'Au plus près atteignable.
- **Nœuds pris** : Foudre, Conversion, Pied ferme, Poussée lourde, Corps à corps, les 3 cernes. **Ouverts** : Boue, les 4 Cœurs, Signature, Prisme, Au plus près, et les bourgeons d'Alchimie, de Tempo, de Terrain et de Soutien. **Fermés** : Transmutation Feu et Air, Glace, Magma, Vapeur, Sable, le bourgeon de Distance.
- Aucun nœud « pas encore » : à 0 point, tout ce que le chemin permet reste ouvert (GD §2.6).
- Rouge : aucun.

---

## 5. Maquette B : héros de 30 qui découvre l'arbre (2 points, rien de pris)

```
 ARBRE DES TALENTS · Sentinelle niv. 30   (2) points à placer · 2 au total        [Outils ▾] [?] [Annuler] [Enregistrer] [×]
════════════════════════════════════════════════════════════════════════════════════════════╤══════════════════════════════
                                                                                            │ 2 POINTS À PLACER
Clés     ───────────────────────────────── (guide #262626) ──────────────────────────────── │ Prochain point : niveau 33
niv. 45                                                                                     │
2 pts                                                                                       │ Commence par une porte.
Étage 2  ─────────────────────────────────────────────────────────────────────────────────  │ Tes deux meilleures
                                                                                            │ caractéristiques, Feu et
          FUSION   ALCHIMIE   MÊLÉE    DISTANCE   TEMPO    TERRAIN   SOUTIEN                │ Air, désignent Foudre.
Étage 1   (..)      (..)      (..)      (..)      (..)      (..)      (..)                  │──────────────────────────────
         la Foudre réactions au contact de loin  rythme   glyphes   alliés                  │ SURVOL
          0/5       0/6       0/4       0/4       0/9       0/10      0/8                   │ (  ) Foudre (porte)
           ┊          ┊         ┊         ┊         ┊         ┊        ┊                    │ ouvert · Feu + Air
           ┊             ┌───────────────────────────────────────────────┐                  │ Réaction : Électrisé.
           ┊             │ 1/3  Choisis une porte : une paire ou un Cœur. │                  │ Ouvre les 6 styles et
           ┊             │ Tes branches pousseront au-dessus. [Suivant]   │                  │ sa branche Fusion.
           ┊             └───────────────────────▽───────────────────────┘                  │ Ferme : Glace, Magma,
           ┊                                     ◆ (vide)                                   │ Vapeur, Sable.
        [conseillé]                                                                         │ Coût : 1 point.
Portes  (( Fo )) (Bo)   (Gl) (Ma)   (Va) (Sa)   ┌─ 1 au choix ─────────┐                    │
        Foudre   Boue   Glace Magma Vapeur Sable │ (Te) (Fe) (Ea) (Ai)  │                    │
       Feu+Air  Ter+Eau ...                      └──────────────────────┘                    │
            │      │     │    │      │    │        │    │    │    │                         │
              ╲     ╲     ╲   ╲   │  ╱   ╱     ╱    ╱              ┌ LÉGENDE ───────────┐    │
Tronc                         (..) 40 · +5 % PV                     │ 4 lignes            │    │
                               ┊                                    │                     │    │
                              (..) 35 · +3 % résist.                │                     │    │
                               ┊                                    │                     │    │
                              (##) 30 · +3 % dégâts                 └─────────────────────┘    │
▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔║▔▔▔▔▔▔▔▔▔▔▔ sol ▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔ │
```

Rendu :
- **Bourgeons** : les 7 en « pas encore » (creux, pointillé `--osmose-locked`, icône `-locked`), avec l'anneau intérieur dans la couleur de leur branche, à pleine opacité : c'est la seule couleur de la moitié haute, qui annonce ce qui poussera. Colonnes réparties à égalité (7 × 88 et 6 écarts de 40).
- **Portes** : les 10 ouvertes (relief neutre, icône claire, liseré des parents, pastilles d'élément). Foudre porte la pastille « conseillé » et la respiration.
- **Bois** : les 10 racines atteignables ; pas de tige ; collet vide ; les 7 charpentières nues ; tronc vivant du sol au cerne 30 seulement, nu au-dessus (désaccord n° 7).
- **Lignes guides** des étages visibles : elles montrent où l'arbre poussera.
- Le seul pistache de l'écran : le badge du compteur, le cerne 30, la pastille « conseillé » et sa respiration. Le regard va du compteur à Foudre.

---

## 6. Ce qui remplace le §3 de `s163-icones-arbre.md`
- Le bois : la sève et l'écorce sont gardées ; « jamais de pointillés » est abandonné (le **nu** est pointillé, pour la lecture sans couleur) ; la branche élaguée est abandonnée (le **fermé** est un aspect du nœud, pas du bois) ; plus de lien de nœud à nœud ni de losange de fourche : le cadre « 1 au choix » du GD les remplace.
- Les aspects : 4 au lieu de 5 ; « trop bas niveau » fusionne dans « pas encore » avec son badge ; les bourgeons « à venir » de 16 px sont supprimés (les tresses `coming:` sortent du canevas).
- Les icônes de talents : une troisième variante `-open` (claire) s'ajoute à la couleur et à `-locked`.
- Le plan : l'anneau pointillé du GD remplace mes badges numérotés.
- La respiration : seulement sur la porte conseillée.
- Le badge de niveau passe en haut à gauche.

## 7. Textes pour le scénariste (FR/EN)
`talent.short.<id>` (noms de 2 × 9 caractères environ, pour les nœuds dont le nom dépasse : Signature appuyée, Transmutation Feu → Foudre (« Feu »), Transmutation Air → Foudre (« Air »), Conversion : Foudre (« Conversion »), Poussée lourde, Au plus près...), `tree.branch.<Branch>.phrase` (« au contact », « de loin », « réactions », « rythme », « glyphes », « alliés », « la Foudre »), `tree.recommended` (« conseillé »), `tree.choice-one` (« 1 au choix »), `tree.counter.*` (compteur, à 0, en plan), `tree.look.*` (les 4 lignes de légende), `tree.gutter.*`.

## 8. Ordre de rendu, aligné sur les étapes du GD
- **É1** : textures des 4 aspects, barre oblique, légende permanente, retrait des jetons rouges et de « à venir », icônes `-open` et `-locked`.
- **É2 et É3** : jetons du bois, `Line2D` du bois en 3 vies, collet, cadres « 1 au choix », gouttière et lignes guides, liseré et pastilles des portes, sol.
- **É4** : bourgeons (anneau, phrase, compteur, « +n »), aperçu de fermeture, focus en anneau.
- **É5** : barre du haut (compteur en badge, Outils, ×), panneau en trois blocs, bulle du tutoriel.
Après É3, je relis un rendu sans fenêtre des deux états A et B, avec la même grille de constats que pour la planche des sorts.
