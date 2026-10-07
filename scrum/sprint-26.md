# Sprint 26 : la visionneuse Qt/OpenGL, ouvrir et regarder

**Objectif :** premier des deux sprints de la visionneuse (projet qt) : une application de bureau Qt 6 en C++ qui ouvre un maillage avec la bibliothèque C du portfolio, l'affiche en OpenGL et en montre la topologie calculée par le projet topologie ; Windows, Linux et macOS.

**Goal:** first of the two viewer sprints (qt project): a Qt 6 desktop application in C++ that opens a mesh with the portfolio's C library, shows it in OpenGL and displays its topology computed by the topologie project; Windows, Linux and macOS.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur, j'ouvre un maillage OBJ, PLY ou STL (menu, glisser-déposer, ligne de commande) dans une application Qt 6 et je le regarde en 3D : OpenGL 3.3 core, ombrage, arêtes en fil de fer, caméra orbitale à la souris et au clavier, recadrage ; lecture par lib-c, structure par topologie ; interface en français et en anglais (Qt Linguist) ; tests Qt Test ; CI Linux, Windows et macOS. | 3 | Fait |
| En tant qu'ingénieur, j'inspecte le maillage : panneau des invariants (caractéristique d'Euler, genre, composantes, bords, arêtes et sommets non-variété, orientabilité), couleur par courbure de Gauss avec sa légende, arêtes de bord et non-variété mises en évidence ; rendu hors écran testé en relisant l'image ; capture sur la fiche du projet. | 3 | Fait |

**Tests :** cinq programmes Qt Test (modèle, caméra, rendu hors écran relu pixel par pixel, fenêtre, traduction) ; fiche vérifiée par Playwright et axe dans cinq navigateurs. **Trouvé en route :** une bande sombre à la torsion du ruban de Möbius (normales opposées qui s'annulaient), un titre resté dans l'ancienne langue, une légende cachée quand la courbure venait de la ligne de commande.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
