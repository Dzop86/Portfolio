# Sprint 232 : Osmose : les textes et les quêtes des Mines

**Objectif :** Les textes de la zone de 46 à 60 relus par le scénariste, et les Mines ont leurs quêtes annexes (D103).

**Goal:** The texts of the zone from 46 to 60 reviewed by the screenwriter, and the Mines get their side quests (D103).

| Story | Points | État |
|---|---|---|
| En tant que joueur, les noms et les textes des ressources (S224), des donjons, des clefs, des quêtes de Berthe et des succès (S227), des pièces, des élixirs et des pains (S228) sont relus par le scénariste et fidèles à la bible, et le fil « Garder ou creuser » annoncé par Anatole se joue en quêtes annexes dans les Mines (rpg) ; xUnit, bilingue. | 4 | Fait : 14 textes changés sur la relecture du scénariste, 6 quêtes annexes rivales deux à deux, salutations selon le parti, T197 |

## Fait
- Relecture du scénariste (`projects/rpg/docs/textes-mines-2.md`, partie A) appliquée : l'Ombre devient la Lotte et le Poisson des grottes la Loche des grottes (et la ceinture et les bottes qui en sont faites), la Clef du ventre devient Clef de la montagne, le succès « Le filon s'est tu » devient « Plus personne n'écoute », trois pièces tressées ou matelassées, le Ceinturon en mue de roche ; dans les quêtes de Berthe à 59 et 60, les Fouille-rocs nommés, Bérold n'entend plus de mots dans le filon avant le niveau 58, le Ronge-montagne reste une rumeur. Les générateurs (`trades_gen.py`, `craft_gen.py`, `dungeon_gen.py`) suivent.
- Le fil « Garder ou creuser » (partie B) : à 46, après les Racines, Bérold propose de murer le boyau des mineurs et Berthe de l'élargir ; prendre l'une ferme l'autre pour de bon (`rivals`, nouveau dans les quêtes), le « J'accepte » nomme la quête à laquelle on renonce et l'offre dit « Ce choix est définitif ». Deux suites de chaque côté (48 et 51), la dernière se finit chez l'autre PNJ. Récompenses : élixirs chez Bérold, pains chez Berthe ; expérience et Éclats de la règle des quêtes annexes (40 et 10 × niveau).
- Quatre objets à examiner posés dans les Mines (`zone_people.py`) : le boyau des mineurs, la borne gravée, deux tunnels de vers.
- Les premières répliques de Bérold et de Berthe changent selon le parti pris (`greetings`, nouveau dans les dialogues).
- Un succès de quêtes « Garder ou creuser » (10 points, groupe de la Forêt des Aïeux) : ses six quêtes en cibles, trois suffisent, l'un ou l'autre côté ; nom et texte de Claude, pas du scénariste.
- Les photos des quatre cartes des Mines où sont posés les objets refaites (`--render-maps` dans godot-env, Xvfb) ; les autres photos et l'index gardés.
- Tests : `KeepOrDigTests` dans `Rpg.Core` (4), `Rpg.Client` (2) et `Rpg.Api` (1, intégration), les quêtes rivales dans `QuestTests` ; le parcours de toutes les quêtes (`QuestBookTests`) laisse un côté entier de côté.
- Réponses prises par défaut aux questions du scénariste (à revoir par Charles) : Lotte ; le choix annoncé dans l'offre ; il n'y a pas d'abandon de quête, la question ne se pose pas ; salutations faites maintenant ; `moved-mountains` suivra `roots-of-the-mother-tree` seule.
- Build 1.0.253 (CI rpg 46051562260, toute verte) livrée, l'API locale reconstruite.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
