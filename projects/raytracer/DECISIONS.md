# Décisions, raytracer

## R1. Tracé de chemins avec échantillonnage direct de la lumière
**Choix :** chaque rebond diffus vise la lumière sphérique dans le cône qu'elle occupe (pdf uniforme sur l'angle solide) et rebondit selon le cosinus ; le métal et le verre suivent la direction miroir ou réfractée.
**Pourquoi :** des ombres douces et un éclairage indirect (le sol pistache sous la sphère) qui convergent en quelques dizaines de passes, ce que demande un rendu progressif dans le navigateur.
**Limite :** pas de caustiques (voir le README) ; une seule lumière.

## R2. Un générateur par pixel et par passe
**Choix :** SplitMix64, graine tirée de (x, y, numéro de passe).
**Pourquoi :** l'image ne dépend pas de l'ordre des bandes ni du nombre de workers ; les tests comparent des images entières octet pour octet (bandes, BVH contre force brute).

## R3. Image de référence comparée par PSNR
**Choix :** deux images de 128 × 72 à 16 passes, commitées en PPM, comparées à plus de 45 dB.
**Pourquoi :** `sin`, `cos` et `tan` ne sont pas arrondis pareil par les bibliothèques des trois systèmes ; une comparaison exacte échouerait pour un ulp, un écart réel (lumière, matériau) fait tomber le PSNR bien plus bas.
**Limite :** une régression qui ne change que quelques pixels peut passer ; les tests de physique couvrent le fond.

## R4. lib-c pour lire, `double` partout
**Choix :** même lecteur que topologie (OBJ, PLY, STL) ; calculs en `double`.
**Pourquoi :** fil rouge ; en WebAssembly, `double` coûte à peine plus que `float`, et les tests de BVH comparent des distances exactes.

## R5. Un web worker, des bandes, une génération par demande
**Choix :** le module tourne dans un seul web worker qui calcule des bandes de 24 lignes et rend la main entre deux (`setTimeout(0)`), poste l'image après chaque passe complète et s'arrête à 256 passes. Chaque changement de scène ou de vue porte un numéro de génération que le worker renvoie avec ses images ; la page ignore celles d'une génération plus ancienne.
**Pourquoi :** la page reste fluide pendant le calcul ; une bande dure quelques millisecondes, un nouveau réglage est donc pris en compte presque aussitôt ; sans la génération, une image de l'ancienne scène déjà en route remplaçait le compteur remis à zéro (vu en relecture, voir REVIEW).
**Alternatives :** plusieurs workers (SharedArrayBuffer exige des en-têtes COOP/COEP que GitHub Pages n'envoie pas, et des copies d'images sinon) ; WebGPU (un autre moteur, sans lien avec le C++ testé).
**Limite :** un seul cœur utilisé.

## R6. Build WebAssembly commité, vérifié par la CI
**Choix :** comme topologie (D15, D16) : `scripts/build-wasm.sh` compile dans l'image `emscripten/emsdk:6.0.11`, la sortie est commitée et un job de CI la recompile et la compare octet pour octet. Exceptions WebAssembly natives (`-fwasm-exceptions`), pour garder les `LoadError` de lib-c.
**Pourquoi :** le site se construit sans Emscripten ; la CI garantit que ce qui est servi correspond aux sources testées.
**Limite :** les exceptions WebAssembly natives demandent un navigateur de 2022 ou plus récent.
