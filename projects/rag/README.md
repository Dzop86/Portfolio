# rag : un assistant documentaire

Un assistant qui répond aux questions d'une équipe à partir de sa documentation technique, en citant ses sources au fichier et à la ligne près, et qui dit quand elles ne répondent pas. Le corpus est la documentation de ce portfolio (G1). Sprint 61 : retrouver le bon passage et le mesurer ; sprint 62 : les réponses citées de Claude, l'API et Docker.

*A documentation assistant: hybrid search (BM25 and multilingual embeddings in Qdrant, fused by reciprocal rank) over the portfolio's Markdown documentation, split into passages that keep their file, headings and lines, and measured on 40 reference questions in French and English (recall@k, MRR). Claude answers from the retrieved passages only, through the API's search_result blocks, and each citation maps back to a file and lines; with no citation, or when the passages hold no answer, it says so. A FastAPI service with Qdrant in docker compose.*

## Comment ça marche
1. **Ingestion** (`chunking.py`) : chaque fichier Markdown devient des blocs (paragraphe, élément de liste, tableau, code) avec leurs lignes, regroupés sous leurs titres en passages de 1 200 caractères au plus (G2).
2. **Index** : BM25 sur les mots (`bm25.py`), et les embeddings des passages (`embed.py`, modèle multilingue local par FastEmbed, G4) dans une collection Qdrant (`retrieve.py`).
3. **Recherche** : `bm25`, `dense` ou `hybrid` (fusion des deux par rang, G3).
4. **Réponse** (`answer.py`) : les six meilleurs passages vont à Claude en blocs `search_result` ; chaque citation revient en fichier et lignes (G6). Pas de citation, ou `NO_ANSWER` : pas de réponse (G7).
5. **API** (`api.py`) : `GET /search`, `POST /ask`, `GET /health`, documentation OpenAPI sur `/docs` (G8).

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
ANTHROPIC_API_KEY=... python -m docrag.evaluate --out eval/evaluation.json --answers eval/answers.json   # les réponses (payant)
uvicorn docrag.api:app --port 8003           # l'API, Qdrant en mémoire
```

Avec Docker, depuis la racine du dépôt :
```sh
docker compose up --build rag-api            # l'API sur http://localhost:8003 et Qdrant
curl -G localhost:8003/search --data-urlencode "q=Quelle version de Node ?"
curl -H 'content-type: application/json' -d '{"question":"Which Node version?"}' localhost:8003/ask   # avec ANTHROPIC_API_KEY
projects/rag/scripts/smoke.sh http://localhost:8003
```

## Tests
33 tests pytest : découpage (lignes, titres, listes, tableaux, code, blocs jamais coupés), BM25, les trois recherches et la fusion RRF sur Qdrant en mémoire, les mesures ; les réponses avec un faux client (requête envoyée, citations ramenées aux lignes, absence de réponse, refus, mesures des réponses) ; l'API (recherche, paramètres refusés, `/ask` sans clé ou avec une clé refusée) ; trois tests d'intégration sur la vraie documentation. Sous Linux, Windows et macOS. Jobs à part : l'évaluation des recherches avec le vrai modèle (G5), l'image Docker avec Qdrant de bout en bout, et les réponses de Claude, lancées à la main parce qu'elles coûtent. 12 mutations, 12 attrapées.

## Limites
- 32 questions : de quoi comparer des choix, pas de quoi trancher des écarts de quelques points.
- Le modèle d'embeddings tronque à 128 tokens (G4).
- Pas d'assistant en direct sur le site, qui est statique : les réponses de la fiche sont enregistrées. `/ask` n'a ni authentification ni limite de débit (G8).
- Pas de seuil pour refuser avant d'appeler Claude (G7) ; une question sans réponse coûte donc un appel.
- Prototype : l'évaluation des réponses n'a pas été lancée (pas de clé d'API) ; elle est écrite, testée avec un faux modèle, et lançable par le job « answers » de la CI.
