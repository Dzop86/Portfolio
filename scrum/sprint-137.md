# Sprint 137 : Osmose : l'arbre des talents, tranche verticale

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je débloque l'arbre des talents au niveau 30 et, avec ma Sentinelle, Enlisé (l'Eau puis la Terre) et Au plus près, de bout en bout : l'empreinte, la réaction, le serveur et le rejeu, la liste des talents, la pastille et la prévision (rpg) ; xUnit, intégration API, T41 à 35 ; go ou pas go de Charles. | 5 | Fait, go et playtest de Charles à faire |

## Fait
- Le moteur des talents : `data/talents.json` (règles typées : réaction, portée compilée, bonus selon la distance), `data/reactions.json` (Enlisé), `Talents.Problem` avec toutes les règles de l'arbre (niveau 30, points, étages, clés, une seule clé de Matière, exclusions, prérequis), le plafond de +35 % (T103).
- L'empreinte des coups de héros, la réaction sur la cible visée, l'état nommé, le chef plafonné à −2 PA et −2 PM ; l'IA qui vaut une réaction et une empreinte utile.
- Le serveur : `characters.Talents` (migration `AddTalents`), les talents dans `PUT …/points` (une liste absente garde ceux enregistrés), le rejeu du combat avec talents, l'ordre d'admin qui baisse le niveau les rend.
- Le client : l'onglet « Talents » de l'écran du personnage, la pastille de l'élément au-dessus du combattant marqué, « Enlisé ! » qui flotte, la réaction dans la prévision et le journal ; auto-tests Godot de la ville (l'onglet) et des combats (les pastilles).
- Mesure T41 au niveau 35 : petits groupes 29,3 sans talents, 29,7 avec (au plus +3) ; hordes 0, 0 et 0,7 sur 10.
- Tests : `TalentTests` (Core, 23), `TemplateTests` (T41 à 35), `TalentTextsTests` (Client), `TalentTests` (API).
- **Reste à faire :** Charles joue la Sentinelle de Plouf (niveau 100, 16 points, mais deux talents seulement) et dit go ou pas go. Il a dit « go, enchaîne tous les sprints » avant la tranche : les sprints suivants continuent, et l'arbre s'arrêtera là si le playtest dit non.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
