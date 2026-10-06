# Décisions, morpion

## M1. Tout l'arbre, mis en cache, sans élagage
**Choix :** minimax (negamax) sur tout l'arbre du jeu, avec `functools.cache` par position, plutôt qu'un élagage alpha-bêta.
**Pourquoi :** le morpion n'a que 5 478 positions atteignables ; le cache les calcule chacune une fois, en une fraction de seconde. Le code reste la définition du minimax, simple à relire, et chaque position a une valeur exacte, ce dont le livre de coups a besoin.
**Limite :** l'approche ne passe pas à des jeux plus grands (l'Othello du portfolio passe à une recherche alpha-bêta).

## M2. Un livre de coups calculé en Python pour la page web
**Choix :** la fiche du projet lira `data/book.json` (valeur et meilleurs coups de chaque position) plutôt que de faire tourner Python dans le navigateur (Pyodide) ou de réécrire l'IA en JavaScript.
**Pourquoi :** une seule implémentation de l'IA, celle qui est testée ; un fichier d'environ 120 kB, sans moteur de plusieurs mégaoctets. La CI vérifie que le fichier commité correspond au programme.
