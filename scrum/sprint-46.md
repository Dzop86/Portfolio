# Sprint 46 : le RPG tactique, le combat dans Godot

**Objectif :** le combat du sprint 45 jouable dans un client Godot 4 en C#, sur une carte isométrique faite des sprites libres de Kenney, exporté pour les trois systèmes.

**Goal:** the sprint 45 combat playable in a Godot 4 C# client, on an isometric map made of Kenney's free sprites, exported for the three systems.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je choisis les visuels sur pièces (rpg) : sprites 2D isométriques ou petits modèles 3D de Kenney vus en caméra isométrique, comparés sur une même scène (directions, animations, personnalisation possible) ; licences notées dans le dépôt ; décision consignée. | 1 | Fait |
| En tant que joueur, je joue un combat contre l'IA (rpg) : carte isométrique, cases atteignables et portée des sorts en surbrillance, chemin prévisualisé, points d'action et de mouvement affichés, fin de tour, écran de victoire ou de défaite ; français et anglais ; tests de l'interface sur les scènes (GdUnit4 ou équivalent). | 3 | Fait |
| En tant que joueur, je télécharge le jeu (rpg) : exports Windows, Linux et macOS par la CI, artefacts liés depuis la fiche ; captures sur la fiche. | 1 | Fait |

**Résultat :** visuels choisis par Charles sur pièces : la 3D (T7). Le combat se joue à la souris et au clavier contre l'IA, en français ou en anglais, sur un plateau 3D animé. Captures sur la fiche, faites par le jeu lui-même.

**Tests :** 10 tests xUnit de `Rpg.Client` (dont 200 combats joués par les commandes, identiques à ceux de l'IA seule) ; auto-test du client Godot (`--selftest`) sur Linux, Windows et macOS, puis dans l'exécutable Linux exporté ; 1 test Node de la fiche (captures dans les deux langues, liens) ; règles : 57 tests après le retrait de la projection 2D. **Trouvé en route :** une première scène 3D d'essai qui la désavantageait, des panneaux qui seraient sortis de l'écran, des couleurs illisibles sur les premières captures, une capture « sort » sans visée, du code mort (`Iso.cs`). Les exports n'ont pas pu être essayés en local (pas de modèles d'export) : c'est la CI qui les vérifie.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
