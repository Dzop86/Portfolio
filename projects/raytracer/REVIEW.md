# Relecture humaine, raytracer (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/render.cpp` : tracé de chemins, échantillonnage de la lumière (pdf du cône), matériaux, roulette russe.
- [ ] `src/bvh.cpp` : coût SAH, partition, parcours (pile bornée).
- [ ] `tests/test_render.cpp` : les tests de physique vérifient-ils vraiment la bonne chose ?
- [ ] `src/assets/raytracer-worker.js`, `src/assets/raytracerplay.js` : bandes, générations, pause, glisser sur l'image (sprint 33, story 2).
- [ ] Sprint 34 : `src/assets/raytracer-bands.js` (bandes par worker), `src/scenes.cpp` (corps noir, `apply_settings`), fichier du visiteur et ses erreurs dans `raytracerplay.js`.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/mesh.cpp` | (Claude) Détecté par un test : `check(mesh_read_buffer(..., &line), line)` lisait `line` avant l'appel, l'erreur remontait en ligne 0. **Même erreur que dans topologie le 6 octobre**, reproduite par l'IA | Appel et lecture de `line` en deux instructions |
| 2026-10-07 | `tests/test_render.cpp` | (Claude) Test faux : le rayon de la fournaise blanche passait à 1,09 du centre d'une sphère de rayon 1 et ne la touchait pas | Rayon visé plus près du centre ; le test échouait bien avant |
| 2026-10-07 | `tests/test_render.cpp` | (Claude) Test d'ombre mal posé : le bloqueur couvrait exactement le cône de la lumière (bords), et, gris, il éclairait lui-même le sol | Bloqueur plus grand et noir |
| 2026-10-07 | `src/render.cpp` | (Claude) Vu sur une image de 640 × 360 : des pixels blancs isolés sur la sphère pistache et autour du tore de verre, des caustiques (rebond diffus, puis verre ou métal, puis lumière) | Ces chemins ne comptent plus la lumière ; limite écrite dans le README |
| 2026-10-07 | `src/bvh.cpp` | (Claude) Vu avec Clang (em++) : `begin() + first` avec un `uint32_t` déclenche `-Wsign-conversion`, que GCC ne signale pas : la CI macOS aurait échoué | Décalage converti en `std::ptrdiff_t` |
| 2026-10-07 | `tools/rt_render.cpp`, `tests/` | (Claude) CI Windows rouge : MSVC traite en erreurs `sscanf` et `getenv` (C4996) et une division par zéro voulue dans le test des boîtes (C4723) | `--size` lu avec `strtoul` ; `_CRT_SECURE_NO_WARNINGS` pour les seuls tests (lecture de `RT_UPDATE_REFERENCE`) ; C4723 coupé dans ce fichier de test, avec la raison |
| 2026-10-07 | `src/assets/raytracerplay.js` | (Claude) Vu sur une capture : après un changement de scène, le compteur de passes gardait la valeur de l'ancienne image ; le test Playwright « au moins 2 passes » passait donc sur les sphères et ne vérifiait pas le tore. Une image de l'ancienne scène déjà en route pouvait aussi arriver après la remise à zéro | Compteur remis à zéro à chaque scène ou vue, numéro de génération renvoyé par le worker (images périmées ignorées) ; le test vérifie maintenant que l'image change |
| 2026-10-07 | `tests/unit/site.test.mjs` | (Claude) Le test « liens affichés seulement si le projet en a » cherchait un projet sans liens dans les données ; le raytracer en ayant reçu, il n'en restait plus et le test échouait | Le cas est vérifié sur une copie d'un projet, liens retirés |
| 2026-10-07 | `DECISIONS.md` (R5) | (Claude) J'avais écarté plusieurs workers au sprint 33 en invoquant SharedArrayBuffer : faux, chaque worker garde ses propres lignes et n'a besoin d'aucune mémoire partagée | R5 réécrite avec la vraie raison et la mesure |
| 2026-10-07 | `src/assets/raytracerplay.js` | (Claude) Vu par Playwright sous WebKit : le compteur de passes avançait à la réception des bandes, avant que l'image soit dessinée (au prochain `requestAnimationFrame`) ; le test lisait un canevas encore vide | Le compteur et la progression changent au moment du dessin |
| 2026-10-07 | `src/templates.mjs` | (Claude) En réutilisant les libellés d'erreur de la démo lib-c, la clé `error` (« Erreur : ») écrasait celle du raytracer (« Le rendu a échoué : {message} ») dans le même objet | Libellés de lib-c rangés sous `mesh.` |
| 2026-10-07 | `include/rt/scenes.hpp` | (Claude) Détecté par un test : la couleur par défaut choisie à 5500 K s'écartait de 3 de la lampe d'origine (55,2 au lieu de 52 en rouge) | 5800 K, la plus proche (calculée de 5500 à 6600 K) |
| 2026-10-07 | `scripts/captures.mjs` | (Claude) Vu sur la capture « coucher de soleil » : la lampe basse (azimut −150°) tombait dans le champ, grand disque blanc près du sol | Lumière du côté de la caméra (60°, 18°), légende corrigée (« ombres longues » ne se voyait pas) |
| 2026-10-07 | `src/assets/raytracerplay.js`, `style.css` | (Claude) Vu sur une capture mobile : « 5800 K » passait à la ligne, et les décimales s'affichaient avec un point en français (0.06, 6.2) | Colonne des valeurs élargie et sans retour à la ligne ; valeurs formatées dans la langue de la page ; test Playwright sur « 0,06 » et « 6,2 » |
| | | | |
