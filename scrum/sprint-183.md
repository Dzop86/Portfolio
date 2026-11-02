# Sprint 183 : Osmose : la page Éléments

**Objectif :** les pages Éléments et Voie à la place de la page Talents, conçues par le game designer (`projects/rpg/docs/elements-specialisations.md`, v3) et validées par Charles : trois paliers à 30, 50 et 100 (D93).

**Goal:** the Elements and Path pages in place of the Talents page, designed by the game designer (`projects/rpg/docs/elements-specialisations.md`, v3) and approved by Charles: three tiers at 30, 50 and 100 (D93).

| Story | Points | État |
|---|---|---|
| En tant que joueur, l'onglet « Éléments » montre mes quatre éléments et leurs points, la roue des 6 paires et des 4 trios avec leurs réactions, Créer et Dissoudre, et la frise des paliers 30, 50 et 100 ; l'arbre des talents s'affiche sans portes ; textes FR et EN (rpg) ; xUnit client, auto-test Godot : créer la Foudre à 30, la page tient en 1280 × 720. | 3 | Fait (T143) |
| En tant que Charles, la remesure (T41 à 35, 40 et 45, hordes, builds de référence) montre que les réactions pour tous ne cassent pas l'équilibre, ou la règle écrite d'avance est appliquée (rpg) ; `TemplateTests`, `BalanceTests` verts, la Disjointe mesurée sur 1000 combats, sa cause trouvée, mesure hors CI écrite. | 2 | Fait (T143) |

**Ce qui est fait.** L'onglet Éléments, dans la fenêtre des caractéristiques : la frise 30, 50, 100 ; les quatre éléments de base avec leurs points, le seuil de D85 et leur bonus ; la roue, quatre éléments aux points cardinaux, chaque paire sur le trait de ses parents, chaque trio dans un coin ; à droite la fusion choisie, sa signature, sa réaction, ses sorts et son bouton Créer ou Dissoudre, gratuit et enregistré avec le reste. Modèle pur `ElementsPage` et disposition `ElementsWheel` dans le client, tests compris ; textes FR et EN. Ils sont de moi, d'après `elements.md` : à relire par le scénariste.

**La remesure.** La règle écrite d'avance ne s'applique pas : à 35, 40 et 45, sans kit, les réactions laissent les petits groupes à 29 à 30 sur 30 et les 4 hordes solides à 0 sur 10. Seul le Mage bouge contre la Disjointe, de 52 à 90 sur 1000 combats, à cause d'Aveuglé ; la moyenne des classes est à 78 (`docs/mesures-arbre.md` v3).

**Ce qui a bougé.** La story visait la Glace à 30, mais ses sorts n'arrivent qu'en S184 : l'auto-test crée donc la Foudre, et les 5 autres paires et les 4 trios s'affichent « À venir », et s'allumeront d'eux-mêmes quand le jeu aura leur composé. La roue est dessinée sans maquette du graphiste. **À trancher par Charles :** garder le plafond de la Disjointe à 80 (mon choix pour l'instant), ou l'affaiblir contre le Mage.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
