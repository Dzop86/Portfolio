# Sprint 62 : assistant documentaire, des réponses citées

**Objectif :** demande de Charles (D58) : répondre avec les passages retrouvés, chaque phrase citée au fichier et à la ligne près, et dire quand les sources ne répondent pas.

**Goal:** Charles's request (D58): answer from the retrieved passages, each sentence cited down to the file and line, and say when the sources hold no answer.

| Story | Points | État |
|---|---|---|
| En tant que développeur, j'obtiens une réponse sourcée (rag) : réponse de Claude à partir des seuls passages retrouvés (blocs search_result, citations ramenées au fichier et aux lignes), refus quand les sources ne répondent pas, API FastAPI, Qdrant et l'API dans docker compose ; évaluation des réponses (citations justes, refus justes) ; fiche du projet avec résultats et exemples enregistrés. | 5 | Fait |

**Résultat :** les réponses citées (blocs `search_result`, citations ramenées au fichier et aux lignes), la garde « pas de citation, pas de réponse », l'API FastAPI, Qdrant et l'API dans docker compose, l'évaluation des réponses écrite et testée, la fiche avec les mesures de la recherche (G6 à G8). **Pas fait :** Charles a choisi de garder le projet en prototype sans clé d'API : l'évaluation des réponses n'a pas été lancée, et la fiche n'a pas d'exemples enregistrés ; le job « answers » de la CI les produira le jour où le secret `ANTHROPIC_API_KEY` existera.

**Tests :** 33 tests pytest (faux client pour les réponses), Docker de bout en bout ; 12 mutations, 12 attrapées. **Trouvé en route :** des chemins cherchés dans `site-packages` une fois le paquet installé, un ordre de fichiers différent sous Windows, un motif faux dans le test de fumée.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
