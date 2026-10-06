# Décisions, naval

## N1. Un modèle sans JavaFX, une vue sans fenêtre
**Choix :** le paquet `model` n'importe rien de JavaFX ; `NavalView` construit la scène à partir d'un `Game` sans créer de `Stage`.
**Pourquoi :** les règles se testent en JUnit pur sur tous les systèmes ; la vue se teste et se photographie sans écran.

## N2. JavaFX sans écran par Monocle
**Choix :** les tests de l'interface et les captures tournent avec la plateforme Monocle « Headless » et le rendu logiciel (`openjfx-monocle`, dépendance de test) ; les captures sont écrites par un petit encodeur PNG (`Png.java`).
**Pourquoi :** pas d'écran en CI ni dans Docker ; pas d'AWT ni de `javafx-swing` pour une seule image.
**Limite :** le rendu du texte demande les bibliothèques de polices du système (Pango, FreeType) : présentes sur les postes et les runners GitHub, à installer dans une image Docker minimale.

## N3. Chasse en damier, puis cible
**Choix :** l'ordinateur tire au hasard sur les cases d'une même couleur du damier, puis autour d'une touche, puis le long de la ligne ; il raye les voisins d'un navire coulé.
**Pourquoi :** la stratégie classique, simple à expliquer, nettement meilleure que le hasard ; la règle de non-contact du jeu d'origine la renforce.
