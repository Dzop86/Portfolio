# aventure : « Le laboratoire de nuit », une aventure textuelle en Java

[![aventure](https://github.com/Dzop86/Portfolio/actions/workflows/aventure.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/aventure.yml)

Réécriture d'un jeu de mes études. L'original, écrit à deux, n'est pas repris : l'histoire, le moteur et les tests sont neufs. Un doctorant s'est endormi sur sa thèse et le laboratoire est fermé ; il doit retrouver la clé USB de son manuscrit et sortir. Jouable dans le terminal et sur la fiche du projet, en français ou en anglais.

*Text adventure written from scratch in Java 21: six places, a badge-locked door, items, a guard with hints, a fight; commands in French and English. JUnit 5 tests play whole games; the engine is compiled to JavaScript by TeaVM for the browser.*

## Le jeu
- Six lieux (hall, couloir, bureau des doctorants, bibliothèque, cafétéria, salle des serveurs), une porte fermée par badge, cinq objets (badge, parapluie, café, sandwich, clé USB), un gardien qui donne des indices, un robot de ménage déréglé qui garde la clé.
- Commandes en français et en anglais (`nord`/`north`, `prendre le badge`/`take the badge`, `sac`/`bag`, `parler`/`talk`, `attaquer`/`attack`...), sans accents ni articles obligatoires (`prendre la Clé` = `prendre cle`), apostrophe typographique comprise.
- Le combat se décide par le parapluie, quel que soit le hasard : à mains nues le robot gagne toujours, avec le parapluie le joueur gagne toujours (le calcul est en commentaire dans `Game.java`, et vérifié sur 200 parties).

## Architecture
- `Game.respond(ligne) -> texte` : tout le jeu derrière une méthode, sans entrée ni sortie. Le terminal (`Main`), le navigateur (`WebGame`) et les tests utilisent le même moteur.
- `Texts` : chaque phrase dans les deux langues, paramètres `{0}` remplacés à la main ; le moteur évite `String.format`, `ResourceBundle` et les expressions régulières pour se compiler en JavaScript.
- **Navigateur** : `mvn -Pweb package` compile le moteur en module JavaScript (TeaVM 0.16, 150 Ko), qui exporte `start`, `respond` et `over` ; la fiche du projet l'affiche dans un petit terminal. La CI vérifie que le fichier servi par le site est celui que produisent les sources.

## Lancer
```sh
mvn package && java -jar target/aventure-0.1.0.jar --lang fr     # ou --lang en, --seed N
mvn test
mvn -Pweb package -DskipTests                                     # target/web/aventure.js
```

## Tests
- **JUnit 5** (`GameTest`) : chaque texte existe dans les deux langues ; analyse des commandes (casse, accents, articles, apostrophes) ; déplacements et sortie refusée sans la clé ; porte à badge ; objets pris, posés, utilisés (énergie plafonnée) ; indices du gardien ; clé gardée tant que le robot tourne ; sur 200 graines, victoire avec le parapluie et défaite sans ; une partie gagnée en français et une en anglais, puis recommencée ; commandes inconnues et vides. Vérifié en cassant le code : sans le bonus du parapluie, trois tests échouent.
- **JavaScript** (`tests/unit/aventure-web.test.mjs` du site) : le module compilé par TeaVM joue des parties complètes dans les deux langues.
- **Page** (Playwright, 5 navigateurs) : commandes tapées et raccourcis, historique aux flèches, partie gagnée, nouvelle partie, accessibilité (axe).
- **CI** (`.github/workflows/aventure.yml`) : Maven sur Linux, Windows et macOS (Java 21, `-Xlint:all -Werror`), et la vérification du JavaScript compilé.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
