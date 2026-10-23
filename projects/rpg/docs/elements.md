# Osmose : les éléments, quatre de base, onze composés, deux indépendants *(proposition v1, 24 octobre 2026)*

> **Demande de Charles** (après lecture de `docs/talents-arbre.md`, v2, §5) : « J'imagine : Feu, Eau, Air, Terre, Lumière et Ténèbres.
> Règles :
> - Feu, Eau, Air et Terre peuvent être combinés entre eux.
> - Lumière et Ténèbres sont des éléments indépendants et ne participent à aucune combinaison.
> - Chaque combinaison de 2, 3 ou 4 éléments produit un nouvel élément unique.
> - Évite les doublons et les résultats trop similaires (ex. : Magma et Volcan).
> - Privilégie des éléments fondamentaux, cohérents et distincts plutôt que des lieux ou des phénomènes spécifiques.
> - Propose un nom et une courte description pour chaque résultat.
> - Présente toutes les combinaisons possibles dans un tableau clair, sans oublier aucune combinaison. »
>
> **C'est un aller-retour, pas une spec.** Ce document répond à Charles et remet en cause le §5 de `talents-arbre.md` (où Lumière = Terre + Feu et Ténèbres = Terre + Eau) sans le modifier : si Charles retient ce modèle, le §5, le §6 et la branche Tresses (§4.3) seront réécrits ensuite. Rien n'est planifié ni implémenté. Les noms sont des **noms de travail** ; leur sens dans le monde revient au scénariste.
>
> Lu avant d'écrire : `docs/talents-arbre.md` (§4.3, §5, §6, §9, §11), `docs/bible.md` (quatre Primordiaux, l'Accord « non pas un cinquième élément », la Saga II « Renaissance de l'Accord »), `src/Rpg.Core/Data.cs` (`Characteristics` : Vitalité + les quatre éléments, 2 dégâts par tranche de 10 points).

---

## Décisions de Charles (24 octobre)

