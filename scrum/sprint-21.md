# Sprint 21 : la bataille, en Ada

**Objectif :** le projet bataille réécrit un jeu de mes études : le jeu de cartes de la bataille, en Ada, avec des types qui excluent les cartes impossibles et des paquets à contrats ; la fiche du projet montre ce qu'en disent 100 000 parties simulées.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je regarde une partie de bataille (bataille) se jouer dans le terminal : 52 cartes mélangées de façon reproductible, plis, batailles en chaîne, fin de partie (y compris les parties qui ne finissent jamais) ; paquets en file circulaire à contrats ; tests AUnit, CI Linux, Windows et macOS. | 2 | Fait |
| En tant que curieux, je lis sur la fiche (bataille) ce que donnent 100 000 parties : durée, nombre de batailles, parties sans fin, avantage du premier joueur ; statistiques calculées par le programme Ada, commitées et vérifiées par la CI. | 1 | Fait |

**Résultat :** sur 100 000 parties, 42,35 % ne finissent jamais (ordre de ramassage fixe) ; 1 689 plis en moyenne quand elles finissent ; le premier joueur en gagne 52,9 %.

**Tests :** 6 groupes AUnit (dont 200 parties et la conservation des 52 cartes à chaque pli) ; fiche vérifiée par axe. **Trouvé par Ada à l'exécution :** une conversion vers `Positive` et un débordement sur 32 bits.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
