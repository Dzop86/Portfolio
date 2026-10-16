# Osmose : les textes de la v5 (Clairval en douze cartes et le fil des 45 quêtes)

> Proposition du scénariste, à relire par Charles avant d'être reportée dans les données. Elle répond au §5 ter.5 de `suite-apres-d69.md`.
> **Convention :** **[P]** = proposition, absente de la bible et du jeu. Le canon de la bible n'est pas modifié. Les identifiants des 21 quêtes existantes sont gardés ; ceux des quêtes nouvelles sans identifiant dans le §5 ter.4 sont proposés ici **[P]**.
> **Le héros n'a pas de genre dans les textes.** Aucun accord au masculin ou au féminin ne porte sur lui (« tu saignes ? » plutôt que « tu es blessé ? »). À garder en relisant.
> **Les touches citées** : I (sac), C (caractéristiques), J (métiers), Q (journal). Ce sont celles du jeu aujourd'hui.

## Sommaire
1. Les personnages : fiches
2. Les douze cartes du village
3. Les répliques de base des habitants
4. Les quêtes de Clairval (le tuto, C1 à C9)
5. Les quêtes du Bois de Clairval
6. Les quêtes du Bois moisi
7. Les quêtes du Lac des Joncs
8. Les quêtes des Champs de Clairval
9. Les objets à examiner (`Interact`) et les objets de quête
10. Ce qui demande du code ou une décision (pour le dev et le game designer)
11. Points de canon et questions pour Charles

**Lire un bloc de quête :**
- **Offre** : ce que dit le donneur quand il propose la quête.
- **Objectifs** : l'intitulé de chaque objectif dans le journal.
- **Réplique de la cible** : ce que dit l'habitant d'un objectif « parler à ». Aujourd'hui, la cible garde sa conversation habituelle (limite de T56). Ces répliques demandent donc un petit ajout au moteur (§10).
- **Remerciement** : ce que dit le donneur quand on rend la quête (le moteur rend la quête au donneur).

---

## 1. Les personnages : fiches

### 1.1 Le tutoiement
| Personnage | Au héros | Pourquoi |
|---|---|---|
| Aubin, Odalric | vous | le guide poli, le copiste cérémonieux |
| Ysaline | vous, puis tu après « La pierre qui murmurait » | le passage marque la confiance gagnée |
| Tous les autres | tu | un village où l'on se connaît |

En anglais, « you » partout ; la distance d'Aubin et d'Odalric passe par des phrases plus soignées.

### 1.2 Les habitants qui existent (rappel de leur voix, rien ne change dans le canon)
- **Aubin**, le guide (place).
  - *Ton :* chaleureux, rassurant, des formules toutes faites.
  - *Trait :* il touche la vieille pierre « pour la chance » chaque matin et apprend le geste aux nouveaux (trame §3).
  - *Ce qu'il sait :* il a fait le rêve du cercle qui relie quatre lumières, mais refuse d'en parler. Il voit la crypte du Bois moisi depuis toujours sans l'avoir jamais « remarquée ». Sa grand-mère fredonnait la berceuse du Lac **[P]**.
  - *Évolution :* du déni (« On ne lui pose pas de questions ») aux larmes de « Le nom rendu ».
- **Rose**, la marchande (marché).
  - *Ton :* gouailleur, des chiffres plein la bouche.
  - *Trait :* elle revend tout, même les cartes de la région qui ne concordent plus (trame §3).
  - *Ce qu'elle sait :* rien du mystère, mais elle entend tout. Elle tient le fil de la ville côtière qui oublie (arc de Névara).
- **Garance**, la maîtresse d'armes (cour d'armes, porte de l'Ouest).
  - *Ton :* sec, à l'impératif, un humour pince-sans-rire.
  - *Trait :* elle tient le compte de tout ce que ses élèves font.
  - *Ce qu'elle sait :* elle a vu l'arme d'entraînement réagir à la colère de la recrue. Elle a fait le même rêve qu'Aubin. Elle tient le fil de la forge interdite (arc de Kaor).
- **Ysaline**, l'érudite (place, puis elle voyage).
  - *Ton :* curieux, précis, enthousiaste ; elle pose des questions et prend des notes.
  - *Trait :* un carnet trop plein, des doigts couverts de mine de plomb.
  - *Ce qu'elle sait :* elle comprend avant les autres que les signes sont liés, mais elle **interprète**. Elle croit longtemps que les pierres sont des avertissements, et elle a parfois tort.

### 1.3 Les nouveaux habitants du village
| Nom | Rôle, carte | Ton | Trait | Lien avec le fil de la pierre, ce qu'il sait |
|---|---|---|---|---|
| **Hortense** | la charpentière, `clairval-0-0` (tuto bûcheron) | bourru, elle compte tout en pieux | elle connaît l'âge de chaque pieu de la palissade | depuis que la pierre s'est fendue, ses pieux frais se fendent en séchant en étoile à cinq branches ; elle met ça sur le compte du bois |
| **Firmin** | le meunier, `clairval-2-0` (donne C4) | bavard, peureux, il jure « bon sang de farine ! » | il a peur des rats et compte ses sacs à voix haute | la nuit de la fissure, la roue du moulin a tourné à l'envers pendant une heure ; il accuse les enfants |
| **Léonie** | la fermière du village, `clairval-1-2` (le grenier) | doux, un peu rêveur, elle parle à ses poules | sa poule Pétronille la suit partout | elle a fait le rêve du cercle, comme Aubin et Garance, et elle le raconte volontiers, elle |
| **Odalric** | le copiste du registre, `clairval-2-2` (chapelle) | posé, phrases complètes, il vouvoie | des taches d'encre verte aux doigts (son encre d'ortie) | il tient le registre depuis trente ans ; il y découvre des silences (une page grattée, un nom lavé) et croit d'abord à des copistes négligents. Nom repris de la trame ; « Frère » retiré **[P]** (voir §11) |
| **Gaspard** | le mineur retraité, `clairval-3-0` (tuto mineur, sans quête) | vieux, un peu sourd, il en rit | il parle à la roche | depuis peu, la carrière « sonne » quand on la frappe, comme une cloche au loin |
| **Anselme** | le forgeron, `clairval-3-1` | peu de mots, des images de marteau | il ne lève jamais les yeux de l'enclume | Garance lui a apporté l'arme qui vibre ; il n'y a rien trouvé (« C'est la main qui ne l'est pas ») et il se trompe |
| **Perrine** | la tailleuse, `clairval-3-1` | pointilleux, elle mesure tout | un mètre ruban autour du cou | les peaux de loup qu'on lui apporte ont des poils raides comme de l'écorce |
| **Sidonie** | l'alchimiste, `clairval-3-1` | parle vite, s'enthousiasme | des sourcils toujours un peu roussis | les spores du Bois moisi restent côte à côte dans ses fioles sans jamais se mêler (le fil « relation ≠ fusion », montré) |
| **Basile** | le boulanger, `clairval-3-1` | endormi, gentil | debout depuis trois heures du matin | pour la fête de la pierre, il fait un pain à quatre entailles ; « ma mère en faisait cinq, je crois ». Un souvenir qui ne tient pas |
| **Gédéon** | le serrurier, `clairval-3-1` | méfiant, pas de crédit | il porte toutes ses clefs sur lui | les serrures des donjons sont plus vieilles que le village ; c'est lui qui forge les clefs offertes par la chaîne principale |
| **Thibaut** | garde de la porte du Nord, `clairval-0-0` | jeune, bavard, il s'ennuie | il rêve de voir le Lac | il entend la nuit les grenouilles chanter « comme une berceuse » |
| **Mahaut** | garde de la porte de l'Ouest, `clairval-0-1` | vétérane, sèche | ancienne élève de Garance | les sangliers grattent la palissade la nuit, ce qu'ils n'avaient jamais fait |
| **Gautier** | garde de la porte du Sud, `clairval-0-2` | placide, blagueur | il garde une écurie sans chevaux (pour les montures, plus tard) | le vent des Champs sent le miel chaud |

Le second garde de la cour d'armes (porte de l'Ouest) reste sans nom : « Garde » / "Guard".

### 1.4 Les habitants des zones
| Nom | Rôle, lieu | Ton | Trait | Lien avec le fil, ce qu'il sait |
|---|---|---|---|---|
| **Mathurin** | le chef des bûcherons, campement, `woods-3-2` | lent, sentencieux | il respecte les arbres comme des anciens | son grand-père interdisait de couper dans le cercle de racines ; il obéit sans savoir pourquoi. Un serment que personne ne comprend (écho de Dhoram) |
| **Barnabé** | l'ermite des mares, Bois moisi (carte d'entrée à fixer) | phrases courtes et retournées, il parle aux champignons | quarante ans au milieu du mycélium | « Le mycélium, il relie. Il ne mélange pas. » Il est le premier à le dire, sans savoir ce que ça veut dire. Les champignons ne l'attaquent jamais (il les croit polis) |
| **Liane** | la passeuse, pontons du Lac | doux, elle chantonne, mélancolique | elle fredonne sans s'en rendre compte | elle chante la berceuse depuis toujours sans savoir qui la lui a apprise ; elle a repris la barque de « celle d'avant », une passeuse dont personne ne se rappelle le nom (trame §3) |
| **Églantine** | la fermière des Champs, sa ferme | vif, la langue bien pendue, elle parle météo | elle fait trois métiers et ne dort jamais | ses ruches chauffent, et les taupes creusent toutes vers la vieille haie que son grand-père défendait d'arracher |
| **Joachim** **[P]** | vieux pêcheur, pontons du Lac (« La berceuse… ») | bougon | cinquante ans sur le lac | pour lui, la berceuse est une chanson de pêche (un témoin qui se trompe, ou pas) |
| **Fleur** **[P]** | sa petite-fille, pontons du Lac | vif, enfantin | elle chante tout le temps | elle dit que les grenouilles lui ont appris la berceuse, et qu'elle parle de « quelqu'un qu'on a caché » |

**Le nom effacé [P] :** la passeuse oubliée s'appelle **Isaure**. C'était la grand-mère d'Aubin, mais personne ne le dit : seul son corps s'en souvient (« Le nom rendu »).

---

## 2. Les douze cartes du village
| Carte | fr | en |
|---|---|---|
| `clairval-0-0` | Les Vergers (porte du Nord) | The Orchards (North Gate) |
| `clairval-1-0` | Les Potagers | The Kitchen Gardens |
| `clairval-2-0` | Le Moulin de l'étang | The Pond Mill |
| `clairval-3-0` | La Petite Carrière | The Little Quarry |
| `clairval-0-1` | La Cour d'armes (porte de l'Ouest) | The Drill Yard (West Gate) |
| `clairval` | Clairval, place de la Pierre | Clairval, Stone Square |
| `clairval-2-1` | Le Marché | The Market |
| `clairval-3-1` | La Rue des artisans | Craftsmen's Row |
| `clairval-0-2` | L'Écurie (porte du Sud) | The Stables (South Gate) |
| `clairval-1-2` | Les Greniers | The Granaries |
| `clairval-2-2` | Le Clos de la chapelle | Chapel Close |
| `clairval-3-2` | Le Pré aux moutons | Sheep Meadow |

**Les portes** (noms des passages) :
- Porte du Nord, chemin du Lac / North Gate, the Lake Road ;
- Porte de l'Ouest, route du Bois / West Gate, the Woods Road ;
- Porte du Sud, chemin des Champs / South Gate, the Fields Road.

**La chapelle** s'appelle la chapelle des Quatre / the Chapel of the Four : on y honore les quatre Primordiaux, ce qui est cohérent avec la bible (« vénèrent leurs divinités »).

---

## 3. Les répliques de base des habitants

Une réplique d'accueil, puis une ou deux répliques sur le métier ou le village. Elles remplacent les textes de `guide.json`, `merchant.json`, `trainer.json` et `scholar.json`, qui parlent encore de la porte est et du marché au nord.

### Aubin (place)
- **Accueil :** « Bienvenue à Clairval ! Si vous cherchez quelqu'un, demandez-moi : je connais toutes les familles, et tous les chiens. » / "Welcome to Clairval! If you're looking for someone, ask me: I know every family here, and every dog."
- **Le village :** « La place est le cœur du village. Le marché est juste à l'est, les artisans au bout de la rue, et nos trois portes regardent toutes vers le Bois. » / "The square is the heart of the village. The market is just to the east, the craftsmen at the end of the street, and our three gates all look out on the woods."
- **La pierre :** « Chaque matin, je touche la pierre pour la chance. Tout le monde le fait, ici, depuis toujours. Essayez, ça ne coûte rien. » / "Every morning I touch the stone for luck. Everyone here does, and always has. Try it, it costs nothing."

