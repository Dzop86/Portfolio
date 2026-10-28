# Sprint 157 : Osmose : le socle graphique et les icônes

**Objectif :** une refonte graphique de l'interface d'Osmose demandée par Charles après S156, glissée avant le socle 45-60 (D84).

**Goal:** a graphic overhaul of Osmose's interface asked for by Charles after S156, slipped in before the 45-60 groundwork (D84).

| Story | Points | État |
|---|---|---|
| En tant que joueur, les fenêtres du jeu partagent un thème tiré de la charte (`tokens.css` : panneaux, boutons, barres, onglets, cadres, bandeau) au lieu de styles posés un à un (rpg) ; xUnit sur le thème, auto-test des fenêtres. | 2 | Fait (panneau Menus, menu Échap, fond commun ; les autres fenêtres au fil de leur refonte) |
| En tant que joueur, les icônes du menu ont de la couleur et de la profondeur (fond en dégradé, contour, reflet, ombre) : un script les compose depuis les silhouettes de game-icons.net et les rend en PNG sans fenêtre (rpg) ; tests du script, auto-test du panneau Menus. | 2 | Fait |
| En tant que Charles, un agent graphiste garde la charte de l'interface, écrit la maquette de chaque écran avant le code et relit les captures (rpg). | 1 | Fait |

## Fait
- Agent graphiste (`~/.claude/agents/graphiste.md`) ; ses maquettes dans `projects/rpg/docs/graphisme/s157-maquettes.md` : recette d'une icône en relief, les 13 icônes, le panneau Menus, 12 composants du thème.
- `tools/ui_art.py` : 13 icônes en relief (ombre, contour, dégradé, reflet, accent en coin) et 15 cadres 9-patch, sans filtre SVG ; silhouettes gardées dans le dépôt ; Bestiaire en tête de loup, Succès en trophée (game-icons.net), teintes revues pour que deux voisines ne se confondent plus (T124).
- Un thème unique (`OsmoseTheme.cs`, couleurs de `UiStyle.cs`, toutes dans `tokens.css`) : boutons en relief (secondaire, principal pistache), tuiles, barres, onglets, champs, infobulles, défilement, variations nommées pour les fenêtres à venir.
- Le panneau Menus refait (fond 92 %, bord chocolat, titre « MENUS » et sa grille, tuiles neutres, tuile ouverte en pistache avec halo, pastille et touche) ; le menu Échap (« Reprendre » en bouton principal) ; le cadre chocolat et l'ombre de toutes les fenêtres.
- xUnit (couleurs dans la charte, couches de chaque icône, crédits, cadres), auto-test du village (tuiles du thème, variations définies) : verts.

- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
