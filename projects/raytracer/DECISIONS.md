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

## R5. Des web workers qui possèdent leurs bandes, une génération par demande
**Choix :** l'image est découpée en bandes de 8 lignes, distribuées tour à tour entre 1 et 8 workers (par défaut le nombre de cœurs moins un, pour laisser la page fluide ; le visiteur peut le changer). Chaque worker a son propre module WebAssembly, garde les échantillons de ses lignes, rend ses bandes passe après passe, en rendant la main entre deux (`setTimeout(0)`), et envoie les lignes de chaque bande ; la page les assemble et affiche le plus petit nombre de passes des bandes. Chaque changement de scène ou de vue porte un numéro de génération que les workers renvoient ; la page ignore les bandes d'une génération plus ancienne. Le compteur de passes change au moment où l'image est dessinée, pas à la réception d'une bande.
**Pourquoi :** le générateur par pixel (R2) donne la même image quel que soit le worker qui calcule une ligne (test Node octet pour octet avec trois modules) ; la distribution tour à tour équilibre le ciel, le sol et l'objet entre les workers ; une bande dure quelques millisecondes, un nouveau réglage est donc pris en compte presque aussitôt. Aucune mémoire partagée : pas besoin de SharedArrayBuffer, que GitHub Pages ne permet pas (en-têtes COOP et COEP).
**Mesure :** `scripts/bench-workers.mjs` rend l'image de la page avec 1, 2, 4 et 8 threads Node (même module, mêmes bandes) et écrit `data/bench.json`, affiché sur la fiche : sur un i5-10400F (6 cœurs, 12 threads), × 1,86 à 2, × 3,23 à 4, × 4,03 à 8.
**Alternatives :** un seul worker (sprint 33) ; des bandes distribuées à la demande au premier worker libre (meilleur équilibre, mais une ligne changerait de worker et perdrait ses échantillons) ; WebGPU (un autre moteur, sans lien avec le C++ testé).
**Limites :** chaque worker charge son module et sa copie du maillage (jusqu'à 8 × 32 Mo pour un gros fichier) ; le texte qui commente la mesure parle de ce processeur-là, à revoir si la mesure est refaite ailleurs.

## R6. Build WebAssembly commité, vérifié par la CI
**Choix :** comme topologie (D15, D16) : `scripts/build-wasm.sh` compile dans l'image `emscripten/emsdk:6.0.11`, la sortie est commitée et un job de CI la recompile et la compare octet pour octet. Exceptions WebAssembly natives (`-fwasm-exceptions`), pour garder les `LoadError` de lib-c.
**Pourquoi :** le site se construit sans Emscripten ; la CI garantit que ce qui est servi correspond aux sources testées.
**Limite :** les exceptions WebAssembly natives demandent un navigateur de 2022 ou plus récent.
