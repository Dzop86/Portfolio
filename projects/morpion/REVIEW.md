# Relecture humaine, morpion (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/morpion/ai.py` : le minimax et la valeur d'une victoire.
- [ ] `src/morpion/board.py` : règles et positions atteignables.
- [ ] Le jeu ressemble-t-il à celui de mes études ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `tests/*.py` | (Claude) Détecté par mypy strict : 31 fonctions de test sans annotations de type | `-> None`, fixtures pytest et `**kwargs` annotés |
| 2026-10-07 | `.gitignore` | (Claude) L'environnement virtuel `.venv/` n'était pas ignoré | Ajouté |
| | | | |
