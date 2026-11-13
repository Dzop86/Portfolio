# Le Nécromancien, deuxième partie : l'Invocateur macabre et l'Occultiste *(12 novembre 2026, sprint 215, à valider par Charles)*

Ce document prépare la story du sprint 215 : « En tant que Nécromancien, je deviens aussi Invocateur macabre ou Occultiste, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 « Invocateur macabre : beaucoup de morts jetables ; Occultiste : aucune invocation, sang et âmes », §1.4 « la branche Occultiste n'invoque rien », §4 les poisons cumulés), de `classes-canon.md`, de `classes-liste.md` §8.2 et §8.3, et de la première partie (`classes-necromancien.md`, validée). Avec lui, le Nécromancien a ses trois branches, comme les huit autres classes écrites : `Arena.Writing` est vide.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **L'Invocateur macabre jette ses morts.** Des Goules, trois au plus, chacune levée sur un cadavre, fragiles (12 % des PV, 30 % des caractéristiques) : le contraire du grand mort du Maître des morts. La Curée marque l'ennemi que ses morts frappent plus fort.
- **L'Occultiste n'invoque rien** (§1.4 de la conception) : ni créature, ni cadavre, ni sort qui vise un mort. Ses poisons d'un même élément se cumulent, jusqu'à 3 doses (la règle `stack` du Voleur, S205 [V]). Il frappe lui-même : le sang (Sanguinomancien) ou l'achèvement (Thanaturge).
- **Une seule règle neuve dans le moteur** : un coup lancé sur l'une de ses créatures l'épargne (`Fight.Spares`), pour le Sacrifice, dont la Goule est le foyer. L'IA ne la compte plus dans ce que le coup coûte (`Ai.Value`).
- **Les choix des lignées** : le Maître des goules les nourrit (18 % et 40 %) et les fait manger un cadavre (Ripaille) ; le Commandant des morts les dépense (Sacrifice). Le Sanguinomancien paie de ses PV un coup qui lui en rend ; le Thanaturge achève (+25 % sous 30 % de PV) d'un coup lourd.

## 1. L'Invocateur macabre (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Invocateur macabre** (`macabre-caller`) | Goule nourrie à 12 % des PV, 30 % des caractéristiques | **Goule** (`path-ghoul`, 2 PA, 1-4, **sur un cadavre**, trois au plus, relance 2) ; **Curée** (`path-quarry`, 2 PA, 1-6, un ennemi que ses morts frappent de 25 % de plus, 2 tours, relance 1) |

