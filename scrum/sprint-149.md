# Sprint 149 : Osmose : les explications de l'arbre

**Objectif :** l'arbre des talents en vrai arbre (tronc, paires d'éléments, styles de jeu), d'après les réponses de Charles au questionnaire et les arbitrages du game designer (`projects/rpg/docs/talents-arbre-questions.md`, D82).

**Goal:** the talent tree as a real tree (trunk, element pairs, play styles), from Charles's answers to the questionnaire and the game designer's rulings (`projects/rpg/docs/talents-arbre-questions.md`, D82).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je comprends chaque couche et chaque nœud sans aide : noms, bandeaux, panneau, légende, glossaire (rpg) ; xUnit, auto-test Godot. | 3 | Fait |
| En tant que joueur, je découvre l'arbre à 30 avec trois bulles guidées, et chaque nœud a son icône (rpg) ; xUnit, auto-test Godot. | 2 | Fait |

## Fait
- Chaque nœud a son icône (game-icons.net, créditée), sa contrepartie et une ligne d'ambiance en FR et en EN (T116).
- Les couches nommées (Fondations, Affinités, Élément pur, Fusion, Styles) avec leurs bandeaux, la fiche d'un nœud (équation d'une paire, effet, contrepartie, ambiance, état), le glossaire au survol, la légende derrière « ? ».
- Le titre du build, ses contreparties, son meilleur coup et l'avertissement de plafond ; le conseil de paire ; Main tendue chiffrée ; les pastilles d'élément et le pictogramme « groupe ».
- Trois bulles guidées à la première ouverture dès le niveau 30, gardées sur le compte (migration `AddSeen`).
- Les couleurs de l'arbre dans `ElementStyle` et `tokens.css`. À faire : relecture des noms et des textes par le scénariste puis par Charles (M10 b).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
