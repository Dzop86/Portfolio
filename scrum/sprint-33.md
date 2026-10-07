# Sprint 33 : le lancer de rayons, moteur C++ et rendu en ligne

**Objectif :** premier des deux sprints du lancer de rayons (D45) : un moteur C++ testé qui rend les maillages du fil rouge, compilé en WebAssembly et utilisable en ligne sur la fiche du projet.

**Goal:** first of the two ray tracer sprints (D45): a tested C++ engine that renders the common-thread meshes, compiled to WebAssembly and usable online on the project page.

| Story | Points | État |
|---|---|---|
| En tant que développeur graphique, je calcule une image par lancer de rayons (raytracer) : C++20, sphères, plans et maillages lus par lib-c (OBJ, PLY, STL), triangles intersectés par Möller-Trumbore, BVH découpée selon la surface (SAH), matériaux diffus, métal et verre (Fresnel par Schlick), lumière et ombres, ciel, anticrénelage par échantillons tirés d'un générateur déterministe ; GoogleTest (intersections, BVH contre force brute, image de référence) ; CI Linux, Windows et macOS, ASan et UBSan. | 3 | Fait |
| En tant que visiteur, je lance un rendu sur la fiche (raytracer) : moteur compilé en WebAssembly (Emscripten épinglé, build commité et vérifié par la CI), rendu progressif dans un web worker, choix de la scène et de la caméra, FR/EN, accessible au clavier et sur mobile ; Playwright. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
