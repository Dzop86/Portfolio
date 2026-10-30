# L'arbre lisible : refonte de la présentation de la fenêtre de l'arbre *(proposition du game designer, 30 octobre 2026)*

> **Demande de Charles (30 octobre)** : « l'arbre des talents est illisible, il mérite une refonte, de ressembler plus à un arbre. Là on distingue mal l'arborescence avec les chemins rouges, les chemins verts, on ne comprend rien. »
>
> **Périmètre** : la **présentation** seulement (`TreeLayout`, `TreePanel`, la légende, le panneau de droite). Les talents, leurs coûts, leurs parents, leurs exclusions et leurs niveaux ne changent pas. Aucun changement de règle n'est nécessaire (§2.9) ; un changement minime possible est décrit à part, et je ne le recommande pas.
>
> **Référence du modèle** : le graphe de D82 (T114, `data/talents.json`), pas les sept colonnes de `talents-arbre.md` v3 §4.1, qui sont périmées pour la fenêtre. Ce graphe comprend le tronc (30, 35, 40), la fourche à sept portes (six paires et l'Élément pur avec ses quatre Cœurs), la tresse de Foudre (cinq nœuds) et six styles (Alchimie 6 nœuds, Mêlée 4, Distance 4, Tempo 9, Terrain 10, Soutien 8). Il compte 56 talents et 3 nœuds de tronc.
>
> **Couleurs** : le graphiste en décide. Ce document fixe les **états**, la **hiérarchie visuelle** (ce qui doit se voir d'abord) et les **formes**.

---

## 1. Pourquoi l'arbre est illisible aujourd'hui

Ce diagnostic s'appuie sur la capture du 30 octobre (un héros de 45 qui a Foudre, Boue, la Conversion et Prisme, avec 2 points libres, la Mêlée déployée et la première bulle du tutoriel ouverte) et sur le code (`TreeLayout.cs`, `TreeCanvas._Draw`, `TreeShapes`).

### 1.1 Trop de formes, et la moitié ne dit rien

| Ce qu'on voit | Nombre | Information portée |
|---|---|---|
| Nœuds du tronc | 3 | faible (automatiques, aucune décision) |
| Portes (6 paires et 4 Cœurs) | 10 | forte |
| Tresse de Foudre | 5 | forte |
| Tiges « à venir » des autres tresses (`coming:`) | 5 | **nulle** (pointillés sans texte) |
| Style déployé (Mêlée) | 4 | forte |
| Étages repliés des cinq autres styles (`fold:Style:Tier`, « ··· ») | 14 | **quasi nulle** (« Distance · clé », « Tempo · é... ») |
| **Total** | **41 formes** | **19 sans information (46 %)** |

En plus des formes, il y a 3 cadres (Transmutation, Cœurs, et la bulle), 3 accolades chocolat sous les couples, 2 anneaux de fond et 5 légendes éparpillées (« Styles : une clé par style, au niveau 45 », « Transmutation (au choix) », « La fourche : une paire... », « Tronc, gratuit », « Élément pur : un seul Cœur ») en 11 à 13 px et à 60 % d'opacité. Au moins **8 noms sont tronqués** (« Conversion : ... », « Signature ... », « Poussée l... », « Au plus p... », « Distance · é... ») : la colonne fait 64 px et le nom n'a qu'une ligne.

### 1.2 Les liens mentent sur la structure

- **Le bus vert.** Il suffit d'une porte prise pour que la ligne horizontale au-dessus de la fourche s'allume en pistache (`DrawLine(... lit ...)` dès que `doorsTaken.Length > 0`). Elle traverse les sept colonnes. **Le vert veut dire « pris » dans la légende, mais ici il veut dire « atteignable ».** C'est le « chemin vert » qu'on ne comprend pas.
- **La tresse se lit à l'envers.** La Conversion (une clé) est dessinée tout en haut, au-dessus de Prisme et de Signature. Pourtant elle ne dépend que de Foudre et du niveau 45, alors que Prisme et Signature dépendent d'elle *ou* d'une Transmutation. Le long trait pistache qui part de Foudre monte jusqu'à la Conversion en passant entre Signature et Prisme : l'œil en conclut que la Conversion est leur parent, ou l'inverse. Le code le sait déjà (« a parent above its child (the Conversion over Prism) is not drawn back down »).
- **Les « n'importe lequel » sont dessinés comme des « tous ».** Dans un style, un nœud demande *un* nœud de l'étage du dessous (`requiresAny`), mais `Links()` trace **chaque** arête parent-enfant. La Mêlée, qui est le style le plus simple, n'en a que 3. Un Terrain déployé en a **24** (12 vers l'étage 2, 12 vers les clés), le Tempo 18, le Soutien 16, et elles se croisent. La capture montre la Mêlée : on n'y voit que le moins grave.
- **Les styles ne pendent de rien.** Puisqu'un style demande « n'importe quelle porte », aucune porte n'est son parent visible. Le bus remplace l'arbre par un organigramme.
- **Le tronc ne ressemble pas à un tronc.** Ses trois nœuds sont posés à plat sous la fourche, avec des pointillés à 12 % qui ne rejoignent rien. Ils se lisent comme trois bonus indépendants, et c'est l'élément le **plus large** en bas de l'écran, alors qu'un tronc est étroit.

### 1.3 Les états : neuf codes visuels, et la hiérarchie inversée

Le joueur doit décoder 5 états (pris, disponible, verrouillé, exclu, trop bas), plus « à venir », « à rendre » (en rouge), le fantôme du plan (anneau pistache pointillé), le chemin survolé (anneau) et le focus clavier (carré). **Neuf codes** en tout. Et :

- **Le rouge sert deux fois.** Il marque les nœuds exclus (6 grosses croix sur la capture) **et** ce qu'on va perdre en rendant un talent. Ce sont les « chemins rouges ».
- **Ce qu'on ne peut pas prendre est ce qui se voit le plus.** Les 6 croix rouges pleines sont les formes les plus saturées de l'écran, plus que les nœuds pris. La hiérarchie est à l'envers : l'œil doit aller d'abord à ce qui est à moi, puis à ce que je peux prendre, et ignorer le reste.
- **« Verrouillé » et « trop bas » se ressemblent** (deux pointillés), mais la légende les sépare.
- **« Verrouillé (points) » éteint tout l'arbre à 0 point.** Un héros qui a tout dépensé voit chaque nœud en pointillés, y compris ceux que sa prochaine montée de niveau lui ouvrira. Le manque de points est une condition globale (un compteur), pas une propriété de 50 nœuds.

### 1.4 Les colonnes de styles vides

Cinq styles sur six sont repliés en trois « ··· » empilés, avec des noms tronqués et leurs liens en pointillés. Ces cinq colonnes occupent **plus de la moitié de la largeur** pour dire seulement « il y a quelque chose ici ». Une colonne repliée n'indique même pas si le style est ouvert, fermé (la Distance, fermée par Corps à corps) ou vide.

### 1.5 La fourche

Dix nœuds sur une ligne, en trois groupes visuels (les accolades des couples, le cadre des Cœurs, les espaces). Les couples (C7) ne se lisent pas : rien n'explique pourquoi Foudre est à côté de Boue. Quatre croix rouges sur six paires (celles qui partagent un élément avec Foudre) attirent l'œil sur ce qui est fermé. La fourche est pourtant **la** décision de 30.

### 1.6 La légende, la barre du haut, le panneau de droite

- **La barre du haut** a cinq outils au même poids que le titre (Build suggéré, Planifier au niveau 60, le champ de code, Charger, Copier). Le titre est tronqué (« Arbre des talen... »), le champ aussi (« Code de buil »).
- **Le panneau de droite** empile treize blocs : points, éléments hors base, bascule, titre du build, contreparties, meilleur coup, un champ vide, « ? », la légende, le glossaire, les raccourcis, puis trois gros boutons. **L'information la plus utile (« 2 points à dépenser ») est écrite en petit.**
- **La légende est cachée derrière « ? »** (M5 b). Elle n'est ouverte que pendant le tutoriel, et elle explique 6 états, dont deux qu'on ne sait pas distinguer.
- **La bulle du tutoriel** couvre le centre de l'arbre (le bus et les premiers étages) et parle du tronc, l'élément le moins utile, pendant qu'elle cache le reste.

### 1.7 Ce qui est bon et qu'on garde

La lecture de bas en haut, les formes par type (rond, grand rond pour une porte, carré pour une clé), les icônes, les pastilles d'élément, la raison d'un refus au survol (`TalentReasons.WhyNot`), le chemin et son coût au survol (`TreePlan.Path`), la confirmation « Rendre / Garder », le plan au niveau 60, le titre du build, le clavier et le principe de tout faire tenir dans 1280 × 720 sans zoom (K3 a, K6 a).

**En une phrase :** l'arbre affiche toutes ses règles en même temps, au même volume, avec des liens qui décrivent la logique du moteur (n'importe quelle porte, n'importe quel nœud du dessous) au lieu de la forme qu'on doit voir.

---

## 2. La structure proposée : l'espalier

### 2.1 L'image

Un **arbre en espalier**, comme un poirier palissé contre un mur : un tronc étroit en bas, une couronne où il se divise, puis des branches qui montent droit, chacune dans son couloir, avec des rameaux courts qui portent les nœuds. On garde ainsi la silhouette d'un arbre (Charles), et une grille stricte (lisibilité, pas de croisement possible).

**Ce qui le fait vivre : une branche pousse quand on y met des points.** Une branche où rien n'est pris est un **bourgeon** (un seul nœud). Une branche où l'on a pris quelque chose est **déployée** (tous ses nœuds). À 30, l'arbre est un jeune plant ; à 100, une couronne. La forme de l'arbre devient le résumé du build, ce qui donne une boucle macro visible d'une session à l'autre.

### 2.2 Ce qui est quoi

| Partie de l'arbre | Ce qu'elle porte | Pourquoi |
|---|---|---|
| **Tronc** (en bas, au centre, vertical) | 30, 35, 40, de bas en haut | automatique : il est le pied, pas une décision |
| **Fourche** (la ligne des portes, au-dessus du tronc) | 6 paires (3 couples) et l'Élément pur (4 Cœurs, « 1 au choix ») | la première décision, au centre de l'écran |
| **Couronne** (un point unique au-dessus de la fourche) | le départ de toutes les branches de style | un style demande « n'importe quelle porte » : les portes prises nourrissent la couronne, la couronne nourrit les styles |
| **Branches** | **7** : la **Fusion** (la tresse de Foudre) et les **6 styles** | une branche = un couloir vertical |
| **Étages** | les lignes de la grille : étage 1, étage 2, clés | « un nœud s'ouvre quand l'étage du dessous de **sa** branche a un nœud pris » : c'est la seule règle à lire |

**Les paires ne sont pas des branches.** Ce sont des portes, au même titre que les Cœurs. **La Fusion est une branche à part, qui pousse directement de sa porte** (Foudre), à gauche, juste au-dessus d'elle. Ce n'est pas un style.

### 2.3 Le sens de lecture

De **bas en haut**, comme aujourd'hui : le tronc, la fourche, la couronne, puis les étages 1, 2 et les clés. Les **niveaux** montent avec la hauteur : le tronc couvre 30 à 40, les clés sont à 45. Une **gouttière** à gauche nomme chaque ligne : « Tronc », « Portes », « Étage 1 », « Étage 2 », « Clés · niv. 45 · 2 pts ». Ces cinq libellés fixes remplacent les cinq légendes éparpillées.

### 2.4 La grille (1280 × 720)

**L'écran :**

| Zone | x | y | Taille |
|---|---|---|---|
| Barre du haut | 0 à 1280 | 0 à 52 | titre, compteur de points, Outils, ?, Annuler, Enregistrer, Fermer |
| Arbre (le canevas) | 16 à 948 | 60 à 704 | 932 × 644 |
| Panneau de droite | 960 à 1264 | 60 à 704 | 304 × 644 |

**Les lignes du canevas** (y local, depuis le haut du canevas) :

| Ligne | Nœuds | y du haut du nœud | Noms |
|---|---|---|---|
| Nom de branche | nom et compteur « 2/4 » | au-dessus du nœud le plus haut de la branche | 13 px, une ligne |
| **Clés** (niv. 45, 2 pts) | 64 px, carrés | 40 | 2 lignes à 11 px (106 à 134) |
| **Étage 2** | 56 px | 144 | 2 lignes (202 à 230) |
| **Étage 1** | 56 px | 240 | 2 lignes (298 à 326) |
| **Couronne** | le point de départ, au centre x = 470 | 352 | aucun |
| **Portes** | paires 64 px, Cœurs 48 px | 368 | 1 ligne (434 à 450), et les pastilles d'élément dans le nœud |
| **Fourche** (racines) | 7 courbes courtes, du tronc vers chaque porte | 450 à 476 | aucun |
| **Tronc** | 3 cernes de 44 px, centrés sur x = 470 : 40 en haut (478), 35 (526), 30 en bas (574) | 478 à 618 | **à droite** du cerne : « 40 · +5 % PV » |
| **Légende** | permanente, en bas à droite du canevas, à côté du tronc (x 640 à 932, y 478 à 618) | | 4 lignes |

Gouttière : x 0 à 72.

**Les colonnes**, toujours dans le même ordre de gauche à droite, quel que soit le focus (rien ne se réordonne) :

| # | Branche | Pourquoi là | Largeur déployée |
|---|---|---|---|
| 1 | **Fusion** | au-dessus de Foudre, la porte la plus à gauche : un trait droit | 196 (étage 1 : 56 + 56 + 64) |
| 2 | **Alchimie** | elle vit des paires : à côté de la Fusion | 188 (3 × 56) |
| 3 | **Mêlée** | Mêlée et Distance côte à côte : leurs exclusions (Corps à corps, Longue vue, Artillerie) se lisent d'un coup d'œil (déjà voulu par T115) | 122 |
| 4 | **Distance** | | 138 (2 clés de 64) |
| 5 | **Tempo** | | 188 |
| 6 | **Terrain** | | 254 (4 × 56) |
| 7 | **Soutien** | le style de groupe, au bord | 254 |

Les nœuds d'une ligne sont espacés de 10 px. Une branche en **bourgeon** occupe 88 px (un nœud de 48 et la place de son nom). Entre les branches, l'espace restant est réparti, à 8 px au moins.

**La Fusion, rangée selon ses vraies dépendances** (sans changer de règle) :

| Ligne | Nœuds | Pourquoi |
|---|---|---|
| Étage 1, encadré « 1 au choix » | Transmutation Feu → Foudre, Transmutation Air → Foudre, **Conversion** (clé, niv. 45, 2 pts, « grande bascule ») | les trois s'excluent et demandent toutes Foudre : c'est **un** choix, « combien de Foudre » (un élément, ou les deux à 45). La Conversion garde sa forme de clé et son badge 45 |
| Étage 2 | Signature appuyée, Prisme | ils demandent l'un des trois nœuds du dessous : c'est exactement la règle des étages |
| Clés | aucune | |

Avec ce rangement, la Fusion obéit à la même règle que les styles. Le problème de la Conversion dessinée au-dessus de Prisme disparaît.

**L'Alchimie** n'a pas d'étage 2 : sa branche traverse la ligne vide et ses clés demandent un nœud de l'étage 1. On l'écrit ainsi : « l'étage **non vide** juste en dessous ».

### 2.5 Montrer et cacher

| Cas | Ce qu'on montre |
|---|---|
| **Branche où un nœud est pris** | **déployée** : toujours, tous ses nœuds. Le chemin pris n'est **jamais** caché |
| **Branche où rien n'est pris** | **bourgeon** : un nœud de 48 px à la hauteur de l'étage 1, avec l'icône de la branche, son nom et un compteur « 0/9 ». Son état est le meilleur de ses nœuds (ouvert si l'un l'est, fermé si tous le sont) |
| **Clic sur un bourgeon** (ou Entrée) | la branche se déploie (le **focus**) ; le focus précédent, s'il n'a rien de pris, redevient bourgeon. Une seule branche en focus à la fois |
| **Survol d'un bourgeon** | aperçu dans le panneau de droite : la fiche de la branche (son bandeau, puis ses étages, avec le nom et l'état de chaque nœud). Le canevas ne bouge pas au survol |
| **Survol d'un nœud non pris** | le chemin qui y mène (L8 a, `TreePlan.Path`) s'allume **dans le bois** (les segments de branche et de tige concernés), et son coût s'affiche dans le panneau. Il n'y a plus d'anneau autour de chaque nœud du chemin |
| **Trop large** (haut niveau, plusieurs branches fournies) | les branches prises passent en **compact** : par étage, les nœuds pris et un seul bourgeon « +n » pour les autres. Le focus reste déployé |
| **Tresses des autres paires** (`coming:`) | **retirées du canevas**. L'infobulle d'une paire dit si elle a une Fusion (« Foudre : sa Fusion pousse au-dessus ») ou si elle viendra plus tard |
| **Fourche, une fois choisie** | elle reste entière : changer de porte est une vraie décision, et voir ce qui est fermé y est utile, mais en **discret** (§2.6) |
| **Anneaux de fond, accolades des couples** | retirés. Les couples se lisent par l'espacement (10 px dans un couple, 28 entre deux couples) et par la ligne « Feu + Air » sous chaque paire |

