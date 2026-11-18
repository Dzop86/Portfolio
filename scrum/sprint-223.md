# Sprint 223 : Osmose : l'IA des sorts de groupe

**Objectif :** Au banc de groupes (D102), les 8 sorts de voie muets même en duo sont joués, ou l'on sait pourquoi.

**Goal:** At the party bench (D102), the 8 path spells left unplayed even in a pair are played, or we know why.

| Story | Points | État |
|---|---|---|
| En tant que joueur, l'IA joue le Bouclier de la foi, la Haie, le Sol grondant, le Rappel, la Feinte, la Cabriole, le Cercle de fer et la Fiole de peste quand ils servent : la valeur d'un bouclier ou d'une glyphe pour un allié menacé, des statuts sur soi, des bonds et des retraits de PM ; chacun à 0,5 lancer par combat au banc de groupes, ou la raison écrite (rpg) ; xUnit, banc hors CI. | 5 | Fait : l'IA comptait mal 4 sorts (bouclier d'allié, glyphes partagées, Rappel, Feinte) et ne prenait jamais un bond sans coup, corrigé ; la Haie passe de 0 à 0,3. Aucun n'atteint 0,5 : la raison est mesurée, leurs montants fixes valent par PA deux à six fois moins qu'un coup au niveau 50 ; victoires inchangées ; Q1-Q2 à Charles (T188, `projects/rpg/docs/sorts-de-groupe.md`) |

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