La Goule (`ghoul`) : modèle du Mort levé à 70 %, 6 PA, 4 PM, initiative 70, **Morsure de goule** (`ghoul-bite`, 3 PA, au contact, 5-8, deux fois, dans l'élément du Nécromancien). Elle se lève dans le corps du monstre, comme le Mort levé (`wearsCorpse` [V]). Le Mort levé du kit et les Goules sont des espèces différentes : un Invocateur macabre peut tenir son mort et trois Goules.

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Maître des goules** (`ghoul-master`) | Goules nourries à 18 % et 40 % ; **Ripaille** (`path-gorging`, 2 PA, 1-5, **sur un cadavre**, cercle de 1 : soin 15-20 et +20 % de dégâts aux alliés, 2 tours, relance 2) | **Seigneur des goules** (`ghoul-lord`, Faster : Goule relancée en 1 tour) ; **Dévoreur d'âmes** (`soul-devourer`, Extends : Ripaille donne aussi 8 PV par tour, 2 tours) |
| **Commandant des morts** (`dead-commander`) | **Sacrifice** (`path-offering`, 3 PA, 1-6, **sur une de ses Goules**, cercle de 1 : un coup autour d'elle, qui l'épargne, relance 2) | **Général des morts** (`dead-general`, Extends : −1 PM aux ennemis touchés) ; **Empereur funeste** (`doom-emperor`, Faster : Sacrifice relancé en 1 tour) |

## 2. L'Occultiste (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Occultiste** (`occultist`) | ses poisons d'un même élément se cumulent, 3 doses au plus (`stack`) | **Malédiction** (`path-hex`, 3 PA, 1-6 : −15 % de résistance et de dégâts à un ennemi, 2 tours, relance 2) ; **Flétrissure** (`path-blight`, 3 PA, 1-6 : un coup et un poison de 6, 3 tours, relance 1) |

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Sanguinomancien** (`sanguimancer`) | **Sang bouillant** (`path-boiling-blood`, 4 PA, 1-4, **10 PV**, rend 20 % de ses dégâts, relance 2) | **Seigneur du sang** (`blood-lord`, Extends : Sang bouillant donne aussi 8 % de dégâts, 2 tours) ; **Hémarque** (`hemarch`, Form : Sang bouillant laisse 8 % de dégâts pour tout le combat) |
| **Thanaturge** (`thanaturge`) | **exécution** : +25 % sur un ennemi sous 30 % de ses PV (la règle du Bourreau, S207 [V]) ; **Moisson** (`path-reaping`, 5 PA, 1-4, relance 2) | **Maître des âmes** (`soul-master`, Extends : Moisson rend 30 % de ses dégâts) ; **Faucheur éternel** (`eternal-reaper`, Faster : Moisson relancée en 1 tour) |

## 3. Ce qui sépare les trois branches

| Branche | Ses morts | Vérifié |
|---|---|---|
| Maître des morts | un seul, nourri et grandissant | `NecromancerTests` |
| Invocateur macabre | trois Goules fragiles, chacune un cadavre | `NecromancerBranchesTests.AGhoul_RisesOnACorpse_…` |
| Occultiste | aucun : aucun sort de la branche n'invoque, ne prend un cadavre ni ne vise un mort | `NecromancerBranchesTests.TheOccultist_RaisesNothing_AndItsPoisonsStack` |

## 4. Réglages en route

- **Le Sacrifice** frappait d'abord sa propre Goule, au cœur de la zone, et l'IA comptait ce coup contre lui. Un coup lancé sur l'une de ses créatures l'épargne désormais (`Fight.Spares`, `Ai.Value`) ; aucun autre sort du jeu n'était un coup qui vise une créature, les empreintes des autres classes ne bougent pas.
- **Sang bouillant** à 4 PA, de 1 à 5 cases, relance 1 : le Sanguinomancien montait à +14,8 de la moyenne de la classe au niveau 60. Le coût en PV et le drain n'y changeaient rien (déjà passés à 10 PV et 20 %) ; échanger son sort avec la Moisson du Thanaturge échangeait aussi les scores : c'est le sort. La relance 1 le laisse revenir un tour sur deux, la Moisson un sur trois, et le barème ne rend que 25 % par tour de relance. À **relance 2 et 1-4 cases**, comme la Moisson : +8,4 au niveau 60 (à la borne).
- **Les finales du Sanguinomancien** à 15 % de dégâts : +10,2 (Seigneur du sang) et +15,2 (Hémarque) sur leur lignée au niveau 100, quand les autres finales de la classe restent à 6 points. L'Occultiste frappe lui-même, ses dégâts pèsent plus que pour une classe dont les créatures frappent. À **8 %** : +3,5 et +4,6.
- **Le poison de la Flétrissure** à 5 au lieu de 6 : rien de lisible sous le bruit du banc (±3 points à 320 combats) ; laissé à 6.

## 5. Mesures

Banc (`Bench.Run`, sorts de voie rangés) sur les 16 groupes de 4 et 5 monstres, **40 combats par groupe** aux niveaux 35 à 60 (640 combats par voie), 30 au niveau 100.

| Niveau | Voie | Points | Sur le héros sans voie | Sur la moyenne |
|---|---|---|---|---|
| 35 | Maître des morts, Invocateur macabre, Occultiste | 63,1, 65,0, 64,8 | +24,5 à +26,4 | −1,2 à +0,7 |
| 45 | Maître des morts, Invocateur macabre, Occultiste | 66,3, 65,0, 68,8 | +20,8 à +24,6 | −1,7 à +2,1 |
| 50 | Liche, Seigneur des os | 67,8, 61,7 | +24,0, +17,9 | +5,9, −0,2 |
| 50 | Maître des goules, Commandant des morts | 60,3, 55,6 | +16,5, +11,8 | −1,6, −6,3 |
| 50 | Sanguinomancien, Thanaturge | 68,6, 57,7 | +24,8, +13,9 | +6,6, −4,2 |
| 60 | Liche, Seigneur des os | 37,8, 39,1 | +15,8, +17,1 | −1,9, −0,6 |
| 60 | Maître des goules, Commandant des morts | 34,1, 31,6 | +12,1, +9,6 | −5,6, **−8,1** |
| 60 | Sanguinomancien, Thanaturge | 48,1, 47,7 | +26,1, +25,7 | **+8,4**, +8,0 |
| 100 | Liche, Seigneur des os et leurs finales | 36,5 à 38,8 | +23,6 à +25,9 | −2,3 à 0,0 |
| 100 | Maître des goules, Commandant des morts et leurs finales | 32,7 à 33,5 | +19,8 à +20,6 | −6,1 à −5,3 |
| 100 | Sanguinomancien et ses finales | 44,2, 47,7, 48,8 | +31,3 à +35,9 | +5,4 à +10,0 |
| 100 | Thanaturge et ses finales | 40,0, 45,4, 45,8 | +27,1 à +32,9 | +1,2 à +7,0 |

Héros sans voie : 38,6 au 35, 44,2 au 45, 43,8 au 50, 22,0 au 60, 12,9 au 100.

**Ce que l'IA joue** (par combat, niveau 60) : Goule 0,5 à 0,6, Curée 0,9 à 1,0, **Ripaille 0,15**, **Sacrifice 0,14** ; Flétrissure 1,7 à 2,0, Sang bouillant 1,8, Moisson 1,7, **Malédiction 0,05 à 0,2**.

**Les bornes (§7.3 de la conception).**

- **B5** (rang 2 à 35 et 45, ±8 de la moyenne) : **tenue**, de −1,7 à +2,1.
- **B6** (rang 3 à 50 et 60, 8 points au plus entre lignées, ±8 de la moyenne) : à 50, la moyenne est tenue (−6,3 à +6,6), et les lignées du Maître des morts (6,1) et de l'Invocateur macabre (4,7) aussi ; **pas celles de l'Occultiste (10,9)**. À 60, les écarts entre lignées sont tenus (2,5, 1,3 et 0,4), la moyenne **à la borne** : Sanguinomancien +8,4, Commandant des morts −8,1, dans le bruit du banc (±2,5). Q3.
- **B8** (rang 4, indicatif) : les finales restent à 2,3 points de leur lignée, sauf celles de l'Occultiste (+3,5 à +5,8). Les finales du Commandant des morts et le Seigneur des goules **ne changent aucun combat** : le Sacrifice (0,07 par combat au niveau 100) et la Goule y sont trop peu lancés : ajoutées à `CiLevelsTests.Unchanged`.
- La Ripaille, le Sacrifice et la Malédiction sont sous 0,5 par combat : le banc lit « non concluant » pour toutes les voies de l'Invocateur macabre et de l'Occultiste. La règle du banc (T152) : réparer l'IA, pas le sort. Q2.

## 6. À trancher par Charles

- **Q1. Les noms** : Goule, Morsure de goule, Curée, Ripaille, Sacrifice, Malédiction, Flétrissure, Sang bouillant, Moisson ; noms de travail, au scénariste. Les titres viennent de ta liste.
- **Q2. Les sorts peu joués** (Ripaille, Sacrifice, Malédiction : 0,05 à 0,2 par combat). L'IA les juge mal : un soin et des dégâts autour d'un cadavre, un coup autour d'une Goule qui doit d'abord rejoindre les ennemis, un affaiblissement. a) **les reprendre au jalon 3 (S219), avec toute la classe (Reco)** ; b) maintenant, ce qui rouvre les mesures du sprint.
- **Q3. B6 pas tout à fait tenue** : à 50, 10,9 points entre Sanguinomancien et Thanaturge ; à 60, le Sanguinomancien et le Commandant des morts à 0,4 et 0,1 au-delà de ±8. a) **les régler au jalon 3, après Q2 : l'Invocateur macabre monte quand l'IA joue ses sorts, la moyenne avec (Reco)** ; b) affaiblir encore Sang bouillant maintenant (le Sanguinomancien redescend sous le Thanaturge à 50).
- **Q4. Un coup lancé sur sa créature l'épargne** (`Fight.Spares`) : a) **oui, c'est ce qui fait de la Goule un foyer (Reco, fait)** ; b) le Sacrifice tue sa Goule et frappe plus fort (un vrai sacrifice : un cadavre de plus à prendre, mais une Goule de moins).
