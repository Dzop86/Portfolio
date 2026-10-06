# Sprint 16 : le modèle de formes en production

**Objectif :** le PointNet du projet ml quitte l'entraînement : exporté en ONNX et vérifié identique au modèle PyTorch, il classe les maillages envoyés à l'API (`POST /v1/mesh/classify`, sans PyTorch dans l'image), et ses résultats sont présentés sur la fiche du projet.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur ML, j'exporte le PointNet en ONNX (ml) dans une étape DVC : sorties ONNX Runtime égales à celles de PyTorch sur tout le jeu de test, précision mesurée par ONNX Runtime et reportée avec le modèle ; tests pytest, CI. | 3 | À faire |
| En tant qu'utilisateur de l'API, j'envoie un maillage et je reçois sa classe et les probabilités (ml) : `POST /v1/mesh/classify`, même prétraitement qu'à l'entraînement (code partagé), ONNX Runtime seul dans l'image ; tests pytest et Docker. | 3 | À faire |
| En tant que recruteur, je vois les résultats du modèle (ml) sur la fiche du projet : précision des deux modèles, seuil de la CI, matrices de confusion ; bilingue, testé. | 1 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
