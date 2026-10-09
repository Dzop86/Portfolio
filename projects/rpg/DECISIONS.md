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

## T7. Des modèles 3D vus en caméra isométrique (choix de Charles, sprint 46)
**Choix :** les petits modèles 3D de Kenney (Mini Characters, Mini Dungeon, Nature Kit, CC0) sous une caméra orthographique inclinée à 30 degrés (chaque case est un losange deux fois plus large que haut), plutôt que ses sprites 2D isométriques.
**Pourquoi :** comparés sur une même scène (captures montrées à Charles) : le pack 2D, plus fini tel quel, n'a qu'un personnage, sans animation d'attaque ni de mort ; les modèles 3D donnent 12 personnages et un orc, chacun avec plus de vingt animations (marche, attaque, tir, mort), se recolorent et s'équipent pour la création de personnage (sprint 48), et les kits donnent les décors. C'est aussi le choix de Dofus 3.
**Alternatives :** sprites 2D (un seul personnage, à compléter par des dessins faits à la main) ; rendre les modèles 3D en sprites (plus de travail pour le même résultat).
**Limites :** style « low poly » simple ; Kenney n'a ni sanglier ni crapaud : les monstres de l'entraînement deviennent un orque et un braconnier, leurs sorts une massue et une fronde (mêmes valeurs, mêmes combats). La projection 2D du sprint 45 (`Iso.cs`) ne sert plus, la case sous la souris se trouve par un rayon de la caméra : retirée avec ses tests.

## T8. Le côté joueur hors du moteur, et un auto-test par les commandes
**Choix :** `Rpg.Client`, une bibliothèque sans Godot, porte tout ce qu'un joueur peut faire (choisir un sort, survoler, cliquer, finir son tour) et ce qu'il voit (aperçu, textes) ; le client Godot ne fait que dessiner. Option `--selftest` : un combat entier joué par ces commandes, chaque événement passé aux vues, puis vérifié par rejeu et contre l'état des vues.
**Pourquoi :** tester des clics dans un moteur est lent et fragile ; ici, xUnit vérifie 200 combats joués par les commandes, et la CI vérifie sur trois systèmes, et dans l'exécutable exporté, que l'affichage suit le combat. Même principe que le client Godot du roguelike.
**Alternatives :** GdUnit4 (tests dans le moteur : plus près du rendu, mais une dépendance de plus et des tests plus lents) ; des tests par captures d'écran (fragiles d'un pilote graphique à l'autre).
**Limites :** l'auto-test ne voit pas l'image : une couleur mal choisie ou un panneau mal placé ne se voit que sur les captures (relues à chaque changement).

## T9. Les données dans la bibliothèque, et l'apparence dans les données
**Choix :** les fichiers de `data/` sont intégrés à `Rpg.Core` (ressources) ; chaque combattant y a un `look`, le nom de son modèle 3D, que les règles ignorent.
**Pourquoi :** le jeu exporté n'a pas le dossier du dépôt ; le serveur (sprint 47) lira les mêmes données. Un nouveau monstre reste une modification de données, apparence comprise.
**Limites :** changer les données demande de recompiler ; le simulateur garde `--data` pour essayer un dossier modifié.

## T10. Un serveur calqué sur celui du roguelike
**Choix :** API minimale ASP.NET Core, jetons JWT signés HMAC-SHA256 (12 heures, clé par variable d'environnement), mots de passe hachés par le `PasswordHasher` d'ASP.NET Core Identity (PBKDF2), EF Core 10 et PostgreSQL, migrations `dotnet ef`, limite d'essais par adresse sur l'inscription et la connexion, OpenAPI et Swagger UI ; image Docker « chiseled » et service compose `rpg-api` sur le port 8002. Noms uniques sans tenir compte de la casse par un index unique sur le nom en majuscules.
**Pourquoi :** la même pile que l'API de scores du roguelike (sprint 24), déjà relue et testée : le portfolio montre une façon de faire qui se répète, pas une nouvelle à chaque projet. L'index unique tranche entre deux créations simultanées, ce qu'une vérification préalable ne fait pas.
**Alternatives :** ASP.NET Core Identity complet (tables, confirmation d'e-mail, réinitialisation : beaucoup plus que nécessaire, et un e-mail que le jeu n'a aucune raison de demander) ; un fournisseur externe (OpenID Connect : une dépendance et un compte de plus pour le joueur) ; SQLite (plus simple, mais pas ce que le multijoueur demandera).
**Limites :** pas de jeton de rafraîchissement, de changement de mot de passe ni de suppression de compte ; la limite de cinq personnages se vérifie avant l'insertion (deux créations simultanées sur un compte pourraient passer) ; HTTP seulement, un proxy HTTPS sera nécessaire en ligne.

## T11. Deux noms : le compte et le personnage
**Choix :** le nom de compte (3 à 20 lettres sans accent, chiffres, `-`, `_`) sert à se connecter et ne s'affiche pas ; le nom de personnage (3 à 20 lettres, accents compris, traits d'union et apostrophes à l'intérieur, `Hero.IsValidName` de `Rpg.Core`) est celui qu'on voit en combat, unique sur tout le serveur.
**Pourquoi :** comme dans Dofus, un joueur a plusieurs personnages ; le serveur et le jeu valident le nom de personnage avec la même fonction, et un nom de connexion en ASCII évite les surprises de normalisation Unicode à la saisie.
**Limites :** pas de liste de noms interdits (insultes, noms de PNJ) : à ajouter avec la ville (sprint 49).

## T12. L'écran de connexion hors du moteur, et un auto-test contre le serveur
**Choix :** `Lobby` (dans `Rpg.Client`, sans Godot) porte ce que l'écran peut faire et transforme chaque refus du serveur en une clé de texte traduite ; `LobbyView` ne fait que le dessiner. `--lobby-selftest` remplit les vrais champs, appuie sur les vrais boutons (inscription, création, « Combattre »), puis enchaîne l'auto-test du combat avec ce héros ; la CI le lance contre l'image Docker servie par compose. Sans serveur, « Jouer hors ligne » garde le combat du sprint 46.
**Pourquoi :** même principe que T8 : la logique se teste en xUnit (contre un faux serveur pour les pannes, contre le vrai pour les refus), et le seul test dans le moteur passe par le chemin d'un joueur.
**Alternatives :** tester l'écran par des clics simulés à des coordonnées (fragile au moindre déplacement d'un bouton) ; ne tester que l'API (rien ne garantirait que le jeu sait s'en servir).
**Limites :** l'auto-test contre le serveur ne tourne que sous Linux (le service Docker) ; ailleurs, la CI joue le combat hors ligne. L'adresse du serveur se donne par `--server` (par défaut `http://localhost:8002/`) : le launcher du sprint 50 la fournira.
