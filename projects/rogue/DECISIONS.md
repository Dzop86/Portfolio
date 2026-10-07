# Décisions, rogue

## R1. Un générateur aléatoire écrit dans le projet
**Choix :** SplitMix64 (une dizaine de lignes, `Rng.cs`), seule source de hasard du jeu, tirée dans un ordre fixe.
**Pourquoi :** le serveur doit rejouer une partie exactement comme le client l'a jouée. `System.Random` ne garantit pas la même suite d'une version de .NET à l'autre, et le client Godot (net8.0) et le serveur (net10.0) ne tourneront pas sur la même.
**Limite :** toute modification des règles (ordre des tirages, valeurs) change l'issue des parties enregistrées : le numéro de version du format devra suivre (les parties de référence des tests le signalent).

## R2. La partie = la graine et les actions
**Choix :** une partie enregistrée ne contient que la graine et les actions, une lettre chacune ; le rejeu recalcule tout le reste et refuse la première action non permise, avec sa position. Aucune propriété en plus n'est acceptée (un champ `score` est refusé).
**Pourquoi :** le serveur ne fait confiance à rien de ce que dit le client ; une partie de 1 500 tours pèse 1,5 Ko.
**Alternatives :** envoyer le score signé par le client (la clé serait dans le client), envoyer l'état final (invérifiable).
**Limite :** rien n'empêche un joueur de chercher hors ligne la meilleure suite d'actions pour une graine choisie : d'où R6.

## R3. La bibliothèque en net8.0, le reste en net10.0
**Choix :** `Rogue.Core` cible net8.0, la version des projets C# de Godot 4 ; le terminal et les tests ciblent net10.0 (LTS).
**Limite :** .NET 8 n'est plus supporté après novembre 2026 ; la cible suivra celle de Godot.

## R4. Des étages simples, toujours connexes
**Choix :** salles rectangulaires triées de gauche à droite, chacune reliée à la suivante par un couloir en L.
**Pourquoi :** la connexité est garantie par construction (et vérifiée par les tests) ; pas de cas d'échec à rattraper.
**Limite :** les couloirs se croisent parfois et traversent des salles ; pas de cycles voulus, ni de portes.

## R5. Un pilote automatique dans la bibliothèque
**Choix :** `Autopilot` joue des parties entières à partir de ce que voit le joueur.
**Pourquoi :** il fournit aux tests des parties complètes et variées (rejeu, parties de référence), il mesure l'équilibrage (`--stats`), et servira de démonstration au client Godot.
**Limite :** il joue mal (il combat tout ce qu'il voit) : son taux de sortie (27,6 %) est une mesure de difficulté, pas une borne.

## R6. Le serveur impose la graine des parties classées
**Choix (proposition validée par Charles le 7 octobre 2026) :** une partie classée commence par une demande au serveur, qui tire la graine, la garde avec le compte et une date d'expiration ; le score n'est accepté que pour cette graine, une seule fois.
**Pourquoi :** sans cela, un joueur pourrait essayer hors ligne des milliers de graines et d'actions avant d'envoyer la meilleure partie.
**Limite :** un joueur peut encore s'aider d'un programme pendant sa partie (le pilote automatique en est un) ; aucune vérification côté serveur ne distingue un humain d'un programme qui joue des coups légaux.

## R7. Une API minimale, sans ASP.NET Core Identity complet
**Choix :** API minimale (`MapGroup`, `TypedResults`), deux tables (joueurs, parties) ; seul le `PasswordHasher` d'Identity est repris pour les mots de passe (PBKDF2) ; jetons JWT signés en HMAC-SHA256 par l'API elle-même.
**Pourquoi :** le besoin se limite à un nom et un mot de passe ; Identity complet apporterait rôles, confirmation d'adresse, verrouillage et une dizaine de tables inutiles ici. Les réponses typées alimentent le document OpenAPI.
**Limite :** pas de jeton de rafraîchissement ni de révocation : un jeton reste valable 12 heures ; pas de changement de mot de passe.

## R8. Le classement en SQL PostgreSQL
**Choix :** la meilleure partie de chaque joueur est choisie par `SELECT DISTINCT ON ("PlayerId")`, sur laquelle EF Core compose le tri, la limite et le nom du joueur.
**Pourquoi :** EF Core 10 ne sait pas traduire `GroupBy` suivi de `First` avec une projection (erreur 500 trouvée par les tests) ; charger toutes les parties en mémoire ne passerait pas à l'échelle.
**Limite :** la requête est propre à PostgreSQL.

## R9. Une erreur du client ne ferme pas la partie
**Choix :** un corps mal formé, une autre graine ou une partie inachevée sont refusés sans fermer la partie ; une partie que les règles refusent (action impossible) la ferme, comme une partie marquée.
**Pourquoi :** un client défectueux ne doit pas faire perdre une partie ; un tricheur ne doit pas pouvoir essayer plusieurs suites d'actions sur la même graine.
