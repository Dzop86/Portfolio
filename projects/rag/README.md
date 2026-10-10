# rag : un assistant documentaire

Un assistant qui répond aux questions d'une équipe à partir de sa documentation technique, en citant ses sources au fichier et à la ligne près, et qui dit quand elles ne répondent pas. Le corpus est la documentation de ce portfolio (G1). Sprint 61 : retrouver le bon passage et le mesurer ; sprint 62 : les réponses citées de Claude, l'API et Docker.

*A documentation assistant: hybrid search (BM25 and multilingual embeddings in Qdrant, fused by reciprocal rank) over the portfolio's Markdown documentation, split into passages that keep their file, headings and lines, and measured on 40 reference questions in French and English (recall@k, MRR).*

## Comment ça marche
1. **Ingestion** (`chunking.py`) : chaque fichier Markdown devient des blocs (paragraphe, élément de liste, tableau, code) avec leurs lignes, regroupés sous leurs titres en passages de 1 200 caractères au plus (G2).
2. **Index** : BM25 sur les mots (`bm25.py`), et les embeddings des passages (`embed.py`, modèle multilingue local par FastEmbed, G4) dans une collection Qdrant (`retrieve.py`).
3. **Recherche** : `bm25`, `dense` ou `hybrid` (fusion des deux par rang, G3).

## Mesures (`eval/retrieval.json`)
40 questions de référence (`eval/questions.jsonl`), dont 32 avec une réponse dans la documentation (un fichier et un titre attendus) et 8 sans.

| Recherche | rappel@1 | rappel@3 | rappel@5 | MRR |
|---|---|---|---|---|
| BM25 | 0,41 | 0,56 | 0,66 | 0,51 |
| Vecteurs | 0,31 | 0,63 | 0,75 | 0,49 |
| Hybride | 0,53 | 0,69 | 0,81 | 0,62 |

## Lancer
```sh
cd projects/rag
python -m pip install -e ".[embed,test]"
python -m docrag.evaluate                    # les trois recherches sur les questions de référence
python -m docrag.evaluate --embedder hash    # sans télécharger de modèle
python -m pytest -q
```

## Tests
21 tests pytest : découpage (lignes, titres, listes, tableaux, code, blocs jamais coupés), BM25, les trois recherches et la fusion RRF sur Qdrant en mémoire, les mesures, et deux tests d'intégration sur la vraie documentation (chaque question de référence pointe vers un passage existant). Sous Linux, Windows et macOS. Un job à part évalue les recherches avec le vrai modèle (G5). 6 mutations, 6 attrapées.

## Limites
- 32 questions : de quoi comparer des choix, pas de quoi trancher des écarts de quelques points.
- Le modèle d'embeddings tronque à 128 tokens (G4).
