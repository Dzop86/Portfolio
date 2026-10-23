# Osmose : les voies de spécialisation dans le canon

> **Proposition du scénariste, 23 octobre 2026, à relire par Charles.** Elle répond à la demande notée dans `suite-apres-d69.md` §11.8 (arbre de talents et spécialisations de classe). Elle fixe **le sens de chaque voie dans le monde**. Elle laisse au game designer (`docs/talents-specialisations.md`, écrit en parallèle) les mécaniques, les chiffres, le niveau exact du choix, le coût des talents et l'effet sur T41 et T96.
> **Convention :** **[P]** = proposition, absente de la bible et du jeu. Tout ce qui touche aux voies est **[P]** : la bible ne dit rien des classes. Le canon de la bible n'est pas modifié.
> **Sources lues :** `docs/bible.md` (et la bible complète transmise par Charles), `DECISIONS.md` (T13, T27, T34, T35, T96 à T99), `data/classes.json`, `tools/spells_gen.py`, `data/quests.json`, `data/towns/clairval-*.json` et `four-ways-*.json`, `docs/trame-saga-1.md`, `docs/chapitre-2-pistes.md` (avec les réponses de Charles, D79), `docs/textes-v5.md`, `textes-v6.md`, `textes-v7.md`, `docs/familles-foret.md`.
> **Non consulté :** le guide des classes de Dragonica cité par Charles (je n'ai pas d'accès au web ici). Je n'en reprends donc rien par construction, mais je ne peux pas garantir qu'aucun nom proposé ne s'y trouve : à vérifier (question 9).

## Sommaire
1. Les classes aujourd'hui, et ce que la bible en dit
2. Six principes de canon pour les voies
3. Les six voies
4. La quête de passage
5. Le vocabulaire : noms permis, noms interdits
6. Risques pour la bible, incohérences signalées
7. Questions pour Charles

---

## 1. Les classes aujourd'hui, et ce que la bible en dit

### 1.1 Dans les données

| Classe | `classes.json` | Ce que disent ses sorts (`spells_gen.py`) |
|---|---|---|
| **Sentinelle / Sentinel** | « Tire de loin, repousse et entrave ; mobile, elle garde ses distances. » 62 PV, +5 par niveau | des flèches dans les quatre éléments, des poussées (Tir de recul), une attirance (Grappin), des poisons d'Eau et de Feu, des pluies de zone, un **Faucon** invoqué, des entraves (Collet, Pieu), **Tir de semonce** (retire des PA), Camouflage, Concentration, Pansement |
| **Garde / Guard** | « Attire, bloque et frappe fort au corps à corps ; se protège elle-même. » 74 PV, +3 par niveau | des lames dans les quatre éléments, Provocation (attire), Rempart et Peau de pierre (sur soi), **Égide** et **Cri de guerre** (sur les alliés), Poigne de fer (entrave), Élan (+PM), Coup de titan, Second souffle |
| **Mage / Mage** | « Fragile, mais frappe en zone, soigne ses alliés et invoque une braise. » 86 PV, +3 par niveau | des sorts dans les quatre éléments, de petits puis de très grands (Météore, Cataclysme, Glacier, Tempête), Soin, Renouveau, **Renaissance**, Bouclier de givre, Sanctuaire, **Élémentaire** (une braise invoquée), Afflux (+PA), Pas léger |

Le fait qui compte le plus pour le canon : **chaque classe a des sorts dans les quatre éléments** (T34). Le héros les manie ensemble sans qu'ils se mélangent. Le jeu l'a déjà dit une fois, par Garance, à la fin de « Bien équipé » (`well-equipped`) : « tu passes d'un élément à l'autre sans qu'ils se marchent dessus. Chez la plupart des gens, l'un étouffe l'autre. » C'est l'indice discret de la Saga I. **Les voies ne doivent pas l'abîmer.**

### 1.2 Ce que la bible en dit

Rien. La bible parle de cultures (Enracinés, Maréens, Forgelames, Hauts-Vents, « pas forcément des races jouables », §6), de Fragments (§9) et de quatre représentants (§10), jamais des classes. Les noms des classes sont épicènes en français (T13), et aucun texte du jeu ne dit ce qu'est une Sentinelle, une Garde ou une Mage dans le monde. Je propose donc **[P]** :

