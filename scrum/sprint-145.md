# Sprint 145 : Osmose : les guildes

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée ou rejoins une guilde de 50 membres au plus, avec ses rangs, son canal /gu et son onglet (rpg) ; xUnit, intégration à deux clients. | 5 | Fait |

## Fait
- Les règles des guildes (`GuildRules`, T112) : un nom de 3 à 20 caractères unique sur le serveur, 1 000 Éclats, 50 membres, trois rangs et leurs droits, le titre transmis à l'officier le plus ancien quand le meneur part, la guilde dissoute avec son dernier membre.
- Le serveur : deux tables et la dernière connexion de chaque personnage (migration `AddGuilds`), fonder, inviter, accepter, quitter, exclure, donner un rang ou transmettre le titre ; les nouvelles poussées aux membres en jeu ; un personnage supprimé quitte sa guilde.
- Edmée, greffière de la mairie, nouveau PNJ de la place de Clairval : c'est chez elle qu'on fonde une guilde.
- Le canal `/gu`, entendu des membres en jeu où qu'ils soient ; le nom de la guilde sous le pseudo des autres joueurs sur la carte.
- La fenêtre Guilde (touche G) : membres par rang, en jeu d'abord, rang, niveau, classe, zone ou dernière connexion, et les icônes permises par son rang ; inviter depuis la fenêtre ou le menu d'un autre joueur.
- 18 tests de plus (xUnit, dont 3 d'intégration à deux et trois clients) et l'auto-test du village.
- Reste : le texte d'Edmée à relire par le scénariste ; quatre rangées au panneau « Menus » à confirmer ; le playtest de Charles.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
