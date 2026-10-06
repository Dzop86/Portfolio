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
