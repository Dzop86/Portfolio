# Sprint 16 : le modèle de formes en production

**Objectif :** le PointNet du projet ml quitte l'entraînement : exporté en ONNX et vérifié identique au modèle PyTorch, il classe les maillages envoyés à l'API (`POST /v1/mesh/classify`, sans PyTorch dans l'image), et ses résultats sont présentés sur la fiche du projet.

**Goal:** the ml project's PointNet leaves training: exported to ONNX and checked identical to the PyTorch model, it classifies the meshes sent to the API (`POST /v1/mesh/classify`, without PyTorch in the image), and its results are shown on the project page.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur ML, j'exporte le PointNet en ONNX (ml) dans une étape DVC : sorties ONNX Runtime égales à celles de PyTorch sur tout le jeu de test, précision mesurée par ONNX Runtime et reportée avec le modèle ; tests pytest, CI. | 3 | Fait |
| En tant qu'utilisateur de l'API, j'envoie un maillage et je reçois sa classe et les probabilités (ml) : `POST /v1/mesh/classify`, même prétraitement qu'à l'entraînement (code partagé), ONNX Runtime seul dans l'image ; tests pytest et Docker. | 3 | Fait |
| En tant que recruteur, je vois les résultats du modèle (ml) sur la fiche du projet : précision des deux modèles, seuil de la CI, matrices de confusion ; bilingue, testé. | 1 | Fait |

**Résultats :** export ONNX identique à PyTorch (écart des logits 1,8·10⁻⁵, précision 95,17 % des deux côtés) ; l'API reconnaît les formes générées et le cube de lib-c ; image de l'API sans PyTorch (environ 410 Mo).

**Tests :** ml 24 tests pytest, API 33 tests pytest et test de fumée Docker, site (test des résultats, axe dans les deux thèmes).

## Rétro (Charles)
- Ce qui a marché : Export ONNX identique à PyTorch, classification dans l'API sans PyTorch, résultats sur la fiche.
- Ce que l'IA a mal fait : Rien de notable.
- À changer au prochain sprint : Retour aux jeux : Othello.
