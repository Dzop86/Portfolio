# Refonte de l'arbre des talents : état des lieux, diagnostic, pistes et questions *(game designer, 1er novembre 2026)*

> **Pourquoi ce document.** Charles veut que le prochain sprint soit une **refonte de l'arbre des talents**, avant les spécialisations (S181 à S184), et il veut d'abord en discuter. Il n'a pas encore dit ce qui le gêne. Ce document **prépare la discussion, il ne tranche rien**. Rien n'est planifié et aucun fichier de code ou de données n'a été modifié.
>
> **Lu avant d'écrire :** `data/talents.json` (l'arbre réel), `src/Rpg.Core/Talents.cs` (`Points`, `Trunk`, `Problem`, `Ceiling`, `Percent`), `Progression.Builds`, `docs/arbre-lisible.md` (l'espalier et la caméra, §7), `docs/mesures-arbre.md` (v1 et v2), `docs/talents-arbre.md` (v3, §1, §4.1, §7), `docs/talents-specialisations.md`, `docs/suite-apres-d69.md` §12, `DECISIONS.md` D80, D82, D86 et D87, `scrum/sprint-181.md` à `sprint-184.md`, et les retours UI de Charles du 30 octobre.
>
> **Ce qui ne bouge pas, quelle que soit la piste :** rien d'Ankama (D54) ; tout texte joueur sera écrit en FR et en EN au moment de la story (pas ici) ; le moteur des combats, le rejeu serveur et la règle de Charles sur le solo (1 à 3 monstres faciles, hordes de 6 à 10 faites pour le multi).

---

## 1. L'état des lieux (chiffres vérifiés dans les données et le code)

### 1.1 La structure

| Partie | Nœuds | Ce qu'on y trouve | Règles |
|---|---|---|---|
| **Tronc** | 3 (hors `talents.json`) | 30 : +3 % de dégâts ; 35 : +3 % de résistance ; 40 : +5 % de PV | automatique, aucun point (`Talents.Trunk`) |
| **Fourche : paires** (`Fork`) | 6 | une réaction chacune : Enlisé, Transi, Fêlé, Électrisé, Embué, Aveuglé | 2 paires au plus, sans élément commun (C2) |
| **Fourche : Élément pur** (`Pure`) | 4 Cœurs | +10 % dans un élément, −5 % dans les autres | un seul Cœur, compatible avec les paires |
| **Tresse de Foudre** (`Braids`) | 5 | 2 Transmutations (une au choix), Signature appuyée, Prisme, **Conversion** (clé, 45) | demande Électrisation ; une seule « grande bascule » dans l'arbre |
| **Alchimie** | 6 (3 + 3 clés) | durée des empreintes, Catalyse, Réaction prolongée ; clés Onde, Empreinte partagée, Virtuose | demande une **paire** (un Cœur ne suffit pas) |
| **Mêlée** | 4 (2 + 1 + 1 clé) | Au plus près, Pied ferme, Poussée lourde, Corps à corps | exclut en partie la Distance |
| **Distance** | 4 (1 + 1 + 2 clés) | Longue vue, Zone serrée, Artillerie, Ligne claire | |
| **Terrain** | 10 (3 + 4 + 3 clés) | Pas de côté, Adossé, Relais, Pas dansant, Sceau (4 sorts), Rémanence, Leurre, Terre brûlée, Nomade, Écho | |
| **Tempo** | 9 (3 + 3 + 3 clés) | Coups appuyés ou Petits coups, Premier geste, Relance courte, Réserve, Deck serré, Un geste par sort, Économie, Souffle long | |
| **Soutien** | 8 (1 + 4 + 3 clés) | Main tendue, Bannière, Double main, Élan partagé, Longue main, Dévouement, Harmonie, Relève | |
| **Total** | **56 talents + 3 cernes** | dont **16 clés** | |

**Les règles communes** (`Talents.Problem`) : 1 point par nœud, **2 par clé** ; les clés s'ouvrent au **niveau 45** ; **une clé par branche** ; un nœud demande **un** de ses parents (`requiresAny`) ; les 6 styles s'ouvrent avec **n'importe quelle porte** (paire ou Cœur), sauf l'Alchimie (une paire) ; 2 éléments ouverts au plus ; une seule Conversion. La règle des « points dépensés dans la branche » (`SecondFloor`) existe encore dans le code, mais elle ne s'applique plus : depuis D82, tout nœud hors porte a un parent, donc elle ne demande rien.

### 1.2 Les points

`Talents.Points(niveau) = 2 + (niveau − 30) / 3` (D82, G1 b).

| Niveau | 30 | 33 | 40 | 45 | 50 | 60 | 75 | 100 |
|---|---|---|---|---|---|---|---|---|
| Points | 2 | 3 | 5 | **7** | 8 | 12 | 17 | **25** |

**Ce qu'on peut tout prendre au plus**, en respectant les exclusions : 2 paires et 1 Cœur (3), la tresse avec la Conversion (4), l'Alchimie avec une clé (5), Mêlée et Distance combinées (6), le Terrain (9), le Tempo (7), le Soutien (7), soit **environ 41 points**. À 100, un héros prend donc **à peu près 60 % de ce que l'arbre permet**, avec **4 à 5 clés**. À 45, il en prend 7 sur 41 (17 %).

**Changer ses talents** : gratuit jusqu'au niveau 40, ensuite **10 × niveau** Éclats (450 à 45, 1 000 à 100), dans un village. Si l'arbre change de version (`Talents.Version`, à 3 aujourd'hui), chaque joueur récupère ses points **gratuitement, une fois** (I5 a) : ce mécanisme existe déjà et servirait à une refonte.

### 1.3 Ce que donne un nœud

Je classe chaque talent d'après son effet principal :

| Genre | Nombre | Lesquels |
|---|---|---|
| **Réaction** (une porte) | 6 | les 6 paires |
| **Un % de dégâts sous condition** (avec ou sans malus) | **19** | les 4 Cœurs, Signature appuyée, Virtuose, Au plus près, Pied ferme, Corps à corps, Artillerie, Adossé, Pas dansant, Terre brûlée, Coups appuyés, Petits coups, Premier geste, Un geste par sort, Bannière, Élan partagé |
| **Une règle changée** (portée, zone, vue, durée, relance, poussée, soutien) | 22 | les 2 Transmutations, Conversion, Prisme, Empreinte tenace, Catalyse, Réaction prolongée, Onde, Empreinte partagée, Poussée lourde, Longue vue, Zone serrée, Ligne claire, Rémanence, Écho, Relance courte, Main tendue, Double main, Longue main, Dévouement, Harmonie, Relève |
| **PA, PM ou taille du deck** | 6 | Pas de côté, Nomade, Réserve, Deck serré, Économie, Souffle long |
| **Des sorts en plus** | 3 | Relais, Sceau (4 glyphes), Leurre |
| **Total** | **56** | |

**Le plafond de +35 %** (`Talents.Ceiling`, `Talents.Percent`). Tous les % sous condition s'additionnent, **bonus et malus confondus**, puis la somme est plafonnée à +35 % sur un coup.

### 1.4 Ce que disent les mesures (`mesures-arbre.md`, S151 et S152)

- **Contre les petits groupes**, les 18 combinaisons de classe et de paire gagnent de 99 à 100 % : l'arbre ne change rien en solo, et c'est voulu.
- **Contre les hordes**, de 12 à 37 % ; l'avance de la Foudre vient de **sa tresse**, pas de sa réaction (S152 : sans la tresse, −0,2 point en moyenne).
- **Les nœuds rarement pris** (moins de 20 % des builds qui visent leur branche) : Empreinte partagée (9 %), Écho (13 %), Terre brûlée (14 %), Nomade, Un geste par sort, Souffle long (15 à 17 %), Économie (18 %). Ce sont tous des clés.
- **Les 9 builds de référence** (`Progression.Builds`) : les 3 builds « Matière » sont **tous les trois la Foudre convertie**, puisque c'est la seule tresse.

### 1.5 La présentation

L'espalier (D86) est entièrement déployé dans une caméra (D87), livré en S176 et S177 : tronc, portes, couronne, 7 couloirs, bois sans croisement, 4 aspects, zoom et panoramique. Charles ne l'a pas encore commenté depuis la livraison.

---

## 2. Le diagnostic du designer

### 2.1 Ce qui marche, et qu'il faut garder

1. **Les réactions.** Le combo en deux temps (« Eau puis Terre : Enlisé ! ») est la meilleure idée de l'arbre : une décision par tour, un effet visible, et une synergie de groupe naturelle (Empreinte partagée).
2. **Les talents qui changent une règle.** Corps à corps (portée 2 au plus, +25 %), Artillerie (rien à 1 ou 2 cases), Économie (deck de 8), Main tendue (soigner avec du Feu), Rémanence, Écho : chacun change **comment on joue**, pas seulement combien on frappe. C'est le principe n° 1 de la v3, et il marche quand il est appliqué.
3. **Les contreparties écrites** sur chaque nœud, et les explications de refus (`TalentReasons`) : on sait toujours ce qu'on paie.
4. **L'échelle de la Matière** (ouvrir, transmuter, convertir) donne la seule vraie bascule d'identité de l'arbre.
5. **Le socle technique** : la règle est unique (`Problem`), il y a un outil de mesure (`Rpg.Sim --talents`), des codes de build, un plan au niveau 60 et une remise gratuite quand la version change. Une refonte peut s'appuyer sur tout cela.

### 2.2 Ce qui pose problème, du plus grave au moins grave

**P1. Le plafond de +35 % rend des décisions fausses, et il efface les contreparties.** Le build de référence `sentinel-manner` (Au plus près +10, Pied ferme +15, Corps à corps +25) fait **+50 %**, plafonné à **+35 %** : **15 points de bonus sont perdus dans un build que nous avons nous-mêmes conçu**. La maquette de l'espalier le montre (« meilleur coup : +55 %, plafonné à +35 % »). Pire, comme les malus s'additionnent **avant** le plafond, **dès qu'on le dépasse, les contreparties ne coûtent plus rien** : le −5 % de Pied ferme quand on bouge, ou le −5 % du Cœur, disparaissent. À 100, avec 25 points, presque tout build qui empile trois de ces 19 nœuds atteint le plafond : on choisit alors celui dont le malus gêne le moins, ce n'est plus un choix de jeu. Le principe « chaque gain a son prix » ne tient plus à haut niveau.

**P2. Un tiers de l'arbre, ce sont des « +x % sous condition ».** Il y en a 19 sur 56. Ils se ressemblent dans la fiche (« +15 % si…, −5 % sinon »), on ne les sent pas en combat (la prévision les détaille, mais le joueur ne les voit pas jouer), et ils sont la cause directe de P1. C'est le défaut que `talents-specialisations.md` (§2.1) reprochait déjà au modèle à points : des points morts.

**P3. Ce n'est pas vraiment un arbre : c'est un carrefour.** Les 6 styles s'ouvrent avec **n'importe quelle** porte. La paire choisie ne décide donc ni du style ni de la branche : elle donne une réaction, puis on va où l'on veut. La fourche ne fourche pas. Le Cœur sert souvent de **péage** à 1 point pour entrer dans un style sans prendre de paire (ce qui explique en partie qu'il ait été rare dans les mesures de S151). L'espalier dessine honnêtement cette structure, mais c'est la structure elle-même qui est plate : 10 portes, une couronne, 6 menus.

**P4. Les builds se ressemblent de plus en plus avec le niveau.** À 45, 7 points sur 41 obligent à choisir. À 100, 25 sur 41 : on remplit 4 ou 5 branches et l'on n'en laisse que les nœuds faibles. La différence entre deux héros **diminue** avec le niveau, alors qu'elle devrait augmenter. Et de 45 à 100, il n'y a ni contenu ni nouveau nœud : 18 points tombent dans un arbre qui ne change plus.

**P5. L'arbre empiète déjà sur les spécialisations prévues (S181 à S183).** La règle de la v3 (§7.3 : « aucun talent ne cible une spécialisation ») est respectée à la lettre, mais **les effets se recouvrent** :

| Spécialisation (`talents-arbre.md` §7.2) | Ce qu'elle donne | Ce que l'arbre donne déjà |
|---|---|---|
| Sorcier | ses états durent +1 tour | **Réaction prolongée** : les états déclenchés durent +1 tour |
| Étau | retraits de PA et de PM +1 tour | Réaction prolongée (sur Transi et Enlisé) |
| Mire | +15 % sans ennemi à 3 cases, +1 PO | **Artillerie** (+15 %, rien à moins de 3 cases), **Longue vue** (+2 PO) |
| Archimage | +1 PA s'il n'a pas bougé ; relance −1 sur les gros sorts | **Pied ferme** (+15 % sans PM dépensé), **Relance courte**, **Coups appuyés** |
| Sente | +2 PM frappée au contact, sauts sans la vue | **Pas de côté**, **Nomade**, **Pas dansant** |
| Affût | ses glyphes durent +1 tour | **Sceau**, **Rémanence**, **Terre brûlée** |
| Palissade | boucliers de 6 aux voisins, boucliers +25 % | **Main tendue** (bouclier de Terre), **Harmonie** |

Si l'on livre les spécialisations sans toucher à l'arbre, une Mire en Distance ou un Sorcier en Alchimie **cumulera deux fois la même chose** (et butera sur le plafond, voir P1), et la spécialisation paraîtra fade à qui a déjà le talent. **C'est la meilleure raison de refaire l'arbre maintenant, et pas après S184.**

**P6. L'asymétrie de la Matière.** Une seule paire sur six a sa tresse : la Foudre. Les 3 builds Matière de référence sont tous de la Foudre, et toute l'avance de la Foudre contre les hordes vient de là (S152). La Glace (S184) réduira l'écart à 2 sur 6. Il reste **4 tresses, Lumière, Ténèbres et les triplets à venir** : à 5 nœuds par tresse, l'arbre va vers **85 à 90 nœuds**.

**P7. La charge de contenu.** Chaque nœud, ce sont 4 textes bilingues (nom, description, contrepartie, ligne d'ambiance), soit 8 chaînes, une icône (script `talent_art.py`), souvent une règle de moteur, et une place dans les mesures. 56 nœuds font **448 chaînes**, déjà relues en partie seulement (noms des couches, 56 lignes d'ambiance en attente de Charles depuis S151). Chaque nœud qu'on retire est un nœud de moins à équilibrer et à traduire.

**P8 (mineur). Des restes.** La règle `SecondFloor` ne s'applique plus ; l'Alchimie demande une paire alors que les autres styles acceptent un Cœur ; Distance et Mêlée s'excluent à moitié (Pied ferme et Poussée lourde restent possibles en Distance). Rien de grave, mais tout cela s'apprend dans l'infobulle et non pas en regardant l'arbre.

**En une phrase :** les fondations sont bonnes (réactions, règles changées, contreparties, outils), mais un tiers de l'arbre est fait de pourcentages qu'un plafond rend inutiles, la fourche ne fourche pas, les héros de 100 se ressemblent, et les spécialisations vont redonner ce que l'arbre donne déjà.

---

## 3. Les pistes de refonte

Quatre directions, de la plus légère à la plus profonde. L'effort est en points de sprint (1 à 4 par story), tests compris.

### Piste A : élaguer et resserrer (on garde la forme, on refait le contenu)

**Principe.** On garde le tronc, les portes, la tresse, les 6 styles, l'espalier et la caméra. On **retire ou fusionne une douzaine de nœuds « +x % »**, on transforme ceux qui restent en règles changées, on **supprime le plafond** (il n'a plus rien à couper) et on **retire de l'arbre tout ce qui fait doublon avec une spécialisation**. On vise **environ 40 nœuds**, au plus **6 en « % sous condition »**, un par style au maximum.

