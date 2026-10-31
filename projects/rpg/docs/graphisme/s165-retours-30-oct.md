# S165-S179 : retours graphiques du 30 oct. (1.0.157), maquettes et chiffrage

Demande de Charles (30 oct., après la 1.0.157, 5 captures), **prioritaire, avant les spécialisations** : icônes des recettes, bestiaire, icônes du livre des quêtes, carte du monde, monde moins cubique, suivi de quête masquable, panneau Menus repliable. L'arbre des talents (retour 8) est traité par le game designer et n'est pas chiffré ici.

Sources : `src/assets/tokens.css`, D54 (rien d'Ankama), D84, D86, `docs/graphisme/s157-maquettes.md` (recette du relief §1, panneau Menus §3, thème §4), `docs/graphisme/s163-icones-arbre.md` (tuiles de sorts, métiers), mémoire « direction artistique » (Kenney gardé, cel shading refusé), `godot/ItemIcon.cs`, `src/Rpg.Client/ItemStyle.cs`, `tools/icons_gen.py`, `godot/JobsPanel.cs`, `godot/BestiaryPanel.cs`, `godot/Main.Bestiary.cs`, `src/Rpg.Client/BestiaryPage.cs`, `godot/JournalPanel.cs`, `godot/QuestKindIcon.cs`, `src/Rpg.Client/QuestBook.cs`, `godot/WorldMapPanel.cs`, `src/Rpg.Client/WorldPicture.cs`, `godot/TownView.cs`, `tools/world_gen.py`, `godot/QuestTracker.cs`, `godot/SideBar.cs`.
Toutes les silhouettes nommées ont été vérifiées sur game-icons.net le 30 oct. 2026 (CC BY 3.0, à créditer).

Note de vocabulaire : le panneau « Menus » du retour 7 est `SideBar.cs`. `MenuPanel.cs` est le menu Échap (son, serveur, quitter) : il n'est pas touché.

Convention de chiffrage : stories de 1 à 4 points, chacune avec ses tests xUnit (règles pures dans `src/Rpg.Client`, scripts testés comme S157/S163) et un pas d'auto-test client ; sprints de 4 à 6 points.

---

## 0. Ce qui revient partout

- **Une seule recette de relief** (s157 §1, s163 §1-2) : silhouette game-icons, couches ombre / contour / dégradé / reflet, `-locked` à plat. Tout nouvel outil importe `layers` et `mix` de `tools/ui_art.py` ; pas de `<filter>` (ThorVG).
- **Plus de dessin d'icône par le code** : `ItemIcon` et `QuestKindIcon` ne dessinent plus de formes, ils posent une texture puis les anneaux d'état du thème (survol `#c9a27f`, sélectionné `#bef374` + halo, s163 §1.6).
- **Plus d'opacité pour dire « inactif »** (`Modulate 0.35`, `0.6`) : variante `-locked` et couleur de texte `--osmose-ui-text-dim` `#9d9d9d`.
- **Plus de pistache en texte** (objectifs atteints du suivi et du journal, noms connus du bestiaire, en-têtes de zone) : pistache = coche, bord, remplissage ; le texte passe en `#e0e0e0` / `#cccccc` / `#9d9d9d`.
- **Les jaunes codés en dur** (`#ffd85a`, `#f0c060`) passent au jeton `--osmose-cost` `#f2d36b` (indication « où aller », « à rendre »).
- Les fenêtres refaites prennent `WindowPanel` + `TitleBar` (titre 18 px, icône 24 px, `CloseButton` 44) au lieu de `InventoryPanel.Background()` et d'un `Label` 20-24 px.

---

## 1. Retour 6 : le suivi de quête affiché ou masqué

### 1.1 Constat
`QuestTracker` : un `PanelContainer` à (16, 96), 320 px, fond `#1f1f1f` à 72 %, sans bord, `MouseFilter = Ignore` partout (aucune prise possible) ; objectifs atteints en texte pistache, « Où » en `#ffd85a`. On ne peut que changer de quête suivie dans le journal.

### 1.2 Maquette (déplié, 320 × hauteur du contenu)
```
 <------------------- 320 ------------------->
+---------------------------------------------+
| [jr] QUÊTE SUIVIE                    [ v ]  |  en-tête h 44 (bouton plein-largeur)
|---------------------------------------------|
| Le chant des grenouilles                    |  16 px gras #e0e0e0
| (v) Écouter les grenouilles  3/3            |  coche 16 px + texte #9d9d9d (fait, en retrait)
| ( ) Parler à Ondine                         |  rond 16 px + texte #e0e0e0 (à faire)
| [o] Où : Lac des Joncs                      |  14 px --osmose-cost #f2d36b
+---------------------------------------------+
```
Replié (le panneau se réduit à son en-tête, même coin haut gauche) :
```
+--------------------------------+
| [jr] QUÊTE SUIVIE   1/2  [ ^ ] |  h 44, largeur au contenu (environ 220)
+--------------------------------+
```

### 1.3 Spécification
- Panneau : variation `HudPanel` (celle des Menus : `--osmose-ui-hud`, bord 2 px `--chocolat`, rayon 10, marges 8 sauf en haut 0).
- En-tête : `Button` plat (nouvelle variation `HudHeader`, h 44, côtés 8), seul élément qui arrête la souris (`MouseFilter.Stop`) ; le corps reste `Ignore`, les clics passent toujours à la carte. Contenu : icône `bar/journal.svg` 20 px, « QUÊTE SUIVIE » 14 px gras capitales `--chocolat-clair` `#c9a27f` (`spacing_glyph 1`, comme MENUS), puis à droite le chevron 16 px (2 traits SVG de 2 px, `#c9a27f`).
- Survol de l'en-tête : fond `--osmose-ui-hover` `#2d2d2d`, chevron `#e0e0e0`. Pressé : contenu +1 px. Infobulle : « Masquer le suivi » / « Afficher le suivi ».
- Replié : à la place du détail, le compte des objectifs atteints « 1/2 » en 14 px `#cccccc` : la seule information utile quand on ne lit pas.
- Objectifs : coche `delapouite/check-mark` en relief (tons pistache, palette `talents` de s157 : c'est « validé ») ; à faire, un rond creux 16 px (cercle `stroke` 2 px `#9d9d9d`, SVG simple).
- L'état replié est **mémorisé** dans le fichier de réglages du client (comme le son et la grille de la carte) ; suivre une autre quête ne le change pas.
- Aucune quête suivie : rien (comme aujourd'hui).
- 1280 × 720 et fenêtre réduite : inchangé (320 px max) ; replié, il ne gêne plus la vue en bas à gauche des grandes cartes.

### 1.4 Tests
- xUnit : `TrackerState` (pur, `Rpg.Client`) : `Folded` lu et écrit dans les réglages, valeur par défaut déplié ; `Summary(quest)` = « atteints/total ».
- Auto-test : suivre une quête, replier (le détail disparaît, l'en-tête montre « 1/2 »), recharger les réglages (toujours replié), déplier.

**Chiffrage : 1 point.**

---

## 2. Retour 7 : le panneau Menus repliable

### 2.1 Constat
`SideBar` (s157 §3) : 168 × 296 en bas à droite, titre « MENUS » de 20 px non cliquable. Rien ne permet de le réduire.

### 2.2 Maquette
Déplié (168 × 312) :
```
 <------------- 168 ------------->
+--------------------------------+
| [::] MENUS                [ v ]|  en-tête h 44, cliquable sur toute la largeur
| +------+ +------+ +------+     |  4
| | ICON | | ICON | | ICON |     |  5 rangées de 48, gouttière 4
|   ...                          |
+--------------------------------+  8
```
Replié (168 × 44, ancré au même coin bas droit) :
```
+--------------------------------+
| [::] MENUS          (3)   [ ^ ]|
+--------------------------------+
```

### 2.3 Spécification
- L'en-tête devient le bouton `HudHeader` du §1.3 (h 44 au lieu de 20 : la cible tactile passe enfin à 44) ; marge haute du panneau 0. Hauteur dépliée 44 + 4 + 5 × 48 + 4 × 4 + 8 = 312.
- Chevron « v » (replier vers le bas : le panneau est ancré en bas) / « ^ » (déplier). Infobulles « Replier les menus » / « Déplier les menus ».
- **Badges conservés replié** : la somme des badges des tuiles (aujourd'hui les points de talent) s'affiche sur l'en-tête, badge pistache 20 × 20 au texte chocolat (s157 §3.3), entre le titre et le chevron ; absent à 0. On ne perd pas l'information en repliant.
- Les raccourcis clavier marchent replié ; une fenêtre ouverte replié ne déplie pas le panneau.
- État mémorisé dans les réglages du client.
- Fenêtre réduite (4 colonnes, 212 px) : même en-tête, largeur 212.

### 2.4 Tests
- xUnit : `SideBarLayout` (pur) : hauteur dépliée 312 / repliée 44, en 3 et 4 colonnes ; somme des badges.
- Auto-test : replier (grille cachée, `Rows` inchangé), donner 3 points de talent (badge « 3 » sur l'en-tête), ouvrir l'inventaire par sa touche replié, déplier.

**Chiffrage : 1 point.**

---

## 3. Retour 3 : les icônes du livre des quêtes

### 3.1 Constat
`QuestKindIcon` dessine un disque plein de la couleur du type et un signe sombre à la main (étoile, « ! », marteau, porte, livre) : plat, illisible à 22 px, sans parenté avec les icônes en relief des menus, sorts et métiers. Les filtres de types s'éteignent par `Modulate 0.35`. Les objectifs sont des « ✓ » / « – » typographiques en pistache ; les récompenses des lignes de texte « – 120 XP ». Le type Premiers pas est pistache, couleur réservée à « validé / sélectionné ».

### 3.2 Les cinq types (recette des menus, s157 §1, silhouette colorée sans tuile ; teintes dérivées par la formule s157 §1.3)
| Type | Silhouette | main | Raison |
|---|---|---|---|
| Histoire (`Main`) | lorc/laurel-crown | `#ffd166` (palette des succès) | la quête principale = la récompense, l'or |
| Annexe (`Side`) | delapouite/chat-bubble | `#6bb8f2` | une demande d'habitant ; garde le bleu actuel |
| Métier (`Trade`) | lorc/anvil-impact | `#a9bccd` (acier du menu Métiers) | parenté avec l'enclume du menu |
| Donjon (`Dungeon`) | delapouite/dungeon-gate | palette de `relief/dungeons` (succès) | même porte que la catégorie Donjons des succès |
| Premiers pas (`Tutorial`) | lorc/open-book | `#4fd1c5` (`--osmose-alchemy`) | quitte le pistache |

Fichiers `assets/icons/relief/quest-<kind>.svg` et `-locked.svg`, générés par `ui_art.py` (entrées `quest-*` de `relief/palette.json`, crédits dans `relief/credits.json`).
`QuestKindIcon` : `DrawTextureRect` de la texture, rien d'autre ; `Colour(kind)` reste pour le sous-titre de la fiche (texte coloré sur `#1f1f1f`, contraste ≥ 4,5:1 pour les cinq).

### 3.3 Filtres et liste
- Boutons de type : tuiles `MenuTile` 44 × 44, icône 28 ; gardé = tuile allumée (`lit`, bord pistache), écarté = tuile normale + icône `-locked`. Plus d'opacité.
- Ligne de quête : `ListRow` (h 44, icône 24, nom 14 px `#cccccc`, niveau à droite `#9d9d9d`) ; sélectionnée : style `ListRow` sélectionné ; « à rendre » : nom `#e0e0e0` + pastille `--osmose-cost` 8 px à droite du nom, plus de nom tout en jaune.
- En-tête de zone : 14 px gras capitales `--chocolat-clair`, flèche de pliage `#c9a27f` (fin du pistache 17 px).

### 3.4 La fiche (colonne droite, 528 px utiles)
```
+------------------------------------------------------------+
| [ICON 40]  Le chant des grenouilles                        |  22 px gras #ffffff
|            Annexe · Niv. 12 · Lac des Joncs                |  14 px, couleur du type
|            Donnée par Ondine                               |  14 px #9d9d9d
| [o] Où : Lac des Joncs                                     |  14 px #f2d36b
|                                                            |
| Texte de la quête, 14 px #cccccc, sur 2 à 6 lignes...      |
|                                                            |
| OBJECTIFS                                                  |  14 px gras capitales #c9a27f
| (v) Écouter les grenouilles                3/3             |  coche relief, texte #9d9d9d
| ( ) Parler à Ondine                        0/1             |  rond creux, texte #e0e0e0
|                                                            |
| RÉCOMPENSES                                                |
| +----+ +----+ +----+                                       |
| |XP  | |ÉCL | |PAIN|   tuiles 48 (ItemIcon pour les objets) |
| +----+ +----+ +----+                                       |
|  120     20    ×2       14 px gras #e0e0e0 sous la tuile   |
|                                                            |
|            [ Suivre ]   [ Masquer ]                        |  ButtonPrimary / ButtonSecondary
+------------------------------------------------------------+
```
- Icônes de récompense : XP = delapouite/upgrade, palette or (`#ffd166`) ; Éclats = lorc/crystal-bars, main `#8fe0f5` ; objets = `ItemIcon` (§4) avec leur carte en infobulle.
- « Suivre » en bouton principal (le point focal de la fiche) ; « Suivie » = principal désactivé ; « Masquer » secondaire.

### 3.5 Tests
- Script : 5 types × 2 fichiers, crédits, palette ; aucun `<filter>`.
- xUnit : `JournalQuest.Rewards` devient une liste typée (`Reward(Kind: Xp|Shards|Item, Amount, ItemId?)`) au lieu de chaînes, avec ses tests ; `QuestKindIcon.Colour` sans pistache.
- Auto-test : journal de Plouf, filtrer « Métier » (tuile allumée, les autres `-locked`), ouvrir une quête, compter 3 tuiles de récompense.

**Chiffrage : 2 points (icônes, filtres, liste) + 2 points (fiche : objectifs, récompenses en tuiles, boutons ; après le §4).**

---

## 4. Retour 1 : les icônes des recettes (et de tous les objets)

### 4.1 Constat
Les recettes montrent des `ItemIcon` : la silhouette blanche de `assets/icons/items/` (107 dessins pour 211 objets) **teinte d'une seule couleur**, une copie noire décalée en guise d'ombre, sur un carré plat `#2b2b2b` bordé de la provenance. Trois défauts :
1. **Plat** : à côté des sorts et des métiers en relief depuis D86, les objets ont l'air d'un autre jeu.
2. **Tous pareils** : la couleur vient de la caractéristique la plus forte (`ItemStyle.Colour`), si bien que la liste d'un forgeron aligne des boucliers identiques de la même couleur ; les ressources sans caractéristique sont toutes grises `#bdbdbd`. Le dessin est partagé par type (`delapouite/viking-shield` pour quatre boucliers, `delapouite/ring` pour quatre anneaux...).
3. **Les glyphes de secours** (anneau, cape, potion... dessinés en `DrawArc` / `DrawColoredPolygon`) et le compte en `s × 0,24` (8 px sur une tuile de 34) sont illisibles.

### 4.2 Principe
**Même recette que les tuiles de sorts (s163 §1.2), mais la tuile est neutre et la silhouette porte la matière.** La couleur d'un objet dit de quoi il est fait (cuivre, if, cuir, gemme), pas son élément ; l'élément devient une pastille ; la provenance reste le liseré.

### 4.3 La tuile (viewBox 512, outil `tools/item_art.py` qui importe `layers` et `mix`)
| # | Couche | Valeurs |
|---|---|---|
| T1 | Ombre portée | celle des sorts (`y=36`, `#000` 0,45) |
| T2 | Contour | `rx=80`, `#0d0d0d` |
| T3 | Liseré | 20 unités, couleur de la **provenance** (§4.5) ; Chef : dégradé `--osmose-ui-gold-top` → `--osmose-ui-gold-bottom` |
| T4 | Corps | dégradé diagonal `--osmose-ui-raised-top` `#3a3a3d` → `#1f1f21` (`mix(#262628, #000, 0.2)`) : neutre, toujours plus sombre que la silhouette |
| T5 | Reflet | celui des sorts |
| T6 | Silhouette | couches des menus (s157 §1.2), `stroke-width 44`, teintes de la **matière** (§4.4), même placement que les sorts (`translate(106 96) scale(0.586)`) |
| T7 | Pastille d'élément (équipement seulement) | haut droit, cercle centre (428, 84) r 44, dégradé de l'élément (`mix(main, #fff, .45)` → `mix(main, #000, .45)`), bord `#0d0d0d` 12, reflet comme la pastille d'origine des sorts ; élément = caractéristique la plus forte (règle actuelle de `ItemStyle.Colour`, déplacée ici) ; Vitalité = `--osmose-heal` |

Le coin bas droit reste libre pour le compte, le coin haut gauche pour un futur badge.
Variante `-locked` (objet non portable maintenant, case vide) : plate, comme les sorts (corps `#141414`, silhouette `#7a7a7a`, liseré `#4c4c4c`, pastille aplatie).
Cases vides de l'équipement : `slot-<emplacement>.svg` (13 fichiers), la silhouette type de l'emplacement en `#4c4c4c` à plat dans un creux `--osmose-ui-well`. **Les glyphes codés de `ItemIcon` disparaissent** (et l'énumération `Glyph` avec eux, remplacée par l'emplacement).

### 4.4 La matière (couleur de la silhouette)
`items/palette.json` : chaque objet → `main` (les autres teintes par la formule s157 §1.3), calculé par une règle puis corrigé à la main :
| Origine de l'objet | Règle | Exemples de `main` |
|---|---|---|
| Ressource de récolte | la matière du métier, nuancée par espèce | bois : chêne `#c08a50`, if `#8a5a36`, aulne `#d4a46a`, bois noir `#5e4a3a` (contour éclairci) ; minerai : cuivre `#d0804a`, fer `#a9bccd`, argent `#d8dde3`, or `#ffd166` ; poisson `#6bb8f2` / `#c7d3dc` ; céréales `#e3c27a` ; plantes `#52c45a` / fleurs selon leur nom |
| Objet fabriqué | la matière de son **premier ingrédient** (la recette le donne) | Bouclier de noisetier : bois clair ; Coutelas trempé : fer |
| Butin de monstre | sa nature | os et crocs `#e8dcc0`, peau et cuir `#a0522d`, gelée `#7fd0b0`, carapace `#c25a3a`, plume `#f0e6cc` |
| Panoplie | la teinte de la famille qui la donne (une par panoplie, 10) | loups `#9aa3ad`, cerfs `#b07a45`... (fixées sur la planche) |
| Consommable | son effet | soin `--osmose-heal`, nourriture `#f2cc94` |
| Objet de quête | parchemin `#f0e6cc` | |

Interdits dans la palette : le pistache (sélection) et une couleur d'élément pure (réservée à la pastille).

### 4.5 La provenance (liseré)
| Provenance | Aujourd'hui | Proposé | Pourquoi |
|---|---|---|---|
| Commune | `#7a7a7a` | `#5c5c5c` | plus discret |
| Artisan | `#8a5a36` | `--chocolat-clair` `#c9a27f` | lisible sur le contour sombre |
| Panoplie | `#bef374` | `--osmose-tempo` `#b48cff` | **le pistache est l'anneau de sélection** (s163 §1.6) : une pièce de panoplie sélectionnée serait illisible (question Q11) |
| Chef | `#e0b84a` | dégradé or (`gold-top` → `gold-bottom`) | récompense |

### 4.6 La ligne de recette (`JobsPanel`, colonne droite)
```
+----------------------------------------------------------------------------+
| +------+  Bouclier de noisetier                 Niv. 20 · 140 XP           |  h 72
| | 48   |  +----+ +----+ +----+                                [Fabriquer]  |
| +------+  | 40 | | 40 | | 40 |                                [Tout (3)]   |
|           +----+ +----+ +----+                                             |
|            3/2    1/1    0/4   <- 14 px, #e0e0e0 ; manquant --osmose-losing |
+----------------------------------------------------------------------------+
```
- Ingrédients en tuiles de 40 (au lieu de 34), le compte « possédé/requis » **sous** la tuile en 14 px ; le nombre requis dans le coin de la tuile disparaît (doublon).
- Compte d'une pile (inventaire, butin) : 14 px gras blanc, contour 3 px noir, coin bas droit, dès 40 px ; en dessous de 40 px, pas de compte sur la tuile.
- Manquant : texte `--osmose-losing` `#f08070` (le rouge brut actuel devient le corail de la charte).

### 4.7 Dédoublonnage des dessins
Règle testée : **dans la liste d'un même métier, deux recettes ne partagent jamais le couple (silhouette, matière)**, et dans tout le jeu, deux objets du même emplacement et du même niveau non plus. Là où la matière ne suffit pas, nouvelle silhouette game-icons (choisie et vérifiée au sprint, sur la liste produite par le test ; nouveaux auteurs crédités dans `items/credits.json`).

### 4.8 Implémentation et tests
- `tools/item_art.py` (après `icons_gen.py`, qui garde les silhouettes blanches dans `items/`) : 211 × 2 fichiers `assets/icons/items/tiles/<id>.svg` et `-locked.svg`, 13 `slot-*.svg` ; import à 128 px avec mipmaps.
- `ItemIcon._Draw` : la texture de l'objet (ou `-locked` si `Faint`), le compte, les anneaux d'état ; plus de `StyleBoxFlat`.
- Script : chaque objet a ses deux fichiers ; aucun `<filter>` ; liseré = provenance ; pastille seulement sur l'équipement à caractéristique ; aucune couleur de palette égale au pistache ou à un élément pur.
- xUnit : `ItemStyle.Material(item, data)` (règle du §4.4), `ItemStyle.Frame` aux nouveaux jetons, `ItemStyle.Element(item)` ; le test de dédoublonnage du §4.7.
- Planche de contrôle (toutes les tuiles à 40, 48 et 64 px, rendue sans fenêtre) relue par moi avant Charles.
- Auto-test : métiers de Plouf, recettes du forgeron (tuiles texturées, `ItemIcon.Drawn` > 0), inventaire, une case d'équipement vide.

**Chiffrage : 4 points (recette, 422 tuiles, `ItemIcon`, provenance) + 2 points (ligne de recette, dédoublonnage et nouvelles silhouettes).**

---

## 5. Retour 4 : la carte du monde

### 5.1 Constat
`WorldMapPanel` montre `WorldPicture` : chaque case de chaque carte en 4 × 4 pixels (filtre `Nearest`), herbe en damier, arbres en disques, le reste du monde en gris d'interface `#181818`, une grille blanche à 22 % sur les bords de cartes, un anneau pistache (joueur), des carrés chocolat (donjons), un anneau jaune (quête). C'est un plan cadastral : aucune région, aucune côte, aucun nom de zone ; au dézoom, une bouillie verte en damier.

### 5.2 Principe : deux niveaux de lecture
**Au dézoom, un atlas illustré ; au zoom, le plan des cases d'aujourd'hui, en fondu.** Référence de style (et de style seulement) : les cartes illustrées de Dofus et Wakfu (régions colorées, contours, mer, repères). Rien n'est repris : ni asset, ni mise en page, ni pictogramme (D54).

### 5.3 Maquette (fenêtre 960 × 600, centrée)
```
 <--------------------------------------- 960 ---------------------------------------->
+--------------------------------------------------------------------------------------+
| [map] Carte du monde                                                          [ X ]  |  TitleBar 44
|--------------------------------------------------------------------------------------|
|  ~ ~    ~        ~         ~                ~              +---------------------+   |
|     ___________________                                    | LÉGENDE         [v] |   |
|  ~ /  LAC DES JONCS    \___       ~                        | [x] Donjons         |   |
|   /    Niv. 20-30   (o)    \_______                        | [x] Pierres de pass.|   |
|  |  ~~~~ lac ~~~~      ____|  BOIS  \  ~                   | [x] Ateliers        |   |
|  |__  MOISIS __ ______/ CLAIRVAL [v]|_                     | [x] Quête suivie    |   |
|  /  ^^ Niv. ^^ |  ^ ^ ^  Niv. 1-10   [*] CLAIRVAL |   ~    | [ ] Grille          |   |
|  \__ 10-20 ____|___ ^ ^ ^ ____________ (village) _/        +---------------------+   |
|  ~  \__________/ CHAMPS  \_/ QUATRE-CHEMINS \_  ~                                    |
|   ~     ~       \ Niv. 15 ___ [D] ______/  ~                                          |
|        ~         \_______/ ANCÊTRES \___/       ~                                     |
|  ~                        Niv. 40-50                                    [ - ][ + ]  |
|--------------------------------------------------------------------------------------|
| Bois de Clairval · carte 2,3 · Niv. 1-10 · Vous êtes ici                             |  barre d'état h 32
+--------------------------------------------------------------------------------------+
```
(Le schéma montre la logique, pas le tracé : les formes viennent des données.)

Zones : vue 928 × 492 (960 − 2 × 16 ; 600 − 44 − 16 − 32 − 16) ; légende flottante 200 px en haut à droite de la vue (repliable comme le §1) ; boutons de zoom 44 × 44 en bas à droite (`ButtonSecondary`) ; barre d'état 14 px `#cccccc`.

### 5.4 L'atlas (`WorldAtlas`, pur, dans `Rpg.Client`, à côté de `WorldPicture`)
Calculé une fois depuis les données (cartes, zones, `MapAt`, cases), sorti en formes (polygones, polylignes, points) que Godot dessine (`Polygon2D`, `Line2D`, textures) ; déterministe (graine fixe).
- **Régions** : l'union des cartes d'une zone, échantillonnée à 1 point toutes les 4 cases, contour extrait (marching squares) puis **ondulé** par un bruit 1D (amplitude ≤ 3 cases, lissage Chaikin 2 passes) : plus de rectangles. L'ondulation ne fait jamais sortir une carte de sa zone (test).
- **Couleurs de biome** (nouveaux jetons `--osmose-map-*`, à ajouter à `tokens.css`, dérivés de `ZoneStyle.Sky` mais désaturés et éclaircis pour une lecture de carte) :
  | Zone | Jeton | Valeur |
  |---|---|---|
  | Bois de Clairval | `--osmose-map-woods` | `#6fae5a` |
  | Bois moisis | `--osmose-map-mouldy` | `#8f9862` |
  | Lac des Joncs | `--osmose-map-lake` | `#72bd7c` |
  | Champs | `--osmose-map-fields` | `#c4c76a` |
  | Forêt des Ancêtres | `--osmose-map-ancestors` | `#4f7f48` |
  | Clairval, Quatre-Chemins (cités) | `--osmose-map-town` | `#d9c39a` (pavé clair) |
  | Mer | `--osmose-map-sea` → `--osmose-map-sea-deep` | `#3f86c4` → `#2a5f94` |
  | Plage | `--osmose-map-shore` | `#e8d9a8` |
- **Texture de biome** : un motif discret par style (`StyleBoxTexture` répétée, 64 × 64, opacité 0,12) : touffes pour les bois, points pour les champs, vaguelettes pour le lac.
- **Forêts, eau, routes** tirées des cases : la part de `T` d'une carte donne un nombre de **tampons d'arbres** (petit dessin SVG en relief, 3 variantes, 12 à 18 px) dispersés sur la carte ; les `~` agrégés deviennent des plans d'eau lissés de la couleur de la mer ; les `=` des pistes en pointillés `#e6e1d2` 2 px.
- **Contours** : entre zones, 3 px `--osmose-ui-frame-dark` `#3a2616` doublé à l'intérieur d'un liseré 1 px `#ffffff33` ; côte 2 px `#1d4a75` puis plage 4 px.
- **Mer** : tout ce qui n'est pas une carte (fini le gris `#181818`), dégradé radial du large (`sea-deep`) vers la côte (`sea`), anneau de haut-fond 12 px `mix(sea, #fff, .25)` le long de la côte, quelques vaguelettes (arcs 2 px `#ffffff40`) semées loin des côtes. La terre déborde des cartes d'un quart de carte pour que la côte n'épouse pas les bords des cartes.
- **Îlots décoratifs** : facultatifs (question Q8).

### 5.5 Repères (icônes en relief de la recette des menus, 28 px, avec ombre)
| Repère | Silhouette | main | Zoom |
|---|---|---|---|
| Cité (Clairval, Quatre-Chemins) | delapouite/village | `#d09a5c` | toujours |
| Entrée de donjon | delapouite/dungeon-gate | palette `relief/dungeons` | toujours |
| Pierre de passage | lorc/magic-portal | `#4fd1c5` | toujours |
| Atelier | lorc/anvil-impact | `#a9bccd` | zoom ≥ 2 |
| Joueur | delapouite/position-marker | pistache (palette `talents` ; c'est « vous ») + anneau pistache qui respire | toujours, taille fixe 32 |
| Quête suivie | anneau 4 px `--osmose-cost` + lorc/laurel-crown 20 px si quête d'histoire | `#f2d36b` | toujours |

- **Noms de zone** : 16 px gras capitales `#f0e6cc`, contour 4 px `#1b1b1b`, `spacing_glyph 2`, au centre de gravité de la région ; dessous « Niv. a-b » 14 px `#e0e0e0`. Visibles au dézoom, ils s'effacent au zoom ≥ 2 au profit du nom de la carte survolée.
- Survol d'un repère : infobulle du thème (nom, niveau, « Donjon : … »). Survol du fond : barre d'état (texte actuel de `HoverAt`, inchangé).

### 5.6 Zoom et fondu
- Zoom < 1,5 : atlas seul. 1,5 à 2,5 : le plan des cases (`WorldPicture`, filtre linéaire au lieu de `Nearest`) apparaît par-dessus en fondu (opacité 0 → 1). ≥ 2,5 : plan seul, contours de zones gardés, grille optionnelle.
- La case à cocher « Grille » passe dans la légende ; ses choix restent mémorisés.
- Ouverture : centrée sur le joueur au zoom 1,2 (on voit sa zone et ses voisines), et non plus au zoom 1,5 sur un damier.

### 5.7 Tests
- xUnit `WorldAtlasTests` : le centre de chaque carte est dans le polygone de sa zone ; aucune côte ne coupe une carte ; chaque contour est fermé ; deux appels donnent les mêmes formes ; chaque zone a un nom placé dans sa région ; un repère par donjon, par pierre de passage, par atelier.
- Auto-test : ouvrir la carte, dézoomer au minimum (atlas visible, plan à opacité 0), zoomer à 3 (plan à 1), décocher Donjons (repères cachés), le texte de survol de la carte du joueur inchangé.

**Chiffrage : 4 points (atlas : régions, couleurs, contours, mer, forêts, eau, routes) + 3 points (repères, noms, légende, zoom à deux niveaux, fenêtre au thème).**

---

## 6. Retour 2 : le bestiaire (mise en page)

### 6.1 Constat
`BestiaryPanel` : 760 × 600, une colonne de boutons « Loups (1/5) » sous des noms de zone à 60 %, puis à droite des `Label` les uns sous les autres ; un monstre inconnu affiche « ??? » et « Jamais combattu : une silhouette » (le texte promet une silhouette qui n'est jamais dessinée), répété pour chaque inconnu de la famille. Noms connus en pistache. Aucune icône, aucune image, aucune progression visible.

Le **contenu** (ce qu'on apprend, à quels paliers, le butin et ses pourcentages) est chiffré par le game designer ; la mise en page ci-dessous l'accueille en sections, quelles qu'en soient les règles. Elle reprend l'encyclopédie voulue le 29 oct. (filtres à gauche, tableau, loupe vers la zone), dont le bestiaire est le premier onglet.

### 6.2 Maquette (fenêtre 960 × 600 ; trois colonnes : filtres 240, tableau 384, fiche 272)
```
 16 <---- 240 ----> 16 <------------ 384 ------------> 16 <------- 272 -------> 16
+-------------------------------------------------------------------------------------+
| [wolf] Bestiaire          [=========------]  23 / 50 connus                  [ X ]  |  TitleBar 44
|-------------------------------------------------------------------------------------|
| [Q Rechercher...   ] | Loups · 3 / 5                     | +-------------------+ |
| Zone [Toutes     v]  |-----------------------------------| |                   | |
| Niv. [ 1 ] à [100]   | [em] Louveteau          Niv. 3    | |   portrait 3D     | |
| [x] Rencontrés seuls | Sbire       [it][it][it]   [loupe]| |   272 x 160       | |
|----------------------|-----------------------------------| |   (tourne)        | |
| v BOIS DE CLAIRVAL   | [em] Loup gris          Niv. 5    | +-------------------+ |
|  [em] Loups    3/5   | Combattant  [it][it]       [loupe]| Loup gris     Niv. 5  |
|  [==========-----]   |-----------------------------------| Loups · Bois de Cl.   |
|  [em] Sangliers 1/5  | [##] ???                 Niv. ?   | [=======---] 7/10 vict|
|  [===------------]   | Pas encore combattu                | -- CARACTÉRISTIQUES --|
| > BOIS MOISIS   0/10 |-----------------------------------| PV 120  Faible : Feu  |
| > LAC DES JONCS 2/10 |  ...                              | [T+10][F-20][E 0][A 0]|
|                      |                                   | -- SORTS ------ [lock]|
|                      |                                   | Encore 3 victoires    |
|                      |                                   | -- BUTIN ------ [lock]|
| 10 familles          | 5 monstres                        |                       |
+-------------------------------------------------------------------------------------+
```

### 6.3 Les zones
- **En-tête** : `TitleBar` (icône `bar/bestiary.svg` 24, « Bestiaire » 18 px) ; la progression globale en `ProgressBar` 16 px (remplissage progrès) avec « 23 / 50 connus » 14 px gras : point focal de la fenêtre.
- **Filtres** (240) : recherche `LineEdit` 44 (icône `relief/search`) ; zone (`OptionButton` 44) ; niveaux (deux `SpinBox` 44, comme le journal) ; « Seulement les rencontrés » (`CheckBox`). Puis l'arbre zones › familles : en-tête de zone repliable (14 px gras capitales `#c9a27f`, compte à droite `#9d9d9d`) ; ligne de famille `ListRow` h 56 : emblème 32, nom 14 px `#cccccc`, « 3/5 » à droite, barre compacte h 8 dessous ; sélectionnée : style sélectionné. Pied : « 10 familles » 14 px `#9d9d9d`.
- **Tableau** (384) : en-tête « Loups · 3 / 5 » 16 px gras ; lignes `ListRow` h 64 : emblème 48, nom 16 px `#e0e0e0`, niveau à droite, dessous le rôle 14 px `#9d9d9d` puis les **ressources du butin** en tuiles `ItemIcon` 28 (sans compte ; infobulle = carte de l'objet + « 12 % » si le contenu le permet), et la **loupe** 44 × 44 (`relief/search`, infobulle « Voir sur la carte ») : elle ouvre la carte du monde centrée sur la zone, la région surlignée (§6.5). Pied : « 5 monstres ».
- **Fiche** (272) : portrait 3D 272 × 160 dans un creux `--osmose-ui-well` (un seul `SubViewport`, le modèle du monstre comme en combat, qui tourne lentement ; c'est l'illustration de la fenêtre, rendue depuis nos propres scènes Kenney) ; nom 18 px gras, famille et zone 14 px `#9d9d9d`, barre des victoires vers le prochain palier ; puis une **section par palier** du contenu (intertitre 14 px gras capitales `#c9a27f` + filet 1 px `#ffffff1f`) :
  - palier atteint : son contenu en composants (PV en 16 px gras ; résistances en **pastilles d'élément** 40 × 24 au fond de la couleur d'élément assombrie, valeur 14 px gras `#ffffff` ; sorts en tuiles de sorts `-small` 32 ; butin en `ItemIcon` 40) ;
  - palier à venir : intertitre + cadenas 16 px à droite, une ligne « Encore 3 victoires » 14 px `#9d9d9d`, rien d'autre.

### 6.4 Le monstre inconnu
- Ligne : emblème `-locked`, « ??? » 16 px `#9d9d9d`, « Pas encore combattu » 14 px `#6a6a6a` ; niveau selon la règle du GD (« Niv. ? » par défaut). Plus de phrase répétée qui parle d'une silhouette.
- Fiche : **la vraie silhouette** : le modèle 3D rendu en noir mat (matériau non éclairé `#0d0d0d`, contour de lumière 2 px `#3a3a3d` par un second passage agrandi), immobile. Le texte promettait une silhouette : on la dessine.

### 6.5 Emblèmes de famille
10 emblèmes (une silhouette game-icons par famille, recette des menus, une teinte par famille, `-locked`), dans `relief/family-<id>.svg`. Ils servent au bestiaire, à la fiche de groupe en exploration et, plus tard, à l'encyclopédie. Les silhouettes seront choisies et vérifiées au sprint sur la liste des familles (lorc/wolf-head est déjà pris par le menu Bestiaire : il ne sert pas aux Loups).

### 6.6 1280 × 720 et fenêtre réduite
960 × 600 tient avec 60 px de marge verticale. Sous 1024 px de large, la fiche passe sous le tableau (deux colonnes, défilement vertical) ; aucun libellé tronqué par « … » dans les noms de monstre (les noms trop longs vont au scénariste).

### 6.7 Tests
- xUnit : `BestiaryPage` expose des sections typées (`BestiarySection(Tier, Reached, VictoriesLeft, Content)`) au lieu de lignes de texte ; filtres purs (`BestiaryView` : recherche, zone, niveaux, rencontrés seuls) ; progression par famille et globale ; la ligne d'un inconnu n'a qu'un texte.
- Auto-test : ouvrir le bestiaire de Plouf, filtrer « Lac », choisir une famille, un monstre connu (portrait chargé, sections remplies), un inconnu (portrait en silhouette), la loupe (carte ouverte sur la zone, région surlignée).

**Chiffrage : 3 points (fenêtre, filtres, arbre des familles, tableau, emblèmes) + 3 points (fiche : portrait 3D, silhouette, sections par palier) + 2 points (loupe vers la carte et surlignage de la région ; butin en tuiles avec infobulle).** Le contenu du GD peut ajouter des sections : la fiche les accueille sans changer ce chiffrage, sauf nouveau composant.

---

## 7. Retour 5 : un monde moins cubique

### 7.1 Constat (ce qui fait « cubique »)
1. **Le sol est un damier** : chaque case est une tuile `ground_grass` de Kenney, deux tons alternés `(c.X + c.Y) % 2` (`TownView.Build`).
2. **Les arbres sont posés au centre exact des cases**, sans rotation, à l'échelle 1, en alternance stricte de deux modèles (`tree.glb` / `tree-high.glb` selon la parité) : un verger au cordeau.
3. **Le bord de chaque carte est une ligne droite d'obstacles** : `world_gen.py` met la bordure (`T` ou `B`) sur toute la première et la dernière rangée et colonne, puis perce des trouées (un tiers à la moitié du côté). On voit donc à chaque bord un mur d'arbres d'une case d'épaisseur, rectiligne.
4. **Au-delà du bord, rien** : la carte s'arrête net sur le fond, alors que la carte voisine continue le même bois.
5. Un seul modèle de buisson, deux de rochers, deux d'arbres importés du Nature Kit de Kenney (`assets/kenney/nature/` n'en contient que 10).

### 7.2 Ce qui est faisable sans changer d'assets ni de rendu (Kenney gardé, pas de cel shading)
**A. Dispersion (client seul, aucune donnée touchée).**
- Chaque arbre, rocher, buisson, champignon reçoit un décalage déterministe dans sa case (± 0,22 case, hachage de la case), une rotation (0-360°) et une échelle (0,8 à 1,25) ; le modèle est tiré parmi plusieurs (hachage), plus jamais par parité.
- Importer davantage de modèles **du même Nature Kit de Kenney (CC0)** : 6 à 8 arbres (`tree_oak`, `tree_fat`, `tree_tall`, `tree_detailed`, `tree_pineRoundA`...), souches, troncs couchés, 3 buissons, 4 rochers, touffes d'herbe et fougères (`grass_large`, `plant_flatShort`...), falaises (`cliff_*`) pour le §C. À confirmer sur le kit au moment du sprint ; environ 30 modèles (question Q4).
- **Bosquets** : autour d'un amas de `T`, 1 à 3 buissons et touffes décoratifs posés sur les bords des cases d'obstacle (jamais sur une case marchable), pour fondre la limite entre un bosquet et l'herbe.
- **Sol** : le damier devient une variation basse fréquence (bruit de valeur sur 6 cases) entre les deux tons de la zone, plus un troisième ton plus sec près des chemins ; la grille tactique reste lisible au survol et en combat (le quadrillage du combat est inchangé). Question Q1 : c'est un choix de lisibilité.
- Touffes d'herbe et fleurs décoratives, petites (hauteur < 0,15 case), sur une case marchable sur huit, non bloquantes.

**B. Fondu en bord de carte (client seul).**
- Au-delà de chaque bord, le client dessine une **lisière** de 6 cases : le sol et le décor des 6 premières cases de la carte voisine (les données de toutes les cartes sont dans le client), non cliquables, sans PNJ ni monstre ; sans voisine (bord de zone, mer, vide), une lisière générée (bois dense ou collines selon le style).
- La lisière s'assombrit et se désature vers l'extérieur (brouillard de distance du `WorldEnvironment`, ou dégradé de couleur sommet par sommet sur un plan de sol prolongé) : la carte ne s'arrête plus, elle s'estompe.
- Le bord jouable reste signalé : les cases de sortie gardent leur marque actuelle ; une case non marchable de la lisière n'est jamais survolable.
- Au loin, du relief **décoratif** : falaises et collines du Nature Kit posées dans la lisière générée, hors du plateau (aucun effet sur le jeu).

**C. Bordures naturelles (générateur, données régénérées).**
- Un passage de `tools/world_gen.py` (ou un outil voisin `tools/border_soften.py`, appliqué aux cartes existantes) remplace la rangée droite par une **bande irrégulière** de 0 à 3 cases de profondeur (bruit 1D le long du côté), faite d'obstacles mêlés selon le style : arbres, rochers, buissons ; roseaux et eau au lac ; haies et talus aux champs.
- Les trouées s'élargissent (la moitié aux trois quarts du côté) ; les côtés entre deux cartes d'une même zone peuvent être **entièrement ouverts** là où rien ne l'empêche. Les deux côtés d'une couture restent cohérents (les trouées sont déjà partagées entre voisines).
- Le passage ne touche **que la bande** : jamais une case de PNJ, d'objet, de point de récolte, de groupe de monstres, de sortie ou de piste ; l'accessibilité de toutes les sorties est revérifiée (la fonction `reachable` existe).
- **Cités exclues** : la palissade de Clairval (`P`) et le contour des cités restent tels quels.

### 7.3 Ce qui doit être discuté avec Charles
- **Le damier du sol** en exploration (Q1) : il aide à compter les cases d'un jeu tactique ; je propose de l'adoucir hors combat, pas de le supprimer en combat.
- **Voir la carte voisine au-delà du bord** (Q2) : plus naturel, mais on voit ce qui vient (un groupe de monstres n'y est jamais dessiné).
- **Régénérer les bordures** (Q3) : les cartes changent de forme à leurs bords ; des repères de joueur (« l'arbre du coin ») bougent.
- **Plus de modèles Kenney** (Q4) : même kit, même style, mais 30 fichiers de plus dans le dépôt et le build.
- **Du vrai relief jouable** (hauteurs, falaises qui bloquent la vue) : c'est du game design (lignes de vue, portée) et du gros travail ; je le sors de ce chiffrage. Le relief proposé ici est décoratif, hors plateau.
- **La direction artistique** reste celle de Kenney (décision du 17 oct.) : ces mesures adoucissent le quadrillage, elles ne remplacent pas les modèles. Les arbres resteront des arbres Kenney.

### 7.4 Tests
- A : xUnit sur `DecorScatter` (pur) : même case → même décalage, rotation, échelle, modèle ; décalage ≤ 0,22 ; aucun décor décoratif sur une case de sortie ou de PNJ ; au moins 3 modèles d'arbre dans une carte de bois. Auto-test : charger une carte des bois, compter les modèles distincts, vérifier qu'aucun arbre n'est au centre exact de sa case.
- B : xUnit sur `MapSkirt` (pur) : pour un bord avec voisine, les cases de la lisière sont celles de la voisine, dans le bon ordre et à la bonne place ; sans voisine, une lisière du style de la zone ; profondeur 6. Auto-test : nombre de nœuds de lisière par bord, aucun survolable.
- C : tests du script : la rangée extérieure d'une carte hors cité n'a jamais plus de 8 obstacles d'affilée ; la profondeur de la bande varie (écart type > 0,5) ; aucune case de PNJ, objet, récolte, groupe ou sortie modifiée ; toutes les sorties accessibles ; Clairval inchangé. xUnit existants des cartes (accessibilité, groupes, trajets de quête) verts ; test d'intégration de l'API sur une carte régénérée.

**Chiffrage : 3 points (A, dispersion et sol) + 3 points (B, lisière et fondu) + 4 points (C, bordures naturelles et régénération des données).**

---

## 8. Découpage en sprints (gains rapides d'abord)

Numérotation provisoire G1 à G7, placée par D87 avec l'arbre du game designer (retour 8) et le contenu du bestiaire : G1 = S165, G2 = S166, G3 = S167, G4 = S168 (carte) et S171 (fenêtre du bestiaire), G5 = S172 (fiche) et S175 (la loupe et le butin en tuiles rejoignent les liens du game designer, B4), l'arbre en S176-S177, G6 = S178, G7 = S179 (D88 : la carte photographiée S169 et la mer peinte S170 s'insèrent après S168). Réponses de Charles : Q1 b, Q2 oui, Q3 oui (côtés ouverts compris), Q4 oui, Q5 relief décoratif ; Q7 non pris ; les autres selon tes recommandations (D87, limites).

| Sprint | Stories | Points | Ce que Charles teste |
|---|---|---|---|
| **G1, gains rapides** | Suivi de quête repliable (1) ; panneau Menus repliable avec badge (1) ; icônes de types de quête, filtres et liste du journal (2) | **4** | Replier les deux encarts, relancer le jeu (état gardé) ; ouvrir le journal |
| **G2, objets en relief** | Recette des tuiles d'objets, 422 tuiles, matière, provenance, `ItemIcon` (4) ; ligne de recette et dédoublonnage des dessins (2) | **6** | Métiers de Plouf (forgeron, tailleur), inventaire, un atelier |
| **G3, fiche de quête et atlas** | Fiche de quête : objectifs, récompenses en tuiles, boutons (2) ; atlas : régions, contours, mer, forêts, eau, routes (4) | **6** | Une quête de Plouf ; la carte du monde dézoomée |
| **G4, carte finie et bestiaire** | Repères, noms de zones, légende, zoom à deux niveaux, fenêtre au thème (3) ; bestiaire : fenêtre, filtres, familles, tableau, emblèmes (3) | **6** | Carte : zoom et légende ; bestiaire : filtres |
| **G5, fiche du bestiaire** | Fiche : portrait 3D, silhouette, sections par palier (3) ; loupe vers la carte, butin en tuiles (2) | **5** | Un monstre connu, un inconnu, la loupe |
| **G6, monde adouci (client)** | Dispersion du décor, modèles Kenney en plus, sol sans damier dur (3) ; lisière et fondu en bord de carte (3) | **6** | Promenade bois, lac, champs, Ancêtres |
| **G7, bordures naturelles** | Bordures irrégulières, trouées élargies, données régénérées (4) | **4** | Traverser une zone d'un bout à l'autre |

**Total : 37 points, 7 sprints.**

Dépendances : G3 (fiche de quête) et G5 (butin) utilisent les tuiles d'objets de G2 ; G5 (loupe) utilise l'atlas de G3-G4 ; G5 attend aussi le contenu du bestiaire du GD (la fiche peut se faire sur le contenu actuel, puis se remplir) ; G6 et G7 attendent les réponses Q1 à Q6. G6 peut passer avant G3 si Charles juge le monde plus pressant que la carte : ces sprints sont indépendants.

---

## 9. Questions pour Charles

Monde (retour 5) :
1. **Damier du sol** en exploration : (a) le garder ; (b) l'adoucir hors combat en taches de deux tons proches, grille au survol et en combat comme aujourd'hui (recommandé) ; (c) le retirer partout sauf en combat ?
2. **Voir le début de la carte voisine** au-delà du bord, estompé et non cliquable (recommandé), ou un simple fondu vers un bois dense générique ?
3. **Régénérer les bordures** des cartes hors cités (positions des PNJ, objets, récoltes, groupes et sorties préservées) : oui ? Et les côtés entre deux cartes d'une zone peuvent-ils être entièrement ouverts quand rien ne s'y oppose ?
4. **Importer une trentaine de modèles de plus du Nature Kit de Kenney** (CC0, même style : arbres variés, souches, herbes, falaises) : d'accord ?
5. **Relief** : on s'en tient à du relief décoratif hors du plateau (recommandé), ou tu veux du vrai relief jouable (hauteurs, lignes de vue) ? Le second est un sujet de game design à chiffrer à part.
6. **Contour de cité** : Clairval garde sa palissade ; Quatre-Chemins (un hameau) en a-t-il une, ou s'ouvre-t-il naturellement ?

Carte du monde (retour 4) :
7. **Brouillard** sur les cartes jamais visitées ? Il demande au serveur de retenir les cartes vues (contenu, à chiffrer avec le GD ; non compris dans les 37 points).
8. **Îlots décoratifs** dans la mer (sans carte jouable) : oui, ou une mer nue tant que le monde n'a pas d'îles ?
9. Les **noms de zones** : seulement au dézoom (recommandé), ou toujours ?

Encarts (retours 6 et 7) :
10. Un **raccourci clavier** pour masquer le suivi de quête, ou le clic suffit ? L'état replié est-il gardé **par poste** (comme le son, recommandé) ou par personnage ?

Objets (retour 1) :
11. Le **liseré des pièces de panoplie** passe du pistache au violet (`#b48cff`), parce que le pistache est l'anneau de sélection : d'accord ?

Bestiaire (retour 2) :
12. Un **portrait 3D** qui tourne dans la fiche, et pour un monstre inconnu sa **vraie silhouette noire** : ça te va ?

---

## 10. Textes à faire écrire par le scénariste (FR / EN)
- `tracker.title` : « Quête suivie » / « Tracked quest » ; `tracker.fold` : « Masquer le suivi » / « Hide tracking » ; `tracker.unfold` : « Afficher le suivi » / « Show tracking ».
- `bar.fold` : « Replier les menus » / « Collapse menus » ; `bar.unfold` : « Déplier les menus » / « Expand menus ».
- `journal.objectives`, `journal.rewards` existent ; `journal.reward.xp` : « {n} XP » ; `journal.reward.shards` : « {n} Éclats » / « {n} Shards » (infobulles des tuiles).
- `map.legend` : « Légende » / « Legend » ; `map.legend.dungeons`, `map.legend.waystones`, `map.legend.workshops`, `map.legend.quest`, `map.grid` (existe) ; `map.zone-levels` : « Niv. {a}-{b} » / « Lvl {a}-{b} ».
- `bestiary.search` : « Rechercher un monstre » / « Search a monster » ; `bestiary.zone-all` : « Toutes les zones » / « All zones » ; `bestiary.met-only` : « Seulement les rencontrés » / « Met only » ; `bestiary.families` : « {n} familles » ; `bestiary.monsters` : « {n} monstres » ; `bestiary.locate` : « Voir sur la carte » / « Show on the map » ; `bestiary.not-met` : « Pas encore combattu » / « Not fought yet » (remplace la phrase sur la silhouette) ; `bestiary.left` : « Encore {n} victoires » / « {n} more victories » ; intertitres des paliers selon le contenu du GD.
- Noms de monstres, de familles et de zones qui dépasseraient leur colonne (240 / 384 / 272 px en 14-16 px) : à recenser au sprint G4.
