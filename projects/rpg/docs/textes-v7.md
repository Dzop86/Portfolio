# Osmose : les textes de la v7 (Quatre-Chemins, le premier lieu du chapitre 2)

> Proposition du scénariste pour le sprint 129, **à relire par Charles** avant d'être reportée dans les données. Elle répond à la ligne S129 de `suite-apres-d69.md` (§11.6) et aux réponses de Charles du 22 octobre (`chapitre-2-pistes.md` §10.3, D79) : Quatre-Chemins est **gardé**, comme l'étape où les routes parties de Clairval se séparent ; la Terre d'abord ; Kaor à l'est, Névara à l'ouest au bout de la rivière du Lac, Dhoram au sud, Élyon au nord.
> **Convention :** **[P]** = proposition, absente de la bible et du jeu. Le canon de la bible n'est pas modifié. Tous les identifiants nouveaux (zone, cartes, habitants, objets posés, quête) sont **[P]** ; ceux qui existent déjà sont cités tels quels.
> **Le héros n'a pas de genre dans les textes**, comme en v5 et v6 : aucun accord ne porte sur lui. Tous les habitants le tutoient.
> **Rien n'est écrit dans `data/`** : le dev transcrit.

## Sommaire
1. Le lieu : la zone, les quatre cartes, les routes
2. Les habitants : fiches
3. Les objets posés (la pierre de passage, le poteau, les trois obstacles)
4. L'étape de la chaîne principale : `where-roads-part`
5. Les textes prêts à copier (dialogues, objets, quête, porte de l'Est)
6. Ce qui demande du code ou une décision (pour le dev et le game designer)
7. Points de canon et questions pour Charles
8. Récapitulatif pour la transcription

**Lire un bloc de quête** (comme en v5 et v6) :
- **Offre** : ce que dit le donneur quand il propose la quête.
- **Objectif** : l'intitulé de l'objectif dans le journal.
- **Remerciement** : ce que dit celui qui reprend la quête.

---

## 1. Le lieu : la zone, les quatre cartes, les routes

**Quatre-Chemins / Four Ways [P]** (`fourways` **[P]**, niveau 30) : un hameau-relais de 2 × 2 cartes à l'est de Clairval. Un relais de voyageurs, un étal, la maison d'un arpenteur, quelques métairies. Il vit de ceux qui passent, et depuis un mois, presque personne ne passe.

**Comment on y arrive.** Par la **porte de l'Est** de Clairval, au bord est de `clairval-3-1` (la rue des artisans), fermée tant que `four-trails` n'est pas rendue. On arrive dans la carte sud-ouest par son bord ouest.

**Pourquoi les routes partent d'ici et pas de Clairval [P].** Clairval regarde le Bois, et ses trois portes aussi. Les routes du pays, elles, ont toujours évité le Bois : elles partent toutes de Quatre-Chemins, même celle de la côte, qui fait le tour par le gué. La porte de l'Est était barrée « depuis le temps de la grand-mère d'Hortense », et personne ne sait plus pourquoi (une petite anomalie de mémoire, niveau 1 d'information ; jamais expliquée ici). C'est ce qui fait tenir ensemble les réponses de Charles : les habitants de Clairval donnent leur piste au village, et les pistes bifurquent ici.

**Les quatre cartes** (nom affiché fr / en, thème du décor) :

| Carte **[P]** | Nom fr / en | Décor (une ligne) | Bord qui sert de route | Habitants, objets |
|---|---|---|---|---|
| `fourways-0-1` (sud-ouest, **arrivée**) | Quatre-Chemins, la place du relais / Four Ways, Inn Square | une place de terre battue autour du poteau aux quatre bras, l'auberge et sa cour d'écurie, l'étal sous un auvent ; au sud, la route descend vers une rivière large et brune | ouest : vers Clairval (passage) ; **sud : la route de la côte, barrée par le gué en crue** | Lucien, Josette, Aristide ; pierre de passage, poteau, gué |
| `fourways-0-0` (nord-ouest) | Quatre-Chemins, le chemin creux / Four Ways, the Sunken Lane | un chemin encaissé qui monte vers le nord entre deux talus de roche et d'ajoncs, des blocs tombés partout, une brouette | **nord : la route du nord, barrée par l'éboulis** | Rémi ; éboulis |
| `fourways-1-0` (nord-est) | Quatre-Chemins, la lande fumante / Four Ways, the Smoking Heath | une lande de bruyère et de genêts, des meules de charbonnier froides, une hutte noire de suie ; une fumée basse qui vient de l'est | **est : la route de l'est, barrée par le brasier** | Ghislaine ; brasier |
| `fourways-1-1` (sud-est) | Quatre-Chemins, la maison des routes / Four Ways, the Road House | la maison haute de l'arpenteur, des bornes de route retirées alignées dans le jardin, des cartes qui sèchent sur un fil ; au sud, une route entre deux haies | **sud : la route du sud (Terre), ouverte mais sans passage dans ce sprint** | Anatole, Romane |

La place est la carte d'arrivée : on y voit tout de suite le poteau, donc l'idée du lieu (les routes se séparent ici). Les quatre cartes communiquent entre elles comme celles de Clairval.

**Les routes :**
- **Le sud (Terre, Dhoram)**, carte sud-est : **ouverte**, mais la forêt n'arrive qu'au S131. Dans ce sprint, la route s'arrête au bord de la carte, sans passage. Romane et Anatole disent que c'est la seule praticable, sans rien promettre au-delà des « collines ». Aucun texte ne nomme la Forêt des Aïeux ni Ormeval.
- **Le nord (Air, Élyon)**, carte nord-ouest : un **éboulis** tombé en une nuit, sans orage ni gel ; le vent siffle entre les blocs.
- **L'est (Feu, Kaor)**, carte nord-est : un **brasier** qui ne s'étend pas et ne s'éteint pas, en travers de la route.
- **Le sud-ouest (Eau, Névara)**, carte sud-ouest : un **gué en crue** sur la rivière qui descend du Lac vers la côte, sans une goutte de pluie.

