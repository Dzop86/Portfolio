# Décisions, qt

## Q1. Qt Widgets et QOpenGLWidget, pas QML
**Choix :** une fenêtre Qt Widgets (menus, panneau, barre d'état) autour d'un `QOpenGLWidget` qui dessine en OpenGL 3.3 core.
**Pourquoi :** une application d'inspection classique sur le bureau ; les widgets se testent directement avec Qt Test ; OpenGL montre le pipeline (buffers, shaders, profondeur) plutôt que de le cacher derrière Qt Quick 3D.
**Limite :** OpenGL est déprécié sur macOS (encore fourni en 4.1) ; un passage à Qt RHI (Vulkan, Metal, Direct3D) serait la suite logique.

## Q2. La logique hors d'OpenGL, le dessin partagé
**Choix :** `MeshModel` et `Camera` ne dépendent pas d'OpenGL ; `Renderer` dessine dans le framebuffer courant, qu'il soit celui de la fenêtre, d'un test hors écran ou de `--screenshot`.
**Pourquoi :** l'essentiel se teste sans contexte graphique ; le rendu se teste en relisant une image dessinée par le même code que la fenêtre.
**Limite :** le test de rendu demande un contexte OpenGL 3.3 : rendu logiciel en CI (Mesa llvmpipe sous Linux et Windows).

## Q3. Les normales alignées par sommet
**Choix :** chaque normale de face est retournée vers la somme déjà accumulée au sommet avant d'y être ajoutée ; l'éclairage vient de l'œil et éclaire les deux faces.
**Pourquoi :** sur un ruban de Möbius (non orientable) ou un maillage mal orienté, des normales opposées s'annulaient et laissaient une bande sombre (vu sur une capture, test ajouté).
**Limite :** le résultat dépend un peu de l'ordre des faces ; pour des arêtes vives (cube), les normales lissées arrondissent l'éclairage.

## Q4. Textes en anglais, traduction française par Qt Linguist
**Choix :** `tr()` sur des textes anglais, `i18n/qtviewer_fr.ts` mis à jour par `lupdate` (sans numéros de ligne, pour que le fichier ne change qu'avec les textes) et compilé dans le programme ; la langue se change à chaud.
**Pourquoi :** l'outil standard de Qt, que la CI vérifie (le `.ts` doit correspondre au code, chaque texte doit être traduit avec ses `%1` et son `&`).
**Limite :** la boîte de dialogue d'ouverture de fichier et les boutons standard suivent la langue du système, pas celle choisie dans le menu.

## Q5. Une échelle de couleurs robuste
**Choix :** la couleur de courbure va de −s à +s, où s est le 95ᵉ centile de |K| sur les sommets intérieurs ; palette divergente RdBu de ColorBrewer.
**Pourquoi :** quelques sommets très courbés (pointes, coins) écraseraient sinon toute la surface vers le blanc ; RdBu reste lisible pour la plupart des daltoniens.
