# Sprint 49 : le RPG tactique, la ville d'accueil

**Objectif :** une première carte d'accueil sans monstre, où l'on se promène et où l'on parle à trois PNJ.

**Goal:** a first welcome map with no monsters, where you walk around and talk to three NPCs.

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'arrive dans une ville et je parle à ses habitants (rpg) : carte isométrique décorée (sprites Kenney), déplacement au clic par le chemin le plus court, trois PNJ aux dialogues bilingues décrits en données (choix de réponses), position sauvegardée sur le serveur, passage vers un combat d'entraînement ; tests des dialogues et de la carte. | 3 | Fait |

**Résultat :** le village de Clairval (16 × 12 cases, quatre maisons, une fontaine, deux étals, une mare), monté avec le Fantasy Town Kit de Kenney (CC0). On y marche au clic par le plus court chemin, on parle à Aubin, Rose et Garance (huit répliques, réponses au clic ou au clavier, en français et en anglais), la porte est et Garance mènent au combat d'entraînement, et l'on revient en ville après. Le serveur garde la position et refuse une case inaccessible (T15, T16).

**Tests :** 13 tests de plus (données de la ville et des dialogues, clics, conversation, position côté serveur) ; dans la CI, le tour complet du village par l'écran sur les trois systèmes, et tout le chemin contre le serveur (inscription, personnage, village, position relue, combat). 3 mutations, 3 attrapées après un test ajouté. **Trouvé en route :** la CI du sprint 48 était rouge (l'essai de bout en bout créait un personnage sans classe : corrigé à part, 8093ed5), des champs calculés envoyés par l'API, des personnages trop petits et une habitante cachée sur les premières captures.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
