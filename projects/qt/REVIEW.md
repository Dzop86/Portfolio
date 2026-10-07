# Relecture humaine, qt (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/renderer.cpp` : buffers, shaders, ordre de dessin (faces décalées, arêtes par-dessus).
- [x] `src/meshmodel.cpp` : normales, arêtes de bord, échelle de courbure.
- [x] La visionneuse chez moi : ouvrir mes propres maillages, la lisibilité des invariants et des couleurs.
- [x] `src/picking.cpp` : rayon, intersection, choix du sommet ou de la face (sprint 27).
- [x] Les exécutables de la CI chez moi : lancer celui de mon système (sprint 27).

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« valide la review, pousse ça »), pour le volet du sprint 26 ; puis, pour le sprint 27 (sélection et exécutables), à sa demande explicite (« valide la review »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/meshmodel.cpp` | (Claude) Vu sur une capture : bande sombre à la torsion du ruban de Möbius, les normales opposées des faces voisines s'annulaient | Normales retournées vers la somme du sommet avant addition ; test sur toutes les normales du ruban, vérifié en retirant la correction |
| 2026-10-07 | `src/mainwindow.cpp` | (Claude) Vu sur une capture : « -0,000000 » pour la somme des défauts angulaires d'un tore | Valeur arrondie avant affichage |
| 2026-10-07 | `src/mainwindow.cpp` | (Claude) Vu sur une capture : la légende de courbure restait cachée quand la courbure était activée par la ligne de commande, et la case du menu restait décochée | La fenêtre suit le signal `optionsChanged` de la vue |
| 2026-10-07 | `src/mainwindow.cpp` | (Claude) Détecté par un test : après un changement de langue, le titre gardait le nom de l'exemple dans l'ancienne langue (nom traduit mémorisé à l'ouverture) | Clé de l'exemple mémorisée, nom traduit à l'affichage |
| 2026-10-07 | `src/mainwindow.cpp` | (Claude) Détecté par un test : la fenêtre ne se retraduisait qu'au tour suivant de la boucle d'événements | Retraduction immédiate dans `setLanguage` |
| 2026-10-07 | `CMakeLists.txt` | (Claude) La traduction compilée était attachée à l'exécutable : ni les tests ni la bibliothèque n'y avaient accès | Traductions attachées à la bibliothèque `viewer` |
| 2026-10-07 | `src/mainwindow.cpp` | (Claude) Signalé par la CI macOS : Clang refuse une capture `this` inutile dans un lambda (`-Wunused-lambda-capture`), avertissement que GCC ne donne pas | Capture retirée ; compilation locale avec Clang ajoutée à ma routine avant de pousser |
| 2026-10-07 | `.github/workflows/qt.yml` | (Claude) Signalé par la CI Windows : aqtinstall ne lit pas le dépôt Windows de Qt 6.11 (somme de contrôle introuvable, reproduit en local) | CI en Qt 6.10.3 sur les trois systèmes |
| 2026-10-07 | `.github/workflows/qt.yml` | (Claude) Signalé par la CI Windows : MSVC compile sans avertissement, mais le test de rendu échoue. Le rapport (journaux `qt.qpa.gl`) montre que Qt choisit de lui-même son vieux `opengl32sw` (Mesa avec LLVM 3.6, OpenGL 3.0) face au pilote de base de la machine | Mesa 26 (llvmpipe) de mesa-dist-win copié à côté des programmes sous le nom `opengl32sw.dll`, avec `QT_OPENGL=software` (avec `desktop`, Qt ne charge `opengl32.dll` que depuis System32 : OpenGL 1.1) ; rapport du test affiché en cas d'échec |
| 2026-10-07 | `tests/test_window.cpp` | (Claude) Mon test d'enregistrement d'image vérifiait le pixel central d'un tore, qui tombe dans le trou | Test fait sur la sphère |
| 2026-10-07 | `packaging/appimage.sh` | (Claude) Détecté en emballant : linuxdeploy ne trouvait pas les bibliothèques d'un Qt installé hors du système (`cmake --install` retire le RPATH de compilation) | Dossier `lib` de Qt (`qmake -query`) ajouté à `LD_LIBRARY_PATH` ; AppImage lancée sur une Ubuntu vierge |
| 2026-10-07 | `.github/workflows/qt.yml` | (Claude) Signalé par la CI Windows : le script de déploiement de Qt refuse un préfixe d'installation relatif | Préfixe absolu ; les lancements d'essai se font aussi sans `QT_PLUGIN_PATH` (posé par install-qt-action), qui aurait masqué un greffon absent du paquet |
| | | | |
