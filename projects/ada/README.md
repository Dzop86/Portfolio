# ada : un carrefour sûr par construction

[![ada](https://github.com/Dzop86/Portfolio/actions/workflows/ada.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/ada.yml)

Contrôleur de feux pour un carrefour à deux axes, en Ada 2022. L'état du contrôleur est une **phase du cycle** (vert, orange, rouge intégral, pour chaque axe) et la couleur de chaque feu en est déduite : un état « deux axes au vert » n'a pas de nom, il ne peut donc pas s'écrire.

*Ada 2022 traffic light controller where dangerous states cannot be expressed, proved with SPARK; AUnit tests, CI on Linux, Windows and macOS with Alire.*

## Ce que garantit le code
- **Par le typage** : les phases forment un type énuméré ; aucune phase ne donne le vert (ou l'orange) aux deux axes. Les durées de vert sont un type intervalle (10 à 120 s) : une durée de 5 s ne peut pas être construite (avertissement dès la compilation pour une constante, `Constraint_Error` à l'exécution sinon).
- **Par les contrats** : `Tick` ne peut que rester dans la phase ou passer à la suivante (le cycle vert, orange, rouge intégral n'est jamais sauté) ; `Light` garantit qu'un axe qui n'est pas au rouge a l'autre au rouge ; `Request_Crossing` ne change ni la phase ni le temps écoulé.
- **Par la preuve (SPARK)** : `gnatprove` démontre, pour toutes les exécutions possibles, l'absence d'erreur à l'exécution (aucun débordement du compteur de secondes), le respect des contrats, et le lemme de sûreté `Lemma_One_Axis_Is_Red` : dans toute phase, au moins un axe est au rouge. 14 vérifications, toutes prouvées ; si l'on donne le vert aux deux axes dans une phase, la preuve échoue (vérifié).
- **Demandes de passage** : une voiture ou un piéton qui attend sur un axe écourte le vert de l'autre, jamais sous 10 s ; la demande est servie une fois.

## Lancer
Avec [Alire](https://alire.ada.dev/) :
```sh
alr build
alr run --args="40 4"      # 40 s de simulation, demande est-ouest à t = 4
cd tests && alr run        # tests AUnit
alr exec -- gnatprove -P ../carrefour.gpr --level=2 --checks-as-errors=on   # preuve, depuis tests/
```

## Sur le site (sprint 35)
La [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-ada.html) fait fonctionner le carrefour en direct, sans réécrire le contrôleur en JavaScript : `carrefour --automaton` (`src/automaton.adb`) explore tous les états que le contrôleur peut atteindre depuis `Start` (durées de vert 30 s et 20 s), avec l'état après une seconde (`Tick`) et après une demande sur chaque axe (`Request_Crossing`), et les écrit en JSON (`data/automaton.json`). La page ne fait que suivre ces transitions : feux, voiture qui attend, demandes de passage, pause, seconde par seconde, vitesse, chronogramme des 60 dernières secondes.
```sh
alr build
./bin/carrefour --automaton > data/automaton.json      # l'automate de la fiche
./bin/carrefour 120 4 > data/simulation-120-4.txt      # la simulation que le test Node rejoue
```
Le job `automaton` de la CI régénère les deux fichiers et échoue s'ils diffèrent de ceux commités ; un test AUnit vérifie que l'automate est fermé (toute transition y reste) et sûr (un axe au rouge dans chaque état), un test Node que le rejeu redonne la simulation du programme ligne pour ligne.

## Tests
- **AUnit** (`tests/src/traffic_tests.adb`) : aucune phase ne laisse passer les deux axes, cycle complet, orange toujours suivi du rouge intégral et d'une durée exacte (2 000 s simulées avec des demandes), demande qui écourte le vert sans passer sous le minimum, demande servie une seule fois, durées hors bornes refusées (le test échoue si l'on élargit l'intervalle, vérifié).
- **CI** (`.github/workflows/ada.yml`) : Alire installe la même chaîne GNAT sur Linux, Windows et macOS ; compilation avec avertissements traités en erreurs, tests, simulation ; preuve SPARK sous Linux.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
