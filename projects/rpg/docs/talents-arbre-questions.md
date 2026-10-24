# Osmose : faire de l'arbre des talents un vrai arbre, questions à Charles

*24 octobre 2026. Questionnaire préparé par le game designer après le retour de Charles sur la version 1.0.131.*

> **Contexte.** L'arbre commun aux classes (v3, D80) est en jeu depuis les sprints 137 à 144. Il compte 54 talents sur les 66 prévus, en 7 branches à 3 étages, et `PointsPanel` les affiche en 7 colonnes droites.
> Charles veut un vrai arbre : un tronc générique, puis des paires d'éléments, puis des styles de jeu. Il veut aussi des noms de branches expliqués, une explication visuelle, et pas de Foudre dans les sorts tant que l'arbre n'a pas changé les éléments.
> Ce document prépare la discussion. Rien n'est décidé, rien n'est modifié dans le code ni dans les autres documents.

## Comment répondre

- Chaque question propose des options (a, b, c…) et ma recommandation (**Reco**). Tu peux répondre « Reco », une lettre, ou en texte libre.
- **Débloque** dit ce que ta réponse permet de lancer.
- Les **cinq questions marquées ★** sont les plus structurantes. Si tu n'as que dix minutes, réponds à celles-là.
- Tu peux aussi répondre en bloc : « toutes les Reco sauf B2, C4 et K1 ».
- Il y a 144 questions en 16 thèmes. Les thèmes A à J portent sur la structure de l'arbre, K à O sur l'interface, P sur la méthode.

---

## 0. Ce que j'ai lu, et ce qui coince

### 0.1 Ce que le joueur voit aujourd'hui (`PointsPanel.BuildTalents`)

- L'onglet « Talents » de la fiche personnage, dans une fenêtre de 1140 × 620 px. En haut, un compteur de points et une aide de quatre phrases (étages, clé, Éclats).
- Ensuite, **7 colonnes de 150 px** côte à côte : « Tresses · Matière », « Pôles · Matière », « Réactions · Manière », etc. Dans chaque colonne, les intertitres « étage 1 », « étage 2 », « clé », puis des boutons qui ne portent que du texte.
- Il n'y a **ni lien entre les talents, ni icône, ni couleur d'élément, ni légende**. Tout passe par l'infobulle : nom, branche, étage, coût, description.
- La colonne **Pôles est vide**, parce qu'aucun talent de Pôles n'est livré. Son titre s'affiche quand même.
- La colonne **Tresses ne contient que Foudre** (7 talents). Les 5 autres paires n'existent pas encore.
- Un talent refusé est **grisé sans explication**. Le joueur ne sait pas si c'est un prérequis, une exclusion, un manque de points, la clé de Matière ou la limite des deux éléments.
- L'aide parle d'une exception (« 1 en Tresses ») qui n'apparaît nulle part à l'écran.

### 0.2 Foudre dans les sorts : un défaut probable, à confirmer

Dans `PointsPanel.BuildSpells` (ligne 205), les pastilles de filtre par élément sont construites à partir de **tous** les sorts jumeaux de la classe (`BookSpells`), que les talents soient pris ou non. Une pastille **« Foudre » s'affiche donc toujours** dans l'onglet Sorts, même sans aucun talent, et elle filtre sur une liste vide.

Les tuiles de Foudre, elles, restent bien cachées tant que le brouillon de talents ne les donne pas (`SpellBook`). Mais elles **apparaissent dès qu'un talent est coché**, avant l'enregistrement. L'autotest de `Main.Town.cs` (ligne 2223) compte aussi ces tuiles cachées.

Voir F1 (ce que tu as vu) et F2 (la règle voulue). La correction revient au développeur. Je ne la fais pas ici.

### 0.3 Les incohérences entre ton schéma et l'arbre actuel

| # | Ton schéma | L'arbre actuel (v3, D80) | Conséquence |
|---|---|---|---|
| I1 | Un tronc de bonus génériques : +10 % d'attaque, 5 % de résistance à tout, 10 % de PV | Principe 1 : « des règles, pas des chiffres ». Principe 4 : « chaque gain a un prix », donc aucun talent sans contrepartie | Un tronc obligatoire est une **fausse décision** (tout le monde le prend) et une **hausse nette de puissance** (voir I8) |
| I2 | On choisit d'abord une paire d'éléments, puis un style | Deux familles indépendantes, Matière (les éléments) et Manière (les règles). Le build « quatre éléments de base, tout en Manière » est complet (Garde « Marée montante », `mage-manner`) | Si les styles sont sous les paires, **il n'y a plus de build sans paire** |
| I3 | Les styles (mêlée, distance, support) sont sous **chaque** paire | Distance, Soutien, Rythme et Terrain sont des branches uniques | 6 paires × 3 styles = **18 sous-branches**. Soit on multiplie le contenu par 6, soit les branches sont partagées (et on a un graphe plutôt qu'un arbre) |
| I4 | Aucune place pour un héros d'un seul élément | Pôles (Cœur, Constance, Pureté) récompense le mono-élément. Le sprint 144 a mesuré qu'un héros mono-élément nu passe déjà 2,7 à 5,7 hordes sur 10 | Le mono-élément **perd sa branche** si on ne lui garde pas un nœud à la fourche |
| I5 | Une paire à chaque embranchement | Une seule paire a son composé (Foudre). Les 6 réactions existent | Pendant plusieurs chapitres, 5 nœuds de paire n'auraient que leur réaction |
| I6 | Profondeur : 3 nœuds de tronc, 1 de paire, puis n nœuds de style | 2 points à 30, 5 à 45, 16 à 100. L'étage 2 demande 2 points dans la branche | Si le tronc est payant, le joueur n'a **que le tronc à 30** et n'atteint **aucun style à 45**. Il faut plus de points, ou un tronc gratuit |
| I7 | « Foudre quand le passif est utilisé et les éléments changés » | L'Ouverture (1 point) ajoute déjà 4 sorts de Foudre sans rien changer. Seules la Transmutation et la Conversion changent des éléments | Si Foudre n'apparaît qu'après un changement d'élément, **l'Ouverture doit changer de rôle** |
| I8 | Des bonus plats | Plafonds du sprint 144 : au plus 1,5 horde sur 10 de plus que le meilleur héros nu, et au plus +35 % de bonus conditionnels par coup | +10 % d'attaque et +10 % de PV pour tous, c'est environ +20 % de puissance effective. La Foudre calculée sur la somme des parents avait fait passer une Sentinelle de 1,4 à 7,1 hordes : **le tronc seul risque de crever le plafond** |
| I9 | Le support comme style | L'effet d'un soutien dépend de l'élément du sort (le Feu soigne, la Terre arme…) | **Bonne nouvelle** : sous une paire, le Soutien prend un sens fort (Magma = soin + bouclier) |
| I10 | Des nœuds de paires | R1 à R6 : déjà une réaction par paire | **Bonne nouvelle** : la réaction est le nœud naturel d'une paire, et elle existe pour les six |

### 0.4 Ce que ça remet en cause dans l'équilibrage mesuré (sprint 144)

- Les **7 builds de référence** (`Progression.Builds`) sont pensés en étages. Avec un tronc payant, ils perdent 2 à 3 talents entre 35 et 45. Ils sont à réécrire.
- Un **tronc gratuit** ajoute de la puissance à tout le monde. Les planchers T41 (30, 35, 40, 45), le plafond des hordes et le duo avec soutien sont à **re-mesurer**.
- Une **seule paire par héros** casse `guard-manner` (Enlisement + Transissement) et tous les builds à deux réactions.
- `Talents.Problem` (étages, 2 points avant l'étage 2, 1 seul en Tresses, une clé par branche) devient un **graphe de prérequis**. Il faut le réécrire, refaire ses tests et migrer les personnages enregistrés (API, Plouf).
- **Le plafond de +35 %** ne couvre que les bonus conditionnels. Il faut décider si le tronc y entre (B5).
- Rien de tout cela n'est un argument contre ton idée : la lisibilité est un vrai problème aujourd'hui. Mais ce coût pèse sur le choix entre « refaire l'arbre » et « redessiner l'arbre actuel en forme d'arbre » (A1).

### 0.5 Ma proposition de départ, pour fixer les idées (pas une décision)

On lit de bas en haut : le tronc pousse vers le haut.

```
   Mêlée   Distance   Soutien   Terrain   Tempo   Alchimie        <- STYLES : les mêmes nœuds quelle que soit la paire
     \        |          |         |        /        /                (achetés une fois ; pointillés depuis les autres paires)
      '-------+----------+---------+-------'--------'
                         |
  Tresse Foudre  --------+    <- propre à la paire : Ouverture, Transmutation, Conversion
                         |
 [Foudre] [Glace] [Boue] [Magma] [Vapeur] [Sable]   [Élément pur]  <- FOURCHE : 6 paires (leur réaction)
 Feu+Air  Eau+Air Terre+ Terre+  Feu+Eau  Terre+    Cœur, Constance,  + 1 nœud mono-élément
                  Eau    Feu              Air       Pureté (puis Lumière, Ténèbres)
    '--------'------'-------+-------'--------'---------'
                            |
                 [3. +5 % PV]          niveau 40, automatique
                 [2. +3 % résistance]  niveau 35, automatique     <- TRONC : gratuit, il marque la progression
                 [1. +5 % dégâts]      niveau 30, automatique
```

À l'écran, en **vue focalisée** : le tronc et les 7 nœuds de la fourche sont toujours visibles. La paire choisie déploie sa Tresse et ses styles. Les autres paires gardent leurs départs de branches **en pointillés**, comme sur ton dessin.

---

## A. L'intention (7 questions)

**A1 ★ Refonte du contenu, ou seulement du dessin ?**
- a) Redessiner l'arbre actuel en forme d'arbre : mêmes talents et mêmes règles. Les deux familles forment le tronc, les 7 branches les embranchements.
- b) Restructurer en tronc, puis paires, puis styles, en recasant les 54 talents (proposition 0.5).
- c) Tout refaire sur ton schéma, talents compris.
- d) a tout de suite pour la lisibilité, puis b au sprint suivant.
- **Reco : b.** Ton schéma a un vrai sens de jeu (la paire donne une identité, le style une manière de jouer), et 80 % des talents actuels s'y recasent sans être réécrits (thème E). Avec a, on aurait un joli dessin d'une structure que tu trouves déjà fausse. Avec c, on jetterait 8 sprints de travail mesuré.
- *Débloque :* tout le reste, et le chiffrage. Mon estimation : a ≈ 5 points, b ≈ 13 à 16, c ≈ 30 ou plus.

