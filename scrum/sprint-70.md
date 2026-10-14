# Sprint 70 : Osmose, le monde en cartes

**Objectif :** demande de Charles (D63) : un monde fait de cartes reliées : une zone est une grille de 5 × 5 cartes, le bois d'abord.

**Goal:** Charles's request (D63): a world made of linked maps: a zone is a grid of 5 × 5 maps, the woods first.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je parcours un monde de cartes (rpg) : une zone = une grille de 5 × 5 cartes de la taille d'un écran, reliées par leurs bords (on passe à la voisine en marchant jusqu'au bord) ; le village de Clairval reste une carte, à l'entrée du Bois (25 cartes, décrites en données et écrites par un script, chacune accessible) ; le serveur garde la carte et la case ; xUnit, auto-test. | 3 | Fait |

**Résultat :** le monde fait de cartes de la taille d'un écran ; le Bois de Clairval en 5 × 5 cartes à l'ouest du village, écrites par `tools/world_gen.py`, ouvertes l'une sur l'autre par trois cases de leurs bords ; chaque passage vérifié au chargement (bord tourné vers la voisine, arrivée sur le bord d'en face) ; le titre donne la place [x, y] ; la carte du monde montre une case par carte en attendant la vue du ciel (T37).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
