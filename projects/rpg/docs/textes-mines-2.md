# Osmose : textes des Mines de Sous-Racine, seconde passe (sprint 232)

> Proposition du scénariste, 27 novembre 2026, pour D103 (`docs/jalon-4.md` §7-8, Q4 a). **Aucun id ne change.** Rien n'est reporté dans `data/` : le dev s'en charge.
> Sources lues : `docs/bible.md`, `docs/chapitre-2-pistes.md` §5, `docs/familles-mines.md`, `docs/familles-mines-textes.md`, `docs/textes-v8.md`, `data/dialogues/berthe.json`, `data/dialogues/berold.json`, `data/quests.json` (quêtes d'Anatole, de Berthe et de Bérold), `data/items.json`, `data/achievements.json`, `data/towns/ancestors-2-1.json` (format des objets `Interact`).
> **[P]** = proposition, absente de la bible et du jeu.
> **Appliqué au sprint 232 (T197)** : toute la partie A ; la partie B avec les récompenses de B.6 (Lotte, choix annoncé dans l'offre, salutations de B.7.1 faites ; B.7.2, le mur visible, non fait). Réponses aux questions prises par défaut, à confirmer par Charles.

## Partie A : relecture des textes de S224, S227 et S228

Légende : **Garder**, ou **Changer** avec la proposition en gras.

### A.1 Les ressources (S224)

| id | Actuel fr / en | Avis | Proposition fr / en | Raison |
|---|---|---|---|---|
| `larch-log` | Mélèze / Larch | Garder | | Un arbre réel, comme Chêne ou Orme. |
| `rootwood-log` | Bois-racine / Rootwood | Garder | | C'est le seul bois inventé, et c'est voulu pour une zone sous l'Arbre. Il se comprend tout seul et ne contient aucun nom de lieu. |
| `cobalt-ore` | Cobalt | Garder | | Même forme que Fer, Zinc ou Étain. |
| `gold-ore` | Or / Gold | Garder | | Même forme qu'Argent / Silver (niveau 30). La monnaie s'appelle les Éclats, donc pas de confusion. |
| `grayling` | Ombre / Grayling | **Changer** | **Lotte / Burbot** | Seul, « Ombre » se lit d'abord « l'ombre », et le chapitre 4 aura « cinq ombres ». Une « Ceinture en peau d'ombre » passerait pour un indice de lore (bible §12, secrets collectifs). La lotte est un vrai poisson d'eau froide qui vit au fond et sort la nuit : elle a sa place dans une rivière de mine. Solution de repli : « Ombre de rivière », mais la ceinture devient trop longue (A.3). |
| `cavefish` | Poisson des grottes / Cavefish | **Changer** | **Loche des grottes / Cave Loach** | « Poisson » est trop vague à côté de Goujon, Tanche ou Silure. La loche des grottes est un vrai poisson. Le nom des bottes se raccourcit aussi (A.3). |
| `barley` | Orge / Barley | Garder | | |
| `millet` | Millet | Garder | | |
| `arnica` | Arnica | Garder | | |
| `gentian` | Gentiane / Gentian | Garder | | |

*Remarque pour le dev, sans rapport avec les textes :* `rootwood-log` et `elm-log` ont la même icône (`delapouite/tree-roots`). `grayling` et `zander` aussi (`various-artists/salmon`), comme `cavefish` et `pool-eel` (`delapouite/circling-fish`).

### A.2 Les donjons, les clefs, les succès et les quêtes de Berthe (S227)

| Texte | Actuel fr / en | Avis | Proposition fr / en | Raison |
|---|---|---|---|---|
| Donjon `ringing-seam` | Filon qui sonne / The Ringing Seam | Garder | | Le nom reprend la roche qui sonne (Gaspard, l'Écoute-roche) sans rien expliquer. |
| Donjon `mountains-belly` | Ventre de la montagne / The Mountain's Belly | Garder | | |
| Clef `ringing-seam-key` | Clef du filon / Seam Key | Garder | | Suit « Clef du récif / Reef Key ». |
| Clef `mountains-belly-key` | Clef du ventre / Belly Key | **Changer** | **Clef de la montagne / Mountain Key** | Hors contexte, dans un inventaire, « Clef du ventre » et *Belly Key* font sourire pour de mauvaises raisons. Aucun autre objet ne porte le nom « montagne ». |
| Succès `dungeon-ringing-seam` | Le filon s'est tu / The Seam Falls Silent | **Changer** | **Plus personne n'écoute / No One Left Listening** | Le filon ne doit pas se taire : la roche qui sonne est un mystère qui continue (Gaspard, `moved-mountains`), et Berthe le dit au rendu (« le filon sonne pour nous, maintenant »). Le nom actuel ferait croire que le chef causait le son. C'est le chef qui se tait, pas la roche. |
| Description du succès | Finissez le Filon qui sonne. | Garder | | Même forme que les autres succès de donjon. |
| Succès `dungeon-mountains-belly` | Plus un craquement / Not a Creak | Garder | | Fait écho au remerciement de Berthe. |
| Succès `dungeons-all` | Douze chefs tombés / Twelve Chiefs Fallen | Garder | | En anglais, *Chief* est déjà le mot du jeu (*Chief's Hall* dans les 12 cartes de donjon). |
| Nom des quêtes | Au fond : … / At the bottom: … | Garder | | C'est le gabarit commun des quêtes de donjon. |

**`ringing-seam-cleared` (Berthe, 59)**

| Champ | Avis | Proposition fr / en | Raison |
|---|---|---|---|
| `text` | **Changer** (un mot) | « Sous les Mines, il y a un filon que la roche fait sonner comme une cloche. Leur Écoute-roche l'a trouvé avant nous, et **ses Fouille-rocs** le gardent à la poudre. Va jusqu'au fond, avec du monde : je veux savoir ce qui sonne, avant que Bérold ne décide que c'est sacré. » / "Under the Mines there's a seam the rock makes ring like a bell. Their Stonelistener found it before we did, and **his Rockdelvers** guard it with powder. Go all the way down, with company: I want to know what's ringing before Bérold decides it's sacred." | « Fouilleurs » n'est pas le nom de la famille. Le joueur doit reconnaître les monstres qu'il a combattus. |
| `thanks` | **Changer** (une phrase) | « L'Écoute-roche s'est tu ? Alors le filon sonne pour nous, maintenant. **Bérold dit que quand la roche parle, on se tait.** Moi, j'entends du minerai. » / "The Stonelistener's gone quiet? Then the seam rings for us now. **Bérold says that when the rock speaks, you hold your tongue.** Me, I hear ore." | « Bérold dit qu'il a entendu des mots dedans » en dit trop. C'est Bérold qui lira le serment à `dhorams-fragment` (58), et il ne doit pas entendre de mots avant. Ça fait aussi de lui un mystique, alors qu'il applique sans comprendre. La nouvelle phrase garde l'opposition des deux voix. |

**`mountains-belly-cleared` (Berthe, 60)**

| Champ | Avis | Proposition fr / en | Raison |
|---|---|---|---|
| `text` | **Changer** (deux mots) | « **On dit que** le Ronge-montagne a mangé le dessous de la montagne, et on creuse au-dessus de son ventre. Chaque fois qu'il se retourne, mes étais craquent. Descends jusqu'à lui, à plusieurs, et fais en sorte qu'il ne se retourne plus. » / "**They say** the Mountain Gnawer ate the underside of the mountain, and we're digging right over its belly. Every time it turns over, my props creak. Go down to it, with others, and make sure it never turns over again." | Le bestiaire dit « On dit que » : c'est une fausse explication, et c'est voulu (`familles-mines-textes.md` §3.12, point 2). Si Berthe l'affirme comme un fait, elle prend la place de la découverte de `moved-mountains`. |
| `thanks` | Garder en français, **changer** la fin en anglais | « … Je te dois mes étais, et sans doute quelques têtes. » / "Not a creak since this morning. My lads are sleeping down the shaft for the first time. I owe you my props, **and a few heads besides**." | *And likely a few heads* n'est pas naturel. |

Ces deux textes restent justes quel que soit le parti pris dans la partie B. Si le joueur a aidé Bérold, Berthe creuse ailleurs, et ses phrases ne disent pas où.

### A.3 Les pièces, les élixirs et les pains (S228)

| id | Actuel fr / en | Avis | Proposition fr / en | Raison |
|---|---|---|---|---|
| `cobalt-pick` | Pic de cobalt / Cobalt Pick | Garder | | |
| `rootwood-greatmaul` | Grande masse de bois-racine / Rootwood Greatmaul | Garder | | Même forme que « Grande hache d'aulne / Alder Greataxe ». |
| `gilded-rootwood-shield` | Écu doré de bois-racine / Gilded Rootwood Shield | Garder | | « Doré » dit qu'il y a de l'or, sans allonger le nom avec la ressource. |
| `barley-spaulders` | Épaulières d'orge / Barley Spaulders | **Changer** | **Épaulières de paille d'orge / Barley-Straw Spaulders** | On ne fait pas d'épaulières avec des grains. On dit ce qu'on tresse, comme dans « Chapeau de paille d'avoine » et « Épaulières de blé tressé ». |
| `millet-cape` | Cape de millet / Millet Cape | **Changer** | **Cape de millet tressé / Plaited Millet Cape** | Même raison : ce sont les tiges qu'on tresse. |
| `millet-smock` | Bourgeron de millet / Millet Smock | **Changer** | **Bourgeron matelassé / Quilted Smock** | « Bourgeron », la blouse de travail des ouvriers, est le bon mot pour une zone de mines. C'est « de millet » qui cloche : la balle de millet sert à rembourrer, et « matelassé » le dit. D'autres objets du jeu ne nomment pas leur ressource (« Manteau de moisson »). |
| `cobalt-ring` | Bague de cobalt / Cobalt Ring | Garder | | Suit « Bague de faïence bleue ». |
| `gold-locket` | Médaillon d'or / Gold Locket | Garder | | |
| `lode-ring` | Anneau de filon / Lode Ring | Garder | | Fait avec le « Morceau de filon », et ça se lit. |
| `grayling-belt` | Ceinture en peau d'ombre / Grayling-Skin Belt | **Changer** | **Ceinture en peau de lotte / Burbot-Skin Belt** | Suit `grayling` (A.1). Avec la solution de repli « Ombre de rivière », la ceinture deviendrait « Ceinture en peau d'ombre de rivière » : trop long. |
| `cavefish-boots` | Bottes en peau de poisson des grottes / Cavefish-Skin Boots | **Changer** | **Bottes en peau de loche / Loach-Skin Boots** | Suit `cavefish`. On passe de six mots à cinq, comme « Bottes en peau de truite ». |
| `slough-swordbelt` | Ceinturon en mue de ver / Worm-Slough Swordbelt | **Changer** | **Ceinturon en mue de roche / Rock-Slough Swordbelt** | La ressource s'appelle « Mue de roche » : le joueur doit la reconnaître dans le nom. « Ceinturon de mue » tout court serait trop proche de la « Cape de mue » de la panoplie. |
| `superior-healing-elixir` | Élixir de soin supérieur / Superior Healing Elixir | Garder | | L'ordre « Grand, supérieur, suprême » se comprend. |
| `supreme-healing-elixir` | Élixir de soin suprême / Supreme Healing Elixir | Garder | | |
| `barley-bread` | Pain d'orge / Barley Bread | Garder | | |
| `millet-flatbread` | Galette de millet / Millet Flatbread | Garder | | |
| `miners-loaf` | Miche des mineurs / Miners' Loaf | Garder | | Pas de nom de personne ni de lieu, et ça répond à la « Miche de Clairval ». |

Aucune proposition ne reprend un nom qui existe déjà dans `items.json` (vérifié : Lotte, Loche, paille d'orge, millet tressé, matelassé, mue de roche, Clef de la montagne).

---

## Partie B : le fil « Garder ou creuser » en quêtes annexes (niveaux 46 à 52)

### B.1 Le principe

- **La dispute porte sur le boyau des mineurs [P]** : le passage que l'équipe de Berthe a percé sous l'Arbre, vers les veines vertes. Elle porte sur ce boyau seulement, pas sur toute la zone. Les Mines restent ouvertes dans les deux cas, et les donjons de 59-60 aussi : le joueur n'est jamais bloqué.
- **Bérold mure ce que des hommes ont creusé, pas ce que les vers ont creusé**. Le serment parle de « nous ». C'est sa rigidité : il applique le serment à la lettre, sans le comprendre.
- **Berthe élargit un passage qu'elle n'a pas vraiment creusé** : les vers de roche avaient fait le plus gros. Elle l'avoue à moitié (la proposition [P] de `familles-mines-textes.md` §3.12, point 3, reprise ici).
- **La première quête de chaque côté a les mêmes objectifs**, avec des textes opposés. On ramasse les mèches des Fouille-rocs dans les deux cas : Bérold, pour que rien ne fasse sauter son mur ; Berthe, pour s'en servir. Le joueur n'en fait qu'une. Ça fait sentir le choix, et le dev ne pose qu'un seul objet.
- **Ni l'un ni l'autre n'a tort** (bible §8, préserver ou exploiter). Bérold découvre que ses registres ne savaient pas tout. Berthe obtient de quoi payer ses gars, en suivant le chemin de quelqu'un d'autre.
- **Ce que le fil ne dit pas** : rien sur des couches à l'envers, une montagne déplacée, le cinquième nom, ni qui a juré. Ces découvertes restent à `moved-mountains` (50) et à `dhorams-fragment` (58). Les seuls mots du serment cités sont « Nous tiendrons », déjà lus dans `mossy-keepers`.
- **Voix** : Berthe tutoie, parle vite, en phrases sèches, avec les filons, les étais et la paye. Bérold vouvoie (« voyageur »), parle en formules au présent, sans exclamation.
- **Niveaux et objets `Bring`** : en suivant le précédent de `quests.json`, une quête ne demande jamais une ressource de métier d'un niveau supérieur au sien. À 46 et à 48, on ne demande donc que du butin de famille (`fuse-cord`, `lode-chip`). Le cobalt (niveau 50) n'arrive qu'à 51.

### B.2 La phrase de rivalité

Elle s'affiche quand le joueur accepte l'une des deux premières quêtes.

| Quête prise | fr | en |
|---|---|---|
| `wall-the-passage` | Tu as pris le parti de Bérold : le boyau sera muré, et Berthe ne te proposera plus de l'élargir. | You've sided with Bérold: the passage will be walled up, and Berthe won't ask you to widen it again. |
| `widen-the-passage` | Tu as pris le parti de Berthe : le boyau sera élargi, et Bérold ne te demandera plus de le murer. | You've sided with Berthe: the passage will be widened, and Bérold won't ask you to wall it up again. |

**[P] Je recommande d'annoncer le choix avant qu'il soit fait** (bible §12 : un choix irréversible doit être « explicitement annoncé »). Il suffit d'une ligne sous le texte de l'offre, ou d'une confirmation :

| Offre | fr | en |
|---|---|---|
| `wall-the-passage` | Si tu acceptes, Berthe ne te proposera plus d'élargir le boyau. Ce choix est définitif. | If you accept, Berthe won't ask you to widen the passage. This choice is final. |
| `widen-the-passage` | Si tu acceptes, Bérold ne te demandera plus de murer le boyau. Ce choix est définitif. | If you accept, Bérold won't ask you to wall up the passage. This choice is final. |

### B.3 Le côté de Bérold : murer

#### `wall-the-passage` · Murer le passage / Wall Up the Passage
- **Donneur et rendu** : `ancestors/berold` · **Niveau** : 46 · **Genre** : Side · **Prérequis** : `roots-of-the-mother-tree` · **Ferme** : `widen-the-passage`.
- **Objectifs** : `Interact` `miners-passage` ; `Defeat` `delver` ×6 ; `Bring` `fuse-cord` ×5.
- **`text`** : « Le serment dit : « Nous tiendrons. » Il ne dit pas quoi, alors nous tiendrons tout. Le boyau des mineurs sera muré, mais les petits à lanterne font sauter tout ce qui est fermé. Allez voir ce boyau, et ôtez-leur leurs mèches. »
  / "The oath says: 'We shall hold.' It does not say what, so we shall hold everything. The miners' passage will be walled up, but the little ones with lanterns blow up whatever is shut. Go and look at that passage, and take their fuses from them."
- **`thanks`** : « Six de moins, et leurs mèches dans ce sac. Les maçons descendront dès que nous saurons où poser le mur. Un serment tenu ne fait pas de bruit, voyageur : n'attendez aucun merci d'Ormeval. Recevez le mien. »
  / "Six fewer, and their fuses in this sack. The masons will go down as soon as we know where the wall must stand. A kept oath makes no noise, traveller: expect no thanks from Ormeval. Take mine."

#### `the-boundary-stone` · La borne / The Boundary Stone
- **Donneur et rendu** : `ancestors/berold` · **Niveau** : 48 · **Genre** : Side · **Prérequis** : `wall-the-passage`.
- **Objectifs** : `Interact` `boundary-stone` ; `Defeat` `borer` ×6.
- **`text`** : « Nos registres parlent d'une borne, sous l'Arbre, au-delà de laquelle on ne creuse pas. Personne ne l'a vue depuis trois générations. Trouvez-la : le mur sera posé là, et pas un pas plus loin. Les vers de roche mangent la pierre, même gravée ; écartez-les de la borne. »
  / "Our registers speak of a boundary stone beneath the Tree, past which no one digs. No one has seen it for three generations. Find it: the wall will stand there, and not one step further. The rock worms eat stone, even carved stone; keep them away from it."
- **`thanks`** : « Cent pas au-delà. Je tiens les registres depuis quarante ans, et personne n'y a écrit ces cent pas. Le mur sera posé à la borne. Ce qu'ils ont creusé derrière, la roche le reprendra. »
  / "A hundred paces beyond it. I have kept the registers for forty years, and no one ever wrote those hundred paces down. The wall will stand at the stone. What they dug beyond it, the rock will take back."
- *Pourquoi* : c'est la première fois que Bérold apprend quelque chose qu'il ne savait pas (« Je sais tout ce qui se passe sous cet Arbre », `roots-of-the-mother-tree`). Il ne change pas d'avis, mais il est touché.

#### `stone-for-stone` · Pierre pour pierre / Stone for Stone
- **Donneur et rendu** : `ancestors/berold` · **Niveau** : 51 · **Genre** : Side · **Prérequis** : `the-boundary-stone`.
- **Objectifs** : `Bring` `lode-chip` ×8 ; `Defeat` `delver` ×8 ; `Talk` `ancestors/berthe` (réplique ci-dessous).
- **`text`** : « Le mur ne sera pas fait de pierre neuve. Ce qu'on a arraché à la roche lui sera rendu, morceau par morceau : c'est la seule façon juste de refermer. Les petits à lanterne rôdent autour des maçons ; tenez-les loin. Ensuite, dites à Berthe ce qui a été fait : c'est à vous de le lui dire, puisque c'est vous qui l'avez fait. »
  / "The wall will not be built of new stone. What was torn from the rock will be given back to it, piece by piece: that is the only just way to close it. The little ones with lanterns are prowling round the masons; keep them off. Then tell Berthe what has been done: it is yours to tell her, since you are the one who did it."
- **Réplique de Berthe** (`Talk`, `line`) : « Alors c'est toi qui as porté les pierres du juge. Je ne t'en veux pas : il fallait bien que quelqu'un choisisse. Mais ce soir, c'est toi qui expliques à mes trente gars pourquoi il n'y a pas de paye. »
  / "So you're the one who carried the judge's stones. I don't hold it against you: somebody had to choose. But tonight, you're the one telling my thirty lads why there's no pay."
- **`thanks`** : « Le mur tient. Il ne porte qu'une pierre gravée, avec les deux mots que je connais. Je ne sais pas ce qu'il garde, voyageur. Je sais qu'il le garde. »
  / "The wall holds. It bears a single carved stone, with the two words I know. I do not know what it keeps, traveller. I know that it keeps it."

### B.4 Le côté de Berthe : élargir

#### `widen-the-passage` · Élargir le passage / Widen the Passage
- **Donneuse et rendu** : `ancestors/berthe` · **Niveau** : 46 · **Genre** : Side · **Prérequis** : `roots-of-the-mother-tree` · **Ferme** : `wall-the-passage`.
- **Objectifs** : `Interact` `miners-passage` ; `Defeat` `delver` ×6 ; `Bring` `fuse-cord` ×5 (les mêmes que `wall-the-passage`).
- **`text`** : « Le juge veut murer ? Moi, je veux y faire passer un wagonnet. Va voir mon boyau : trop étroit, et les petits à lanterne font sauter mes étais à mesure que je les pose. Calme-les, et rapporte-moi leurs mèches : de la poudre gratuite, je ne crache pas dessus. »
  / "The judge wants to wall it up? I want to run a cart through it. Go and look at my passage: too narrow, and the little ones with lanterns blow up my props as fast as I put them in. Settle them down, and bring me their fuses: free powder, I'm not turning that down."
- **`thanks`** : « Cinq mèches, et pas un étai par terre. Demain, on fait sauter le goulet, proprement, pas à leur façon. Tiens, mange : la poussière, ça creuse. »
  / "Five fuses, and not a prop down. Tomorrow we blast the narrow bit, properly, not their way. Here, eat: dust works up an appetite."

#### `follow-the-worms` · Suivre les vers / Follow the Worms
- **Donneuse et rendu** : `ancestors/berthe` · **Niveau** : 48 · **Genre** : Side · **Prérequis** : `widen-the-passage`.
- **Objectifs** : `Interact` `worm-tunnel-1` ; `Interact` `worm-tunnel-2` ; `Defeat` `borer` ×6.
- **`text`** : « Je vais te dire un secret, garde-le pour le juge : le passage, on ne l'a pas creusé, on l'a élargi. Les vers de roche avaient fait le plus gros. Va voir jusqu'où vont leurs tunnels, et chasse ceux qui mordent mes gars en chemin. »
  / "I'll tell you a secret, and you keep it from the judge: we didn't dig that passage, we widened it. The rock worms had done most of the work. Go and see how far their tunnels run, and see off the ones biting my lads along the way."
- **`thanks`** : « Plus loin que ma carte, et dans tous les sens ? Si on suit les vers, j'économise trois ans de pic. Le juge dirait qu'on marche dans les pas de quelqu'un d'autre ; moi, je dis qu'un tunnel vide, ça se prend. »
  / "Further than my map, and every which way? If we follow the worms, I save three years of pick work. The judge would say we're walking in someone else's footsteps; I say an empty tunnel is there for the taking."

#### `the-first-cart` · Le premier wagonnet / The First Cart
- **Donneuse et rendu** : `ancestors/berthe` · **Niveau** : 51 · **Genre** : Side · **Prérequis** : `follow-the-worms`.
- **Objectifs** : `Bring` `cobalt-ore` ×6 ; `Defeat` `borer` ×8 ; `Talk` `ancestors/berold` (réplique ci-dessous).
- **`text`** : « Le passage est large, les étais tiennent, et j'ai un wagonnet tout neuf. Il ne manque que de quoi le remplir : six morceaux de cobalt, et que les vers arrêtent de ronger mes rails. Puis va dire au juge que le premier wagonnet est sorti. Je préfère que ce soit toi : moi, il me ferait un discours. »
  / "The passage is wide, the props are holding, and I've got a brand-new cart. All it needs is something to fill it: six pieces of cobalt, and the worms to stop gnawing my rails. Then go and tell the judge the first cart is out. I'd rather it was you: he'd give me a speech."
- **Réplique de Bérold** (`Talk`, `line`) : « Vous avez ouvert ce que nos serments gardaient fermé. Je ne dirai pas que vous avez eu tort : je ne sais pas ce qu'il y a derrière. Je dirai que c'était à Ormeval de choisir, et pas à un voyageur. »
  / "You have opened what our oaths kept shut. I will not say you were wrong: I do not know what lies beyond. I will say it was for Ormeval to choose, and not for a traveller."
- **`thanks`** : « Six morceaux de cobalt, un wagonnet plein, et un juge qui n'a pas crié. Je prends les trois. Ormeval mangera cet hiver, et c'est un peu grâce à toi. »
  / "Six pieces of cobalt, a full cart, and a judge who didn't shout. I'll take all three. Ormeval will eat this winter, and that's partly down to you."

Les deux répliques finales se tiennent avant comme après les autres objectifs : les objectifs n'ont pas d'ordre (`chapitre-2-pistes.md` §2, point 5).

### B.5 Les objets à examiner (`Interact`), à poser dans les Mines de Sous-Racine

Format de `data/towns/*.json` (`props` : `id`, `look`, `name`, `text`). Les emplacements sont des suggestions.

| id | Pour | Où (suggestion) | `look` | Nom fr / en | Texte fr / en |
|---|---|---|---|---|---|
| `miners-passage` | les deux premières quêtes | profondeur 0, près de l'arrivée depuis `ancestors-2-4` | `stone` | Le boyau des mineurs / The miners' passage | Un boyau taillé au pic, à peine assez large pour un homme et sa lanterne. Les étais sont neufs, les coups de pic aussi. Mais à hauteur d'épaule, la paroi est ronde et lisse, comme si autre chose était passé là avant eux. / A passage hacked out with picks, barely wide enough for a man and his lantern. The props are new, and so are the pick marks. But at shoulder height the wall is round and smooth, as if something else had come through before them. |
| `boundary-stone` | `the-boundary-stone` | profondeur 1 | `stone` | Une borne gravée / A carved boundary stone | Une borne de granit, haute comme un genou, pareille à celles qu'on voit au bord des champs. Deux mots dessus : « Nous tiendrons. » Les traces de pic des mineurs passent à côté sans s'arrêter. / A granite marker, knee-high, like the ones you see at the edge of a field. Two words on it: 'We shall hold.' The miners' pick marks go straight past it without stopping. |
| `worm-tunnel-1` | `follow-the-worms` | profondeur 1 | `stone` | Un tunnel de ver / A worm tunnel | Un tunnel parfaitement rond, lisse comme l'intérieur d'une flûte. Les marques de craie des mineurs s'arrêtent au premier coude ; le tunnel, lui, continue de descendre. / A perfectly round tunnel, smooth as the inside of a flute. The miners' chalk marks stop at the first bend; the tunnel keeps going down. |
| `worm-tunnel-2` | `follow-the-worms` | profondeur 2 | `stone` | Un croisement de tunnels / A crossing of tunnels | Trois tunnels se croisent ici, chacun dans sa direction. Du gravier frais au sol : un ver est passé ce matin. / Three tunnels cross here, each going its own way. Fresh gravel on the ground: a worm came through this morning. |

Aucun texte ne montre de couches de roche, ni ce qu'il y a sous la montagne : c'est le terrain de `moved-mountains`.

### B.6 Les récompenses secondaires (à trancher)

L'idée : **Bérold soigne, Berthe nourrit**. Bérold veut préserver, et il donne de quoi tenir (la protection de Dhoram). Berthe veut exploiter, et elle paie en vivres, comme ses salaires.

| Quête | Bérold : élixirs | | Quête | Berthe : pains |
|---|---|---|---|---|
| `wall-the-passage` (46) | `superior-healing-elixir` ×2 | | `widen-the-passage` (46) | `sorghum-flatbread` ×4 (le « mange » du remerciement) |
| `the-boundary-stone` (48) | `superior-healing-elixir` ×3 | | `follow-the-worms` (48) | `barley-bread` ×3 |
| `stone-for-stone` (51) | `supreme-healing-elixir` ×2 | | `the-first-cart` (51) | `millet-flatbread` ×3 (« Ormeval mangera cet hiver ») |

Variante pour Berthe à 51 : `gold-ore` ×3, sa part du filon. L'expérience et les Éclats sont les mêmes des deux côtés : c'est au game designer de les fixer.

### B.7 [P] Ce qui demanderait du code (facultatif)

1. **Des salutations qui changent selon le parti pris** (bible §12, « le choix change les répliques suivantes »). Il faudrait une condition « quête faite » sur la réplique `hello`. Proposition, une fois la quête 46 faite :
   - Bérold, si le joueur a pris son parti : « Voyageur. Vous êtes entré dans la forêt d'Ormeval, où l'on tient parole. Vous avez tenu la vôtre. » / "Traveller. You came into the forest of Ormeval, where people keep their word. You have kept yours."
   - Bérold, si le joueur a pris le parti de Berthe : « Voyageur. Le boyau est ouvert, et je tiens toujours parole. Ce sont deux choses vraies. » / "Traveller. The passage is open, and I still keep my word. Both things are true."
   - Berthe, si le joueur a pris son parti : « Ah, toi. Le wagonnet roule, fais vite quand même. Tu veux quoi ? » / "Ah, you. The cart's rolling; make it quick all the same. What do you want?"
   - Berthe, si le joueur a pris le parti de Bérold : « Fais vite : mes gars creusent ailleurs, à cause d'un mur que tu connais. Tu veux quoi ? » / "Make it quick: my lads are digging somewhere else, thanks to a wall you know all about. What do you want?"
2. **Un mur ou un passage élargi visibles** : seulement si le moteur sait montrer un objet à un personnage selon ses quêtes faites. Sinon, on n'en pose pas : un mur et un passage élargi visibles par tout le monde se contrediraient.

## Remarques (rien n'est corrigé ailleurs que dans ce document)

1. **`keep-or-dig` change de nature.** Dans `chapitre-2-pistes.md` §5, c'est l'étape 5 du fil principal (« choix de dialogue »). Elle devient un fil annexe de trois quêtes. `moved-mountains` (50) ne doit donc pas demander une quête de ce fil : elle doit suivre `roots-of-the-mother-tree`, sinon un joueur qui n'a pas fait le fil serait bloqué (bible §17).
2. **L'id `keep-or-dig` n'est plus utilisé.** Les deux premières quêtes jouent son rôle. Il ne figure nulle part dans `data/`, donc rien à retirer.
3. **La borne de Bérold [P]** est une borne d'Ormeval, citée dans ses registres. Elle ne précède pas Ormeval et ne dit rien de ceux qui ont juré. Elle porte les deux mots déjà connus.
4. **Dans `ringing-seam-cleared`, Bérold « entendait des mots » dans le filon**, avant la lecture du serment à 58. Cette phrase est remplacée (A.2).
5. **Pas de prénom nouveau, ni Margaux ni Camille.** Les maçons et le wagonnet sont anonymes. Seuls Berthe et Bérold parlent.

## Questions à Charles

1. **Le poisson de niveau 50** : Lotte / Burbot (ma recommandation), ou Ombre de rivière en gardant Grayling ?
2. **Annoncer le choix avant** : une ligne « Ce choix est définitif » sous l'offre, ou une confirmation (B.2) ? Ou seulement la phrase affichée une fois le choix fait ?
3. **L'abandon** : je propose que le parti soit fixé dès qu'on accepte. Si le joueur abandonne la quête, il peut la reprendre, mais la quête rivale reste fermée. D'accord ?
4. **Les salutations selon le parti pris** (B.7, point 1) : est-ce qu'on les veut maintenant (une condition sur une réplique), ou plus tard ?
5. **`moved-mountains`** : prérequis `roots-of-the-mother-tree`, indépendamment du fil annexe (remarque 1) ?