**A2. Le ressenti visé quand on ouvre l'arbre ?**
- a) « Je vois où je vais » : un objectif lointain visible qui donne envie (Path of Exile).
- b) « Je comprends en 10 secondes » : peu de nœuds, des choix nets (WoW Dragonflight).
- c) « Je bricole » : beaucoup de petites combinaisons.
- **Reco : a + b.** On montre le but, mais une seule paire déployée à la fois.
- *Débloque :* la densité de l'arbre (G8) et la vue focalisée (K1).

**A3. Quels arbres de référence te plaisent, visuellement ou dans leur structure ?**
- a) WoW Dragonflight (arbre de classe et arbre de spécialisation, nœuds à choix).
- b) Diablo IV (tronc à paliers, grappes de nœuds).
- c) Path of Exile ou Grim Dawn (constellation partagée immense).
- d) Last Epoch (petits arbres très lisibles, une icône et un rang par nœud).
- Autre : Dofus et Wakfu n'ont pas d'arbre (ce sont des caractéristiques). Dragonica ?
- **Reco : b pour la structure (tronc et paliers), d pour la lisibilité des nœuds.** C est trop dense pour des parties tactiques au tour par tour.
- *Débloque :* la direction visuelle (K et L).

**A4. Le choix d'une paire d'éléments doit-il être la première décision de l'arbre ?**
- a) Oui, comme sur ton dessin : on choisit son affinité, puis son style.
- b) Non, on choisit son style d'abord, et la paire le colore.
- c) Les deux en parallèle : deux sous-arbres côte à côte.
- **Reco : a.** C'est l'identité Osmose (les éléments), et la réaction de la paire est un premier pouvoir immédiat à 30.
- *Débloque :* C1, D1.

**A5. Un héros peut-il ignorer les éléments combinés et rester sur ses quatre éléments de base ?** C'est vrai aujourd'hui (principe 2 de la v3).
- a) Oui, il doit garder un chemin complet sans paire.
- b) Non : choisir une paire fait partie du jeu à partir de 30.
- c) Oui, mais il prend une paire pour sa réaction sans jamais ouvrir son composé.
- **Reco : c.** Le nœud de paire ne donne que la réaction, pas de sort. On garde l'esprit « tout en Manière » sans casser ton arbre.
- *Débloque :* C3, C12.

**A6. Combien de temps le joueur passe-t-il dans l'arbre à chaque point gagné ?**
- a) 10 secondes : un clic évident.
- b) 1 à 2 minutes : il compare deux ou trois options.
- c) Longtemps : il planifie jusqu'au niveau 100.
- **Reco : b**, avec un mode planification facultatif pour ceux qui veulent c (N2).
- *Débloque :* la richesse des infobulles (M4) et la planification (N2).

**A7. L'arbre doit-il signaler les chemins faits pour le groupe (Soutien, Empreinte partagée) ?**
- a) Oui, avec un pictogramme « groupe ».
- b) Non, on le dit dans la description.
- c) Oui, avec un filtre « solo » ou « groupe ».
- **Reco : a.** Le Soutien ne vaut presque rien en solo (v3, §4.6), et il faut le dire avant que le joueur dépense.
- *Débloque :* une icône de plus dans la légende (M5).

---

## B. Le tronc (10 questions)

**B1 ★ Le tronc : automatique, payant, ou à choix ?**
- a) **Automatique** : les nœuds s'allument seuls aux niveaux 30, 35 et 40. Pas de point dépensé.
- b) Payant et obligatoire : 1 point par nœud, passage forcé avant la fourche.
- c) Payant mais facultatif : on peut aller directement à la fourche.
- d) À choix : chaque nœud du tronc propose deux options, comme +attaque ou +PV, et on en prend une.
- **Reco : a.** Un passage payant et obligatoire (b) est la définition d'une fausse décision : il mange 3 des 5 points de 45 sans rien demander au joueur. Avec a, le tronc devient un **jalon visible de progression** (« tu as grandi »), et tous les points vont aux vraies décisions. Si tu veux des décisions dans le tronc, d est la bonne variante, pas b.
- *Débloque :* G1 (le nombre de points), J2 (ce qu'on mesure), et toute la mise en page (le tronc compte pour 3 nœuds ou pas).

**B2. Les valeurs du tronc : +10 % d'attaque, 5 % de résistance à tout, 10 % de PV ?**
- a) Celles de ton dessin.
- b) Divisées par deux : +5 % de dégâts, +3 % de résistances, +5 % de PV.
- c) Celles du dessin, avec des monstres de 30 et plus rehaussés pour compenser.
- d) Pas de chiffres, mais des déblocages : nœud 1 « tes coups laissent une empreinte », nœud 2 « tu peux tresser deux éléments », nœud 3 « choisis ta paire ».
- **Reco : b, puis mesure.** a ajoute environ 20 % de puissance effective à tout le monde, et S144 a montré que les hordes y sont très sensibles. Avec c, on gonfle les chiffres pour rien. d est élégant mais retire le plaisir du « +10 % » que tu as dessiné.
- *Débloque :* les mesures T41 et hordes (J1, J2).

**B3. Combien de nœuds de tronc ?**
- a) 3, comme sur ton dessin (30, 35, 40).
- b) 5, un tous les 10 niveaux de 30 à 70.
- c) 3 avant la fourche, puis un tronc central qui continue entre les branches (un nœud commun à 60 et un à 80).
- **Reco : c.** Les niveaux 60 à 100 ont alors aussi leurs jalons, sans allonger l'attente avant la fourche.
- *Débloque :* la hauteur de l'arbre à l'écran (K1).

**B4. « Attaque », c'est quoi exactement ?**
- a) Les dégâts de tous les sorts, neutres compris.
- b) Les sorts d'élément seulement.
- c) Les chances de critique.
- d) Une caractéristique (+X à toutes les caractéristiques d'élément).
- **Reco : a**, appliqué comme l'équipement, avant les résistances.
- *Débloque :* le code (une règle `BonusRule` existe déjà).

**B5. Le tronc entre-t-il dans le plafond de +35 % de bonus par coup ?**
- a) Non : c'est un bonus permanent, comme l'équipement.
- b) Oui : tout ce qui vient de l'arbre est plafonné.
- **Reco : a.** Sinon les 5 % du tronc « disparaissent » dès qu'un build atteint le plafond, et le joueur ne voit pas pourquoi.
- *Débloque :* `Talents.Percent`.

**B6. La résistance du tronc s'ajoute-t-elle aux résistances des caractéristiques ?**
- a) Oui, en points de pourcentage, sous le plafond de résistance qui existe déjà.
- b) Elle multiplie les dégâts reçus à part (×0,95).
- **Reco : a.** Une seule règle à expliquer.
- *Débloque :* la fiche caractéristiques (O7).

**B7. Les PV du tronc : un pourcentage des PV max, ou un nombre fixe ?**
- a) +X % des PV max, Vitalité comprise.
- b) +N PV fixes (par exemple +20).
- **Reco : a.** C'est ton dessin, et ça vaut autant pour la Garde que pour la Mage.
- *Débloque :* `HpRule`, qui existe déjà (Nomade).

**B8. À quel niveau l'arbre s'ouvre-t-il ?**
- a) 30, comme aujourd'hui : le tronc commence à 30.
- b) 20 : le tronc remplit les niveaux 20 à 30 et la fourche arrive à 30.
- c) 25.
- **Reco : a** si le tronc est automatique (rien ne change pour le chapitre 1 ni pour `BalanceTests`). **b** si tu veux que l'arbre se montre plus tôt : c'est tentant, mais il faudra re-mesurer le chapitre 2.
- *Débloque :* les mesures de 20 à 30, ou pas.