- **La Sentinelle** est celle qui regarde de loin : les archers du chemin de ronde, les éclaireurs des routes, les chasseurs. Son métier commence quand elle voit avant d'être vue.
- **La Garde** est celle qui se tient devant : à la porte, au pont, devant un blessé. Le mot sert aussi aux gardes des portes de Clairval (Thibaut, Mahaut, Gautier) : c'est un poste, pas seulement une classe (voir §6.2).
- **La Mage** est celle qui porte les éléments au-delà de la main : elle soigne, elle protège, elle lâche de grandes forces. À Clairval, on la confond volontiers avec l'alchimiste ; elle-même ne sait pas toujours où passe la frontière.

Aucune des trois n'appartient à une culture ni à un Primordial. Garance les forme toutes les trois à Clairval : c'est sa cour d'armes qui fait d'une recrue une Sentinelle, une Garde ou une Mage.

---

## 2. Six principes de canon pour les voies

1. **Une voie est une tradition humaine, pas un don divin.** La puissance des Primordiaux entre dans la progression par les **Fragments** (bible §9), acquis pendant les arcs du chapitre 2. Si les voies étaient aussi « la voie de Dhoram » ou « la voie du Feu », elles doubleraient les Fragments et réduiraient le héros à un élément. Une voie est une **école** : une façon de se battre que des gens ont inventée, transmise, et parfois trahie.

