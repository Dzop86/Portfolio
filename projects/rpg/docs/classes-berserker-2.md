# Le Berserker, seconde moitié : le Barbare et le Gladiateur *(8 novembre 2026, sprint 207, à valider par Charles)*

Ce document prépare la story du sprint 207 : « En tant que Berserker, je deviens aussi Barbare ou Gladiateur, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 : Barbare, le clan au contact ou la bête ; Gladiateur, une cible et l'exécution ; §1.4 : aucun bouclier, aucune attirance, aucun retrait de PM), de `classes-liste.md` §9 et de `classes-berserker.md` (le sang, la Rage, le Ravageur).

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Deux règles neuves**, aucune autre brique : **le flanc** (`flank`, +10 % sur une cible qu'un allié touche aussi : le clan au contact) et **l'exécution** (`execute`, +25 % sur une cible sous 30 % de ses PV, au-delà du plafond des talents comme la fureur).
- **8 sorts de voie**, **8 finales** : le Berserker a ses 3 branches, 6 lignées et 12 finales.
- **L'Avatar primal** est la première finale du gabarit « Forme » : entrer en Rage lui donne aussi +2 PA et +2 PM pendant 1 tour (`patch` du sort Rage).

## 1. L'arbre

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Règle et sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Barbare** (`barbarian`) | flanc +10 % ; **Cri du clan** (alliés +15 % de dégâts, cercle 2), **Coup sauvage** | Guerrier tribal (`tribal-warrior`) | **Étendard de guerre** (alliés +15 % de résistance, cercle 2) | Chef de guerre (`warchief`, Faster : Étendard en 2 tours) ; Roi des clans (`clan-king`, Extends : Cri du clan en cercle 3) |
| | | Sauvage (`savage`) | **Bond sauvage** (bond, croix 1 à l'arrivée) | Prédateur (TargetState : +20 % sous un état) ; Avatar primal (Form : la Rage donne +2 PA et +2 PM) |
| **Gladiateur** (`berserker-gladiator`) | exécution +25 % sous 30 % ; **Défi** (−10 % de résistance, 2 tours), **Coup d'arène** | Champion (`blood-champion`) | **Duel sanglant** (10 PV, vol de vie 30 %) | Légende de l'arène (Extends : Duel à 2 cases) ; Héros du sang (StayOrMove : +15 % sans avoir bougé) |
| | | Exécuteur (`executor`) | exécution +15 % de plus ; **Décapitation** (12 PV) | Bourreau (`headsman`, Kill : « Prime touchée ») ; Décapiteur (`beheader`, Faster : relance 1) |

Les identifiants qui existent déjà au Guerrier prennent un préfixe (`berserker-gladiator`) ou un autre mot (`blood-champion`, `headsman`, `warchief`), comme la conception le prévoit (§8, les doublons).

## 2. Ce qui le sépare du Guerrier

Vérifié par un test sur le kit et sur les 12 sorts de voie : aucun bouclier, aucune attirance, aucun retrait de PM chez le Berserker. Le Gladiateur du Guerrier repose sur les murs et les attirances ; celui du Berserker sur l'exécution payée en PV.

## 3. Mesures

- **Les exigences en solo de la CI, les groupes de référence et les empreintes en solo** : tenus, inchangés (aucune nouvelle classe).
- **La fumée de la CI** : avec six classes complètes, elle ne laisse plus que 3 combats par héros et par groupe (4 au sprint 205). L'IA n'y joue aucun des sorts du Barbare ni du Gladiateur, et même plus les poisons de l'Empoisonneur (joués au sprint 205). Rangés comme non concluants (`Unplayed`, `UnplayedOn`), Roi des clans et Décapiteur parmi les finales qui ne changent rien. **C'est le premier sujet du jalon 2** : un sort de voie au barème de son niveau reste sous les sorts du kit et leurs rangs (Q5 de `classes-berserker.md`), et une fumée à budget fixe dit de moins en moins à mesure que les classes arrivent.

## 4. À trancher par Charles

- **Q1. Les noms** des 8 sorts de voie : noms de travail, au scénariste.
- **Q2. L'exécution au-delà du plafond** : a) **oui (Reco)**, comme la fureur ; b) sous le plafond.
- **Q3. Les finales qui reprennent un passif** (Bourreau : « Prime touchée », comme le Boucher, l'Ombre mortelle et l'Élu céleste) : a) **gardé tant que les finales n'ont pas leurs passifs propres (Reco)** ; b) un passif neuf par finale, environ 1 point par classe.