**B9. Le tronc est-il le même pour les trois classes ?**
- a) Oui, identique (règle D80 : aucun talent ne nomme une classe).
- b) Teinté par classe : la Garde reçoit plus de PV, la Mage plus d'attaque.
- **Reco : a.** Les classes ont déjà leurs spécialisations à 50.
- *Débloque :* rien à coder de plus.

**B10. Faut-il voir le tronc avant le niveau 30 ?**
- a) Oui, grisé, avec « s'ouvre au niveau 30 » : le joueur voit ce qui l'attend.
- b) Non, l'onglet reste fermé (comme aujourd'hui, il n'affiche qu'une phrase).
- **Reco : a.** Un but visible motive (on sait pourquoi on monte).
- *Débloque :* l'état « hors niveau » des nœuds (L5).

---

## C. La fourche : les paires d'éléments (12 questions)

**C1. Combien de nœuds à la fourche ?**
- a) Les 6 paires, comme sur ton dessin.
- b) Les 6 paires et un nœud **Élément pur** (pour le mono-élément).
- c) Les 6 paires, Élément pur, et un nœud **Pôles** (Lumière, Ténèbres).
- d) Deux niveaux : d'abord un élément de base (Feu), puis une paire (Feu + Air).
- **Reco : b.** Le mono-élément est fort (S144) et il a besoin d'une porte. Lumière et Ténèbres viendront plus tard sous Élément pur. d double la profondeur pour rien.
- *Débloque :* la largeur de l'arbre (K1), et la place des Pôles (E8).

**C2. Combien de paires un héros peut-il prendre ?**
- a) Une seule.
- b) Une, puis une seconde à 60 ou 75.
- c) Deux dès qu'il a les points (c'est la règle actuelle des deux éléments ouverts).
- d) Autant qu'il veut pour les réactions, mais deux composés ouverts au plus.
- **Reco : c.** Les builds à deux réactions restent possibles (la Garde poseuse d'Eau), et la limite actuelle se garde telle quelle. Avec a, on perd l'hybride, qui est un pilier (« le multi-élément paie »).
- *Débloque :* H2, et la mise en page des deux paires déployées en même temps (K1).

**C3. Que donne le nœud de paire lui-même ?**
- a) La réaction de la paire (R1 à R6).
- b) L'Ouverture du composé (4 sorts).
- c) Les deux.
- d) Rien, c'est une porte vers les branches.
- **Reco : a.** La réaction existe pour les 6 paires, elle ne fait apparaître aucun sort (ce qui répond à ta remarque sur Foudre), et elle donne un pouvoir lisible dès 30 (« Enlisé ! »).
- *Débloque :* E1, F2.

**C4. Quel nom afficher sur un nœud de paire ?**
- a) « Feu + Air ».
- b) « Foudre ».
- c) « Foudre » en titre, et deux pastilles Feu et Air dessous.
- d) Le nom de la réaction, « Électrisation ».
- **Reco : c.** Le composé se retient mieux, et les pastilles expliquent la combinaison sans texte.
- *Débloque :* M3, L4.

**C5. Faut-il conseiller la paire selon les caractéristiques du héros ?**
- a) Non.
- b) Un conseil discret : « tes caractéristiques les plus hautes sont Feu et Air ».
- c) Un avertissement si la paire ne correspond à aucune caractéristique investie.
- **Reco : b.** Un composé ouvert frappe sur le plus haut de ses parents (décision 8 de `elements.md`). Le joueur doit pouvoir le deviner.
- *Débloque :* une ligne de l'infobulle (M4).

**C6. Que faire des paires dont le composé n'existe pas encore (Glace, Boue, Magma, Vapeur, Sable) ?**
- a) Le nœud de paire avec sa réaction, et une Tresse grisée « au prochain chapitre ».
- b) On les masque tant que leur composé n'est pas livré.
- c) On attend d'avoir les six composés pour sortir le nouvel arbre.
- **Reco : a.** Les réactions existent et sont mesurées. Masquer 5 paires sur 6 casserait l'idée de fourche.
- *Débloque :* l'état « à venir » (L10).

**C7. Dans quel ordre disposer les paires ?**
- a) En éventail, dans l'ordre de livraison (Foudre, Glace, puis les autres).
- b) En **rose des éléments** : Terre, Feu, Eau et Air aux quatre coins, et chaque paire sur le côté ou la diagonale qui relie ses deux parents. La disposition explique toute seule qui se combine avec qui.
- c) Par affinité de jeu : contrôle d'un côté (Boue, Glace, Sable), dégâts de l'autre (Foudre, Magma, Vapeur).
- **Reco : b** dans une vue radiale. **a** si on garde l'arbre vertical de ton dessin.
- *Débloque :* K1.

**C8. Changer de paire, est-ce repartir de la fourche ?**
- a) Oui : rendre la paire rend tout ce qui est au-dessus.
- b) Non : les styles communs restent, seule la Tresse de la paire est rendue.
- **Reco : b**, si les styles sont partagés (D1). Le joueur qui passe de Foudre à Glace garde son style de jeu.
- *Débloque :* I1.

**C9. Où placer les triplets (Tresse triple : Éther, Cendre, Sève, Sel) ?**
- a) Comme un nœud passerelle entre deux branches de paires voisines. Le lien montre « il faut Foudre ou Magma ».
- b) Au bout de la Tresse d'une paire.
- c) Hors de l'arbre, dans une quête.
- **Reco : a.** Le seul endroit où l'arbre devient un graphe, mais c'est le sens même d'un triplet. Ça n'arrivera qu'après 50.
- *Débloque :* G6 et G7 (nœuds à deux parents).

**C10. Où placer Lumière, Ténèbres et Crépuscule ?**
- a) Sous Élément pur (ce sont des polarités du héros, qui récompensent la concentration).
- b) Comme un 8e nœud de fourche.
- c) Sur le tronc central, après 60.
- **Reco : a.** Crépuscule est un « tout ou rien » d'identité, comme Pureté.
- *Débloque :* E8.

**C11. Faut-il réserver une place visible à Unisson (Saga II) ?**
- a) Oui, en pointillés « Saga II ».
- b) Non, invisible jusqu'à sa sortie.
- **Reco : b.** Une promesse affichée pendant toute une saga finit par frustrer.
- *Débloque :* rien.

**C12. Peut-on atteindre un style sans passer par une paire ?**
- a) Non : ton schéma à la lettre.
- b) Oui, par le nœud Élément pur.
- c) Oui, par un 8e nœud **Polyvalent** pour qui garde ses quatre éléments (sans réaction, mais avec l'accès aux styles).
- d) Oui, le dernier nœud du tronc ouvre directement les styles, et les paires sont un chemin parallèle.
- **Reco : b, avec a pour le reste.** Avec la Reco de A5 (c), un héros qui veut rester sur ses quatre éléments prend une réaction pour 1 point. Le seul build qui y perd vraiment est `mage-manner` (tout en Rythme), qui devra payer ce point.
- *Débloque :* J4 (les builds de référence), D1.

---

## D. Les styles de jeu, les sous-branches (12 questions)

**D1 ★ Les styles sont-ils propres à chaque paire, ou partagés ?**
- a) **Propres** : 18 sous-branches ou plus (la Mêlée de Foudre n'est pas la Mêlée de Boue). Identité forte, mais contenu multiplié par 6, et équilibrage aussi.
- b) **Partagés** : la même branche Mêlée, quelle que soit la paire, achetée une fois. À l'écran, elle part de chaque paire en pointillés vers les mêmes nœuds.
- c) **Partagés, avec une signature de paire** : les styles sont communs, mais chacun finit sur un petit nœud propre à la paire (« Mêlée de Foudre : tes coups au contact sautent »). Soit 6 × n nœuds de signature, livrés avec chaque composé.
- d) **Restreints** : chaque paire n'ouvre que 2 ou 3 styles qui lui ressemblent (Foudre : distance et tempo ; Boue : mêlée et contrôle).
- **Reco : b tout de suite, c à terme.** b recase les talents actuels sans en écrire de nouveaux. c ajoute la saveur de ton dessin, paire par paire, au rythme des chapitres. a est hors de portée (environ 100 talents à concevoir et mesurer). d enferme les classes : une Garde Foudre au contact doit pouvoir exister.
- *Débloque :* tout le thème E, le chiffrage, et la mise en page (graphe ou arbre strict).

**D2. La liste des styles.** Je propose 6 styles communs, plus la Tresse propre à chaque paire :

| Style | Pour qui | Vient de |
|---|---|---|
| **Mêlée** | « je frappe fort au contact » | Distance D1, D3, D5, D7 |
| **Distance** | « je tire de loin, à l'abri » | Distance D2, D4, D8, D9 |
| **Soutien** | « mes sorts aident mes alliés » | Soutien S1 à S8 |
| **Terrain** | « je tiens le sol : glyphes, placement » | Terrain G1 à G9, D6 |
| **Tempo** | « je règle mes PA et mon rythme » | Rythme Y1 à Y9 |
| **Alchimie** | « je vis des réactions » | Réactions R7 à R12 |
| *Tresse (par paire)* | « je change mes éléments » | Tresses T1 à T7 |

