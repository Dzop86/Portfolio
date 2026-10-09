# Sprint 52 : Osmose, le choix du serveur et des personnages

**Objectif :** à l'ouverture, le jeu montre le choix du serveur puis la liste visuelle des personnages, comme Dofus.

**Goal:** when it opens, the game shows the server choice then a visual list of characters, as in Dofus.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je choisis mon serveur puis mon personnage (rpg) : liste des serveurs donnée par l'API (connexion directe s'il n'y en a qu'un) ; personnages en vignettes (portrait 3D, pseudo, classe, niveau) ; « Créer » s'il y en a moins de cinq ; suppression confirmée ; xUnit, auto-test et captures. | 3 | Fait |

**Résultat :** à l'ouverture, le jeu passe le choix du serveur (Osméria, seul pour l'instant, nommé par Charles) et montre les personnages en cartes : portrait 3D, pseudo, classe, niveau ; « Jouer » mène au village ; suppression confirmée ; création dans un panneau. Sans le launcher, le jeu propose de l'ouvrir ou de jouer hors ligne (T21).

**Tests :** 3 tests de plus (serveur et client), l'auto-test contre le serveur passe par les cartes, l'essai de bout en bout vérifie la liste des serveurs. **Trouvé en route :** une caméra orientée hors de la scène, un bouton mal nommé, une migration à régénérer après le choix du nom du serveur.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
