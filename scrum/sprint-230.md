# Sprint 230 : Osmose : le deck automatique

**Objectif :** Au jalon 4 (D103), le sort d'un nouveau palier n'entre plus dans le deck automatique s'il casse un enchaînement ou une invocation de la classe.

**Goal:** At milestone 4 (D103), a new tier's spell no longer enters the automatic deck if it breaks one of the class's combos or summons.

| Story | Points | État |
|---|---|---|
| En tant que joueur qui n'a pas composé son deck, le deck automatique ne fait entrer un sort nouveau que s'il ne casse pas un enchaînement ou une invocation de ma classe (la Pierre tombale ne chasse plus l'Ossuaire), vérifié au banc à chaque palier de sorts de 45 à 80 (55, 60, 65…) ; puis B4 et B6 remesurées à 60, sans régler les classes avant (Q2) (rpg) ; xUnit, banc hors CI. | 3 | Fait : `Hero.Keeps`, aucun deck changé avant 60 ; marche du niveau 60 effacée de 59 à 80 au banc ; B4 et B6 remesurées à 60 (`docs/deck-automatique.md`, T195) |

## Fait
- Les decks des dix classes et de leurs lignées listés de 30 à 100 : à 60, six sorts de 6 ou 7 PA chassaient un sort qui laissait de la place dans le tour (Ossuaire, Verdict de pierre, Tertre, Fauchage, Grondement, Croc-en-jambe).
- `Hero.Keeps` : un sort nouveau garde la place que l'ancien laissait au sort le moins cher du deck dans un tour de 8 PA. Une règle plus large (zone et effets) écartée : elle changeait les decks dès 40.
- Banc ancien contre nouveau de 59 à 80 (huit classes) : de 59 à 60, Nécromancien −4,7 (−21 avant), Prêtre −2,3 (−16), Druide −1,8 (−9) ; le Guerrier à voie perd sa marche vers le haut (77,2 → 54,8).
- À 60, banc du jalon : 58,7 (56,8) ; B4 non tenue pour le Prêtre seul (17,1) ; 9 lignées hors de ±8 (15).
- Tests : 9 cas de plus dans `ProgressionTests`, la fumée mise à jour ; Cœur, Client 469, Sim et format verts.
- Q1 (Coup de titan) et Q2 (bornes restantes, Reco : au jalon 5) à Charles.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
