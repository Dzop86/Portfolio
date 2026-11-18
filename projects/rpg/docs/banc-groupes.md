# Le banc de groupes, B7, B16 et les zones de 46 à 100 *(17 novembre 2026, sprint 222, validé par Charles : D102)*

La story : « En tant que Charles, je sais quels sorts de groupe l'IA joue en groupe : un banc de 2 et 3 héros qui compte leurs lancers ; B7 retirée des bornes, le seuil de 1 seconde par tour de 3 Invocateurs vérifié par `--durations` ; les zones de 60 à 100 chiffrées. » Elle vient du jalon 3 (`docs/jalon-3.md`, Q2 à Q5, D101).

## 1. Le banc de groupes

`Rpg.Sim --matrix --casts --party-size 2|3` (nouveau, `Bench.Run(..., partySize)`) : chaque voie de chaque classe, son héros rangé comme au banc seul, avec des alliés nus et fixes, pour que les voies d'une classe se comparent entre elles :

- **à 2**, un Guerrier devant (un Paladin si le héros est Guerrier) ;
- **à 3**, un Archer derrière en plus (un Mage si le héros est Archer) ;
- contre les **hordes de 6 monstres et plus** de la famille synthétique (9 groupes), 20 combats par groupe, aux niveaux 35, 45 et 50 ; seuls les sorts de voie du héros sont comptés (les alliés, d'une autre classe et sans voie, n'en ont pas).

**Un premier essai a été jeté.** Le duo jouait d'abord les groupes de 4 et 5 du banc seul : **100 % de victoires** à tous les niveaux. Un allié qui ne tombe jamais n'a besoin ni d'une Résurrection ni d'un bouclier, et la mesure ne disait rien. Contre les hordes, le duo gagne 68 à 73 % (le héros seul : 63 à 66 % au banc) : des combats disputés. **Le trio reste trop fort** : 96 à 99 % contre les hordes, les plus gros groupes de la famille. Il n'y a pas plus dur à lui opposer sans monter le niveau des monstres. **Le duo est la mesure de référence**, le trio un complément.

| Moyenne des voies (points) | 35 | 45 | 50 |
|---|---|---|---|
| Seul, groupes de 4 et 5 | 63,3 | 65,1 | 66,0 |
| Duo, hordes | 68,3 | 70,1 | 72,7 |
| Trio, hordes | 96,3 | 98,0 | 98,8 |

*Durée : 15 minutes pour le duo, 14 pour le trio, sur 12 cœurs.*

## 2. Les 13 sorts jamais joués seuls

Lancers par combat, au mieux sur les voies et les niveaux qui les ont (seuil de lecture : 0,5) :

| Sort | Seul | Duo | Trio | Ce que c'est |
|---|---|---|---|---|
| Arrachement (`path-wrench`) | 0,6 | **1,4** | 1,2 | traction |
| Coup d'arène (`path-arena-strike`) | 0,6 | **0,9** | 0,9 | coup |
| Rempart d'os (`path-bone-rampart`) | 0,3 | **0,7** | 0,6 | protection d'allié |
| Résurrection (`path-resurrect`) | 0,0 | **0,5** | 0,1 | sort d'allié |
| Lame enduite (`path-envenom`) | 0,2 | **0,5** | 0,6 | poison |
| Bouclier de la foi (`path-shield-of-faith`) | 0,0 | 0,3 | 0,4 | bouclier d'allié |
| Fiole de peste (`path-plague`) | 0,0 | 0,2 | 0,1 | poison en statut |
| Sol grondant (`path-rumbling-ground`) | 0,0 | 0,1 | 0,2 | glyphe partagée |
| Rappel (`path-recall`) | 0,0 | 0,1 | 0,1 | échange avec le Familier |
| Feinte (`path-feint`) | 0,0 | 0,1 | 0,1 | statut sur soi |
| Cercle de fer (`path-iron-ring`) | 0,0 | 0,0 | 0,1 | retrait de PM autour de soi |
| Cabriole (`path-vault`) | 0,0 | 0,0 | 0,0 | bond |
| Haie (`path-hedge`) | 0,0 | 0,0 | 0,0 | glyphe partagée |