**Exemples concrets** (à valider) :
- Coups appuyés et Petits coups deviennent **un seul nœud à deux faces**, et Premier geste disparaît (doublon de rythme).
- Les Cœurs deviennent une règle : « ton élément pose son empreinte et la tient un tour de plus ; tu ne peux plus ouvrir de paire ». Ce serait une vraie porte, plus un péage.
- Réaction prolongée quitte l'arbre (elle revient au Sorcier), Artillerie perd son +15 % (Mire le porte), Pied ferme quitte l'arbre (Archimage).
- **Les points** : 2 à 30, 1 tous les 3 niveaux jusqu'à 45 (**7 à 45, rien ne change pour l'équilibre mesuré**), puis 1 tous les 5 niveaux (**18 à 100**, environ 45 % d'un arbre de 40 nœuds au lieu de 60 %).

| | |
|---|---|
| **Avantages** | Traite P1, P2, P4, P5, P7 sans jeter les deux sprints de l'espalier et de la caméra ; le moteur, les codes de build et la remise gratuite servent tels quels ; c'est la plus prévisible |
| **Risques** | P3 reste (la fourche ne fourche toujours pas) ; retirer des nœuds déçoit qui les aimait (Charles a demandé Rémanence et Soutien : on n'y touche pas) ; la puissance à 45 bouge, il faut remesurer |
| **Effort** | **8 à 9 points, 2 sprints** : contenu et règles (3), points, plafond, version 4 et builds de référence (2), remesure `Rpg.Sim` et T41/J3 (2), espalier recalculé et textes FR/EN (1 à 2) |
| **Casse** | les builds enregistrés (remise gratuite, version 4), les codes de build partagés, `mesures-arbre.md` (v3 à refaire), une partie des succès de talents (`Achievements`, cibles par id) |
| **Réutilise** | tout le moteur, `TreeLayout`, `TreeCamera`, le bois, les icônes en relief, `TalentReasons`, `TreePlan` |

### Piste B : des clés de voûte (moins de nœuds, plus lourds)

**Principe.** Chaque style se réduit à **2 nœuds mineurs** (des règles, pas de %) et **2 clés de voûte qui s'excluent**, chacune changeant le jeu en profondeur (sur le modèle des *keystones* de Path of Exile : un gros gain, un gros renoncement). Exemples : « Corps à corps » contre « Rempart mobile » en Mêlée ; « Économie » contre « Souffle long » en Tempo. Les points se font rares (**12 à 100**) : chaque point compte, et deux héros de 100 n'ont pas les mêmes clés.

| | |
|---|---|
| **Avantages** | Le choix le plus fort et le plus lisible (un héros se résume à ses 3 ou 4 clés) ; **environ 30 nœuds** ; règle P2 et P4 à la racine |
| **Risques** | Une clé dominante se voit tout de suite et se copie (il faut mesurer chaque paire de clés) ; 10 à 12 clés nouvelles, c'est du moteur nouveau ; peu de « petites récompenses » entre deux clés : la montée de 45 à 100 paraît plus vide |
| **Effort** | **11 à 13 points, 3 sprints** : clés nouvelles (4 à 5, moteur et tests), refonte des styles (2), points et version (1), remesure (2), espalier à 2 + 2 nœuds par style (1), textes (1) |
| **Casse** | la moitié des règles actuelles, les builds de référence, les mesures ; l'espalier reste, mais ses couloirs rétrécissent |
| **Réutilise** | le moteur des règles, la caméra, le bois, les portes et la tresse |

### Piste C : des rangées de choix (plus d'arbre à points)

**Principe.** Plus de points à dépenser : à 30, 35, 40, 45, 55, 65, 80 et 100, une **rangée de 3 talents, on en prend 1** (comme les rangées de talents de World of Warcraft depuis Mists of Pandaria). Les portes deviennent la rangée de 30, la tresse une rangée de Matière. C'était le modèle hybride de `talents-specialisations.md` §3.

| | |
|---|---|
| **Avantages** | La lisibilité maximale (8 décisions, chacune entre 3 options de même poids) ; l'équilibre le plus simple (on règle chaque rangée à part, en 3 passes) ; plus de plafond ni de points morts |
| **Risques** | **Ce n'est plus un arbre**, alors que Charles a demandé « une notion arborescente vraiment » (D81) puis « un vrai arbre » (D82, A1 b), et que deux sprints viennent d'être consacrés à l'espalier et à sa caméra (12 points) ; on perd l'image d'un arbre qui pousse ; les combos libres entre styles disparaissent |
| **Effort** | **12 à 14 points, 3 sprints**, plus les 12 points de l'espalier jetés en grande partie |
| **Casse** | l'espalier, la caméra (ou alors une caméra pour 24 tuiles), `TreePlan`, les codes de build, les mesures |
| **Réutilise** | les règles des talents (la plupart deviennent des options de rangée), les textes, les icônes |

### Piste D : un tronc commun, des branches de classe (l'arbre absorbe les spécialisations)

**Principe.** Le bas de l'arbre reste commun (tronc, portes, réactions, tresses : « avec quoi je frappe »). Au-dessus, les 6 styles communs deviennent **3 styles communs + 3 branches de la classe**, qui **sont les spécialisations** : à 50, choisir Mire, Affût ou Sente revient à ouvrir une des trois branches de classe de l'arbre, avec son passif, sa règle et ses sorts. On n'a plus deux systèmes qui se recouvrent, mais **un seul arbre, une seule fenêtre et une seule monnaie de points**.

| | |
|---|---|
| **Avantages** | Traite P5 à la racine ; les classes se distinguent enfin dans l'arbre ; la fourche devient vraie (une branche de classe exclut les deux autres) ; S181 à S183 sont absorbés au lieu d'être ajoutés |
| **Risques** | **Revient sur D80** (« l'arbre commun aux classes ») ; c'est la piste la plus coûteuse en contenu (3 classes × 3 branches × environ 5 nœuds, soit 45 nœuds de classe) et en équilibre (9 branches à mesurer contre la moyenne de leur classe) ; elle retarde la Glace |
| **Effort** | **16 à 18 points, 4 sprints**, dont environ 12 remplacent S181 à S183 : **environ 5 points de plus** que le plan actuel |
| **Casse** | D80, l'ordre de S181 à S184, les builds de référence, l'espalier (9 couloirs de classe, dont 3 visibles à la fois) |
| **Réutilise** | les designs des 9 spécialisations (§7.2 de la v3), le moteur des passifs des monstres, la caméra, le bois, les portes et la tresse |

### Synthèse

| | A : élaguer | B : clés de voûte | C : rangées | D : branches de classe |
|---|---|---|---|---|
| Traite P1 et P2 (plafond, %) | oui | oui | oui | en partie |
| Traite P3 (la fourche) | non | en partie | sans objet | **oui** |
| Traite P4 (héros de 100) | en partie | **oui** | oui | oui |
| Traite P5 (spécialisations) | oui (on retire les doublons) | en partie | en partie | **oui (fusion)** |
| Garde l'espalier et la caméra | **oui** | oui | non | en partie |
| Contredit une décision de Charles | non | non | D81, D82 | D80 |
| Points | **8 à 9** | 11 à 13 | 12 à 14 (+12 jetés) | 16 à 18 (dont 12 déjà prévus) |

### Ma recommandation : la piste A, en ouvrant la porte de D

**A**, parce qu'elle traite les défauts **mesurables** (le plafond qui perd 15 points dans nos propres builds, les 19 nœuds « % », les doublons avec les spécialisations, les héros de 100 qui se ressemblent) en 2 sprints, sans jeter l'espalier qu'on vient de livrer, et sans revenir sur une décision de Charles. Les spécialisations S181 à S183 démarrent alors sur un terrain dégagé : elles portent le **métier**, et l'arbre les **règles**.

**Avec une seule idée de D, peu coûteuse :** la spécialisation choisie à 50 apparaît **dans la fenêtre de l'arbre**, comme une 8e branche (au-dessus de la couronne, à droite), sans points à dépenser. On a ainsi « une seule fenêtre » sans fusionner les deux systèmes : environ 1 point, à placer dans S181.

**Je change de recommandation si Charles répond à Q1 :**
- « **c'est encore illisible** » (a) : la refonte doit d'abord **retirer** des nœuds, et A le fait ; si cela ne suffit pas, alors B.
- « **les choix ne m'excitent pas** » (c) : **B**.
- « **les trois classes sont pareilles** » (e) : **D**.

---

## 4. Les questions pour Charles

> Réponds par une lettre, « Reco », ou en texte libre. **Q1 et Q2 suffisent pour lancer le sprint** ; les autres peuvent prendre ma recommandation.

**Q1 ★. Qu'est-ce qui te gêne dans l'arbre actuel ?** (plusieurs réponses possibles, c'est la question qui décide de tout)
- a) Il reste **difficile à lire**, même déployé et avec le zoom : trop de nœuds, trop de texte par nœud
- b) Il y a **trop de nœuds** pour ce qu'ils apportent : j'hésite sans savoir ce qui compte
- c) Les choix **ne m'excitent pas** : beaucoup de « +15 % si… », je ne les sens pas en combat
- d) **La Foudre écrase tout** : une seule paire a sa tresse, les autres paires sont vides au-dessus
- e) **Les trois classes construisent pareil** : rien dans l'arbre ne dit que je suis Garde
- f) **La fourche ne fourche pas** : ma paire ne décide de rien au-dessus, les styles sont ouverts à tous
- g) **La progression** : trop ou trop peu de points, rien de neuf entre 45 et 100
- h) Autre chose (dis-le en une phrase, ou une capture)
- *Pas de recommandation ici : c'est ton ressenti qui compte. Mon diagnostic penche pour c, f et un risque à venir que tu n'as pas encore pu voir (les doublons avec les spécialisations, P5).*

