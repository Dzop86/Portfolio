# qt : visionneuse de maillages Qt/OpenGL

[![qt](https://github.com/Dzop86/Portfolio/actions/workflows/qt.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/qt.yml)

Application de bureau Qt 6 en C++20 qui ouvre un maillage OBJ, PLY ou STL, l'affiche en OpenGL et en montre la topologie. Elle ferme la boucle du fil rouge : le fichier est lu par [lib-c](../lib-c/), la structure demi-arête, les invariants et la courbure viennent de [topologie](../topologie/).

*Qt 6 / OpenGL 3.3 desktop mesh viewer in C++20: reads OBJ, PLY and STL with the portfolio's C library, shows the topology computed by its C++ project (Euler characteristic, genus, boundaries, non-manifold elements, orientability, Gaussian curvature colours), in French and English, tested with Qt Test (including off-screen rendering read back pixel by pixel) on Linux, Windows and macOS.*

![Un tore coloré par courbure de Gauss, et le panneau Topologie](../../src/assets/images/qt-fr.png)

## Ce qu'elle fait (sprints 26 et 27)
- **Ouvrir** : menu Fichier, glisser-déposer, ligne de commande ; quatre exemples intégrés (tore, sphère, ruban de Möbius, selle, ceux du viewer de topologie). Une erreur de lecture garde le maillage affiché et donne la ligne fautive.
- **Regarder** : OpenGL 3.3 core, éclairage des deux faces (un ruban de Möbius n'en a qu'une), arêtes en fil de fer ; souris (gauche tourne, droit ou milieu déplace, molette zoome) et clavier (flèches, Maj+flèches, + et −, F recadre).
- **Inspecter** : panneau Topologie (sommets, arêtes, faces, composantes connexes, boucles de bord, arêtes et sommets non-variété, variété, orientabilité, caractéristique d'Euler, genre, somme des défauts angulaires sur 2π, égale à χ par Gauss-Bonnet) ; couleurs de courbure de Gauss (touche C) du bleu (selle) au rouge (dôme), avec leur légende ; arêtes de bord en pistache et non-variété en rouge (touche B).
- **Sélectionner** (sprint 27) : un clic désigne le sommet le plus proche du curseur (à 8 pixels près) ou, sinon, la face visée, trouvés par un lancer de rayon sur le processeur ; l'élément est dessiné en chocolat et ses propriétés s'affichent : position, valence, courbure de Gauss, défaut angulaire et bord pour un sommet ; sommets, aire et normale pour une face. Au clavier : Espace sélectionne au centre de la vue, `]` et `[` passent d'un sommet à l'autre, Échap efface.
- **Enregistrer la vue** en image PNG ou JPEG (Ctrl+S).
- **Deux langues** : textes source en anglais, traduction française (`i18n/qtviewer_fr.ts`, Qt Linguist), changement à chaud par le menu Langue, nombres au format de la langue.

## Organisation
- `src/meshmodel.*` : le maillage prêt à afficher (normales, arêtes, bords, échelle de couleurs), sans OpenGL.
- `src/camera.*` : caméra orbitale (cadrage, rotation, zoom, déplacement), sans OpenGL.
- `src/picking.*` : rayon à travers un pixel, intersection rayon-triangle (Möller-Trumbore), choix du sommet ou de la face, sans OpenGL.
- `src/renderer.*` : le dessin OpenGL (shaders GLSL 3.30), utilisé par la fenêtre, par les tests hors écran et par `--screenshot`.
- `src/viewerwidget.*`, `src/legendwidget.*`, `src/mainwindow.*`, `src/main.cpp` : l'interface.

## Télécharger
La CI produit à chaque changement un exécutable par système, gardé 30 jours dans les artefacts du [workflow qt](https://github.com/Dzop86/Portfolio/actions/workflows/qt.yml) (dernière exécution réussie, section « Artifacts ») :
- **Windows** (`qtviewer-windows-x64`) : un dossier à décompresser, lancer `bin/qtviewer.exe` ; Qt et le runtime Visual C++ sont inclus (windeployqt).
- **macOS** (`qtviewer-macos`) : une image disque ; glisser l'application dans Applications. Signée ad hoc seulement : au premier lancement, clic droit puis « Ouvrir » (ou `xattr -d com.apple.quarantine qtviewer.app`).
- **Linux** (`qtviewer-linux-x86_64`) : une AppImage, à rendre exécutable (`chmod +x`) puis lancer.

Chaque paquet est lancé une fois par la CI après l'emballage (capture d'écran d'un exemple) ; l'AppImage a aussi été lancée sur une Ubuntu 24.04 vierge, sans Qt.

## Compiler et lancer
Qt 6.5 ou plus (la CI utilise Qt 6.10.3, développée avec Qt 6.11.3), CMake 3.21, un compilateur C++20 :
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
./build/qtviewer sample:torus                       # un exemple
./build/qtviewer --lang fr ../lib-c/tests/data/cube.obj
./build/qtviewer --curvature --screenshot shot.png --size 1280x720 sample:saddle   # capture, puis quitte
ctest --test-dir build -C Release --output-on-failure
cmake --build build --target update_translations   # met à jour le .ts après un changement de texte
cmake --install build --prefix dist                  # Windows et macOS : programme et bibliothèques Qt (deploy)
packaging/appimage.sh build dist                     # Linux : dist/Mesh_viewer-x86_64.AppImage (linuxdeploy)
```

## Tests
Qt Test, six programmes (CTest) :
- `test_meshmodel` : cube OBJ et STL, tétraèdre PLY de lib-c ; normales unitaires et sortantes ; topologie des exemples (tore de genre 1, sphère χ = 2, ruban de Möbius non orientable à un bord) ; normales sans annulation à la torsion du ruban ; arête non-variété ; échelle et couleurs de courbure ; erreur de lecture avec sa ligne.
- `test_camera` : le cadrage montre les huit coins de la boîte, l'orbite garde la distance et borne l'inclinaison, le zoom reste dans ses limites, le déplacement reste dans le plan de vue.
- `test_picking` : le rayon central va de l'œil à la cible, projection et rayon concordent, la face la plus proche est touchée (et rien derrière l'œil), un clic sur un sommet le sélectionne et un clic au milieu d'une face la sélectionne, valences et aires cohérentes (somme des valences = 2 × arêtes, aire du cube = 6, valence 6 partout sur le tore).
- `test_render` : dessin dans un framebuffer hors écran, relu pixel par pixel : vue vide, cube au centre et pas dans les coins, arêtes seulement quand elles sont demandées, sphère en rouge et selle en bleu en mode courbure, bord du ruban en pistache et aucun sur la sphère, face et sommet sélectionnés en chocolat (un point d'une centaine de pixels).
- `test_window` : panneau rempli, erreur qui garde le maillage et donne la ligne, changement de langue immédiat (menus, valeurs, titre, format des nombres), glisser-déposer, menu qui suit les options de la vue, clavier, panneau de la sélection dans les deux langues, parcours des sommets au clavier, vue enregistrée en image.
- `test_translations` : chaque texte est traduit, garde ses `%1` et son raccourci `&`, et la traduction compilée est intégrée au programme.
- Vérifié en cassant le code : sans l'alignement des normales, le test du ruban de Möbius échoue ; en acceptant les intersections derrière l'œil, le test du lancer de rayon échoue.

**CI** (`.github/workflows/qt.yml`) : un job d'emballage par système après les tests (dossier Windows, image disque macOS, AppImage Linux), chacun lancé une fois ; Linux (GCC), Windows (MSVC) et macOS (Clang), avertissements traités en erreurs ; OpenGL logiciel de Mesa (llvmpipe) sous Linux (écran virtuel) et Windows (Mesa de mesa-dist-win copié à côté des programmes sous le nom `opengl32sw.dll`) ; sous Linux, le `.ts` doit correspondre au code (`lupdate`), une capture est faite en ligne de commande et un fichier invalide doit faire échouer le programme. Relancée quand lib-c ou topologie changent.

## Limites
- La sélection teste tous les triangles à chaque clic (pas de structure accélératrice) : instantané jusqu'à quelques centaines de milliers de triangles.
- Applications non signées par un certificat : SmartScreen (Windows) et Gatekeeper (macOS) avertissent au premier lancement. Pas d'icône dans l'exécutable Windows ni dans le paquet macOS (icône par défaut), seulement sur Linux et dans la fenêtre.
- Les exécutables vivent 30 jours dans les artefacts de la CI : pas de page de téléchargement permanente (une publication GitHub Releases serait l'étape suivante).
- OpenGL 3.3 est nécessaire (une carte graphique de moins de quinze ans, ou le rendu logiciel) ; sur macOS, OpenGL est déprécié par Apple mais toujours fourni (4.1).
- Tout le maillage est envoyé à la carte en une fois : quelques millions de triangles au plus.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
