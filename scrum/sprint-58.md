# Sprint 58 : Osmose, expérience, niveaux et points

**Objectif :** L'expérience et les niveaux 1 à 100, et la répartition des points de caractéristique et de sort, validées par le serveur.

**Goal:** Experience and levels 1 to 100, and the spending of characteristic and spell points, checked by the server.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je progresse (rpg) : XP des combats (rien d'un monstre de 20 niveaux de moins ou davantage) et des quêtes, bonus de groupe, courbe jusqu'au niveau 100 ; 10 points de caractéristique par niveau (Vitalité, Force, Intelligence, Chance, Agilité) et 1 point de sort, écrans de caractéristiques et de sorts ; serveur qui valide et rejoue ; xUnit. | 3 | Fait (équilibre de haut niveau à trancher) |

**Résultat :** l'XP des combats gagnés et des quêtes (« Première leçon »), les niveaux 1 à 100, 10 points de caractéristique et 1 point de sort par niveau, l'écran « Personnage » du village et l'XP dite à la fin du combat ; le serveur tire la graine de chaque combat, rejoue son enregistrement une seule fois et donne l'XP ; il refuse les points hors des règles (T29).

**Équilibre :** le simulateur fait jouer des héros dont les points sont dépensés ; le niveau 1 ne change pas, le haut niveau dépend de la valeur d'un point : mesures et choix proposé dans T29 et `projects/rpg/REVIEW.md`.

**Tests :** 19 tests de plus (11 des règles, 4 du serveur, 4 du client) ; auto-tests du village et du parcours connecté étendus ; 5 mutations, 5 attrapées.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
