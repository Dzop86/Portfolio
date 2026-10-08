# Relecture humaine, rpg (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] Sprint 45 : `src/Rpg.Core/Fight.cs` : déroulé d'un tour, vérification des actions, dégâts, morts, fin du combat.
- [ ] Sprint 45 : `src/Rpg.Core/LineOfSight.cs` : la règle des coins (un segment qui frôle un seul obstacle passe). Est-ce le comportement que tu attends d'un jeu à la Dofus ?
- [ ] Sprint 45 : `data/spells.json` et `data/scenarios/*.json` : le format te convient-il pour ajouter ensuite caractéristiques et monstres ?
- [ ] Sprint 46 : le jeu chez toi (`godot --path godot -- --lang fr`, ou l'exécutable de la CI) : lisibilité du plateau, des surbrillances et de l'interface ; vitesse des animations ; difficulté de l'entraînement.
- [ ] Sprint 46 : `src/Rpg.Client/FightController.cs` (ce qu'un clic peut faire) et `godot/Main.cs` (enchaînement des animations, tours de l'IA, auto-test).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-09 | `src/Rpg.Core/Fight.cs` | (Claude) Trouvé en cassant le code : rien ne vérifiait qu'un lanceur qui évalue un tir depuis une autre case ne se bloque pas la vue avec son propre corps, resté sur sa case de départ | Test ajouté (`FromAnotherCell_TheCastersOwnBodyNoLongerBlocksTheView`), mutation attrapée |
| 2026-10-09 | `tests/Rpg.Core.Tests/FightTests.cs` | (Claude) Deux assertions écrites trop vite : une case calculée par une expression absurde, une assertion toujours vraie | Réécrites avant la première compilation : case explicite, assertion retirée |
| 2026-10-09 | `src/Rpg.Core/FightRecord.cs` | (Claude) L'enregistrement faisait 2,6 Ko pour 31 actions (JSON indenté), alors que la documentation annonçait quelques centaines d'octets ; et la graine était un nombre, que JavaScript arrondit au-delà de 2⁵³ | JSON compact (1,2 Ko), graine en chaîne ; tests de taille et de la graine maximale |
| 2026-10-09 | `.github/workflows/rpg.yml` | (Claude) `grep -x` sur la sortie du simulateur aurait échoué sous Windows (fins de ligne `\r\n`) | `grep -F` sans `-x` |
| 2026-10-09 | mutations | (Claude) Mon script de mutation remettait le fichier d'origine avec une date antérieure à la compilation : MSBuild gardait le binaire muté, un test « échouait » sur le bon code | `touch` après restauration ; résultats des mutations revérifiés |
| 2026-10-09 | `data/scenarios/training.json` | (Claude) Mesuré par simulation : le héros joué par l'IA gagnait 2 000 combats d'entraînement sur 2 000, la leçon du sprint 23 du roguelike (« mesurer l'équilibre par simulation dès le départ ») | Sanglier 40 PV, Crapaud 30 PV : 69 % de victoires sur 5 000 combats ; un test garde l'entraînement entre 60 et 78 % et le duel entre 25 et 45 % |
| 2026-10-09 | comparaison des visuels | (Claude) Première scène 3D d'essai : sol gris uni et éclairage bleuté, ce qui désavantageait la 3D face à la 2D | Sol en herbe (Nature Kit), éclairage neutre, avant de montrer les captures à Charles |
| 2026-10-09 | `godot/Hud.cs` | (Claude) Vu en relisant avant la première exécution : les panneaux placés par `Position` négative après une ancre seraient sortis de l'écran (en Godot 4, `Position` part du coin du parent) | Décalages par rapport à l'ancre (`Offset*`) |
| 2026-10-09 | `godot/BoardView.cs`, `godot/Main.cs` | (Claude) Vu sur les captures : herbe délavée puis fluo, cases impossibles à compter, cases ciblables de la même teinte que le chemin | Deux verts en damier, lumière baissée, cases ciblables en bleu |
| 2026-10-09 | `godot/Main.cs` (`--screenshot`) | (Claude) Vu sur la capture « sort » : aucune visée ; le mode capture choisissait le sort une seconde fois, ce qui l'annule | Ne le choisit que s'il ne l'est pas |
| 2026-10-09 | `src/Rpg.Core/Iso.cs` | (Claude) Devenu du code mort avec le choix de la 3D | Retiré avec ses tests (T7) |
| | | | |