- a) Cette liste.
- b) Seulement les trois styles de ton dessin (mêlée, distance, support), et le reste ailleurs.
- c) Une autre liste (dis-moi laquelle).
- **Reco : a.** Tes pointillés disaient « il y a d'autres branches » : les voici.
- *Débloque :* E2 à E7, M2.

**D3. Mêlée et Distance : deux styles, ou une branche à deux bras ?**
- a) Deux styles séparés.
- b) Une branche « Portée » qui se sépare en deux bras.
- **Reco : a.** Ce sont les deux mots de ton dessin, et le joueur les comprend sans explication.
- *Débloque :* H4.

**D4. Combien de styles un héros peut-il suivre ?**
- a) Un seul.
- b) Deux au plus.
- c) Autant que ses points le permettent, avec une clé par style.
- **Reco : c.** Les points sont le vrai frein, et la clé unique par style garde des choix forts.
- *Débloque :* H1.

**D5. Combien de nœuds par style ?**
- a) 3 étages, comme aujourd'hui (étage 1, étage 2, clé).
- b) 4 : deux petits nœuds, un nœud à choix, une clé.
- c) Variable selon le style.
- **Reco : b.** C'est le format qui loge les talents actuels (8 à 9 par branche) en nœuds à choix (D7).
- *Débloque :* G1, G8.

**D6. La forme intérieure d'un style ?**
- a) Une ligne : chaque nœud demande le précédent.
- b) Une petite fourche à chaque palier : on choisit un nœud parmi deux.
- c) Une grappe libre : tout nœud du palier est permis.
- **Reco : b.** Le dilemme se voit sur l'écran, au lieu d'être caché dans une règle d'exclusion.
- *Débloque :* D7, L1.

**D7. Faut-il des nœuds à choix (un emplacement, deux talents, un seul pris) pour remplacer les exclusions actuelles ?** Par exemple Au plus près ou Longue vue, Coups appuyés ou Petits coups, Corps à corps ou Artillerie.
- a) Oui.
- b) Non : deux nœuds séparés, et un trait rouge entre eux.
- **Reco : a**, comme dans WoW Dragonflight. Les exclusions deviennent évidentes.
- *Débloque :* H3, L1.

**D8. Rythme et Terrain sont-ils de vrais styles ?** Ils ne parlent pas de distance, mais de tempo et de sol.
- a) Oui, ils restent des styles à part (Tempo et Terrain).
- b) On les fond dans Mêlée et Distance selon le talent.
- c) Ce sont des branches transversales, accessibles depuis le tronc sans paire.
- **Reco : a.** Leurs talents se combinent avec n'importe quelle portée. Les fondre créerait des branches de 15 nœuds.
- *Débloque :* E5, E6.

**D9. Faut-il montrer dans l'arbre ce que ta paire donne en soutien ?** (Foudre : petit soin et PM ; Magma : soin et bouclier.)
- a) Oui, dans l'infobulle de Main tendue, calculée sur tes paires.
- b) Non, la table générale suffit.
- **Reco : a.** Sous une paire, le Soutien devient concret. C'est l'un des gains de ton schéma.
- *Débloque :* M4.

**D10. Un nœud de style pris depuis Foudre est-il le même que depuis Boue ?** Cette question découle de D1.
- a) Oui, exactement (D1 b).
- b) Oui, plus une signature de paire au bout (D1 c).
- c) Non (D1 a).
- **Reco : b à terme.**
- *Débloque :* le format de `talents.json` (un nœud signature porte une condition de paire).

**D11. Des styles réservés à certaines paires (par exemple le Soutien seulement avec Feu ou Terre) ?**
- a) Non.
- b) Oui.
- **Reco : a.** Chaque élément a son effet de soutien, donc aucun style n'est vide pour une paire.
- *Débloque :* rien.

**D12. Comment nommer cette couche ?**
- a) Styles.
- b) Voies.
- c) Postures.
- d) Écoles.
- **Reco : a, Styles.** « Voie » est déjà pris par la spécialisation dans `talents-canon.md`.
- *Débloque :* M1, les textes.

---

## E. Où vont les 54 talents actuels (11 questions)

Ma proposition de recasage, avec D2 et D1 b :

| Où dans le nouvel arbre | Talents actuels |
|---|---|
| **Nœud de paire** | R1 Enlisement (Boue), R2 Transissement (Glace), R3 Fêlure (Magma), R4 Électrisation (Foudre), R5 Buée (Vapeur), R6 Poudroiement (Sable) |
| **Tresse Foudre** (propre à Feu + Air) | T1 Ouverture, T2 Plein grimoire, T4 Signature appuyée, T5 Prisme, T3 Transmutation (Feu ou Air), T6 Conversion (clé) |
| **Alchimie** | R7 Empreinte tenace, R8 Catalyse, R9 Réaction prolongée, clés R10 Onde, R11 Empreinte partagée, R12 Virtuose |
| **Mêlée** | D1 Au plus près, D3 Pied ferme, D5 Poussée lourde, clé D7 Corps à corps |
| **Distance** | D2 Longue vue, D4 Zone serrée, clés D8 Artillerie, D9 Ligne claire |
| **Terrain** | G1 Pas de côté, G2 Adossé, G3 Relais, D6 Pas dansant, G4 Sceau, G5 Rémanence, G6 Leurre, clés G7 Terre brûlée, G8 Nomade, G9 Écho |
| **Tempo** | Y1 Coups appuyés ou Y2 Petits coups (à choix), Y3 Premier geste, Y4 Relance courte, Y5 Réserve, Y6 Deck serré, clés Y7, Y8, Y9 |
| **Soutien** | S1 Main tendue (porte), S2 Bannière, S3 Double main, S4 Élan partagé, S5 Longue main, clés S6 Dévouement, S7 Harmonie, S8 Relève |
| **Élément pur** (pas encore livré) | P1 Cœur, P2 Constance, P5 Résonance, clé P6 Pureté ; plus tard P3, P4, P7 (Lumière, Ténèbres, Crépuscule) |

**E1. Les réactions R1 à R6 deviennent les nœuds de paire. D'accord ?**
- a) Oui.
- b) Non, le nœud de paire doit donner autre chose (dis-moi quoi).
- **Reco : a.**
- *Débloque :* C3, la migration la plus simple (les identifiants ne changent pas).

**E2. R7 à R12 forment le style Alchimie. Ou faut-il les répartir ?** (Onde vers Distance, puisqu'elle concerne les zones ; Empreinte partagée vers Soutien, puisqu'elle sert le groupe.)
- a) Un style Alchimie complet.
- b) Les répartir.
- **Reco : a.** Ils n'ont de sens qu'avec une réaction, et ils forment un vrai style « je vis des combos ».
- *Débloque :* D2.

**E3. La Tresse : faut-il imposer l'échelle Ouverture, puis Transmutation, puis Conversion ?** Aujourd'hui, la Conversion ne demande pas la Transmutation.
- a) Oui, chaque marche demande la précédente.
- b) Non, comme aujourd'hui : la Transmutation et la Conversion sont deux chemins.
- **Reco : b.** La v3 dit qu'un héros transmuté qui veut convertir doit refaire sa Transmutation, parce que les deux s'excluent sur la même paire. Ce sont deux bras d'une fourche, pas deux marches.
- *Débloque :* la forme de la Tresse (D6).

**E4. Distance se coupe en Mêlée et Distance comme dans le tableau ?** (Poussée lourde va en Mêlée, Zone serrée en Distance, Pas dansant en Terrain.)
- a) Oui.
- b) Une autre répartition.
- **Reco : a.**
- *Débloque :* les deux styles.

**E5. Rythme devient Tempo sans changement ?**
- a) Oui.
- b) Oui, mais renommé autrement.
- **Reco : a.**
- *Débloque :* rien d'autre.

**E6. Terrain reste un seul style de 10 nœuds, ou se coupe en Mobilité (G1, G3, D6, G8, G9) et Glyphes (G4, G5, G7) ?**
- a) Un seul style.
- b) Deux styles.
- **Reco : a pour l'instant.** Deux styles de 4 à 5 nœuds ont du sens, mais ça fait 7 styles à lire. On coupera quand les signatures de paire arriveront.
- *Débloque :* D2.

**E7. Le Soutien reste tel quel, avec Main tendue comme porte ?**
- a) Oui.
- b) Non.
- **Reco : a.** Mesuré au sprint 144 : le duo avec soutien ne gagne pas plus que le duo de dégâts.
- *Débloque :* rien.

**E8. Les Pôles (pas encore livrés) passent sous Élément pur, comme dans le tableau ?**
- a) Oui.
- b) Non (voir C10).
- **Reco : a.**
- *Débloque :* la colonne vide d'aujourd'hui disparaît.