Charles a validé la proposition (« c'est parfait ») et tranché les sept questions du §5 selon les valeurs par défaut :

| # | Décision |
|---|---|
| 1 | La signature d'une paire est la petite version de sa réaction. **Attisé devient Électrisé.** |
| 2 | Les onze noms sont retenus tels quels : Foudre, Glace, Boue, Magma, Vapeur, Sable, Éther, Cendre, Sève, Sel et Unisson. **Magma et Cendre restent tous les deux.** |
| 3 | Les triplets sont un talent final (une clé) de Tresses, exclusif avec Conversion et Pureté. Ils ont 4 sorts par classe et frappent sur 2 × la moyenne des parents. |
| 4 | Unisson est réservé à la Saga II, hors de l'arbre de la Saga I. |
| 5 | Lumière **ou** Ténèbres s'obtient par un talent d'étage 2 et frappe sur la plus haute des quatre caractéristiques d'élément. |
| 6 | Lumière et Ténèbres sont des polarités du héros, ni bien ni mal. Le scénariste doit le valider. |
| 7 | Crépuscule est retenu : les 16 sorts d'élément sont perdus et la frappe se fait sur la somme des quatre, sans bonus. C'est une clé exclusive des composés. Le Demi-crépuscule reste en repli. |

Ces règles sont reprises dans l'arbre v3 (`docs/talents-arbre.md`), qui **remplace le §4.3 de ce document**. Deux points s'y précisent : Lumière, Ténèbres, Pureté et Crépuscule y forment leur propre branche, **Pôles**, et le remplacement partiel s'appelle **Transmutation**.

---

## 1. Le compte : onze composés, aucun oublié

Quatre éléments de base, combinés sans ordre et sans répétition : C(4,2) + C(4,3) + C(4,4) = **6 + 4 + 1 = 11**. Vérification par ce qui manque : chaque triplet est défini par **l'élément absent** (4 éléments, donc 4 triplets), le quadruplet par aucun absent.

| | Terre | Feu | Eau | Air |
|---|---|---|---|---|
| **Terre** | base | Magma | Boue | Sable |
| **Feu** | | base | Vapeur | Foudre |
| **Eau** | | | base | Glace |
| **Air** | | | | base |

Triplets : sans Terre (Éther), sans Eau (Cendre), sans Air (Sève), sans Feu (Sel). Quadruplet : Unisson.

## 2. Le tableau complet

Règle de lecture proposée au joueur : **une paire est une matière, un triplet est « ce qui reste sans le quatrième », le quadruplet est l'accord des quatre.**

| # | Rang | Nom (fr / en) | Parents | Description courte | Signature (une ligne) |
|---|---|---|---|---|---|
| 1 | paire | **Foudre** / Lightning | Feu + Air | le feu porté par le vent : rapide, qui saute d'un corps à l'autre | **Arc** : 35 % des dégâts sautent sur l'ennemi le plus proche à 2 cases (un saut) |
| 2 | paire | **Glace** / Ice | Eau + Air | l'eau saisie par le vent froid : ce qui fige et mord | **Morsure** : −1 PA, 1 tour |
| 3 | paire | **Boue** / Mud *(alt. Limon / Silt)* | Terre + Eau | la terre gorgée d'eau : lourde, qui colle aux pieds | **Engluement** : −1 PM, 1 tour |
| 4 | paire | **Magma** / Magma *(alt. Lave / Lava)* | Terre + Feu | la roche en fusion : ce qui fend et fait fondre les défenses | **Fonte** : −8 % de résistance à tous les éléments, 1 tour |
| 5 | paire | **Vapeur** / Steam *(alt. Brume / Mist)* | Feu + Eau | l'eau changée en souffle chaud : ce qui voile et étouffe | **Voile** : −10 % de dégâts infligés, 1 tour |
| 6 | paire | **Sable** / Sand *(alt. Poussière / Dust)* | Terre + Air | la terre émiettée par le vent : ce qui aveugle et use | **Grain** : −1 PO à ses sorts, 1 tour (jamais sous 1) |
| 7 | triplet, sans Terre | **Éther** / Aether *(alt. Nuée / Nimbus, mais trop proche de Vapeur)* | Feu + Eau + Air | ce qui n'a pas de poids : la matière la plus légère, qui passe partout | **Traversée** : le sort ne demande pas la ligne de vue |
| 8 | triplet, sans Eau | **Cendre** / Ash *(alt. Soufre / Sulphur)* | Terre + Feu + Air | ce qui reste quand rien ne l'éteint : sèche, qui retombe et couvre le sol | **Retombée** : la case de la cible devient une glyphe de cendre 2 tours (dégâts à qui y commence son tour) |
| 9 | triplet, sans Air | **Sève** / Sap *(alt. Germe / Seed)* | Terre + Feu + Eau | la terre, l'eau et la chaleur sans le vent : ce qui pousse et nourrit | **Bourgeon** : l'allié le plus blessé à 2 cases est soigné de 20 % des dégâts infligés |
| 10 | triplet, sans Feu | **Sel** / Salt *(alt. Cristal / Crystal, mais proche de Glace)* | Terre + Eau + Air | ce que l'eau laisse en partant au vent : ce qui conserve et dessèche | **Salure** : −50 % de soins reçus, 2 tours |
| 11 | quadruplet | **Unisson** / Unison *(alt. Concorde / Concord)* | Terre + Feu + Eau + Air | les quatre qui sonnent ensemble sans se confondre | **Concordance** : le coup frappe dans la **plus faible** des quatre résistances de base de la cible |

**Ce que j'ai vérifié.**
- *Aucun lieu, aucun phénomène trop précis* : pas de Volcan, Orage, Tempête, Marais, Désert, Blizzard. Toutes sont des matières ou des états (Boue, Sable, Sel, Cendre, Sève), sauf Foudre et Glace, gardées parce que ce sont les exemples de Charles et les plus lisibles.
- *Les paires trop proches* : Magma et Cendre (Terre + Feu toutes deux) sont deux états opposés, l'un en fusion, l'autre éteinte ; Boue et Sable, l'humide et le sec ; Glace et Sel, le froid et le sec. Je les juge distinctes ; Charles tranche (question 2). Les alternatives Nuée et Cristal sont signalées comme trop proches de Vapeur et Glace.
- *Les signatures* : 13 en tout (avec Lumière et Ténèbres, §3), aucune en double. Éther recoupe un peu le talent D9 *Ligne claire* (ligne de vue), c'est acceptable.
- *Le canon* : **Éther** et **Quintessence** désignent traditionnellement un cinquième élément, ce que la bible refuse pour l'Accord. J'ai écarté Quintessence ; Éther reste en nom de travail, au scénariste de dire s'il tient. **Unisson** est volontairement l'image de l'Accord (les quatre ensemble) : c'est une force si on le réserve à la Saga II (« Renaissance de l'Accord »), un spoiler sinon (§4.3).

### 2.1 Les paires et les réactions : une seule grammaire

Les six paires sont exactement les six réactions du §6 de la v2. Garder deux vocabulaires (Boue **et** Enlisé, Vapeur **et** Embué) ferait 12 effets à apprendre pour 6 idées. Proposition : **la signature d'un composé de paire est la petite version de la réaction de la même paire.**

| Paire | Composé, signature (chaque coup, payée au budget) | Réaction v2 (deux coups, gratuite, plus forte) |
|---|---|---|
| Terre + Eau | Boue, −1 PM | Enlisé, −2 PM |
| Eau + Air | Glace, −1 PA | Transi, −2 PA |
| Terre + Feu | Magma, −8 % de résistance | Fêlé, −15 % de résistance, 2 tours |
| Feu + Eau | Vapeur, −10 % de dégâts | Embué, −25 % de dégâts |
| Terre + Air | Sable, −1 PO | Aveuglé, −2 PO |
| Feu + Air | Foudre, Arc 35 % | **Électrisé** *(remplace Attisé)* : pendant 1 tour, chaque coup reçu par la cible saute à 35 % sur l'ennemi le plus proche à 2 cases |

Le choix devient lisible : **le composé, c'est la fiabilité** (l'effet à chaque coup, mais moins fort et payé en dégâts) ; **la réaction, c'est le pic** (deux coups de deux éléments, gratuit, plus fort). Le mono-élément converti et l'hybride qui combote jouent la même idée de deux façons. Le prix : la brûlure d'Attisé disparaît (elle peut revenir comme signature d'une spécialisation ou d'une famille).

## 3. Lumière et Ténèbres, éléments indépendants

### 3.1 Comment on les obtient

| Piste | Pour | Contre |
|---|---|---|
| **A. Un talent d'ouverture**, étage 2 de Tresses, Lumière **ou** Ténèbres (exclusifs) | une seule règle pour tous les éléments ; accessible dès 31-32 | encore un élément dans l'arbre |
| B. Un choix à la spécialisation (50) | moment fort | casse la règle de séparation de la v2 (« une spécialisation ne touche aucun élément ») et multiplie 9 spécialisations par 2 |
| C. Une quête ou un Fragment | narratif | la bible dit qu'un Fragment ne double pas l'arbre ; et une quête n'est pas un choix |

**Recommandation : A**, exclusif (on est de la Lumière **ou** des Ténèbres, jamais les deux), compte dans la limite de deux éléments ouverts, **4 sorts** par classe, 8 avec *Plein grimoire*. Pour avoir les deux à la fois, il faut la **Conversion Crépuscule** proposée par Charles (§3.4).

### 3.2 Sur quelle caractéristique

Sans parents, la somme du §5.2 ne s'applique pas. Quatre options :

| Option | Effet sur un mono-Feu (450) | Sur un hybride Feu/Air (225/225) | Verdict |
|---|---|---|---|
| **La plus haute** des quatre caractéristiques d'élément | 450 | 225 | **recommandée** |
| La moyenne des quatre | 112 | 112 | faible pour tous, il faut gonfler la base : un neutre bis |
| Vitalité | dépend de la Vitalité | idem | la Vitalité donnerait PV **et** dégâts : dominant, et casse les builds de tank |
| Une sixième caractéristique | à part | à part | fiche à 6 cases, builds existants faux, refusé dès la v2 |

**La symétrie qui en sort** : les composés récompensent l'**hybride** (leur valeur grandit avec plusieurs éléments), Lumière et Ténèbres récompensent le **mono-élément** (« ce que tu es le plus »). *Pureté* (T9) les tolère comme des neutres. Résistances des monstres : deux colonnes nouvelles, **0 par défaut** (aucune des 50 fiches à réécrire), une famille peut en porter une.

### 3.3 Leur signature

| Élément | Description | Signature |
|---|---|---|
| **Lumière** / Light | ce qui révèle et dissipe, ni bien ni camp | **Dissipation** : retire à la cible ses boucliers et un statut bénéfique |
| **Ténèbres** / Darkness *(alt. Ombre / Shadow)* | ce qui cache et prend, ni mal ni Disjonction | **Ponction** : le lanceur reprend 20 % des dégâts infligés en PV |

### 3.4 La piste de Charles : une Conversion vers la Lumière et les Ténèbres

> **Ajout de Charles** : les mettre dans l'arbre commun sous la forme d'une Conversion : on perd 16 sorts, on gagne 8 sorts de Lumière et 8 de Ténèbres.

**Le fait qui décide** : chaque classe a **4 sorts par élément de base** (16) et **8 neutres** (`spells_gen.py`, §3 de la v2). « Deux éléments complets » ne font donc que **8 sorts**. **Perdre 16 sorts, c'est perdre les quatre éléments** (4 de chacun) : le héros converti n'a plus que ses 8 neutres, 8 sorts de Lumière et 8 de Ténèbres. Son grimoire garde 24 sorts. Les neutres ne sont jamais retirés : ils portent l'identité de la classe (poussées, sauts, attirances, soins).

| Variante | Perdus | Gagnés | Éléments de base gardés |
|---|---|---|---|
| **Crépuscule** *(la piste de Charles)* | les 16 sorts d'élément | 8 Lumière + 8 Ténèbres | **0** |
| Demi-crépuscule | les 8 sorts de **deux** éléments au choix du joueur | 4 Lumière + 4 Ténèbres | 2 |

**La caractéristique.** Pour la Conversion : **la somme des caractéristiques perdues**, donc des quatre éléments pour Crépuscule (deux pour le Demi). Ça vaut la force d'un mono-élément (450 points à 45), jamais plus, et ça évite un piège : un hybride 225/225 qui se convertit ne se retrouve pas à moitié de sa force à devoir payer une redistribution. L'*Ouverture* (§3.1) garde la règle « la plus haute » : avec la somme, une simple ouverture donnerait la pleine force à tout le monde pour 1 point.

**Le prix réel, comparé à T8.** 16 sorts contre 8 se lit comme « deux fois plus cher », mais ce n'est pas le vrai prix :

| | Conversion T8 (paire) | Crépuscule |
|---|---|---|
| Sorts d'élément | 8 perdus, 8 gagnés | 16 perdus, 16 gagnés |
| Éléments de base gardés | 2, donc une réaction possible entre eux | 0 : **aucune réaction**, la branche Réactions devient morte |
| Composés | non (une seule conversion) | **exclus** : sans éléments de base, aucun parent |
| Pureté (T9) | exclue | exclue |
| Bonus | +15 %, signature +50 % | **aucun** : deux signatures sur tous les coups suffisent |
| Résistances des monstres | moyenne des parents, les faiblesses comptent | 0 partout par défaut : ni faiblesse à exploiter, ni mur |

Le vrai prix est **la perte de tout le jeu hybride** (réactions, composés, choix d'élément selon la famille), pas le nombre de sorts. En échange, le converti a la performance la plus **régulière** du jeu : jamais faible, jamais fort contre une famille.

**L'équilibre.**
- *À 0 élément gardé* : le risque est le **build sûr dominant**, car il ignore la leçon de chaque famille (« faible au Feu »). Parade : les familles gagnent au fil des chapitres une résistance ou une faiblesse en Lumière ou en Ténèbres quand le thème le permet, et le build de référence « Crépuscule » entre dans les mesures T41 ; s'il dépasse le seuil du talent dominant (1,5 victoire sur 30), on retire 10 % à ses sorts.
- *À 2 éléments gardés* (Demi) : plus souple, une réaction reste possible ; mais c'est une T8 bis, moins marquante.

**Mon avis.** Crépuscule est une vraie fantaisie d'identité (« je ne frappe plus qu'en lumière et en ombre ») et un vrai renoncement : je la garde **telle que Charles la propose**, à 0 élément gardé, comme **clé de Tresses** (2 points, une parmi Conversion de paire, Pureté, Tresse triple, Crépuscule), donc pas avant 45. Elle exclut les composés, l'*Ouverture : Lumière / Ténèbres* (elle la contient) et rend inutiles les réactions. Le Demi-crépuscule reste en réserve si les mesures la montrent dominante. Côté contenu, elle ne coûte rien de plus : ses 8 + 8 sorts sont ceux que *Plein grimoire* ouvre déjà pour chaque polarité (§4.2).

**Canon (au scénariste)** : deux éléments sans parents se lisent comme un cinquième et un sixième, et Lumière/Ténèbres comme un bien et un mal que la bible refuse. Piste : ce ne sont pas des éléments du monde mais des **polarités** du héros (révéler, cacher), qui ne viennent d'aucun Primordial. Ténèbres ne touche jamais le métal sombre ni la Disjonction.

## 4. Avis de game designer : ce que le modèle gagne et coûte

### 4.1 Ce qu'il gagne

- **Une règle complète et vérifiable** : « toute combinaison existe ». Le joueur qui cherche « et Terre + Air ? » trouve une réponse ; la v2 laissait deux paires vides.
- **Lumière et Ténèbres retrouvent leur sens** : en v2, Lumière = Terre + Feu était l'image la plus faible ; ici elles ne sont plus forcées.
- **L'unification paires = réactions** (§2.1) rend l'ensemble *plus* lisible que la v2 : six idées, chacune en deux intensités.

### 4.2 Ce qu'il coûte, et les parades

| Coût | Pourquoi | Parade |
|---|---|---|
| **Lisibilité** | 4 + 11 + 2 = **17 éléments** (18 avec le neutre), 13 couleurs de plus | les triplets ne sont **visibles qu'à leur ouverture** ; couleur d'un composé = mélange de ses parents, avec les pastilles des parents sur l'icône ; la fiche affiche le calcul (« Sève : 2 × moyenne (Terre, Feu, Eau) = 300 ») |
| **Contenu** | à 8 sorts partout : 13 × 3 classes × 8 = **312 sorts**, 624 noms (v2 : 96) | **8 sorts par paire, 4 par triplet, 3 pour Unisson, 4 (8 avec Plein grimoire) pour Lumière/Ténèbres** : 48 + 16 + 3 + 16 = 83 par classe, **249**. Transposés par `spells_gen.py` ; le coût réel est dans les noms et les icônes. Un élément par chapitre |
| **La caractéristique d'un triplet** | la somme de la v2 donnerait à Unisson **tous** les points de tout le monde : il frappe toujours à pleine force, ignore les faiblesses, devient dominant | **2 × la moyenne des parents** : identique à la somme pour une paire (la v2 ne bouge pas) ; un triplet vaut 2/3 de la somme, Unisson la moitié du total. Pour un build équilibré, tous les composés se valent ; pour un spécialiste, plus il y a de parents, moins ça frappe. Le triplet paie donc sa signature plus forte. Écartés : la somme (Unisson dominant), le minimum × n (punit trop le mono) |
| **Triplets contre paires** | un triplet a une meilleure signature et touche trois éléments | budget ×0,75 (paires ×0,8), 4 sorts au lieu de 8, et **clé** de Tresses : exclusif avec Conversion et Pureté (une clé par branche). Prérequis : avoir ouvert une de ses paires |
| **Conversion (T8)** | convertir vers un triplet retire 12 sorts (un seul élément de base restant), vers Unisson les retire tous | **Conversion vers les paires seulement** |
| **Le quadruplet** | Unisson est l'image de l'Accord ; dans la Saga I, c'est un spoiler et un « tout-en-un » | **après 100, dans la Saga II** (« Renaissance de l'Accord »), par une quête, hors de l'arbre de la Saga I |
| **Empreintes et Prisme (T7)** | un triplet poserait quelle empreinte ? | les triplets, Unisson, Lumière et Ténèbres **ne posent ni ne déclenchent** d'empreinte ; Prisme reste réservé aux paires |
| **Électrisé** | l'arc sur chaque coup reçu peut toucher une horde | une seule cible électrisée à la fois par lanceur, un saut par coup, et le plafond des hordes de T41 tient |

