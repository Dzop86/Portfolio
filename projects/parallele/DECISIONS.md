# Décisions, parallele

## P1. Deux passes plutôt qu'une boucle sur les faces
**Choix :** une passe par face qui écrit les angles et les parts d'aire de ses coins dans des tableaux, puis une passe par sommet qui somme ses coins.
**Pourquoi :** la boucle de topologie ajoute la contribution de chaque face à ses trois sommets ; en parallèle, deux faces voisines écriraient le même sommet (il faudrait des atomiques sur des doubles, ou des verrous). Les deux passes n'ont aucune écriture partagée, et la même structure sert à OpenMP, OpenCL et CUDA.
**Limite :** deux tableaux de 3T doubles en plus (48 octets par triangle).

## P2. Le même ordre d'addition partout
**Choix :** les coins de chaque sommet sont rangés par numéro de face (tri par comptage) ; les deux passes sont écrites une seule fois (`kernels.hpp`) ; la somme totale se fait dans l'ordre des sommets, hors de la région parallèle.
**Pourquoi :** l'addition des doubles n'est pas associative ; dans le même ordre, OpenMP donne exactement le résultat séquentiel, lui-même exactement celui de topologie. Les tests peuvent alors exiger l'égalité au bit, et non une tolérance qui masquerait une erreur.
**Limite :** une réduction parallèle de la somme totale serait plus rapide sur des milliards de sommets ; elle donnerait un total différent dans les derniers bits.

## P3. Double précision et tolérance argumentée pour OpenCL
**Choix :** noyaux en double, `FP_CONTRACT OFF` ; appareils sans `cl_khr_fp64` écartés ; comparaison au séquentiel à 1e-12, la courbure K vérifiée multipliée par l'aire.
**Pourquoi :** les fonctions `atan2`, `tan` et `sqrt` d'OpenCL peuvent différer de la bibliothèque C dans le dernier bit ; K = défaut / aire divise cette erreur par l'aire du sommet (1e-4 sur un maillage fin), d'où un écart de 2e-11 sur K pour 2e-15 sur le défaut (mesuré). Comparer K × aire, c'est comparer le défaut qu'il représente.
**Limite :** les cartes graphiques grand public calculent en double bien plus lentement qu'en simple précision ; une version en float sera mesurée au sprint 29.

## P4. OpenCL exécuté sur le processeur en CI
**Choix :** PoCL (OpenCL sur processeur) sous Linux, avec `PAR_REQUIRE_OPENCL=1` : un appareil manquant fait échouer les tests au lieu de les sauter.
**Pourquoi :** les machines de GitHub n'ont pas de carte graphique ; PoCL exécute vraiment les noyaux, avec le même compilateur OpenCL C qu'ailleurs.
**Limite :** les performances de PoCL ne disent rien d'une carte graphique.

## P5. CUDA : les mêmes passes, en double et en simple précision
**Choix :** les deux passes en noyaux CUDA (`src/cuda.cu`) paramétrés par le type flottant, compilés avec `--fmad=false` (deux arrondis pour a × b + c, comme le C++) ; une version double (comparée au séquentiel à 1e-12, comme OpenCL) et une version float (écart mesuré). En float, π est découpé en `hi = float(π)` + `lo = π − hi`, le reste étant rajouté après la somme des angles. Cartes Turing (sm_75) et suivantes, avec du PTX pour les futures.
**Pourquoi :** la GTX 1660 calcule 32 fois moins vite en double qu'en simple ; la version float montre ce qu'on gagne et ce qu'on perd. Sans le découpage, chaque défaut part de `float(2π)`, trop grand de 1,75e-7 : l'erreur est la même sur tous les sommets et s'additionne (Gauss-Bonnet à 0,023 sur 120 000 sommets ; 0,0017 avec le découpage, mesuré). En double, `lo` vaut exactement 0 et rien ne change.
**Limite :** la CI n'a pas de carte : elle compile les noyaux dans l'image `nvidia/cuda` et saute les tests ; ils ne tournent que sur la GTX 1660, en local. Pas de CUDA sur macOS (NVIDIA l'a abandonné), ni en CI Windows.

## P6. Des benchmarks qui mesurent le calcul
**Choix :** `parbench` chronomètre chaque version sur des tores de taille croissante : un appel d'échauffement, puis la médiane de 7 ; voisinage construit à part ; copies vers la carte et retour comprises, car c'est ce que paie un utilisateur pour une courbure ; temps des copies et des noyaux mesurés par événements CUDA. Le contexte et le programme OpenCL compilé sont gardés par appareil. OpenMP sur 6 threads, un par cœur physique. Les mesures sont versionnées (`data/bench.json`) et lues par le générateur du site.
**Pourquoi :** avant le cache, chaque appel OpenCL recompilait le noyau (45 ms avec PoCL) et le banc mesurait la compilation ; sur 12 threads, les threads en attente active sur les cœurs logiques ralentissent les autres (mesuré). Un test du site vérifie que les mesures sont complètes et cohérentes (versions exactes à 0, double à 1e-12, temps total au moins égal aux copies plus le calcul).
**Limite :** une seule machine, sous WSL 2 (bande passante mémoire plafonnée à 5 ou 6 Go/s) ; OpenCL tourne sur le processeur (PoCL), pas sur la carte. Les mesures ne sont refaites qu'à la main.
