# Sprint 78 : Osmose, le fil du combat et le clic droit

**Objectif :** demande de Charles (D64) : une frise qui montre le tour en cours, un combat lancé par clic droit, plus de jeu hors ligne pour le joueur.

**Goal:** Charles's request (D64): a timeline showing the current round, a fight started with a right click, no offline play for the player.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue comme dans les jeux du genre (rpg) : la frise du combat ne montre que l'ordre du tour en cours et recommence à chaque nouveau tour (plus de défilement sans fin) ; un clic droit sur un groupe de monstres ouvre un menu « Attaquer », le clic gauche ne lance plus de combat ; l'écran de connexion ne propose plus de jouer hors ligne (l'option --offline reste pour les tests) ; xUnit, auto-test. | 3 | Fait |

**Résultat :** la frise limitée au tour en cours (`Fight.RoundOrder`), le combat lancé par « Attaquer » dans le menu du clic droit, l'écran de connexion qui demande le compte au lieu de proposer le jeu hors ligne (T45).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
