# ml : reconnaître des formes 3D

[![ml](https://github.com/Dzop86/Portfolio/actions/workflows/ml.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/ml.yml)

Classification de formes 3D (sphère, tore, boîte, cylindre, cône, gélule) à partir de nuages de points. Les données sont générées par le code : aucune donnée de laboratoire, aucune donnée médicale.

*3D shape classification on synthetic point clouds: DVC pipeline, scikit-learn baseline and PointNet (PyTorch), tracked in MLflow, CI-gated on accuracy.*

## État (sprint 8)
- **Jeu de données** (`src/shapeml/`) : six classes de maillages fermés aux dimensions, résolutions, rotations et bruits aléatoires ; 512 points tirés uniformément sur la surface, centrés et ramenés dans la sphère unité. 300 nuages par classe pour l'entraînement, 100 pour le test, avec des graines différentes.
- **Modèles** : un modèle de référence (scikit-learn, `HistGradientBoostingClassifier` sur des descripteurs invariants par rotation : distribution des distances entre points D2, distances au centre, valeurs propres de la covariance) et un PointNet réduit (PyTorch, 40 époques sur CPU, rotations aléatoires en augmentation).
- **Résultats** (600 nuages de test, `metrics.json`) :

  | Modèle | Précision | F1 macro | Entraînement (CPU) |
  |---|---|---|---|
  | Référence (descripteurs + boosting) | 97,2 % | 0,971 | 33 s |
  | PointNet | 95,2 % | 0,951 | 4 min 20 |

  Sur ces formes simples, des descripteurs invariants bien choisis battent le réseau. Les erreurs viennent presque toutes de la confusion boîte / cylindre (une boîte plate vue sous certains angles ressemble à un cylindre court) ; sphère, tore, cône et gélule sont reconnus à 100 %.
- **MLflow** : paramètres, métriques, courbe de perte de chaque entraînement dans `mlflow.db` (`mlflow ui --backend-store-uri sqlite:///mlflow.db`) ; la CI joint la base à chaque exécution.
- **Seuil** : l'étape `check` échoue si la meilleure précision passe sous `check.min_accuracy` (0,90), ce qui fait échouer la CI.
- **DVC** : `dvc.yaml` décrit le pipeline, `params.yaml` ses paramètres, `dvc.lock` les empreintes des données produites. Les données (13,6 Mo) restent dans le cache DVC, hors de git.

## Export ONNX (sprint 16)
- L'étape DVC `export` (`src/shapeml/export.py`) exporte le PointNet en ONNX (`export/pointnet.onnx`, 300 Ko, gardé dans git) et vérifie l'export : sur les 600 nuages de test, les logits d'ONNX Runtime ne s'écartent de ceux de PyTorch que de 1,8·10⁻⁵ au plus (seuil 10⁻⁴, sinon l'étape échoue), et la précision mesurée par ONNX Runtime, 95,17 %, est celle de PyTorch. `export/pointnet.json` garde les classes, le nombre de points, cette précision et l'empreinte SHA-256 du modèle.
- L'étape `check` applique aussi le seuil de précision au modèle exporté, celui que sert l'API.
- `src/shapeml/infer.py` fait l'inférence sur un maillage avec le même prétraitement qu'à l'entraînement (échantillonnage pondéré par l'aire, centrage, sphère unité, graine fixe) ; il ne dépend que de numpy et d'onnxruntime (`pip install ".[serve]"`), pas de PyTorch.

## Lancer
```sh
python -m pip install torch --index-url https://download.pytorch.org/whl/cpu
python -m pip install ".[train,test,dvc]"
python -m pytest
dvc repro            # données, entraînement, évaluation et seuil, selon ce qui a changé
```

## Tests
- **Unitaires** (`tests/test_shapes.py`) : chaque classe donne un maillage valide, de caractéristique d'Euler 2 (0 pour le tore), comme le vérifie le projet Topologie 3D ; les formes varient vraiment.
- **Échantillonnage** (`tests/test_dataset.py`) : points sur la surface, tirage proportionnel à l'aire, normalisation, déterminisme par la graine, classes équilibrées, test disjoint de l'entraînement.
- **Modèles** (`tests/test_models.py`) : descripteurs invariants par rotation et par ordre des points, PointNet invariant par permutation des points, apprentissage sur un petit jeu, seuil de précision.
- **Export** (`tests/test_export.py`) : parité PyTorch / ONNX Runtime sur un petit modèle entraîné dans le test, lots de toute taille, probabilités, prétraitement déterministe et identique à l'entraînement, maillages sans surface refusés ; le modèle commité correspond à son empreinte et reconnaît au moins 90 % de 120 nuages neufs. Vérifié en cassant le code : sans normalisation à l'inférence, un test échoue.
- **CI** (`.github/workflows/ml.yml`) : tests sur Linux, Windows et macOS ; données régénérées de zéro avec la même empreinte que `dvc.lock` ; entraînement complet et seuil de précision à chaque modification du projet.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
