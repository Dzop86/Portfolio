# bataille : le jeu de cartes, en Ada

[![bataille](https://github.com/Dzop86/Portfolio/actions/workflows/bataille.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/bataille.yml)

Réécriture d'un jeu de mes études : la bataille, en Ada 2022. Les cartes impossibles ne s'écrivent pas, les paquets vérifient leurs longueurs par contrats, et 100 000 parties simulées disent quelque chose d'inattendu sur le jeu.

*War, the card game, in Ada 2022: typed cards, contract-checked circular piles, reproducible shuffles, chained wars; 100,000 simulated games show that with a fixed pick-up order 42% of games never end. AUnit tests on Linux, Windows and macOS.*

## Le jeu
- **Types** : la valeur d'une carte est un intervalle de 2 à 14, la couleur une énumération ; un « 1 de cœur » ou une cinquième couleur ne compilent pas.
- **Paquets** : une file circulaire d'au plus 52 cartes ; `Take_Top` exige une pile non vide et garantit qu'elle perd une carte (et rend celle du dessus), `Put_Bottom` exige de la place et garantit qu'elle en gagne une. Contrats vérifiés à l'exécution (`contracts = "Yes"`).
- **Mélange** : xorshift64 et Fisher-Yates, écrits ici, pour que la graine 1 donne la même partie sur tous les systèmes.
- **Règles** : chaque pli, la plus forte carte prend tout ; à égalité, chacun pose une carte face cachée et une face visible, et les batailles s'enchaînent ; un joueur qui ne peut plus suivre perd (égalité si aucun ne peut). Une assertion vérifie à chaque pli qu'aucune carte n'apparaît ni ne disparaît. Une partie qui atteint 10 000 plis est déclarée sans fin.

## Ce que disent 100 000 parties (`data/stats.json`)
- **42,35 % des parties ne finissent jamais.** Le gagnant d'un pli ramasse toujours les cartes dans le même ordre : le jeu devient entièrement déterministe et retombe sur des positions déjà vues. Entre de vrais joueurs, l'ordre varie et la partie finit presque toujours.
- Une partie qui finit dure en moyenne 1 689 plis (de 20 à 9 994) et compte 39 batailles.
- Le premier joueur gagne 52,9 % des parties finies : la donne et l'ordre de ramassage (ses cartes d'abord) ne sont pas symétriques.
- La fiche du projet affiche ces chiffres ; la CI les recalcule et échoue s'ils changent.

## Lancer
Avec [Alire](https://alire.ada.dev/) :
```sh
alr build
./bin/bataille                  # une partie mélangée au hasard, pli par pli
./bin/bataille --seed 1         # la partie de la graine 1 (2 870 plis)
./bin/bataille --stats 100000   # les statistiques, en JSON (2 minutes)
cd tests && alr run             # tests AUnit
```

## Tests
- **AUnit** (`tests/src/war_tests.adb`) : paquet de 52 cartes toutes différentes, file dans le bon ordre, précondition qui refuse de tirer dans une pile vide, symboles en UTF-8 ; mélange reproductible et différent selon la graine, donne alternée ; un pli, une bataille, deux batailles enchaînées ; bataille impossible faute de cartes, égalité, partie coupée à la limite, graine 1 identique partout ; 200 parties terminées, déterministes, un rapport par pli ; statistiques et grands totaux. Vérifié en cassant le code : sans les cartes face cachée, trois groupes de tests échouent.
- **CI** (`.github/workflows/bataille.yml`) : Alire sur Linux, Windows et macOS, avertissements traités en erreurs, tests et une partie complète ; les 100 000 parties recalculées et comparées au fichier commité.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
