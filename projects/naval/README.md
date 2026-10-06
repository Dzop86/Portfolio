# naval : une bataille navale en Java et JavaFX

[![naval](https://github.com/Dzop86/Portfolio/actions/workflows/naval.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/naval.yml)

Réécriture d'un jeu programmé pendant mes études : une bataille navale contre l'ordinateur. Le modèle est en Java pur et testé, l'interface en JavaFX ; la fiche du projet en montre une capture, générée sans écran par les tests.

*Battleship against the computer: Java 21 model (placement rules, shots, hunt-and-target AI) tested with JUnit 5, JavaFX interface tested headless with Monocle; Maven build on Linux, Windows and macOS.*

## Le jeu (`src/main/java/portfolio/naval/model`)
- Grille de 10 × 10 (A1 à J10), flotte classique : porte-avions (5), cuirassé (4), croiseur (3), sous-marin (3), torpilleur (2), soit 17 cases.
- Placement aléatoire reproductible (graine), sans chevauchement ni contact, même par un coin ; une raison claire si un navire ne peut pas aller quelque part.
- Tirs : à l'eau, touché, coulé (avec le nom du navire), déjà joué (ne compte pas) ; fin de partie quand une flotte est entièrement coulée.
- **L'ordinateur** chasse en damier (le plus petit navire couvre deux cases, il ne peut pas se cacher entre), cible autour d'une touche puis suit la ligne dès que deux touches sont alignées ; quand un navire coule, il raye les cases voisines, que la règle de non-contact rend vides. Sur 300 parties, il lui faut en moyenne moins de 60 tirs pour couler une flotte, contre environ 96 au hasard.

## L'interface (`src/main/java/portfolio/naval/ui`)
- Votre flotte et les tirs reçus à gauche, la grille ennemie où l'on clique à droite, messages de partie, score, nouvelle partie ; en français ou en anglais selon le système (`messages*.properties`).
- `NavalView` construit la scène à partir d'une partie, sans fenêtre : les tests et l'outil de captures la pilotent sans écran.

## Lancer
```sh
mvn javafx:run                                   # Java 21 et Maven ; JavaFX est téléchargé par Maven
mvn test                                         # modèle et interface (sans écran)
mvn test -Dnaval.screenshots=target/shots        # les captures de la fiche (polices du système nécessaires)
```

## Tests
- **Modèle** (`ModelTest`, JUnit 5) : notation des cases, règles de placement (bord, chevauchement, contact, déjà placé), 2 000 flottes aléatoires valides et reproductibles, tirs et navire coulé, l'ordinateur ne tire jamais deux fois au même endroit et finit toujours, cible autour d'une touche, bat largement le hasard, une partie complète a un seul vainqueur.
- **Interface** (`ViewTest`, JavaFX sans écran par Monocle) : un clic tire et l'ordinateur répond, un second tir au même endroit ne compte pas, la flotte du joueur est visible, la grille ennemie se désactive à la fin.
- **CI** (`.github/workflows/naval.yml`) : Maven sur Linux, Windows et macOS (Java 21), compilation avec `-Xlint:all -Werror`.

## Limites
- Pas de démo dans le navigateur (JavaFX est une application de bureau) : une capture et la commande pour lancer le jeu.
- Pas de placement manuel des navires : la flotte du joueur est tirée au hasard, comme celle de l'ordinateur.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