**E9. Les noms des 54 talents restent-ils les mêmes ?**
- a) Oui.
- b) On les renomme pour coller aux styles (Au plus près devient « Mêlée I », par exemple).
- **Reco : a.** Les noms évocateurs se retiennent mieux que les numéros, et ils sont déjà traduits.
- *Débloque :* rien.

**E10. Que deviennent les clés ?**
- a) « Une clé par branche » devient « une clé par style ». « Une seule clé de Matière » devient « une seule des quatre grandes bascules : Conversion, Tresse triple, Pureté ou Crépuscule ».
- b) On supprime les clés : chaque nœud coûte 1.
- **Reco : a.**
- *Débloque :* H1, la réécriture de `Talents.Problem`.

**E11. Faut-il écrire de nouveaux talents pour remplir des styles trop courts (Distance n'en aurait que 4) ?**
- a) Non, 4 nœuds suffisent.
- b) Oui, un ou deux de plus par style court.
- **Reco : a pour le premier jet**, puis b selon le playtest.
- *Débloque :* le chiffrage.

---

## F. Foudre, les éléments combinés et la fenêtre des sorts (10 questions)

**F1. Qu'as-tu vu exactement dans la 1.0.131 ?**
- a) Une pastille « Foudre » dans le filtre de l'onglet Sorts, sans talent (le défaut 0.2).
- b) Des sorts de Foudre listés alors que tu n'avais pris aucun talent de Tresse.
- c) Des sorts de Foudre apparus dès que tu as coché un talent, avant d'enregistrer.
- d) Foudre dans la barre de combat ou le deck.
- **Pas de Reco** : c'est un constat. Ta réponse dit s'il s'agit d'un défaut (a, d) ou d'une règle à changer (b, c).
- *Débloque :* un correctif tout de suite, ou F2.

**F2. Quand les sorts de Foudre doivent-ils apparaître dans la fenêtre des sorts ?**
- a) Dès que l'Ouverture est **enregistrée** (la règle actuelle, en retirant la pastille fantôme).
- b) Seulement quand une Transmutation ou la Conversion a **changé des éléments** (ta phrase, prise à la lettre).
- c) Dès l'Ouverture, mais dans une section à part, « Sorts de l'arbre ».
- d) a, plus un aperçu en pointillés pendant que le talent est seulement coché dans le brouillon.
- **Reco : d.** Si tu choisis b, l'Ouverture n'ajoute plus rien de visible et doit changer de rôle (F3).
- *Débloque :* le correctif de `BuildSpells` et de l'autotest.

**F3. Si tu choisis F2 b, que devient l'Ouverture ?**
- a) Supprimée. La Transmutation devient la première marche (« tes sorts de Feu deviennent de la Foudre »).
- b) Elle devient un passif sans sort, par exemple « tes coups de Feu ou d'Air ont l'Arc à 15 % ».
- c) Elle donne un seul sort de Foudre, à l'essai.
- **Reco : a.** C'est la lecture la plus fidèle à ta phrase. Mais la décision du sprint 144 (un composé ouvert frappe sur le plus haut parent) disparaît avec l'Ouverture, et les builds `*-matter` sont à re-mesurer.
- *Débloque :* E3, J4.

**F4. Dans « quand le passif dans l'arbre des talents sera utilisé », de quel talent parles-tu ?**
- a) La Conversion (la clé qui remplace Feu et Air par Foudre).
- b) La Transmutation.
- c) L'Ouverture.
- d) Un nouveau talent, à inventer.
- **Pas de Reco** : j'ai besoin de ta lecture.
- *Débloque :* F2, F3.

**F5. La fenêtre des sorts doit-elle dire d'où vient un sort ?** (« Donné par l'arbre : Ouverture de Foudre » ; « remplace Flammèche ».)
- a) Oui, sur la carte du sort.
- b) Non.
- **Reco : a.** Sinon le joueur ne fait pas le lien entre l'arbre et son grimoire.
- *Débloque :* `Texts.SpellCard`.

**F6. Comment montrer un sort remplacé par une Transmutation ou une Conversion ?**
- a) Masqué.
- b) Grisé et barré, avec une flèche vers son jumeau.
- c) Rangé sous son jumeau, replié.
- **Reco : b.** Le joueur voit ce qu'il a payé, ce qui est le principe 4.
- *Débloque :* l'onglet Sorts.

**F7. Quand un talent retire du deck des sorts remplacés, le jeu met-il leur jumeau à leur place ?** Aujourd'hui, `Grimoire.Replaced` le permet.
- a) Oui, automatiquement, avec un message.
- b) Non, la place reste vide.
- **Reco : a.**
- *Débloque :* un message de plus.

**F8. Avant 30, ou sans talent, faut-il montrer les éléments combinés quelque part ?**
- a) Nulle part.
- b) Dans l'arbre seulement, verrouillés.
- c) Dans l'arbre, et dans la fenêtre des sorts en aperçu.
- **Reco : b.** Ça respecte ta remarque (pas dans les sorts) tout en montrant le but.
- *Débloque :* L5.

**F9. Les points de sort dépensés dans un sort remplacé sont rendus (`RankList`). Faut-il le dire ?**
- a) Oui, un message : « 3 points de sort rendus ».
- b) Non.
- **Reco : a.**
- *Débloque :* un texte.

**F10. Les sorts que donne l'arbre (Relais, Sceau, Leurre) suivent-ils la même règle d'affichage que Foudre ?**
- a) Oui.
- b) Non, ils apparaissent toujours.
- **Reco : a.** Une seule règle : un sort de l'arbre apparaît quand son talent est enregistré.
- *Débloque :* une règle unique dans `SpellBook`.

---

## G. Points, coûts, profondeur, prérequis (10 questions)

**G1 ★ Combien de points de talent ?**
- a) Comme aujourd'hui : 2 à 30, puis 1 tous les 5 niveaux. Soit 5 à 45, 16 à 100.
- b) 2 à 30, puis 1 tous les 3 niveaux. Soit 7 à 45, 12 à 60, 25 à 100.
- c) 2 à 30, puis 1 tous les 2 niveaux. Soit 9 à 45, 37 à 100.
- d) 1 par niveau à partir de 30 (71 à 100), avec des nœuds plus petits.
- **Reco : b avec un tronc automatique (B1 a).** À 45 : une paire (1), 4 points dans un style et une clé, ou deux styles entamés. À 100 : deux paires, une Tresse et trois styles. a est trop court pour un arbre plus profond. d impose des nœuds de « +1 % », que le principe 1 refuse.
- *Débloque :* la profondeur (D5), la part de l'arbre prise à 100 (G8), et toutes les mesures.

**G2. Faut-il des points gagnés autrement que par le niveau (quête, succès) ?**
- a) Non.
- b) Un point avec la quête de passage « Une route à soi ».
- c) Des points de succès.
- **Reco : a.** Le canon dit que les Fragments ne donnent rien. Tous les joueurs d'un même niveau doivent rester comparables pour les mesures.
- *Débloque :* rien.

**G3. Combien coûte un nœud ?**
- a) 1 point, et 2 pour une clé (comme aujourd'hui).
- b) Un coût qui monte avec la profondeur (1, 1, 2, 3).
- c) Des nœuds à rangs (1/3, 2/3, 3/3), comme dans WoW.
- **Reco : a.** Les rangs multiplient les clics et les chiffres sans ajouter de décision.
- *Débloque :* `TalentSpec.Cost`.

**G4. Quels prérequis pour prendre un nœud ?**
- a) Seulement son parent (un arbre pur).
- b) Son parent, et X points dans la branche (comme aujourd'hui).
- c) Son parent, et un niveau minimal selon la profondeur.
- **Reco : a, et c pour les clés seulement (niveau 45).** C'est ce qu'un arbre dessiné laisse deviner : « je suis relié, donc je peux ».
- *Débloque :* la réécriture de `Talents.Problem` et de ses tests.

**G5. Le niveau minimal d'une clé : 45, comme on l'obtient aujourd'hui par les points ?**
- a) 45.
- b) 40.
- c) 50, avec les spécialisations.
- **Reco : a.** C'est la valeur mesurée au sprint 144.
- *Débloque :* une règle de niveau dans `Talents.Problem`.

**G6. Un nœud peut-il avoir deux parents (triplet, signature de paire) ?**
- a) Oui.
- b) Non, jamais : arbre strict.
- **Reco : a, au compte-gouttes** (triplets, signatures). Sinon les triplets n'ont pas de place.
- *Débloque :* C9.

**G7. Avec deux parents, faut-il l'un ou les deux ?**
- a) L'un (OU).
- b) Les deux (ET).
- **Reco : a**, sauf pour une signature de paire (sa paire et son style).
- *Débloque :* le format des prérequis dans `talents.json`.

**G8. Quelle part de l'arbre prend-on à 100 ?**
- a) Environ un quart, comme aujourd'hui (16 points pour 66 tuiles).
- b) Environ un tiers.
- c) La moitié.
- **Reco : b.** Assez pour un build complet, trop peu pour tout prendre.
- *Débloque :* G1.

