# Sprint 6 : le STL, puis l'API Python

**Objectif :** lib-c lit le STL, ce qui permet de déposer des modèles d'impression 3D dans les démos ; le projet fastapi démarre et expose lib-c par HTTP.

| Story | Points | État |
|---|---|---|
| En tant que Charles, je dépose un fichier STL (binaire ou ASCII) dans les démos : lib-c le lit, soude les sommets identiques et écarte les facettes dégénérées. | 2 | Fait |
| En tant que développeur, j'interroge lib-c par HTTP : squelette du projet fastapi (FastAPI, pytest, CI Linux, Windows et macOS) avec un point d'accès qui renvoie les statistiques d'un maillage envoyé. | 3 | Fait |

**Tests :** lib-c 11 tests STL de plus (et 15 millions d'entrées fuzzées), fastapi 14 tests pytest sur 3 OS × 2 versions de Python, site 171 + 210 end-to-end.

**Décisions de Charles (6 octobre) :** pas de miroir GitLab ; les modèles Pokémon restent hors du dépôt (droits de Nintendo), montrés en local par glisser-déposer ; le modèle « chocolat » n'est pas publié (source et licence inconnues, décision du 6 octobre).

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
