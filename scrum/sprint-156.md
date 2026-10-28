# Sprint 156 : Osmose : la guilde qui sert, playtest de la boucle

**Objectif :** la suite d'Osmose après S151, conçue par le game designer (v8 de `projects/rpg/docs/suite-apres-d69.md`, §12) et prise avec ses recommandations (D83).

**Goal:** Osmose after S151, designed by the game designer (v8 of `projects/rpg/docs/suite-apres-d69.md`, §12) and taken with its recommendations (D83).

| Story | Points | État |
|---|---|---|
| En tant que membre, ma guilde monte de niveau (1 à 10) par les contrats, les succès et les salles de ses membres, sans bonus de combat, avec un objectif de la semaine et le message du meneur (rpg) ; xUnit, intégration à trois clients. | 2 | Fait |
| En tant que membre, je dépose au coffre de guilde chez Edmée (onglets selon le niveau, retraits selon le rang, journal des mouvements) et je vois le blason composé de la guilde (rpg) ; xUnit, intégration PostgreSQL. | 2 | Fait |
| En tant que Charles, je joue la boucle une semaine avec 2 à 5 joueurs ; le serveur tient des compteurs anonymes lisibles par l'admin (rpg). | 1 | Compteurs faits (`Rpg.Api playtest`) ; playtest d'une semaine à faire par Charles |

## Fait
- Le niveau de guilde, de 1 à 10 : 10 points par contrat rendu (20 pour un contrat de groupe), 5 par succès obtenu une fois membre, 30 par donjon fini par deux membres ou plus ; 150 × n points du niveau n au suivant ; aucun bonus de combat (T123).
- Les points de la semaine (du lundi à l'aube au suivant) contre l'objectif du meneur, et son message, dans la fenêtre Guilde ; le meneur les écrit, avec le blason, depuis le crayon.
- Le blason : 6 formes, 30 pictogrammes de game-icons.net, 8 couleurs de la charte ; vu dans la fenêtre et à côté du nom de guilde sur la carte, par identifiants (remplaçables à la direction artistique).
- Le coffre chez Edmée (« J'ai affaire au coffre de ma guilde. ») : onglets de 40 emplacements (1, puis 2 au niveau 3, 3 au 6, 4 au 9), chacun dépose, le meneur et les officiers retirent, le meneur ouvre un onglet aux membres ; journal des 100 derniers mouvements ; la ligne de la guilde verrouillée à chaque mouvement.
- Les compteurs du playtest, sans nom : jours joués, temps en jeu, contrats rendus sur contrats offerts, lus par l'opérateur seul (`Rpg.Api playtest`).
- xUnit (niveaux, semaine, coffre, journal, blason, page), intégration PostgreSQL à trois clients (message, blason vu sur la carte, dépôt, retrait refusé à un membre, journal, niveau 3 et son deuxième onglet), un donjon fini à deux, les compteurs ; auto-test du village (en-tête, éditeur réservé au meneur, coffre).
- Textes d'Edmée (article quatre) et libellés écrits par moi, pas par le scénariste : à relire.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