**G9. Peut-on garder des points sans les dépenser ?**
- a) Oui (comme aujourd'hui).
- b) Non, le jeu insiste jusqu'à ce qu'ils soient dépensés.
- **Reco : a**, avec une pastille « points à dépenser » sur l'onglet.
- *Débloque :* une pastille dans le HUD.

**G10. Les prérequis à l'intérieur d'un style : en ligne ou en fourche ?** (Ça rejoint D6.)
- a) Chaque palier demande un nœud du palier précédent.
- b) Chaque palier demande tout le palier précédent.
- **Reco : a.**
- *Débloque :* la forme des styles.

---

## H. Exclusivités et limites (7 questions)

**H1. Garde-t-on « une seule grande bascule » (Conversion, Tresse triple, Pureté ou Crépuscule) dans tout l'arbre ?**
- a) Oui.
- b) Deux à partir de 75.
- **Reco : a.** C'est la décision d'identité du build.
- *Débloque :* E10.

**H2. Garde-t-on « deux éléments ouverts au plus » ?**
- a) Oui.
- b) Non.
- **Reco : a.** C'est cohérent avec C2 c.
- *Débloque :* rien de nouveau.

**H3. Les exclusions actuelles (D1 et D2, D7 et D8, Y1 et Y2, P3 Lumière ou Ténèbres) deviennent-elles des nœuds à choix ?**
- a) Oui.
- b) Non.
- **Reco : a** (D7).
- *Débloque :* L1.

**H4. Mêlée et Distance s'excluent-elles ?**
- a) Les deux styles entiers.
- b) Seulement à la clé : Corps à corps ou Artillerie.
- c) Pas du tout.
- **Reco : b.** On peut piocher dans les deux, mais l'identité se tranche à la clé.
- *Débloque :* une règle d'exclusion de plus.

**H5. Crépuscule compte pour deux éléments et exclut les paires. À l'écran, toutes les paires se grisent quand on le prend ?**
- a) Oui, avec la raison.
- b) Non, refus au clic seulement.
- **Reco : a.**
- *Débloque :* L5.

**H6. Faut-il montrer pourquoi un nœud est refusé avant de cliquer ?**
- a) Oui : un cadenas, et la raison dans l'infobulle (« il te faut Main tendue » ; « tu as déjà une grande bascule »).
- b) Seulement au clic, par un message.
- **Reco : a.** Aujourd'hui le grisé ne dit rien (0.1).
- *Débloque :* `Talents.Problem` doit rendre la raison nœud par nœud, ce qu'il fait déjà sous forme de texte.

**H7. Élément pur et les paires s'excluent-ils ?**
- a) Le nœud Élément pur exclut les paires.
- b) Seule la clé Pureté exclut les réactions (comme aujourd'hui).
- **Reco : b.** On peut alors prendre Cœur avec une réaction, et ne renoncer aux réactions qu'à la clé.
- *Débloque :* une exclusion de moins.

---

## I. Retours en arrière, changement de talents, migration (8 questions)

**I1. Comment rendre un nœud ?**
- a) Seulement une feuille (un nœud dont rien ne dépend).
- b) En cascade : rendre un nœud rend aussi ses enfants, avec un aperçu et une confirmation.
- c) Pas au détail : tout ou rien.
- **Reco : b.** a oblige à défaire dix clics, c est punitif.
- *Débloque :* la confirmation et l'aperçu des nœuds perdus (surlignés en rouge).

**I2. Le prix : gratuit jusqu'à 40, puis 10 × le niveau en Éclats. Pour tout refaire, ou par nœud ?**
- a) Pour tout l'enregistrement, quel que soit le nombre de nœuds rendus (comme aujourd'hui).
- b) Par nœud rendu.
- **Reco : a.** Le joueur expérimente sans compter.
- *Débloque :* rien de nouveau.

**I3. Faut-il toujours être dans un village pour rendre des talents ?**
- a) Oui.
- b) Non, partout hors combat.
- **Reco : a.** Le village reste un lieu de préparation.
- *Débloque :* rien de nouveau.

**I4. Faut-il un bouton « annuler les changements » avant l'enregistrement ?**
- a) Oui : on revient à l'arbre enregistré.
- b) Non : on ferme sans enregistrer.
- **Reco : a.** Le brouillon existe déjà (`PointsEditor`), il ne manque que le bouton.
- *Débloque :* un bouton.

**I5. À la sortie du nouvel arbre, que deviennent les talents déjà pris ?**
- a) Réinitialisation gratuite et unique pour tous les personnages, avec un message à la connexion.
- b) Conversion automatique quand l'identifiant existe encore, le reste rendu.
- **Reco : a.** C'est plus simple, plus honnête, et l'arbre est trop récent pour que quelqu'un y perde beaucoup.
- *Débloque :* une migration côté API et la commande admin.

**I6. Les trois personnages de niveau 100 de Plouf : un build de référence chacun, ou l'arbre vide ?**
- a) Vide, tu choisis en jeu.
- b) Un build de référence chacun, restauré par la commande admin.
- **Reco : b.** Tu testes un arbre rempli tout de suite, et tu peux le vider.
- *Débloque :* la commande admin de restauration de Plouf.

**I7. Plusieurs plans de talents par personnage (bascule solo ou groupe dans un village) ?**
- a) Pas maintenant.
- b) Deux plans.
- c) Trois plans.
- **Reco : a**, puis b avec les guildes. Le code de build (N4) couvre le besoin en attendant.
- *Débloque :* un champ de plus côté API, plus tard.

**I8. Changer de spécialisation à 50 réinitialise-t-il l'arbre ?**
- a) Non, les deux sont indépendants (règle de la v3).
- b) Oui.
- **Reco : a.**
- *Débloque :* rien.

---

## J. Équilibrage (7 questions)

**J1. Les plafonds du sprint 144 restent-ils la référence ?** Au plus 1,5 horde sur 10 au-dessus du meilleur héros nu ; les familles à ±1,5 victoire sur 30 l'une de l'autre ; au plus +35 % par coup.
- a) Oui.
- b) On les relâche pour le nouvel arbre.
- **Reco : a.**
- *Débloque :* les tests T41 et `TemplateTests`.

**J2. Le tronc automatique fait-il partie du « héros nu » ?**
- a) Oui : il devient la nouvelle base. On re-mesure les planchers T41 de 30 à 45, et on baisse les valeurs du tronc si les hordes deviennent trop faciles.
- b) Non : il compte comme un talent, et le héros nu n'a pas de tronc.
- **Reco : a.** Tout héros de 40 l'a, donc la vraie comparaison inclut le tronc.
- *Débloque :* les mesures.

**J3. Que devient la règle « Matière contre Manière à ±1,5 » ?**
- a) Chaque style à ±1,5 victoire sur 30 des autres styles, à 45.
- b) Chaque paire à ±1,5 des autres paires (en réaction seule).
- c) Les deux.
- **Reco : c.** On cherche les fausses décisions des deux côtés de la fourche.
- *Débloque :* de nouveaux tests d'équilibre.

**J4. Combien de builds de référence pour le nouvel arbre ?**
- a) 7, comme aujourd'hui (une Matière et une Manière par classe, plus un soutien).
- b) 9 : une paire et un style par classe, plus un mono-élément par classe.
- **Reco : b.** Le mono-élément a besoin d'un garde-fou mesuré.
- *Débloque :* `Progression.Builds`.

**J5. Faut-il que chaque paire soit viable pour chaque classe (18 combinaisons) ?**
- a) Oui, mesuré en CI.
- b) Oui, mesuré hors CI.
- c) Non, certaines paires vont mieux à certaines classes.
- **Reco : b.** C'est hors CI, parce que 18 combinaisons fois 100 combats coûtent cher, mais c'est mesuré.
- *Débloque :* un outil de mesure hors CI.

**J6. La règle « un talent pris par moins de 5 % des builds générés est revu » s'applique-t-elle aux nœuds à choix ?**
- a) Oui, par moitié de nœud.
- b) Non.
- **Reco : a.**
- *Débloque :* l'outil de mesure.

**J7. Peut-on rehausser les monstres de 30 et plus si le tronc rend tout trop facile ?**
- a) Oui.
- b) Non : on baisse plutôt le tronc.
- **Reco : b.** On touche une seule variable plutôt que toutes les familles de monstres.
- *Débloque :* J2.

---

## K. Interface : disposition et navigation (10 questions)

**K1 ★ La forme générale à l'écran ?**
- a) Un arbre **montant** : le tronc en bas, les branches qui montent, comme un vrai arbre.
- b) Un arbre **descendant**, comme ton dessin : le tronc en haut.
- c) **Radial** : le tronc au centre, les paires en rose autour, les styles vers l'extérieur.
- d) **Vue focalisée**, montante : le tronc et les 7 nœuds de la fourche toujours visibles. La paire sélectionnée déploie sa Tresse et ses styles au-dessus d'elle, et les autres paires restent en départs **pointillés** (exactement tes pointillés).
- **Reco : d.** Tout tient sur un écran de 1280 × 720 sans zoom. L'œil suit un seul chemin, et on bascule d'une paire à l'autre d'un clic. a et b ouverts en entier font 7 × 6 colonnes : il faudrait faire défiler l'écran. c est le plus beau, mais aussi le plus long à faire et le plus dur à lire pour les textes.
- *Débloque :* toute la maquette (P1), et K2 à K4.

