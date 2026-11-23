# Sprint 225 : Osmose : la zone de 46 à 60, sa première famille

**Objectif :** La première famille de monstres de la zone, réglée pour les dix classes (D102).

**Goal:** The zone's first monster family, balanced for the ten classes (D102).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'affronte la première famille de la zone, son chef, son butin et sa panoplie ; ses groupes gagnables seul et nu par chacune des dix classes (rpg) ; xUnit, T41 et `--party`. | 4 | Fait |

## Fait
- Les Fouille-rocs (`delver`) des Mines de Sous-Racine, conçus par le game designer (`docs/familles-mines.md` §3), textes du scénariste : Boutefeu, Jeteur d'escarbilles, Étançonneur, Porte-lanterne et leur chef l'Écoute-roche ; 11 sorts, 5 passifs, le tonnelet et le baril de poudre (2 PV, ils sautent au début de leur second tour, à 2 cases, sur tout le monde), la panoplie du fouille-roc (4 pièces), deux succès, leurs dessins, icônes et portraits.
- 10 tests `DelverTests` (tonnelet posé, usé puis sauté ; sauté tout de suite à la mort du Boutefeu, sauf en fin de combat ; frappé ; Acharné ; Calage ; Lampée ; chef ; panoplie ; rejeu) ; une étape de la CI joue le combat du chef dans le client.
- Mesure à dix classes au niveau 46 : petits groupes 30 sur 30 pour chaque classe, chef seul 0 sur 10 ; hordes réglées par les PV (+50 %) ; chef en groupe 95 % à deux, 100 % à trois.
- Client : l'ouverture d'un combat (les passifs du premier tour) est montrée, elle était sautée.
- Limites et questions (T190) : 4 classes au-dessus du plafond des hordes (le Berserker nu, 4 classes avec les pièces d'artisan de 45) ; pas d'échantillon en CI, le budget de combats est plein ; aucune quête annexe dans les Mines d'ici S229 ; donjon en S227.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
