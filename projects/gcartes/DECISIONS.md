# Décisions, gcartes

## G1. Tableaux d'involutions plutôt que graphe d'objets
**Choix :** une G-carte est trois tableaux `alpha[i][d]` ; un brin libre pour αi pointe sur lui-même.
**Pourquoi :** les contraintes s'écrivent comme dans la définition (`alpha[i][alpha[i][d]] === d`), les orbites sont un parcours en largeur, et la structure se copie et se teste facilement.
**Alternatives :** brins comme objets avec des pointeurs (plus proche d'une implémentation C++, plus de code pour le même cours).

## G2. Orientabilité par deux couleurs
**Choix :** une composante est orientable si ses brins se colorent en deux couleurs telles que chaque liaison αi (hors brin libre) relie deux couleurs.
**Pourquoi :** chaque couleur est une orientation ; le ruban de Möbius échoue parce qu'un tour le long du ruban revient sur la couleur de départ.