**K2. Une fenêtre à part, ou l'onglet de la fiche personnage ?**
- a) L'onglet actuel (1140 × 620 px).
- b) Une fenêtre plein écran dédiée, avec son raccourci, et un bouton depuis l'onglet.
- **Reco : b.** Un arbre a besoin de place. L'onglet garde un résumé et le bouton « Ouvrir l'arbre ».
- *Débloque :* un raccourci clavier (lequel ?).

**K3. Zoom et déplacement ?**
- a) Aucun : tout tient.
- b) Molette pour zoomer, glisser pour déplacer.
- c) Boutons + et −, et une mini-carte.
- **Reco : a avec la vue focalisée.** b seulement si tu choisis l'arbre entier (K1 a, b ou c).
- *Débloque :* rien, ou un composant de caméra 2D.

**K4. Sur quoi l'arbre s'ouvre-t-il ?**
- a) La paire déjà prise.
- b) Le tronc.
- c) Ce qui est accessible maintenant.
- **Reco : a**, et b tant qu'aucune paire n'est prise.
- *Débloque :* rien.

**K5. Navigation au clavier ou à la manette ?**
- a) Flèches pour passer de nœud en nœud, Entrée pour prendre.
- b) Souris seulement.
- **Reco : a.** C'est utile pour l'accessibilité, et c'est peu coûteux dans un graphe de nœuds.
- *Débloque :* la navigation par voisins.

**K6. Résolution minimale ?**
- a) 1280 × 720.
- b) 1920 × 1080.
- **Reco : a.**
- *Débloque :* la taille des nœuds (L2).

**K7. Où placer le compteur de points, le résumé du build et les boutons ?**
- a) Une barre en haut (points, éléments ouverts, grande bascule) et des boutons en bas.
- b) Un panneau latéral à droite (résumé, détail du nœud, boutons).
- **Reco : b**, comme la carte de sort de l'onglet Sorts : le joueur retrouve un repère.
- *Débloque :* N1, K8.

**K8. Infobulle, ou panneau de détail ?**
- a) Une infobulle seulement (comme aujourd'hui).
- b) Un panneau seulement.
- c) Les deux : le survol donne une infobulle courte (nom, effet), le clic remplit le panneau (tout, plus les chiffres).
- **Reco : c.**
- *Débloque :* M4.

**K9. Le fond ?**
- a) Abstrait, sombre, aux couleurs de la charte, avec un tronc stylisé.
- b) Un arbre dessiné (écorce, racines, feuillage).
- **Reco : a pour l'instant.** b attend la direction artistique finale, reportée à la fin du jeu.
- *Débloque :* rien à commander aux artistes maintenant.

**K10. Quelles animations et quels sons ?**
- a) Aucun.
- b) Sobres : le lien s'allume de proche en proche jusqu'au nœud pris, et un son court.
- c) Riches : particules, nœud qui « pousse », musique.
- **Reco : b.** Un retour visible à chaque point dépensé, sans retarder le joueur qui en dépense dix.
- *Débloque :* un son à choisir.

---

## L. Interface : nœuds, liens, états (10 questions)

**L1. Une forme par type de nœud ?**
- a) Rond pour un nœud simple, hexagone pour une paire, losange pour une clé, carré à deux moitiés pour un nœud à choix, cercle plein pour le tronc.
- b) Une seule forme, avec la taille qui varie.
- **Reco : a.** On lit le type avant le texte.
- *Débloque :* la légende (M5).

**L2. La taille des nœuds ?**
- a) 56 px pour un nœud simple, 72 px pour une paire ou une clé.
- b) 44 px partout (le minimum tactile de la charte).
- **Reco : a.**
- *Débloque :* la mise en page.

**L3. Que montre un nœud ?**
- a) Une icône seule.
- b) Une icône, et son nom court dessous.
- c) Son nom seul (comme aujourd'hui).
- **Reco : b.** Il faut environ 60 icônes. On pourrait prendre game-icons.net (CC BY 3.0, avec un crédit) ou les générer en SVG. Voir P3.
- *Débloque :* une commande d'icônes.

**L4. Les couleurs ?**
- a) Une paire est bicolore (deux moitiés aux couleurs de ses parents). Un style a sa propre teinte. Une clé a un bord pistache. Le tronc est gris clair.
- b) Tout en gris, et seuls les nœuds pris en couleur.
- **Reco : a**, avec toutes les couleurs dans `tokens.css` et `ElementStyle`.
- *Débloque :* une palette de styles à valider.

**L5. Combien d'états distinguer ?**
- a) 3 : pris, accessible, verrouillé.
- b) 5 : **pris** (plein, lumineux) ; **accessible maintenant** (bord pulsé) ; **verrouillé** par un prérequis (sombre, cadenas) ; **exclu** (barré, avec la raison) ; **hors niveau** (« niv. 45 »).
- c) b, plus « trop cher » et « à venir ».
- **Reco : b**, avec « à venir » traité à part (L10).
- *Débloque :* les styles des nœuds.

**L6. Les liens ?**
- a) Trait plein pistache entre deux nœuds pris, trait gris vers un nœud accessible, pointillés vers un nœud verrouillé. Les pointillés servent aussi aux branches repliées (ton dessin).
- b) Un seul style de trait.
- **Reco : a.**
- *Débloque :* le dessin des liens.

**L7. Un chiffre sur le nœud ?**
- a) Le coût, seulement s'il vaut 2 (les clés).
- b) Toujours le coût.
- c) Aucun chiffre.
- **Reco : a.**
- *Débloque :* rien.

**L8. Au survol d'un nœud lointain, surligner tout le chemin et son coût total ?** (« 4 points pour y arriver ».)
- a) Oui.
- b) Non.
- **Reco : a**, comme Path of Exile. C'est l'aide à la planification la moins chère.
- *Débloque :* un calcul de chemin.

**L9. Daltonisme : des icônes d'élément en plus des couleurs ?**
- a) Oui, une petite icône sur les pastilles d'élément.
- b) Non.
- **Reco : a.** Feu et Terre, ou Eau et Air, se confondent pour certains joueurs.
- *Débloque :* 4 icônes d'élément.

**L10. Comment montrer ce qui n'existe pas encore (les composés à venir, les Pôles) ?**
- a) Grisé, avec « bientôt ».
- b) Masqué.
- c) En pointillés, sans texte.
- **Reco : c.** C'est ta convention, et ça ne promet pas de date.
- *Débloque :* C6.

---

## M. Interface : explications, noms, légende, infobulles (10 questions)

**M1. Faut-il renommer les couches ?** « Matière », « Manière », « Tresses » et « Pôles » sont du jargon : c'est ton retour sur les noms des colonnes.
- a) Tronc : **Fondations** ; fourche : **Affinités** ; styles : **Styles** ; Tresse : **Fusion** ; Pôles : **Élément pur**.
- b) Tronc : **Racines** ; fourche : **Paires** ; styles : **Voies**.
- c) On garde les noms, avec une explication.
- **Reco : a.** On comprend chaque mot sans le lire deux fois. Le scénariste doit valider (`talents-canon.md`).
- *Débloque :* les textes FR et EN.

**M2. Faut-il un bandeau en tête de chaque branche ?** Une icône, le nom, une phrase « pour qui » (« Mêlée : frappe fort au contact, au prix de ta portée »), et deux mots-clés.
- a) Oui.
- b) Le nom seul.
- **Reco : a.** C'est la réponse directe à « les noms des colonnes méritent explication ».
- *Débloque :* 13 phrases à écrire (7 nœuds de fourche et 6 styles), en FR et en EN.

**M3. Comment expliquer une paire ?**
- a) « Foudre = Feu + Air. Tes sorts sautent d'un ennemi à l'autre. Réaction : Électrisé. »
- b) Le nom seul.
- **Reco : a**, sur le nœud et dans le panneau.
- *Débloque :* 6 textes.

**M4. Que met-on dans l'infobulle ou le panneau d'un nœud ?**
- a) Le nom, l'effet (en vert), la contrepartie (en chocolat ou en rouge doux), les prérequis manquants, le coût.
- b) a, plus « ce que ça change pour toi », chiffré sur tes sorts (« Flammèche : 12-15 → 13-17 »).
- **Reco : b, en deux temps.** a d'abord, puis la ligne chiffrée.
- *Débloque :* un calcul « avant et après » à partir de `Talents.Compile` et `Talents.Percent`.

**M5. La légende : permanente, ou derrière un bouton ?**
- a) Toujours visible dans un coin.
- b) Derrière un bouton « ? », ouverte d'office à la première visite.
- **Reco : b.**
- *Débloque :* un panneau de légende.

