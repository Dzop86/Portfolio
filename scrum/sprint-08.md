# Sprint 8 : apprendre à reconnaître des formes 3D

**Objectif :** le projet ml entraîne, sur des formes 3D générées par le code, un modèle de référence (scikit-learn) et un PointNet (PyTorch), suivis dans MLflow, reproductibles avec DVC, et la CI échoue si la précision passe sous le seuil.

| Story | Points | État |
|---|---|---|
| En tant que chercheur, je génère un jeu de formes 3D synthétiques (ml) : six classes, paramètres et bruit aléatoires, graine fixée, pipeline DVC, tests pytest. | 3 | Fait |
| En tant que chercheur, j'entraîne et compare un modèle de référence et un PointNet (ml), suivis dans MLflow ; la CI échoue sous le seuil de précision. | 3 | Fait |

**Résultats :** référence 97,2 %, PointNet 95,2 % sur 600 nuages de test ; confusion surtout entre boîte et cylindre.

**Tests :** ml 17 tests pytest sur 3 OS, pipeline DVC complet et seuil en CI.

**Reporté (décision de Charles, 6 octobre) :** export ONNX du modèle, classification dans l'API et page de résultats, après les projets Ada, SQL, mini-langage, LaTeX et G-cartes.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
