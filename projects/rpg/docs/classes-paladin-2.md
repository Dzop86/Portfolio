# Le Paladin, seconde moitié : le Templier et le Vengeur *(9 novembre 2026, sprint 209, à valider par Charles)*

Ce document prépare la story du sprint 209 : « En tant que Paladin, je deviens aussi Templier ou Vengeur, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 : Templier, frappe qui marque et qui rend ; Vengeur, frappe d'autant plus que les siens ont souffert ; §3 : le Porte-lumière relève ; §8 : les doublons avec le Prêtre) et de `classes-paladin.md`.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Une brique** : **la vengeance** (`vengeance`), le pendant de la fureur du Berserker sur tout son camp : la moitié de la part de PV que les siens (lui compris) manquent en moyenne, 30 % au plus, au-delà du plafond des talents.
- **8 sorts de voie, 8 finales** : le Paladin a ses 3 branches, 6 lignées et 12 finales. Plus aucune classe en cours d'écriture : les 7 classes de la vague 2 sont complètes.
- **Le Porte-lumière relève** : son Égide relève aussi un allié tombé sur sa case (la résurrection du Prêtre, une fois par combat), ajoutée au sort par sa finale.

## 1. L'arbre

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Templier** (`templar`) | +10 % sous un état ; **Sceau de lumière** (−10 % de résistance), **Retour sacré** (vol de vie 30 %) | Inquisiteur (`paladin-inquisitor`) | **Sentence** (purge l'ennemi) | Juge sacré (TargetState : +20 % sous deux états) ; Purificateur (`paladin-purifier`, Faster : sans relance) |
| | | Croisé (`paladin-crusader`) | **Croisade** (bond, croix 1 à l'arrivée) | Croisé divin (FirstTurn : +2 PA) ; Porte-lumière (Saves : Égide relève) |
| **Vengeur** (`avenger`) | vengeance ; **Rétribution**, **Vœu de vengeance** (+20 % de dégâts, 2 tours) | Justicier (`justiciar`) | **Coup du verdict** | Exécuteur divin (Kill : « Prime touchée ») ; Main du jugement (Faster : relance 1) |
| | | Fanatique (`paladin-fanatic`) | **Fureur sacrée** (+2 PA, −10 % de résistance) | Zélote (`paladin-zealot`, Faster : relance 2) ; Avatar sacré (Form : +20 % de dégâts 2 tours) |

**Les doublons avec le Prêtre** (Inquisiteur, Purificateur, Fanatique, Zélote) prennent `paladin-` devant, comme la conception l'a prévu : mêmes noms, jamais les mêmes ids, et toujours la même séparation, au contact pour le Paladin, à distance pour le Prêtre.

## 2. Mesures

- **La suite** (`Rpg.Core`, 1 127 tests) : verte. Les exigences en solo de la CI, les bandes de ±15 des groupes de référence et les hordes de la Forêt tiennent sans remesure ; la vengeance, la règle et les finales neuves ne touchent que les héros qui les portent.
- **Le budget de combats de D97** : 19 507 combats (19 629 au sprint 208), sous les 20 000 : les 12 héros de voie du Paladin à 100 se partagent le budget fixe de la fumée.
- **La fumée** : avec les 19 héros du Paladin à 100, chaque héros joue moins de combats. Le Vœu de vengeance n'est pas joué par le Vengeur à 35 ni par le Justicier (l'IA dépense ses PA en Rétribution et en Coup du verdict ; le Fanatique le joue). Quatre finales ne changent aucun combat : le Purificateur, la Main du jugement et le Zélote du Paladin (leur sort une relance plus tôt) et la Baliste vivante de l'Archer, dont le budget a baissé. Toutes rangées comme non concluantes, à reprendre au jalon 2.
- **Non mesuré** : la vengeance en groupe (la fumée se joue seul, et le camp d'un héros seul n'est que lui) et le Porte-lumière qui relève (vérifié par un test sur l'arène).

## 3. À trancher par Charles

- **Q1. Les noms** des 8 sorts de voie : noms de travail, au scénariste.
- **Q2. La vengeance compte le Paladin lui-même** : a) **oui (Reco)**, sinon elle ne vaut rien seul ; b) seulement ses alliés.
- **Q3. Le Porte-lumière qui relève par Égide** : a) **oui (Reco)**, la résurrection reste une fois par combat ; b) un passif « Pas aujourd'hui » à la place, comme le Gardien éternel.
