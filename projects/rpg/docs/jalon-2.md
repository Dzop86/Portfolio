# Jalon 2 : sept classes *(9 novembre 2026, sprint 210, à valider par Charles)*

La story : « En tant que Charles, je joue 7 classes, la mesure est dans `REVIEW.md`, et je décide si on continue, classes ou zones de 60 à 100 ; banc hors CI, build livrée. » Au jalon 1, Charles a dit de continuer jusqu'au sprint 219 et de ne livrer qu'au 219 : la build attend, et la question « classes ou zones » est posée quand même (Q5).

Les sept classes : Guerrier, Archer, Mage, Prêtre (vague 1), Voleur, Berserker, Paladin (vague 2). Chacune a 3 branches à 30, 2 lignées par branche à 50 et 2 finales par lignée à 100.

## 1. Ce que les sprints 204 à 209 ont laissé au jalon

Trois constats revenaient de sprint en sprint :

- **Des sorts de voie que l'IA ne joue pas** : le Coup bas du Voleur, les sorts du Ravageur payés en PV, le Vœu de vengeance du Vengeur (Q5 du Voleur et du Berserker).
- **La fumée de la CI** ne joue plus que 3 combats par héros et par groupe à 7 classes : les listes d'exceptions (`UnplayedOn`, `Unchanged`) grossissent à chaque classe.
- **Le Fauchage (niveau 10) et la Lame infernale (65) du Guerrier** : portée 1, zone en croix de 1, ils touchent toujours leur lanceur ; les zones du Berserker l'épargnent depuis le sprint 206.

**La cause du premier, trouvée à ce jalon.** Le banc et la fumée construisent le héros **sans voie**, dépensent ses points de sorts, puis lui donnent sa voie : ses sorts de voie restent au rang 1, sous les sorts du kit déjà montés au rang 2 ou 3. Par PA, l'IA préfère le sort du kit. Un joueur qui connaît sa voie monterait ses sorts de voie : le banc mesure un héros que personne ne jouera.

J'ai ajouté au banc l'option `--ranked-paths` (`Bench.Run(..., rankedPaths: true)`) : chaque héros de voie dépense ses points en connaissant sa voie (`Progression.Built`). Le défaut ne change pas, pour que les mesures restent comparables.

## 2. La mesure

`Rpg.Sim --matrix --simulate 30` (480 combats par héros, 16 groupes de 4 et 5 monstres, mêmes graines, héros nus), aux niveaux 35, 45 et 50 : 84 voies. Reproductible à l'identique.

**Héros sans voie** (points du banc) :

| Classe | 35 | 45 | 50 |
|---|---|---|---|
| Archer | 53,5 | 47,7 | 44,2 |
| Guerrier | 37,9 | 45,0 | 50,2 |
| Mage | 41,5 | 57,7 | 55,4 |
| Prêtre | 18,8 | 24,2 | 25,4 |
| Voleur | 42,9 | 48,5 | 44,8 |
| Berserker | 66,9 | 72,7 | 73,3 |
| Paladin | 49,0 | 54,0 | 51,3 |

**Les voies, au-dessus du héros sans voie de leur classe** :

| Modèle | Concluantes sur 84 | Moyenne | Au-delà de +15 (M2) | Au-delà de +24 (clôture de la CI) | Sous −3 |
|---|---|---|---|---|---|
| Défaut (sorts de voie au rang 1) | 25 | +5,9 | 16 | 0 | 15 |
| **Sorts de voie rangés** | **32** | **+14,9** | **45** | **17** | **7** |
| Rangés, sorts de voie à ×0,8 | 29 | +12,4 | 34 | 12 | — |
| Rangés, sorts de voie au barème de leur classe | 34 | +14,3 | 41 | 16 | — |

Les deux dernières lignes viennent d'essais sur une copie des données, mesurés à la session précédente et non gardés.

**Par classe, moyenne des 12 voies** (concluantes entre parenthèses) :

