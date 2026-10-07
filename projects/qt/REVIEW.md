# Relecture humaine, qt (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/renderer.cpp` : buffers, shaders, ordre de dessin (faces décalées, arêtes par-dessus).
- [ ] `src/meshmodel.cpp` : normales, arêtes de bord, échelle de courbure.
- [ ] La visionneuse chez moi : ouvrir mes propres maillages, la lisibilité des invariants et des couleurs.

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
| | | | |