### Rose (marché)
- **Accueil :** « L'étal est ouvert ! Je rachète ce que tu rapportes et je vends ce qui te manque. Le pain de Clairval, dix Éclats : c'est donné. » / "The stall is open! I buy what you bring back and sell what you're missing. Clairval bread, ten Éclats: a steal."
- **Le métier :** « Des voyageurs me vendent des cartes de la région. Trois cartes du même chemin, trois chemins différents. Je les revends quand même, hein. » / "Travellers sell me maps of the area. Three maps of the same road, three different roads. I sell them on anyway, mind."

### Garance (cour d'armes)
- **Accueil :** « Encore toi ? Bien. Celui qui revient est celui qui apprend. » / "You again? Good. The ones who come back are the ones who learn."
- **Le métier :** « Frappe avec ta tête, pas avec ta rage. Les armes, cette année, ont l'air de préférer la rage. » / "Strike with your head, not your temper. This year, weapons seem to prefer the temper."
- **Revanche** (après « Première leçon », relance le combat d'entraînement) : « Le mannequin ne se vexe jamais. Tu veux recommencer ? » / "The dummy never sulks. Another go?"

### Ysaline (place, ou chez le donneur de la zone)
- **Première rencontre** (gardée de `scholar.json`) : « Ah, un visage neuf. Je suis Ysaline : j'étudie les vieilles pierres de Clairval, et celle-ci plus que les autres. » / "Ah, a new face. I am Ysaline: I study the old stones of Clairval, and this one more than the others."
- **Ensuite :** « Te voilà. J'ai trois questions et pas une réponse : c'est une bonne journée. » / "There you are. Three questions and not a single answer: it's a good day."
- **La pierre :** « Une graine, une goutte, un marteau, une plume. Les anciens disent que ces signes protègent le village. Moi, je me demande ce qu'ils racontent. » / "A seed, a drop, a hammer, a feather. The elders say the signs protect the village. I wonder what they are telling us."

### Hortense, la charpentière (Vergers)
- « Attention aux pieux, ils sont frais. » / "Mind the stakes, they're fresh."
- « Mille deux cents pieux, trois portes et pas un trou. Enfin, pas un trou que je n'aie pas vu. » / "Twelve hundred stakes, three gates and not one gap. Well, not one gap I haven't seen."
- « Un chêne des vergers, une hache et un peu de dos : voilà comment on garde un village. » / "An oak from the orchard, an axe and a strong back: that's how you keep a village safe."

### Firmin, le meunier (Moulin)
- « Ah ! Tu m'as fait peur. Tout me fait peur, depuis les rats. » / "Ah! You gave me a fright. Everything frightens me since the rats."
- « Le moulin tourne avec l'étang. Sauf la nuit où la pierre s'est fendue : la roue a tourné à l'envers pendant une heure. Les enfants, sûrement. » / "The mill runs on the pond. Except the night the stone cracked: the wheel turned backwards for a whole hour. The children, I'm sure."
- « L'étang est plein de goujons. Ils ne mordent que pour les gens patients. » / "The pond is full of gudgeon. They only bite for patient folk."

### Léonie, la fermière (Greniers)
- « Bonjour ! Attention à Pétronille, c'est ma poule la plus curieuse. » / "Hello! Mind Pétronille, she's my nosiest hen."
- « J'ai rêvé d'un cercle qui reliait quatre lumières. Aubin et Garance aussi, mais eux n'en parlent pas. Moi, je trouvais ça joli. » / "I dreamt of a circle joining four lights. So did Aubin and Garance, but they won't talk about it. I thought it was lovely."
- « Le grenier garde mon grain et la farine de Firmin. Quand il n'y a pas de rats. » / "The loft keeps my grain and Firmin's flour. When there aren't rats in it."

### Odalric, le copiste (Clos de la chapelle)
- « Bonjour. Parlez doucement, je vous prie : l'encre sèche. » / "Good day. Speak softly, if you please: the ink is drying."
- « Le registre ne ment pas. Il lui arrive seulement de se taire. » / "The registry does not lie. It merely falls silent at times."
- « Naissances, mariages, morts, récoltes : Clairval tient dans quatre gros volumes. Et dans le cimetière, bien sûr. » / "Births, marriages, deaths, harvests: Clairval fits into four thick volumes. And into the churchyard, of course."

### Gaspard, le mineur retraité (Petite Carrière)
- « Hein ? Parle plus fort, la roche m'a mangé les oreilles. » / "Eh? Speak up, the rock ate my ears."
- « Le cuivre, c'est la roche verte, là. Approche-toi et frappe ; tout est dans ta fenêtre des métiers (J). » / "Copper's the green rock, there. Walk up and strike it; it's all in your trades window (J)."
- « Depuis peu, la carrière sonne quand on la frappe, comme une cloche au loin. Cinquante ans de mine, et je n'avais jamais entendu ça. » / "Lately the quarry rings when you strike it, like a bell far away. Fifty years of mining and I'd never heard the like."

### Anselme, le forgeron (Rue des artisans)
- « Hm. La forge est chaude. Parle vite. » / "Hm. The forge is hot. Talk fast."
- « Du métal, du bois, de la patience. Le reste, c'est le marteau. » / "Metal, wood, patience. The rest is the hammer."
- « Garance m'a apporté l'arme qui vibre. Le métal est bon. C'est la main qui ne l'est pas. » / "Garance brought me the humming weapon. The metal is sound. It's the hand that isn't."

### Perrine, la tailleuse (Rue des artisans)
- « Ne bouge pas, je prends tes mesures. Voilà. Tu peux bouger. » / "Hold still, I'm taking your measurements. There. You may move."
- « Lin, cuir, peaux : je couds tout ce qui se coud, et droit. Le travers, je le laisse aux zombies. » / "Flax, leather, pelts: I sew anything that can be sewn, and straight. Crooked I leave to the zombies."
- « Les peaux de loup qu'on m'apporte ont des poils raides comme de l'écorce. Je les couds quand même, mais ça m'agace. » / "The wolf pelts I'm brought have hairs as stiff as bark. I sew them anyway, but it irks me."

### Sidonie, l'alchimiste (Rue des artisans)
- « Ne touche à rien ! Sauf à ça. Non, pas à ça non plus. » / "Don't touch anything! Except that. No, not that either."
- « Une plante, de l'eau, du feu sous l'alambic et beaucoup de chance : c'est ça, l'alchimie. Surtout la chance. » / "A plant, some water, a flame under the still and plenty of luck: that's alchemy. Mostly the luck."
- « Les spores du Bois moisi restent côte à côte dans la fiole, sans jamais se mêler. Je n'avais jamais vu une poudre aussi bien élevée. » / "Spores from the Mouldy Woods sit side by side in the flask and never mix. I've never seen such well-mannered powder."

### Basile, le boulanger (Rue des artisans)
- « Mmh, bonjour. Je suis debout depuis trois heures, sois gentil. » / "Mmh, morning. I've been up since three, be kind."
- « Des céréales, une herbe ou un poisson, et le four fait le reste. Le pain se mange hors combat : c'est lui qui te remet debout. » / "Grain, a herb or a fish, and the oven does the rest. Bread is eaten outside fights: it's what puts you back on your feet."
- « Pour la fête de la pierre, on fait un pain à quatre entailles. Ma mère en faisait cinq, je crois. Ou quatre. Quatre, sûrement. » / "For the stone festival we bake a loaf with four cuts. My mother made five, I think. Or four. Four, surely."

### Gédéon, le serrurier (Rue des artisans)
- « Tu viens pour une clef ? Tout le monde vient pour une clef. » / "Here for a key? Everyone's here for a key."
- « Apporte les ressources, je te fais la clef d'un donjon. Une clef, une porte. Pas de crédit. » / "Bring the materials and I'll make you a dungeon key. One key, one door. No credit."
- « Les serrures des donjons sont plus vieilles que le village. Celui qui les a posées voulait qu'on n'entre pas. Moi, je veux qu'on ressorte. » / "The dungeon locks are older than the village. Whoever fitted them wanted nobody to get in. I want folk to get out."

### Thibaut, garde de la porte du Nord
- « Porte du Nord, chemin du Lac ! Enfin, le Bois d'abord, le Lac après. » / "North Gate, the Lake Road! Well, the woods first, then the lake."
- « La nuit, quand le vent vient du nord, on entend les grenouilles chanter. On dirait une berceuse. Mahaut dit que j'ai trop d'imagination. » / "At night, when the wind's from the north, you can hear the frogs singing. Sounds like a lullaby. Mahaut says I've too much imagination."

### Mahaut, garde de la porte de l'Ouest
- « Halte. Ah, c'est toi. Passe, et reviens en un seul morceau. » / "Halt. Oh, it's you. Go on, and come back in one piece."
- « J'ai été l'élève de Garance, il y a dix ans. Elle crie toujours aussi fort ? Bien. » / "I was Garance's pupil ten years ago. Does she still shout that loud? Good."
- « Les sangliers grattent la palissade, la nuit. Ils n'avaient jamais fait ça. » / "The boars scratch at the palisade at night. They never used to."

### Gautier, garde de la porte du Sud
- « Porte du Sud, chemin des Champs. Et l'écurie, si tu cherches un cheval : il n'y en a pas encore, mais elle est prête. » / "South Gate, the Fields Road. And the stables, if you're after a horse: there aren't any yet, but they're ready."
- « Quand le vent vient des Champs, il sent le miel chaud. Le miel, ça ne devrait pas sentir chaud. » / "When the wind blows from the fields, it smells of warm honey. Honey shouldn't smell warm."

### Mathurin, le chef des bûcherons (campement)
- « Doucement. Les arbres écoutent, et moi aussi. » / "Easy. The trees are listening, and so am I."
- « On coupe ce qui doit l'être, on laisse ce qui doit rester. Le reste, c'est du bavardage. » / "You cut what needs cutting and leave what needs to stay. The rest is chatter."
- « Ce bois est plus vieux que Clairval. Il se souvient de choses que nous, on a laissé tomber. » / "These woods are older than Clairval. They remember things we let fall."

### Barnabé, l'ermite des mares
- « Chut. Ils dorment, les champignons. Toi aussi, tu devrais. » / "Hush. They're sleeping, the mushrooms. You should too."
- « Quarante ans que je vis ici. Jamais ils ne m'ont mordu. Polis, ils sont. » / "Forty years I've lived here. Never once have they bitten me. Polite, they are."
- « Le mycélium, il relie. Il ne mange pas, il ne mélange pas. Il tient la main, c'est tout. » / "The mycelium, it links. It doesn't eat, it doesn't mix. It holds hands, that's all."

### Liane, la passeuse (pontons)
- « Bonjour ! Attention aux planches, la troisième est pourrie. » / "Hello! Mind the boards, the third one's rotten."
- « Je suis passeuse, comme celle d'avant moi. Tout le monde dit "celle d'avant", comme si elle n'avait pas de nom. » / "I'm the ferrywoman, like the one before me. Everyone says 'the one before', as if she had no name."
- (Elle fredonne.) « Pardon, je chante sans m'en rendre compte. Tout le monde chante ça, ici. » / (She hums.) "Sorry, I sing without noticing. Everyone sings that round here."

### Églantine, la fermière des Champs
- « Ah, enfin quelqu'un ! Le vent tourne, la pluie arrive et mes ruches chauffent. Bonjour. » / "Ah, someone at last! The wind's turning, the rain's coming and my hives are hot. Hello."
- « Du blé, des fleurs pour les abeilles, du miel pour Rose : la ferme, c'est trois métiers et pas de sommeil. » / "Wheat, flowers for the bees, honey for Rose: a farm is three trades and no sleep."
- « Mon grand-père défendait d'arracher la vieille haie. Remarque, il défendait tout. » / "My grandfather forbade anyone to pull up the old hedge. Mind you, he forbade everything."

### Joachim et Fleur (pontons du Lac)
- **Joachim :** « Hm. Le poisson ne mord pas, alors je parle. Qu'est-ce que tu veux ? » / "Hm. The fish aren't biting, so I'm talking. What do you want?" ; « Cinquante ans sur ce lac. Les grenouilles ne chantaient pas comme ça, avant. » / "Fifty years on this lake. The frogs never sang like that before."
- **Fleur :** « Tu sais chanter ? Moi oui. Les grenouilles m'ont appris. » / "Can you sing? I can. The frogs taught me." ; « Grand-père dit que les grenouilles ne savent pas chanter. Il a tort, mais faut pas lui dire. » / "Grandad says frogs can't sing. He's wrong, but don't tell him."

---

## 4. Les quêtes de Clairval (le tuto, niveaux 1 à 5)

Une quête = un système appris. Chaque remerciement dit où aller ensuite.

### `welcome` (C1, niv. 1, tuto) : marcher, changer de carte, parler
- **Donneur :** Aubin (place).
- **Titre :** Bienvenue à Clairval / Welcome to Clairval
- **Offre :**
  - fr : « Bienvenue à Clairval ! Ici, tout le monde commence par voir Garance, notre maîtresse d'armes. Elle vous attend dans la cour d'armes, à la porte de l'Ouest : quittez la place par l'ouest, vous ne pouvez pas la manquer. »
  - en : "Welcome to Clairval! Everyone here starts by seeing Garance, our weapons master. She's waiting for you in the drill yard by the West Gate: leave the square heading west, you can't miss her."
- **Objectif :** Parler à Garance (cour d'armes, porte de l'Ouest) / Talk to Garance (drill yard, West Gate)
- **Réplique de la cible (Garance) :**
  - fr : « La nouvelle recrue ? Aubin t'envoie, bien. Ici, on apprend à tenir debout avant d'apprendre à courir. »
  - en : "The new recruit? Aubin sent you, good. Here we learn to stand before we learn to run."
- **Remerciement (Aubin) :**
  - fr : « Vous avez trouvé Garance, et elle ne vous a pas mordu. C'est bon signe ! Retournez la voir : elle a une leçon pour vous. »
  - en : "You found Garance, and she didn't bite. That's a good sign! Go back to her: she has a lesson for you."
  - *Si la quête peut se rendre à la cible (§10, point 2), dire plutôt (Garance) :* « Aubin t'a bien guidé. Maintenant, on travaille. » / "Aubin pointed you the right way. Now we work."

### `first-lesson` (C2, niv. 1, gardée) : le combat, la frise, les sorts
- **Donneur :** Garance.
- **Titre :** Première leçon / First Lesson
- **Offre :**
  - fr : « Mon élève a animé ce mannequin pour qu'il rende les coups. Bats-les tous les deux. La frise, en haut, te dit qui joue. Tes PA lancent tes sorts, tes PM te font marcher. »
  - en : "My pupil has bewitched this dummy so it hits back. Beat them both. The strip at the top shows whose turn it is. Your AP cast your spells, your MP let you walk."
- **Objectif :** Gagner la leçon contre le mannequin et l'élève de Garance / Win the lesson against Garance's dummy and pupil
- **Remerciement :**
  - fr : « Pas mal. Mais l'arme que je t'ai prêtée vibrait, à la fin, quand tu t'énervais. Ça, je ne l'avais jamais vu. Tiens, mon insigne, et cet anneau : tu vas en avoir besoin. »
  - en : "Not bad. But the weapon I lent you was humming at the end, when you lost your temper. I've never seen that before. Here, my badge, and this ring: you'll need it."
- *L'anomalie canon « une arme réagit à une émotion » est montrée ici, jamais expliquée (trame, indice 2).*

### `well-equipped` (C3, niv. 1, tuto, nouvelle) : l'inventaire, l'équipement, les caractéristiques
- **Donneur :** Garance.
- **Titre :** Bien équipé / Kitted Out
- **Offre :**
  - fr : « Un anneau au fond du sac ne protège personne. Ouvre ton sac (I) et passe-le au doigt. Puis regarde tes caractéristiques (C) : tu as des points à placer, place-les selon ta façon de te battre. »
  - en : "A ring at the bottom of a bag protects nobody. Open your bag (I) and put it on. Then look at your characteristics (C): you have points to spend, so spend them to suit the way you fight."
- **Objectif :** Porter l'anneau de cuivre / Wear the copper ring
- **Remerciement :**
  - fr : « Là, tu ressembles à quelqu'un. Prends ceci aussi, c'est ce qui va le mieux à ta façon de te battre. Et dis donc : tu passes d'un élément à l'autre sans qu'ils se marchent dessus. Chez la plupart des gens, l'un étouffe l'autre. Bon, Firmin a des rats, et Hortense manque de bras : va les voir. »
  - en : "Now you look like somebody. Take this too, it suits the way you fight. And I must say: you go from one element to another without them treading on each other. In most people, one smothers the other. Right, Firmin has rats and Hortense needs a hand: go and see them."
- *Le seul indice sur le héros aux quatre éléments dans le tuto : une remarque de Garance, que personne ne reprend.*

### `cellar-rats` (C4, niv. 2, tuto, nouvelle) : le deck, le butin de quête, la vie qui se garde
- **Donneur :** Firmin, le meunier (Moulin de l'étang).
- **Titre :** Les rats du grenier / Rats in the Loft
- **Offre :**
  - fr : « Des rats ! Dans le grenier de Léonie, aux Greniers, là où je range ma farine ! Choisis bien tes sorts avant de monter : Garance dit toujours que ça se prépare avant, pas pendant. Et rapporte-moi leurs queues, que j'en sois sûr ! »
  - en : "Rats! In Léonie's loft, over at the Granaries, where I keep my flour! Choose your spells carefully before you go up: Garance always says you prepare before, not during. And bring me their tails, so I know for sure!"
- **Objectifs :**
  - Ouvrir la trappe du grenier et chasser les rats (Greniers) / Open the loft hatch and drive out the rats (Granaries)
  - Rapporter 3 queues de rat / Bring 3 rat tails
- **Indice dans le journal :** « Ton deck se compose dans l'écran des sorts : douze sorts au plus. » / "Your deck is put together on the spells screen: twelve spells at most."
- **Remerciement :**
  - fr : « Trois queues ! Bon sang de farine, tu saignes ? Ta vie reviendra plus vite ici qu'en plein bois, ou mange un morceau. Tiens, du pain, et la ceinture de mon père : il la portait pour soulever les sacs. »
  - en : "Three tails! Flour and thunder, you're bleeding! Your life comes back faster here than out in the woods, or have a bite to eat. Here's some bread, and my father's belt: he wore it to lift the sacks."

### `palisade-stakes` (C5, niv. 2, tuto, nouvelle) : la récolte, la fenêtre Métiers, un atelier
- **Donneur :** Hortense, la charpentière (Vergers).
- **Titre :** Des pieux pour la palissade / Stakes for the Palisade
- **Offre :**
  - fr : « La palissade, c'est mille deux cents pieux, et il en pourrit un par semaine. Coupe six chênes dans les vergers, puis taille un pieu à la forge, rue des artisans ; prends deux cuivres à la carrière pour le ferrer. Ouvre ta fenêtre des métiers (J) : tout y est écrit. »
  - en : "The palisade is twelve hundred stakes, and one rots every week. Cut six oaks in the orchard, then shape a stake at the forge on Craftsmen's Row; get two copper from the quarry to shoe it. Open your trades window (J): it's all written there."
- **Objectifs :**
  - Rapporter 6 chênes / Bring 6 oak
  - Tailler un pieu de palissade à la forge et le rapporter / Shape a palisade stake at the forge and bring it
- **Remerciement :**
  - fr : « Droit, solide, bien taillé. Tu vois les pieux qui sèchent, là ? Ils se fendent en étoile, cinq branches. Le bois fait ce qu'il veut, cette année. »
  - en : "Straight, solid, well shaped. See the stakes drying over there? They split into stars, five points. The wood does as it pleases this year."

### `roses-stall` (C6, niv. 3, tuto, nouvelle) : vendre, acheter, manger hors combat
- **Donneur :** Rose (Marché).
- **Titre :** L'étal de Rose / Rose's Stall
- **Offre :**
  - fr : « Tout le monde veut du lin pour ses chemises, et je n'en ai plus un brin : cueille-m'en quatre aux potagers. Et pour apprendre, achète un pain à mon étal, puis rends-le-moi. Oui, je sais. Le commerce, c'est ça. »
  - en : "Everyone wants flax for their shirts and I haven't a thread left: pick me four in the kitchen gardens. And to learn, buy a loaf at my stall, then give it back to me. Yes, I know. That's trade."
- **Objectifs :**
  - Rapporter 4 lins sauvages / Bring 4 wild flax
  - Acheter un pain à l'étal de Rose et le lui rapporter / Buy a loaf at Rose's stall and bring it to her
- **Remerciement :**
  - fr : « Tu as payé dix Éclats un pain que je reprends : tu iras loin. Tiens, trois vrais pains. Mange-les hors combat, quand ta vie est basse. Et Ysaline, sur la place, cherche quelqu'un qui l'écoute. »
  - en : "You paid ten Éclats for a loaf I'm taking back: you'll go far. Here, three real loaves. Eat them outside fights, when your life runs low. And Ysaline, on the square, is looking for someone who'll listen."

### `whispering-stone` (C7, niv. 3, principale, gardée) : les indices, le journal ; le début du fil
- **Donneur :** Ysaline (place). Elle vouvoie encore.
- **Titre :** La pierre qui murmurait / The Whispering Stone
- **Offre :**
  - fr : « Cette nuit, la vieille pierre a montré un cinquième signe que personne n'avait jamais vu, puis elle s'est fendue. Les anciens disent que ce n'est rien. Regardez-la vous-même, puis demandez à Aubin et à Odalric, le copiste, ce qu'ils en savent. On ne m'écoute pas ; vous, peut-être. »
  - en : "Last night the old stone showed a fifth sign nobody had ever seen, then it cracked. The elders say it's nothing. Look at it yourself, then ask Aubin and Odalric the copyist what they know. They won't listen to me; perhaps they will to you."
- **Objectifs :**
  - Examiner la vieille pierre (place) / Examine the old stone (square)
  - Interroger Aubin / Question Aubin
  - Interroger Odalric, le copiste (Clos de la chapelle) / Question Odalric the copyist (Chapel Close)
- **Réplique de la cible (Aubin) :**
  - fr : « On touche la pierre pour la chance. On ne lui pose pas de questions. … Bon. J'ai rêvé d'un cercle qui reliait quatre lumières. Un rêve, c'est un rêve. »
  - en : "We touch the stone for luck. We don't ask it questions. … All right. I dreamt of a circle joining four lights. A dream is a dream."
- **Réplique de la cible (Odalric) :**
  - fr : « La pierre est dans le registre depuis la fondation, avec quatre signes. Mais la page d'avant a été grattée, proprement, par quelqu'un qui savait tenir un canif. »
  - en : "The stone has been in the registry since the founding, with four signs. But the page before it was scraped off, neatly, by someone who knew how to hold a penknife."
- **Remerciement (Ysaline passe au tu) :**
  - fr : « Un rêve partagé, une page grattée, une face polie par des siècles de mains… Ou quelqu'un a voulu qu'on oublie, ou tout le monde a oublié en même temps. Je ne sais pas ce qui est pire. Merci : tu m'as crue. »
  - en : "A shared dream, a scraped page, a face worn smooth by centuries of hands… Either someone wanted us to forget, or everyone forgot at once. I don't know which is worse. Thank you: you believed me."

### `the-registry` (C8, niv. 4, principale, nouvelle) : `Interact` sur une autre carte, l'herboriste
- **Donneur :** Ysaline.
- **Titre :** Ce que le registre tait / What the Registry Leaves Out
- **Offre :**
  - fr : « Odalric dit que deux tombes du cimetière sont plus vieilles que le village lui-même. C'est impossible, donc c'est intéressant. Va les examiner, et cueille trois orties aux potagers : il en fait son encre, et il voudra recopier ce que tu trouveras. »
  - en : "Odalric says two graves in the churchyard are older than the village itself. That's impossible, so it's interesting. Go and examine them, and pick three nettles in the kitchen gardens: he makes his ink from them, and he'll want to copy whatever you find."
- **Objectifs :**
  - Examiner les tombes marquées du cimetière (0/2) / Examine the marked graves in the churchyard (0/2)
  - Rapporter 3 orties / Bring 3 nettles
- **Remerciement :**
  - fr : « Plus vieilles que Clairval, et un signe gratté avec soin… On n'efface pas avec soin ce qu'on a oublié. On efface ce dont on se souvient trop bien. Garance voulait te voir, je crois : les sangliers s'approchent. »
  - en : "Older than Clairval, and a sign carefully scraped away… You don't carefully erase what you've forgotten. You erase what you remember too well. Garance wanted to see you, I think: the boars are coming closer."

### `edge-of-the-woods` (C9, niv. 4-5, gardée, donneur changé : Garance) : le premier vrai combat dehors ; ouvre le Bois
- **Donneur :** Garance.
- **Titre :** La lisière / The Edge of the Woods
- **Offre :**
  - fr : « Le grenier, la leçon, et tu survis aux questions d'Ysaline : il est temps. Derrière la porte de l'Ouest, des sangliers grattent la palissade la nuit. Chasses-en trois, et rapporte-moi deux poignées de soies, que je sache que ce n'était pas de la chance. »
  - en : "The loft, the lesson, and you survive Ysaline's questions: it's time. Beyond the West Gate, boars scratch at the palisade at night. Hunt three and bring me two handfuls of bristles, so I know it wasn't luck."
- **Objectifs :**
  - Vaincre 3 sangliers (derrière la porte de l'Ouest) / Defeat 3 boars (beyond the West Gate)
  - Rapporter 2 soies de sanglier / Bring 2 boar bristles
- **Remerciement (les mots de Garance qui ouvre la porte de l'Ouest) :**
  - fr : « Deux poignées, et pas une égratignure de trop. À partir d'aujourd'hui, Mahaut ne te demandera plus où tu vas : la porte de l'Ouest est à toi. Va voir Ysaline, elle ne parle que de loups depuis ce matin. »
  - en : "Two handfuls, and not one scratch too many. From today, Mahaut won't ask where you're going: the West Gate is yours. Go and see Ysaline, she's talked of nothing but wolves since this morning."

---

## 5. Les quêtes du Bois de Clairval (niveaux 5 à 10)

**Le donneur de la zone :** Mathurin, au campement des bûcherons (`woods-3-2`). Ysaline donne les quêtes principales, sauf « Quatre signes », relayée par Mathurin.

### `restless-beasts` (niv. 5, principale, gardée)
- **Donneur :** Ysaline.
- **Titre :** Les bêtes inquiètes / The Restless Beasts
- **Offre :**
  - fr : « Les bûcherons disent que les loups ne fuient plus : ils tournent en rond, toujours au même endroit. Chasses-en cinq et rapporte-moi trois crocs, s'ils portent une marque. Regarde aussi les troncs le long de leur passage : les griffes laissent des traces. »
  - en : "The woodcutters say the wolves no longer run: they go round in circles, always in the same spot. Hunt five and bring me three fangs, if any of them carry a mark. And look at the trunks along their path: claws leave traces."
- **Objectifs :**
  - Vaincre 5 loups / Defeat 5 wolves
  - Rapporter 3 crocs marqués / Bring 3 marked fangs
  - Examiner les traces de griffes (0/2) / Examine the claw marks (0/2)
- **Remerciement :**
  - fr : « Regarde : une fêlure à cinq branches sur chaque croc. Comme sur la pierre. Et ces loups tournaient autour de quelque chose. Je veux savoir quoi. »
  - en : "Look: a crack with five branches on every fang. Like on the stone. And those wolves were circling something. I want to know what."

### `the-root-circle` (niv. 7, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** Le cercle de racines / The Root Circle
- **Offre :**
  - fr : « Les traces mènent à une clairière où les racines ont poussé en cercle parfait. Mathurin, le chef des bûcherons, refuse d'y couper un seul arbre, et il ne veut pas me dire pourquoi. Va voir le cercle, puis va le voir, lui : avec toi, il parlera peut-être. »
  - en : "The tracks lead to a clearing where the roots have grown in a perfect circle. Mathurin, the head woodcutter, refuses to fell a single tree there, and he won't tell me why. Go and see the circle, then go and see him: he might talk to you."
- **Objectifs :**
  - Examiner le cercle de racines / Examine the root circle
  - Parler à Mathurin (campement des bûcherons) / Talk to Mathurin (woodcutters' camp)
- **Réplique de la cible (Mathurin) :**
  - fr : « Mon grand-père ne coupait pas dans le cercle, son grand-père non plus. Pourquoi ? On ne demandait pas. Un serment, c'est fait pour être tenu, pas pour être compris. »
  - en : "My grandfather never cut inside the circle, nor did his grandfather. Why? We didn't ask. An oath is made to be kept, not understood."
- **Remerciement :**
  - fr : « Un serment que personne ne comprend, autour d'une pierre que personne n'a vue. Les sangliers, eux, fouillent jusqu'à la bauge du Roi, plus loin. Si quelque chose dort sous ce bois, c'est là qu'il gronde. »
  - en : "An oath nobody understands, around a stone nobody has seen. The boars, though, dig all the way to the Boar King's wallow, further on. If something sleeps under these woods, that's where it rumbles."

### `rumble-under-the-wallow` (niv. 8, principale, gardée)
- **Donneur :** Ysaline.
- **Titre :** Ce qui gronde sous la bauge / What Rumbles under the Wallow
- **Offre :**
  - fr : « Les bûcherons entendent gronder sous la bauge du Roi sanglier. Prends cette clef : elle ne servira qu'une fois, ensuite il faudra voir Gédéon, le serrurier. Descends jusqu'à la grande salle ; c'est sous elle que ça gronde. Et si tu peux, ne descends pas seul. »
  - en : "The woodcutters hear a rumbling under the Boar King's wallow. Take this key: it will only work once, after that you'll need Gédéon the locksmith. Go down to the great hall; the rumbling is under it. And if you can, don't go down alone."
  - *« seul » est au masculin ; s'il faut l'éviter : « et si tu peux, emmène quelqu'un » / "and if you can, take someone with you".*
- **Objectif :** Atteindre la grande salle de la Bauge du Roi sanglier / Reach the great hall of the Boar King's Wallow
- **Remerciement :**
  - fr : « Une pierre comme la nôtre, sous la grande salle, avec le même cinquième signe ? Alors il y en a d'autres. Et elle n'est pas tombée là : on l'a enterrée. »
  - en : "A stone like ours, under the great hall, with the same fifth sign? Then there are others. And it didn't fall there: somebody buried it."

### `four-signs` (niv. 9, principale, nouvelle) : ouvre le Bois moisi
- **Donneur :** Mathurin (relais).
- **Titre :** Quatre signes / Four Signs
- **Offre :**
  - fr : « Ton érudite a couvert trois pages de mon carnet de comptes avec des signes. Elle dit qu'elle a compris quelque chose, et que c'est à toi qu'il faut le dire, pas à moi. Va la voir ; moi, je retourne à mes arbres. »
  - en : "Your scholar has covered three pages of my tally book with signs. She says she's understood something, and it's you she needs to tell, not me. Go and see her; I'm going back to my trees."
- **Objectif :** Parler à Ysaline / Talk to Ysaline
- **Réplique de la cible (Ysaline) :**
  - fr : « Chaque signe de la pierre regarde un côté du pays : la graine vers le Bois, la goutte vers le Lac, la plume vers les Champs, le marteau vers un endroit qu'on ne connaît pas. La face lisse, celle du cinquième, regarde le Bois moisi. Et les filaments blancs de là-bas poussent jusqu'ici. »
  - en : "Each sign on the stone faces a part of the land: the seed towards the woods, the drop towards the lake, the feather towards the fields, the hammer towards somewhere we don't know. The smooth face, the fifth one's, looks towards the Mouldy Woods. And the white threads from over there grow all the way here."
- **Remerciement (Mathurin) :**
  - fr : « Le Bois moisi. Mes gars n'y vont plus : ça sent la cave et ça colle aux bottes. Si tu y vas, Barnabé, l'ermite des mares, connaît le chemin. Prends du pain. »
  - en : "The Mouldy Woods. My lads don't go there any more: it smells of cellars and sticks to your boots. If you go, Barnabé, the hermit of the pools, knows the way. Take some bread."

### `the-pack` (niv. 6, secondaire, gardée, donneur changé)
- **Donneur :** Mathurin.
- **Titre :** La meute / The Pack
- **Offre :**
  - fr : « Une meute rôde au sud de la coupe, et mes gars n'osent plus y aller seuls. Six loups. Pas pour le plaisir : pour qu'on puisse travailler. »
  - en : "A pack roams south of the felling, and my lads daren't go there alone any more. Six wolves. Not for sport: so we can work."
- **Objectif :** Vaincre 6 loups / Defeat 6 wolves
- **Remerciement :**
  - fr : « Six. Les haches vont pouvoir chanter de nouveau. Ça mérite une miche, et un peu plus. »
  - en : "Six. The axes can sing again. That's worth a loaf, and a bit more."

### `winter-pelts` (niv. 7, secondaire, nouvelle)
- **Donneur :** Mathurin. Ouverte par `the-pack`.
- **Titre :** Des peaux pour l'hiver / Pelts for Winter
- **Offre :**
  - fr : « L'hiver vient, et mes gars dorment sous la toile. Rapporte-moi quatre peaux de loup pour les couvertures, et six noisetiers pour monter une vraie cabane. Les noisetiers poussent le long du ruisseau. »
  - en : "Winter's coming, and my lads sleep under canvas. Bring me four wolf pelts for blankets, and six hazel to build a proper hut. Hazel grows along the stream."
- **Objectifs :**
  - Rapporter 4 peaux de loup / Bring 4 wolf pelts
  - Rapporter 6 noisetiers / Bring 6 hazel
- **Remerciement :**
  - fr : « Une cabane et des couvertures : mes gars vont te bénir jusqu'au printemps. C'est peu, mais c'est de bon cœur. »
  - en : "A hut and blankets: my lads will bless you till spring. It's not much, but it's heartfelt."

### `the-old-loner` (niv. 8, secondaire, nouvelle)
- **Donneur :** Mathurin.
- **Titre :** Les vieux solitaires / The Old Loners
- **Offre :**
  - fr : « Il y a de vieux sangliers qui vivent seuls, la peau dure comme la pierre. Ils mâchent des cailloux, je te jure, et ils chargent les charrettes. Trois, et la route du bois sera sûre. »
  - en : "There are old boars that live alone, with hides as hard as stone. They chew pebbles, I swear, and they charge the carts. Three of them, and the forest road will be safe."
- **Objectif :** Vaincre 3 vieux solitaires / Defeat 3 old loners
- **Remerciement :**
  - fr : « Dans l'estomac du dernier, mes gars ont trouvé des éclats de pierre gravée. Qui mange des cailloux qui vibrent ? Enfin. La route est sûre, c'est l'essentiel. »
  - en : "In the last one's belly my lads found shards of carved stone. What eats pebbles that hum? Well. The road is safe, that's what matters."

### `boar-wallow-cleared` (niv. 9, donjon entier, gardée, donneur changé)
- **Donneur :** Mathurin. Ouverte après `rumble-under-the-wallow`.
- **Titre :** Au fond : Bauge du Roi sanglier / At the bottom: The Boar King's Wallow
- **Offre :**
  - fr : « La grande salle, c'est une chose. Le Roi, au bout, c'en est une autre : il a un éclat de pierre fiché dans la défense et il charge comme une avalanche. Va jusqu'au fond, à plusieurs, et je graverai ton nom sur le poteau du campement. »
  - en : "The great hall is one thing. The King, at the end, is another: he has a shard of stone stuck in his tusk and he charges like an avalanche. Go all the way down, with friends, and I'll carve your name on the camp post."
- **Objectif :** Finir la Bauge du Roi sanglier (groupe conseillé) / Clear the Boar King's Wallow (group advised)
- **Remerciement :**
  - fr : « Le Roi est tombé ? Je grave ton nom sur le poteau, sous celui de mon grand-père. »
  - en : "The King has fallen? I'll carve your name on the post, under my grandfather's."

### `wolf-den-cleared` (niv. 10, donjon entier, gardée, donneur changé)
- **Donneur :** Mathurin. Ouverte après `rumble-under-the-wallow`.
- **Titre :** Au fond : Tanière de la meute / At the bottom: The Pack's Den
- **Offre :**
  - fr : « La Tanière de la meute, c'est trois salles de crocs, et un chef dont la fourrure ressemble à de l'écorce. Personne ici n'est allé au bout. Va-y avec des amis : seul, c'est de l'entêtement. »
  - en : "The Pack's Den is three rooms full of fangs, and a leader whose fur looks like bark. Nobody here has reached the end. Go with friends: alone, it's just stubbornness."
- **Objectif :** Finir la Tanière de la meute (groupe conseillé) / Clear the Pack's Den (group advised)
- **Remerciement :**
  - fr : « Le chef de meute… Il avait vraiment de l'écorce sur le dos ? Grave ton nom toi-même, j'ai les mains qui tremblent. »
  - en : "The pack leader… Did he really have bark on his back? Carve your name yourself, my hands are shaking."

---

## 6. Les quêtes du Bois moisi (niveaux 10 à 18)

**Le donneur de la zone :** Barnabé, l'ermite des mares. Ysaline donne les quêtes principales, sauf « Le sceau fissuré », relayée par Barnabé.
*Le cœur thématique du chapitre : les champignons relient sans mélanger, les zombies sont des morceaux forcés ensemble (trame §2.2). On le montre d'abord, Ysaline l'interprète ensuite.*

### `what-grows-between` **[P]** (niv. 10, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** Ce qui pousse entre / What Grows Between
- **Offre :**
  - fr : « Les filaments blancs du Bois moisi s'accrochent à tout ce qu'ils touchent : la pierre, l'eau des mares, les racines. Suis-les jusqu'à trois nœuds, là où ils se rejoignent, et regarde bien. Les champignons les défendent : il faudra en écarter quelques-uns. »
  - en : "The white threads of the Mouldy Woods cling to everything they touch: stone, pool water, roots. Follow them to three knots, where they meet, and look closely. The mushrooms defend them: you'll have to clear a few out of the way."
- **Objectifs :**
  - Examiner les nœuds de mycélium (0/3) / Examine the mycelium knots (0/3)
  - Vaincre 5 champignons / Defeat 5 mushrooms
- **Remerciement :**
  - fr : « Ils relient sans mélanger : la pierre reste pierre, l'eau reste eau. Je n'avais jamais rien vu de pareil. Et pourtant, tout ce qu'ils touchent finit par pourrir. Bon signe ou mauvais signe ? Je n'en sais rien. »
  - en : "They link without mixing: the stone stays stone, the water stays water. I've never seen anything like it. And yet everything they touch ends up rotting. A good sign or a bad one? I have no idea."

### `the-undug-crypt` **[P]** (niv. 12, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** La crypte que personne n'a creusée / The Crypt Nobody Dug
- **Offre :**
  - fr : « Les filaments mènent à l'entrée d'une crypte qui n'est sur aucune carte ni dans aucun registre. Pourtant ses morts marchent, et ils portent sur eux des pages arrachées. Rapporte-m'en trois, et demande à Aubin et à Odalric s'ils connaissent cet endroit. »
  - en : "The threads lead to the entrance of a crypt that's on no map and in no registry. Yet its dead are walking, and they carry torn-out pages. Bring me three, and ask Aubin and Odalric whether they know the place."
- **Objectifs :**
  - Rapporter 3 pages de registre (sur les zombies) / Bring 3 registry pages (from zombies)
  - Interroger Aubin / Question Aubin
  - Interroger Odalric / Question Odalric
- **Réplique de la cible (Aubin) :**
  - fr : « La crypte du Bois moisi ? Mais elle a toujours été là, voyons. Je… je ne me souviens pas d'y avoir jamais pensé. C'est drôle, non ? »
  - en : "The crypt in the Mouldy Woods? Why, it's always been there. I… I don't remember ever thinking about it. Funny, isn't it?"
- **Réplique de la cible (Odalric) :**
  - fr : « Ces pages viennent d'un registre comme le mien, de la même main que nos plus vieilles pages. Mais les morts y sont rangés en quatre colonnes : terre, eau, feu, air. Nous n'avons jamais rangé personne ainsi. »
  - en : "These pages come from a registry like mine, in the same hand as our oldest pages. But the dead are listed in four columns: earth, water, fire, air. We have never listed anyone that way."
- **Remerciement :**
  - fr : « Des morts rangés par élément, une crypte que tout le monde voit sans la remarquer… Clairval ne l'a pas oubliée : il a appris à ne pas la regarder. Il faut descendre. »
  - en : "The dead sorted by element, a crypt everyone sees without noticing… Clairval didn't forget it: it learnt not to look at it. We have to go down."

### `down-into-the-crypt` **[P]** (niv. 14, principale, nouvelle ; « Crypte oubliée » dans le §5 ter.4)
- **Donneur :** Ysaline. Elle offre la clef.
- **Titre :** Descendre dans la crypte / Down into the Crypt
- *Titre changé pour ne pas se confondre avec « Au fond : Crypte oubliée ».*
- **Offre :**
  - fr : « Gédéon m'a forgé cette clef d'après le moulage de la serrure ; elle ne servira qu'une fois. La crypte a quatre ailes, et quelque chose les tient fermées en leur centre. Descends jusqu'à la deuxième salle, trouve ce qui scelle tout ça, et regarde-le pour moi. »
  - en : "Gédéon forged me this key from a cast of the lock; it will only work once. The crypt has four wings, and something at their centre keeps them shut. Go down to the second room, find whatever seals it all, and look at it for me."
- **Objectifs :**
  - Atteindre la deuxième salle de la Crypte oubliée / Reach the second room of the Forgotten Crypt
  - Examiner le sceau / Examine the seal
- **Remerciement :**
  - fr : « Un sceau pour garder les morts séparés, chacun dans son aile. Il se fissure, et ils se relèvent mêlés, de travers. Les filaments, eux, reliaient sans forcer. Quelqu'un a cru que séparer suffisait. »
  - en : "A seal to keep the dead apart, each in its own wing. It's cracking, and they rise all jumbled, put together wrong. The threads, though, linked without forcing anything. Someone believed that keeping things apart was enough."

### `the-cracked-seal` **[P]** (niv. 16, principale, nouvelle) : ouvre le Lac
- **Donneur :** Barnabé (relais).
- **Titre :** Le sceau fissuré / The Cracked Seal
- **Offre :**
  - fr : « Ton érudite, elle a dormi trois nuits chez mes champignons, à écrire. Maintenant, elle tourne en rond comme un loup. Va lui parler, toi. Moi, elle me fatigue, et les morilles aussi. »
  - en : "Your scholar slept three nights among my mushrooms, writing. Now she's pacing like a wolf. You go and talk to her. She wears me out, and the morels too."
- **Objectif :** Parler à Ysaline / Talk to Ysaline
- **Réplique de la cible (Ysaline) :**
  - fr : « Le sceau de la crypte et la vieille pierre parlent la même langue, j'en suis sûre. Et cette nuit, au Lac des Joncs, les pêcheurs ont tous chanté une berceuse qu'aucun d'eux n'a apprise. Ça recommence ailleurs. »
  - en : "The crypt's seal and the old stone speak the same language, I'm sure of it. And last night, at Reed Lake, the fishermen all sang a lullaby none of them ever learnt. It's starting again somewhere else."
- **Remerciement (Barnabé) :**
  - fr : « Le Lac ? Prends la porte du Nord du village, puis le chemin du Lac. Et salue Liane, la passeuse : elle chante faux, mais elle chante vrai. »
  - en : "The lake? Take the village's North Gate, then the Lake Road. And say hello to Liane, the ferrywoman: she sings off-key, but she sings true."

### `spores-for-the-stall` (niv. 11, secondaire, gardée, donneur changé)
- **Donneur :** Barnabé.
- **Titre :** Des spores pour l'étal / Spores for the Stall
- **Offre :**
  - fr : « Rose me commande un remède aux spores chaque saison, et c'est elle qui le vend. Il me faut cinq poussières de spores et trois chapeaux, et mes genoux ne plient plus. Prends-les sur les champignons. Demande poliment. Ou pas. »
  - en : "Rose orders a spore remedy from me every season, and she sells it. I need five spore dusts and three caps, and my knees won't bend any more. Take them from the mushrooms. Ask nicely. Or don't."
- **Objectifs :**
  - Rapporter 5 poussières de spores / Bring 5 spore dust
  - Rapporter 3 chapeaux de champignon / Bring 3 mushroom caps
- **Remerciement :**
  - fr : « Bien. Rose paiera le remède ; moi, je te donne ce que j'ai, et un conseil. Les champignons n'attaquent pas : ils défendent. Ce n'est pas pareil. »
  - en : "Good. Rose will pay for the remedy; I'll give you what I have, and some advice. Mushrooms don't attack: they defend. It's not the same thing."

### `hermits-remedy` **[P]** (niv. 13, secondaire, nouvelle)
- **Donneur :** Barnabé. Ouverte par `spores-for-the-stall`.
- **Titre :** Le remède de l'ermite / The Hermit's Remedy
- **Offre :**
  - fr : « Le remède de Rose marche. Le mien marche mieux, mais il lui faut deux anguilles des mares et trois pieds-de-loup. Pêche, cueille, rapporte. Le pied-de-loup pousse à l'ombre, là où le mycélium est le plus épais. »
  - en : "Rose's remedy works. Mine works better, but it needs two pool eels and three clubmoss. Fish, pick, bring them back. Clubmoss grows in the shade, where the mycelium is thickest."
- **Objectifs :**
  - Rapporter 2 anguilles des mares / Bring 2 pool eels
  - Rapporter 3 pieds-de-loup / Bring 3 clubmoss
- **Remerciement :**
  - fr : « Regarde : la potion reste en deux couches, l'anguille dessous, la plante dessus. Ça ne se mélange pas, et ça soigne quand même. Mieux, même. Prends-en une. »
  - en : "Look: the potion stays in two layers, eel below, plant on top. They don't mix, and it heals all the same. Better, even. Take one."

### `the-walking-dead` (niv. 12, secondaire, gardée, donneur changé)
- **Donneur :** Barnabé.
- **Titre :** Les morts qui marchent / The Walking Dead
- **Offre :**
  - fr : « Les morts de la crypte sortent la nuit et piétinent mes champignons. Le mycélium essaie de les recouvrir, de les tenir, et il n'y arrive pas. Huit. Couche-les, et qu'ils restent couchés. »
  - en : "The dead from the crypt come out at night and trample my mushrooms. The mycelium tries to cover them, to hold them, and it can't. Eight. Put them down, and make them stay down."
- **Objectif :** Vaincre 8 zombies / Defeat 8 zombies
- **Remerciement :**
  - fr : « Huit. Le mycélium les recouvre déjà. Lui, au moins, il ne lâche jamais. »
  - en : "Eight. The mycelium is already covering them. It never lets go, that one."

### `mushroom-cellar-cleared` (niv. 15, donjon entier, gardée, donneur changé)
- **Donneur :** Barnabé.
- **Titre :** Au fond : Champignonnière / At the bottom: The Mushroom Cellar
- **Offre :**
  - fr : « La Champignonnière, c'est la maison du plus vieux d'entre eux, le champignon ancestral. On ne le tue pas, tu sais : il rentre sous terre et il attend. Va jusqu'au fond, à plusieurs de préférence, et dis-moi s'il t'a regardé. »
  - en : "The Mushroom Cellar is home to the oldest of them all, the elder fungus. You don't kill him, you know: he goes back underground and waits. Go all the way down, with friends ideally, and tell me if he looked at you."
- **Objectif :** Finir la Champignonnière (groupe conseillé) / Clear the Mushroom Cellar (group advised)
- **Remerciement :**
  - fr : « Il s'est rétracté ? Bien. Il reviendra, il revient toujours. Ce n'est pas une défaite, c'est une politesse. »
  - en : "He drew back? Good. He'll come back, he always does. It's not a defeat, it's good manners."

### `zombie-crypt-cleared` (niv. 17, donjon entier, gardée, donneur changé)
- **Donneur :** Barnabé.
- **Titre :** Au fond : Crypte oubliée / At the bottom: The Forgotten Crypt
- **Offre :**
  - fr : « Au fond de la crypte, il y a un colosse fait des quatre ailes : des morceaux qui ne vont pas ensemble, cousus de force. Abats-le, avec des amis, et le bois dormira enfin. »
  - en : "At the bottom of the crypt there's a colossus made from all four wings: pieces that don't belong together, sewn by force. Bring it down, with friends, and the woods will sleep at last."
- **Objectif :** Finir la Crypte oubliée (groupe conseillé) / Clear the Forgotten Crypt (group advised)
- **Remerciement :**
  - fr : « Le colosse est tombé ? Les mares sont plus calmes ce soir. Même les filaments ont l'air de respirer. »
  - en : "The colossus has fallen? The pools are calmer tonight. Even the threads seem to be breathing."

---

## 7. Les quêtes du Lac des Joncs (niveaux 18 à 26)

**La donneuse de la zone :** Liane, sur les pontons. Ysaline donne les quêtes principales, sauf « Le dernier couplet », donnée par Liane.
*Le thème : l'eau garde les souvenirs (Névara). Des témoins qui se contredisent, un nom lavé, un couplet que personne ne connaissait.*

### `the-unlearned-lullaby` **[P]** (niv. 18, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** La berceuse que personne n'a apprise / The Lullaby Nobody Learnt
- **Offre :**
  - fr : « Au Lac des Joncs, tout le monde chante la même berceuse, et personne ne sait qui la lui a apprise. Parle à Liane, la passeuse, puis à Joachim et à sa petite-fille Fleur, sur les pontons. Et écoute les roseaux : il paraît qu'ils chantent aussi. »
  - en : "At Reed Lake everyone sings the same lullaby, and nobody knows who taught it to them. Talk to Liane the ferrywoman, then to Joachim and his granddaughter Fleur, on the jetties. And listen to the reeds: they say the reeds sing too."
- **Objectifs :**
  - Parler à Liane / Talk to Liane
  - Parler à Joachim / Talk to Joachim
  - Parler à Fleur / Talk to Fleur
  - Écouter les roseaux chantants (0/3) / Listen to the singing reeds (0/3)
- *Le §5 ter.4 dit « Liane et 2 villageois » : Joachim et Fleur sont ces deux villageois, à créer (deux habitants de plus sur les pontons).*
- **Réplique de la cible (Liane) :**
  - fr : « Je l'ai toujours chantée. Mais je ne sais pas qui me l'a chantée. Ma mère, sans doute. Ma mère ne chantait pas. »
  - en : "I've always sung it. But I don't know who sang it to me. My mother, I suppose. My mother didn't sing."
- **Réplique de la cible (Joachim) :**
  - fr : « Une berceuse ? C'est une chanson de pêche. Elle parle de filets et de brume, pas d'enfants. »
  - en : "A lullaby? It's a fishing song. It's about nets and mist, not children."
- **Réplique de la cible (Fleur) :**
  - fr : « Elle parle de quelqu'un qu'on a caché ! C'est les grenouilles qui me l'ont apprise. Grand-père dit que non, mais si. »
  - en : "It's about someone who was hidden! The frogs taught it to me. Grandad says they didn't, but they did."
- **Remerciement :**
  - fr : « Une berceuse, une chanson de pêche, quelqu'un qu'on a caché : trois chansons sur le même air. Et elle s'arrête toujours au même endroit. Il manque la fin, et les grenouilles chantent plus loin que nous. »
  - en : "A lullaby, a fishing song, someone hidden: three songs to the same tune. And it always stops at the same place. The ending is missing, and the frogs sing further than we do."

### `the-last-verse` **[P]** (niv. 20, principale, nouvelle)
- **Donneur :** Liane (relais).
- **Titre :** Le dernier couplet / The Last Verse
- **Offre :**
  - fr : « Les grenouilles chantent la fin, celle que personne ne connaît. Le crapaud-roi, au fond de sa grotte, la chante mieux que toutes, à ce qu'on dit. Va jusqu'à lui et rapporte-moi le couplet. Je voudrais l'entendre une fois, rien qu'une. »
  - en : "The frogs sing the ending, the one nobody knows. The toad king, deep in his grotto, sings it best of all, so they say. Go to him and bring me back the verse. I'd like to hear it once, just once."
- **Objectifs :**
  - Atteindre la deuxième salle de la Grotte du Roi grenouille / Reach the second room of the Frog King's Grotto
  - Rapporter le dernier couplet (crapaud-roi) / Bring the last verse (toad king)
- **Remerciement :**
  - fr : « "Toi que la mer a caché"… Il n'y a pas de mer, ici. Il n'y a que le lac. Alors pourquoi est-ce que j'ai les yeux qui piquent ? Merci. »
  - en : "'You whom the sea has hidden'… There's no sea here. There's only the lake. So why are my eyes stinging? Thank you."

### `the-washed-name` **[P]** (niv. 23, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** La passeuse au nom lavé / The Ferrywoman's Washed-Out Name
- **Offre :**
  - fr : « Liane a repris la barque d'une passeuse dont personne ne se rappelle le nom. Les bernard-l'ermite du lac se font des coquilles avec des tessons peints, des morceaux d'une vieille plaque de proue. Rapporte-m'en six, et montre-les à Odalric : s'il y a un nom dessus, il le lira. »
  - en : "Liane took over the boat of a ferrywoman whose name nobody remembers. The lake's hermit crabs make shells out of painted shards, pieces of an old prow plaque. Bring me six, and show them to Odalric: if there's a name on them, he'll read it."
- **Objectifs :**
  - Rapporter 6 tessons peints (bernard-l'ermite) / Bring 6 painted shards (hermit crabs)
  - Montrer les tessons à Odalric / Show the shards to Odalric
- **Réplique de la cible (Odalric) :**
  - fr : « Six tessons, six lettres : I, S, A, U, R, E. Isaure. Ce nom est dans le registre, sur une page que j'ai toujours crue blanche. L'encre a été lavée, mais on la devine encore, à contre-jour. »
  - en : "Six shards, six letters: I, S, A, U, R, E. Isaure. That name is in the registry, on a page I always believed was blank. The ink was washed away, but you can still make it out against the light."
- **Remerciement :**
  - fr : « Isaure. Une passeuse de Clairval, effacée du registre et de toutes les mémoires, sauf d'une chanson. On n'oublie pas quelqu'un par hasard. Il faut le dire à Aubin : il connaît toutes les familles. »
  - en : "Isaure. A ferrywoman from Clairval, wiped from the registry and from every memory, except a song. Nobody forgets someone by chance. We must tell Aubin: he knows every family."

### `the-name-returned` **[P]** (niv. 25, principale, nouvelle) : ouvre les Champs
- **Donneur :** Ysaline.
- **Titre :** Le nom rendu / The Name Returned
- **Offre :**
  - fr : « Aubin connaît toutes les familles de Clairval, depuis toujours. Dis-lui le nom d'Isaure, simplement, et regarde ce qu'il fait. Moi, je n'ose pas. »
  - en : "Aubin has known every family in Clairval all his life. Just tell him Isaure's name, and watch what he does. I don't dare."
- **Objectif :** Dire le nom d'Isaure à Aubin / Tell Aubin Isaure's name
- **Réplique de la cible (Aubin) :**
  - fr : « Isaure… Je ne connais personne de ce nom. Alors pourquoi je pleure ? Ma grand-mère fredonnait un air, le soir, sur le ponton. Je l'avais oublié jusqu'à maintenant. »
  - en : "Isaure… I don't know anyone by that name. Then why am I crying? My grandmother used to hum a tune of an evening, on the jetty. I'd forgotten it, until now."
- **Remerciement :**
  - fr : « Sa tête ne s'en souvenait pas, mais lui, si. Rien ne s'efface tout à fait, alors : ça se cache. Les Champs, maintenant : Églantine, la fermière, dit que ses ruches brûlent. »
  - en : "His head didn't remember, but he did. So nothing is ever wiped out completely: it hides. The fields now: Églantine, the farmer, says her hives are burning."

### `frog-song` (niv. 19, secondaire, gardée, donneur changé)
- **Donneur :** Liane.
- **Titre :** Le chant des grenouilles / The Frog Song
- **Offre :**
  - fr : « Les grenouilles chantent jour et nuit depuis le printemps, et les pêcheurs ne dorment plus. Moi, j'aime bien. Eux, non. Fais-en taire huit, doucement si tu peux. »
  - en : "The frogs have sung day and night since spring, and the fishermen can't sleep. I quite like it. They don't. Quieten eight of them, gently if you can."
- **Objectif :** Vaincre 8 grenouilles / Defeat 8 frogs
- **Remerciement :**
  - fr : « Le silence. C'est presque triste. Les pêcheurs te paient ; moi, je te chanterais bien quelque chose. Non ? Tant pis. »
  - en : "Silence. It's almost sad. The fishermen are paying you; I'd happily sing you something. No? Never mind."

### `lianes-nets` **[P]** (niv. 22, secondaire, nouvelle)
- **Donneur :** Liane. Ouverte par `frog-song`.
- **Titre :** Les filets de Liane / Liane's Nets
- **Offre :**
  - fr : « Mes filets ont plus de trous que de mailles. Il me faut huit brassées de laîche des rives pour les retresser, et cinq perches pour payer le cordier, qui ne prend que du poisson. »
  - en : "My nets have more holes than mesh. I need eight armfuls of shore sedge to reweave them, and five perch to pay the ropemaker, who only takes fish."
- **Objectifs :**
  - Rapporter 8 laîches des rives / Bring 8 shore sedge
  - Rapporter 5 perches / Bring 5 perch
- **Remerciement :**
  - fr : « Des filets neufs ! On dit que la passeuse d'avant moi faisait les mêmes nœuds. Personne ne sait qui l'a dit. Tiens, c'est pour toi. »
  - en : "New nets! They say the ferrywoman before me tied the same knots. Nobody knows who said so. Here, this is for you."

### `shells-and-claws` (niv. 21, secondaire, gardée, donneur changé)
- **Donneur :** Liane.
- **Titre :** Carapaces et pinces / Shells and Claws
- **Offre :**
  - fr : « Rose me paie les carapaces du lac pour ses boutons, mais les crabes deviennent hargneux. Quatre carapaces, deux pinces. Rose fera les boutons, moi je ferai les comptes. »
  - en : "Rose pays me for the lake's shells to make her buttons, but the crabs are turning nasty. Four shells, two claws. Rose will make the buttons, I'll do the sums."
- **Objectifs :**
  - Rapporter 4 carapaces de crabe / Bring 4 crab shells
  - Rapporter 2 pinces de crabe / Bring 2 crab claws
- **Remerciement :**
  - fr : « Regarde-moi ce nacre. Rose va dire que c'est trop cher. Rose dit toujours ça. »
  - en : "Look at that mother-of-pearl. Rose will say it's too dear. Rose always says that."

### `frog-grotto-cleared` (niv. 24, donjon entier, gardée, donneur changé)
- **Donneur :** Liane.
- **Titre :** Au fond : Grotte du Roi grenouille / At the bottom: The Frog King's Grotto
- **Offre :**
  - fr : « Tout au fond de la grotte, c'est là que la chanson est la plus forte. Les pêcheurs n'y vont pas, même à plusieurs. Toi, emmène du monde, va jusqu'au bout, et reviens me dire ce qu'on y entend. »
  - en : "Right at the back of the grotto is where the song is loudest. The fishermen won't go, not even together. You, take some company, go all the way, and come back and tell me what you hear."
- **Objectif :** Finir la Grotte du Roi grenouille (groupe conseillé) / Clear the Frog King's Grotto (group advised)
- **Remerciement :**
  - fr : « Le roi s'est tu ? Le lac va me sembler bien grand. Merci quand même. »
  - en : "The king has gone quiet? The lake is going to feel very big. Thank you all the same."

### `crab-reef-cleared` (niv. 25, donjon entier, gardée, donneur changé)
- **Donneur :** Liane.
- **Titre :** Au fond : Récif des pinces / At the bottom: The Pincer Reef
- **Offre :**
  - fr : « Le Récif des pinces, c'est le cimetière des barques. Le crabe géant, au bout, porte une proue sur le dos, peut-être celle de la passeuse d'avant. Va au fond, avec des amis, et dis-moi s'il y reste quelque chose de peint. »
  - en : "The Pincer Reef is where boats go to die. The giant crab at the end carries a prow on its back, maybe the old ferrywoman's. Go all the way down, with friends, and tell me if anything painted is left on it."
- **Objectif :** Finir le Récif des pinces (groupe conseillé) / Clear the Pincer Reef (group advised)
- **Remerciement :**
  - fr : « Juste une goutte peinte, à moitié effacée ? C'est déjà beaucoup. Je la peindrai sur ma barque. »
  - en : "Just a painted drop, half worn away? That's already a lot. I'll paint it on my boat."

---

## 8. Les quêtes des Champs de Clairval (niveaux 26 à 30)

**La donneuse de la zone :** Églantine, dans sa ferme. Ysaline donne les quêtes principales, sauf « Sous les haies », donnée par Églantine.
*Le thème : les abeilles sont Air et Feu et tiennent, parce qu'elles coopèrent ; le Disjoint est fait de quatre bêtes forcées en une et se déchire. C'est la thèse du chapitre, montrée deux fois avant d'être dite.*

### `burning-hives` **[P]** (niv. 26, principale, nouvelle)
- **Donneur :** Ysaline.
- **Titre :** Les ruches brûlantes / The Burning Hives
- **Offre :**
  - fr : « Églantine dit que ses ruches sont chaudes comme des fours et que ses abeilles piquent avec du feu. Écartes-en huit en les regardant bien, puis va voir Rose : c'est elle qui vend ce miel, elle saura s'il a changé. »
  - en : "Églantine says her hives are as hot as ovens and her bees sting with fire. Fend off eight while you watch them closely, then go and see Rose: she sells that honey, she'll know if it's changed."
- **Objectifs :**
  - Vaincre 8 abeilles / Defeat 8 bees
  - Parler à Rose (Marché) / Talk to Rose (Market)
- **Réplique de la cible (Rose) :**
  - fr : « Le miel des Champs ? Il reste tiède dans le pot, même l'hiver, et il soigne mieux qu'avant. Je l'ai vendu deux fois plus cher. Tu crois que j'aurais dû m'inquiéter ? »
  - en : "Honey from the fields? It stays warm in the jar, even in winter, and it heals better than before. I've been selling it at twice the price. Do you think I should have been worried?"
- **Remerciement :**
  - fr : « De l'air et du feu dans la même ruche, et rien ne brûle. Les zombies étaient des morceaux forcés ensemble ; les abeilles, elles, travaillent ensemble. Je crois que c'est même le contraire. »
  - en : "Air and fire in the same hive, and nothing burns. The zombies were pieces forced together; the bees work together. I think it's the very opposite."

### `under-the-hedges` **[P]** (niv. 28, principale, nouvelle)
- **Donneur :** Églantine (relais).
- **Titre :** Sous les haies / Under the Hedges
- **Offre :**
  - fr : « Les taupes creusent toutes vers la même haie, la vieille, au bout de mon champ. Regarde trois de leurs galeries, et si tu croises la taupe-roi, prends ce qu'elle garde : on dit qu'elle a déterré un bout de métal qui chante. »
  - en : "The moles all dig towards the same hedge, the old one at the end of my field. Look into three of their tunnels, and if you come across the mole king, take what it's guarding: they say it dug up a piece of metal that sings."
- **Objectifs :**
  - Examiner les galeries des taupes (0/3) / Examine the mole tunnels (0/3)
  - Rapporter le débris de métal (taupe-roi) / Bring the metal debris (mole king)
- **Remerciement :**
  - fr : « Ça vibre, ce truc, et mes abeilles deviennent folles dès que je l'approche. Je le descends au fond du puits, loin des ruches. Va raconter à ton érudite ce que tu as vu. »
  - en : "That thing hums, and my bees go mad the moment I bring it near. I'm lowering it to the bottom of the well, away from the hives. Go and tell your scholar what you saw."

### `the-disjoint` **[P]** (niv. 30, principale, nouvelle) : le boss du chapitre
- **Donneur :** Ysaline.
- **Titre :** Le Disjoint / The Disjoint
- **Offre :**
  - fr : « Sous les Galeries des taupes, quelque chose remue, qui n'est ni taupe, ni crabe, ni abeille, et les bêtes du pays fuient toutes à la fois. Va à la porte des Galeries. D'après les pêcheurs qui l'ont aperçu, il s'use tout seul à chaque instant, mais il éclate quand il tombe : ne reste pas collé à lui. »
  - en : "Under the Mole Burrows something is stirring that's neither mole, crab nor bee, and every beast in the land is fleeing at once. Go to the Burrows' gate. From what the fishermen glimpsed, it wears itself out with every moment, but it bursts when it falls: don't stay right next to it."
- **Objectif :** Vaincre le Disjoint (porte des Galeries des taupes) / Defeat the Disjoint (Mole Burrows gate)
- **Remerciement :**
  - fr : « Quatre bêtes forcées en une seule, et elle n'a pas tenu un jour. En tombant, ses morceaux ont dessiné le cinquième signe sur la terre. Rentrons à Clairval : la pierre a quelque chose à nous dire, j'en suis sûre. »
  - en : "Four beasts forced into one, and it didn't last a day. As it fell, its pieces traced the fifth sign on the ground. Let's go back to Clairval: the stone has something to tell us, I'm sure of it."
- *Le conseil tactique (il perd de la vie à chaque tour, il explose en mourant) vient de `design-familles.md` §4.9.*

### `four-trails` (niv. 30, principale, gardée, déplacée du niveau 8) : la fin du chapitre
- **Donneur :** Ysaline (place).
- **Titre :** Quatre pistes / Four Trails
- **Offre :**
  - fr : « La pierre s'est tue. Les quatre signes, eux, se sont allumés, chacun tourné vers une route qu'on n'a jamais prise. Rose, Garance et Aubin savent chacun quelque chose de ces routes : va les trouver, ils t'attendent. »
  - en : "The stone has gone quiet. But the four signs have lit up, each one facing a road we've never taken. Rose, Garance and Aubin each know something about those roads: go and find them, they're expecting you."
- **Objectifs :**
  - Parler à Rose / Talk to Rose
  - Parler à Garance / Talk to Garance
  - Parler à Aubin / Talk to Aubin
- **Réplique de la cible (Rose) :**
  - fr : « Mes voyageurs parlent d'une ville sur la côte où personne ne raconte deux fois la même histoire. Ils y vendent bien, et ils en repartent vite. »
  - en : "My travellers talk of a town on the coast where nobody tells the same story twice. They sell well there, and they leave quickly."
- **Réplique de la cible (Garance) :**
  - fr : « J'ai appris l'épée chez des forgerons, très loin vers le soleil levant. Ils ont fermé un atelier, l'an passé, après des accidents. Les armes qui en sortaient se mettaient en colère avant leur porteur. »
  - en : "I learnt the sword among smiths, far away towards the sunrise. Last year they shut a workshop after some accidents. The weapons that came out of it lost their temper before whoever held them."
- **Réplique de la cible (Aubin) :**
  - fr : « J'ai vu passer des cartes de toutes sortes. Celles du nord changent d'une nuit à l'autre, et celles du sud montrent un arbre si grand qu'il porte une ville. Si vous partez, la pierre continuera de parler sans vous. »
  - en : "I've seen all sorts of maps go by. The northern ones change from one night to the next, and the southern ones show a tree so big it holds a town. If you leave, the stone will go on talking without you."
- **Remerciement :**
  - fr : « Une ville qui oublie, une forge interdite, des cartes qui changent la nuit, des racines sous un arbre géant. Quatre routes, dans l'ordre que tu voudras. Moi, je reste avec la pierre : quelqu'un doit l'écouter quand tu n'es pas là. »
  - en : "A town that forgets, a forbidden forge, maps that change overnight, roots beneath a giant tree. Four roads, in whatever order you like. I'm staying with the stone: someone has to listen to it while you're away."

### `clairval-honey` (niv. 27, secondaire, gardée, donneur changé)
- **Donneur :** Églantine.
- **Titre :** Le miel de Clairval / Clairval Honey
- **Offre :**
  - fr : « Rose attend mon miel, mais mes abeilles piquent avec du feu et je n'ose plus ouvrir les ruches. Cinq rayons. Prends-les sur les abeilles elles-mêmes : elles en portent toujours un peu. »
  - en : "Rose is waiting for my honey, but my bees sting with fire and I daren't open the hives any more. Five combs. Take them from the bees themselves: they always carry a little."
- **Objectif :** Rapporter 5 rayons de miel / Bring 5 honeycombs
- **Remerciement :**
  - fr : « Tiède dans la main, comme du pain qui sort du four. Rose va le vendre une fortune, et moi je n'en verrai qu'un quart. Prends ta part. »
  - en : "Warm in the hand, like bread fresh from the oven. Rose will sell it for a fortune, and I'll see a quarter of it. Take your share."

### `harvest-bread` **[P]** (niv. 28, secondaire, nouvelle)
- **Donneur :** Églantine. Ouverte par `clairval-honey`.
- **Titre :** Le pain de la moisson / The Harvest Bread
- **Offre :**
  - fr : « La moisson approche, et la coutume veut qu'on la fête avec un pain de nos propres blés. Rapporte-moi dix blés de Clairval et deux pains cuits de tes mains au four de Basile. Il râlera, mais il te prêtera son four. »
  - en : "Harvest is coming, and custom says we celebrate with bread from our own wheat. Bring me ten Clairval wheat and two loaves you've baked yourself in Basile's oven. He'll grumble, but he'll lend you his oven."
- **Objectifs :**
  - Rapporter 10 blés de Clairval / Bring 10 Clairval wheat
  - Cuire 2 pains au four et les rapporter / Bake 2 loaves in the oven and bring them
- **Remerciement :**
  - fr : « Croustillant ! On le rompt en cinq morceaux, c'est la coutume. Pourquoi cinq ? Ma mère disait que c'était pour l'invité qu'on attend. Je n'en sais pas plus. »
  - en : "Lovely and crusty! We break it into five pieces, that's the custom. Why five? My mother said it was for the guest we're waiting for. That's all I know."

### `under-the-furrows` (niv. 27, secondaire, gardée, donneur changé)
- **Donneur :** Églantine.
- **Titre :** Sous les sillons / Under the Furrows
- **Offre :**
  - fr : « Les taupes creusent sous mes sillons jusqu'à faire tomber la charrue. Huit, et je te nourris tout l'hiver. Et regarde où elles vont : toutes du même côté, comme si on les appelait. »
  - en : "The moles dig under my furrows until the plough falls in. Eight, and I'll feed you all winter. And watch where they go: all the same way, as if something were calling them."
- **Objectif :** Vaincre 8 taupes / Defeat 8 moles
- **Remerciement :**
  - fr : « Les sillons tiennent. Tu as gagné ta soupe, et le droit de te plaindre de ma cuisine. »
  - en : "The furrows are holding. You've earned your soup, and the right to complain about my cooking."

### `royal-hive-cleared` (niv. 29, donjon entier, gardée, donneur changé)
- **Donneur :** Églantine.
- **Titre :** Au fond : Ruche royale / At the bottom: The Royal Hive
- **Offre :**
  - fr : « La Ruche royale, c'est là que naît la chaleur. La reine change d'humeur et d'élément selon l'état de sa ruche. Va au fond, à plusieurs, et dis-moi si elle va bien. Oui, si elle va bien. »
  - en : "The Royal Hive is where the heat begins. The queen changes mood and element with the state of her hive. Go to the bottom, with friends, and tell me if she's all right. Yes, if she's all right."
- **Objectif :** Finir la Ruche royale (groupe conseillé) / Clear the Royal Hive (group advised)
- **Remerciement :**
  - fr : « Elle est vaincue, et la ruche refroidit déjà. Elle repartira : les reines repartent toujours. Merci. »
  - en : "She's beaten, and the hive is already cooling. She'll start again: queens always do. Thank you."

### `mole-burrows-cleared` (niv. 29, donjon entier, gardée, donneur changé)
- **Donneur :** Églantine.
- **Titre :** Au fond : Galeries des taupes / At the bottom: The Mole Burrows
- **Offre :**
  - fr : « Les Galeries des taupes descendent plus bas que mon puits. La taupe-roi y garde ce qu'elle déterre. Prends des amis et va jusqu'au fond : je veux savoir ce qu'il y a sous mes champs. »
  - en : "The Mole Burrows go deeper than my well. The mole king keeps whatever it digs up down there. Take some friends and go all the way: I want to know what's under my fields."
- **Objectif :** Finir les Galeries des taupes (groupe conseillé) / Clear the Mole Burrows (group advised)
- **Remerciement :**
  - fr : « Rien que de la terre, et encore de ce métal qui vibre ? Bon. Je planterai des fleurs au-dessus, ça ne fera pas de mal. »
  - en : "Nothing but earth, and more of that humming metal? Right. I'll plant flowers on top, it can't hurt."

---

## 9. Les objets à examiner (`Interact`) et les objets de quête

### 9.1 Les objets à examiner (`Town.Props`)
Les identifiants sont proposés **[P]**. Le texte est ce qu'on lit en cliquant.

| Id **[P]** | Carte, quête | fr | en |
|---|---|---|---|
| `old-stone` | place, `whispering-stone` | « Une pierre grise plus haute qu'un homme, fendue de haut en bas : une graine, une goutte, un marteau, une plume. Sur la face lisse, usée par les mains, un cinquième signe affleure : cinq traits en cercle, qui se font face sans se toucher. » | "A grey stone taller than a man, split from top to bottom: a seed, a drop, a hammer, a feather. On the smooth face, worn by hands, a fifth sign shows through: five strokes in a ring, facing each other without touching." |
| `loft-hatch` | Greniers, `cellar-rats` (lance le combat) | « Une trappe en bois qui gratte de l'intérieur. Quelque chose a rongé le loquet. » | "A wooden hatch, scratching from the inside. Something has gnawed through the latch." |
| `old-grave-1` | Clos de la chapelle, `the-registry` | « Une dalle mangée de mousse. Le nom est effacé, mais on lit encore une date : trois cents ans avant la fondation de Clairval. » | "A slab eaten by moss. The name is gone, but a date can still be read: three hundred years before Clairval was founded." |
| `old-grave-2` | Clos de la chapelle, `the-registry` | « Quatre signes gravés autour d'un creux gratté au burin. Celui qui l'a gratté a pris soin de ne rien abîmer d'autre. » | "Four signs carved around a hollow scraped out with a chisel. Whoever scraped it took care to damage nothing else." |
| `claw-marks-1` | Bois, `restless-beasts` | « Des griffures fraîches, toutes à la même hauteur, sur des troncs qui dessinent un arc de cercle. » | "Fresh claw marks, all at the same height, on trunks that form an arc." |
| `claw-marks-2` | Bois, `restless-beasts` | « Le sol est tassé en un sentier courbe. Les loups ont tourné ici des nuits entières, autour de quelque chose. » | "The ground is trodden into a curving path. The wolves have circled here for nights on end, around something." |
| `root-circle` | Bois, `the-root-circle` | « Des racines épaisses comme des bras forment un cercle parfait, plus serré qu'aucune main ne l'aurait tressé. Au centre, la terre est bombée, comme si elle couvrait une pierre. » | "Roots as thick as arms form a perfect circle, tighter than any hand could weave. In the middle the earth is mounded, as if covering a stone." |
| `mycelium-knot-1` | Bois moisi, `what-grows-between` | « Des filaments blancs tiennent un éclat de pierre gravée sans l'entamer. La pierre reste pierre, le filament reste filament. » | "White threads hold a shard of carved stone without biting into it. The stone stays stone, the thread stays thread." |
| `mycelium-knot-2` | Bois moisi, `what-grows-between` | « Un filament plonge dans une mare et en ressort sec. Autour, l'eau est claire comme de la source. » | "A thread dips into a pool and comes out dry. Around it the water is clear as spring water." |
| `mycelium-knot-3` | Bois moisi, `what-grows-between` | « Une racine et un filament s'enroulent l'un autour de l'autre sans se mêler, comme deux mains qui se tiennent. » | "A root and a thread wind around each other without merging, like two hands holding." |
| `crypt-seal` | Crypte oubliée, salle 2, `down-into-the-crypt` | « Un disque d'un métal sombre que personne ne sait nommer, posé là où les quatre ailes se rejoignent. Chaque signe regarde une aile. Une fissure le traverse, et elle est tiède. » | "A disc of a dark metal nobody can name, set where the four wings meet. Each sign faces one wing. A crack runs through it, and it is warm." |
| `singing-reed-1` | Lac, `the-unlearned-lullaby` | « Le vent passe dans les roseaux, et on reconnaît les premières notes de la berceuse. » | "The wind runs through the reeds, and you recognise the first notes of the lullaby." |
| `singing-reed-2` | Lac, `the-unlearned-lullaby` | « Les roseaux chantent plus fort quand la brume monte. Pourtant, il n'y a pas de vent. » | "The reeds sing louder as the mist rises. Yet there is no wind." |
| `singing-reed-3` | Lac, `the-unlearned-lullaby` | « Ici, tous les roseaux se taisent en même temps, au milieu d'une note, comme si la suite manquait. » | "Here every reed falls silent at once, in the middle of a note, as if the rest were missing." |
| `mole-tunnel-1` | Champs, `under-the-hedges` | « Une galerie fraîche, droite comme un sillon tracé au cordeau. Elle file vers la vieille haie. » | "A fresh tunnel, straight as a furrow laid with a line. It runs towards the old hedge." |
| `mole-tunnel-2` | Champs, `under-the-hedges` | « Les parois vibrent sous la main, très légèrement, comme une corde pincée au loin. » | "The walls tremble under your hand, very slightly, like a string plucked far away." |
| `mole-tunnel-3` | Champs, `under-the-hedges` | « Au fond, une écaille de métal sombre, la même que le sceau de la crypte. Elle est tiède. » | "At the bottom, a flake of dark metal, the same as the crypt's seal. It is warm." |
| `burrows-gate` **[P]** | Champs, porte des Galeries, `the-disjoint` (lance le combat, si le moteur le fait par un objet) | « La terre devant la porte bouge toute seule, comme si quelque chose respirait dessous. » | "The earth in front of the gate moves on its own, as if something were breathing beneath it." |

*Le cinquième signe (« cinq traits en cercle, qui se font face sans se toucher ») est une proposition de grammaire visuelle **[P]** : il dit la relation, et ce n'est pas un mélange des quatre symboles (bible §18). À valider avant de le dessiner.*

### 9.2 Les objets de quête (`kind: Quest`)
| Id **[P]** | Nom fr / en | Vient de, quête | Description fr | Description en |
|---|---|---|---|---|
| `rat-tail` | Queue de rat / Rat Tail | rats du grenier, `cellar-rats` | « Longue, rose et pas très propre. Firmin veut la voir pour y croire. » | "Long, pink and not very clean. Firmin wants to see it to believe it." |
| `marked-fang` | Croc marqué / Marked Fang | loups, `restless-beasts` | « Un croc de loup parcouru d'une fêlure à cinq branches, comme la vieille pierre. Il est tiède. » | "A wolf fang crossed by a five-branched crack, like the old stone. It is warm." |
| `registry-page` | Page de registre / Registry Page | zombies, `the-undug-crypt` | « Une page de vélin moisie. Des noms rangés en quatre colonnes, sous quatre signes. La marge est grattée. » | "A mouldy vellum page. Names listed in four columns, under four signs. The margin has been scraped." |
| `last-verse` | Le dernier couplet / The Last Verse | crapaud-roi, `the-last-verse` | « Une coquille nacrée, encore humide. Contre l'oreille, elle chante : "Dors, toi que la mer a caché, / Ni compté, ni nommé, ni oublié." » | "A pearly shell, still damp. Held to the ear, it sings: 'Sleep, you whom the sea has hidden, / Never counted, never named, never forgotten.'" |
| `painted-shard` | Tesson peint / Painted Shard | bernard-l'ermite, `the-washed-name` | « Un tesson de terre cuite poli par l'eau, qui servait de coquille à un bernard-l'ermite. Une lettre peinte, et le bord d'une goutte. » | "A water-worn piece of fired clay that a hermit crab used as a shell. One painted letter, and the edge of a drop." |
| `humming-debris` | Débris de métal / Metal Debris | taupe-roi, `under-the-hedges` | « Un éclat de métal lourd et sombre qui vibre comme la vieille pierre. Personne, pas même Anselme, n'a jamais vu ce métal. » | "A heavy, dark sliver of metal that hums like the old stone. Nobody, not even Anselme, has ever seen this metal." |

**Les objets qui ne sont pas des objets de quête :**
- **Pieu de palissade** / Palisade Stake (fabriqué, `palisade-stakes`) : « Un pieu de chêne taillé en pointe et ferré de cuivre. Hortense en compte mille deux cents. » / "An oak stake sharpened to a point and shod with copper. Hortense counts twelve hundred of them."
- **Insigne de Garance** (existant) : « L'insigne de cuivre des élèves de Garance. Elle ne le donne qu'à ceux qui ont tenu debout. » / "The copper badge of Garance's pupils. She only gives it to those who stayed on their feet."

---

## 10. Ce qui demande du code ou une décision (pour le dev et le game designer)

1. **Les répliques des cibles « parler à ».** Aujourd'hui, la cible garde sa conversation habituelle (limite de T56). Sans réplique propre, les témoignages de « La pierre qui murmurait », « La berceuse… », « Le nom rendu » et « Quatre pistes » ne se lisent nulle part. Proposition : un champ `line` sur l'objectif `Talk`, montré par la cible quand l'objectif est en cours. C'est petit, mais c'est indispensable au fil.
2. **Rendre une quête à la cible.** Le moteur rend la quête au donneur. Pour `welcome` (Aubin donne, Garance est la cible), le joueur doit faire un aller-retour inutile au tout début du jeu. Proposition : un champ facultatif `handIn` (« clairval/garance »). Le remerciement de rechange est écrit plus haut.
3. **Le combat de « Première leçon ».** Le §5 ter.3 parle d'un mannequin animé et de l'élève de Garance, mais le scénario `training` oppose un orque et un braconnier (texte de `trainer.json`). J'ai écrit pour le mannequin et l'élève. Si `training` reste tel quel, il faut changer l'offre et la revanche (une phrase chacune).
4. **`the-old-loner` vise un monstre, pas une famille.** `Defeat` compte une famille (`boar`). « Vaincre 3 vieux solitaires » demande un `Defeat` par identifiant de monstre (`old-loner`), ou bien la quête devient « 3 sangliers de la harde du vieux solitaire ».
5. **Des ressources au-dessus du niveau de la quête :**
   - `hermits-remedy` (niv. 13) demande du pied-de-loup, qui est de niveau 15 ; proposition : la mousse-lumière (niv. 10), et le texte devient « là où le mycélium brille » ;
   - `harvest-bread` (niv. 28) demande du blé de Clairval, qui est de niveau 30 ; proposition : le riz des joncs (niv. 25) ou le seigle (niv. 15), ou bien la quête passe au niveau 30.
6. **Un ordre de sprints à vérifier.** `palisade-stakes` (S104) fait tailler un pieu à la forge, alors que le forgeron arrive en S106. Il faut soit une forge de tuto en S102, soit un pieu taillé à l'établi du serrurier.
7. **Des objectifs dans un donjon :**
   - `down-into-the-crypt` demande d'examiner le sceau **dans** la salle 2 de la crypte : il faut des objets `Interact` dans les salles de donjon ;
   - `the-last-verse` demande la salle 2 de la grotte et le couplet du crapaud-roi, qui est le chef du bout du donjon. Il faut que le crapaud-roi se trouve aussi dans les groupes de la zone, ou que l'objectif devienne « finir la grotte » ;
   - même question pour la taupe-roi de `under-the-hedges`.
8. **Le « Débris gravé » du Disjoint** (`design-familles.md` §4.9) ne sert plus, puisque l'objectif est `Win`. Proposition : le retirer, ou en faire un objet de collection et non un objet de quête.
9. **La porte de l'Ouest est-elle fermée avant `edge-of-the-woods` ?** L'objectif de C9 se trouve derrière la porte. J'ai donc écrit « Mahaut ne te demandera plus où tu vas », qui marche dans les deux cas. Si la porte est fermée avant C9, il faut que le combat de C9 se fasse juste devant (ou que la porte s'ouvre dès l'offre).
10. **Les objectifs gardés qui changent.** `edge-of-the-woods` perd son « parler à Rose » ; `restless-beasts` change ses objectifs. Un personnage enregistré en cours de route aura un état qui ne correspond plus (bible §17). Il faut une règle de migration.
11. **Où se tient Ysaline ?** « Ysaline voyage » peut vouloir dire qu'elle se tient chez le donneur de la zone, ou qu'elle reste sur la place. J'ai écrit ses textes sans « ici » pour que les deux marchent. Proposition **[P]** : elle se tient chez le donneur de la zone tant qu'une quête principale de cette zone est en cours, et sur la place sinon.
12. **La récompense de `well-equipped`.** « Dague ou bouclier selon la classe », mais il y a trois classes. Que reçoit le Mage ? (Le texte dit seulement « ce qui va le mieux à ta façon de te battre ».)
13. **Deux habitants de plus au Lac** (Joachim, Fleur), en plus des 17 du §5 ter.5.

---

## 11. Points de canon et questions pour Charles

### 11.1 Ce que je signale (rien n'est corrigé)
- **Les niveaux du chapitre 1.** La bible dit 1-15 ; la v5 étend de fait le chapitre 1 jusqu'au niveau 30 (l'option A de la trame). C'est cohérent avec le jeu, mais c'est un écart avec la bible à noter dans `DECISIONS.md`.
- **La trame est en retard sur le jeu à trois endroits :** « Garance ouvre la porte est » (il n'y a plus de porte à l'est) ; « les classes bi-élémentaires » (elles ont les quatre éléments depuis T34) ; « Aubin donneur principal » (c'est Ysaline depuis T56).
- **L'Accord n'est jamais nommé** ni ressuscité. Le joueur atteint le niveau 2 d'information (bible §15) : une présence a été effacée exprès (la page grattée, la tombe grattée, le nom lavé), et la séparation forcée échoue (le sceau, le Disjoint). Le rôle des Primordiaux n'est qu'effleuré, par l'ordre « des quatre colonnes ».
- **Les indices de la Saga III restent discrets :**
  - le couplet « toi que la mer a caché » (trame, indice 7), et Fleur, qui parle de « quelqu'un qu'on a caché » ;
  - le cinquième morceau du pain de la moisson, « pour l'invité qu'on attend » **[P]**, qui évoque l'Accord sans le nommer.
  Aucun enfant de Primordial n'est nommé ni montré.
- **Le héros aux quatre éléments :** une seule remarque, de Garance (`well-equipped`), jamais reprise.
- **La devise de Névara** est seulement frôlée (« Rien ne s'efface tout à fait »), sans la citer, pour garder la vraie phrase pour son arc.
- **La réplique du gardien de la Chambre sans nom** n'apparaît pas : elle est réservée au chapitre 4.

### 11.2 Les questions
1. **Les noms** : Hortense, Firmin, Léonie, Odalric, Gaspard, Anselme, Perrine, Sidonie, Basile, Gédéon, Thibaut, Mahaut, Gautier, Mathurin, Barnabé, Liane, Églantine, Joachim, Fleur, Isaure. Lesquels gardez-vous ?
2. **Odalric** : copiste laïc (ma proposition), ou « Frère Odalric », comme dans la trame, attaché à la chapelle des Quatre ?
3. **Isaure, la grand-mère oubliée d'Aubin** : ce lien vous va-t-il ? Il donne à Aubin son évolution (du déni aux larmes), mais il ancre le mystère dans sa famille.
4. **Le cinquième signe**, « cinq traits en cercle, qui se font face sans se toucher » : est-ce la bonne image pour le dessiner ?
5. **Le couplet de la Saga III** (la mer qui cache quelqu'un) : on le garde dès le niveau 20, ou on le garde pour plus tard ?
6. **Pip**, l'enfant de la trame, n'est pas utilisé ; Fleur reprend une partie de son rôle au Lac. Voulez-vous Pip au village ?