| Classe | Défaut | Rangés | Voie la plus haute, rangée |
|---|---|---|---|
| Archer | +9,8 (1) | +18,8 (6) | Barrage à 50, +34,2 |
| Guerrier | −2,0 (6) | +19,2 (6) | Geôlier à 50, +44,2 (94 contre 50) |
| Mage | +9,4 (7) | +17,4 (9) | +24,8 |
| Prêtre | +16,2 (7) | +24,0 (7) | Exorciste à 50, +36,5 |
| Voleur | +1,1 (0) | +3,4 (0) | +15,4 |
| Berserker | −1,9 (0) | +3,2 (0) | +21,5 |
| Paladin | +8,9 (4) | +18,4 (4) | +31,9 |

**Ce que ça dit :**

- **Le défaut sous-estime les voies.** Rangées, elles valent en moyenne 9 points de plus ; le Guerrier, dont les lignées passaient sous son héros sans voie (Q2 du jalon 1), passe de −2,0 à +19,2. La Q2 du jalon 1 venait en partie du banc.
- **Rangées, elles sont trop fortes** au regard de M2 (+15 au plus sur un héros sans voie) : 45 voies au-delà, 17 au-delà de la clôture de +24 de `TemplateTests`. Les baisser de 20 % ou les mettre au barème de leur classe n'y suffit pas.
- **Le Voleur et le Berserker** n'ont aucune voie concluante, même rangée : leurs voies reposent sur des bonds, des sorts payés en PV ou des sorts d'alliés que l'IA ne joue pas.
- **À 0,0 dans les deux modèles (16 sorts)**, surtout des bonds, des pièges et des sorts d'alliés : Assaut, Arrachement, Mâchoires, Cercle de fer, Rune, Résurrection, Cabriole, Pas de l'ombre, Lame enduite, Feinte, Charge sanglante, Cri du clan, Bond sauvage, Coup d'arène, Croisade, Bouclier de la foi. Rangés, le Coup bas, le Coup sauvage et le Duel sanglant sortent de 0,0 ; la Fiole de peste y tombe (17 sorts à 0,0).
- **Le Prêtre sans voie** est le plus faible (18,8 à 35, contre 24,4 au jalon 1) et ses voies les plus hautes : son héros sans voie n'est pas une bonne référence. **Le Berserker sans voie** est le plus fort (67 à 73) et ses voies montent peu.

## 3. Ce que j'ai joué

Rien en vrai : Godot ne tourne chez moi que sans écran. La CI joue la création, la page Spécialisation de chaque classe et un combat. Le playtest des 7 classes est pour Charles, avec la build du sprint 219.

## 4. À trancher par Charles

- **Q1. Le modèle du banc** : a) **sorts de voie rangés, comme un joueur les monterait, et clôture de la CI comparée entre voies d'une même classe (écart de 15 au plus) plutôt qu'au héros sans voie (Reco)** : le héros sans voie est une référence fragile (Prêtre trop bas, Berserker trop haut) ; b) rangés, en gardant +15 sur le héros sans voie : 45 voies à baisser une à une ; c) garder le défaut, qui sous-estime les voies.
- **Q2. Les 16 sorts jamais joués** (bonds, pièges, sorts d'alliés) : a) **un sprint d'IA pris sur les 15 points de marge, avant l'Invocateur, dont les créatures demandent la même IA (Reco)** ; b) les juger en groupe seulement ; c) les retravailler en sorts que l'IA joue déjà.
- **Q3. Le Fauchage et la Lame infernale qui touchent le Guerrier** : a) **les lancer sur sa case et l'épargner, comme les zones du Berserker (Reco)** ; b) les laisser, c'est un prix voulu.
- **Q4. La fumée de la CI** : a) **ne plus lui demander que chaque voie change au moins un combat, et laisser « chaque sort de voie est joué » au banc hors CI (Reco)** : à 10 classes, elle n'aura plus que 2 combats par héros ; b) lui donner plus de combats, au-delà du budget de 20 000 de D97.
- **Q5. La suite** : a) **les classes, sprints 211 à 219 (Invocateur, Nécromancien, Druide), comme décidé au jalon 1 (Reco)** ; b) les zones de 60 à 100 d'abord, les trois classes restantes grisées.
