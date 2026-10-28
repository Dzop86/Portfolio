# Sprint 160 : Osmose : la fenêtre de guilde

**Objectif :** une refonte graphique de l'interface d'Osmose demandée par Charles après S156, glissée avant le socle 45-60 (D84).

**Goal:** a graphic overhaul of Osmose's interface asked for by Charles after S156, slipped in before the 45-60 groundwork (D84).

| Story | Points | État |
|---|---|---|
| En tant que membre, l'en-tête de la fenêtre de guilde montre le blason dans son écu, le nom, le niveau, sa barre, le message du meneur, et des onglets Membres, Coffre et Journal (rpg) ; auto-test de la fenêtre. | 2 | Fait |
| En tant que membre, la liste des membres montre qui est connecté, la classe, le nom, une note, le rang, le niveau, les points apportés et les points de succès, triable et filtrable, avec les membres déconnectés à afficher, le signal de connexion d'un membre, le nombre de membres et leur niveau moyen (rpg) ; xUnit, intégration à trois clients. | 2 | Fait |
| En tant que joueur sans guilde, je postule à une guilde par son nom avec un mot, et son meneur voit les candidatures, les accepte ou les refuse ; le meneur et les officiers invitent un joueur depuis la fenêtre (rpg) ; xUnit, intégration à trois clients. | 2 | Fait |

## Fait
- Maquette du graphiste (`projects/rpg/docs/graphisme/s160-guilde.md`).
- En-tête : blason encadré, nom, niveau en hexagone et sa barre, semaine, membres et niveau moyen, message du meneur et son crayon (T127).
- Onglets Membres (tableau trié, recherche, note, points apportés, points de succès, menu d'actions, membres déconnectés, signal de connexion), Coffre, Journal, Recrutement.
- Candidatures : postuler par le nom avec un mot, le meneur accepte ou refuse, retirer la sienne ; trois en attente, une semaine ; invitation depuis la fenêtre.
- Serveur : points apportés et note par membre, tables des candidatures et du journal (`AddGuildRecruitment`), nouvelles `Online` et `Applied`.
- Icônes des classes et crayon en relief, hexagone de niveau (`ui_art.py`).
- xUnit (règles des candidatures et des notes), intégration PostgreSQL à trois clients (candidature acceptée et refusée, note, journal, connexion signalée, expiration, points), auto-test du village (recrutement réservé, journal, candidatures, vue hors guilde) : verts.

- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
