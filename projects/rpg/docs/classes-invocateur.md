# L'Invocateur, première partie : les créatures et l'ordre *(10 novembre 2026, sprint 211, validé par Charles : recommandations, D100)*

Ce document prépare la première story du sprint 211 : « En tant qu'Invocateur, l'IA joue mes créatures et un sort marque leur cible, 3 créatures au plus ; xUnit. PloufInvocateur créé. » Le Familieriste vient au sprint 212, le Démoniste et le Conjurateur au sprint 213. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §1.4 ce qui le sépare du Nécromancien, §4 les invocations commandées, Q4 a) et de `classes-liste.md` §10.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Huitième classe neuve**, id `summoner` (celui de la page de création [V, `Lobby.Roster`]). 54 PV + 3, 8 PA, 4 PM, initiative 10, **budget de dégâts 0,75**, de 1 à 5 cases. La question de son tour : « Qui je lève, et sur qui je l'envoie ? »
- **Son moteur, les créatures nourries** (règles de classe `summon-share`, celles des voies de l'Archer [V]) : chacune de ses créatures prend une part de ses PV et de ses caractéristiques, et frappe dans son élément le plus fort. Elles grandissent avec lui : un Faucon à 30 PV fixes ne vaudrait plus rien au niveau 60.
- **Deux créatures dès le niveau 1** : l'Appel du molosse en lève deux au plus.
- **L'ordre** (niveau 5, Q4 a de la conception) : un sort marque un ennemi ; les créatures de l'Invocateur vont sur lui et le frappent de 25 % de plus, 2 tours. Une seule cible marquée à la fois par Invocateur. Les créatures restent jouées par l'IA, même pour un joueur [V, `FightController.IsPlayerTurn`] : le tour reste court.
- **3 créatures au plus par héros**, toutes sortes confondues (`classes-conception.md` §4, le risque de la durée d'un combat). Aucun héros n'en avait plus de 3 en jeu jusqu'ici, sauf un Illusionniste à Leurre, Braise, Image et Double : il en garde 3.
- **Aucune voie ce sprint** : à 30, l'Invocateur n'a rien à choisir jusqu'au sprint 213. Rien n'est livré avant la build du sprint 219.

## 1. Ce qui le sépare des autres

| Avec | Règle (§1.4 de la conception) | Vérifié |
|---|---|---|
| Nécromancien | il **investit** : ses créatures sont chères (3 à 5 PA), durables, nourries ; aucun sort ne lit un cadavre | test sur le kit |
| Archer | le Faucon reste à l'Archer ; l'Invocateur ne garde pas ses distances, il place ses créatures | kits disjoints [V, `DataTests`] |
| Prêtre | aucun soin ; il protège ses créatures par un bouclier, pas par des PV | test sur le kit |

## 2. Les créatures

Nourries par la règle de classe : une part des PV maximum et des caractéristiques de l'Invocateur, dans son élément [V, `Fight.Summon`, `SummonRule`].

| Créature (id) | Niveau | Au plus | Modèle | PV | PA, PM, init. | Sort |
|---|---|---|---|---|---|---|
| **Molosse** (`hound`) | 1 | 2 | loup | 20 % | 6, 4, 60 | Croc (`hound-fang`) : 3 PA, au contact, 2 fois |
| **Feu follet** (`wisp`) | 30 | 1 | abeille | 12 % | 6, 3, 70 | Lueur (`wisp-glow`) : 3 PA, 1 à 4 cases, 2 fois |
| **Colosse** (`colossus`) | 55 | 1 | golem | 40 % | 4, 3, 40 | Poing du colosse (`colossus-fist`) : 4 PA, au contact, repousse d'une case |

Caractéristiques : 30 % de celles de l'Invocateur pour toutes, réglées au banc (§5). Les modèles sont ceux des monstres, plus petits ; la refonte artistique les remplacera (`classes-conception.md` §4, limite des modèles).

## 3. L'ordre

- **Ordre** (`command`, neutre, 2 PA, 1 à 6 cases, sans ligne de vue, relance 1) pose l'état `Ordered` (25 %, 2 tours) sur un ennemi.
- **Ses créatures** frappent ce porteur de 25 % de plus (`Fight.Damage`, comme un pourcentage de talent) et le visent d'abord : l'IA d'une créature compte double ses coups sur lui, et marche vers lui plutôt que vers l'ennemi le plus proche.
- **Une seule cible** : marquer un autre ennemi retire la marque du premier (comme Électrisé, sprint 140 [V]).
- **L'IA de l'Invocateur** compte l'ordre pour ce qu'il ajoute : le quart de ce que ses créatures à portée du porteur lui feraient en un tour, sur ses 2 tours. Sans créature, il ne vaut rien.

## 4. Les 24 sorts

| Niv. | Sort (id) | Élément | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| 1 | Motte `clod` | Terre | 3 | 1-5 | | |
| 1 | Escarbille `cinder` | Feu | 3 | 1-5 | | |
| 1 | Goutte `droplet` | Eau | 3 | 1-5 | | |
| 1 | Murmure `murmur` | Air | 3 | 1-5 | repousse d'une case | |
| 1 | Appel du molosse `call-hound` | — | 3 | 1-2 | lève un Molosse (2 au plus) | 1 |
| 5 | Ordre `command` | — | 2 | 1-6 | marque la cible de ses créatures | 1 |
| 10 | Pierre de lien `binding-stone` | Terre | 4 | 1-4 | −1 PM | |
| 15 | Marque ardente `searing-brand` | Feu | 4 | 1-4 | poison de Feu 4, 3 tours | |
| 20 | Bourbier `mire` | Eau | 4 | 1-4 | −1 PM | |
| 25 | Souffle `breath` | Air | 4 | 1-5 en ligne | | |
| 30 | Feu follet `call-wisp` | — | 3 | 1-3 | lève un Feu follet | 2 |
| 35 | Grondement `rumble` | Terre | 5 | 2-5, cercle 1 | | |
| 40 | Bûcher `pyre` | Feu | 5 | 2-5, croix 1 | | |
| 45 | Ressac lointain `far-undertow` | Eau | 5 | 1-4 en ligne | attire d'une case | |
| 50 | Grain `squall` | Air | 5 | 2-5, cercle 1 | −1 PA | 1 |
| 55 | Colosse `call-colossus` | — | 5 | 1-2 | lève un Colosse | 4 |
| 60 | Obélisque `obelisk` | Terre | 6 | 2-5 | | 1 |
| 65 | Tempête de feu `firestorm` | Feu | 6 | 2-5, cercle 1 | | 1 |
| 70 | Appel des fonds `deep-call` | Eau | 6 | 2-5, croix 1 | | 1 |
| 75 | Vortex `vortex` | Air | 6 | 2-6 | | 2 |
| 80 | Exhortation `rouse` | — | 3 | 0-5, cercle 1 | alliés +25 % de dégâts, 2 tours | 3 |
| 85 | Permutation `switch` | — | 2 | 1-6 | échange sa place avec un allié | 2 |
| 90 | Voile `veil` | — | 3 | 0 | bouclier 30 sur soi | 4 |
| 95 | Pas d'appel `calling-step` | — | 1 | 0 | +2 PM, 1 tour | 2 |

Les dégâts suivent le barème (`tools/spells_gen.py`) à 0,75. Noms de travail, au scénariste.

## 5. Mesures

- **Le banc sans voie** (parts réglées : 20, 12 et 40 % des PV, 30 % des caractéristiques) : 7,5 au niveau 10, 34 au 30, 57 au 35, 62 au 45 et au 50, 54 au 70, 29 au 100. Les parts de la conception donnaient 69 au 30 et 95 au 50.
- **Les exigences en solo de la CI** : la grande salle de la Bauge et les hordes de la Forêt tenues ; le Disjoint 100 sur 100 (Q4).
- **Les groupes de référence** (hordes au niveau 45) : Guerrier + Invocateur 70 %, Prêtre + Invocateur 70 %, Berserker + Invocateur 66 %, Archer + Voleur + Invocateur, Paladin + Mage + Invocateur et Guerrier + Prêtre + Archer + Invocateur 100 %.
- **Les autres classes** : leurs combats de niveau 10 ne bougent pas avec l'Invocateur seul ; l'IA de D99 (seconde story) change ceux de l'Archer, du Guerrier, du Mage et du Prêtre.

## 6. Tranché par Charles (« go reco », D100)

- **Q1. Les noms** des 24 sorts, des 3 créatures et de l'état « Ordre » : noms de travail, au scénariste.
- **Q2. La limite de 3 créatures** : a) **par héros, toutes classes (Reco)** : la durée d'un tour, et un Illusionniste ne perd que son 4ᵉ ; b) pour l'Invocateur seulement.
- **Q3. Les modèles des créatures** : a) **ceux des monstres, plus petits, d'ici la refonte artistique (Reco)** ; b) des humains teintés, comme la Braise du Mage.
- **Q4. Le Disjoint seul au niveau 30 est devenu trop facile.** L'Invocateur le bat 100 fois sur 100 (ses créatures prennent ses coups). Surtout, avec l'IA de D99 (un retrait de PM vaut le coup qu'il empêche), l'Archer passe de 76 à 100 et le Prêtre de 62 à 98 : ils le tiennent à distance pendant qu'il s'use, comme le ferait un joueur. Moyenne des 8 classes : 98,75, pour un plafond de 90. a) **plafond levé en attendant, le plancher de 30 gardé (fait), puis rendre le Disjoint plus dur au jalon 3 : Tenir bon (indéplaçable) ou un coup à distance (Reco)** ; b) l'affaiblir maintenant ; c) baisser la valeur des retraits de PM, au prix d'une IA qui joue moins bien.
