# Sprint 33 : le lancer de rayons, moteur C++ et rendu en ligne

**Objectif :** premier des deux sprints du lancer de rayons (D45) : un moteur C++ testé qui rend les maillages du fil rouge, compilé en WebAssembly et utilisable en ligne sur la fiche du projet.

**Goal:** first of the two ray tracer sprints (D45): a tested C++ engine that renders the common-thread meshes, compiled to WebAssembly and usable online on the project page.

| Story | Points | État |
|---|---|---|
| En tant que développeur graphique, je calcule une image par lancer de rayons (raytracer) : C++20, sphères, plans et maillages lus par lib-c (OBJ, PLY, STL), triangles intersectés par Möller-Trumbore, BVH découpée selon la surface (SAH), matériaux diffus, métal et verre (Fresnel par Schlick), lumière et ombres, ciel, anticrénelage par échantillons tirés d'un générateur déterministe ; GoogleTest (intersections, BVH contre force brute, image de référence) ; CI Linux, Windows et macOS, ASan et UBSan. | 3 | Fait |
| En tant que visiteur, je lance un rendu sur la fiche (raytracer) : moteur compilé en WebAssembly (Emscripten épinglé, build commité et vérifié par la CI), rendu progressif dans un web worker, choix de la scène et de la caméra, FR/EN, accessible au clavier et sur mobile ; Playwright. | 2 | Fait |

**Tests :** 32 tests GoogleTest (optique, intersections, BVH contre force brute, fournaise blanche, irradiance analytique, deux images de référence) sur Linux, Windows et macOS, plus ASan et UBSan ; 4 tests Node du module WebAssembly (mêmes images de référence que le build natif, bandes dans le désordre, erreurs de lib-c) et 1 test de la fiche ; 15 tests Playwright (5 navigateurs : rendu progressif, changement de scène, clavier, pause, axe, 375 px) ; job de CI qui recompile le WebAssembly et le compare octet pour octet. **Trouvé en route :** la ligne d'erreur de lib-c lue avant l'appel (même erreur que dans topologie) ; deux tests de physique mal posés ; des caustiques en pixels blancs ; `-Wsign-conversion` vu seulement par Clang ; MSVC qui refuse `sscanf`, `getenv` et une division par zéro voulue ; le compteur de passes qui gardait la valeur de l'ancienne scène (le test Playwright passait sur la mauvaise image).

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
