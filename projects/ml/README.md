# ml : reconnaître des formes 3D

[![ml](https://github.com/Dzop86/Portfolio/actions/workflows/ml.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/ml.yml)

Classification de formes 3D (sphère, tore, boîte, cylindre, cône, gélule) à partir de nuages de points. Les données sont générées par le code : aucune donnée de laboratoire, aucune donnée médicale.

*3D shape classification on synthetic point clouds: DVC pipeline, scikit-learn baseline and PointNet (PyTorch), tracked in MLflow, CI-gated on accuracy.*

## État (sprint 8)
- **Jeu de données** (`src/shapeml/`) : six classes de maillages fermés aux dimensions, résolutions, rotations et bruits aléatoires ; 512 points tirés uniformément sur la surface, centrés et ramenés dans la sphère unité. 300 nuages par classe pour l'entraînement, 100 pour le test, avec des graines différentes.
- **DVC** : `dvc.yaml` décrit le pipeline, `params.yaml` ses paramètres, `dvc.lock` les empreintes des données produites. Les données (13,6 Mo) restent dans le cache DVC, hors de git.

## Lancer
```sh
python -m pip install ".[test,dvc]"
python -m pytest
dvc repro            # régénère data/shapes.npz si le code ou les paramètres ont changé
```

## Tests
- **Unitaires** (`tests/test_shapes.py`) : chaque classe donne un maillage valide, de caractéristique d'Euler 2 (0 pour le tore), comme le vérifie le projet Topologie 3D ; les formes varient vraiment.
- **Échantillonnage** (`tests/test_dataset.py`) : points sur la surface, tirage proportionnel à l'aire, normalisation, déterminisme par la graine, classes équilibrées, test disjoint de l'entraînement.
- **CI** (`.github/workflows/ml.yml`) : tests sur Linux, Windows et macOS ; pipeline DVC reconstruit de zéro, et `dvc.lock` ne doit pas changer.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
