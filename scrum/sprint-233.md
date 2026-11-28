# Sprint 233 : Osmose : la zone de 61 à 75, ses cartes

**Objectif :** Une deuxième zone, de 61 à 75, au gabarit des Mines : ses 25 cartes et ses ressources (D103).

**Goal:** A second zone, from 61 to 75, built like the Mines: its 25 maps and its resources (D103).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je trouve après les Mines une zone de 61 à 75 de 25 cartes, son entrée, ses ressources des niveaux 65 et 70 et sa pierre de voyage (rpg) ; xUnit, données validées. | 5 | Fait : 25 cartes, 10 ressources, une pierre de passage, T198 |

## Fait
- Le Val des Sources (`wellspring`, « Wellspring Vale »), Reco A du scénariste retenue par Charles (`projects/rpg/docs/zone-61-75.md`) : 25 cartes de [4, 11] à [8, 15], niveau 61, au sud des Mines, de l'autre côté de la montagne ; on y passe par le milieu du fond des Mines (`underroot-2-4` → `wellspring-2-0`), creusé dans la carte telle qu'elle est.
- Son décor : de l'eau sur chaque carte en chapelets de mares, des murets de pierre sèche, des bouleaux et des noyers, des fleurs sur les terrasses, une bordure d'arbres, bordures naturelles ; teinte vert d'eau (`--osmose-map-wellspring`), relief de 1,8.
- Dix ressources aux noms du scénariste (bouleau, noyer, calcite, platine, barbeau, omble chevalier, maïs, amidonnier, cresson, valériane), 65 près de l'entrée et 70 au fond, leurs tuiles ; aucune de 60. Une pierre de passage (132 Éclats) ; derrière « Quatre pistes ».
- Photos des 25 cartes et de `underroot-2-4` refaites (`--render-maps` dans godot-env), les autres gardées.
- Tests : `WellspringTests` dans `Rpg.Core` (8) et `Rpg.Api` (1, intégration) ; cinq tests de comptes mis à jour (70 ressources, 191 cartes). Vérifié en local : Cœur, Client, API, Sim, `dotnet format`, auto-test du village, `npm test`.
- Réponses prises par défaut aux questions du scénariste (à revoir par Charles) : zone annexe de Dhoram, sans étape du fil principal ; Calcite plutôt que Nickel ; Odon le fontainier pour le relais, à écrire plus tard ; la question des quatre zones par tranche reste ouverte.
- Build 1.0.254 (CI rpg 46142804208, toute verte) livrée, l'API locale reconstruite.
- Limite : ni monstres (S234, S235), ni donjons (S236), ni recettes (S237).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