**M6. Un tutoriel à la première ouverture (niveau 30) ?**
- a) Trois bulles guidées : le tronc, choisis ta paire, choisis ton style.
- b) Une quête de PNJ qui explique l'arbre dans le monde.
- c) Rien.
- **Reco : a**, et b plus tard si le scénariste en fait un moment d'histoire.
- *Débloque :* trois textes et un drapeau « vu » sur le compte.

**M7. Un glossaire au survol (empreinte, réaction, glyphe, composé) ?**
- a) Oui : les mots soulignés ouvrent leur définition.
- b) Non.
- **Reco : a.** Ces quatre mots reviennent dans la moitié des talents.
- *Débloque :* un petit glossaire FR et EN.

**M8. Le ton des descriptions ?**
- a) Sec et chiffré (comme aujourd'hui).
- b) Une phrase d'ambiance, puis les chiffres.
- **Reco : b**, l'ambiance en italique et en une ligne au plus.
- *Débloque :* 54 phrases à écrire (au scénariste ?).

**M9. Faut-il illustrer les mécaniques ?** (Un petit schéma animé « Eau, puis Terre = Enlisé » ; le losange de la Rémanence.)
- a) Oui, pour les nœuds de paire et quelques clés.
- b) Non.
- **Reco : a, plus tard.** Ça coûte cher, mais c'est l'« explication visuelle » que tu demandes, à réserver à ce qui ne se dit pas en une phrase.
- *Débloque :* une liste de 6 à 10 schémas.

**M10. Qui valide les noms ?**
- a) Toi seul.
- b) Le scénariste, puis toi.
- **Reco : b**, comme le prévoit `talents-canon.md`.
- *Débloque :* une passe du scénariste.

---

## N. Aperçu du build, planification, partage (8 questions)

**N1. Que montre le résumé du build ?**
- a) Les points dépensés et restants, les éléments ouverts (0/2), la grande bascule, les bonus et les contreparties cumulés (« −2 PO à tous tes sorts »), et l'avertissement de plafond (+35 %).
- b) Les points seulement.
- **Reco : a.** C'est l'« aperçu d'un build » de ta demande.
- *Débloque :* le panneau de droite (K7).

**N2. Un mode planification, pour poser des nœuds au-delà de ses points (fantômes) et viser un build de niveau 60 ?**
- a) Oui, impossible à enregistrer.
- b) Non.
- **Reco : a.**
- *Débloque :* un état « fantôme » des nœuds.

**N3. Des builds suggérés, à charger d'un clic ?**
- a) Oui : les builds de référence, avec leur nom (« Orage fêlé »).
- b) Non.
- **Reco : a**, en brouillon que le joueur ajuste. C'est une aide précieuse pour qui découvre l'arbre.
- *Débloque :* des noms et des textes pour les builds de référence.

**N4. Un code de build à copier et coller (comme le code de deck) ?**
- a) Oui.
- b) Non.
- **Reco : a.** On partage son build entre joueurs ou sur un forum.
- *Débloque :* un format de code.

**N5. Un bouton « tout réinitialiser », avec son coût affiché avant ?**
- a) Oui, avec une confirmation.
- b) Non.
- **Reco : a.**
- *Débloque :* un bouton.

**N6. Comparer deux builds (avant et après) ?**
- a) Oui : les nœuds gagnés et perdus surlignés avant d'enregistrer.
- b) Non.
- **Reco : a**, ce qui rejoint I1.
- *Débloque :* rien de plus.

**N7. Voir l'arbre d'un autre joueur (groupe, guilde) ?**
- a) Plus tard, avec les guildes.
- b) Tout de suite.
- **Reco : a.**
- *Débloque :* une route d'API en lecture.

**N8. Donner un titre au build, choisi par le joueur ou généré (« Sentinelle de la Foudre, au contact ») ?**
- a) Généré à partir de la paire et du style principal.
- b) Choisi par le joueur.
- c) Aucun.
- **Reco : a.** Le joueur se reconnaît en une phrase.
- *Débloque :* une règle de nommage.

---

## O. La fenêtre des sorts et le combat (7 questions)

**O1. Les pastilles de filtre de l'onglet Sorts : seulement les éléments que le héros possède vraiment ?**
- a) Oui (ce qui corrige le défaut 0.2).
- b) Tous, avec ceux qui ne sont pas possédés grisés.
- **Reco : a.**
- *Débloque :* le correctif.

**O2. Un badge « nouveau » sur les sorts gagnés par l'arbre ?**
- a) Oui, jusqu'au premier survol.
- b) Non.
- **Reco : a.**
- *Débloque :* un drapeau local.

**O3. Sur la carte d'un sort, une ligne « modifié par l'arbre : −2 PO (Au plus près) » ?**
- a) Oui.
- b) Non.
- **Reco : a.** Le joueur relie un changement de portée à sa cause.
- *Débloque :* `Texts.SpellCard`.

**O4. En combat, peut-on voir l'arbre ?**
- a) Oui, en lecture seule (comme la fiche aujourd'hui).
- b) Seulement un résumé des talents actifs.
- c) Non.
- **Reco : a.** Ça existe déjà pour la fiche (`ReadOnly`).
- *Débloque :* rien de neuf.

**O5. Faut-il des indicateurs de talents vivants en combat ?** (« Pied ferme actif », « Pas dansant +12 % ».)
- a) Des icônes d'état sur le portrait du héros.
- b) Seulement dans la prévision de dégâts.
- c) Rien.
- **Reco : a + b.** Un talent conditionnel dont on ne voit pas l'état est un talent qu'on ne joue pas.
- *Débloque :* des icônes d'état.

**O6. La prévision de dégâts doit-elle détailler les bonus de l'arbre au survol ?** (« +10 % Au plus près, +5 % tronc ».)
- a) Oui.
- b) Non.
- **Reco : a.**
- *Débloque :* une infobulle de la prévision.

**O7. L'onglet Caractéristiques doit-il afficher les bonus du tronc (PV, résistances) ?**
- a) Oui, dans une colonne « arbre ».
- b) Non.
- **Reco : a.** Sinon les chiffres de la fiche ne s'expliquent plus.
- *Débloque :* une colonne de la fiche.

---

## P. Méthode et livraison (5 questions)

**P1. Une maquette avant le code ?**
- a) Oui : une maquette statique (une image générée hors écran, sans fenêtre visible) que tu valides, puis le prototype Godot.
- b) Non, on code directement.
- **Reco : a.** Une maquette se change en une heure, l'interface Godot en un sprint.
- *Débloque :* le premier livrable.

**P2. Faut-il livrer en deux temps ?**
- a) Un seul sprint, tout d'un coup.
- b) Deux temps : (1) la structure (tronc, fourche, styles partagés) et la nouvelle interface ; (2) les signatures de paire et les schémas animés, avec les chapitres.
- **Reco : b.** Les guildes, prévues après l'arbre, glissent d'un ou deux sprints : à confirmer.
- *Débloque :* le plan (`PLAN.md`, `scrum.json`, risques R2 et R8), une fois que tu as dit go.

**P3. Les icônes ?**
- a) game-icons.net (CC BY 3.0, avec un crédit).
- b) Générées en SVG, dans le style des icônes de sorts actuelles (`SpellIcon`).
- c) Dessinées plus tard, avec la direction artistique finale.
- **Reco : b**, cohérent avec les sorts. a est un bon repli.
- *Débloque :* environ 60 icônes.

**P4. Comment valider ?**
- a) Tu joues un build de chaque classe avec Plouf.
- b) a, plus un joueur extérieur qui découvre l'arbre sans explication.
- **Reco : b.** La lisibilité se mesure sur quelqu'un qui ne connaît pas l'arbre.
- *Débloque :* le playtest.

**P5. Le critère de réussite ?**
- a) Après deux minutes dans l'arbre, un joueur explique son build en une phrase : « je suis une Garde Boue, au contact, qui pose des glyphes ».
- b) Autre critère.
- **Reco : a.**
- *Débloque :* le test de P4.

---

## Résumé des questions les plus structurantes

| ★ | Question | Ma Reco | Pourquoi elle bloque tout |
|---|---|---|---|
| A1 | Refondre le contenu ou seulement le dessin ? | restructurer en recasant les 54 talents | Elle fixe le chiffrage (5, 15 ou 30 points et plus) |
| B1 | Le tronc : automatique, payant ou à choix ? | automatique aux niveaux 30, 35 et 40 | Avec un tronc payant, il ne reste rien à dépenser à 30 et aucun style n'est atteint à 45 |
| D1 | Des styles propres à chaque paire, ou partagés ? | partagés tout de suite, avec une signature de paire à terme | Avec des styles propres, le contenu est multiplié par 6 ; avec des styles partagés, on a un graphe |
| G1 | Combien de points ? | 1 tous les 3 niveaux (7 à 45, 25 à 100) | Elle dicte la profondeur et toutes les mesures |
| K1 | La forme à l'écran ? | vue focalisée montante, avec les autres paires en pointillés | Elle conditionne la maquette et la fenêtre |

Juste derrière : **C12** (un style sans paire ?), **F2 et F4** (quand Foudre apparaît, et de quel passif tu parles), **C2** (combien de paires).
