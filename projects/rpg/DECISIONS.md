# Décisions, rpg

Le choix du projet et de sa pile (Godot 4 en C#, règles partagées, ASP.NET Core, launcher Tauri, solo d'abord) est D54, dans le `DECISIONS.md` du portfolio.

## T1. Une grille carrée dessinée en losanges, distances en pas
**Choix :** le plateau est une grille carrée ; le client la dessine en losanges (projection isométrique, `Iso.cs`). On se déplace vers les quatre cases qui partagent un côté, et toutes les distances (portées, déplacements) se comptent en pas (distance de Manhattan).
**Pourquoi :** c'est le modèle des jeux du genre : une portée dessine un losange autour du lanceur, et les règles ne connaissent pas l'affichage.
**Alternatives :** une grille hexagonale (autre genre de jeu) ; des déplacements en diagonale (portées en carré, moins lisibles en isométrique).

## T2. Une ligne de vue exacte, et la règle des coins
**Choix :** les cases traversées par le segment entre les deux centres, calculées en entiers par produits en croix (aucun arrondi) ; un coin franchi exactement n'est bloqué que si les deux cases voisines bloquent.
**Pourquoi :** en entiers, le calcul est le même dans les deux sens : si A voit B, B voit A, ce que les joueurs attendent et ce qu'un test vérifie sur 3 000 paires. Le cas du coin doit être tranché ; le plus permissif évite qu'un pilier touché par un seul angle cache une case.
**Alternatives :** Bresenham (asymétrique : A peut voir B sans que B voie A) ; bloquer dès qu'un côté du coin bloque (plus strict, plus de cases cachées).
**Limite :** la règle des coins est un choix de jeu, à confirmer à la relecture.

## T3. Des règles décrites en données, lues strictement
**Choix :** sorts, cartes et scénarios sont des fichiers JSON (`data/`) ; la lecture refuse un champ manquant, inconnu ou nul, et le chargement refuse les incohérences (sort inconnu, case de départ prise ou absente, équipe vide, bornes impossibles).
**Pourquoi :** Charles a annoncé des extensions (caractéristiques, monstres, paysages) ; les ajouter doit être une modification de données. Une faute de frappe doit arrêter le chargement, pas donner un sort silencieusement faux.
**Limite :** les effets des sorts se limitent aux dégâts ; soins, poussées, effets sur la durée demanderont d'étendre le format (et son numéro de version).

## T4. Un combat déterministe, enregistré par ses actions
**Choix :** SplitMix64 écrit dans le projet, seule source de hasard ; un dé est tiré à chaque sort, même sur une case vide, pour que la suite des tirages ne dépende que des actions. Un combat enregistré = scénario, graine (en chaîne) et actions acceptées ; le rejeu refuse la première action non permise, avec sa position.
**Pourquoi :** le serveur pourra rejouer un combat pour le vérifier (le multijoueur et les récompenses en dépendront), et la CI vérifie que deux combats enregistrés donnent le même résultat sur les trois systèmes.
**Limite :** toute modification des règles ou des données change l'issue des combats enregistrés : le numéro de version du format devra suivre.

## T5. Une IA simple et déterministe
**Choix :** frapper le plus fort possible tout de suite (dégâts moyens plafonnés aux points de vie de la cible, puis la cible la plus faible) ; sinon marcher vers la case la plus proche d'où frapper ; sinon s'approcher, en distance de marche réelle autour des obstacles ; sinon finir son tour. Les choix ne dépendent jamais d'un ordre de hachage.
**Pourquoi :** assez bonne pour tester le jeu et le jouer en solo ; déterministe, donc rejouable et testable.
**Alternatives :** recherche sur plusieurs coups (minimax, Monte-Carlo) : plus forte, beaucoup plus lente et difficile à tester ; à envisager avec les monstres.
**Limite :** l'IA ne fuit pas et ne garde pas ses PM ; dans le duel symétrique, celui qui s'approche en premier perd deux fois sur trois.

## T6. Fin de combat garantie
**Choix :** match nul après 50 tours.
**Pourquoi :** deux combattants qui ne peuvent pas s'atteindre (un trou entre eux) ne doivent pas faire tourner un combat sans fin ; les tests et le serveur ont besoin d'une borne.
