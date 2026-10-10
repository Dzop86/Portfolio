# Relecture humaine, rag (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/docrag/chunking.py` : les lignes de chaque bloc, les titres sautés, les blocs de code.
- [ ] `eval/questions.jsonl` : les 40 questions de référence sont-elles justes, et assez variées ? Les 8 sans réponse sont-elles vraiment sans réponse ?
- [ ] `src/docrag/retrieve.py` : la fusion RRF et le choix de 20 candidats par liste.

## Constats

| Date | Fichier | Problème | Correction |
|---|---|---|---|
| 2026-10-10 | `src/docrag/embed.py` | (Claude) Avec `paraphrase-multilingual-mpnet-base-v2`, l'évaluation était tuée sans message (code 137) : FastEmbed encode par lots de 256, des gigaoctets pour des séquences de 512 tokens | Lots de 32 |
| 2026-10-10 | `tests/test_retrieve.py` | (Claude) Mutation non attrapée : BM25 sans son IDF passait tous les tests | Test d'un mot rare qui l'emporte sur un mot courant répété ; 6 mutations, 6 attrapées |
