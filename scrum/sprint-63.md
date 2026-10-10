# Sprint 63 : assistant documentaire, retrouver le bon passage

**Objectif :** demande de Charles (D58) : retrouver, dans une documentation technique, les passages qui répondent à une question, et mesurer à quel point on les retrouve.

**Goal:** Charles's request (D58): find, in technical documentation, the passages that answer a question, and measure how well they are found.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je retrouve le bon passage de la documentation (rag) : ingestion des README, décisions et descriptions OpenAPI du portfolio, découpage par titres qui garde fichier et lignes, embeddings multilingues dans Qdrant, recherche hybride (BM25 et vecteurs, fusion RRF) ; questions de référence en français et en anglais, rappel@k et MRR des trois recherches ; pytest, CI Linux, Windows, macOS. | 5 | Fait |

**Résultat :** 421 passages tirés de 45 fichiers, une recherche hybride qui trouve un bon passage dans les cinq premiers pour 81 % des questions (BM25 seul 66 %, vecteurs seuls 75 %), un modèle d'embeddings choisi parmi trois sur ces mesures, une taille de passage choisie de même (G2 à G4). Les descriptions OpenAPI attendront : les README et les décisions suffisent à un premier corpus.

**Tests :** 21 tests pytest sur trois OS et deux versions de Python, un job d'évaluation avec le vrai modèle ; 6 mutations, 6 attrapées. **Trouvé en route :** un modèle tué faute de mémoire (lots trop gros), un test qui manquait pour l'IDF de BM25, la documentation du projet qui se serait citée elle-même.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
