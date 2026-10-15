# Osmose : trame narrative de la Saga I (v2, réalignée sur le projet réel)

> **Convention** : **[P]** = proposition (absente de la bible et du jeu). Le canon de la bible n'est pas modifié. Le contenu réel du jeu (Clairval, Aubin / Rose / Garance, Éclats, Osméria, 3 classes, 4 zones, 8 familles, 8 donjons, quête `first-lesson`) **prime** sur toutes les propositions.
> **Écart signalé** : la bible place le Ch1 sur les niveaux 1-15, mais les zones réelles vont jusqu'au niveau 26. Voir §4.0.

> **⚠ État du jeu au sprint 89 (commit `40ffc91`) : ce qui est déjà implémenté prime sur ce document.**
> - **La quête principale du Ch1 existe déjà (T56)**, donnée par **Ysaline**, une érudite arrivée à Clairval, debout à côté de la vieille pierre (le rocher au sud-ouest du village). Elle compte quatre quêtes : `whispering-stone` « La pierre qui murmurait » (niv. 1), `restless-beasts` « Les bêtes inquiètes » (niv. 2), `rumble-under-the-wallow` « Ce qui gronde sous la bauge » (niv. 5) et `four-trails` « Quatre pistes » (niv. 8, qui ouvre les 4 arcs). S'y ajoutent deux quêtes annexes par zone et une quête par donjon.
> - **Le §4 (la chaîne `osm_main_001` à `014`) est donc remplacé** : ne pas le réimplémenter. Le **§3** l'est en partie : la « Borne » est la vieille pierre, et l'érudit est Ysaline (pas Odalric). Pip, Liane et Corwin restent des propositions libres.
> - **Ce qui reste utile** : le §1 (le pitch et le fil « relation ≠ fusion » ; attention, les classes ont des sorts dans les quatre éléments depuis T34, elles ne sont pas « bi-élémentaires »), le §2 (le sens des zones et des familles, repris par `design-familles.md`), le §5 (les chapitres 2 à 5) et le §6 (les indices, à placer dans les quêtes existantes ou à venir, par exemple les crocs à la fêlure à cinq branches de « Les bêtes inquiètes »).
> - L'écart de niveaux du §4.0 a été tranché par le jeu : la chaîne principale du Ch1 s'arrête au niveau 8 ; le Lac et les Champs n'ont que des quêtes annexes. Le Disjoint (`design-familles.md` §4.9) peut devenir la suite de cette chaîne au niveau 27-30.

---

## 1. Pitch narratif de la Saga I

À l'Âge de l'Éveil, le monde se dérègle par petites touches. La borne de Clairval se couvre d'un symbole de plus, une arme d'entraînement vibre sous la colère de celui qui la tient, une berceuse que personne n'a apprise circule, et des bêtes portent deux éléments à la fois. Le joueur, une recrue de Clairval, remonte ces anomalies jusqu'aux héritages des quatre Primordiaux, puis jusqu'à la Disjonction : une machine rituelle oubliée qui sépare de force les éléments depuis la Fracture, et qui est en train de céder. En rassemblant les Fragments, il comprend qu'il répare le monde avec les outils de ceux qui l'ont brisé, et qu'il a existé autrefois une cinquième chose, ni élément ni dieu : une relation. La Saga se termine sur une crise contenue, pas sur une victoire.

- **Question centrale** : *« Ce qui nous sépare nous protège-t-il, ou nous tue-t-il lentement ? »*
- **Émotion visée** : d'abord une **inquiétude curieuse** (Ch1-2 : « qu'est-ce qui cloche ? »), puis un **malaise moral** (Ch3-5 : « et si la réparation était pire que la panne ? »).
- **Fil thématique [P] : relation ≠ fusion.** L'Accord est une relation entre éléments qui restent distincts. La Disjonction, en s'effondrant, produit au contraire des **fusions** instables.
- **Ancrage dans les classes réelles** : les trois classes sont déjà bi-élémentaires (Sentinelle Air/Eau, Garde Terre/Feu, Mage Feu/Eau). Dans leurs sorts, les deux éléments **coexistent sans se mélanger** : le joueur incarne une relation qui fonctionne. Les hybrides, eux, montrent une fusion qui échoue. Ce contraste est déjà dans le jeu, il suffit de le nommer.

---

## 2. La région de Clairval (Chapitre 1)