2. **Une voie garde les quatre éléments.** Elle change la **manière** (la distance, le corps, ce qu'on fait pour les autres, ce qu'on retient ou ce qu'on lâche), jamais le mélange des éléments. Pas de « Mage de feu », pas de « Garde de pierre ». C'est la condition pour que la réplique de Garance reste vraie, et pour que la phrase du gardien de la Chambre sans nom (« Vous cherchez à réparer le monde avec les pouvoirs de ceux qui l'ont brisé. ») garde toute sa force : les éléments du héros sont l'héritage des Primordiaux, ses voies sont celui des hommes.

3. **Chaque paire porte une petite version de la question centrale**, sans camp du bien. Dans chaque classe, une voie **tient** (garder une ligne, se serrer, soigner) et l'autre **sort** (aller dans le sauvage, ouvrir une brèche, lâcher la force). C'est la question de la trame en miniature : « ce qui nous sépare nous protège-t-il ? ». Les deux voies ont raison, et chacune a son **angle mort**. Le jeu ne dit jamais laquelle est la bonne.

4. **Un maître du village, un maître de la route.** Pour chaque classe, la voie qui tient s'apprend à Clairval, chez quelqu'un qui connaît le héros depuis le niveau 1 ; la voie qui sort s'apprend à Quatre-Chemins, là où les routes se séparent. La géographie raconte le choix sans un mot de plus.

5. **On choisit sa voie à Quatre-Chemins, au seuil du chapitre 2.** Le hameau où « les routes se séparent » (`where-roads-part`) est le lieu naturel où la route du héros se sépare aussi. Le niveau exact est au game designer ; le sens demande seulement que le choix vienne **après** la fin du chapitre 1 (le Disjoint, la pierre qui se tait), quand le héros quitte le village pour de bon.

6. **Rien d'irréversible, rien de bloquant.** Une voie se reprend (« une route se reprend », dit Anatole, qui raye et réécrit dans son livre des routes). Aucune quête principale ne dépend de la voie choisie : un groupe de trois joueurs aux voies différentes suit la même histoire (bible §12). Le coût d'un changement est au game designer.

---

## 3. Les six voies

**La règle des noms [P] :** une voie porte le **nom d'une chose ou d'un lieu**, pas d'un métier ni d'une personne. Les noms sont donc épicènes par construction, comme ceux des classes, et ils évitent les titres de métier que tous les jeux du genre se partagent (assassin, acrobate, paladin…). Le titre s'affiche ainsi : « Sentinelle · Mire » / "Sentinel · Sightline".

Pour chaque voie : son nom, ce qu'elle signifie, ses sorts d'ancrage (pour que le game designer voie ce que la voie raconte, pas pour fixer la mécanique), son maître, une phrase d'ambiance, son angle mort, et ce qu'elle ne doit jamais devenir.

### 3.1 La Sentinelle

#### La Mire / Sightline (la voie qui tient)
- **Ce qu'elle signifie :** la tradition du chemin de ronde. Voir loin, compter ce qu'on voit, **prévenir avant de frapper**. Son geste emblème est le **Tir de semonce** : la Mire tire d'abord pour avertir, et celui qui avance quand même a choisi. Elle garde une ligne : une palissade, une porte, un col.
- **Sorts d'ancrage :** Tir de semonce, Concentration, Flèche perçante, Tir de recul, Tir lourd.
- **Maîtresse : Mahaut**, garde de la porte de l'Ouest (Clairval, cour d'armes ; existe). Vétérane, sèche, ancienne élève de Garance ; depuis « La lisière », elle laisse passer le héros sans lui demander où il va. Elle parle en ordres et en comptes : combien, où, à quelle distance.
- **Phrase d'ambiance :**
  - fr : « Tu tires la semonce d'abord. Celui qui avance quand même, il a choisi. Toi, tu as juste visé. »
  - en : "Warning shot first. Anyone who keeps coming has made their choice. You've only taken aim."
- **Angle mort :** la Mire garde une ligne sans demander qui l'a tracée. La porte de l'Est de Clairval est restée barrée « depuis le temps de la grand-mère d'Hortense », et personne ne sait pourquoi : une Mire l'aurait gardée sans poser la question. *(Écho pour les auteurs, jamais dit en jeu : la faille de Dhoram, préserver plus que comprendre.)*
- **Ne doit jamais devenir :** une tueuse qu'on ne voit pas venir (la Mire prévient, c'est son identité) ; un éloge des murs (« on garde parce qu'on a toujours gardé » est une question du jeu, pas sa réponse) ; une voie d'un seul élément.

#### L'Affût / Ambush (la voie qui sort)
- **Ce qu'elle signifie :** la tradition des chasseurs. Sortir de la palissade, vivre au bord du sauvage, **lire les traces** et attendre. Elle chasse **avec** une bête (le Faucon), pas à sa place : deux êtres qui restent chacun ce qu'ils sont et qui travaillent ensemble.
- **Sorts d'ancrage :** Faucon, Collet, Pieu, Flèche empoisonnée, Tir brûlant, Camouflage, Grappin.
- **Maîtresse : Bertille [P], la fauconnière** (nouvelle ; Quatre-Chemins, place du relais). Elle vend son gibier à Josette ; son faucon se pose sur le poteau aux quatre bras. Voix : basse, patiente, elle parle au héros comme à une bête craintive, par petites questions, sans jamais hausser le ton. Petite anomalie du niveau 1, jamais expliquée : depuis un mois, son faucon refuse de survoler les trois routes fermées. Elle ne propose pas de les franchir (règle de la v7 : pas de contenu fantôme).
- **Phrase d'ambiance :**
  - fr : « Chut. Le bois te parle, si tu te tais assez longtemps. Mon faucon l'a compris avant toi. »
  - en : "Hush. The woods will talk to you, if you keep quiet long enough. My hawk worked that out before you did."
- **Angle mort :** l'Affût n'appartient à personne. Il connaît chaque piste et ne s'engage dans aucune histoire qui n'est pas la sienne. *(Écho pour les auteurs : la faille d'Élyon, fuir ce qui retient.)*
- **Ne doit jamais devenir :** une voie de métamorphose ou de fusion avec la bête (la fusion forcée, c'est le Disjoint et les zombies : le faucon reste un faucon, d'un seul élément) ; une dresseuse des bêtes changées par l'Éveil (elles sont des symptômes, pas des montures) ; une empoisonneuse au ton sombre et sans morale (ses poisons sont ceux de la chasse).

### 3.2 La Garde

#### La Palissade / Palisade (la voie qui tient)
- **Ce qu'elle signifie :** la tradition des portes. Se tenir **entre** le danger et les autres, et tenir **avec** eux : une palissade n'est pas un mur, ce sont des pieux qui se tiennent entre eux. La force de cette voie ne vient pas de l'armure d'une seule personne, mais de ce qu'elle fait pour ceux qui sont à côté.
- **Sorts d'ancrage :** Égide, Provocation, Cri de guerre, Poigne de fer, Rempart, Peau de pierre.
- **Maître : Gautier**, garde de la porte du Sud (Clairval ; existe). Placide, blagueur ; il garde une écurie sans chevaux. Sa placidité devient une doctrine : il ne bouge pas, ce sont les autres qui bougent autour. **[P]** : il a tenu un pont, autrefois, et il n'en dit pas plus.
- **Phrase d'ambiance :**
  - fr : « Un pieu tout seul, c'est un bâton. Dix pieux qui se tiennent, c'est une palissade. Moi, je fais le pieu du milieu : je bouge pas. »
  - en : "One stake on its own is a stick. Ten stakes holding each other up, that's a palisade. Me, I'm the middle stake: I don't move."
- **Angle mort :** la Palissade tient même quand il faudrait céder. Elle confond parfois la promesse et la raison de la promesse, comme Mathurin qui obéit au serment de son grand-père sans le comprendre. *(Écho : la faille de Dhoram.)*
- **Ne doit jamais devenir :** une martyre qui prêche le sacrifice ; une fanatique du serment ; « la voie de la Terre » ; une forteresse solitaire (« un pieu tout seul, c'est un bâton » est sa règle). Sans le dire, elle montre ce que la bible appelle une coexistence : des pièces distinctes qui se tiennent. Elle ne doit **jamais** nommer cette idée.

#### La Brèche / Breach (la voie qui sort)
- **Ce qu'elle signifie :** la tradition de ceux qui passent les premiers. **Ouvrir** une ligne pour que les autres passent : la porte enfoncée, le passage fait dans la mêlée. Elle frappe fort et avance, mais à froid : la Brèche est une décision, pas une colère.
- **Sorts d'ancrage :** Élan, Fente ardente, Fauchage, Coup de titan, Châtiment, Ressac, Ouragan.
- **Maître : Rémi**, le cantonnier du chemin creux (Quatre-Chemins ; existe). Gai, têtu, sans illusion ; il déblaie l'éboulis douze pierres par jour. **[P]** : vingt ans dans les premiers rangs, avant de prendre la brouette. Il plaisante sur tout, et d'abord sur son malheur. Il n'ouvre pas l'éboulis pendant la quête : le texte de la v7 dit qu'on ne passera pas, et ça reste vrai.
- **Phrase d'ambiance :**
  - fr : « Vingt ans à enfoncer des lignes, et maintenant je déblaie des routes. C'est le même métier, sauf que les cailloux, eux, se plaignent pas. »
  - en : "Twenty years breaking lines, and now I clear roads. Same trade, except the stones don't complain."
- **Angle mort :** la Brèche ouvre et passe, et elle ne reste pas pour tenir ce qu'elle a ouvert ; elle casse aussi ce qu'elle aurait pu réparer. *(Écho : les failles de Kaor et d'Élyon.)*
- **Ne doit jamais devenir :** une voie de rage ou de furie. C'est le point de canon le plus important de ce document : au chapitre 2, **la colère est un symptôme** (les armes qui se mettent en colère avant leur porteur, « la colère faite armure », le métal sombre qui tremble dans les mains en colère). Une Garde qui se bat mieux en colère confondrait le héros avec ce qu'il combat. Pas de mot « rage », pas de « furie » dans les textes ni dans les noms des talents. Pas non plus de destruction pour le plaisir.

### 3.3 La Mage

#### Le Baume / Balm (la voie qui tient)
- **Ce qu'elle signifie :** la tradition des remèdes. **Garder entiers** ceux qui se battent : soigner, protéger, doser. Le Baume pense la magie comme une pharmacie : tout est dans la dose, et un excès soigne aussi mal qu'un manque.
- **Sorts d'ancrage :** Soin, Renouveau, Renaissance (voir §6.3), Bouclier de givre, Sanctuaire.
- **Maîtresse : Sidonie**, l'alchimiste (Clairval, rue des artisans ; existe). Elle parle vite et s'enthousiasme ; ses sourcils sont toujours un peu roussis. C'est elle qui a vu que les spores du Bois moisi restent côte à côte dans ses fioles sans jamais se mêler.
- **Phrase d'ambiance :**
  - fr : « Un remède, c'est un poison qui a appris les bonnes manières ! Tout est dans la dose. Et dans les sourcils, si tu rates. »
  - en : "A remedy is just a poison with good manners! It's all in the dose. And in your eyebrows, if you get it wrong."
- **Angle mort :** le Baume ne sait pas laisser partir. Il soigne encore quand il faudrait accepter une perte. *(Écho : la faille de Névara, refuser l'oubli et la perte.)*
- **Ne doit jamais devenir :** une voie qui relève les morts (les morts relevés, ce sont les zombies de la crypte : une contrainte, pas un soin) ; une voie qui guérit en faisant oublier (l'oubli est le cœur du mystère : on ne soigne pas une douleur en effaçant un souvenir) ; une sainte. Sa magie garde les quatre éléments : pas de « guérisseuse de l'Eau ».

#### L'Épicentre / Epicentre (la voie qui sort)
- **Ce qu'elle signifie :** la tradition de la grande force. **Retenir, puis lâcher** : accumuler longtemps, choisir l'instant, et que tout parte d'un point. Le danger de cette voie est connu de ceux qui la suivent, et c'est ce qu'elle enseigne d'abord.
- **Sorts d'ancrage :** Afflux, Météore, Cataclysme, Glacier, Tempête, Éboulement, Élémentaire.
- **Maîtresse : Ghislaine**, la charbonnière de la lande fumante (Quatre-Chemins ; existe). Sèche, noire de suie, des phrases courtes ; elle juge. Elle enseigne le principe de la meule de charbon, qu'elle applique à tout : ça couve, puis ça part. **Ce n'est pas une voie du Feu** : Ghislaine le dit elle-même (« Le feu, la terre, l'eau, le vent : tout pareil. Ça couve, puis ça part. »). Sa voix ne se confond pas avec celle de Mahaut : Mahaut ordonne et compte, Ghislaine juge et fait des proverbes de charbonnière.
- **Phrase d'ambiance :**
  - fr : « Une meule, ça couve des jours. Puis ça part d'un coup. Celui qui ouvre trop tôt n'a que de la fumée. »
  - en : "A charcoal stack smoulders for days. Then it goes all at once. Open it too early and all you get is smoke."
- **Angle mort :** l'Épicentre change le monde en le cassant, et il ne mesure pas toujours ce qui reste autour du point d'impact. *(Écho : la faille de Kaor.)*
- **Ne doit jamais devenir :** une fusion des quatre éléments en un seul coup (pas de « rayon des quatre éléments », pas de sort qui les mêle : la fusion, c'est le Disjoint, et le héros est justement celui chez qui ils ne se mêlent pas) ; un fantasme de puissance sans limite ; une voie du Feu ; une voie qui invoque des créatures à plusieurs affinités (l'Élémentaire reste une braise, d'un seul élément).

### 3.4 Pourquoi deux voies, et pas trois

Deux voies par classe donnent six maîtres, trois au village et trois sur la route : c'est lisible et ça tient dans les lieux qui existent. Une troisième voie se justifierait seulement par une manière de se battre que les deux premières ne couvrent pas, et les candidates naturelles posent un problème de canon :
- une **Mage invocatrice** pousserait à fabriquer des créatures, c'est-à-dire le thème de la fusion forcée (le Disjoint, le colossal assemblé) ;
- une **Sentinelle empoisonneuse** séparée de l'Affût tirerait vers un ton d'assassin que le jeu n'a pas.

Je recommande donc de rester à deux. Toute voie liée de près ou de loin au cinquième signe est **exclue de la Saga I** : si elle doit exister, c'est une décision de Charles pour la Saga II, après le niveau 100.

### 3.5 Ce que les maîtres deviennent [P]

Chaque maître a une réplique de plus quand le héros revient après « Quatre fragments » (`four-fragments`, niveau 60, chapitre 2) : il reconnaît une fois **l'angle mort de sa propre voie**, sans renier sa voie. Exemple pour Gautier : « Le pont que j'ai tenu, je me demande encore s'il fallait le tenir. Je l'ai tenu quand même. » Rien n'est révélé de l'intrigue : c'est la question centrale vue de la cour d'armes.

---

## 4. La quête de passage

### 4.1 L'idée
Garance a formé la recrue. Elle refuse de choisir à sa place : elle l'envoie voir les deux maîtres de sa classe. Le héros les écoute, passe l'épreuve d'un seul, et cette épreuve **est** le choix. Anatole note la route prise dans son livre des routes. Six épreuves, six types d'activité différents (bible §11).

### 4.2 Les fiches en bref (bible §16, à compléter quand la mécanique sera fixée)

**`a-road-of-ones-own` [P] · « Une route à soi » / "A Road of One's Own"**
- **Chapitre :** charnière entre le chapitre 1 et le chapitre 2. **Type :** quête de classe (ni principale ni secondaire). **Niveau :** celui du game designer (≈ 30).
- **Donneuse :** Garance (cour d'armes). **Prérequis :** `where-roads-part` rendue.
- **Objectifs :** parler aux deux maîtres **de sa classe** (deux `Talk` avec réplique).
- **Texte de Garance (proposition) :**
  - fr : « Je t'ai appris à tenir debout et à frapper juste. Où tu vas maintenant, ce n'est plus mon affaire. Va voir les deux qui en savent plus que moi, et choisis. »
  - en : "I taught you to stand up straight and hit true. Where you go from here is none of my business. Go and see the two who know more than I do, and choose."
- **Rendue à :** Garance. **Débloque :** les deux épreuves de sa classe.

**Les six épreuves [P]** (une seule se prend ; la prendre retire l'offre de l'autre tant qu'elle est en cours, l'abandonner la rend) :

| id | Voie | Maître | Activité | Ce qu'on fait (objectifs du moteur) |
|---|---|---|---|---|
| `path-sightline` | Mire | Mahaut | combat de position | une nuit sur le chemin de ronde de la porte de l'Ouest : tenir la palissade contre les sangliers qui la grattent encore (le fil de « La lisière ») (`Win`, scénario de quête). Mahaut juge qu'on a tiré la semonce avant de tirer pour de bon (une réplique, pas une règle du moteur) |
| `path-ambush` | Affût | Bertille | exploration, pistage | le faucon de Bertille ne revient pas ; suivre trois traces sur les cartes de Quatre-Chemins (trois `Interact`) jusqu'à lui, perché au bord d'une route fermée, et le ramener (`Talk` Bertille) |
| `path-palisade` | Palissade | Gautier | protection | tenir la porte du Sud pendant que Léonie rentre ses poules, attaquées par des loups descendus du Bois (`Win`, scénario de quête ; idéalement une condition « protéger », voir §4.4) |
| `path-breach` | Brèche | Rémi | combat d'ouverture | un convoi de Lucien coincé derrière des taupes qui ont miné le chemin creux : passer à travers et rejoindre la charrette (`Win`, scénario de quête). L'éboulis, lui, ne bouge pas |
| `path-balm` | Baume | Sidonie | artisanat, collecte | préparer un baume avec Sidonie : rapporter des spores du Bois moisi et des fleurs des Champs (`Bring`), qu'elle dose devant le héros ; la raison diégétique est le dosage qu'elle enseigne |
| `path-epicentre` | Épicentre | Ghislaine | énigme d'information | Ghislaine monte une meule et demande trois fois au héros s'il faut l'ouvrir ; il faut répondre selon ce que montrent la fumée et sa couleur (une réponse de dialogue juste, une mauvaise se rejoue sans perte, comme `the-pieced-map`) |

- **Rendue à :** chaque maître, puis une réplique d'Anatole (`Talk`, facultative) qui écrit la voie dans le livre des routes :
  - fr : « Nom, route, et façon de la prendre. J'ai ajouté une colonne l'an dernier. Personne ne la remplissait. »
  - en : "Name, road, and the way you mean to take it. I added a column last year. Nobody ever filled it in."
- **Effet :** la voie est enregistrée **sur le personnage** (drapeau persistant). La récompense est la voie elle-même ; le reste (talents, titre) est au game designer.
- **Changer de voie :** reparler à l'autre maître, qui propose son épreuve à nouveau (coût au game designer). Anatole raye et réécrit. Aucune quête faite n'est défaite.

### 4.3 Coopération, abandon, reconnexion (bible §12 et §17)
- Chaque personnage valide sa propre épreuve ; un groupe peut faire ensemble les épreuves de combat, et chacun est crédité pour la sienne si elle est en cours.
- Deux joueurs de la même classe peuvent choisir deux voies différentes dans le même groupe : rien ne dépend de ce qu'a fait l'autre.
- Abandon : on revient à « aucune épreuve en cours », et les deux offres réapparaissent. Reconnexion : la voie est un drapeau du serveur, pas un état du client.
- Aucune étape obligatoire de l'histoire ne demande une voie : la quête de passage n'est prérequise par rien dans la chaîne principale.

### 4.4 Ce qui demande du code ou une décision (pour le dev et le game designer)
1. **Une offre réservée à une classe** : chaque maître ne propose son épreuve qu'à la classe concernée (condition de classe sur une quête ou un objectif).
2. **Des offres exclusives** : prendre une épreuve masque l'autre de la même classe tant qu'elle est en cours.
3. **Une réponse de dialogue qui valide un objectif** (`path-epicentre`), déjà demandée par `chapitre-2-pistes.md` §9.4.
4. **Une voie gardée par personnage**, lisible par le serveur (et sa remise à zéro lors d'un changement).
5. **Quatre scénarios de quête** (`Win`) ; celui de `path-palisade` serait meilleur avec une condition « protéger un allié », déjà demandée pour `the-casting` (§9.5 du même document). Sans elle, c'est un combat ordinaire où Léonie reste hors de la carte.
6. **Un nouveau personnage**, Bertille, à la place du relais, et son faucon posé sur le poteau (un décor).

---

## 5. Le vocabulaire : noms permis, noms interdits

**La fenêtre.** « Arbre de talents » est le mot de tous les jeux ; dans Osmose, l'arbre est aussi l'image de Dhoram (la graine dans la montagne, l'Arbre-Mère d'Ormeval). Pour ne pas donner à une fenêtre de progression la couleur d'un Primordial, je propose **[P]** : la fenêtre s'appelle **« Ma route » / "My Road"**, et un talent est un **« jalon » / "waymark"**. Cela prolonge Quatre-Chemins, le livre des routes d'Anatole et les bornes retirées de son jardin. La forme de la fenêtre (un arbre, des embranchements) est au game designer : seul le mot change.

**Noms à ne jamais donner à une voie, à un talent ou à sa description :**
- tout ce qui évoque l'Accord ou le cinquième : Accord, Concorde, Harmonie, Lien, Liant, Relation, Cinquième, Quintessence, Équilibre, Union ;
- tout ce qui évoque la Disjonction : Disjoint, Disjonction, Fracture, Sceau, et le métal sombre (aucun talent ne doit « tirer sa force » de ce métal, fait pour contraindre) ;
- les noms des Primordiaux, de leurs devises, des cultures (Enracinés, Maréens, Forgelames, Hauts-Vents) et des éléments seuls (« voie de l'Eau ») ;
- les mots de la colère pour la Garde : rage, furie, frénésie ;
- les titres de classe d'autres jeux, même communs : assassin, acrobate, paladin, druide, rôdeur, berserker, templier, nécromancien, invocateur, sage, thaumaturge, tireur d'élite ; et bien sûr tous les noms de classes d'Ankama (D54).

---

## 6. Risques pour la bible, incohérences signalées

Rien n'est corrigé ici : je signale.

### 6.1 Les risques des voies elles-mêmes
1. **Doubler les Fragments.** Une voie liée à un Primordial ferait double emploi avec les Fragments et figerait le héros dans un élément. Parade : principes 1 et 2 du §2.
2. **Abîmer l'indice des quatre éléments.** Une voie « d'un élément » rendrait fausse la réplique de Garance. Parade : chaque voie garde des sorts dans les quatre éléments ; à vérifier par le game designer quand il fixera les sorts de chaque voie.
3. **Célébrer la colère.** Une Brèche de rage contredirait l'arc de Kaor. Parade : §3.2 et la liste du §5.
4. **Faire de la voie qui tient la voie des sages et de la voie qui sort celle des imprudents** (ou l'inverse). Parade : chaque voie a un angle mort, et chaque maître reconnaît le sien une fois (§3.5).
5. **Trop d'indices sur le héros.** La tentation est grande de faire dire à chaque maître « toi, c'est différent ». Garance l'a dit une fois, et le métal qui ne tremble pas est déjà prévu dans l'arc de Kaor (oui de Charles, D79). Ma valeur par défaut : **aucun maître ne le relève** (question 6).

### 6.2 Incohérences et points d'attention trouvés en lisant
1. **Le mot « garde ».** La classe Garde et le poste de garde des portes (Thibaut, Mahaut, Gautier) portent le même mot. Mahaut, garde de porte, enseigne une voie de **Sentinelle**. Ce n'est pas une erreur si l'on admet que le poste est tenu par des gens de toutes les classes. Je le mets dans ses répliques : « Garde de la porte, oui. Garde tout court, non : moi, je tire. »
2. **La Mage est « fragile » mais a le plus de PV** : 86 PV, contre 74 pour la Garde et 62 pour la Sentinelle (`classes.json`). La description et les chiffres se contredisent. C'est au game designer ou au dev de trancher ; la voie du Baume n'en dépend pas.
3. **Le sort « Renaissance / Rebirth » de la Mage** (un soin de zone) porte le mot du titre de la Saga II, « la Renaissance de l'Accord ». Un joueur attentif peut y lire une promesse, ou un lien entre soigner et ressusciter que la bible ne fait pas. Je recommande de renommer le sort un jour (par exemple « Reverdie » / "Regrowth" **[P]**) ; c'est au game designer.
4. **« Forgelames », le nom de travail de la culture du Feu dans la bible** (§6), est très proche du nom d'une classe récente d'un jeu d'Ankama (« Forgelance », si ma mémoire est bonne : à vérifier par Charles). La bible dit que ce sont des noms de travail ; D54 interdit tout nom d'Ankama. Je ne change rien, je signale.
5. **Les descriptions de classe disent « elle »** pour les trois classes (accord avec « la Sentinelle », « la Garde » ; « la Mage » est moins naturel en français). Les textes des voies suivent la même convention ; à confirmer.

---

## 7. Questions pour Charles

| # | Question | Ma valeur par défaut |
|---|---|---|
| 1 | Le sens général : une voie est une **tradition humaine** qui garde les quatre éléments, et la puissance des Primordiaux reste aux Fragments ? | **Oui.** Aucune voie ne porte un élément ni un Primordial |
| 2 | Deux voies par classe, une qui **tient** et une qui **sort**, apprises l'une à Clairval et l'autre à Quatre-Chemins ? | **Oui, deux**, pas de troisième voie dans la Saga I |
| 3 | Les noms : **Mire / Sightline** et **Affût / Ambush** (Sentinelle), **Palissade / Palisade** et **Brèche / Breach** (Garde), **Baume / Balm** et **Épicentre / Epicentre** (Mage), avec la règle « une chose ou un lieu, jamais un métier » ? | **Gardés** tels quels |
| 4 | Les maîtres : Mahaut, Bertille (nouvelle, Quatre-Chemins), Gautier, Rémi, Sidonie, Ghislaine ; Gautier ancien gardien de pont et Rémi ancien soldat ? | **Oui** ; un seul personnage nouveau (Bertille) |
| 5 | Le moment : la quête de passage s'ouvre après « Là où les routes se séparent » (`where-roads-part`), au seuil du chapitre 2 ? | **Oui**, le niveau exact est au game designer |
| 6 | Un maître remarque-t-il que les quatre éléments ne se mêlent pas chez le héros ? | **Non** : Garance l'a déjà dit, et le métal de l'arc de Kaor suffit |
| 7 | Une voie se reprend-elle ? | **Oui**, chez l'autre maître, avec un coût fixé par le game designer ; rien n'est défait |
| 8 | La fenêtre s'appelle « Ma route » et un talent un « jalon », plutôt qu'« arbre de talents » ? | **Oui** ; la forme de la fenêtre reste au game designer |
| 9 | Vérifier qu'aucun nom de voie ni « Bertille » n'existe dans Dragonica ou chez Ankama (je n'ai pas pu ouvrir le guide de Dragonica) | **À vérifier par vous** ; je n'en connais pas |
| 10 | « Forgelames » et la classe d'Ankama au nom proche (§6.2, point 4) | **À vérifier par vous** ; si le nom est trop proche, renommer la culture, qui n'est qu'un nom de travail |
| 11 | Renommer le sort « Renaissance » de la Mage (§6.2, point 3) | **Oui, un jour**, par le game designer ; pas urgent |
