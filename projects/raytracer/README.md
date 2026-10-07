# raytracer : lancer de rayons en C++, utilisable en ligne

[![raytracer](https://github.com/Dzop86/Portfolio/actions/workflows/raytracer.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/raytracer.yml)

Un moteur de rendu par lancer de rayons (tracé de chemins) écrit en C++20, qui rend les maillages du fil rouge du portfolio (tore, sphère, ruban de Möbius, selle de `../topologie/samples`) lus par [lib-c](../lib-c/). Compilé en WebAssembly, il tourne dans le navigateur sur la fiche du projet (sprint 33, story 2).

*A C++20 path tracer for the portfolio's common-thread meshes (read by lib-c): SAH BVH, diffuse, metal and glass materials, soft shadows from a sampled spherical light, anti-aliasing, deterministic per-pixel random streams; GoogleTest checks against physics (white furnace, analytic irradiance) and a reference image; CI on Linux, Windows and macOS.*

## Ce qu'il fait
- **Géométrie :** sphères (racines sans annulation, précises loin de l'origine), plans, triangles (Möller-Trumbore, deux faces).
- **Maillages :** OBJ, PLY ou STL lus par lib-c, normales lissées pondérées par l'aire, qui tiennent aussi sur une surface non orientable (ruban de Möbius) ; posés sur le sol et ramenés dans une sphère de rayon 1.
- **BVH :** découpe selon l'heuristique de surface (SAH) sur 12 compartiments, feuilles de 4 triangles au plus, parcours itératif du plus proche au plus lointain ; vérifiée contre le test de tous les triangles un par un.
- **Lumière :** tracé de chemins ; à chaque rebond diffus, la lumière sphérique est échantillonnée directement dans le cône qu'elle occupe (ombres douces), puis le chemin rebondit selon le cosinus ; roulette russe après 3 rebonds.
- **Matériaux :** diffus (damier pour le sol), métal (flou réglable), verre (Snell, réflexion totale, Fresnel par Schlick).
- **Image :** anticrénelage par un point tiré au hasard dans chaque pixel à chaque passe, courbe filmique ACES puis gamma 2.
- **Rendu progressif :** chaque passe ajoute un échantillon par pixel, bande par bande ; le pixel (x, y) tire son échantillon k toujours du même générateur (SplitMix64), si bien que l'image ne dépend ni de l'ordre des bandes ni du nombre de workers qui les calculent.

## Organisation
- `include/rt/` : `math.hpp` (vecteurs, optique), `rng.hpp`, `geometry.hpp` (intersections), `mesh.hpp`, `bvh.hpp`, `scene.hpp`, `render.hpp` (caméra, tracé, rendu progressif), `scenes.hpp` (scènes de la fiche), `image.hpp` (PPM, PSNR).
- `src/` : leur code ; `tools/rt_render.cpp` : rendu en ligne de commande ; `tools/c_api.cpp` : interface C pour WebAssembly.
- `scripts/build-wasm.sh` : compile le moteur et le lecteur de lib-c en WebAssembly (Emscripten 6.0.11 épinglé, dans Docker) vers `src/assets/wasm/raytracer.{js,wasm}`, commités.
- `tests/` : GoogleTest ; `tests/reference/` : images de référence.

## Lancer
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --parallel
ctest --test-dir build --output-on-failure
build/rt_render --scene mesh --mesh ../topologie/samples/torus.obj --finish glass --size 640x360 --spp 128 -o tore.ppm
```
Options de `rt_render` : `--scene spheres|mesh`, `--mesh FICHIER`, `--finish diffuse|metal|glass`, `--size LxH`, `--spp N`, `--yaw`, `--pitch`, `--distance`, `--brute-force` (sans BVH), `-o FICHIER.ppm`.

## En ligne
Sur la fiche du projet, `src/assets/raytracer-worker.js` charge le module dans un web worker et calcule passe après passe, par bandes de 24 lignes (pour répondre vite à un changement de scène ou de caméra), jusqu'à 256 passes ; `src/assets/raytracerplay.js` affiche chaque passe dans un canevas de 480 × 270. Choix de la scène (trois sphères ou un des quatre maillages de topologie) et de la matière, orbite au clavier par les curseurs ou en faisant glisser l'image, pause, vue par défaut ; textes en français et en anglais, annonces pour les lecteurs d'écran.

```sh
scripts/build-wasm.sh          # régénère src/assets/wasm/raytracer.{js,wasm}
```

## Tests (32 GoogleTest, 5 Node, 15 Playwright)
- **Optique et hasard :** réflexion, loi de Snell à plusieurs angles, réflexion totale au-delà de l'angle critique, Schlick de 4 % à 100 %, base orthonormée, générateur reproductible et uniforme.
- **Intersections :** sphère de l'extérieur et de l'intérieur, petite sphère à un million d'unités, plan parallèle, triangle (barycentriques, deux faces), boîte avec un rayon posé sur une face (0 × ∞).
- **Maillages et BVH :** erreurs de lib-c avec leur ligne, normales de la sphère vers l'extérieur, normales du ruban de Möbius, mise à l'échelle ; même distance que la force brute sur 3 000 rayons par maillage et sur 2 000 triangles au hasard ; arbre valide (chaque triangle une fois, boîtes englobantes) ; triangles tous superposés.
- **Physique :** fournaise blanche (une sphère diffuse d'albédo 0,5 sous un ciel blanc uniforme renvoie exactement 0,5, le métal son albédo, le verre 1) ; sol sous une lumière sphérique égal à la valeur analytique albédo × Le × (r/d)² à 1 % ; ombre complète derrière un bloqueur ; la lumière vue directement n'est pas comptée deux fois.
- **Images :** bandes dans n'importe quel ordre = image entière, BVH = force brute pixel pour pixel, deux images de référence (PSNR au-dessus de 45 dB, pour absorber les derniers bits de `sin`, `cos` et `tan` qui diffèrent entre les bibliothèques standard). `RT_UPDATE_REFERENCE=1` les réécrit.

- **WebAssembly** (`tests/unit/raytracer-wasm.test.mjs`) : le module du site rend les deux images de référence du build natif (plus de 45 dB), bandes dans le désordre = passes entières, vue par défaut et remise à zéro, erreurs de lib-c avec leur ligne ; `tests/unit/raytracer-page.test.mjs` : la fiche, ses contrôles et ses textes dans les deux langues.
- **Playwright** (`tests/e2e/raytracer.spec.js`, 5 navigateurs) : l'image se construit, l'image change avec la scène, curseurs au clavier, pause, accessibilité (axe), pas de défilement horizontal.

**CI** (`.github/workflows/raytracer.yml`) : Linux, Windows et macOS, plus ASan et UBSan sous Linux ; avertissements en erreurs, contraction des multiplications-additions coupée (`-ffp-contract=off`) pour la même image partout. Un job recompile le WebAssembly et le compare octet pour octet au build commité.

## Limites
- Pas de caustiques : un chemin qui atteint la lumière à travers le verre ou un miroir après un rebond diffus n'est pas compté (rare et très lumineux, il laisserait des pixels blancs isolés pendant des centaines de passes). L'ombre d'une sphère de verre est donc sombre.
- Une seule lumière, sphérique ; pas de textures ; sol infini (le damier crénèle au loin).
- Un seul fil de calcul, natif comme en ligne (un seul web worker) : le générateur par pixel permettrait de répartir les bandes sur plusieurs workers sans changer l'image.
- Rendu en ligne en 480 × 270 : vers 90 ms par passe dans Chromium sur un ordinateur de bureau (mesuré), davantage sur un téléphone.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md) et D45 dans le [`DECISIONS.md` du site](../../DECISIONS.md).
