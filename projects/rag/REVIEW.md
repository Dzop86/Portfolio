# Relecture humaine, rag (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/docrag/chunking.py` : les lignes de chaque bloc, les titres sautés, les blocs de code.
- [ ] `eval/questions.jsonl` : les 40 questions de référence sont-elles justes, et assez variées ? Les 8 sans réponse sont-elles vraiment sans réponse ?
- [ ] `src/docrag/retrieve.py` : la fusion RRF et le choix de 20 candidats par liste.
- [ ] `src/docrag/answer.py` : la consigne, la garde « pas de citation, pas de réponse », le passage des citations aux lignes.
- [ ] `src/docrag/api.py` et `compose.yaml` : `/ask` sans authentification ni limite de débit, acceptable tant que rien n'est exposé ?

## Constats

| Date | Fichier | Problème | Correction |
|---|---|---|---|
| 2026-10-10 | `src/docrag/embed.py` | (Claude) Avec `paraphrase-multilingual-mpnet-base-v2`, l'évaluation était tuée sans message (code 137) : FastEmbed encode par lots de 256, des gigaoctets pour des séquences de 512 tokens | Lots de 32 |
| 2026-10-10 | `tests/test_retrieve.py` | (Claude) Mutation non attrapée : BM25 sans son IDF passait tous les tests | Test d'un mot rare qui l'emporte sur un mot courant répété ; 6 mutations, 6 attrapées |
| 2026-10-10 | `scripts/smoke.sh` | (Claude) Le test de fumée échouait contre une API qui marchait : le motif attendait un guillemet devant « D8. », alors que le titre commence par « Journal des décisions > » | Motif corrigé |
| 2026-10-10 | `src/docrag/chunking.py` | (Claude) La documentation de ce projet citait des mots des questions de référence (`zone.js`, MLflow, D45) : elle aurait répondu à la place des vrais passages | Exclue du corpus (G1) |
| 2026-10-10 | `src/docrag/evaluate.py`, `api.py` | (Claude) CI rouge partout : le dépôt et les questions étaient cherchés à partir de l'emplacement du paquet, dans `site-packages` une fois installé (en local, l'installation éditable le masquait) | `find_root` remonte depuis le dossier courant ; tests rejoués sur une installation non éditable |
| 2026-10-10 | `src/docrag/chunking.py` | (Claude) Sous Windows, les fichiers du corpus sortaient dans un autre ordre : `WindowsPath` se compare sans tenir compte de la casse | Tri sur le chemin relatif en texte, le même partout |
