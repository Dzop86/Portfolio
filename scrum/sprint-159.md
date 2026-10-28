# Sprint 159 : Osmose : récompenses, suites de quêtes et donjons

**Objectif :** une refonte graphique de l'interface d'Osmose demandée par Charles après S156, glissée avant le socle 45-60 (D84).

**Goal:** a graphic overhaul of Osmose's interface asked for by Charles after S156, slipped in before the 45-60 groundwork (D84).

| Story | Points | État |
|---|---|---|
| En tant que joueur, chaque succès rapporte des Éclats et de l'XP que je réclame d'un bouton dans la fenêtre, une seule fois, y compris pour les succès déjà obtenus (rpg) ; xUnit, intégration PostgreSQL. | 2 | Fait |
| En tant que joueur, les suites de quêtes d'une zone sont des succès en plusieurs étapes, et les finir toutes donne un méta-succès « Obtenir les succès suivants » avec un lien vers chacun (rpg) ; xUnit, textes du scénariste. | 2 | Fait |
| En tant que joueur, finir un donjon est un succès, rangé par tranche de niveaux (rpg) ; xUnit. | 1 | Fait (les dix succès de donjon comptent le donjon fini) |

## Fait
- Formule du game designer (`projects/rpg/docs/succes-recompenses-s159.md`) : un niveau de référence par succès, p × L / 2 XP et p × L / 4 Éclats ; réclamés une fois, rétroactifs, d'un bouton ou tous d'un coup ; pastille « n à réclamer » sur l'icône (T126).
- 19 succès de suites de quêtes (82 en tout, 1 110 points) : une suite principale, une secondaire et un méta-succès par zone, Clairval et ses premiers pas ; noms et phrases du scénariste (`projects/rpg/docs/succes-textes-s159.md`).
- Les dix succès de donjon comptent le donjon fini (chef au bestiaire ou quête du donjon rendue).
- Fenêtre : récompenses et « Réclamer », « Tout réclamer », quêtes cochées, succès requis avec « Voir › ».
- xUnit (formule et exemples, règle des niveaux, chaque quête dans une suite, donjon fini, méta-succès, lignes de la page), intégration PostgreSQL (réclamer, 409, 400, tout réclamer, deux réclamations à la fois), auto-test du village : verts.

- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
