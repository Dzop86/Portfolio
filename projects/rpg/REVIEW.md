# Relecture humaine, rpg (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] Sprint 45 : `src/Rpg.Core/Fight.cs` : déroulé d'un tour, vérification des actions, dégâts, morts, fin du combat.
- [ ] Sprint 45 : `src/Rpg.Core/LineOfSight.cs` : la règle des coins (un segment qui frôle un seul obstacle passe). Est-ce le comportement que tu attends d'un jeu à la Dofus ?
- [ ] Sprint 45 : `data/spells.json` et `data/scenarios/*.json` : le format te convient-il pour ajouter ensuite caractéristiques et monstres ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-09 | `src/Rpg.Core/Fight.cs` | (Claude) Trouvé en cassant le code : rien ne vérifiait qu'un lanceur qui évalue un tir depuis une autre case ne se bloque pas la vue avec son propre corps, resté sur sa case de départ | Test ajouté (`FromAnotherCell_TheCastersOwnBodyNoLongerBlocksTheView`), mutation attrapée |
| 2026-10-09 | `tests/Rpg.Core.Tests/FightTests.cs` | (Claude) Deux assertions écrites trop vite : une case calculée par une expression absurde, une assertion toujours vraie | Réécrites avant la première compilation : case explicite, assertion retirée |
| 2026-10-09 | `src/Rpg.Core/FightRecord.cs` | (Claude) L'enregistrement faisait 2,6 Ko pour 31 actions (JSON indenté), alors que la documentation annonçait quelques centaines d'octets ; et la graine était un nombre, que JavaScript arrondit au-delà de 2⁵³ | JSON compact (1,2 Ko), graine en chaîne ; tests de taille et de la graine maximale |
| 2026-10-09 | `.github/workflows/rpg.yml` | (Claude) `grep -x` sur la sortie du simulateur aurait échoué sous Windows (fins de ligne `\r\n`) | `grep -F` sans `-x` |
| 2026-10-09 | mutations | (Claude) Mon script de mutation remettait le fichier d'origine avec une date antérieure à la compilation : MSBuild gardait le binaire muté, un test « échouait » sur le bon code | `touch` après restauration ; résultats des mutations revérifiés |
| 2026-10-09 | `data/scenarios/training.json` | (Claude) Mesuré par simulation : le héros joué par l'IA gagnait 2 000 combats d'entraînement sur 2 000, la leçon du sprint 23 du roguelike (« mesurer l'équilibre par simulation dès le départ ») | Sanglier 40 PV, Crapaud 30 PV : 69 % de victoires sur 5 000 combats ; un test garde l'entraînement entre 60 et 78 % et le duel entre 25 et 45 % |
| | | | |
