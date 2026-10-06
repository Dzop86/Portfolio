# Relecture humaine, morpion (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/morpion/ai.py` : le minimax et la valeur d'une victoire.
- [x] `src/morpion/board.py` : règles et positions atteignables.
- [x] Le jeu ressemble-t-il à celui de mes études ?

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« je valide, envoie »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `tests/*.py` | (Claude) Détecté par mypy strict : 31 fonctions de test sans annotations de type | `-> None`, fixtures pytest et `**kwargs` annotés |
| 2026-10-07 | `.gitignore` | (Claude) L'environnement virtuel `.venv/` n'était pas ignoré | Ajouté |
| 2026-10-07 | `../../src/assets/morpionplay.js` | (Claude) Détecté par Playwright : les flèches partaient de la dernière case cliquée, pas de celle qui avait le focus | Le focus suit la case active (`focusin`) |
| | | | |
