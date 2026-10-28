# S158 : la fenêtre des succès (maquette)

Sources : `src/assets/tokens.css`, `src/Rpg.Client/UiStyle.cs`, `godot/OsmoseTheme.cs`, `docs/graphisme/s157-maquettes.md` (recette des icônes §1, cadre en relief §4.1, composants §4.3), D84, D54 (les captures Wakfu servent de référence d'ergonomie : pas de mise en page, d'asset ni de pictogramme repris ; ici l'arbre se déplie à gauche, la bannière est un rendu de nos scènes, le détail est une colonne fixe).
Remplace `godot/AchievementsPanel.cs` (800×620, OptionButton du titre en haut).

Point focal : **le bandeau doré du total**, puis la barre de la vue affichée. Tout le reste est en gris.

---

## 1. Cadre commun (1040×640, centré ; à 1280×720 : marges 120 / 40)

```
x: 0  16                276 292                                   1024 1040
   +----------------------------------------------------------------------+  y 0
   | [trophée24] Succès (H)        <[trophée20]  1 240 >             [ X ] |  TitleBar h 44
   +======================================================================+  y 44 (liseré 2 px frame-dark)
   |                                                                      |  12
   | [loupe] Rechercher un succès  |  EN-TÊTE DE LA VUE (titre + n / N)   |  y 56
   |  (LineEdit 260×44)       [x]  |                                      |
   |-------------------------------|  [=========== barre h16 ==== 47 % ]  |  y 88-104
   | ARBRE (ScrollContainer)       |                                      |  12
   |  260 × 464, y 108-572         |  CORPS DE LA VUE  732 × 456          |  y 116
   |                               |  (§2 ou §3)                          |
   |                               |                                      |  y 572
   |                                                                      |  8
   | [v] Masquer les succès déverrouillés    Titre porté : « … » [ Ôter ] |  y 580-624, h 44
   +----------------------------------------------------------------------+  y 640 (marge 16)
```

- Panneau : variation `WindowPanel` (fond `--osmose-ui-window` #1f1f1f, bord 2 px `--osmose-ui-frame` #7a5434, rayon 8, ombre). `WindowDrag.Make` conservé.
- Barre de titre `TitleBar` (h 44, côtés 12) : icône `achievements` (lorc/trophy, palette S157 §2) 24 px, écart 8, « Succès » 18 px gras #ffffff, écart 8, « (H) » 14 px `--osmose-ui-text-dim` #9d9d9d. À droite `CloseButton` 44×44.
- **Bandeau doré** : `GoldBand` (h 32, côtés 20, queue d'aronde) + `GoldBandLabel` 16 px gras `--osmose-ui-gold-text` #3a2410 ; trophée 20 px à gauche, écart 6 ; nombre groupé par milliers (espace fine). Centré **sur la fenêtre** (ancre 0,5, pas centré dans l'espace restant), y 6-38, largeur min 160. Infobulle : `achievements.band.tip`. Gain « +N » : animation S157 §4.3 inchangée.
- Colonnes : gauche x 16-276 (260), gouttière 16, droite x 292-1024 (732).
- En-tête de vue (les deux vues, même gabarit) : ligne y 56-80 : titre 18 px gras #ffffff à gauche (fil d'Ariane « Donjons › Niveaux 1 à 20 » en vue catégorie, séparateur « › » en `text-dim`), compte « 28 / 61 succès » 14 px `--osmose-ui-text` #cccccc à droite ; barre `ProgressBar` h 16, y 88-104, % centré dans la barre (14 px gras blanc, contour 3 px noir) ; à 100 % → variation `GoldBar`.
- Pied y 580-624 : `CheckBox` « Masquer les succès déverrouillés » à gauche ; à droite « Titre porté : « Le Cartographe » » 14 px `--osmose-ui-text` + `Button` (secondaire) « Ôter » min 88×44 ; sans titre : « Titre porté : aucun », bouton masqué. Message d'erreur du serveur (`Tell`) : centré dans le pied, 14 px `--osmose-ui-close-hover-text` #f08070, effacé après 4 s.

### 1.1 Colonne gauche
- `LineEdit` 260×44 (thème S157) : pictogramme lorc/magnifying-glass 20 px `text-dim` à gauche, via un `TextureRect` 20 px posé à x 12 et `content_margin_left` 40 (nouvelle variation `SearchField` = `LineEdit` + marge gauche 40). `clear_button_enabled = true`. Indice `achievements.search`.
- Arbre (`ScrollContainer`, piste 12 → lignes de 248) :

```
 <------------------------- 248 ------------------------->
 +------+--------------------------------------------------+
 |      | [ic28] Progression générale            47 %      |  h 44  (pas de +/-)
 |------+--------------------------------------------------|  séparateur 1 px #ffffff0f, marge 4
 | [+]  | [ic28] Exploration                     62 %      |  h 44
 | [-]  | [ic28] Donjons                         30 %      |
 |      |   |    Niveaux 1 à 20                  45 %      |  sous-catégorie h 44
 |      |   |    Niveaux 21 à 40                 10 %      |
 |      | [ic28] Métiers                        100 %      |  100 % en doré
 +------+--------------------------------------------------+
  44 px   8 | 28 | 8 | nom (≤ 100) ... | % 48 aligné droite | 8
```
- Bouton +/− : nouvelle variation `TreeToggle` (Button 44×44, plat transparent, survol `--osmose-ui-hover` #2d2d2d rayon 6) ; signe dessiné 12×12, traits 2 px, `text-dim`, survol `text-strong` #e0e0e0. Absent (place gardée) pour une catégorie sans sous-catégorie et pour « Progression générale ».
- Ligne : nouvelle variation `TreeRow` (Button, `toggle_mode`, un `ButtonGroup` pour tout l'arbre), 204×44, alignement gauche :
  - normal : transparent, nom 14 px gras (catégorie) ou 14 px normal (sous-catégorie) `--osmose-ui-text` ;
  - survol : fond `--osmose-ui-hover` #2d2d2d, texte `text-strong` ;
  - choisi (`pressed`) : fond `--osmose-ui-selected` #33421f, bord gauche 3 px `--pistache`, texte #ffffff (comme `ListRow` sélectionnée) ;
  - sous-catégorie : icône remplacée par un filet vertical 2 px #ffffff1f à x 14 de la zone icône (rattache visuellement au parent).
- % à droite : 14 px gras, `text-dim` #9d9d9d (5,7:1) ; 100 % : `--osmose-ui-gold` #ffd166 (10,9:1 sur #252526, 11,8:1 sur #1f1f1f). Jamais pistache en texte.
- Le % ne dépend ni de la recherche ni de la case « Masquer ».
- Clic sur le nom d'une catégorie à sous-catégories : affiche toute la catégorie **et** la déplie ; le +/− seul ne change pas la vue.

---

## 2. Vue « Progression générale »

```
x 292                                    644 660                                   1012 (piste 1012-1024)
   PROGRESSION GÉNÉRALE                                        28 / 61 succès        y 56-80
   [==================== 47 % ====================                         ]      y 88-104
                                                                                     y 116
   +------------------------------------+  +------------------------------------+
   |                                    |  |                                    |
   |   BANNIÈRE 352 × 96                |  |   BANNIÈRE                         |
   | [ic48]                        [T]  |  | [ic48]                             |  [T] trophée 20 si 100 %
   |------------------------------------|  |------------------------------------|  y 212
   | Exploration              12 / 20   |  | Chasse                    3 / 14   |  12 + 20
   | [=========== 60 % ==========     ] |  | [===== 21 % ==              ]      |  8 + 16 + 12
   +------------------------------------+  +------------------------------------+  y 280
                       gouttière 16 (horizontale et verticale)
   +----- Donjons ----------------------+  +----- Métiers ----------------------+  y 296
   ...  4 rangées (7 tuiles), défilement vertical ; 2 rangées pleines visibles
```

### 2.1 Tuile de catégorie (nouvelle variation `AchievementTile`, `Button` contenant un `VBoxContainer`)
- 352×164 : bannière 352×96 (bord à bord en haut), puis marges 12 : ligne nom h 20 (nom 16 px gras #ffffff à gauche ; « 12 / 20 » 14 px `--osmose-ui-text` à droite), écart 8, `ProgressBar` h 16 (% dedans), marge basse 12. 96 + 12 + 20 + 8 + 16 + 12 = 164.
- Corps (StyleBoxFlat) : fond `--osmose-ui-tab` #252526, bord 1 px `--osmose-ui-edge` #0d0d0d, rayon 8, ombre #00000080 taille 6 décalage (0, 3) (élément cliquable = ombre).
- États :

| État | Fond | Bord | Autre |
|---|---|---|---|
| normal | #252526 | 1 px #0d0d0d | ombre (0, 3) |
| survol | `--osmose-ui-hover` #2d2d2d | 1 px `--chocolat-clair` #c9a27f | bannière `modulate` 1,08 |
| pressé | #252526 | 1 px #0d0d0d | sans ombre, contenu +1 px |
| focus clavier | idem normal | 2 px `--pistache` | — |
| complète (100 %) | idem | 2 px `--osmose-ui-gold` #ffd166 | barre `GoldBar`, trophée 20 px en haut à droite de la bannière (8, 8 du coin) |

- Clic : ouvre la vue de la catégorie (§3) et la choisit dans l'arbre.

### 2.2 Bannière
- Texture PNG **704×192** (2×), affichée 352×96, `TextureRect` `expand_mode = IgnoreSize`, `stretch_mode = KeepAspectCovered` (recadrage centré si la tuile rétrécit). Coins haut arrondis à 7 px (14 px dans le PNG, transparents) : cuits dans l'image, pas de découpe à l'exécution.
- Par-dessus, cuits aussi : voile bas `#000000` 0 → 0,55 sur les 40 % inférieurs (asseoir l'icône), filet bas 1 px `--osmose-ui-edge`.
- Icône de catégorie 48×48 (PNG 72 → 48) à (12, 36) : en bas à gauche, sur le voile.
- **Rendu de scène (cible)** : rendu hors écran (SubViewport, headless) d'une scène Kenney existante, caméra orthographique 3/4 ; une par catégorie : Exploration = la carte avec une pierre de passage ; Chasse = un groupe de monstres en forêt ; Donjons = l'entrée d'un donjon ; Métiers = l'atelier ou la forge ; Quêtes = un PNJ du village et son point de quête ; Arbre = le grand arbre du village (ou à défaut un bosquet) ; Ensemble = trois personnages côte à côte. Fichiers `assets/ui/achievements/banner-<Category>.png`, produits par un outil (test : 704×192, coins transparents).
- **Repli (S158 si les rendus ne sont pas prêts)** dessiné par `tools/ui_art.py` : dégradé linéaire diagonal haut-gauche → bas-droite de `mix(main, #1f1f1f, 0,55)` à `mix(main, #1f1f1f, 0,85)` ; filigrane = la silhouette de la catégorie en #ffffff opacité 0,10, hauteur 240 (PNG), ancrée à droite (x 520, y −24, débordante, rognée) ; puis voile, filet et icône comme ci-dessus.

| Catégorie | Icône (menu S157) | main | Dégradé haut-gauche | bas-droite |
|---|---|---|---|---|
| Exploration | map (lorc/treasure-map) | #52c45a `--osmose-air` | #36693a | #273828 |
| Chasse (Hunt) | bestiary (lorc/wolf-head) | #ef4b4b `--osmose-fire` | #7d3333 | #3e2626 |
| Donjons | **dungeons (lorc/dungeon-gate)**, nouvelle | #b48cff `--osmose-tempo` | #625084 | #352f41 |
| Métiers | jobs (lorc/anvil + claw-hammer) | #a9bccd | #5d666d | #343739 |
| Quêtes | journal (lorc/scroll-unfurled + quill-ink) | #b89a6a (dark du parchemin : le main #f0e6cc donnerait un gris) | #645641 | #36312a |
| Arbre (Tree) | talents (delapouite/skills) | #bef374 `--pistache` | #677e45 | #373f2c |
| Ensemble (Together) | contacts (delapouite/three-friends) | #ff7eb6 `--osmose-heal` | #844a63 | #412d36 |

- Aucun nouveau jeton : ces teintes sont dérivées par formule dans `ui_art.py` à partir des jetons ; le test de l'outil vérifie les 14 hex ci-dessus.
- **Nouvelle icône Donjons** : `lorc/dungeon-gate` (CC BY 3.0, à créditer dans `credits.json`), recette S157 §1, palette main #b48cff, light #d6c0ff, dark #634d8c, outline #241c33 (= celles de `spells`, sans accent : ici sans voisin violet). PNG 72×72. Entrée `dungeons` dans `palette.json`.

---

## 3. Vue d'une catégorie ou sous-catégorie

```
x 292                                 708 724                                  1024
   DONJONS › NIVEAUX 1 À 20                                     9 / 20 succès          y 56-80
   [=============== 45 % ==========                                         ]        y 88-104
                                                                                       y 116
   +--------------------------------------+  +------------------------------------+
   |# [cadre52] Gardien du terrier   10 T |  | [cadre64]  Gardien du terrier      |
   |#           Finir le Terrier…         |  |            [T] 10                  |  détail 300 × 456
   +--------------------------------------+  | Finir le Terrier des taupes sans   |  (StyleBox `DetailPanel`)
   |  [cadre52] Sans une égratignure  20 T|  | perdre un allié.                   |
   |            Finir un donjon sans…     |  |------------------------------------|
   |            [======      ] 3 / 5      |  | OBJECTIFS                          |
   +--------------------------------------+  | [v] Terrier des taupes             |  objectif booléen
   |  ...  cartes 400 × 80, écart 8        |  | 3 / 5  [=========        ]         |  objectif chiffré
   |  5 cartes visibles                   |  |------------------------------------|
   |                                      |  | RÉCOMPENSES                        |
   |                                      |  | [T] 10 points de succès            |
   |                                      |  | (S159) [éclat] 200 Éclats  [xp] … |
   |                                      |  | (S159) [     Réclamer       ]      |  ButtonPrimary 268×44
   |                                      |  | Titre : « Gardien des terriers »   |
   |                                      |  | [    Porter ce titre       ]       |  ButtonSecondary 268×44
   |                                      |  |------------------------------------|
   |                                      |  | (S159, méta) SUCCÈS REQUIS         |
   |                                      |  | [ic24] Nom du sous-succès   Voir > |  ligne h 44
   +--------------------------------------+  +------------------------------------+  y 572
    # = liseré doré 4 px (obtenu)            liste 416 (cartes 400 + piste 12 + 4)
```

### 3.1 Carte de succès (nouvelle variation `AchievementCard`, `Button` `toggle_mode`, même `ButtonGroup` que la liste)
- 400×80, marges 12 / 12 / 12 / 12 (gauche 16 si obtenu, voir liseré). Cadre d'icône 52×52 à gauche, écart 12, puis colonne : ligne 1 h 20 = nom 16 px gras + à droite points « 10 » 14 px gras et trophée 16 px ; ligne 2 h 18 = phrase 14 px sur une ligne, `text_overrun_behavior = TrimEllipsis` (texte entier dans le détail et en infobulle) ; ligne 3 (non obtenu à compte > 1) : `ProgressBar` compacte h 8 (rayon 4, sans reflet) largeur 200 + « 3 / 5 » 14 px `text-dim`, écart 8.
- Liseré obtenu : `ColorRect` 4×56 `--osmose-ui-gold` à (0, 12), enfant de la carte, indépendant du StyleBox (il reste visible quand la carte est choisie).

| État | Fond | Bord | Nom | Phrase | Points + trophée | Icône |
|---|---|---|---|---|---|---|
| non obtenu | `--osmose-ui-tab` #252526 | 1 px #ffffff0f | `text-strong` #e0e0e0 | `text-dim` #9d9d9d | `text-dim`, trophée gris | variante grise, cadre `IconFrame` éteint |
| **obtenu** | #252526 | 1 px #ffd16659 (`--osmose-ui-gold` à 35 %) | #ffffff | `--osmose-ui-text` #cccccc | `--osmose-ui-gold` #ffd166, trophée en couleur | couleur, cadre `IconFrame` doré, liseré 4 px |
| survol | `--osmose-ui-hover` #2d2d2d | 1 px `--chocolat-clair` #c9a27f | inchangé | inchangé | inchangé | — |
| choisi | `--osmose-ui-selected` #33421f | 2 px `--pistache` | #ffffff | #cccccc | inchangé | — |
| pressé | comme choisi, contenu +1 px | | | | | |

- Distinction obtenu / non obtenu portée par **quatre** indices (liseré, icône en couleur, cadre doré, points dorés) : lisible sans la couleur (daltonisme) grâce au liseré et au cadre.
- Clic : choisit la carte et remplit le détail. À l'ouverture d'une vue : la première carte non obtenue est choisie (sinon la première).

### 3.2 Cadre d'icône (nouvelle variation `IconFrame`, PanelContainer, 9-patch de la recette S157 §4.1, rayon 8)
| État | Corps haut → bas | Contour | Icône |
|---|---|---|---|
| obtenu | `--osmose-ui-raised-top` #3a3a3d → `raised-bottom` #262628 (pas le vert `lit-*`, réservé au choix) | 2 px `--osmose-ui-gold` #ffd166 | PNG couleur 40×40 |
| non obtenu | `--osmose-ui-well` #141414 plat, sans reflet | 1 px `--osmose-ui-edge` #0d0d0d | PNG gris 40×40 |

- Marges de contenu 6 → icône 40 dans 52 (64 → 52 dans le détail, marges 6).
- Icône du succès = celle de sa catégorie ; méta-succès (S159) = `achievements` (trophée).
- Variantes grises : produites par `ui_art.py` (`<nom>-locked.png`), palette main #8a8a8a (`--osmose-locked`), light #bfbfbf, dark #4c4c4c, outline #1c1c1c (formule S157 §1.3). Pas de shader de désaturation.

### 3.3 Détail (nouvelle variation `DetailPanel`, PanelContainer + ScrollContainer interne)
- 300×456, fond `--osmose-ui-tab` #252526 plat (surface de lecture), bord 1 px `--osmose-ui-frame-dark` #3a2616, rayon 8, marges 16 → largeur utile 268.
- Tête : `IconFrame` 64×64, écart 12, colonne : nom 18 px gras #ffffff (retour à la ligne) ; dessous trophée 16 + points 14 px gras (doré si obtenu, `text-dim` sinon) ; « Obtenu » 14 px `--osmose-ui-gold` si obtenu. Puis 12, phrase entière 14 px `--osmose-ui-text`, retour à la ligne.
- Titre de section (« OBJECTIFS », « RÉCOMPENSES », « SUCCÈS REQUIS ») : nouvelle variation `SectionLabel` (Label) 14 px gras capitales, `spacing_glyph` 1, `--chocolat-clair` #c9a27f (6,6:1), comme le titre MENUS ; 16 au-dessus, filet 1 px #ffffff0f et 8 en dessous.
- Objectifs (lecture seule, pas des cibles) : lignes h 28. Objectif à compte 1 : case 20×20 dessinée comme `CheckBox` (vide : creux `well` bord 2 px `text-dim` ; atteint : fond `--pistache`, coche 3 px `--chocolat`) + libellé 14 px. Objectif chiffré : « 3 / 5 » 14 px gras puis `ProgressBar` h 16 sur le reste de la ligne ; atteint → `GoldBar`. En S158 un succès = un objectif ; S159 (méta) donne plusieurs lignes.
- Récompenses : lignes h 28, pictogramme 20 + texte 14 px `--osmose-ui-text`, valeur en `--osmose-cost` #f2d36b (comme les infobulles). S158 : points de succès seulement (+ titre). S159 : Éclats et XP, puis `ButtonPrimary` « Réclamer » 268×44 (désactivé tant que non obtenu ; masqué une fois réclamé, remplacé par « Réclamé » 14 px `text-dim`).
- Titre à porter : ligne « Titre : « {0} » » 14 px `--osmose-ui-text`, valeur en #ffffff ; dessous bouton secondaire 268×44 :
  - obtenu, non porté : « Porter ce titre » ;
  - obtenu, porté : « Ôter ce titre » (même bouton, l'action s'inverse) et la ligne devient « Titre porté » ;
  - non obtenu : bouton désactivé (texte #6a6a6a, sans reflet), infobulle `achievements.title-locked`.
  - émet `TitleChosen(id)` / `TitleChosen(null)` (événement existant).
- Succès requis (S159) : lignes h 44, icône 24 (couleur ou grise), nom 14 px (doré si obtenu), à droite lien « Voir › » : nouvelle variation `LinkButton` 14 px `--chocolat-clair`, survol `text-strong` souligné, min 64×44. « Voir » choisit la sous-catégorie du sous-succès dans l'arbre et sa carte.
- Aucun succès choisi (liste vide) : texte centré `achievements.pick` 14 px `text-dim`.

---

## 4. Recherche, case « Masquer », états vides

- Recherche : à partir de 2 caractères, sans accents ni casse, sur nom + phrase + titre. L'arbre garde sa sélection mais se grise (alpha 0,5) ; la vue droite devient « Résultats (N) » (même gabarit que §3, sans barre d'en-tête ; sous chaque nom, le fil « Donjons › Niveaux 1 à 20 » 14 px `text-dim` remplace la phrase). Effacer le champ rend la vue précédente.
- Case « Masquer les succès déverrouillés » : retire les cartes obtenues de la liste et des résultats ; ne touche ni aux % ni aux tuiles ; mémorisée dans les réglages du client.
- Vide de recherche : au centre de la zone liste + détail, trophée gris 64, écart 12, `achievements.empty.search` 16 px `--osmose-ui-text`, écart 16, bouton secondaire « Effacer la recherche » 44 de haut.
- Vide par la case (tout est obtenu ici) : trophée **doré** 64 + `achievements.empty.all-earned` 16 px ; le détail affiche `achievements.pick`.

## 5. Tailles et fenêtre réduite
- 1280×720 : fenêtre 1040×640 centrée. Viewport plus petit (fenêtre de l'OS réduite) : fenêtre = viewport − 32, minimum 960×560 ; seule la liste rétrécit (416 → 336 à 960), le détail reste à 300, la colonne gauche à 260 ; tuiles (732 − 12 − 16)/2 → 312 à 960, bannière recadrée (`KeepAspectCovered`), hauteur 96 fixe. Hauteur 560 : corps 376, toujours 2 rangées de tuiles moins 1/3.
- Toutes les cibles ≥ 44 (lignes d'arbre, +/−, cartes 80, boutons, lien Voir, case) ; tout le texte ≥ 14 px.
- Grille : tout multiple de 4.

## 6. Composants
**Réutilisés (S157)** : `WindowPanel`, `TitleBar`, `CloseButton`, `GoldBand`/`GoldBandLabel`, `ProgressBar`/`GoldBar`, `LineEdit`, `CheckBox`, `Button` (secondaire), `ButtonPrimary` (S159), barre de défilement.

**À ajouter à `OsmoseTheme`** (une constante par nom, un test xUnit chacune : couleurs = `UiStyle`, hauteur ≥ 44 pour les cibles) :

| Variation | Base | Valeurs |
|---|---|---|
| `SearchField` | LineEdit | thème LineEdit, `content_margin_left` 40 |
| `TreeRow` | Button | normal transparent ; survol `Hover` ; pressed/hover_pressed `Selected` + bord gauche 3 `Pistachio` ; texte 14, `Text` / `TextStrong` / blanc ; marges 8 ; h 44 |
| `TreeToggle` | Button | 44×44, transparent, survol `Hover` rayon 6 ; signe `TextDim` → `TextStrong` |
| `AchievementTile` | Button | §2.1 |
| `AchievementCard` | Button | §3.1 |
| `IconFrame` / `IconFrameLocked` | PanelContainer | §3.2 ; 9-patch `frame-gold` à ajouter à `ui_art.py` |
| `DetailPanel` | PanelContainer | §3.3 |
| `SectionLabel` | Label | 14 gras, `ChocolateLight`, spacing 1 |
| `LinkButton` (variation `Link`) | Button | plat, 14, `ChocolateLight`, survol `TextStrong` + souligné, min 64×44 |
| `ProgressBarCompact` | ProgressBar | h 8, rayon 4, remplissage sans reflet, pas de texte |

**Jetons** : aucun nouveau `--osmose-ui-*`. Seule composition : `Gold` + alpha 59 (bord d'une carte obtenue), comme `Pistachio + "4d"` en S157.

**Données (à signaler au dev, hors graphisme)** : `AchievementSpec` n'a pas de sous-catégorie ; il faut un champ facultatif (`group`) dans `data/achievements.json` (Donjons : tranche de niveaux du donjon ; Quêtes : zone ; Chasse : `general` / `families`), et le % d'une catégorie = obtenus / total (pas pondéré par les points). Éclats et XP n'existent qu'en S159.

## 7. Textes FR / EN (`Texts.cs`)
Interface (à écrire directement) :

| Clé | FR | EN |
|---|---|---|
| `achievements.shortcut` | ({0}) | ({0}) |
| `achievements.band.tip` | Points de succès | Achievement points |
| `achievements.search` | Rechercher un succès | Search achievements |
| `achievements.overall` | Progression générale | Overall progress |
| `achievements.count` | {0} / {1} succès | {0} / {1} achievements |
| `achievements.percent` | {0} % | {0}% |
| `achievements.expand` / `.collapse` (infobulles) | Déplier / Replier | Expand / Collapse |
| `achievements.sub.general` | Général | General |
| `achievements.sub.families` | Familles | Families |
| `achievements.sub.levels` | Niveaux {0} à {1} | Levels {0} to {1} |
| `achievements.results` | Résultats ({0}) | Results ({0}) |
| `achievements.unlocked` | Obtenu | Unlocked |
| `achievements.objectives` | Objectifs | Objectives |
| `achievements.rewards` | Récompenses | Rewards |
| `achievements.reward.points` | {0} points de succès | {0} achievement points |
| `achievements.reward.shards` (S159) | {0} Éclats | {0} Shards |
| `achievements.reward.xp` (S159) | {0} XP | {0} XP |
| `achievements.claim` / `.claimed` (S159) | Réclamer / Réclamé | Claim / Claimed |
| `achievements.reward.title` | Titre : « {0} » | Title: “{0}” |
| `achievements.wear-title` | Porter ce titre | Wear this title |
| `achievements.remove-title` | Ôter ce titre | Remove this title |
| `achievements.worn` | Titre porté : « {0} » | Title worn: “{0}” |
| `achievements.worn-none` | Titre porté : aucun | Title worn: none |
| `achievements.remove` | Ôter | Remove |
| `achievements.required` (S159) | Succès requis | Required achievements |
| `achievements.see` | Voir › | View › |
| `achievements.hide-earned` | Masquer les succès déverrouillés | Hide unlocked achievements |
| `achievements.empty.clear` | Effacer la recherche | Clear search |

À faire relire au scénariste (ton du jeu) :

| Clé | Proposition FR | EN |
|---|---|---|
| `achievements.title-locked` | Obtiens ce succès pour porter son titre. | Earn this achievement to wear its title. |
| `achievements.pick` | Choisis un succès pour voir ses objectifs. | Pick an achievement to see its objectives. |
| `achievements.empty.search` | Aucun succès ne correspond à « {0} ». | No achievement matches “{0}”. |
| `achievements.empty.all-earned` | Tout est déverrouillé ici. Décoche la case pour revoir ces succès. | Everything here is unlocked. Untick the box to see them again. |

Clés retirées : `achievements.wear`, `achievements.gives` (remplacées par `reward.title` / `wear-title`). Noms de zones des sous-catégories de Quêtes : clés de zone existantes.
