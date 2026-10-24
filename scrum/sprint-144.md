# Sprint 144 : Osmose : l'équilibre de l'arbre, playtest

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, les builds de référence sont mesurés de 35 à 45, en solo et en duo avec soutien, puis Charles joue l'arbre (rpg) ; xUnit, mesures sur 100 combats. | 3 | Fait (playtest de Charles à venir) |
| En tant que joueur, après le playtest : une fenêtre d'amis et un panneau de menus à la manière de Dofus, pas de Foudre dans le filtre des sorts sans son talent (rpg, D81) ; xUnit, auto-test du village. | 2 | Fait |

## Fait
- Les six derniers talents de Réactions, R7 à R12 : Empreinte tenace, Catalyse, Réaction prolongée, Onde, Empreinte partagée, Virtuose (T110).
- Sept builds de référence (`Progression.Builds`, `Progression.Talented`), mesurés en CI à 35, 40 et 45 : petits groupes saturés (29 à 30 sur 30), familles à égalité.
- Mesure : une Ouverture de Foudre donnait la pleine force pour 1 point (somme Feu + Air) ; décision de Charles : le plus haut parent, la somme pour la seule Conversion.
- Mesure : un héros d'un seul élément, sans talent, passe déjà 2,7 à 5,7 hordes sur 10 ; décision de Charles : l'arbre est jugé contre le meilleur héros nu, à 1,5 près.
- Duo « dégâts + soutien » contre « dégâts + dégâts » : le soutien ne gagne pas plus.
- Playtest de Charles sur la 1.0.131 (D81) : la fenêtre des contacts refaite (onglets, colonnes, cartes, icônes, compte sur 100), le panneau « Menus » en grille de trois en bas à droite, la pastille Foudre cachée sans son talent.
- L'arbre en vrai arbre (tronc, paires, styles) : questionnaire du game designer, 144 questions, `projects/rpg/docs/talents-arbre-questions.md` ; chiffré après les réponses de Charles.
- Reste : les mesures sur 100 combats hors CI.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
