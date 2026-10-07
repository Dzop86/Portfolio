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
