# S157 : icônes en relief, panneau Menus, thème commun (maquettes)

Sources : `src/assets/tokens.css`, D84, D54 (aucun asset ni mise en page d'Ankama : la capture Wakfu sert de référence de finition, pas de modèle), `projects/rpg/godot/SideBar.cs`, `assets/icons/bar/credits.json`.
Convention : « silhouette » = path game-icons.net 512×512 monochrome. Toutes les coordonnées SVG sont en unités du viewBox 512 sauf mention contraire. À 36 px d'affichage, 1 px = 14,2 unités.

---

## 1. Recette commune d'une icône en relief

### 1.1 Cadre
- `viewBox="0 0 512 512"`, fond transparent, **pas de socle** dans l'icône : le socle est la tuile du bouton (§3), neutre, pour que la couleur reste portée par le dessin.
- La silhouette est placée par `<g transform="translate(40 28) scale(0.84375)">` (432 unités de large) : marge de 40 à gauche et à droite pour le contour, 28 en haut, 52 en bas pour l'ombre. Toutes les couches 1 à 4 utilisent ce même groupe.
- Lumière venant d'en haut à gauche : le dégradé va du haut à gauche vers le bas à droite, l'ombre tombe en bas à droite.

### 1.2 Couches, de bas en haut (coordonnées dans le groupe transformé, donc celles du path d'origine)

| # | Couche | Valeurs exactes |
|---|---|---|
| 1 | Ombre portée | `<g opacity="0.45" transform="translate(12 28)">` contenant le path avec `fill="#000"`, `stroke="#000"`, `stroke-width="48"`, `stroke-linejoin="round"`. Opacité sur le groupe (pas `fill-opacity`) pour éviter la double densité là où trait et remplissage se chevauchent. |
| 2 | Contour | Le path, `fill={outline}`, `stroke={outline}`, `stroke-width="48"`, `stroke-linejoin="round"`, `stroke-linecap="round"`. Placé SOUS le remplissage : seule la moitié extérieure (24 unités, soit 20 après échelle ≈ 1,4 px) dépasse. Les trous de moins de 48 unités se ferment en couleur de contour : voulu, ils se lisent comme des traits sombres. |
| 3 | Remplissage | Le path, `fill="url(#g)"` ; `<linearGradient id="g" gradientUnits="userSpaceOnUse" x1="96" y1="32" x2="416" y2="480">` arrêts : `0` = {light}, `0.5` = {main}, `1` = {dark}. |
| 4 | Reflet | `<clipPath id="c">` = le path ; un `<rect x="0" y="0" width="512" height="512" clip-path="url(#c)" fill="url(#r)">` ; `<linearGradient id="r" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="0" y2="512">` arrêts : `0` = `#fff` opacité `0.45`, `0.22` = `#fff` opacité `0.20`, `0.48` = `#fff` opacité `0`. |
| 5 | Accent (facultatif) | Seconde silhouette dans `<g transform="translate(272 8) scale(0.44)">` (coin haut droit, 225 unités ≈ 16 px ; le coin haut gauche est réservé au badge, le bas droit à la lettre du raccourci). Elle reçoit les couches 1 à 4 avec `stroke-width="64"` (≈ 28 après échelle, 1 px dehors) et ombre `translate(16 36)`, ses propres {light}/{main}/{dark}/{outline}. Dessinée après la couche 4 de la silhouette principale. |

Les IDs `g`, `r`, `c` sont suffixés (`g-main`, `g-acc`...) pour que les deux silhouettes coexistent. ThorVG gère dégradés, `clipPath` et opacité de groupe ; aucun `filter`.

### 1.3 Teintes dérivées (formule, pour le script)
- `light = mix(main, #ffffff, 0.45)` ; `dark = mix(main, #000000, 0.45)` ; `outline = main × 0.20` (chaque canal), sauf valeurs corrigées à la main dans le tableau §2 (marquées *).
- Le script lit ces valeurs dans `assets/icons/bar/palette.json` (nom → main, light, dark, outline, accent) ; `Hues` de `SideBar.cs` disparaît (les icônes sont en couleur, voir §3).

### 1.4 Rendu
- PNG 72×72 (2×) sans fenêtre, import Godot : filtre linéaire, mipmaps activés ; affiché à 36×36 (`icon_max_width = 36`).
- Test du script : chaque SVG produit contient exactement les couches 1-4 (+1-4 de l'accent), aucun `<filter>`, viewBox 512, couleurs égales à `palette.json`.

---

## 2. Les 13 icônes

| Icône | Silhouette (auteur/nom) | main | light | dark | outline | Accent |
|---|---|---|---|---|---|---|
| characteristics | lorc/muscle-up (garde) | `#e8a33d` (= `--osmose-melee`) | `#f2cc94` | `#805a22` | `#2e210c` | aucun |
| spells | delapouite/spell-book (garde) | `#b48cff` (= `--osmose-tempo`) | `#d6c0ff` | `#634d8c` | `#241c33` | delapouite/sparkles, couleurs d'achievements (`#ffd166` / `#ffe6ab` / `#8c7338` / `#332a14`) |
| inventory | lorc/knapsack (garde) | `#d09a5c` | `#e5c7a5` | `#725533` | `#2a1f12` | aucun |
| map | lorc/treasure-map (garde) | **`#52c45a`** (= `--osmose-air` ; était `#7fd06b`, trop proche du pistache des talents) | `#a0dfa4` | `#2d6c32` | `#102712` | aucun |
| journal | lorc/scroll-unfurled (garde) | **`#f0e6cc`** * (parchemin ; était `#f2d36b`, confondu avec le doré des succès) | `#fbf6e8` * | `#b89a6a` * | `#2e2214` * | lorc/quill-ink, `#c9a27f` (= `--chocolat-clair`) / `#e1ccb9` / `#6f5946` / `#282019` |
| jobs | lorc/anvil (garde) | `#a9bccd` | `#d0dae4` | `#5d6771` | `#222629` | lorc/claw-hammer, `#b07a45` (= `--osmose-earth`) / `#d4b699` / `#614326` / `#23180e` |
| contacts | delapouite/three-friends (garde) | **`#ff7eb6`** (= `--osmose-heal` ; était `#6bb8f2`, cédé à la guilde côté bleus) | `#ffb8d7` | `#8c4564` | `#331924` | aucun |
| talents | delapouite/skills (garde) | `#bef374` (= `--pistache`) | `#e6f8c9` (= `--pistache-pale`) * | `#4d7a1c` (= `--bambou`) * | `#1f310b` * | aucun |
| guild | lorc/flying-flag (garde) | **`#3d9cff`** (= `--osmose-water` ; était `#f2876b`, trop proche du rouge du bestiaire) | `#94c9ff` | `#22568c` | `#0c1f33` | aucun |
| bestiary | **lorc/wolf-head** (remplace lorc/fangs-circle : un cercle lu comme un bouton rond à 36 px ; la tête de loup se lit « monstre ») | **`#ef4b4b`** (= `--osmose-fire` ; était `#d9b38c`, confondu avec l'inventaire) | `#f69c9c` | `#832929` | `#300f0f` | aucun |
| achievements | **lorc/trophy** (remplace la coupe dessinée pour Osmose : même recette pour toutes, crédit à ajouter) | `#ffd166` | `#ffe6ab` | `#8c7338` | `#332a14` | aucun |
| minimap | lorc/radar-sweep (garde) | `#4fd1c5` (= `--osmose-alchemy`) | `#9ee6df` | `#2b736c` | `#102a27` | aucun |
| menu | lorc/cog (garde) | `#cfcfcf` | `#e5e5e5` | `#727272` | `#292929` | aucun |

- Roue des teintes résultante : rouge, orange, brun, doré, parchemin, pistache, vert, turquoise, bleu, violet, rose, acier, gris : deux voisines ne se confondent plus.
- `credits.json` : `bestiary` → `lorc/wolf-head`, `achievements` → `lorc/trophy`, ajouter une clé `accents` : `spells` → `delapouite/sparkles`, `journal` → `lorc/quill-ink`, `jobs` → `lorc/claw-hammer` (toutes CC BY 3.0, vérifiées sur game-icons.net le 28 oct. 2026).
- Aucune recoloration par découpe en v1 : sans rendu sous les yeux, une découpe de path se devine. À rouvrir après la relecture des PNG (candidats : hampe de `flying-flag` en chocolat, pied de `trophy`).

---

## 3. Panneau Menus

### 3.1 Schéma (1280×720, coin bas droit, `OffsetRight = OffsetBottom = -12`)

```
 <------------- 168 px ------------->
+------------------------------------+  ^
| [::] MENUS                         |  | 20 (titre)      marge 8 tout autour
|------------------------------------|  | 4
| +------+ +------+ +------+         |  |
| |(3)   | |      | |      |   48×48 |  |
| | ICON | | ICON | | ICON |  gouttière 4
| |     C| |     S| |     I|         |  | 296 px
| +------+ +------+ +------+         |  |
|   ... 5 rangées (13 icônes) ...    |  |
| +------+                           |  |
| | MENU |                           |  |
| +------+                           |  v
+------------------------------------+
 (3) badge haut gauche    C lettre du raccourci bas droite
```
Largeur 8 + 3×48 + 2×4 + 8 = 168 ; hauteur 8 + 20 + 4 + 5×48 + 4×4 + 8 = 296.

### 3.2 Panneau
- Variation de thème `HudPanel` : fond `--osmose-ui-hud` (`#1b1b1cEB`, 92 %), bord 2 px `--chocolat` `#5a3a22`, rayon 10, marges de contenu 8, ombre `#00000080` taille 8 décalage (0, 4).
- Titre : pictogramme « grille » 14×14 (4 carrés de 6×6, rayon 1, écart 2, dessinés en SVG simple, pas game-icons) + « MENUS », 14 px gras, `FontVariation.spacing_glyph = 1`, couleur `--chocolat-clair` `#c9a27f` (6,6:1 sur le fond), aligné à gauche, écart pictogramme-texte 6. Remplace le libellé 12 px centré à 70 % d'opacité.
- Fenêtre réduite (hauteur de viewport < 640) : 4 colonnes, 4 rangées → 212 × 244 ; l'auto-test des rangées reçoit la règle.

### 3.3 Tuile (variation `MenuTile`, Button 48×48, `StyleBoxTexture` d'un SVG 9-patch, recette §4.1)
Tuiles neutres pour toutes les icônes ; `icon_*_color` = `#ffffff` partout (plus de teinte : l'icône porte ses couleurs).

| État | Corps (haut → bas) | Contour | Extra | Icône |
|---|---|---|---|---|
| normal | `#3a3a3d` → `#262628` | 1 px `#0d0d0d` | reflet standard, ombre (0, 2) `#000` 0,45 | opacité 1 |
| survol | `#46464a` → `#2e2e31` | 1 px `--chocolat-clair` `#c9a27f` | idem | `icon_hover_color = #ffffff` ; infobulle après 400 ms |
| ouvert (toggle actif) / survol ouvert | `#3d4f24` → `#25301a` | 2 px `--pistache` `#bef374` | halo : contour supplémentaire 3 px `#bef374` opacité 0,35 hors forme | opacité 1 |
| pressé (bouton menu, instant) | `#262628` → `#3a3a3d` (inversé) | 1 px `#0d0d0d` | pas d'ombre, contenu décalé de +1 px (`content_margin_top` 7 au lieu de 6) | opacité 1 |
| désactivé (futur) | `#2a2a2b` plat | 1 px `#0d0d0d` | pas de reflet | modulate alpha 0,4 |

- Marges de contenu 6 → icône 36 px.
- Badge : 20×20, fond `#bef374`, texte `#5a3a22` 14 px gras (7,8:1), rayon 10, bord 2 px `#1f1f1f`, position (−4, −4) pour déborder le coin.
- Lettre du raccourci : 12 px gras, `#ffffff` à 85 %, contour 3 px `#000000E6`, ancrée bas droite (−3, −1). Exception assumée au minimum de 14 px : indice redondant avec l'infobulle.

---

## 4. Thème commun (`OsmoseTheme`, un seul `Theme` à la racine de l'UI, variations nommées)

### 4.1 Recette du cadre en relief (SVG 9-patch, même script que les icônes)
- Canevas 56×56, forme `rect x=4 y=3 w=48 h=48 rx={r}` ; `texture_margin` 12 sur les 4 côtés ; `expand_margin` gauche 4, droite 4, haut 3, bas 5.
- Couches : (1) ombre : même rect en `y=5`, `#000`, opacité 0,45 ; (2) corps : dégradé vertical {top} → {bottom} ; (3) reflet : `rect x=5 y=4 w=46 h=20 rx={r−1}`, dégradé vertical `#fff` 0,18 → `#fff` 0 ; (4) contour : `stroke={border}`, épaisseur {w}, inséré de w/2 ; (5) halo facultatif : rect agrandi de 2, `stroke` 3, opacité 0,35.
- Utilisé par : `MenuTile`, `ButtonPrimary`, `ButtonSecondary`, barre de progression (remplissage), `GoldBand`. Les surfaces de lecture (panneau, liste, champ, infobulle) restent en `StyleBoxFlat` plat.

### 4.2 Jetons à ajouter à `tokens.css` (bloc « Osmose, l'interface (S157, D84) »)

```css
--osmose-ui-window: #1f1f1f;          /* = --gris-fond, fond des fenêtres */
--osmose-ui-hud: #1b1b1ceb;           /* panneaux posés sur la carte, 92 % */
--osmose-ui-titlebar: #181818;        /* = --gris-titre */
--osmose-ui-frame: #7a5434;           /* bord des fenêtres : chocolat éclairci, visible sur #1f1f1f */
--osmose-ui-frame-dark: #3a2616;      /* liseré sous la barre de titre */
--osmose-ui-raised-top: #3a3a3d;      /* tuiles et boutons secondaires */
--osmose-ui-raised-bottom: #262628;
--osmose-ui-raised-hover-top: #46464a;
--osmose-ui-raised-hover-bottom: #2e2e31;
--osmose-ui-edge: #0d0d0d;            /* contour sombre des éléments en relief */
--osmose-ui-primary-top: #d4f79e;
--osmose-ui-primary-bottom: #a3d95a;
--osmose-ui-primary-edge: #4d7a1c;    /* = --bambou */
--osmose-ui-lit-top: #3d4f24;         /* tuile ouverte, onglet actif, ligne sélectionnée */
--osmose-ui-lit-bottom: #25301a;
--osmose-ui-selected: #33421f;        /* = --osmose-taken-fill */
--osmose-ui-well: #141414;            /* creux : fond de barre, champ, case */
--osmose-ui-gold-top: #ffe6ab;
--osmose-ui-gold: #ffd166;
--osmose-ui-gold-bottom: #c9962e;
--osmose-ui-gold-text: #3a2410;       /* texte sur le doré, 10:1 */
--osmose-ui-danger: #ef4b4b;          /* = --osmose-fire, barre de vie */
```

### 4.3 Composants

| Composant (variation) | Fond | Bord | Rayon | Marges | Texte | États |
|---|---|---|---|---|---|---|
| Panneau de fenêtre `WindowPanel` | `--osmose-ui-window` plat | 2 px `--osmose-ui-frame` (remplace le pistache 1 px actuel) | 8 | contenu 16 ; ombre `#00000080` taille 12, décalage (0, 6) | corps 14 px `#cccccc` (`--gris-texte`, 9,7:1) | — |
| Barre de titre `TitleBar` | `--osmose-ui-titlebar` | bas 2 px `--osmose-ui-frame-dark` ; coins haut 8 | 8 / 8 / 0 / 0 | h 44, côtés 12 | titre 18 px gras `#ffffff` ; pictogramme de la fenêtre 24 px (son icône du §2) à gauche, écart 8 | — |
| Bouton fermer `CloseButton` | transparent | aucun | 6 | cible 44×44, croix 16 px (lorc/cross-mark n'est pas requis : 2 traits SVG 2 px) `#9d9d9d` | — | survol : fond `#3a2424` (`--osmose-losing-fill`), croix `#f08070` ; pressé : fond `#2a1f1f` |
| Bouton principal `ButtonPrimary` | relief `primary-top` → `primary-bottom` | 1 px `--osmose-ui-primary-edge` | 8 | h 44, côtés 20 | 16 px gras `--chocolat` `#5a3a22` (7,8:1 en haut, 5,9:1 en bas) | survol : `#e6f8c9` → `#bef374` ; pressé : inversé + contenu +1 px, sans ombre ; désactivé : `#3a3a3a` plat, texte `#8a8a8a`, sans reflet |
| Bouton secondaire `ButtonSecondary` | relief `raised-top` → `raised-bottom` | 1 px `--osmose-ui-edge` | 8 | h 44, côtés 16 | 16 px `#e0e0e0` | survol : `raised-hover-*`, bord `#c9a27f` ; pressé : inversé + 1 px ; désactivé : texte `#6a6a6a`, sans reflet |
| Onglet `Tab` (TabBar/TabContainer) | inactif `--gris-panneau` `#252526` ; actif `--osmose-ui-window` (fusionne avec le contenu) | actif : haut 3 px `--pistache` ; inactif : bas 1 px `#ffffff1f` | 6 / 6 / 0 / 0 | h 44, côtés 16, écart entre onglets 2 | 14 px ; inactif `#9d9d9d` (5,6:1), survol `#cccccc` sur `#2d2d2d`, actif `#ffffff` gras | désactivé : `#5c5c5c` |
| Barre de progression `ProgressBar` | creux `--osmose-ui-well` | 1 px `#000000` | 6 (h 16) / 4 (h 8, compacte en liste) | — | valeur 14 px gras `#ffffff`, contour 3 px `#000000` | remplissage relief §4.1 : progrès `#d4f79e`→`#a3d95a` ; vie `#f69c9c`→`#c23a3a` ; récompense `--osmose-ui-gold-top`→`gold-bottom`. Pas de reflet si h < 12 |
| Champ de saisie `LineEdit` | `--osmose-ui-well` | 1 px `#ffffff2e` | 6 | h 44, côtés 12 | 16 px `#e0e0e0` ; indice `#9d9d9d` ; curseur `#bef374` ; sélection `#bef3744d` | focus : bord 2 px `#bef374` ; désactivé : texte `#6a6a6a`, bord `#ffffff14` |
| Case à cocher `CheckBox` | case 20×20 `--osmose-ui-well` | 2 px `#9d9d9d` | 4 | ligne h 44, écart case-texte 8 | 14 px `#cccccc` | survol : bord `#e0e0e0` ; cochée : fond `#bef374`, coche 3 px `#5a3a22` ; désactivée : alpha 0,4 |
| Ligne de liste `ListRow` | normale transparente, une sur deux `#ffffff08` | séparateur bas 1 px `#ffffff0f` | 0 | h 44 (min), côtés 12, icône 32 + écart 8 | 14 px `#cccccc`, valeur à droite `#ffffff` | survol `--gris-survol` `#2d2d2d` ; sélectionnée `--osmose-ui-selected`, bord gauche 3 px `#bef374`, texte `#ffffff` ; désactivée texte `#6a6a6a` |
| Infobulle `TooltipPanel` | `#181818f7` | 1 px `--chocolat-clair` `#c9a27f` | 6 | 8 / 12 ; largeur max 320 ; ombre `#00000080` 8, (0, 4) | titre 16 px gras `#ffffff` ; corps 14 px `#cccccc` ; coût/valeur `--osmose-cost` `#f2d36b` | délai 400 ms |
| Barre de défilement `VScrollBar` | piste `#ffffff0a`, largeur 12 | — | 4 | poignée 8 px de large, marge 2 | — | poignée `#4a4a4a`, survol `#6a6a6a`, glissée `#bef374` |
| Bandeau de points doré `GoldBand` | relief `gold-top` → `--osmose-ui-gold` (0,5) → `gold-bottom` | 2 px `--chocolat` | 4, extrémités en queue d'aronde (encoche triangulaire 8 px dans le 9-patch, canevas 64×40) | h 32, côtés 20 | 16 px gras `--osmose-ui-gold-text` ; pictogramme lorc/trophy 20 px à gauche (palette du §2) | lecture seule ; « +N » qui vient d'être gagné : texte `#ffffff` contour 3 px `#5a3a22` au-dessus, 1,2 s |

Règles transverses : grille de 4 px ; corps minimal 14 px (seule exception : la lettre de raccourci, §3.3) ; pistache jamais en texte (accent : bords, coches, remplissages, curseur) ; texte sur pistache ou doré toujours chocolat ; chaque variation a son test xUnit (couleurs = tokens, hauteur ≥ 44 pour les cibles).

### 4.4 Textes à faire écrire (scénariste, FR/EN)
- `bar.title` : reste « Menus » / « Menus » (affiché en capitales par le code).
- Aucune nouvelle clé : les infobulles existantes (`bar.tip`) suffisent.