**Géographie réelle** : à l'ouest de Clairval s'étend le Bois de Clairval. Plus à l'ouest se trouve le Bois moisi, au nord du bois le Lac des Joncs, au sud du bois les Champs de Clairval.
**Lecture élémentaire [P]** : chaque zone a un élément dominant, sauf le Bois moisi où ils se mêlent. Le Feu est volontairement discret dans la région ; il apparaît dans les Champs, à travers la chaleur des ruches.

| Zone (réelle) | Niv. | Élément [P] | Anomalie de l'Éveil (canon) | Mystère local [P] | Donjon-jalon |
|---|---|---|---|---|---|
| Bois de Clairval | 3 | Terre | Ruines effacées | Les bêtes gardent un cercle de racines autour d'une pierre gravée enfouie | Bauge du Roi sanglier (éclat gravé) |
| Bois moisi | 10 | Les quatre, mêlés | Symboles inconnus, créatures multi-affinités | Une crypte absente de tous les registres, dont les morts se relèvent | **Crypte oubliée** (ruine effacée post-Fracture) |
| Lac des Joncs | 18 | Eau | Souvenirs faux | Une berceuse que personne n'a apprise, et un nom effacé du registre | Grotte du Roi grenouille (dernier couplet) |
| Champs de Clairval | 26 | Air (Feu secondaire, Terre en dessous) | Cartes qui changent, matériau inconnu | Les ruches surchauffent, les taupes creusent vers quelque chose sous les haies | Donjon des taupes (débris de la Disjonction), puis boss de chapitre |

### 2.1 Bois de Clairval (niv. 3, Terre) : « ce que la terre se rappelle »
Chênes anciens, bûcherons, arbres poussés en cercles parfaits. Pour Dhoram, la Terre est mémoire : les racines ont « gardé » une pierre que les hommes ont oubliée.

**Sangliers** (sanglier chargeur, marcassin lance-glands, vieux solitaire, laie qui soigne, roi des sangliers). Lieu : **Bauge du Roi sanglier**.
*Sens* : ils fouillent le sol et déterrent des éclats de pierre gravée, qu'ils mâchent parce que ces éclats vibrent. Cela les rend agressifs et durs comme la roche (le vieux solitaire, le tank, a la peau « pétrifiée »). *Anomalie* : le **Roi des sangliers** a un éclat gravé fiché dans la défense. C'est le premier objet de quête, qui porte le symbole Terre, incomplet.

