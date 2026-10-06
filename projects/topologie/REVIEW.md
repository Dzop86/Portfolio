# Relecture humaine, topologie (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/mesh.cpp`, constructeur : appariement des jumelles, cas des arêtes partagées par 3 faces.
- [ ] `src/invariants.cpp` : formule du genre et traitement des sommets isolés.
- [ ] `src/curvature.cpp` : défaut au bord (π − Σθ) et sommets non-variété.
- [ ] `tests/test_halfedge.cpp`, `expect_consistent` : les invariants vérifiés sont-ils les bons ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/mesh.cpp` | (Claude) Détecté par les tests : `check(mesh_read_buffer(..., &line), line)` lisait `line` avant l'appel (ordre d'évaluation des arguments non spécifié en C++), les erreurs remontaient toujours en ligne 0 | Appel et lecture de `line` en deux instructions |
| 2026-10-06 | `src/curvature.cpp` | (Claude) Indice de boucle en `int` sur un `std::array` : Clang (macOS) inclut `-Wsign-conversion` dans `-Wconversion`, contrairement à GCC en C++, et la CI macOS aurait échoué avec `-Werror` | Indice en `std::size_t`, vérifié avec Clang 19 dans Docker |
| | | | |
