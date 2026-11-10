# L'Invocateur, deuxième partie : le Familieriste *(10 novembre 2026, sprint 212, à valider par Charles)*

Ce document prépare la story du sprint 212 : « En tant qu'Invocateur, je deviens Familieriste, avec la fusion ; xUnit. » Le Démoniste et le Conjurateur viennent au sprint 213. Il part de `classes-conception.md` (§1.3 « une créature qui grandit, puis la fusion », §3 « fusion : tuer sa créature donne un état au lanceur »), de `classes-canon.md` §2.3 (la fusion et la question morale) et de `classes-liste.md` §10.3.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Une bête élevée, qui grandit.** La branche donne un Familier, nourri de 20 % des PV et de 30 % des caractéristiques de l'Invocateur ; il gagne 8 % de dégâts à chacun de ses tours, 40 % au plus.
- **Deux briques neuves** : la **croissance** (règle `growth`, état `Grown`, que rien ne retire, purge comprise) et la **fusion** (effet `fuse`) : la créature visée meurt, l'Invocateur prend ses PV restants en bouclier et 35 % de dégâts, sa croissance en plus, pour 3 tours.
- **Le choix des lignées** : le Dompteur garde sa bête et la fait grandir plus vite ; le Fusionniste la prend en lui. C'est la question du canon (§2.3) : « ma créature, je l'ai élevée, elle me connaît, et ça tient ».

## 1. La branche (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Familieriste** (`familiarist`) | Familier nourri à 20 % des PV, 30 % des caractéristiques ; croissance 8 % par tour, 40 % au plus | **Familier** (`path-familiar`, 3 PA, 1-2, relance 4) ; **Becquée** (`path-titbit`, 2 PA, sur le Familier : soin 15-20 et +20 % de dégâts 2 tours) |

Le Familier (`familiar`) : modèle de loup un peu plus grand, 6 PA, 4 PM, initiative 65, **Griffe du familier** (3 PA, au contact, 7-10, deux fois, dans l'élément de l'Invocateur). Il compte dans les 3 créatures d'un héros.

## 2. Les lignées (niveau 50) et les finales (niveau 100)

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Dompteur** (`tamer`) | croissance +4 % par tour, +20 % au plus (12 et 60 en tout) ; **Rappel** (`path-recall`) : échange sa place avec le Familier | **Maître des familiers** (`familiar-master`, Faster : Familier relancé en 2 tours au lieu de 4) ; **Roi des créatures** (`creature-king`, Extends : toutes ses créatures grandissent de 5 % par tour, 25 % au plus) |
| **Fusionniste** (`fusionist`) | **Fusion** (`path-fusion`, 3 PA, 1-6, relance 3) : le Familier meurt en lui | **Chimériste** (`chimerist`, Extends : la Fusion donne aussi 2 PM pour 3 tours) ; **Avatar fusionnel** (`fusion-avatar`, Form : la Fusion laisse une forme de 15 % de dégâts pour tout le combat) |

**Le Maître des familiers changé en route.** La liste prévoyait deux Familiers (gabarit Second). Mais avec deux Molosses, un Feu follet et le Familier, l'Invocateur est à ses 3 créatures : le second ne venait jamais, et la finale valait le Dompteur au point près (44,6). Relancé en 2 tours, un Familier tombé revient vite : 52,1.

## 3. L'IA

- **La croissance** n'a pas de valeur propre : la créature frappe plus fort, et l'IA le lit dans ses coups (`Fight.Expected` lit `Grown`).
- **La fusion** vaut son bouclier (au plus ce que les coups à portée prendraient), la part de ses dégâts en plus sur les coups de l'Invocateur, moins ce que la créature aurait frappé en deux tours, **à proportion de ce qu'elle vivrait** : une créature que les coups à portée abattraient coûte peu à prendre (`Ai.FuseWorth`).

## 4. Mesures

Banc `--class summoner --simulate 30`, sans voie : 56,7 au niveau 35, 62,1 au 50, 25,0 au 100.

| Niveau | Voie | Points | Sur le héros sans voie | Sur la moyenne |
|---|---|---|---|---|
| 35 | Familieriste | 75,6 | +19,0 | |
| 50 | Dompteur | 86,7 | +24,6 | +10,0 |
| 50 | Fusionniste | 66,7 | +4,6 | −10,0 |
| 100 | Dompteur, Maître des familiers, Roi des créatures | 44,6, 52,1, 45,6 | +19,6 à +27,1 | |
| 100 | Fusionniste, Chimériste, Avatar fusionnel | 37,1, 36,9, 41,0 | +11,9 à +16,0 | |

Réglé par deux essais : à 35 % des PV, 50 % des caractéristiques et 10 % de croissance (la proposition de départ), le Familieriste était à +34,6 et l'écart entre lignées à 31 points. Le Rappel (un échange que l'IA ne fait que pour secourir) et la Becquée sont peu joués : ajoutés à `CiLevelsTests.Unplayed`.

## 5. À trancher par Charles

- **Q1. Les noms** : Familier, Becquée, Rappel, Fusion, Griffe du familier ; les titres viennent de ta liste.
- **Q2. Le Maître des familiers** : a) **relancé en 2 tours (Reco, fait)** ; b) deux Familiers, qui ne comptent pas dans les 3 créatures ; c) deux Familiers et pas de Molosses.
- **Q3. La fusion tue le Familier** pour le reste du combat, jusqu'à sa relance : a) **oui, le prix du canon (Reco)** ; b) il revient au tour suivant, affaibli.
