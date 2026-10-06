# Relecture humaine, ml (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/shapeml/shapes.py` : les classes sont-elles assez variées, et pas trop faciles ?
- [ ] `src/shapeml/pointnet.py` : invariance par permutation (max sur les points), augmentation par rotation.
- [ ] `src/shapeml/dataset.py` : tirage uniforme sur la surface (racine carrée sur la coordonnée barycentrique).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/test_models.py` | (Claude) `pytest.importorskip("torch")` aurait sauté en silence tous les tests du PointNet sur une machine sans PyTorch, CI comprise | Import direct ; la CI installe PyTorch (CPU) |
| 2026-10-06 | `tests/test_export.py` | (Claude) Mon test supposait que `train_pointnet` renvoie le réseau seul (il renvoie aussi l'historique des pertes) | Tuple déballé |
| 2026-10-06 | `src/shapeml/export.py` | (Claude) L'exporteur TorchScript émet des avertissements de dépréciation, fatals sous `pytest -W error` | Ignorés autour de l'appel d'export seulement (M5) |
| 2026-10-06 | `../../src/assets/style.css` | (Claude) Détecté par axe : 12 cases des matrices de confusion sous le seuil de contraste en thème sombre (texte clair sur teinte pistache) | Couleurs des badges de risque, déjà validées ; plus de teinte selon l'effectif (D27) |
| | | | |
