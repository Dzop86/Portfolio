# Décisions, lib-c

## C1. C11 et CMake, sans dépendance d'exécution
**Choix :** C11 strict (`CMAKE_C_EXTENSIONS OFF`), CMake 3.20, aucune bibliothèque à l'exécution (seulement `libm`).
**Pourquoi :** compile tel quel avec GCC, Clang et MSVC, et plus tard avec Emscripten.

## C2. Unity récupéré par FetchContent, figé sur v2.6.1
**Choix :** `FetchContent` télécharge Unity au tag `v2.6.1` ; `UNITY_INCLUDE_DOUBLE` est activé pour comparer des coordonnées.
**Alternatives :** copier Unity dans le dépôt (code tiers à maintenir), CMocka ou Check (moins simples à compiler sous MSVC).
**Limite :** le premier `cmake` demande un accès réseau ; un tag peut en théorie être déplacé (un hash de commit serait plus sûr).

## C3. Triangulation en éventail à la lecture
**Choix :** chaque polygone de `n` sommets donne `n - 2` triangles `(premier, précédent, courant)` ; `polygon_count` garde le nombre de faces du fichier.
**Pourquoi :** les calculs de topologie à venir (caractéristique d'Euler, genre) travaillent sur des triangles.
**Limite :** correct pour les polygones convexes ; un polygone concave peut donner des triangles qui débordent.

## C4. Une ligne copiée dans un tampon terminé par NUL
**Choix :** le texte est découpé ligne à ligne et chaque ligne est copiée avant `strtod`/`strtoll`.
**Pourquoi :** ces fonctions sautent les espaces, retours à la ligne compris : sur `v 1 2\n3`, une lecture directe aurait pris le `3` de la ligne suivante. Un test couvre ce cas.

## C5. Lecture depuis une chaîne, le fichier n'est qu'une enveloppe
**Choix :** `mesh_read_obj_file` charge tout le fichier puis appelle `mesh_read_obj_string`.
**Pourquoi :** les tests n'ont pas besoin de fichiers temporaires, et `fmemopen` n'existe pas sous Windows. Le même point d'entrée servira à la démo WebAssembly.
**Limite :** le fichier entier tient en mémoire en plus du maillage.

## C6. PLY : en-tête borné, comptes vérifiés avant toute allocation
**Choix :** l'en-tête est décrit par des tableaux de taille fixe (16 éléments, 32 propriétés) ; avant de lire le corps, chaque compte d'élément est comparé au nombre minimal d'octets qu'il occupe (taille binaire des propriétés, ou 1 octet par valeur en ASCII). Un élément non vide sans propriété est refusé.
**Pourquoi :** le lecteur servira dans le navigateur sur des fichiers déposés par n'importe qui ; un en-tête annonçant 4 milliards de sommets ne doit ni allouer ni boucler.
**Limite :** des fichiers PLY exotiques (plus de 16 éléments) sont refusés.

## C7. Fuzzing des lecteurs avec libFuzzer
**Choix :** `tests/fuzz/fuzz_read.c` passe chaque entrée à `mesh_read_buffer` (OBJ ou PLY) sous ASan et UBSan, et vérifie que les indices produits restent dans les bornes. La CI fuzz 60 s par push ; les entrées trouvées deviennent des tests Unity.
**Pourquoi :** les tests écrits à la main n'avaient trouvé aucun des trois défauts que le fuzzer a révélés en quelques minutes (voir `REVIEW.md`).
**Limite :** 60 s en CI ne remplacent pas une campagne longue ; le corpus n'est pas conservé entre deux exécutions.

## C8. Topologie par tri des arêtes
**Choix :** chaque arête devient une clé 64 bits (petit indice en poids fort), les 3T clés sont triées par `qsort` ; clés distinctes = arêtes, clés uniques = arêtes de bord. F = nombre de triangles.
**Alternatives :** table de hachage (O(T) mais plus de code et de mémoire à justifier), structure demi-arête (prévue pour le projet Topologie 3D).
**Limite :** O(T log T) ; χ porte sur la triangulation, ce qui ne change rien pour une surface (la triangulation en éventail ajoute autant d'arêtes que de faces).

## C9. STL : soudure exacte des sommets, ordre du fichier conservé
**Choix :** le STL binaire est reconnu à sa taille exacte (84 + 50 × facettes), pas à son en-tête, car certains exportateurs commencent l'en-tête binaire par `solid`. Les coins sont soudés par égalité exacte des coordonnées (tri, −0 confondu avec 0), puis numérotés dans l'ordre de leur première apparition ; les facettes devenues dégénérées sont écartées.
**Pourquoi :** sans soudure, un STL donne autant de composantes que de triangles et une caractéristique d'Euler sans sens. Les exportateurs écrivent les mêmes flottants pour un même sommet, l'égalité exacte suffit.
**Alternatives :** soudure à tolérance (fusionnerait des sommets voisins mais distincts, et change la topologie selon le réglage).
**Limite :** deux sommets distants d'un ulp restent distincts ; `polygon_count` compte les facettes gardées, pas celles du fichier.

## C10. Octets NUL de fin de fichier tolérés en OBJ
**Choix :** les octets NUL et blancs en fin de fichier OBJ sont ignorés ; un NUL ailleurs reste une erreur de syntaxe.
**Pourquoi :** un export ZBrush réel se termine par un octet NUL ; le refuser rendait le fichier illisible pour une raison sans effet sur le maillage.
