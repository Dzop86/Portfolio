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