**Q2 ★. L'ampleur de la refonte.**
- a) **Élaguer et resserrer** : on garde la forme, une quarantaine de nœuds, plus de plafond, plus de doublons avec les spécialisations (piste A, 8 à 9 points) **(Reco)**
- b) **Des clés de voûte** : environ 30 nœuds, 2 clés exclusives par style, des points rares (piste B, 11 à 13 points)
- c) **Des rangées de choix** : plus d'arbre à points, 1 talent sur 3 à 8 paliers (piste C, 12 à 14 points, l'espalier est jeté)
- d) **Des branches de classe** : l'arbre absorbe les spécialisations (piste D, 16 à 18 points dont 12 déjà prévus)

**Q3. Les pourcentages de dégâts sous condition** (19 nœuds sur 56, le plafond de +35 %).
- a) On en garde **au plus un par style** (6 en tout), on transforme les autres en règles changées ou on les retire, et **le plafond disparaît** **(Reco)**
- b) On les garde tous, mais le plafond s'applique **après** les malus (les contreparties restent toujours payées)
- c) On garde tout tel quel

**Q4. Le nombre de points.**
- a) Tel quel : 7 à 45, **25 à 100** (environ 60 % de l'arbre)
- b) 2 à 30, 1 tous les 3 niveaux jusqu'à 45, **puis 1 tous les 5** : 7 à 45 (l'équilibre mesuré ne bouge pas), **18 à 100** **(Reco)**
- c) 1 tous les 5 niveaux dès 30 : **5 à 45, 16 à 100** (comme la v3 d'origine ; il faut remesurer 45)

**Q5. Les portes et les styles.**
- a) Tel quel : n'importe quelle porte ouvre les 6 styles (sauf l'Alchimie)
- b) **Les styles s'ouvrent depuis le tronc**, sans porte : les portes ne servent plus qu'aux éléments (réactions, tresses, Cœurs). On respecte enfin « un héros peut rester tout en Manière » (v3, §2.1), et le Cœur n'est plus un péage **(Reco)**
- c) **Chaque porte n'ouvre que 2 ou 3 styles** : la fourche fourche vraiment, mais certaines combinaisons deviennent impossibles

