# S163-S164 : icônes de sorts et de métiers en relief, langage visuel de l'arbre

Demande de Charles (30 oct.) : « les icônes de sorts méritent un coup de neuf (à l'image des icônes de menus). D'ailleurs certains sorts élémentaires n'ont pas d'icône (les sorts Foudre par exemple !). Les icônes de métiers aussi ! L'arbre des talents est illisible, il mérite une refonte, de ressembler plus à un arbre. »

Sources : `src/assets/tokens.css`, D54 (rien d'Ankama), D84 (refonte graphique), `docs/graphisme/s157-maquettes.md` (recette des menus, §1), `tools/ui_art.py`, `godot/SpellIcon.cs`, `godot/TreePanel.cs`, `godot/JobsPanel.cs`, `data/spells.json`, `data/jobs.json`, `godot/assets/icons/{spells,talents,relief}/credits.json`, captures `spells.png` et `tree.png` du 30 oct.
Périmètre : les icônes (sorts, métiers, talents) et le **langage** de l'arbre. La **structure** de l'arbre (quels nœuds, où, combien) vient du game designer ; la maquette finale de l'arbre suivra sa proposition.
Toutes les silhouettes citées ont été vérifiées sur game-icons.net le 30 oct. 2026 (CC BY 3.0, à créditer).

---

## 0. État des lieux

### 0.1 Sorts
- `SpellIcon.cs` dessine un `StyleBoxFlat` (fond = couleur d'élément assombrie de 45 %, bord 2 px de la couleur d'élément, rayon 8) puis la silhouette **blanche** `assets/icons/spells/<id>.svg` en `DrawTextureRect`, insérée de 14 %. Aucun relief, aucune ombre ; « non débloqué » = tout à 40 % d'opacité, si bien que la couleur du panneau transparaît et que la tuile paraît sale (capture : Fente ardente, Ressac, Lame du vent).
- 72 SVG pour les sorts de classe (`spells/credits.json`). **Aucun fichier** pour les 24 jumeaux de Foudre (`lightning-*`) : leur champ `icon` existe dans `spells.json`, mais `TextureOf` cherche `spells/<id>.svg`, introuvable ; le code se rabat donc sur la première lettre du nom. Les 6 sorts de l'arbre (`relay`, `seal-earth/fire/water/air`, `decoy`) n'ont ni fichier ni champ `icon`. Les sorts de monstres non plus.
- Problèmes de silhouettes parmi les 72 :
  - trois sorts d'**Air** dessinés avec des éclairs : `bolt` (lorc/focused-lightning), `skyfall` (lorc/lightning-arc), `tempest` (lorc/lightning-storm). Depuis que la Foudre existe, ces dessins mentent ;
  - un sort de **Terre** dessiné avec un éclair : `titan-blow` (lorc/thor-fist, rangé dans l'étiquette « lightning » du site) ;
  - deux doublons : `ember-volley` et `inferno-blade` (lorc/fire-ring), `field-dressing` et `mend` (delapouite/healing).
- Livre de sorts (capture) : l'élément est dit **trois fois** par carte (bord gauche coloré, fond teinté, bord de la tuile). Le sort affiché à droite (Coup de bouclier) n'est pas marqué dans la grille : pas d'état « sélectionné ».

### 0.2 Métiers
- **Aucune icône de métier n'existe.** `JobsPanel.cs` liste les 12 métiers de `data/jobs.json` sous forme de `Button` texte de 56 px (« Nom   Niveau N » puis « xp a / b »), à 60 % d'opacité quand le métier n'est pas pratiqué, sous deux intertitres (« Récolte », « Artisanat ») à 65 %. Seules les recettes et ressources ont des icônes (`ItemIcon`, glyphes dessinés par le code). Le seul pictogramme « métiers » est celui du menu (lorc/anvil + accent lorc/claw-hammer), réutilisé par la catégorie Métiers des succès.

### 0.3 Talents
- `assets/icons/talents/*.svg` : 59 silhouettes blanches, teintées par l'état dans `TreeNodeView._Draw` (pistache, blanc, blanc à 40 %). Pas de relief.

---

## 1. Icônes de sorts en relief

### 1.1 Principe
Contrairement aux menus (silhouette colorée posée sur une tuile neutre), **la tuile d'un sort porte la couleur de son élément**, parce qu'ici la couleur est une information de jeu (D62). Le relief des menus est repris tel quel sur la silhouette, et la tuile reçoit à son tour le relief des cadres (§4.1 de s157) : ombre portée, contour sombre, liseré, dégradé, reflet.

**Une seule icône par sort, pour toutes les tailles** (40 dans le deck, 44 dans le livre et la barre de combat, 56 dans le détail ; on vise 36 à 64). Deux fichiers par sort : `<id>.svg` (normal) et `<id>-locked.svg` (non débloqué ou indisponible). Les états survol et sélectionné sont dessinés par le code autour de l'icône (§1.6), pas dans le fichier.

### 1.2 La tuile (viewBox 512, unités SVG)
À 44 px, 1 px = 11,6 unités ; à 36 px, 14,2.

| # | Couche | Valeurs exactes |
|---|---|---|
| T1 | Ombre portée | `<rect x="24" y="36" width="464" height="464" rx="80" fill="#000" opacity="0.45"/>` (16 plus bas que le bord : environ 1,4 px à 44) |
| T2 | Contour sombre | `<rect x="24" y="20" width="464" height="464" rx="80" fill="#0d0d0d"/>` (`--osmose-ui-edge`) |
| T3 | Liseré | `<rect x="36" y="32" width="440" height="440" rx="68" fill="{rim}"/>` : 12 unités de contour visibles, puis 20 de liseré (environ 1,7 px à 44) |
| T4 | Corps | `<rect x="56" y="52" width="400" height="400" rx="48" fill="url(#body)"/>`, dégradé diagonal `x1="56" y1="52" x2="456" y2="452"` (unités utilisateur) : `0` = {bodyTop}, `1` = {bodyBottom} |
| T5 | Reflet du corps | le même rect, `fill="url(#bevel)"`, dégradé vertical `y1="52" y2="452"` : `0` = `#fff` opacité 0,22 ; `0.45` = `#fff` opacité 0 |
| T6 | Silhouette | `<g transform="translate(106 96) scale(0.586)">` (300 unités : 26 px à 44, 21 px à 36, 37 px à 64), couches du §1.3 |
| T7 | Marques (facultatif) | §1.5 : accent en haut à droite, pastille d'origine en bas à droite, coin corné en haut à gauche |

Le coin bas gauche reste libre : la barre de combat y écrit le chiffre du raccourci (`Hud.cs`, position (3, 33)).
Pas de `<filter>` (ThorVG) ; les identifiants `body`, `bevel`, `gf`, `r`, `c`, `rim` sont suffixés comme dans `ui_art.layers`.

### 1.3 Deux polarités selon la clarté de l'élément
Un jaune assombri devient olive : la Foudre en tuile sombre se confondrait avec la Boue. Les éléments **clairs** prennent donc une tuile claire et une silhouette sombre, gravée. La règle est calculable : luminance relative de {main} > 0,45 → polarité claire.

**Polarité sombre** (Terre, Feu, Eau, Air, Neutre, Boue, Magma, Vapeur) :
- `rim = main` ; `bodyTop = mix(main, #000, 0.35)` ; `bodyBottom = mix(main, #000, 0.70)`.
- Silhouette, couches identiques à celles des menus (`ui_art.layers`), coordonnées du path d'origine :
  1. ombre : `<g opacity="0.45" transform="translate(12 28)">` path `fill="#000" stroke="#000" stroke-width="44"` ;
  2. contour : path `fill=stroke=mix(main, #000, 0.85)`, `stroke-width="44"`, joints et bouts ronds (environ 0,9 px dehors à 44) ;
  3. remplissage : dégradé `gf` `x1="96" y1="32" x2="416" y2="480"` : `0` = `#ffffff`, `0.5` = `mix(main, #fff, 0.70)`, `1` = `mix(main, #fff, 0.45)` ;
  4. reflet : identique aux menus (clip sur le path, blanc 0,45 → 0,20 → 0).
- Résultat : une silhouette presque blanche, teintée de l'élément vers le bas à droite, sur une tuile foncée de la même famille. Contraste silhouette/corps ≥ 7:1 pour toutes les couleurs du tableau.

**Polarité claire** (Foudre, Glace, Sable) :
- `rim = mix(main, #000, 0.45)` ; `bodyTop = mix(main, #fff, 0.20)` ; `bodyBottom = mix(main, #000, 0.20)`.
- Silhouette gravée :
  1. lèvre de lumière : `<g opacity="0.5" transform="translate(10 18)">` path `fill="#fff"` (la lumière venant d'en haut à gauche accroche le bord inférieur de l'entaille) ;
  2. remplissage : dégradé `gf`, `0` = `mix(main, #000, 0.70)`, `1` = `mix(main, #000, 0.88)` ;
  3. ni contour ni reflet.
- Contraste silhouette/corps : environ 8:1 sur la Foudre.

### 1.4 Couleurs par élément
`main` est le jeton. Les cinq composés et le Neutre n'ont pas encore de jeton : à ajouter à `tokens.css` (bloc « Osmose, les éléments ») et à `ElementStyle.Colour`, le jour où l'enum `Element` les reçoit.

| Élément | Jeton | main | Polarité | Liseré (T3) |
|---|---|---|---|---|
| Terre | `--osmose-earth` | `#b07a45` | sombre | main |
| Feu | `--osmose-fire` | `#ef4b4b` | sombre | main |
| Eau | `--osmose-water` | `#3d9cff` | sombre | main |
| Air | `--osmose-air` | `#52c45a` | sombre | main |
| Neutre | **`--osmose-neutral` (nouveau)** | `#b0b0b0` | sombre (L = 0,43) | main |
| Foudre (Feu + Air) | `--osmose-lightning` | `#f2d03b` | claire | **dégradé des parents** |
| Glace (Eau + Air) | **`--osmose-ice`** | `#8fe0f5` | claire | dégradé des parents |
| Boue (Terre + Eau) | **`--osmose-mud`** | `#8c7a3a` | sombre | dégradé des parents |
| Magma (Terre + Feu) | **`--osmose-magma`** | `#ff7a2e` | sombre | dégradé des parents |
| Vapeur (Feu + Eau) | **`--osmose-steam`** | `#c3a6e0` | sombre (L = 0,44) | dégradé des parents |
| Sable (Terre + Air) | **`--osmose-sand`** | `#d6bc84` | claire | dégradé des parents |

- **Liseré d'un composé** : `fill="url(#rim)"`, dégradé diagonal `x1="36" y1="32" x2="476" y2="472"`, `0` = parent A (le premier de la paire dans `ReactionSpec`), `1` = parent B, dans les **main** des parents. Il apprend sans un mot que la Foudre vient du Feu et de l'Air, et sépare la Boue (liseré brun → bleu) de la Terre (liseré brun uni). Le même liseré sert aux nœuds de paire de l'arbre (§3.3).
- Voisins à surveiller au rendu : Boue et Terre (corps proches, séparés par le liseré) ; Vapeur et le violet du style Tempo (`#b48cff`), qui ne se croisent que dans l'arbre ; Sable et l'or des récompenses (`#ffd166`), le Sable étant plus terne. Si la planche de contrôle (§4) les confond, on corrige `main`, jamais la recette.
- Neutre gris acier plutôt que `#e0e0e0` (actuel) : la tuile d'un sort neutre ne doit pas ressembler à un bouton désactivé. Le gris ne pose pas de problème avec l'état verrouillé, qui se lit par la **platitude** (§1.6), pas par le gris.

### 1.5 Marques
| Marque | Où | Recette | Qui |
|---|---|---|---|
| Accent | haut droit, `<g transform="translate(300 36) scale(0.34)">` (174 unités, environ 15 px à 44) | seconde silhouette, couches des menus (`stroke-width="64"`, ombre `translate(16 36)`) dans les tons de son élément | les 4 Sceaux : la marque d'élément |
| Pastille d'origine | bas droit, cercle centre (424, 424), r 48 | `fill` dégradé vertical light → dark de l'élément d'origine, `stroke="#0d0d0d" stroke-width="12"`, reflet : ellipse (424, 404) rx 30 ry 18, `#fff` opacité 0,35 | les 24 jumeaux de Foudre : rouge si l'original est de Feu, vert s'il est d'Air |
| Coin corné | haut gauche, `<path d="M56 52H184L56 180Z"/>` coupé par le clip du corps, plus un trait `#0d0d0d` de 8 sur l'hypoténuse | couleur du style de la voie (`ElementStyle.StyleColour`) | les sorts de voie (S167) |

Ordre de dessin : T1 à T6, puis le coin corné, l'accent, la pastille.

### 1.6 États
| État | Où | Rendu |
|---|---|---|
| Normal | partout | `<id>.svg`, opacité 1 |
| Non débloqué (niveau) | livre, détail | `<id>-locked.svg` : **plat**, sans T1, T5, ombre ni reflet de silhouette. Corps uni `mix(main, #141414, 0.85)` ; liseré `mix(main, #1f1f1f, 0.60)` (la famille de l'élément reste lisible) ; contour T2 conservé ; silhouette unie `#7a7a7a` (contraste 3,6:1 sur le corps, au-dessus du minimum de 3:1 pour un graphisme) ; marques aplaties de la même façon. **Plus jamais d'opacité 0,4.** |
| Indisponible en combat (PA, portée, recharge) | barre de combat | même `-locked`, plus le compte de recharge actuel (26 px, contour noir). Une seule convention : plat = pas maintenant. |
| Survol | livre, deck, barre | dessiné par `SpellIcon` : anneau 1 px `--chocolat-clair` `#c9a27f` à 2 px du bord, rayon 10 (= `tile-hover`), icône remontée de 1 px |
| Sélectionné | livre (le sort ouvert à droite), deck, barre (sort choisi) | anneau 2 px `--pistache` `#bef374` à 2 px du bord, rayon 10, plus halo 3 px `#bef374` à 0,35 (= `tile-lit`). La barre de combat abandonne `FighterView.PlayerColour` pour cette convention, valable dans tout le jeu. |
| Pressé | barre | icône redescendue de 1 px, ombre T1 masquée (le code dessine alors la texture décalée ; pas de troisième fichier) |
| Case vide du deck | deck, barre | le cadre `frame-well` existant (`#141414`, bord 1 px `#0d0d0d`, rayon 8) |

Livre de sorts : avec des tuiles qui portent l'élément, la carte n'a plus besoin de fond teinté. On garde le `ListRow` neutre et la barre gauche de 3 px de l'élément ; la ligne sélectionnée prend le style `ListRow` sélectionné (`--osmose-ui-selected`, bord pistache).

### 1.7 Lisibilité de 36 à 64 px
- Import Godot : SVG rasterisé à 128 px (échelle d'import 0,25), mipmaps, filtre linéaire. Un seul fichier couvre 36, 44, 48, 56 et 64 px.
- Comme pour les menus, les trous de moins de 44 unités se ferment sous le contour : voulu.
- À 36 px, les marques mesurent encore 6 px (pastille) et 12 px (accent) : elles se lisent comme une tache de couleur, ce qui leur suffit. En dessous de 32 px (journal de combat, infobulles), le code ne dessine que le carré de couleur et la silhouette, sans marque (`SpellIcon.Marks = false` → variante `<id>-small.svg` sans T7, générée par le même outil).
- Silhouettes à vérifier sur la planche (détails fins) : lorc/arrow-cluster, lorc/tornado-discs, lorc/lightning-branches, willdabeast/chain-lightning, delapouite/falling-rocks.

### 1.8 Le jumeau de Foudre
**Silhouette propre, qui reprend le geste de l'original, sur une tuile Foudre, avec la pastille de l'élément d'origine.**

Pourquoi pas la même silhouette avec une marque : la moitié des originaux sont **dessinés en flammes ou en vent** (lorc/flaming-arrow, lorc/candle-flame, lorc/flame-spin, lorc/burning-meteor, lorc/whirlwind...). En jaune, une flamme reste une flamme : le joueur lirait « Feu jaune ». Le geste (flèche, lame, coup, pluie, tourbillon) est conservé, la matière change ; la pastille rouge ou verte relie le jumeau à son original, ce qui aide quand la Conversion remplace le deck automatiquement.

| Jumeau | Nom FR | Original (silhouette actuelle) | Silhouette du jumeau | Pastille |
|---|---|---|---|---|
| lightning-fire-arrow | Flèche d'orage | fire-arrow (lorc/flaming-arrow) | lorc/charged-arrow | Feu |
| lightning-recoil-shot | Tir tonnant | recoil-shot (lorc/arrow-flights) | lorc/lightning-bow | Air |
| lightning-ember-volley | Salve d'éclairs | ember-volley (lorc/fire-ring) | lorc/lightning-trio | Feu |
| lightning-piercing-arrow | Flèche filante | piercing-arrow (lorc/arrow-cluster) | lorc/sonic-lightning | Air |
| lightning-searing-shot | Tir fulgurant | searing-shot (lorc/burning-round-shot) | lorc/thunderball | Feu |
| lightning-gale | Grain orageux | gale (lorc/whirlwind) | lorc/heavy-lightning | Air |
| lightning-ember-rain | Pluie d'éclairs | ember-rain (lorc/burning-meteor) | lorc/lightning-branches | Feu |
| lightning-skyfall | Chute de foudre | skyfall (lorc/lightning-arc → lorc/twister, §1.10) | lorc/thunder-struck | Air |
| lightning-burning-blade | Lame d'orage | burning-blade (delapouite/swords-power) | lorc/thunder-blade | Feu |
| lightning-slash | Taillade fulgurante | slash (lorc/sword-slice) | lorc/lightning-slashes | Air |
| lightning-fire-lunge | Fente foudroyante | fire-lunge (lorc/fire-dash) | lorc/deadly-strike | Feu |
| lightning-wind-blade | Lame crépitante | wind-blade (lorc/wind-slap) | lorc/lightning-saber | Air |
| lightning-smite | Coup de tonnerre | smite (lorc/fire-punch) | lorc/thor-fist (repris à titan-blow) | Feu |
| lightning-cyclone-blade | Tourbillon d'orage | cyclone-blade (lorc/tornado) | lorc/lightning-helix | Air |
| lightning-inferno-blade | Lame de foudre | inferno-blade (lorc/fire-ring → lorc/fire-axe) | lorc/zeus-sword | Feu |
| lightning-hurricane | Fureur d'orage | hurricane (lorc/tornado-discs) | lorc/lightning-dissipation | Air |
| lightning-flare | Crépitement | flare (lorc/candle-flame) | lorc/static | Feu |
| lightning-gust | Souffle d'orage | gust (lorc/wind-hole) | lorc/static-waves | Air |
| lightning-burn | Électrocution | burn (lorc/flame-spin) | lorc/lightning-shadow | Feu |
| lightning-bolt | Éclair | bolt (lorc/focused-lightning → lorc/crossed-air-flows) | lorc/focused-lightning (repris à bolt) | Air |
| lightning-meteor | Foudre céleste | meteor (lorc/meteor-impact) | lorc/power-lightning | Feu |
| lightning-cyclone | Œil de l'orage | cyclone (lorc/stomp-tornado) | lorc/bolt-eye | Air |
| lightning-cataclysm | Fulgurance | cataclysm (lorc/bright-explosion) | delapouite/bolt-bomb | Feu |
| lightning-tempest | Orage | tempest (lorc/lightning-storm → lorc/entangled-typhoon) | willdabeast/chain-lightning | Air |

- 24 silhouettes distinctes, aucune déjà prise par un sort ; aucune ne recoupe un talent (lorc/lightning-arc, lorc/lightning-storm et lorc/lightning-tree restent aux talents Électrisation, Transmutation Air et Conversion).
- Nouvel auteur à créditer : **willdabeast**.
- `spells.json` : le champ `icon` des 24 jumeaux passe à ces silhouettes ; `spells/credits.json` les reçoit.

### 1.9 Les sorts de l'arbre
| Sort | Élément | Silhouette | Accent | Remarque |
|---|---|---|---|---|
| relay (Relais) | Neutre | lorc/back-forth | aucun | Conseil : le talent `relay-talent` quitte lorc/relic-blade pour la même silhouette (un talent et le sort qu'il donne se reconnaissent) |
| seal-earth (Sceau de Terre) | Neutre | lorc/rune-stone (= talent `seal`) | lorc/stone-sphere, tons Terre | l'accent reprend le Cœur de l'élément |
| seal-fire | Neutre | lorc/rune-stone | lorc/fire-ring, tons Feu | idem |
| seal-water | Neutre | lorc/rune-stone | lorc/drop, tons Eau | idem |
| seal-air | Neutre | lorc/rune-stone | lorc/whirlwind, tons Air | idem |
| decoy (Leurre) | Neutre | lorc/scarecrow (= talent `decoy-talent`) | aucun | |

La tuile suit `spell.Element` (Neutre) pour rester cohérente avec le filtre « Neutre » du livre ; l'élément du sceau est porté par l'accent.

### 1.10 Les 72 sorts de classe : on garde, sauf six
On garde 66 silhouettes. On change :
| Sort | Élément | Actuelle | Nouvelle | Raison |
|---|---|---|---|---|
| bolt (Trait de vent) | Air | lorc/focused-lightning | lorc/crossed-air-flows | un éclair sur un sort d'Air ; la silhouette passe à son jumeau |
| skyfall (Chute céleste) | Air | lorc/lightning-arc | lorc/twister | idem |
| tempest (Tempête) | Air | lorc/lightning-storm | lorc/entangled-typhoon | idem, et doublon avec un talent |
| titan-blow (Coup de titan) | Terre | lorc/thor-fist | lorc/punch-blast | poing électrique sur un sort de Terre ; il passe à `lightning-smite` |
| inferno-blade (Lame infernale) | Feu | lorc/fire-ring | lorc/fire-axe | doublon de `ember-volley` |
| field-dressing (Pansement) | Neutre | delapouite/healing | lorc/bandage-roll | doublon de `mend` |

À juger sur la planche, sans changement d'office : `elemental` (lorc/fire-silhouette, une flamme sur un sort neutre) et `freeze` (lorc/frostfire, mi-feu mi-glace sur un sort d'Eau).

### 1.11 Sorts de voie (S178) et de monstres
- Voies (9) : même recette ; tuile de l'élément du sort ; coin corné de la couleur du style (§1.5) ; silhouette absente de toutes les listes ci-dessus. Je les choisirai sur la liste du GD.
- Monstres : pas de dessin par sort pour l'instant. Le repli « première lettre » disparaît au profit d'un **emblème d'élément** : tuile de l'élément et silhouette du Cœur (lorc/stone-sphere, lorc/fire-ring, lorc/drop, lorc/whirlwind ; Neutre : lorc/swirl-ring, libre). Fichiers `spells/_element-<Element>.svg`.

### 1.12 Implémentation
- Nouvelle fonction `spell_tile(tone, glyph, marks)` dans `tools/ui_art.py`, ou outil voisin `tools/spell_art.py` qui importe `layers` et `mix` : un seul endroit pour la recette du relief.
- Entrée : `spells.json` (id, element, icon, twin, et la voie à venir) et `spells/credits.json`. Les paths : `bar/shapes/<auteur>_<nom>.path` comme les menus ; un petit script extrait le `d` des SVG blancs déjà présents dans `spells/` et `talents/` (même source game-icons) et télécharge les nouveaux.
- Sortie : `godot/assets/icons/spells/<id>.svg`, `<id>-locked.svg`, `<id>-small.svg` (remplace les silhouettes blanches ; elles restent dans `shapes/`).
- `SpellIcon._Draw` : plus de `StyleBoxFlat` ; `DrawTextureRect` sur tout le rectangle (`<id>` ou `<id>-locked` selon `Faint`), puis les anneaux de survol et de sélection. `Faint` ne touche plus l'opacité.
- Tests du script (comme S157) : chaque sort visible (72 + 24 + 6) a ses trois fichiers ; aucun `<filter>` ; viewBox 512 ; couleurs dérivées des jetons ; silhouettes uniques parmi les sorts de joueur ; chaque jumeau a sa pastille, de la couleur de l'élément de son `twin`.

---

## 2. Icônes de métiers

### 2.1 Recette
Exactement celle des menus (s157 §1) : silhouette colorée en relief sur fond transparent, posée sur la tuile neutre `tile` (48 × 48, icône 36). La couleur sert à distinguer les métiers, pas à porter un sens de jeu : une teinte par métier, prise sur la matière de l'objet. Tons dérivés avec la formule de s157 §1.3 (`light = mix(main, #fff, 0.45)`, `dark = mix(main, #000, 0.45)`, `outline = main × 0,20`). Entrées `job-<id>` dans `relief/palette.json`, générées par `ui_art.py` avec leur variante `-locked` (la fonction `grey` existe).

### 2.2 Silhouettes et couleurs
| Métier | id | Famille | Silhouette | main | Matière |
|---|---|---|---|---|---|
| Bûcheron | woodcutter | Récolte | lorc/wood-axe | `#d09a5c` | bois clair |
| Mineur | miner | Récolte | lorc/mining | `#a9bccd` | acier |
| Pêcheur | fisher | Récolte | delapouite/fishing-pole | `#6bb8f2` | eau claire |
| Paysan | farmer | Récolte | lorc/wheat | `#e3c27a` | blé |
| Herboriste | herbalist | Récolte | delapouite/herbs-bundle | `#52c45a` | feuille |
| Serrurier | locksmith | Artisanat | lorc/skeleton-key | `#d4a94a` | laiton |
| Forgeron | smith | Artisanat | lorc/sword-smithing | `#e8803d` | métal chauffé |
| Tailleur | tailor | Artisanat | lorc/sewing-needle | `#ff7eb6` | fil |
| Bijoutier | jeweller | Artisanat | lorc/cut-diamond | `#4fd1c5` | gemme |
| Cordonnier | cobbler | Artisanat | lorc/leather-boot | `#a0522d` | cuir |
| Alchimiste | alchemist | Artisanat | lorc/round-bottom-flask | `#b48cff` | philtre |
| Boulanger | baker | Artisanat | delapouite/bread | `#f2cc94` | croûte |

lorc/anvil n'est pas employé : il reste l'icône du menu Métiers.

### 2.3 Ligne de métier (`JobsPanel`, colonne gauche de 360)
```
+--------------------------------------------------------+
| +------+  Bûcheron                           Niv. 12   |  h 64
| | ICON |  [=============---------]   340 / 900         |
| +------+                                               |
+--------------------------------------------------------+
  12 | 48 | 12 | texte et barre (ProgressBar compacte, h 8) | 12
```
- Nom 16 px gras `#e0e0e0`, niveau 14 px `#cccccc` aligné à droite, XP 14 px `#9d9d9d` à droite de la barre ; remplissage `fill-progress` (pistache).
- Non pratiqué : icône `-locked`, nom `#9d9d9d`, barre vide. Plus d'opacité 0,6 sur toute la ligne.
- Sélectionné : `ListRow` sélectionné (fond `#33421f`, bord gauche 3 px pistache) ; survol `#2d2d2d`.
- Intertitres « Récolte » et « Artisanat » : 14 px gras, `--chocolat-clair` `#c9a27f`, capitales, comme le titre du panneau Menus.
- Ces icônes servent aussi aux ateliers (infobulle, carte) et aux succès de métier, le jour venu.

---

## 3. Le langage visuel de l'arbre

### 3.1 Relecture de la capture `tree.png`, du plus visible au plus fin
1. **Ce n'est pas un arbre, c'est un organigramme à l'envers.** Trois bandes empilées (styles en haut, fourche au milieu, tronc en bas) et des colonnes régulières. Le tronc, trois disques isolés en bas, n'est relié à rien de visible (pointillés à 12 % d'opacité). On ne voit ni d'où ça pousse, ni où l'on en est.
2. **Six croix rouges géantes** (Air, Feu, Glace, Magma, Vapeur, Sable) sont l'élément le plus saillant de l'écran, alors qu'elles disent seulement « tu as choisi autre chose ». Le rouge doit rester réservé à l'alerte (ce que tu vas perdre).
3. **Quatre langages de liens** : traits pistache pleins de 2 et 3 px, traits gris pleins à 60-70 %, pointillés blancs à 25-30 %, traits rouges ; s'y ajoutent l'accolade chocolat sous les paires et les cadres blancs à 25 % autour des choix. Le bus pistache horizontal (y ≈ 485), le trait le plus contrasté de l'écran, n'est qu'un rail de dépendance.
4. **La moitié haute est faite de fantômes** : sept carrés pointillés « ··· » et deux rangées de cercles pointillés « ··· » pour des styles non ouverts. Bruit pur.
5. **Libellés tronqués** (« Conversion : … », « Signature … », « Poussée l… », « Distance · é… », « Au plus p… ») et trop petits (12 px), estompés par une opacité de 0,6.
6. **Trois halos concurrents** : un carré pistache autour de nœuds ronds (focus), un anneau doré (chemin), un anneau pistache pointillé (plan).
7. **Le fond** : deux anneaux concentriques (pistache 5 %, chocolat 12 %) forment une cible qui traverse les nœuds sans rien dire.
8. **Les chiffres** : le « 2 » du coût (12 px doré dans un coin) et le niveau (11 px) sont illisibles.
9. **Icônes plates teintées** : la silhouette pistache sur fond vert sombre du nœud pris manque de contraste ; aucun relief, alors que les menus en ont maintenant.
10. **La couleur des éléments**, cœur du build, se limite à des pastilles de 5 px de rayon sous les nœuds.
11. **Six états dans la légende**, dont trois visuellement identiques (verrouillé, trop bas niveau, à venir : cercles gris pointillés), plus un glossaire qui défile dans la même colonne.
12. Barre d'outils : titre tronqué (« Arbre des talen… »), champ tronqué (« Code de buil »), cinq boutons de même poids ; légendes flottantes (« Styles : une clé par style, au niveau 45 », « La fourche : … », « Tronc, gratuit ») posées dans le dessin comme des étiquettes de schéma.

### 3.2 La métaphore et une seule convention
**L'arbre pousse de bas en haut. Le lien, c'est la branche elle-même. Ce qui est pris est parcouru de sève pistache.**

- **Sol** : une bande de 96 px en bas, dégradé de `--osmose-ground` `#1b1b1b` vers `#231a13` (`mix(#1b1b1b, #5a3a22, 0.15)`) ; une ligne de 2 px `--osmose-ui-frame-dark` `#3a2616` où le tronc sort de terre. Plus d'anneaux.
- **Bois** : chaque lien est une courbe de Bézier cubique, du haut du parent au bas de l'enfant, avec les points de contrôle verticaux (`(parent.x, y_milieu)`, `(enfant.x, y_milieu)`) : un S organique au lieu d'angles droits. Elle s'**affine** du parent vers l'enfant.
- **Sève** : un filet pistache au cœur du bois, uniquement sur les segments dont les deux bouts sont pris.
- Il n'y a plus de pointillés dans le dessin, ni de bus, ni d'accolade, ni de cadre de choix.

### 3.3 Le bois (Godot : `Line2D` avec `width_curve`, `joint_mode = Round`, `begin_cap_mode = end_cap_mode = Round`, `antialiased = true`, courbe échantillonnée par `Curve2D.Tessellate`)
| Ordre | Largeur départ → arrivée | Usage |
|---|---|---|
| Tronc | 28 → 20 | du sol à la fourche |
| Charpentière | 14 → 10 | de la fourche aux grands choix (paire, Cœur, bascules) |
| Branche | 10 → 6 | vers un style ou une tresse |
| Rameau | 6 → 4 | d'un talent à l'autre |

Chaque segment est fait de trois `Line2D` superposés :
1. ombre : largeur + 4, `#3a2616` (`--osmose-ui-frame-dark`) opacité 0,6, décalée de (0, 2) ;
2. écorce : largeur, `--chocolat` `#5a3a22` ;
3. reflet : 35 % de la largeur, `#7a5434` (`--osmose-ui-frame`), décalé de −20 % de la largeur vers la gauche (lumière en haut à gauche).

| État du segment | Rendu |
|---|---|
| Pris (deux bouts pris) | le reflet 3 est remplacé par la **sève** : `max(3, 35 % de la largeur)`, `--pistache` `#bef374`, plus une lueur (largeur + 6, `#bef374` opacité 0,15) |
| Accessible (parent pris, enfant disponible) | écorce normale, sans sève |
| Pas encore accessible | le segment entier à 45 % d'opacité (modulate), jamais pointillé |
| Exclu (l'autre branche d'un choix est prise) | **branche élaguée** : la courbe s'arrête à 70 % de sa longueur, bout plat (`end_cap_mode = None`) et une petite coupe : ellipse 6 × 4 `#7a5434` |
| Perdu (aperçu d'un changement) | sève **corail** `--osmose-losing` `#f08070` sur les segments qui seraient perdus ; seulement pendant l'aperçu |
| Chemin (survol d'un nœud lointain) | sève pistache à 50 % sur le trajet manquant, en pointillés de 6 px : le seul pointillé de l'écran, éphémère, qui veut dire « si tu prends tout ça » |

Un **choix exclusif** (Transmutation, paire ou Cœur, grande bascule) se dessine comme une vraie fourche : la branche arrive sur un **nœud de bois**, un losange de 14 px `#7a5434` bordé de 2 px `#3a2616`, d'où partent les rameaux des options. Infobulle du losange : « Un seul au choix ». Il remplace les cadres blancs et l'accolade chocolat.

### 3.4 Les nœuds
Formes (la structure dira lesquelles existent) :
| Forme | Taille | Usage |
|---|---|---|
| Cerne (disque incrusté dans le tronc) | 44 | nœuds du tronc, gratuits, au niveau ; non cliquables |
| Médaillon (disque) | 56 | talent |
| Écusson (carré arrondi, rayon 14) | 64 | clé de style ; cerclage doré (récompense), gemme 12 px de la couleur du style en haut au centre |
| Grand médaillon | 72 | grands choix de la fourche (paire, Cœur, Conversion) : liseré intérieur de 3 px de la couleur de l'élément, **dégradé des deux parents pour une paire** (même règle que le liseré des sorts composés, §1.4) |
| Bourgeon | 16 | ce qui viendra plus tard (« à venir ») : disque `#2a1d12`, bord 1 px `#7a5434`, ni libellé ni survol |

Médaillon en relief, dessiné par des textures SVG 9-patch ou disque générées par `ui_art.py` (`node-<état>.svg`, `key-<état>.svg`), même recette que les cadres (s157 §4.1) :

| État | Corps | Cerclage | Icône | Ombre | Libellé (14 px) |
|---|---|---|---|---|---|
| **Pris** | `--osmose-ui-lit-top` → `lit-bottom` (`#3d4f24` → `#25301a`) | 3 px `#bef374` + halo 3 px `#bef374` 0,35 | en relief, couleur | (0, 2) `#000` 0,45 | `#e0e0e0` gras |
| **Disponible** | `raised-top` → `raised-bottom` (`#3a3a3d` → `#262628`) | 2 px `#e0e0e0` | en relief, couleur | oui | `#e0e0e0` |
| **Verrouillé** (parent, points, porte) | `--osmose-ui-well` `#141414` plat | 2 px `#4c4c4c` | `-locked` (gris plat) | non (creux) | `#9d9d9d` |
| **Trop bas niveau** | comme verrouillé | comme verrouillé | `-locked` | non | `#9d9d9d`, plus la **pastille de niveau** |
| **Exclu** | `#141414` plat | 2 px `--osmose-ui-frame-dark` `#3a2616` | `-locked` à 40 % | non | `#9d9d9d` ; sa branche est élaguée |
| Perdu (aperçu) | `--osmose-losing-fill` `#3a2424` | 3 px `#f08070` | couleur | oui | `#f08070` |

- **Quatre états à apprendre** (pris, disponible, verrouillé, exclu), la pastille de niveau en plus. « Exclu » se lit par la branche coupée et le cerclage chocolat, **sans croix ni rouge**. « À venir » sort de la légende : c'est un bourgeon.
- **Pastille de niveau** : 32 × 20, à cheval sur le bas du nœud, fond `#141414`, bord 1 px `#5c5c5c`, rayon 10, texte « 45 » en 14 px gras `#9d9d9d`.
- **Pastille de coût 2** : disque 20 px en haut à droite, débordant de 4 px, dégradé `--osmose-ui-gold-top` → `--osmose-ui-gold-bottom`, bord 2 px `--chocolat`, « 2 » en 14 px gras `--osmose-ui-gold-text` `#3a2410`.
- **Disponible quand il reste des points** : le halo pistache respire (opacité 0 → 0,35 → 0, 1,6 s). C'est le seul mouvement de l'écran : il désigne ce que l'on peut faire maintenant, point focal de la fenêtre avec le compteur de points.
- **Survol** : cerclage `--chocolat-clair` `#c9a27f` (sauf un nœud pris, qui reste pistache), nœud remonté de 1 px, main ; infobulle et panneau de droite comme aujourd'hui.
- **Focus clavier** : anneau 2 px `#ffffff` à 4 px du bord, **de la forme du nœud** (fini le carré autour d'un rond).
- **Plan** (fantôme N2 a) : badge 20 × 20 pistache en haut à gauche portant l'**ordre** de prise (« 1 », « 2 »…), 14 px gras `--chocolat` (même badge que les menus). Remplace l'anneau pointillé et dit en plus dans quel ordre prendre.
- **Équipe** (deux petites têtes roses) : deviennent un accent en relief (delapouite/three-friends, palette `contacts`) en haut à gauche, 20 px, quand il n'y a pas de badge de plan.
- **Libellés** : 14 px, sous le nœud, largeur 104 (nœud + 24 de chaque côté), **deux lignes au plus, jamais de « … »**. Les noms trop longs sont à raccourcir par le scénariste (liste au §3.7). Couleur et non opacité.

### 3.5 Les icônes de talents en relief
Même recette que les menus (silhouette colorée sans tuile ; le médaillon fait office de tuile), deux fichiers par talent : `talents/<id>.svg` et `<id>-locked.svg` (fonction `grey`), plus de teinte par le code. Couleur par famille :
- grands choix de la fourche : l'élément ou le composé (§1.4) ;
- talents de style : la couleur du style (`ElementStyle.StyleColour`) ;
- tronc : l'or des succès (`#ffd166` / `#ffe6ab` / `#8c7338` / `#332a14`) ;
- tresses et autres : parchemin (`#f0e6cc`, palette `journal`).

Jamais pistache pour une icône : le pistache signifie « pris ». Les 59 silhouettes actuelles sont conservées, à une exception près : `relay-talent` passe à lorc/back-forth (§1.9).
Collisions de couleur connues mais tolérées, car séparées dans l'espace : Distance `#6bb8f2` / Eau `#3d9cff`, Soutien `#ff7eb6` / soin, Alchimie `#4fd1c5` / Glace `#8fe0f5`.

### 3.6 Ce qui disparaît de l'écran actuel
- Les deux anneaux de fond.
- Le bus pistache horizontal et tous les traits droits, gris ou pointillés.
- Les croix rouges ; le rouge n'existe plus qu'en aperçu de perte.
- Les carrés et cercles fantômes « ··· » des styles non ouverts. Un style non ouvert = une branche nue, terminée par un **écriteau** : son nom en 14 px gras de sa couleur (contraste ≥ 4,5:1 sur `#1b1b1b` pour les six), sur une plaquette `#141414` au bord 1 px de sa couleur, plus ses bourgeons.
- Les cadres blancs des choix et l'accolade chocolat des paires (remplacés par la fourche et le losange).
- Le carré de focus, l'anneau doré de chemin, l'anneau pointillé de plan.
- Les pastilles d'élément de 5 px (l'élément est dans l'icône et dans le liseré).
- Les légendes flottantes dans le dessin (« Styles : … », « La fourche : … », « Tronc, gratuit ») : elles passent dans l'aide (« ? ») et dans les infobulles des écriteaux.
- La légende à six états et le glossaire défilant de la colonne droite : légende de 4 pastilles (pris, disponible, verrouillé, exclu) plus la pastille de niveau et le losange « un seul au choix » ; le glossaire passe dans l'aide.
- Barre d'outils : le titre de la fenêtre va dans la `TitleBar` du thème (18 px, sans troncature) ; un seul bouton principal (Enregistrer) ; le compteur « 2 points à dépenser » devient un badge pistache au texte chocolat (« 2 ») suivi du libellé en `#e0e0e0` : avec le halo qui respire, c'est le point focal.

### 3.7 Textes à faire écrire par le scénariste (FR/EN)
- Noms de nœuds qui dépassent deux lignes de 104 px en 14 px : à recenser sur la structure du GD ; déjà tronqués aujourd'hui : « Conversion : Foudre », « Signature forte », « Poussée lourde », « Au plus près », « Distance · … », « Corps à corps · clé » (« · clé » disparaît : l'écusson le dit).
- `tree.choice-one` : « Un seul au choix » / « Choose one ».
- `tree.pruned` (infobulle d'un nœud exclu) : « Fermé par ton choix : {nom} » / « Closed by your choice: {name} ».
- `tree.sign.<style>` (infobulle d'un écriteau) : une phrase par style, reprise de la légende flottante actuelle.

### 3.8 Comportement à 1280 × 720 et en fenêtre réduite
Ce qui ne dépend pas de la structure : les nœuds gardent leur taille (cibles ≥ 44) ; le dessin défile verticalement (molette, glisser) et s'ouvre **calé en bas**, tronc et fourche visibles, couronnes des styles au-dessus ; la colonne de détail garde 300 px. Le reste attend la proposition du GD.

---

## 4. Ordre de livraison conseillé
1. **Les sorts, d'un coup** (le bogue d'abord) : recette dans `ui_art.py`, les 102 tuiles (72 de classe, dont les 6 silhouettes changées, 24 jumeaux, 6 sorts de l'arbre) avec leurs variantes `-locked` et `-small`, `SpellIcon` qui dessine la texture, `credits.json` et le champ `icon` des jumeaux mis à jour, tests du script. Plus une **planche de contrôle** (une grille de toutes les tuiles à 36, 44 et 64 px, rendue sans fenêtre par Godot sous écran virtuel) que je relis avant Charles. Charles teste : le livre de sorts de Plouf, puis une Conversion Foudre (les 8 sorts doivent avoir leur icône), puis un combat.
2. **Les états** : survol, sélectionné (livre, deck, barre de combat avec la nouvelle convention pistache), indisponible en combat, cases vides ; livre de sorts sans fond teinté. Petit pas, testable dans les mêmes écrans.
3. **Les métiers** : 12 icônes et leurs `-locked`, nouvelle ligne de `JobsPanel` avec barre d'XP. Indépendant, testable en ouvrant la fenêtre des métiers.
4. **Les icônes de talents en relief** (59 + `-locked`) et les médaillons d'état (`node-*.svg`, `key-*.svg`) : faisables avant la structure du GD, ils améliorent déjà l'arbre actuel (plus de croix rouges, quatre états lisibles).
5. **L'arbre** sur la structure du GD : sol, bois et sève en `Line2D`, fourches et losanges, écriteaux, légende réduite, barre d'outils. Je fais la maquette ASCII aux dimensions réelles dès réception de la proposition.
6. Plus tard : les 9 sorts de voie (S178) et l'emblème d'élément des sorts de monstres.