**Loups** (loup gris, loup hurleur, grand mâle, louve qui protège la meute d'un bouclier, chef de meute). Lieu : **Tanière de la meute**.
*Sens* : depuis toujours, la meute tourne autour du cercle de racines, par un instinct de garde hérité. L'Éveil a réveillé la pierre enfouie et les loups sont devenus territoriaux. *Anomalie* : la fourrure du **chef de meute** ressemble à de l'écorce. Le **bouclier de la louve** est fait de terre durcie : c'est la Terre qui protège, un écho du futur Fragment de Dhoram (protection, ancrage).

### 2.2 Bois moisi (niv. 10, les quatre mêlés) : « ce qui pousse entre »
Champignons géants, mares, filaments blancs entre les troncs. La zone porte **l'ambiguïté centrale** du Ch1 à travers ses deux familles.

**Champignons** (champignon cogneur, vesse-de-loup, gros chapeau, morille guérisseuse, champignon ancestral). Lieu : **Champignonnière**.
*Sens* : c'est **la relation**. Leur mycélium relie des éclats de pierre du Bois, de l'eau des mares et des racines, **sans les fusionner**. C'est l'écho le plus proche de l'Accord dans la région, et pourtant ils font pourrir ce qu'ils touchent : l'Accord n'est peut-être pas « gentil ». Ils n'attaquent pas d'eux-mêmes, ils **défendent le réseau**. *Anomalie* : la **morille guérisseuse** soigne aussi bien les champignons que les loups blessés qui viennent mourir là. Le **champignon ancestral** n'est pas tué : vaincu, il se rétracte sous terre.

**Zombies** (zombie cogneur, cracheur, bouffi, rebouteux, colossal). Lieu : **Crypte oubliée**.
*Sens* : c'est **la fusion forcée**. La Crypte oubliée [P] est une ruine post-Fracture, effacée des cartes et des registres. Ses morts y ont été enterrés **séparés par élément**, dans quatre ailes scellées. Le sceau qui maintenait cette séparation (une petite cousine de la Disjonction) se fissure : les morts se relèvent avec des affinités **mêlées**, instables. *Anomalie* : le **rebouteux** « remet en place » des corps faits de morceaux qui ne vont pas ensemble. Le **colossal** est assemblé à partir des quatre ailes. Le mycélium tente de recouvrir les zombies, sans y parvenir : c'est la relation face à la contrainte.

### 2.3 Lac des Joncs (niv. 18, Eau) : « ce dont personne ne se souvient »
Grand lac, pontons, îlots, brume. Pour Névara, l'Eau garde les souvenirs et refuse l'oubli : le lac « rend » des souvenirs qui n'appartiennent à personne.

**Grenouilles** (bagarreuse, cracheuse, crapaud buffle, rainette guérisseuse, crapaud-roi). Lieu : **Grotte du Roi grenouille**.
*Sens* : la nuit, elles chantent une berceuse, et Clairval l'a « reçue » d'elles sans le savoir (faux souvenir collectif). *Anomalie* : le **crapaud-roi**, vaincu, chante le **dernier couplet**, que personne ne connaissait (indice Saga III, §6). La **rainette guérisseuse** soigne par l'eau, un écho du Fragment de Névara (adaptation, soin).

**Crabes** (pinceur, à bulles, cuirassé, bernard-l'ermite qui protège, crabe géant). Lieu : donjon des crabes (nom existant).
*Sens* : les crabes ramassent ce que le lac rejette, c'est-à-dire les débris d'un ponton et d'une barque engloutis. *Anomalie* : les **bernard-l'ermite** protègent leurs alliés avec des **coquilles faites de tessons gravés**. Le **crabe géant** porte sur le dos la proue d'une barque où figure un nom que le registre de Clairval a « lavé » : celui d'une passeuse dont personne ne se souvient. Le joueur collecte ces tessons, et la collecte est justifiée par l'histoire (Odalric les reconstitue).

### 2.4 Champs de Clairval (niv. 26, Air, Feu secondaire, Terre en dessous) : « ce qu'il y a sous les fleurs »
Fleurs, haies, ruches, vent. Rose y achète son miel. C'est la dernière zone du Ch1 et la plus « normale » en apparence, ce qui rend la chute plus forte.

**Abeilles** (ouvrière, archère, faux-bourdon, nourricière, reine). Lieu : donjon des abeilles (nom existant).
*Sens* : les ruches **surchauffent**. Les abeilles sont devenues Air **et** Feu, ce sont les premiers hybrides « vivants et stables » que croise le joueur. Elles ne sont pas mêlées, elles **coopèrent**, et la chaleur de la ruche est collective. *Anomalie* : la **reine** change d'élément dominant selon l'état de sa ruche. La **nourricière** distribue un miel tiède qui soigne. Pour le joueur, c'est une preuve que deux éléments peuvent coexister dans une créature sans la détruire, à condition de rester en relation.

**Taupes** (fouisseuse, lance-mottes, vieille taupe, mère taupe qui protège, taupe-roi). Lieu : donjon des taupes (nom existant).
*Sens* : les taupes creusent toutes **dans la même direction**, sous les haies, vers quelque chose qui vibre. *Anomalie* : la **taupe-roi** a creusé jusqu'à un **débris de métal inconnu**, qui est un morceau de la machine de la Disjonction (le joueur ne le sait pas encore ; c'est le même matériau que dans l'arc Kaor). Ce débris déclenche le boss de chapitre.

**Hybride rare [P] (seul nouveau monstre) : le Disjoint.** Il est instancié par une quête, dans la dernière salle du donjon des taupes. Le modèle est un kitbash des modèles existants (corps de taupe-roi, carapace de crabe, ailes d'abeille, crinière de loup), et il a **4 affinités**. Chaque tour, il change d'élément dominant et perd des PV à chaque changement : si on temporise, il s'effondre seul, mais il explose en zone. En mourant, ses quatre parties tombent en croix et dessinent **le 5e signe** de la Borne. C'est le jalon de fin du Ch1.

---

## 3. Clairval et ses PNJ

**La Borne de Clairval [P]** : une borne de carrefour sur la place, gravée de 4 symboles, chacun tourné vers une zone. Les élus de la Borne ne sont pas choisis par elle : n'importe qui peut la toucher. La face tournée vers le Bois moisi est **lisse**. Depuis des siècles, les gens de Clairval la touchent « pour la chance », et ce geste rituel a **poli et effacé** le 5e symbole. Au début du jeu, le 5e signe réapparaît en relief sur cette face.

| PNJ | Statut | Rôle dans la trame | Réplique |
|---|---|---|---|
| **Aubin** | Existant (guide) | Il accueille les nouveaux et **gardien informel de la Borne** : c'est lui qui enseigne le geste « de chance ». Il refuse de voir le changement, en écho à la faille de Dhoram. C'est le donneur de quêtes principal du Ch1. | « On touche la pierre pour la chance. On ne lui pose pas de questions. » |
| **Rose** | Existante (marchande) | Elle vend en Éclats et achète aux voyageurs, y compris des **cartes de la région qui ne concordent plus**. C'est le relais vers les Champs (son miel) et elle signale la surchauffe des ruches. | « Trois cartes du même chemin, trois chemins différents. Je les vends quand même, hein. » |
| **Garance** | Existante (maîtresse d'armes, porte est, quête `first-lesson`) | Dès la Première leçon, elle remarque que **l'arme d'entraînement réagit à la colère** de la recrue (anomalie canon). Elle valide la progression martiale, et c'est elle qui ouvre la porte vers les 4 routes en fin de chapitre. | « Frappe avec ta tête, pas avec ta rage. Ton arme, elle, a l'air de préférer la rage. » |
| **Frère Odalric** [P] | Nouveau | Copiste qui tient le registre de Clairval. C'est le PNJ « enquête » : il compare, consigne, reconstitue les tessons. | « Le registre ne ment pas. Il lui arrive seulement de se taire. » |
| **Pip** [P] | Nouveau | Enfant qui voit le 5e signe avant tout le monde et le dessine partout. Les champignons ne l'attaquent pas (indice ouvert). | « Il était déjà là, le signe. Vous regardiez pas. » |
| **Liane** [P] | Nouvelle (Lac des Joncs) | Passeuse sur les pontons. Elle connaît la berceuse et ne sait pas qui la lui a apprise. Elle fait le lien avec la passeuse effacée. | « Je l'ai toujours chantée. Mais je ne sais pas qui me l'a chantée. » |
| **Corwin** [P] | Nouveau (fin de Ch1) | Colporteur Haut-Vent. Il apporte une carte à 4 routes (et la trace pâle d'une 5e) qui ouvre le Ch2. | « Ta carte est fausse ? Non, elle est en avance. » |

---

## 4. Chaîne de quêtes principale du Chapitre 1

### 4.0 Écart de niveaux (à trancher)
La bible fixe le **Ch1 à 1-15** et le **Ch2 à 15-60**, mais le jeu réel va jusqu'au **niveau 26** (Champs).
- **Option A (recommandée)** : le Ch1 couvre les niveaux **1-30**, et le Ch2 passe à **30-60** (les arcs démarrent à 30, Ch3 à Ch5 inchangés). Le canon narratif ne bouge pas, seule la grille de niveaux change. Il faut l'accord de l'auteur.
- **Option B** : on garde le Ch1 à 1-15 (Bois et Bois moisi seulement). Le Lac et les Champs deviennent alors des **préludes régionaux** du Ch2 (le Lac pour Névara, les Champs pour Kaor via le débris). C'est plus fidèle à la bible, mais deux zones existantes se retrouvent hors du chapitre d'ouverture, et le Disjoint sort de la région.
- Les écarts 3, 10, 18, 26 laissent des trous de 7 à 8 niveaux entre zones. On les comble avec les donjons optionnels (marqués « opt. » ci-dessous) et du farm, ce qui est normal pour un MMO tactique.

### 4.1 Chaîne (option A)
**Types de quête utilisés (tous réalistes avec un système minimal)** : parler à un PNJ, interagir avec un objet, vaincre N monstres, terminer un donjon ou vaincre un boss, rapporter un objet. Les clés de donjon des quêtes principales sont **données par la quête une seule fois** ; les retours en donjon passent ensuite par le serrurier.

| id | Titre | Niv. | Zone | Type | Résumé | Objectif | Débloque |
|---|---|---|---|---|---|---|---|
| osm_main_001 *(= `first-lesson`, existante)* | Première leçon | 1 | Clairval, porte est | Combat tuto + dialogue | Combat d'entraînement avec Garance. **Ajout** : en fin de combat, l'arme vibre et Garance le remarque. | Gagner le combat, parler à Garance. | 002 |
| osm_main_002 | **La pierre qui murmurait** *(titre canon)* | 2 | Clairval | Enquête (dialogue + interaction) | Pip dit que la Borne « parle ». Le joueur la touche et voit le 5e signe. Aubin nie, Odalric note. | Interagir avec la Borne, parler à Pip, Aubin et Odalric. | 003 |
| osm_main_003 | Les bûcherons ne reviennent pas | 3 | Bois de Clairval | Combat | Les loups bloquent la coupe. Le joueur découvre le cercle de racines. | Vaincre 6 loups, interagir avec le cercle. | 004 |
| osm_main_004 | La défense du Roi | 6 | Bauge du Roi sanglier | Donjon / boss + rapport | Le roi des sangliers porte un éclat gravé. Comparé à la Borne, c'est le symbole Terre, mais incomplet. | Terminer la Bauge, rapporter l'éclat à Odalric. | 005 *(opt. : Tanière de la meute)* |
| osm_main_005 | Ce qui pousse entre | 10 | Bois moisi | Exploration (interaction) | Des filaments relient éclats, mares et racines. Le joueur suit 3 filaments, et les champignons défendent le réseau. | Interagir avec 3 nœuds de mycélium, vaincre 5 champignons. | 006 *(opt. : Champignonnière)* |
| osm_main_006 | La crypte que personne n'a creusée | 12 | Bois moisi | Enquête (dialogue) | Les filaments mènent à une entrée de crypte absente de tous les registres. Odalric est troublé. Aubin, qui l'a toujours vue, ne l'avait jamais « remarquée ». | Parler à Odalric et Aubin, recevoir la clé. | 007 |
| osm_main_007 | Crypte oubliée | 14 | Crypte oubliée | Donjon / boss | 4 ailes séparées par élément, un sceau fissuré, des morts aux affinités mêlées. Le colossal est fait des 4 ailes. Le joueur voit pour la 1re fois la fusion forcée. | Terminer la Crypte, interagir avec le sceau. | 008 |
| osm_main_008 | La berceuse que personne n'a apprise | 18 | Lac des Joncs | Enquête (dialogue) | 4 PNJ (dont Liane) chantent la berceuse avec des paroles différentes : ce sont des témoins contradictoires, qui annoncent l'arc Névara. | Parler à 4 PNJ, écouter les grenouilles (interaction nocturne ou simple). | 009 |
| osm_main_009 | Le dernier couplet | 20 | Grotte du Roi grenouille | Donjon / boss | Vaincu, le crapaud-roi chante la fin de la berceuse. | Terminer la Grotte, rapporter le couplet à Liane. | 010 |
| osm_main_010 | La passeuse au nom lavé | 23 | Lac des Joncs + donjon des crabes | Combat + rapport | Les tessons gravés des bernard-l'ermite et la proue sur le crabe géant portent un nom effacé du registre. Odalric le réécrit, et Aubin pleure sans savoir pourquoi. | Rapporter 6 tessons, terminer le donjon des crabes, parler à Odalric. | 011 |
| osm_main_011 | Les ruches brûlantes | 26 | Champs de Clairval | Combat + dialogue | Rose signale que le miel brûle. Les abeilles sont Air **et** Feu, mais stables, parce qu'elles coopèrent. C'est le contrepoint des zombies. | Vaincre 8 abeilles, parler à Rose puis à Garance (« comme ta classe, en fait »). | 012 *(opt. : donjon des abeilles)* |
| osm_main_012 | Sous les haies | 28 | Donjon des taupes | Donjon / boss | La taupe-roi a déterré un débris de métal inconnu, qui vibre comme la Borne. | Terminer le donjon, interagir avec le débris. | 013 |
| osm_main_013 | Le Disjoint | 30 | Donjon des taupes (salle de quête) | Boss de chapitre (groupe conseillé) | Le débris libère un hybride à 4 affinités qui se déchire. En mourant, il trace le 5e signe. | Vaincre le Disjoint, rapporter le tracé à la Borne. | 014 |
| osm_main_014 | Quatre routes | 30 | Clairval | Dialogue / choix | Le 5e signe s'efface de la Borne, et les 4 symboles s'illuminent chacun vers une région lointaine. Corwin apporte sa carte. Garance ouvre la porte est. Aubin : « Si tu pars, la pierre continuera de parler sans toi. » | Choisir sa première région. | Arcs Dhoram / Névara / Kaor / Élyon |

**Rythme** : combat, enquête, combat, donjon, exploration, enquête, donjon, enquête, donjon, combat/rapport, combat/dialogue, donjon, boss, choix. Entre deux donjons, il y a toujours une respiration narrative.

---

## 5. Vue d'ensemble Ch2 à Ch5

| Chapitre / arc | Fil narratif | Révélation | Mystère *(sait / soupçonne / à découvrir)* |
|---|---|---|---|
| **Ch2 Dhoram : Racines du passé** (30-60 selon l'option A) | Arbre géant, ruines sous les mines, gardiens de pierre qui appliquent des serments que plus personne ne comprend. | Les serments mentionnent **cinq** parties, et le 5e nom a été retiré à la pierre (comme sur la Borne). | *Sait* : on a effacé quelque chose. *Soupçonne* : les Primordiaux l'ont fait volontairement. |
| **Ch2 Névara : Souvenirs noyés** | Ville côtière, témoins contradictoires, sanctuaire submergé, gardienne des marées qui ment. | Elle ment **pour protéger** un secret. La mémoire a été triée, pas perdue (comme la passeuse du Lac). | *Sait* : la mémoire a été falsifiée. *À découvrir* : ce qu'elle cache (réservé à la Saga III). |
| **Ch2 Kaor : Forge des serments** | Matériau inconnu, armes qui réagissent à la colère, ouvriers malades. | C'est le métal du débris des Champs. Il amplifie les émotions parce qu'il est fait pour **contraindre**. | *Soupçonne* : une structure artificielle existe. |
| **Ch2 Élyon : Cartes impossibles** | Îles flottantes, carte à 5 routes dont une déchirée, voyageur qui connaît déjà le joueur. | La 5e route menait à un lieu retiré de la géographie (la future Chambre sans nom). | *Sait* : un lieu est caché. *À découvrir* : pourquoi le voyageur connaît le joueur. |
| **Ch3 La convergence des quatre** (60-80) | Les 4 représentants se réunissent, la menace est nommée « Disjonction », et ils ne s'accordent pas sur sa nature. Le joueur arbitre sans pouvoir trancher. | Les Fragments ne fonctionnent pleinement qu'**ensemble**. | *Sait* : la Disjonction existe. *Soupçonne* : elle est artificielle. |
| **Ch4 La Chambre sans nom** (80-95) | 4 portes / 5 ombres. Gardien : « Vous cherchez à réparer le monde avec les pouvoirs de ceux qui l'ont brisé. » | La 5e ombre est ce qui reste de l'Accord. Les Fragments viennent des Primordiaux, pas de l'Accord. | *À découvrir* : l'Accord est-il souhaitable ? |
| **Ch5 Le Cœur de la Disjonction** (95-100) | Une machine post-Fracture qui forçait la séparation s'effondre et crée des hybrides. Le boss impose l'équilibre par la contrainte. | Le sceau de la Crypte, le débris des Champs et le Disjoint venaient tous d'elle. **Contraindre n'est pas accorder.** Crise stabilisée, **pas de résurrection**. | *À découvrir (Saga II)* : rétablir l'Accord, et à quel prix. |

**Les 4 représentants du Ch3 [P]** : chacun porte la faille de son Primordial.
- **Bérold Serment-Fixe** (Enraciné) : juge des serments, digne et rigide. Il veut **réparer la machine** pour retrouver la stabilité.
- **Ilse des Basses-Marées** (Maréenne) : archiviste chaleureuse qui en sait plus qu'elle ne dit. Elle veut **tout préserver et ne rien révéler**.
- **Kesh Brasemain** (Forgelame) : forgeronne brillante et ambitieuse. Elle veut **exploiter** le métal de la Disjonction, quitte à tout casser.
- **Sorrel Vent-Vif** (Haut-Vent) : éclaireur drôle et insaisissable. Selon lui, **ce n'est pas leur problème** : il faut laisser s'effondrer et partir.

---

## 6. Indices semés [P]

| # | Indice | Où | Ce qu'il prépare |
|---|---|---|---|
| 1 | La face lisse de la Borne, polie par le geste « de chance » d'Aubin et des habitants : un oubli **rituel**, pas forcé. | Ch1, osm_main_002 | L'effacement de l'Accord par les Primordiaux (Ch4) |
| 2 | L'arme de la Première leçon vibre sous la colère. | Ch1, osm_main_001 | Le métal de Kaor (Ch2), la contrainte de la machine (Ch5) |
| 3 | Le sceau de la Crypte oubliée sépare les morts par élément. | Ch1, osm_main_007 | La Disjonction est une machine de séparation (Ch5) |
| 4 | Garance, à propos des abeilles : « comme ta classe, en fait. » Deux éléments qui coopèrent, sans fusion. | Ch1, osm_main_011 | Relation ≠ fusion, la nature de l'Accord |
| 5 | Le voyageur d'Élyon appelle le joueur « la recrue de la Borne ». | Ch2 Élyon | Le rôle de Clairval et de sa pierre (Saga II) |
| 6 | Le métal du Ch2 Kaor porte le **poinçon personnel de Kaor**, alors qu'il a été forgé **après** la Fracture. Les forgerons disent que c'est impossible. | Ch2 Kaor (description d'objet uniquement) | **Saga III** : un héritier du Forgeron est actif |
| 7 | Le dernier couplet du crapaud-roi : *« Dors, toi que la mer a caché, / Ni compté, ni nommé, ni oublié. »* La gardienne du Ch2 Névara s'arrête au même vers. | Ch1, osm_main_009, puis Ch2 Névara | **Saga III** : Névara a caché un enfant (rien n'est affirmé) |
| 8 | Pip n'est jamais attaqué par les champignons. Ce n'est jamais commenté. | Ch1, ambiance | Volontairement ouvert (Saga II ou III, ou fausse piste) |

---

## 7. Recommandation finale

**Par quoi commencer (scope solo, environ 3-5 semaines)** : faire une **vertical slice Clairval + Bois de Clairval**, soit les quêtes osm_main_001 à 004.
1. **Étendre `first-lesson`** plutôt que la remplacer : ajouter une ligne de Garance sur l'arme qui vibre, et brancher la suite. Conserver l'id existant (alias osm_main_001) pour ne pas casser les sauvegardes.
2. **Étendre le système de quêtes JSON avec 5 types d'étapes seulement** : `talk`, `interact`, `kill` (N × famille), `dungeon_complete`, `deliver`. Ajouter un champ `grantsKey` pour donner une clé de donjon une seule fois. Ces 5 briques suffisent à toute la chaîne du Ch1.
3. **Ajouter la Borne** : un objet interactif avec 2 états visuels, avec ou sans 5e signe.
4. **Ajouter 2 PNJ** (Odalric, Pip), qui réutilisent les modèles d'Aubin ou de Rose.
5. **Monstres** : aucun nouveau monstre avant osm_main_013. Seules les descriptions, les répliques et les objets de quête (éclat du roi des sangliers) changent.
6. **Écriture** : 3 à 5 lignes maximum par réplique. Les anomalies se **montrent** avant d'être expliquées.

Le Bois moisi vient ensuite. C'est la zone-clé : si les joueurs ne ressentent pas « champignons = lien, zombies = fusion forcée », la thèse de la Saga ne passera pas. **Il faut la faire tester par 3 à 5 joueurs avant d'écrire le Lac.** Le Disjoint arrive en dernier : c'est le seul asset neuf, et il est fait en kitbash.

**Risques** :
- Les 7 à 8 niveaux entre zones risquent de donner l'impression d'un « grind sans histoire ». Prévoir 1 ou 2 quêtes secondaires par zone, reliées aux donjons optionnels.
- Le Feu est quasi absent de la région, alors que la Garde et le Mage sont des classes Feu.
- Le Ch2 en ordre libre sur 30 niveaux demandera du scaling ou des paliers.

**3 questions à trancher** :
1. **La grille de niveaux** : Ch1 = 1-30 (option A, recommandée) ou garder 1-15 avec le Lac et les Champs en préludes du Ch2 (option B) ?
2. **Les classes bi-élémentaires sont-elles « normales » dans le monde, ou un phénomène de l'Éveil ?** Si elles sont nouvelles, les joueurs deviennent eux-mêmes une anomalie, ce qui est fort mais engageant. Je recommande « normales mais rares autrefois », évoqué par une seule réplique de Garance, sans plus.
3. **Les clés de donjon de la quête principale** : sont-elles offertes une fois (recommandé) ou faut-il passer par le serrurier, ce qui risque de bloquer la progression narrative derrière un métier ?