**Q6 ★. La frontière avec les spécialisations** (S181 à S183).
- a) L'arbre garde les **règles** (portée, rythme, terrain, réactions), la spécialisation le **métier** ; on **retire de l'arbre** ce qui fait doublon (Réaction prolongée, le +15 % d'Artillerie, Pied ferme…) **(Reco)**
- b) On garde l'arbre tel quel et on **change les spécialisations** pour qu'elles ne doublonnent plus
- c) On laisse les doublons se cumuler : ce sont des synergies voulues
- d) On fusionne les deux (piste D)

**Q7. La spécialisation dans la fenêtre de l'arbre.**
- a) Oui : à 50, elle apparaît comme une **8e branche** de l'arbre, sans points à dépenser (environ 1 point, dans S181) **(Reco)**
- b) Non : elle a sa propre fenêtre (le panneau Voie prévu)

**Q8. Les tresses et l'avance de la Foudre.**
- a) La Glace en S184, comme prévu, puis une tresse par sprint de socle de chapitre **(Reco)**
- b) On fait les 5 tresses manquantes dans la refonte (environ 15 points de plus)
- c) On retire la tresse de Foudre en attendant que les autres existent (la Matière se réduit aux portes)

**Q9. Les héros existants (Plouf, Plop, les joueurs).**
- a) Version 4 de l'arbre : **chacun récupère tous ses points gratuitement**, une fois, avec un mot à la connexion (le mécanisme existe) ; Plouf est restauré par la commande d'administration **(Reco)**
- b) Remise gratuite **et changements gratuits pendant une semaine**
- c) On essaie de traduire automatiquement les anciens builds (les codes partagés restent valides, mais c'est fragile et cher)

**Q10. Le calendrier.**
- a) **Deux sprints de refonte** (S181 et S182), les spécialisations décalées de deux sprints (S183 à S186) **(Reco pour A)**
- b) Un seul sprint (on ne fait que Q3 et Q6, environ 5 points), le reste plus tard
- c) La refonte fondue dans les sprints de spécialisations (piste D)

---

*Après tes réponses : je chiffre la piste retenue story par story, avec ses tests (xUnit sur `Talents.Problem`, `Percent` et les builds de référence ; remesure `Rpg.Sim --talents` ; auto-test de l'espalier), puis la gestion de projet est mise à jour (décision D92, points, sprints, `PLAN.md`, R2, R8).*
