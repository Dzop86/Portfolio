# Relecture humaine, rpg (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] Sprint 45 : `src/Rpg.Core/Fight.cs` : déroulé d'un tour, vérification des actions, dégâts, morts, fin du combat.
- [ ] Sprint 45 : `src/Rpg.Core/LineOfSight.cs` : la règle des coins (un segment qui frôle un seul obstacle passe). Est-ce le comportement que tu attends d'un jeu à la Dofus ?
- [ ] Sprint 45 : `data/spells.json` et `data/scenarios/*.json` : le format te convient-il pour ajouter ensuite caractéristiques et monstres ?
- [ ] Sprint 46 : le jeu chez toi (`godot --path godot -- --lang fr`, ou l'exécutable de la CI) : lisibilité du plateau, des surbrillances et de l'interface ; vitesse des animations ; difficulté de l'entraînement.
- [ ] Sprint 46 : `src/Rpg.Client/FightController.cs` (ce qu'un clic peut faire) et `godot/Main.cs` (enchaînement des animations, tours de l'IA, auto-test).
- [ ] Sprint 47 : le serveur (`docker compose up --build rpg-api`, puis http://localhost:8002/swagger) et l'écran de connexion du jeu (`godot --path godot -- --lang fr`) : créer un compte, des personnages, en supprimer, se tromper de mot de passe, couper le serveur.
- [ ] Sprint 47 : `src/Rpg.Api/Endpoints/` (ce que le serveur vérifie et ce qu'il répond) et `src/Rpg.Client/Lobby.cs` (le message que chaque refus donne au joueur).
- [ ] Sprint 48 : créer un personnage de chaque classe et jouer l'entraînement avec : les classes te semblent-elles différentes et équilibrées (`data/classes.json`) ? Les couleurs de tenue sur les différentes apparences.

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
| 2026-10-09 | `.github/workflows/rpg.yml` | (Claude) CI rouge sous Windows seulement : l'import Godot durait 0,5 s, PowerShell rendant la main sans attendre Godot ; les modèles .glb n'étaient pas importés et l'auto-test restait bloqué jusqu'au délai de 5 min | Import lancé par bash, qui attend Godot |
| 2026-10-09 | `tests/Rpg.Api.Tests` | (Claude) 32 appels écrits sans le jeton d'annulation du test (xUnit1051, erreur puisque les avertissements bloquent) : un test arrêté aurait laissé ses requêtes HTTP et SQL tourner | `TestContext.Current.CancellationToken` passé à chaque appel, comme dans les tests du roguelike |
| 2026-10-09 | `src/Rpg.Api` | (Claude) Vérifié en cassant le code : limite de 5 personnages, suppression du personnage d'un autre, liste des personnages d'un autre, nom insensible à la casse | 4 mutations sur 4 attrapées par les tests d'intégration |
| 2026-10-09 | `scripts/smoke.sh` | (Claude) Premier essai arrêté net (code 141) : `tr < /dev/urandom \| head` reçoit SIGPIPE, fatal avec `pipefail` | Nom tiré avec `$RANDOM`, essai vert contre l'image Docker |
| 2026-10-09 | `godot/Main.cs` (`--shot lobby`) | (Claude) Vu sur la première capture de l'écran de connexion : modèle 3D trop gros qui débordait de son socle, tuile d'herbe décalée, apparences affichées par leur identifiant (« female-e ») | Modèle à l'échelle du plateau, disque d'herbe centré, `Texts.Look` (« Femme E », « Woman E ») et son test |
| 2026-10-09 | `godot/LobbyView.cs`, `godot/Main.cs` | (Claude) Première compilation : `_ => _ = SignIn(...)` assignait la tâche au paramètre `_` au lieu de l'ignorer, et `HttpClient` était ambigu avec celui de Godot | Paramètre nommé, `System.Net.Http.HttpClient` écrit en entier |
| 2026-10-09 | `data/classes.json` | (Claude) Mesuré par simulation avant d'écrire les tests : la Garde telle que je l'avais imaginée perdait 2 000 combats d'entraînement sur 2 000, la Mage en gagnait 8 | Garde 7 PA et Hache 9-12, Mage 55 PV, 8 PA, Étincelle 4-6 deux fois par tour : 73 et 74 % sur 5 000 combats ; test d'équilibre par classe |
| 2026-10-09 | `godot/LobbyView.cs` | (Claude) Vu sur la capture : les pastilles montraient les colonnes de la palette, pas le résultat (pastille orange, tenue violette) | `Looks.MainColumn` lit la couleur dominante du modèle ; chaque pastille montre ce qu'elle devient |
| 2026-10-09 | `tests/Rpg.Api.Tests/CharacterTests.cs` | (Claude) Mon premier test de migration relisait la valeur par défaut qu'il venait lui-même d'écrire : il ne prouvait rien | Remplacé par une ligne insérée sans classe, comme au sprint 47, relue par l'API |
| 2026-10-09 | mutations | (Claude) Deux de mes trois premières mutations ne compilaient pas (code inaccessible, nullabilité) : elles ne testaient rien | Réécrites pour compiler : 3 sur 3 attrapées |
| 2026-10-09 | `scripts/smoke.sh` | (Claude) CI du sprint 48 rouge (job docker) : l'essai de bout en bout créait encore un personnage sans classe, refusé depuis que la classe est obligatoire ; je n'avais relancé que les tests .NET, pas ce script | Classe et couleur dans l'essai, plus le refus d'une classe inconnue ; vérifié contre l'image Docker avant de pousser |
| | | | |