### 4.3 Où ça se range (si Charles dit oui)

- **Tresses, étage 1** : six *Ouvertures* de paires (au lieu de quatre).
- **Tresses, étage 2** : *Plein grimoire*, *Signature appuyée*, *Prisme*, *Ouverture : Lumière / Ténèbres* (exclusifs).
- **Tresses, clé** (une seule) : *Conversion* (paires), *Pureté*, **Tresse triple** (ouvre un triplet dont on a une paire), ou **Crépuscule** (§3.4).
- **Limite** : deux éléments ouverts au plus, quel que soit le rang (une paire + un triplet, une paire + Lumière…).
- **Unisson** : Saga II.
- **Ordre de livraison inchangé** : Foudre d'abord, puis Glace, puis les autres paires, Lumière ou Ténèbres, et les triplets en dernier (après 50, quand les points de clé sont là).

## 5. Questions à Charles

| # | Question | Valeur par défaut |
|---|---|---|
| 1 | La signature d'une paire est la petite version de sa réaction (mêmes six idées ; Attisé devient Électrisé) ? | **oui** |
| 2 | Les noms : Foudre, Glace, Boue, Magma, Vapeur, Sable ; Éther, Cendre, Sève, Sel ; Unisson. Magma et Cendre te semblent-ils trop proches (ton exemple « Magma et Volcan ») ? | **ces onze** ; *sinon* Soufre à la place de Cendre |
| 3 | Les triplets en clé de Tresses (exclusifs avec Conversion et Pureté), 4 sorts par classe, caractéristique = 2 × moyenne des parents ? | **oui** |
| 4 | Le quadruplet Unisson réservé à la Saga II, hors de l'arbre de la Saga I ? | **oui** ; *alternative :* un accomplissement à 100 |
| 5 | Lumière et Ténèbres : un talent d'étage 2, l'un **ou** l'autre, sur la plus haute des quatre caractéristiques d'élément ? | **oui** ; *alternative :* choix à 50 avec la spécialisation |
| 6 | Lumière et Ténèbres lues comme des polarités du héros (révéler, cacher), ni bien ni mal, à faire valider par le scénariste ? | **oui** |
| 7 | Crépuscule : les 16 sorts d'élément perdus (0 élément gardé), frappe sur la somme des quatre, sans bonus, clé de Tresses exclusive des composés ? | **oui** ; *repli :* Demi-crépuscule (2 éléments perdus, 4 + 4 sorts) si elle se montre dominante |