*(Arrachement et Coup d'arène passent 0,6 seuls ici, à 20 combats par groupe, contre moins de 0,5 au jalon 3, à 30 : ils étaient au bord.)*

**Ce que ça dit :**

- **Le groupe en fait jouer 5 sur 13** : la Résurrection et le Rempart d'os, deux vrais sorts de groupe, et trois sorts qui demandaient seulement des combats plus longs. Sur l'ensemble des sorts de voie, le duo en laisse **26 sous 0,5** contre 33 seul, et **59 voies concluantes sur 120** contre 53.
- **8 restent muets, et ce n'est plus faute d'alliés.** Quatre sont de vrais sorts de groupe que l'IA ne valorise pas, même avec un allié à protéger : le Bouclier de la foi, les deux glyphes partagées (Haie, Sol grondant) et le Rappel. Les quatre autres ne sont pas des sorts de groupe : la Feinte et la Cabriole du Voleur (un statut sur soi, un bond), le Cercle de fer (retrait de PM) et la Fiole de peste (un poison en statut, quand la Lame enduite est jouée). **C'est donc l'IA**, l'option b de la Q4 du jalon 3, qui reste à faire.
- **La Résurrection à 0,5 n'est jouée qu'en duo** (0,1 en trio) : à trois, on gagne trop vite pour qu'un allié tombe. Le seuil est atteint de justesse.
- Le banc de groupes ne remplace pas le banc seul pour les bornes B5 et B6 : les alliés nus pèsent autant que la voie, et le trio gagne presque tout.

## 3. B7 retirée

Déjà faite par D101 dans la CI et la conception (`classes-conception.md` §7.3) : depuis le jalon 2 (D98), `TemplateTests` compare les voies d'une classe entre elles, et le seul garde-fou sur le héros sans voie est qu'il gagne entre 5 et 85 % au banc de la CI. Ce sprint n'a rien eu à retirer du code ; la colonne « sur le héros sans voie » du banc reste affichée, pour l'information.

## 4. B16, 1 seconde par tour

`Rpg.Sim --durations` écrit maintenant le verdict de B16 sous son tableau (`Program.RoundBudget`, seuil `RoundBudgetMs` = 1 000 ms). Au niveau 45, 60 combats par ligne, deux mesures à la suite, la machine au repos :

| Ligne | Jalon 3 | Mesure 1 | Mesure 2 |
|---|---|---|---|
| 3 Invocateurs (27 combattants) | 810 ms | 313 ms | 286 ms |
| 3 Nécromanciens | 523 ms | 311 ms | 192 ms |
| 3 Voleurs | 195 ms | 184 ms | 146 ms |

**B16 tenue**, à 0,3 seconde. Les victoires, les tours et le nombre de combattants sont exactement ceux du jalon 3 : seul le temps a bougé. Au jalon 3, la mesure avait sans doute tourné pendant le banc complet. **Le temps dépend de la charge de la machine, et d'une mesure à l'autre de 10 à 40 %** : c'est pourquoi le seuil n'est pas un test de la CI (ses machines sont partagées), mais une mesure à refaire à chaque jalon, la machine au repos.

## 5. Les zones de 46 à 100, chiffrées

**Un constat d'abord :** les zones s'arrêtent au **niveau 45**, pas à 60. Les Aïeux (31 à 45) sont la dernière ; aucun monstre, carte ou donjon n'existe au-dessus (`monsters.json`). Les classes, elles, montent à 100.

**Le coût d'une zone, mesuré sur la dernière** (la Forêt des Aïeux, S131 à S135) : 25 cartes et 10 ressources (5 points), deux familles avec chef, butin et panoplie (3 + 3), deux donjons à trois salles (5), l'artisanat des niveaux de la zone (3), soit **19 points**, sans compter les glyphes (S130, 5 points), une mécanique neuve. Elle a été réglée pour 3 classes ; il en faut maintenant 10 (chaque famille et chaque grande salle gagnées seul et nu par chaque classe, comme les Aïeux à 41) : **+1 point par famille et +1 pour les donjons, soit 22 points par zone.**

| Option | Contenu | Points | Sprints |
|---|---|---|---|
| a) **Une zone de 46 à 60, puis un jalon** | 25 cartes, 2 familles, 2 donjons, artisanat 50 à 60 ; jalon 4 : banc à 60, playtest | **22 + 2 = 24** | 6 |
| b) Les quatre zones (46-60, 61-75, 76-90, 91-100) | comme a), quatre fois ; les finales à 100 réglées au banc (hors bornes depuis S220) ; deux jalons | 88 + 4 + 4 = **96** | 24 |
| c) Deux grandes zones (46-72, 73-100), trois familles chacune | moins de cartes, plus de niveaux par zone ; finales ; un jalon | 2 × 25 + 4 + 2 = **56** | 14 |

**Avant les zones, la Q1** : le sprint d'IA sur les 8 sorts muets, environ **5 points**, la taille du sprint d'IA de D98 (S210, seconde story).

**Ce que je n'ai pas mesuré :** le coût réel de régler une famille pour 10 classes (estimé +1 point, sans preuve) ; les mécaniques neuves qu'une zone voudra (la règle des Aïeux : une par zone, 5 points de plus chacune, non comptées) ; la direction artistique, reportée à la fin du jeu (modèles Kenney gardés).

## 6. À trancher par Charles

- **Q1. Les 8 sorts muets** : a) **un sprint d'IA de 5 points avant les zones : la valeur d'un bouclier ou d'une glyphe pour un allié menacé, le Rappel, les statuts sur soi, les bonds et les retraits de PM du Voleur et du Guerrier ; mesuré au banc de groupes (Reco)** ; b) changer ces sorts en sorts que l'IA sait jouer (des dégâts en plus) ; c) laisser le playtest dire s'ils manquent.
- **Q2. Les zones** : a) **une zone de 46 à 60, puis un jalon : la première dit ce que coûte une zone à 10 classes, et 46 à 60 est ce qu'un joueur atteint d'abord (Reco)** ; b) les quatre zones d'un coup (96 points) ; c) deux grandes zones (56 points).
- **Q3. Le trio du banc de groupes**, trop fort (96 à 99 %) : a) **le garder comme complément, le duo faisant foi (Reco)** ; b) l'opposer à des hordes de niveau +5 (le banc devient plus lent et moins lisible) ; c) le retirer.

**Réponses de Charles (17 novembre 2026, « go », D102) :** Q1 à Q3, les recommandations. Le sprint d'IA des sorts de groupe au 223 ; la zone de 46 à 60 du 224 au 228 (cartes, deux familles, donjons, artisanat) ; le jalon 4 au 229 ; le trio gardé en complément.