**À l'ouverture**, le focus est vide si une branche est déjà prise (les branches prises sont déployées). Sinon, il n'y a pas de focus : tout est en bourgeons et le clavier part de la porte conseillée (C5 b).

### 2.6 Une seule convention pour les états

On passe de neuf codes à **quatre aspects permanents**, plus **trois signes passagers**. Chaque aspect se distingue par la **forme du trait et le remplissage**, pas seulement par la couleur (daltonisme). Le graphiste choisit les couleurs ; la hiérarchie de saillance, elle, est imposée.

| Aspect | Vient de (`NodeState`, `RefusalKind`) | Sens, en une phrase (c'est aussi la ligne de la légende) | Dessin | Saillance |
|---|---|---|---|---|
| **Pris** | `Taken` | « Il est à toi. » | rempli, trait épais, icône pleine | **1** (le plus visible) |
| **Ouvert** | `Open`, **et `Points`** | « Le chemin y mène : un clic le prend, s'il te reste des points. » | contour plein, icône blanche, fond vide | **2** |
| **Pas encore** | `Locked` (porte, parent) et `Level` (30, 45) | « Il te manque le nœud d'en dessous, ou un niveau : le chiffre dit lequel. » | contour en pointillés, icône à 40 % ; badge « 45 » si c'est le niveau | 3 |
| **Fermé** | `Excluded` (exclusion, paire, clé du style, bascule, éléments) | « Un choix que tu as fait le ferme : survole-le pour savoir lequel. » | **sans contour**, fond et icône à 25 %, une barre oblique fine. **Jamais en rouge, jamais de croix** | 4 (le plus discret) |

**Signes passagers** (ils n'apparaissent que pendant une action) :

| Signe | Quand | Dessin |
|---|---|---|
| **À rendre** | pendant la confirmation « Rendre / Garder » | le **seul rouge** de la fenêtre : les nœuds et le bois qui seraient perdus |
| **Planifié** | mode « Planifier au niveau 60 » | contour pointillé de la couleur « pris » (inchangé, N2 a) |
| **Chemin** | survol d'un nœud non pris | le bois du chemin s'allume en « sève » (§2.7) |

Le focus clavier garde son cadre carré, qui n'est pas un état.

**Pourquoi « Points » devient « Ouvert » :** `WhyNot` teste les points **en dernier**. Un nœud refusé pour les points passe donc toutes les autres règles : il est réellement atteignable. Le manque de points est dit **une fois**, en gros, dans le compteur de la barre du haut (« 0 point à placer · prochain au niveau 48 »). Un clic sur un nœud ouvert sans point répond « Il te faut 1 point (2 pour une clé) : prochain au niveau 48 ». Ainsi l'arbre ne s'éteint plus quand on a tout dépensé.

**Pourquoi fusionner « verrouillé » et « trop bas » :** pour le joueur, c'est la même réponse (« pas encore »). Le badge de niveau suffit à distinguer les deux.

### 2.7 Une seule convention pour le bois (les liens)

**Il n'y a plus de lien de nœud à nœud.** Le bois est fait de pièces hiérarchisées par l'**épaisseur**, et chaque pièce a l'une de trois **vies** :

| Pièce | Va de | Épaisseur relative |
|---|---|---|
| **Tronc** | du cerne 30 à la fourche | 5 (la plus épaisse) |
| **Racine de fourche** | du haut du tronc à chaque porte | 2 |
| **Tige** | d'une porte prise à la couronne | 3 |
| **Charpentière** | de la couronne au pied d'une branche de style (une courbe en éventail, qui ne monte jamais à l'envers) | 3 |
| **Flèche de la Fusion** | de Foudre, tout droit, au pied de la Fusion | 3 |
| **Axe** | le long d'une branche, d'un étage à l'étage suivant | 2,5 |
| **Rameau** | de l'axe au nœud, court et horizontal | 1,5 |

| Vie | Quand | Dessin |
|---|---|---|
| **Vivant** | la pièce mène à un nœud pris (le tronc est toujours vivant jusqu'au dernier cerne atteint) | plein, couleur « pris » |
| **Atteignable** | la pièce mène à un nœud ouvert | plein, fin, neutre |
| **Nu** | sinon | pointillé, neutre, faible |

La règle « n'importe quel nœud de l'étage du dessous » se lit sans un trait de plus : **l'axe d'une branche est vivant jusqu'au dernier étage où j'ai un nœud ; l'étage suivant est atteignable.** Le bus disparaît : seules les **tiges des portes prises** (deux au plus, une paire et un Cœur, ou deux paires) rejoignent la couronne. Une couronne nourrie a ses charpentières atteignables ; sans porte prise, tout est nu.

**Les exclusions entre branches** (Corps à corps ferme Longue vue et Artillerie) ne sont jamais tracées. Au survol d'un nœud, les nœuds qu'il fermerait prennent un contour « fermé » en aperçu, et le panneau les nomme.

### 2.8 La légende

- **Permanente**, en bas à droite du canevas, à côté du tronc (une place vide aujourd'hui) : **quatre lignes**, une par aspect, avec la phrase du §2.6 en raccourci (« pris : à toi », « ouvert : un clic », « pas encore : il manque un nœud ou un niveau », « fermé : survole pour savoir pourquoi »).
- **Le glossaire** (empreinte, réaction, porte, clé, grande bascule, hors base) reste derrière « ? » et dans les liens survolables des fiches (M7 a).
- **« À venir » disparaît** de la légende, puisque les tiges à venir ne sont plus dessinées. **« À rendre »** est expliqué au moment où il apparaît, par le texte de la confirmation.
- Les **raccourcis** (« Flèches : se déplacer · Entrée : prendre ou rendre · Ctrl+S ») passent dans la ligne d'aide sous la légende, en 11 px.

### 2.9 La barre du haut et le panneau de droite

**Barre du haut**, de gauche à droite : « ARBRE DES TALENTS · Sentinelle niv. 45 », puis le **compteur**, l'élément le plus gros de la barre (« 2 points à placer · 7 au total » ; à 0 : « prochain point au niveau 48 »), puis **[Outils ▾]**, qui regroupe Build suggéré, Planifier au niveau 60, Copier le code et Charger un code (la saisie du code passe dans une petite fenêtre), et enfin [?] [Annuler] [Enregistrer] [Fermer]. En mode plan, un bandeau dit « Plan au niveau 60 : rien ne s'enregistre ».

**Panneau de droite**, trois blocs fixes de haut en bas :

1. **Mon build** : le titre (« Foudre convertie · Mêlée »), « hors base 1 sur 2 », la grande bascule, « Ce que tu paies » (les contreparties, 3 lignes au plus, puis « et 2 autres » au survol), et le meilleur coup avec son plafond.
2. **Le nœud ou la branche** survolé ou au clavier : la fiche de T116 et T117, sans changement de contenu. Au survol d'un bourgeon, la fiche de la branche.
3. **Ce que l'enregistrement change** (N6 a, F7 a), seulement s'il y a un brouillon.

**Le tutoriel** (trois bulles) : les bulles sont ancrées sur la grille et ne couvrent jamais les portes. Leur ordre change : 1. les **portes** (la seule chose à faire à 30), dans la couronne vide ; 2. les **branches** ; 3. le **tronc**, à droite du tronc. La clé de compte `Seen` change de nom (`tree2`) pour que Charles et les joueurs existants revoient les bulles une fois.

### 2.10 Le changement de règle minime (à part, non recommandé)

> **Option R1 : la Conversion demanderait l'une des Transmutations** (et la remplacerait, comme aujourd'hui avec `Talents.With`). La Fusion deviendrait alors une vraie échelle, Transmuter puis Convertir, ce qui colle à « l'échelle de la Matière » de la v3 (§2.2).
>
> **Prix :** il faudrait un point libre de plus au moment de convertir, les builds de référence et les codes de build (`BuildCode` recharge « dans un ordre où chaque nœud tient ») seraient à revoir, et les mesures T41 et des hordes seraient à refaire. **Je ne la recommande pas** : le rangement « 1 au choix » du §2.4 donne la même clarté sans toucher aux règles.

---

## 3. Maquettes (1280 × 720)

Signes : `(■■■■)` pris · `(    )` ouvert · `(····)` pas encore · `(░░░░)` fermé · crochets `[ ]` = clé (carré). Bois : `║` vivant · `│` atteignable · `┊` nu. Les diagonales du rendu sont des **courbes** ; la ligne `╠═══╝` de la maquette A est une courbe de Foudre vers la couronne, pas un bus. Une colonne de caractères vaut environ 10 px.

### 3.1 Héros de 45, Foudre convertie et Corps à corps (7 points dépensés sur 7)

Build : Électrisation (Foudre), Conversion : Foudre, Pied ferme, Poussée lourde, Corps à corps, soit 1 + 2 + 1 + 1 + 2 = 7. La Mêlée et la Fusion sont déployées ; les cinq autres branches sont des bourgeons. La Distance est fermée (Corps à corps exclut Longue vue, son seul nœud d'étage 1). Les nœuds ouverts restent ouverts malgré les 0 point.

```
 ARBRE DES TALENTS · Sentinelle niv. 45    0 point à placer · prochain au niv. 48    [Outils ▾] [?] [Annuler] [Enregistrer] [Fermer]
──────────────────────────────────────────────────────────────────────────────────────────────┬───────────────────────────────
                                             MÊLÉE · 4/4                                    │ FOUDRE CONVERTIE · MÊLÉE
Clés 45                                        [■■■■]                                       │ Hors base : 1 sur 2
2 pts                                       Corps à corps                                   │ Grande bascule : prise
                                                  ║                                         │ Ce que tu paies :
            FUSION · Foudre                       ║                                         │ · plus aucun sort Feu ni Air
Étage 2         (    )      (    )             (■■■■)                                       │ · portée 2 au plus
              Signature     Prisme         Poussée lourde                                   │ · −5 % si tu as bougé
                  └─────┬─────┘                   ║                                         │ Meilleur coup : +55 %,
                        │        ALCHIMIE         ║         DISTANCE TEMPO  TERRAIN SOUTIEN │ plafonné à +35 %
        ┌─ 1 au choix ──┴────────┐                ║                                         │ ─────────────────────────────
Étage 1 │ (░░░░) (░░░░) [■■■■]   │ (    )   (■■■■)═╬─(    )   (░░░░)  (    )  (    )  (    ) │ SURVOL : Corps à corps
        │  Feu    Air   Convers. │      Pied ferme  Au plus près                            │ Clé de Mêlée · à toi
        └───╥────────────────────┘  0/6           ║           0/4     0/9     0/10    0/8   │ +25 % ; portée 2 au plus.
            ║                        │            ║            ┊       │       │       │    │ Ferme dans Distance :
            ║                           ╲         ║         ·     ╱     ╱     ╱             │ Longue vue, Artillerie.
            ║                                ╲    ║    ╱╱╱                                  │ ─────────────────────────────
            ║                                     ◆  couronne                               │ Rien à enregistrer.
            ╠═════════════════════════════════════╝                                         │
Portes   (■■■■) (    )   (░░░░) (░░░░)   (░░░░) (░░░░)   │ (  ) (  ) (  ) (  ) │            │
         Foudre  Boue    Glace  Magma    Vapeur Sable      Ter. Feu  Eau  Air               │
        Feu+Air Ter+Eau Eau+Air Ter+Feu Feu+Eau Ter+Air        1 au choix                   │
                                  ╲  ╲  ║  ╱  ╱                                             │
Tronc                                  (■■) 40 · +5 % PV          LÉGENDE                   │
                                        ║                       (■■) pris : à toi           │
                                       (■■) 35 · +3 % résist.   (  ) ouvert : un clic       │
                                        ║                       (··) pas encore (45)        │
                                       (■■) 30 · +3 % dégâts    (░░) fermé : survole        │
```

Ce qu'on lit en deux secondes : un tronc plein, **une** porte vivante (Foudre), une flèche vivante qui monte dans la Fusion jusqu'à la Conversion, une charpentière vivante jusqu'en haut de la Mêlée. Le reste est soit ouvert (Boue, Signature, Prisme, Au plus près, les bourgeons d'Alchimie, de Tempo, de Terrain et de Soutien), soit discret. **27 formes**, dont 5 bourgeons, contre 41 aujourd'hui ; **aucun** rouge ; **aucune** croix.

### 3.2 Héros de 30 qui découvre l'arbre (2 points, rien de pris)

On suppose que ses deux meilleures caractéristiques sont Feu et Air : la porte conseillée est Foudre (C5 b). Toutes les branches sont des bourgeons « pas encore » (il faut une porte), et la Fusion pend de Foudre. Les lignes des étages sont tracées en guides très légers, pour montrer où l'arbre va pousser. La première bulle est dans la couronne vide et pointe vers les portes.

```
 ARBRE DES TALENTS · Sentinelle niv. 30    2 points à placer · prochain au niv. 33           [Outils ▾] [?] [Annuler] [Enregistrer] [Fermer]
──────────────────────────────────────────────────────────────────────────────────────────────┬───────────────────────────────
                                                                                            │ 2 POINTS À PLACER
Clés 45 ┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈ │ Prochain point : niveau 33
                                                                                            │
                                                                                            │ Commence par une porte.
Étage 2 ┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈ │ Tes deux meilleures
                                                                                            │ caractéristiques, Feu et
                                                                                            │ Air, désignent Foudre.
         FUSION     ALCHIMIE      MÊLÉE     DISTANCE      TEMPO      TERRAIN     SOUTIEN    │ ─────────────────────────────
        la Foudre   réactions  au contact    de loin    rythme PA    glyphes     alliés     │ SURVOL : Foudre (porte)
Étage 1   (····)     (····)      (····)      (····)      (····)      (····)      (····)     │ Feu + Air · réaction Électrisé
           0/5        0/6         0/4         0/4         0/9         0/10        0/8       │ Ouvre les 6 styles et sa
            ┊           ┊           ┊           ┊           ┊           ┊           ┊       │ branche Fusion.
            ┊                 ·         ·       ┊       ·         ·         ·               │ Ferme : Glace, Magma, Vapeur,
            ┊     ┌─────────────────────────────────────────────────────────────┐           │ Sable (un élément en commun).
            ┊     │ 1/3  Choisis une porte : une paire ou un Cœur.              │           │ Coût : 1 point.
            ┊     │ Tes branches pousseront au-dessus.               [Suivant]  │           │
            ┊     └─────────────────────────────▽───────────────────────────────┘           │
            ┊                                   ◇                                           │
Portes   (    ) (    )   (    ) (    )   (    ) (    )   │ (  ) (  ) (  ) (  ) │            │
         Foudre  Boue    Glace  Magma    Vapeur Sable      Ter. Feu  Eau  Air               │
        Feu+Air Ter+Eau Eau+Air Ter+Feu Feu+Eau Ter+Air        1 au choix                   │
       ▲ conseillé                ╲  ╲  ║  ╱  ╱                                             │
Tronc                                  (··) 40 · +5 % PV          LÉGENDE                   │
                                        ┊                       (■■) pris : à toi           │
                                       (··) 35 · +3 % résist.   (  ) ouvert : un clic       │
                                        ║                       (··) pas encore (45)        │
                                       (■■) 30 · +3 % dégâts    (░░) fermé : survole        │
```

Ce qu'on lit : « je suis un jeune arbre, mon tronc est à 30, il y a une ligne de **portes ouvertes** et une qui m'est conseillée ; au-dessus, sept branches qui attendent ». L'arbre n'affiche que **10 portes ouvertes** et 7 bourgeons, au lieu de 41 formes dont 6 croix. Les deux phrases de chaque bourgeon (« au contact », « de loin ») sont de nouvelles clés de texte en FR et en EN, à faire relire par le scénariste.

---

## 4. Ce qui change dans le code

**Ce qui ne change pas :** `data/talents.json`, `Rpg.Core` (`Talents`, `TalentReasons`, `TreeDraws`), `TreePlan`, `BuildCode`, `PointsEditor`, `TreeGuide`, l'API et le contenu des fiches (`Texts.TreeCard`).

### 4.1 Les étapes

Chaque étape arrive avec ses tests (règle 1) et se livre seule ; Charles peut juger l'étape 1 dans la journée.

| # | Étape | Ce qu'elle fait | Fichiers | Points |
|---|---|---|---|---|
| **É1** | **Les états et le bruit** (gain rapide) | `TreeLayout.Look(NodeState)` donne 4 aspects ; `State` envoie `Points` vers `Open` ; les nœuds `coming:` disparaissent ; « fermé » sans croix ni rouge, à 25 % ; le rouge réservé à « à rendre » ; le bus ne s'allume plus (seules les montées des portes prises sont vivantes) ; la légende devient permanente sur 4 lignes ; noms sur 2 lignes sans points de suspension. Textes `tree.look.*` en FR et en EN | `TreeLayout.cs`, `TreePanel.cs` (`TreeShapes`, `TreeCanvas`, `LegendDot`), `data/i18n` du jeu | **2** |
| **É2** | **La grille de l'espalier** | `TreeLayout` refait : `enum Row { Trunk, Doors, First, Second, Key }`, `RowOf(TalentSpec)` (la Fusion : Transmutations et Conversion en `First`, Signature et Prisme en `Second`), l'ordre des 7 colonnes, `enum Spread { Full, Compact, Bud }`, le constructeur qui reçoit `taken` (pour savoir quoi déployer), des ids de bourgeon `bud:{Branch}` au lieu de `fold:{Style}:{Tier}`, `BudState` (le meilleur aspect de la branche), le tronc vertical, les Cœurs à 48 px, la gouttière | `TreeLayout.cs`, `TreeLayoutTests.cs` (réécrit), `TreePanel.Lay` | **3** |
| **É3** | **Le bois** | `TreeLayout.Wood(taken, stateOf)` rend des `WoodPiece(Kind, Points, Life)` : tronc, racines, tiges, couronne, charpentières (Bézier), flèche de la Fusion, axes, rameaux. Il remplace `Links()`. `TreeCanvas._Draw` trace le bois par épaisseur et par vie, et le chemin survolé en « sève » ; le bus, les accolades, les anneaux et le cadre de la Transmutation disparaissent (le cadre « 1 au choix » couvre la Fusion et les Cœurs) | `TreeLayout.cs`, `TreePanel.cs` | **3** |
| **É4** | **Pousser et replier** | déploiement des branches prises, focus au clic sur un bourgeon, mode compact quand c'est trop large, fiche de branche au survol d'un bourgeon, aperçu des nœuds fermés au survol, **clavier sur la grille** (gauche et droite : branche voisine sur la même ligne ; haut et bas : étage voisin de la même branche ; sous l'étage 1 : la porte la plus proche ; sous les portes : le tronc) | `TreeLayout.cs` (`Neighbour`), `TreePanel.cs`, `Texts` (fiche de branche, phrases des bourgeons en FR et en EN) | **2** |
| **É5** | **Le cadre** | barre du haut (compteur, menu Outils), panneau en trois blocs, bulles ré-ancrées et réordonnées (portes, branches, tronc) avec la clé `Seen` renouvelée, auto-test Godot mis à jour (résumé attendu), maquette statique régénérée sans fenêtre (`docs/maquette-arbre.svg`) | `TreePanel.cs`, auto-test, `Texts`, `docs/` | **2** |
| | **Total** | | | **12** |

**L'ordre :** É1 tout de suite, comme une version à essayer. É2 et É3 **ensemble** : la grille sans le bois afficherait les anciens liens sur de nouvelles positions. É4 et É5 ensuite. Cela fait environ un sprint et demi, à insérer dans le plan (numéro de décision et de sprint à attribuer, points et risques à reporter dans `PLAN.md`).

### 4.2 Ce que `TreeLayout` doit garantir (les tests)

Les tests portent sur un **corpus de dispositions** : pour chaque classe, l'arbre vide à 30, les 9 builds de référence (`Progression.Builds`) à 45, 60 et 100, chaque branche en focus, et 300 builds tirés par `TreeDraws.Random` aux niveaux 30, 45, 60 et 100.

| # | Garantie | Test |
|---|---|---|
| G1 | **Chaque nœud est sur sa ligne** : `Box.Y` vaut la ligne de `RowOf` (le tronc, les portes, les étages 1 et 2, les clés) | sur le corpus |
| G2 | **Chaque nœud est dans le couloir de sa branche** (`Columns[branch]` le contient), et la Fusion est au-dessus de Foudre (le centre de sa colonne à moins de 60 px du centre de Foudre) | sur le corpus |
| G3 | **Chaque enfant est au-dessus de ses parents** : pour chaque `requires` et `requiresAny` présent dans la disposition, `enfant.CenterY < parent.CenterY` | sur le corpus |
| G4 | **Invariant des données** : dans une branche, les `requiresAny` d'un nœud sont exactement les nœuds de l'étage non vide juste en dessous dans cette branche (les portes pour l'étage 1, Foudre pour l'étage 1 de la Fusion). Si un futur talent casse la forme, ce test échoue et la convention « l'axe vivant suffit » doit être revue | sur `data/talents.json` |
| G5 | **Aucun croisement de bois** : deux pièces ne se coupent qu'en une extrémité commune (intersection de segments ; une courbe est découpée en 16 segments) | sur le corpus |
| G6 | **Aucun chevauchement** : nœuds, noms sur 2 lignes, noms de branches, gouttière et légende, deux à deux ; tout tient dans le canevas de 932 × 644 | sur le corpus |
| G7 | **Le chemin pris n'est jamais caché** : tout nœud pris a sa propre boîte, jamais un bourgeon, même en compact | sur le corpus, surtout à 100 |
| G8 | **Taille** : aucun nœud sous 44 px ; nœuds 56, portes et clés 64, Cœurs et bourgeons 48, cernes 44 | sur le corpus |
| G9 | **Stabilité** : l'ordre des 7 colonnes est le même quelle que soit la disposition ; déployer une branche ne déplace aucune ligne et ne réordonne rien | focus sur chaque branche |
| G10 | **Les aspects** : chaque `RefusalKind` donne l'aspect du §2.6 (`Points` donne Ouvert, `Level` et `Door` donnent Pas encore, `Excludes` et `Pair` donnent Fermé...), et un bourgeon prend le meilleur aspect de sa branche | un test par règle, sur des arbres construits pour |
| G11 | **Le bois vivant** : une pièce est vivante si et seulement si elle mène à un nœud pris ; l'axe d'une branche est vivant jusqu'à son dernier étage pris ; seules les portes prises ont une tige ; aucune pièce n'est vivante sur un arbre vide (hors tronc) | sur le corpus |
| G12 | **Le clavier** : depuis le cerne 30, on atteint chaque nœud navigable ; « haut » depuis un nœud mène à l'étage du dessus de sa branche s'il existe ; « gauche » puis « droite » revient au point de départ sur une ligne | graphe des voisins, sur le corpus |
| G13 | **Plus de `coming:`** ni de `fold:` dans la disposition ; un seul `bud:` par branche repliée | sur le corpus |
| G14 | **Bilingue** : chaque texte nouveau (aspects, gouttière, phrases des bourgeons, menu Outils, compteur) existe en FR et en EN | test de clés existant |

Côté Godot, l'auto-test vérifie : la légende visible avec 4 lignes, aucun rouge hors confirmation (lecture de `TreeShapes` sur l'état), le bourgeon qui se déploie au clic, Entrée sur une porte qui fait vivre sa tige, et aucune couleur écrite en dur (le test de T116 reste).

---

## 5. Les risques

| # | Risque | Gravité | Parade |
|---|---|---|---|
| R1 | **La largeur à haut niveau.** À 100 (25 points), trois ou quatre branches fournies ne tiennent pas en largeur, même en compact | moyenne | G6 et G7 sur 300 tirages à 100 ; sinon, un mode **dense** (nœuds de 48, écarts de 6) ; le zoom reste exclu (K3 a) |
| R2 | **Le temps 2 : six tresses.** La colonne Fusion devra en porter jusqu'à deux (deux paires) | moyenne, plus tard | la Fusion ne montre que les tresses des paires prises ; avant toute paire, son bourgeon les liste dans la fiche ; G2 généralisé (chaque tresse au-dessus de sa porte) |
| R3 | **Le bois implicite repose sur la forme des données** (« un nœud de l'étage du dessous ») | faible aujourd'hui | G4 casse la CI au premier talent qui sort de la forme |
| R4 | **Les exclusions entre branches ne se voient qu'au survol** (Mêlée contre Distance) | faible | aperçu « fermé » au survol, nommé dans le panneau ; les deux colonnes restent côte à côte |
| R5 | **« Ouvert » à 0 point** : un joueur clique et se fait refuser | faible | un compteur gros et clair, et un refus qui dit quand vient le prochain point |
| R6 | **La disposition bouge** quand une branche se déploie : les colonnes voisines glissent | faible | ordre fixe (G9), une seule branche en focus ; plus tard, une animation de 150 ms (K10 b) |
| R7 | **Le graphiste en parallèle** dessine 5 états, des croix et un rouge d'exclusion | **haute si on ne lui dit rien** | lui transmettre **aujourd'hui** le contrat : 4 aspects et 3 signes passagers (§2.6), 7 pièces de bois en 3 vies (§2.7), 3 formes (rond, grand rond, carré) et le bourgeon, et une hiérarchie de saillance où pris > ouvert > pas encore > fermé, avec le rouge réservé à « à rendre ». Il choisit les couleurs, pas l'ordre |
| R8 | **Daltonisme** | moyenne | les aspects se distinguent par le trait (plein, pointillé, absent) et le remplissage, pas par la teinte |
| R9 | **Les habitudes** : Charles connaît déjà la fenêtre ; ses repères (le bus, les colonnes repliées) disparaissent | faible | les bulles sont remontrées une fois (clé `Seen` renouvelée) |
| R10 | **Le périmètre qui glisse** : « tant qu'on y est », retoucher les fiches, les icônes en relief ou l'encyclopédie | moyenne | ce document ne touche que la disposition, les états et la légende ; les icônes en relief (retours UI du 28-29 octobre) relèvent du graphiste |
| R11 | **Pas de fenêtre visible** pour vérifier | connue | rendu sans fenêtre (auto-test de la CI, maquette SVG régénérée), puis l'essai de Charles sur la version livrée |

### 5.1 Le test de jeu qui dira si c'est gagné

À faire par Charles sur la version d'É3, avec un joueur qui n'a jamais vu l'arbre. Trois questions, chacune sur une fenêtre ouverte depuis 10 secondes :

1. « Montre-moi ce que tu peux prendre maintenant. » Il doit montrer les nœuds ouverts, et pas les fermés.
2. « Qu'est-ce que tu as déjà ? » Il doit suivre le bois vivant.
3. (Sur le build du §3.1) « Pourquoi tu ne peux pas aller en Distance ? » Il doit trouver la réponse en un survol.

Et le critère de T118 (P5 a) : il explique son build en une phrase après deux minutes.

---

## 6. Recommandation

**Faire l'espalier**, dans l'ordre É1, puis É2 et É3 ensemble, puis É4 et É5, soit 12 points, **sans changement de règle** (l'option R1 du §2.10 est écartée).

**É1 (2 points) est à livrer tout de suite.** Elle traite les deux plaintes exactes de Charles : les « chemins verts » (le bus éteint, l'état « points » qui n'éteint plus l'arbre) et les « chemins rouges » (plus de croix rouges, le rouge réservé à la confirmation). Elle retire aussi 5 tiges sans texte et affiche une légende de 4 lignes. Elle est jugeable dans la journée, sans attendre la refonte de la disposition.

**É2 et É3 font le reste de la demande, « ressembler à un arbre » :** un tronc étroit, une couronne, des branches qui poussent quand on y met des points, et un bois dont l'épaisseur dit la hiérarchie et dont la vie dit le build. La grille garantit, tests à l'appui, qu'aucun lien ne se croise et que chaque nœud est au-dessus de son parent.