Les trois obstacles sont tombés **le même mois** (Anatole le note, et barre le mot « hasard »). C'est une anomalie du niveau 1 (bible §2 et §15 : des phénomènes sans cause) ; personne ne l'explique, et chaque obstacle porte discrètement son élément (le vent, l'eau, le feu), sans qu'on le dise.

**Pas de contenu fantôme.** Les textes des obstacles disent qu'on ne passera pas. Aucun habitant ne propose de les franchir, aucun ne dit « plus tard » ni « quand tu seras plus fort ». Ils seront le point de départ des autres arcs quand leurs régions existeront (voir §7.2, question 9).

---

## 2. Les habitants : fiches

Sept habitants. Aucun prénom ne sert déjà dans `data/` ni dans les documents du projet (vérifié : Anatole, Romane, Lucien, Josette, Aristide, Rémi, Ghislaine) ; ni Margaux ni Camille. Les apparences sont des propositions : si une silhouette ne va pas (âge, carrure), le dev change sans me demander.

| id **[P]** | Nom affiché fr / en | Apparence | Carte | Fiche (une ligne) |
|---|---|---|---|---|
| `anatole` | Anatole, l'arpenteur / Anatole, the surveyor | `male-c` | sud-est, devant sa maison | **l'érudit du lieu** : un vieil arpenteur qui compte tout en pas et tient « le livre des routes », où il note chaque voyageur et chaque route ; sec, précis, ironique ; correspondant d'Ysaline depuis vingt ans. Il reprend la quête. |
| `romane` | Romane, l'apprentie / Romane, the apprentice | `female-a` | sud-est, près du départ de la route du sud | l'apprentie d'Anatole, qui repeint chaque printemps les signes du poteau ; vive, pratique, moqueuse ; elle lit le courrier d'Ysaline par-dessus l'épaule d'Anatole. |
| `lucien` | Lucien, le marchand / Lucien, the merchant | `male-e` | sud-ouest, sous l'auvent de l'étal | **le marchand** : le frère cadet de Rose, mêmes prix « au même Éclat près » ; bonhomme, commerçant jusqu'au bout des phrases. Son dialogue ouvre l'étal. |
| `josette` | Josette, l'aubergiste / Josette, the innkeeper | `female-e` | sud-ouest, à la porte du relais | l'aubergiste du relais : maternelle, débordée même quand l'auberge est vide ; pose trois questions sans attendre les réponses. |
| `aristide` | Aristide, le charretier / Aristide, the carter | `male-f` | sud-ouest, avec sa charrette, près du gué | un charretier bloqué au gué depuis trois semaines avec du miel des Champs pour la côte ; optimiste, parle à sa mule, Biscotte. |
| `remi` | Rémi, le cantonnier / Rémi, the road-mender | `male-d` | nord-ouest, devant l'éboulis | le cantonnier de la route du nord, qui déblaie l'éboulis à la brouette, douze pierres par jour ; gai, têtu, sans illusion. |
| `ghislaine` | Ghislaine, la charbonnière / Ghislaine, the charcoal burner | `female-c` | nord-est, près de sa hutte | la charbonnière de la lande : sèche, noire de suie, des phrases courtes ; elle connaît le feu, et celui-là n'en est pas un. |

**Les voix ne se confondent pas.** Anatole compte et corrige ; Romane se moque et s'exclame ; Lucien vend ; Josette nourrit ; Aristide parle à sa mule ; Rémi plaisante sur son malheur ; Ghislaine juge. Personne ici ne ressemble aux habitants de Clairval : Odalric reste le seul cérémonieux, Joachim le seul vieux pêcheur bougon.

**Qui dit pourquoi chaque route est fermée.** Anatole parle des trois (et du sud). Chaque obstacle a aussi son habitant : Rémi (l'éboulis), Ghislaine (le brasier), Aristide (le gué).

**Ce que chacun sait de la trame.** Rien, sauf ce qui est déjà au niveau 1 : trois routes fermées sans cause le même mois (Anatole), une pierre qui parle à Clairval dont la rumeur court les routes (Josette), un poteau dont les signes n'ont jamais changé de bras alors que la pierre d'Ysaline ne regarde plus du même côté (Romane, voir §7.1). Ghislaine relie sans le savoir l'atelier fermé des forgerons de l'est, dont Garance a parlé.

---

## 3. Les objets posés

| id **[P]** | Carte | Nom fr / en | Aspect | Rôle |
|---|---|---|---|---|
| `waystone-fourways` | sud-ouest, à deux cases au plus de l'arrivée (règle de T95) | Pierre de passage des Quatre-Chemins / Four Ways waystone | `waystone` (existe) | la pierre de passage du lieu |
| `four-armed-signpost` | sud-ouest, au milieu de la place | Le poteau aux quatre bras / The four-armed signpost | **nouveau** (`signpost`) | objectif de la quête ; l'image du lieu |
| `north-rockfall` | nord-ouest, au bord nord, sur la route | L'éboulis / The rockfall | **nouveau** (`rockfall`) | barre la route du nord ; objectif de la quête |
| `east-blaze` | nord-est, au bord est, sur la route | Le brasier / The blaze | **nouveau** (`blaze`) | barre la route de l'est ; objectif de la quête |
| `flooded-ford` | sud-ouest, au bord sud, sur la route | Le gué en crue / The flooded ford | **nouveau** (`ford`) | barre la route de la côte ; objectif de la quête |

Les textes sont au §5.2. Les noms en anglais suivent la casse des objets existants (« The old stone »).

---

## 4. L'étape de la chaîne principale : `where-roads-part`

### Fiche (bible §16)
- **Identifiant :** `where-roads-part` **[P]**
- **Titre :** Là où les routes se séparent / Where the Roads Part
- **Résumé :** la porte de l'Est s'ouvre. Ysaline envoie le héros à Quatre-Chemins, chez Anatole, son vieux correspondant, qui sait quelles routes on peut prendre. Le héros découvre le poteau aux quatre bras, voit de ses yeux les trois routes barrées, et Anatole lui confirme que seule celle du sud est praticable pour l'instant.
- **Chapitre :** 2, les Quatre Héritages (première étape) ; `kind` **Main**.
- **Niveau :** 30 (minimum et conseillé).
- **Donneur :** Ysaline (`clairval/ysaline`), qui reste avec la pierre comme elle l'a dit dans `four-trails`.
- **Reprise :** Anatole (`fourways/anatole`), par `handIn` (comme `welcome`, que Garance reprend).
- **Prérequis :** `"after": ["four-trails"]`. C'est aussi la condition qui ouvre la porte de l'Est : la quête n'est jamais proposée porte fermée.
- **Objectifs** (moteur actuel, sans ordre) :
  1. `Interact` `four-armed-signpost` : examiner le poteau aux quatre bras ;
  2. `Interact` `north-rockfall` : examiner l'éboulis ;
  3. `Interact` `east-blaze` : examiner le brasier ;
  4. `Interact` `flooded-ford` : examiner le gué en crue ;
  5. `Talk` `fourways/anatole`, avec une réplique (`line`).
- **Validation :** les cinq objectifs, puis rendre à Anatole.
- **Type d'activité :** dialogue et exploration (un tour du hameau, quatre cartes). Aucun combat : le chapitre 1 s'est fini sur un boss, le chapitre 2 s'ouvre sur un lieu.
- **Dialogues associés :** l'offre d'Ysaline, la réplique d'Anatole (pendant l'objectif), son remerciement ; les dialogues des sept habitants (§5.1), facultatifs.
- **Combats :** aucun.
- **Récompenses :** **1 200 XP** et **200 Éclats** **[P]**, sous `four-trails` (1 800 XP, 300 Éclats) : c'est une étape de découverte, sans combat ni collecte. Au game designer de trancher.
- **Déblocage de la suite :** aucun dans ce sprint. Les étapes de la Forêt (S136) devront porter `"after": ["where-roads-part"]` (voir §6, point 9).
- **Drapeaux persistants :** la quête faite (`where-roads-part`), enregistrée par personnage comme les autres. La porte de l'Est dépend de `four-trails`, pas de celle-ci.
- **Coopération :** les `Interact` et le `Talk` se valident pour chaque personnage (règle actuelle) ; un groupe peut faire le tour ensemble, chacun examine pour lui-même.
- **Abandon, reconnexion :** rien de particulier : les compteurs des objectifs sont gardés par le serveur, comme pour toute quête donnée.
- **Échec :** impossible (rien ne se perd, rien ne se rate).
- **Événements déclenchés :** aucun dans le monde. Les obstacles restent en place après la quête.
- **Pourquoi elle existe (bible §11) :** elle introduit un lieu et un personnage (Anatole), fait comprendre la géographie du chapitre (les routes se séparent ici), dit sans menu pourquoi l'ordre libre « dans l'ordre que tu voudras » commence par la Terre, et ajoute une anomalie (trois routes fermées sans cause, le même mois).

### Les textes
- **Offre (Ysaline) :**
  - fr : « Hortense vient d'ôter les barres de la porte de l'Est, au bout de la rue des artisans : personne ne l'avait ouverte depuis le temps de sa grand-mère. Les routes de Rose, de Garance et d'Aubin passent toutes par là, jusqu'à un relais qu'on appelle Quatre-Chemins. Anatole y tient le livre des routes ; il m'écrit depuis vingt ans. Va le voir : il sait lesquelles on peut prendre. »
  - en : "Hortense has just taken the bars off the East Gate, at the end of Craftsmen's Row: nobody had opened it since her grandmother's day. Rose's, Garance's and Aubin's roads all go that way, as far as a coaching inn they call Four Ways. Anatole keeps the book of roads there; he's been writing to me for twenty years. Go and see him: he knows which ones can be taken."
- **Objectifs** (intitulés du journal, produits par le moteur à partir des noms) : « Examiner : Le poteau aux quatre bras », « Examiner : L'éboulis », « Examiner : Le brasier », « Examiner : Le gué en crue », « Parler à Anatole ».
- **Réplique d'Anatole pendant l'objectif (`line`)** :
  - fr : « Ysaline t'envoie ? Bien. Elle écrit trop, mais elle écrit juste. Fais le tour du hameau et regarde les trois routes fermées de tes yeux : mon livre ne suffit pas à tout le monde. »
  - en : "Ysaline sent you? Good. She writes too much, but she writes true. Walk round the hamlet and look at the three closed roads with your own eyes: my book isn't enough for everyone."
- **Remerciement (Anatole) :**
  - fr : « Laisse-moi deviner : Ysaline t'a dit « dans l'ordre que tu voudras ». Sur le papier, oui. Cette saison, c'est la route qui choisit, et elle a choisi le sud. Je l'écris dans le livre : trois routes fermées, une ouverte, et ton nom à côté. »
  - en : "Let me guess: Ysaline told you 'in whatever order you like'. On paper, yes. This season the road does the choosing, and it has chosen the south. I'll put it in the book: three roads closed, one open, and your name beside them."

*Le remerciement clôt l'étape sans envoyer nulle part : la forêt n'existe pas encore. Il dit « le sud », pas « va au sud ». La réplique d'Anatole tient dans les deux ordres (avant ou après le tour du hameau), puisque les objectifs n'ont pas d'ordre.*

---

## 5. Les textes prêts à copier

### 5.1 Les dialogues (`data/dialogues/<id>.json`)
Même forme que ceux que produit `tools/village_dialogues.py` (`hello`, puis `topic1`, `topic2`…, chaque réplique renvoie aux autres sujets et à « À bientôt. »), sauf Lucien, qui suit `merchant.json` (réponses `"shop": true`). Toutes les répliques sont atteignables depuis `hello`, toutes ont une sortie.

**`anatole.json`** (4 répliques)
```json
{
  "id": "anatole",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Ah. Encore quelqu'un qui croit que les routes vont quelque part. Elles partent, c'est déjà beaucoup. Anatole, arpenteur : je tiens le livre des routes.", "en": "Ah. Another one who thinks roads go somewhere. They set off, and that's already a lot to ask. Anatole, surveyor: I keep the book of roads." },
      "answers": [
        { "text": { "fr": "Les routes ?", "en": "The roads?" }, "next": "topic1" },
        { "text": { "fr": "Pourquoi fermées ?", "en": "Why are they closed?" }, "next": "topic2" },
        { "text": { "fr": "Pourquoi par l'est ?", "en": "Why the east?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Quatre routes partent d'ici, une par bras du poteau. Au nord, un éboulis ; à l'est, la lande brûle ; au sud-ouest, le gué est en crue. Reste le sud : c'est la seule qu'on peut prendre en ce moment.", "en": "Four roads leave from here, one for each arm of the signpost. North, a rockfall; east, the heath is burning; south-west, the ford is in flood. That leaves the south: it's the only one you can take just now." },
      "answers": [
        { "text": { "fr": "Pourquoi fermées ?", "en": "Why are they closed?" }, "next": "topic2" },
        { "text": { "fr": "Pourquoi par l'est ?", "en": "Why the east?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Un éboulis sans orage, une crue sans pluie, un feu que personne n'a allumé, et les trois le même mois. Dans mon livre, j'ai écrit « hasard ». Puis je l'ai barré.", "en": "A rockfall without a storm, a flood without rain, a fire nobody lit, and all three in the same month. In my book I wrote 'chance'. Then I crossed it out." },
      "answers": [
        { "text": { "fr": "Les routes ?", "en": "The roads?" }, "next": "topic1" },
        { "text": { "fr": "Pourquoi par l'est ?", "en": "Why the east?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic3": {
      "text": { "fr": "Clairval regarde le Bois, ses trois portes aussi. Les routes, elles, ont toujours évité le Bois : elles partent toutes d'ici, même celle de la côte, qui fait le tour par le gué. Ta porte de l'Est était la seule qui menait quelque part.", "en": "Clairval faces the woods, and so do its three gates. The roads have always kept clear of the woods: they all start from here, even the coast road, which goes round by the ford. Your East Gate was the only one that led anywhere." },
      "answers": [
        { "text": { "fr": "Les routes ?", "en": "The roads?" }, "next": "topic1" },
        { "text": { "fr": "Pourquoi fermées ?", "en": "Why are they closed?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```

**`romane.json`** (4 répliques)
```json
{
  "id": "romane",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Tu viens de Clairval ? Par la porte de l'Est ? Personne n'est jamais arrivé par là, de toute ma vie !", "en": "You've come from Clairval? Through the East Gate? Nobody's ever come that way, not in my whole life!" },
      "answers": [
        { "text": { "fr": "Le sud ?", "en": "The south?" }, "next": "topic1" },
        { "text": { "fr": "Le poteau ?", "en": "The signpost?" }, "next": "topic2" },
        { "text": { "fr": "Anatole ?", "en": "Anatole?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Le sud, c'est la seule route où l'on garde les bottes sèches et les sourcils entiers. Elle monte vers les collines. Plus loin, je ne sais pas : Anatole dit que je compte mal les pas.", "en": "The south's the only road where you keep your boots dry and your eyebrows on. It climbs towards the hills. Beyond that I couldn't say: Anatole says I can't count paces." },
      "answers": [
        { "text": { "fr": "Le poteau ?", "en": "The signpost?" }, "next": "topic2" },
        { "text": { "fr": "Anatole ?", "en": "Anatole?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "C'est moi qui repeins les signes, chaque printemps, sans jamais les changer de bras. Ysaline a écrit que sa pierre ne regardait plus du même côté. Eh bien, ce n'est pas mon poteau qui a bougé.", "en": "I'm the one who repaints the signs every spring, and I never move them to another arm. Ysaline wrote that her stone isn't facing the same way any more. Well, it isn't my signpost that's moved." },
      "answers": [
        { "text": { "fr": "Le sud ?", "en": "The south?" }, "next": "topic1" },
        { "text": { "fr": "Anatole ?", "en": "Anatole?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic3": {
      "text": { "fr": "Il compte tout : les pas, les voyageurs, les marches de son escalier. Le soir, il recompte, au cas où. Je l'aime bien, mais je ne le lui dis pas : il le noterait.", "en": "He counts everything: paces, travellers, the steps on his stairs. In the evening he counts them again, just in case. I'm fond of him, but I don't tell him: he'd write it down." },
      "answers": [
        { "text": { "fr": "Le sud ?", "en": "The south?" }, "next": "topic1" },
        { "text": { "fr": "Le poteau ?", "en": "The signpost?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```
*La réplique du poteau est la seule qui touche à la trame (l'anomalie voulue des signes qui ont tourné, D79). Si Charles préfère la garder pour Ysaline au début d'un arc, voir §7.2, question 4 : une réplique de remplacement y est donnée.*

**`lucien.json`** (3 répliques, l'étal de Rose)
```json
{
  "id": "lucien",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Bienvenue au relais ! Mêmes prix que ma sœur Rose, au même Éclat près : on s'est disputés une fois, ça a suffi.", "en": "Welcome to the inn yard! Same prices as my sister Rose, to the last Éclat: we argued about it once, and once was enough." },
      "answers": [
        { "text": { "fr": "Voyons l'étal.", "en": "Let's see the stall." }, "shop": true },
        { "text": { "fr": "Rose est ta sœur ?", "en": "Rose is your sister?" }, "next": "sister" },
        { "text": { "fr": "Et les voyageurs ?", "en": "And the travellers?" }, "next": "travellers" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "sister": {
      "text": { "fr": "L'aînée, et elle ne me laisse pas l'oublier. Elle a pris le village, j'ai pris le carrefour : chacun voit passer son monde.", "en": "The elder, and she never lets me forget it. She took the village, I took the crossroads: we each get our own passers-by." },
      "answers": [
        { "text": { "fr": "Voyons l'étal.", "en": "Let's see the stall." }, "shop": true },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "travellers": {
      "text": { "fr": "D'habitude, ils arrivent des quatre côtés. Cette saison, il n'en vient plus que du sud. Trois routes fermées, c'est trois clients sur quatre envolés.", "en": "Usually they come in from all four sides. This season they only come from the south. Three roads closed means three customers in four gone." },
      "answers": [
        { "text": { "fr": "Voyons l'étal.", "en": "Let's see the stall." }, "shop": true },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```
*« Voyons l'étal. » reprend la réponse de Rose ; son anglais actuel, "Let us see the stall.", sonne raide, d'où "Let's see the stall." ici. On peut aligner les deux dans un sens ou dans l'autre.*

**`josette.json`** (3 répliques)
```json
{
  "id": "josette",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Une chambre ? Une soupe ? Les deux ? Assieds-toi, tu as la tête de quelqu'un qui a marché.", "en": "A room? Some soup? Both? Sit yourself down, you look like someone who's been walking." },
      "answers": [
        { "text": { "fr": "Le relais ?", "en": "The inn?" }, "next": "topic1" },
        { "text": { "fr": "Rien d'étrange ?", "en": "Anything odd?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Douze lits, et onze vides depuis la dernière lune. Le douzième, c'est Aristide, et il me paie en miel.", "en": "Twelve beds, and eleven empty since the last moon. The twelfth is Aristide's, and he pays me in honey." },
      "answers": [
        { "text": { "fr": "Rien d'étrange ?", "en": "Anything odd?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Mes derniers voyageurs, d'où qu'ils viennent, m'ont tous demandé s'il était vrai qu'une pierre parlait, à Clairval. Moi, je n'en savais rien. Toi, on dirait que si.", "en": "My last travellers, wherever they came from, all asked me whether it was true a stone talked, over in Clairval. I'd no idea. You look as if you do." },
      "answers": [
        { "text": { "fr": "Le relais ?", "en": "The inn?" }, "next": "topic1" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```

**`aristide.json`** (3 répliques)
```json
{
  "id": "aristide",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Doucement, Biscotte, on ne vient pas nous voler. Enfin, je crois. Bonjour !", "en": "Easy, Biscotte, nobody's here to rob us. I don't think. Hello!" },
      "answers": [
        { "text": { "fr": "Le gué ?", "en": "The ford?" }, "next": "topic1" },
        { "text": { "fr": "La charrette ?", "en": "The cart?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "D'habitude, on passe avec de l'eau aux genoux, Biscotte et moi. Là, la rivière emporterait la charrette, la mule et le charretier. Et il n'a pas plu depuis un mois.", "en": "Usually Biscotte and I cross with the water up to our knees. Now the river would take the cart, the mule and the carter. And it hasn't rained for a month." },
      "answers": [
        { "text": { "fr": "La charrette ?", "en": "The cart?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Du miel des Champs pour la côte. Trois semaines que j'attends ici : il va finir par cristalliser, et moi avec.", "en": "Honey from the Fields, for the coast. Three weeks I've been waiting here: it'll set solid before long, and so will I." },
      "answers": [
        { "text": { "fr": "Le gué ?", "en": "The ford?" }, "next": "topic1" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```

**`remi.json`** (3 répliques)
```json
{
  "id": "remi",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Bonjour ! Attention, je déblaie. Enfin, je commence à déblayer.", "en": "Morning! Careful, I'm clearing. Well, I'm starting to clear." },
      "answers": [
        { "text": { "fr": "L'éboulis ?", "en": "The rockfall?" }, "next": "topic1" },
        { "text": { "fr": "Le vent ?", "en": "The wind?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Tombé en une nuit, sans orage et sans gel : au matin, la route du nord n'était plus là. J'en sors douze pierres par jour. À ce compte-là, mes petits-enfants passeront.", "en": "Came down in one night, no storm, no frost: in the morning the north road just wasn't there. I shift twelve stones a day. At that rate, my grandchildren will get through." },
      "answers": [
        { "text": { "fr": "Le vent ?", "en": "The wind?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Entre les blocs, le vent siffle sans arrêt, même quand rien ne bouge ici. On dirait qu'il cherche à passer, lui aussi. Il a plus de chances que moi.", "en": "The wind whistles between the blocks all the time, even when nothing's stirring down here. It's as if it's trying to get through as well. It's got a better chance than I have." },
      "answers": [
        { "text": { "fr": "L'éboulis ?", "en": "The rockfall?" }, "next": "topic1" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```

**`ghislaine.json`** (3 répliques)
```json
{
  "id": "ghislaine",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Recule, tu vas prendre la suie. Moi, j'y suis faite.", "en": "Stand back, you'll get soot on you. I'm used to it." },
      "answers": [
        { "text": { "fr": "Le brasier ?", "en": "The blaze?" }, "next": "topic1" },
        { "text": { "fr": "Le charbon ?", "en": "The charcoal?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Trente ans que je fais du feu, je le connais. Celui-là ne mange rien : il ne s'étend pas, il ne s'éteint pas, il tient la route. Un feu qui ne mange pas, ce n'est pas un feu.", "en": "Thirty years I've been making fires; I know them. That one eats nothing: it doesn't spread, it doesn't go out, it just holds the road. A fire that doesn't eat isn't a fire." },
      "answers": [
        { "text": { "fr": "Le charbon ?", "en": "The charcoal?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Les forgerons de l'est m'achetaient tout mon charbon. Depuis qu'ils ont fermé leur atelier, l'an passé, ils en prenaient moins. Maintenant, ils n'en auront plus du tout.", "en": "The smiths out east used to buy all my charcoal. Since they shut their workshop last year they'd been taking less. Now they won't get any at all." },
      "answers": [
        { "text": { "fr": "Le brasier ?", "en": "The blaze?" }, "next": "topic1" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```
*« L'atelier fermé l'an passé » reprend mot pour mot ce que dit Garance dans `four-trails`.*

### 5.2 Les objets posés (`props` des cartes)
```json
{
  "id": "waystone-fourways",
  "look": "waystone",
  "name": { "fr": "Pierre de passage des Quatre-Chemins", "en": "Four Ways waystone" },
  "text": {
    "fr": "Une pierre de passage plus usée que celle de Clairval : ici, tout le monde arrive de quelque part. Qui l'a touchée une fois peut y revenir d'une autre pierre, contre quelques Éclats.",
    "en": "A waystone more worn than Clairval's: here, everyone has come from somewhere. Whoever has touched it once can come back to it from another stone, for a few Éclats."
  }
},
{
  "id": "four-armed-signpost",
  "look": "signpost",
  "name": { "fr": "Le poteau aux quatre bras", "en": "The four-armed signpost" },
  "text": {
    "fr": "Un poteau de chêne à quatre bras, sans un seul nom : seulement des signes. La graine montre le sud, la goutte le sud-ouest, le marteau l'est, la plume le nord. Le bois est neuf ; les signes ont été recopiés d'un poteau plus vieux, et celui-là d'un plus vieux encore.",
    "en": "An oak signpost with four arms and not a single name: only signs. The seed points south, the drop south-west, the hammer east, the feather north. The wood is new; the signs were copied from an older post, and that one from an older one still."
  }
},
{
  "id": "north-rockfall",
  "look": "rockfall",
  "name": { "fr": "L'éboulis", "en": "The rockfall" },
  "text": {
    "fr": "Un pan du chemin creux s'est couché en travers de la route du nord : des blocs plus gros qu'une charrette, empilés jusqu'en haut du talus. Entre eux, le vent siffle sans arrêt. Personne ne passera par ici, ni à pied ni à cheval.",
    "en": "A whole bank of the sunken lane has come down across the north road: blocks bigger than a cart, heaped to the top of the bank. The wind whistles between them without stopping. Nobody's getting through here, on foot or on horseback."
  }
},
{
  "id": "east-blaze",
  "look": "blaze",
  "name": { "fr": "Le brasier", "en": "The blaze" },
  "text": {
    "fr": "La lande brûle en travers de la route de l'est, sur cent pas de large. Le feu ne gagne pas la bruyère d'à côté, et il ne baisse pas non plus : il reste sur la route. On sent sa chaleur d'ici.",
    "en": "The heath is burning right across the east road, a hundred paces wide. The fire doesn't spread to the heather beside it, and it doesn't die down either: it stays on the road. You can feel the heat from here."
  }
},
{
  "id": "flooded-ford",
  "look": "ford",
  "name": { "fr": "Le gué en crue", "en": "The flooded ford" },
  "text": {
    "fr": "D'habitude, on traverse ici avec de l'eau aux genoux. Aujourd'hui, la rivière roule large et brune, et charrie des joncs arrachés au Lac. Le piquet qui marquait le gué a disparu sous l'eau.",
    "en": "Usually you cross here with the water at your knees. Today the river runs wide and brown, carrying reeds torn from the lake. The post that marked the ford has gone under."
  }
}
```
Les positions (`at`) sont au dev : chaque obstacle **sur** la route, au bord de sa carte, de façon à la barrer (voir §6, point 3).

### 5.3 La quête (`data/quests.json`, après `four-trails`)
```json
  {
    "id": "where-roads-part",
    "name": { "fr": "Là où les routes se séparent", "en": "Where the Roads Part" },
    "giver": "clairval/ysaline",
    "zone": "fourways",
    "level": 30,
    "kind": "Main",
    "text": {
      "fr": "Hortense vient d'ôter les barres de la porte de l'Est, au bout de la rue des artisans : personne ne l'avait ouverte depuis le temps de sa grand-mère. Les routes de Rose, de Garance et d'Aubin passent toutes par là, jusqu'à un relais qu'on appelle Quatre-Chemins. Anatole y tient le livre des routes ; il m'écrit depuis vingt ans. Va le voir : il sait lesquelles on peut prendre.",
      "en": "Hortense has just taken the bars off the East Gate, at the end of Craftsmen's Row: nobody had opened it since her grandmother's day. Rose's, Garance's and Aubin's roads all go that way, as far as a coaching inn they call Four Ways. Anatole keeps the book of roads there; he's been writing to me for twenty years. Go and see him: he knows which ones can be taken."
    },
    "thanks": {
      "fr": "Laisse-moi deviner : Ysaline t'a dit « dans l'ordre que tu voudras ». Sur le papier, oui. Cette saison, c'est la route qui choisit, et elle a choisi le sud. Je l'écris dans le livre : trois routes fermées, une ouverte, et ton nom à côté.",
      "en": "Let me guess: Ysaline told you 'in whatever order you like'. On paper, yes. This season the road does the choosing, and it has chosen the south. I'll put it in the book: three roads closed, one open, and your name beside them."
    },
    "objectives": [
      { "goal": "Interact", "target": "four-armed-signpost" },
      { "goal": "Interact", "target": "north-rockfall" },
      { "goal": "Interact", "target": "east-blaze" },
      { "goal": "Interact", "target": "flooded-ford" },
      {
        "goal": "Talk",
        "target": "fourways/anatole",
        "line": {
          "fr": "Ysaline t'envoie ? Bien. Elle écrit trop, mais elle écrit juste. Fais le tour du hameau et regarde les trois routes fermées de tes yeux : mon livre ne suffit pas à tout le monde.",
          "en": "Ysaline sent you? Good. She writes too much, but she writes true. Walk round the hamlet and look at the three closed roads with your own eyes: my book isn't enough for everyone."
        }
      }
    ],
    "handIn": "fourways/anatole",
    "after": [ "four-trails" ],
    "xp": 1200,
    "shards": 200
  }
```
Comme en v6, les guillemets intérieurs sont des chevrons « » en français et des apostrophes en anglais, pour ne pas échapper de `"`. Si la zone `fourways` ne figure pas dans le tri du journal, on retire `"zone"` (comme `four-trails`, qui n'en a pas).

### 5.4 La porte de l'Est fermée (phrase dite au joueur qui bute dessus)
- fr : « La porte de l'Est est barrée de grosses planches. Personne à Clairval ne se souvient de l'avoir vue ouverte. »
- en : "The East Gate is barred with heavy planks. Nobody in Clairval remembers ever seeing it open."

Le nom du passage, s'il en faut un (comme « Bois de Clairval » sur les passages de la cour d'armes) : **Quatre-Chemins / Four Ways**.

### 5.5 Facultatif : un sujet de plus pour Hortense (Clairval)
Hortense dit aujourd'hui « trois portes et pas un trou », et l'offre d'Ysaline lui fait ôter les barres de la quatrième. Un sujet de plus la fait répondre (elle passe à 3 sujets, 4 réponses). À coller dans `tools/village_dialogues.py`, à la suite de ses deux sujets :
```python
  (t("La porte de l'Est ?", "The East Gate?"), t("Celle-là, je ne la comptais pas : une porte qui ne s'ouvre pas, c'est un mur. Ma grand-mère disait qu'on l'avait barrée « pour de bonnes raisons ». Elle n'a jamais dit lesquelles.", "That one I never counted: a gate that doesn't open is a wall. My grandmother said it was barred 'for good reasons'. She never said which."))
```
Sans ce sujet, rien ne se contredit vraiment (une porte barrée n'est pas une porte, pour Hortense), mais le sujet rend la chose voulue.

### 5.6 Facultatif : la route du sud au bord de la carte (S129 et S130 seulement)
Si le jeu dit quelque chose quand on bute sur un bord de carte sans passage (je ne crois pas qu'il le fasse), une phrase neutre, qui ne promet rien :
- fr : « La route du sud file entre les haies vers les collines. Pour aujourd'hui, le relais est le bout du voyage. »
- en : "The south road runs off between the hedges towards the hills. For today, the inn is the end of the journey."

À retirer au S131, quand la forêt ouvre le passage. Sinon, un simple bord de carte suffit, et je préfère.

---

## 6. Ce qui demande du code ou une décision (pour le dev et le game designer)

1. **Une zone nouvelle, `fourways` [P]**, de quatre cartes (`fourways-0-0`, `1-0`, `0-1`, `1-1`), niveau 30, au style de Clairval (S104). Les références des habitants deviennent `fourways/<id>`, comme `woods/mathurin`. Les identifiants sont au dev : s'il préfère `four-ways`, seules les références changent.
2. **Un passage conditionnel.** La porte de l'Est (bord est de `clairval-3-1`, aujourd'hui une palissade `P`) ne mène à `fourways-0-1` que si `four-trails` est faite pour ce personnage ; sinon, le joueur reçoit la phrase du §5.4. Le moteur n'a pas encore de passage qui dépend d'une quête : c'est un ajout (côté serveur pour la validation du déplacement, côté client pour la phrase). Le texte peut vivre dans `Texts.cs` ou sur le passage lui-même.
3. **Des obstacles qui barrent.** Chaque obstacle est un objet posé **et** des cases infranchissables sur la route, au bord de la carte, sans passage derrière. `KeepsWaysOpen` doit l'accepter : la route existe, elle ne mène nulle part. La route du sud de `fourways-1-1` est un bord sans passage jusqu'au S131.
4. **Quatre aspects nouveaux** : `signpost`, `rockfall`, `blaze`, `ford`. Avec Kenney (D76) : un poteau indicateur existe sans doute ; l'éboulis peut être un tas de rochers ; le gué, de l'eau sur la route et `reeds` ; le brasier demande un effet de feu (des particules, ou des braises fixes). En attendant, `stone` et `reeds` peuvent servir pour l'éboulis et le gué ; le brasier n'a pas de remplaçant lisible.
5. **Un deuxième étal.** Lucien ouvre l'étal de Rose (`"shop": true`), mêmes objets, mêmes prix. À vérifier : le client accepte une réponse `shop` chez un autre habitant que Rose, et l'étal est bien le même (T95 rappelle que « le jeu n'a qu'un étal »).
6. **La pierre de passage** : `tools/zone_people.py` pose les autres ; celle-ci suit la règle de T95 (deux cases au plus de l'arrivée de `fourways-0-1`) et son voyage coûte 10 + 2 × 30 = 70 Éclats. Son texte diffère des cinq autres (la première phrase) : si un test compare les textes des pierres, il faut l'adapter, ou garder le texte commun et ne changer que le nom.
7. **Une quête rendue dans une autre zone.** Ysaline donne, Anatole reprend (`handIn`), comme `welcome`. À vérifier : le fil du journal (`QuestTrail`) mène bien de Clairval à Quatre-Chemins à travers la porte de l'Est, et les quatre `Interact` le guident d'une carte à l'autre. Ysaline n'a pas d'autre offre en cours à ce moment (limite de deux offres par habitant, T70).
8. **Les noms affichés** des habitants avec leur métier (« Anatole, l'arpenteur »), si le jeu les affiche ainsi depuis la v6 ; sinon, le prénom seul.
9. **La suite.** Les étapes de la Forêt (S136) prennent `"after": ["where-roads-part"]`, pour que personne n'entre dans la forêt sans être passé par le carrefour. Une réplique d'Anatole qui change quand la forêt existe (« trois routes fermées, une ouverte » reste vraie) n'est pas nécessaire.
10. **Rien ne bloque** (bible §17) : la quête n'est proposée que porte ouverte (même prérequis) ; aucun objectif ne dépend d'un contenu futur ; les cinq objectifs se font dans n'importe quel ordre ; aucun combat, aucun objet pris ni donné, donc rien à rendre deux fois.

---

## 7. Points de canon et questions pour Charles

### 7.1 Ce que je signale (rien n'est corrigé)
- **Aucun écart avec la bible.** L'Accord n'est ni nommé ni évoqué, aucun enfant de Primordial n'est montré, rien n'est révélé au-delà du niveau 1 : trois routes fermées sans cause le même mois, une porte oubliée, une rumeur de pierre qui parle, des signes qui n'ont pas bougé sur le poteau. Chaque obstacle porte son élément sans qu'on le dise. La réplique du gardien de la Chambre sans nom n'apparaît pas.
- **Hortense compte « trois portes ».** La porte de l'Est n'était pas comptée parce qu'elle était barrée ; c'est cohérent, mais le sujet facultatif du §5.5 le rend voulu.
- **La géographie de la route de la côte.** Névara est à l'ouest, au bout de la rivière qui quitte le Lac des Joncs et longe le Bois moisi. Partir vers l'ouest par une porte de l'Est demande une raison : je propose **[P]** que les routes aient toujours évité le Bois (Anatole, « Pourquoi par l'est ? »), et que celle de la côte fasse le tour par le sud-ouest jusqu'au gué, la rivière ayant tourné au sud de Clairval avant de filer vers la mer. Si la carte du monde vue du ciel (T38) ne le permet pas, il faut le dire, et je réécris.
- **La porte barrée « depuis le temps de la grand-mère d'Hortense »** est une anomalie de mémoire de plus, sans explication **[P]**. Elle ne dit pas qui l'a barrée. Elle explique aussi comment les voyageurs de Rose venaient jusqu'ici : en faisant le tour.
- **Les signes du poteau** (graine au sud, goutte au sud-ouest, marteau à l'est, plume au nord) suivent la géographie décidée (D79), pas ceux de `four-signs`. C'est l'anomalie voulue des signes qui ont tourné (§10.3, réponse 2) : le poteau, repeint chaque printemps, n'a pas bougé ; la pierre, si. Romane le dit. Le cadre du chapitre 2 donnait cette remarque à Ysaline, au début d'un arc : je l'avance d'une étape et la fais porter par quelqu'un d'autre (question 4).
- **La première étape de l'arc de Kaor, `east-gate`**, faisait percer la porte de l'Est par Garance (`chapitre-2-pistes.md` §3). La porte est maintenant ouverte par `four-trails` : cette étape devra être réécrite quand viendra le Feu (par exemple autour du brasier). Rien à faire avant.
- **« Les quatre habitants donnent toujours leur piste à Clairval »** (réponse 7) : ils sont trois (Rose, Garance, Aubin, qui en donne deux). Je l'ai lu comme « les quatre pistes ».
- **Le titre « l'érudit »** du plan est porté par Anatole, un arpenteur, pas un savant des pierres : Ysaline reste l'érudite de la pierre, et Anatole est l'érudit des routes. Il n'« ouvre » pas les quatre pistes (`design-familles.md` §7) : depuis D79, elles s'ouvrent à Clairval, et il dit où elles se séparent.

### 7.2 Les questions (dans l'ordre où je les relirais)
1. **Anatole, l'érudit** : un arpenteur qui tient « le livre des routes », correspondant d'Ysaline depuis vingt ans. Ça vous va ? C'est le personnage qui revient le plus (il reprend la quête et parle des quatre routes).
2. **Lucien, le frère de Rose** : c'est ce qui explique en un mot les mêmes prix. Rose n'a aucune famille dans le jeu jusqu'ici. Gardé ?
3. **La porte de l'Est barrée depuis la grand-mère d'Hortense, sans raison connue**, et le sujet facultatif d'Hortense (§5.5). Le mystère vous convient, ou préférez-vous une raison banale (un incendie ancien, une querelle avec le relais) ?
4. **La réplique de Romane sur le poteau** (les signes qui n'ont pas bougé, la pierre d'Ysaline qui ne regarde plus du même côté). On la garde ici, ou on la laisse à Ysaline au début d'un arc ? Réplique de remplacement, sans la pierre : « C'est moi qui repeins les signes, chaque printemps, sans jamais les changer de bras. Graine au sud, goutte au sud-ouest, marteau à l'est, plume au nord : depuis toujours. » / "I'm the one who repaints the signs every spring, and I never move them to another arm. Seed south, drop south-west, hammer east, feather north: always have been."
5. **Le nom anglais « Four Ways »**. Autre possibilité, très anglaise : « Four Lane Ends » (un nom de carrefour courant en Angleterre), mais les routes du jeu ne sont pas des chemins de campagne. Je recommande « Four Ways ».
6. **La géographie de la route de la côte** (§7.1, troisième point) : la rivière qui tourne au sud de Clairval, d'accord ?
7. **La récompense** : 1 200 XP et 200 Éclats (sous `four-trails`), ou autant que `four-trails` ?
8. **La phrase du bord sud** (§5.6) : la voulez-vous, ou un bord de carte muet suffit-il jusqu'au S131 ?
9. **Les obstacles comme départs des autres arcs** : en les faisant tomber le même mois, je crée une attente (pourquoi maintenant ?), qui servira quand Névara, Kaor et Élyon arriveront (chaque arc commencerait par lever son obstacle). C'est une promesse faite au lecteur attentif, pas au joueur : aucun texte ne dit « plus tard ». Ça vous va ?
10. **Les prénoms** Anatole, Romane, Lucien, Josette, Aristide, Rémi, Ghislaine et la mule Biscotte : à vérifier de votre côté qu'aucun PNJ d'Ankama ne les porte ; je n'en connais pas, sans pouvoir le garantir.

---

## 8. Récapitulatif pour la transcription

- **Zone [P]** : `fourways`, « Quatre-Chemins / Four Ways », niveau 30. Cartes : `fourways-0-1` (sud-ouest, arrivée, « Quatre-Chemins, la place du relais / Four Ways, Inn Square »), `fourways-0-0` (nord-ouest, « …, le chemin creux / …, the Sunken Lane »), `fourways-1-0` (nord-est, « …, la lande fumante / …, the Smoking Heath »), `fourways-1-1` (sud-est, « …, la maison des routes / …, the Road House »).
- **Habitants (7)**, avec leur dialogue `data/dialogues/<id>.json` au §5.1 :
  - `fourways-0-1` : `lucien` (marchand, `male-e`, `"shop": true`), `josette` (`female-e`), `aristide` (`male-f`) ;
  - `fourways-0-0` : `remi` (`male-d`) ;
  - `fourways-1-0` : `ghislaine` (`female-c`) ;
  - `fourways-1-1` : `anatole` (l'érudit, `male-c`, reprend la quête), `romane` (`female-a`).
- **Objets posés (5)**, au §5.2 : `waystone-fourways` (`waystone`, sud-ouest), `four-armed-signpost` (`signpost`, sud-ouest), `flooded-ford` (`ford`, sud-ouest, bord sud), `north-rockfall` (`rockfall`, nord-ouest, bord nord), `east-blaze` (`blaze`, nord-est, bord est).
- **Quête (1)**, au §5.3 : `where-roads-part`, « Là où les routes se séparent / Where the Roads Part », Main, niveau 30, donneuse `clairval/ysaline`, `handIn` `fourways/anatole`, `after` `four-trails`, objectifs `Interact` × 4 (poteau, éboulis, brasier, gué) et `Talk` Anatole avec réplique, 1 200 XP, 200 Éclats.
- **Phrase de la porte de l'Est fermée** : §5.4.
- **Facultatifs** : le sujet d'Hortense (§5.5), la phrase du bord sud (§5.6).
