# Plan du portfolio

## Objectif
Montrer qu'un ingénieur docteur en informatique graphique sait piloter la génération de code par l'IA dans une quinzaine de technologies, et surtout la relire, la tester et la livrer. Public visé : recruteurs (FR et EN) et milieu académique.

## Principes
- Un site vitrine multi-pages, bilingue, responsive, installable (PWA), hébergé gratuitement sur GitHub Pages.
- Un fil rouge : les maillages 3D. Les projets forment une chaîne cohérente.
- Chaque projet : tests unitaires + intégration, CI (multi-OS si compilé), Docker si serveur, démo en ligne (statique ou pré-calculée), README, `REVIEW.md`, `DECISIONS.md`.
- Roadmap par sprints de deux semaines (S1 à S145), affichée sur la page Gestion de projet : l'état de chaque phase (fait, en cours, prévu) est calculé depuis `scrum/sprint-NN.md`. Les dates réelles sont celles des commits.

## Les 23 projets
Source de vérité : `data/projects.json`. Résumé :

| # | Projet | Stack principale | Sprint | Points |
|---|---|---|---|---|
| 1 | Vitrine (ce site) | HTML, CSS, JS, Node, Playwright | S1-S9+S35+S40+S44+S63 | 16 |
| 2 | Topologie 3D | C++, Three.js | S4-S5+S36-S41+S43 | 36 |
| 3 | Bibliothèque C de maillages | C, CMake, WebAssembly, libFuzzer | S2-S6+S42 | 10 |
| 4 | Visionneuse Qt/OpenGL | C++, Qt, OpenGL | S26-S27 | 13 |
| 5 | API Python | FastAPI, pytest, Docker | S6-S7 | 8 |
| 6 | ML et MLOps | PyTorch, scikit-learn, DVC, MLflow, ONNX | S8+S16 | 13 |
| 7 | Base SQL des benchmarks | PostgreSQL, sql.js, pgTAP | S10 | 5 |
| 8 | Mini-langage | Flex, Bison, C, OCaml, js_of_ocaml | S11-S12 | 8 |
| 9 | Carrefour en Ada | Ada, SPARK, AUnit | S9+S35 | 8 |
| 10 | Calcul parallèle | OpenMP, CUDA, OpenCL | S28-S29 | 13 |
| 11 | Dashboard React | React, TypeScript, Vite | S30-S31 | 13 |
| 12 | Dashboard Angular | Angular, Jest, Cypress | S32 | 8 |
| 13 | Lancer de rayons | C++, WebAssembly | S33-S34 | 8 |
| 14 | Éditeur LaTeX | TypeScript, KaTeX | S13-S14 | 8 |
| 15 | Mini-cours G-cartes | JS, SVG | S15+S40 | 7 |
| 16 | Jeu : Othello | C, WebAssembly | S17-S18 | 8 |
| 17 | Jeu : bataille navale | Java, JavaFX | S19 | 5 |
| 18 | Jeu : aventure textuelle | Java | S20 | 5 |
| 19 | Jeu : bataille (cartes) | Ada | S21 | 3 |
| 20 | Jeu : morpion | Python | S22 | 3 |
| 21 | Jeu : roguelike 2D | Godot 4, C#, ASP.NET Core, EF Core, PostgreSQL, JWT | S23-S25 | 13 |
| 22 | Jeu : Osmose, RPG tactique (en pause après S60, D58) | Godot 4, C#, ASP.NET Core, PostgreSQL, Rust, Tauri | S45-S60+S64-S145 | 364 |
| 23 | Assistant documentaire (RAG) | Python, FastAPI, Qdrant, embeddings, API Claude, Docker | S61-S62 | 10 |

Total : 585 points sur 145 sprints de deux semaines.

## CI/CD
- GitHub Actions : CI principale, déploiement Pages, matrice multi-OS.
- GitLab CI : abandonné le 6 octobre (pas de compte GitLab) ; tous les projets, ML compris, passent par GitHub Actions.
- Docker : un Dockerfile multi-stage par service, `compose.yaml` global, images sur GHCR.
- MLOps : DVC (données et modèle), MLflow (expériences), entraînement et évaluation en CI avec seuil de précision bloquant, déploiement du modèle dans le conteneur FastAPI.

## Ordre de réalisation (décision de Charles, 6 octobre 2026)
Faits : vitrine (en continu), bibliothèque C, topologie 3D, API Python, ML (jeu de données et modèles).
Ensuite, dans cet ordre : Ada, base SQL, mini-langage, éditeur LaTeX, mini-cours G-cartes. La suite du ML (export ONNX, classification dans l'API, page de résultats) est reportée après eux. Le tableau ci-dessus et `data/scrum.json` suivent cet ordre.

## Périmètre revu (décision de Charles, 6 octobre 2026, D25)
Les microservices Spring et l'API ASP.NET sont retirés : ils ne seront pas faits. La visionneuse Qt/OpenGL est gardée et vient juste après la suite du ML : S16 ML (ONNX, classification dans l'API), S17-S18 Qt/OpenGL, S19-S20 calcul parallèle, S21-S22 React, S23 Angular, S24 Bootstrap.

## Jeux (décision de Charles, 6 octobre 2026, D26)
Une section « Jeux de mes études » s'ajoute à la page Projets : quatre jeux programmés pendant ses études, réécrits de zéro avec l'IA (tests, CI, démo jouable quand c'est possible). Othello en C jouable dans le navigateur (S17-S18), bataille navale en Java/JavaFX (S19), aventure textuelle en Java (S20, réécrite sans reprendre le code d'origine, écrit à deux), bataille en Ada (S21). Ils viennent après la suite du ML (S16) ; Qt/OpenGL passe en S22-S23 et la suite est décalée de cinq sprints.

## Deux jeux de plus (décision de Charles, 7 octobre 2026, D31)
Un morpion en Python (S22) et un roguelike 2D rejoignent les jeux, juste après la bataille. Le roguelike (D32, 13 points, S23-S25) : client Godot 4 en C# pour le bureau (Godot 4 n'exporte pas le C# vers le web), règles partagées en C#, API ASP.NET Core de scores avec comptes JWT, EF Core, PostgreSQL, Swagger, Docker et GitHub Actions ; l'API vérifie chaque score en rejouant la partie. Qt/OpenGL passe en S26-S27 et la suite est décalée d'autant.

## Un lancer de rayons à la place de Bootstrap (décision de Charles, 7 octobre 2026, D45)
La migration Bootstrap (jQuery puis Bootstrap) est retirée. À sa place, un lancer de rayons en C++ compilé en WebAssembly, utilisable en ligne sur sa fiche (S33-S34, 8 points) : sphères, plans et maillages OBJ du fil rouge accélérés par une BVH, matériaux diffus, métal et verre, ombres, anticrénelage, rendu progressif, choix de la scène et de la caméra, image de référence testée.

## Finitions (demande de Charles, 7 octobre 2026, D46)
Après le sprint 34, un sprint 35 de finitions (5 points) : une interface pour le carrefour en Ada (3 points de plus pour ce projet, qui passe à 8), dont la logique reste celle du programme Ada (il exporte son automate, la fiche le rejoue), le rôle de chaque technologie sur chaque fiche et une gestion de projet à jour (vitrine terminée, backlog vide, bilan des risques) : 2 points de plus pour la vitrine, qui passe à 7. Règle depuis (remarque de Charles) : tout ajout met à jour la gestion de projet, poids, risques et calendrier, et des tests le vérifient.

## Hauteur et points critiques (demande de Charles, 7 octobre 2026, D47)
Sprint 36 (4 points, topologie passe de 13 à 17) : sur la visionneuse de topologie, une filtration par la hauteur comme le filtre Elevation de ParaView, et les points critiques de la hauteur (minimums, selles, maximums) par la théorie de Morse discrète, calculés en C++.

## Persistance et graphe de Reeb (demande de Charles, 8 octobre 2026, D48)
Deux sprints de 4 points, topologie passe de 17 à 25. Sprint 37 : diagrammes de persistance de la hauteur (paires naissance-mort en dimensions 0, 1 et 2, classes sans fin égales aux nombres de Betti), calculés en C++ par réduction de la matrice de bord, affichés sur la fiche avec un seuil qui sépare le bruit des vraies formes. Sprint 38 : graphe de Reeb de la hauteur (composantes des lignes de niveau entre deux valeurs critiques), autant de boucles que le genre, dessiné sur le maillage.

## Relecture de la persistance, temps de calcul, G-cartes (demande de Charles, 8 octobre 2026, D49)
Neuf points, deux sprints. Sprint 39 (3 points, topologie) : le diagramme de persistance, jugé peu clair à la relecture, est repensé : code-barres à côté du nuage, chaque paire reliée à ses deux sommets sur le maillage, explication guidée qui suit le seuil de hauteur. Sprint 40 (6 points) : temps de calcul mesurés de la persistance et du graphe de Reeb, affichés sur la fiche, avec ce qu'apporterait un GPU, et la limite de 32 Mo des fichiers expliquée (topologie, 2 points, qui passe à 30) ; le cours G-cartes corrigé : décomposition d'un objet par dimensions croissantes, α0, α1, α2, et liaisons αi redessinées (G-cartes, 2 points, qui passe à 7) ; le nombre de projets et de points à jour partout, captures des tableaux de bord comprises (vitrine, 1 point) ; le burndown compté en stories livrées, chaque ajout de périmètre visible au sprint où il arrive (vitrine, 1 point, qui passe à 9).

## Persistance étendue (demande de Charles, 8 octobre 2026, D50)
Un sprint de 5 points, topologie passe de 30 à 35. Sprint 41 : à la relecture, Charles attendait que la boucle du tore naisse à une selle et meure à l'autre, comme celle du graphe de Reeb ; la persistance ordinaire (des sous-niveaux) la garde jusqu'à l'infini, à raison. La persistance étendue (filtration des sous-niveaux puis des sur-niveaux) apparie aussi les classes sans fin : calculée en C++, testée par les dualités de Poincaré et de Lefschetz, affichée sur la fiche avec la paire selle-selle reliée à la boucle du graphe de Reeb.

## Comptage d'arêtes de lib-c (décision de Charles, 8 octobre 2026, D51)
Un sprint de 2 points, lib-c passe de 8 à 10. Sprint 42 : les mesures du projet SQL ont montré que lib-c trie ses clés d'arêtes avec `qsort` plus longtemps qu'elle ne lit le fichier ; un tri par base en temps linéaire les remplace, mesuré avant et après, résultats inchangés.

## Repère d'orientation (demande de Charles, 8 octobre 2026, D52)
Un sprint de 1 point, topologie passe de 35 à 36. Sprint 43 : un petit repère x rouge, y vert, z bleu dans un coin de la visionneuse, qui tourne avec la caméra.

## Chaque projet expliqué sans jargon (demande de Charles, 8 octobre 2026, D53)
Un sprint de 2 points, la vitrine passe de 9 à 11. Sprint 44 : en haut de chaque fiche, un encart dépliable « En bref, sans jargon » pour une personne qui n'est pas informaticienne : ce qu'est le projet, à quoi il sert, ce qu'il montre des compétences de Charles.

## Un RPG tactique à la manière de Dofus (décision de Charles, 9 octobre 2026, D54)
Six sprints, 26 points. Un jeu à télécharger, écrit de zéro (aucun code, nom ni image d'Ankama), avec des sprites libres (Kenney, CC0) : S45 les règles du combat dans une bibliothèque C# partagée (grille isométrique, points d'action et de mouvement, ligne de vue, sorts, IA) ; S46 le combat isométrique jouable dans Godot 4 ; S47 le serveur ASP.NET Core de comptes et de personnages ; S48 la création de personnage personnalisable ; S49 la ville d'accueil sans monstre, avec trois PNJ ; S50 le launcher en Rust (Tauri) et les exécutables pour les trois systèmes. Solo d'abord, multijoueur plus tard ; caractéristiques, monstres, paysages et décor viendront en sprints de plus. Pas de démo web : captures, vidéo et explication sur la fiche.

## Osmose : le launcher, le serveur et la création refaits (décision de Charles, 9 octobre 2026, D55)
Trois sprints, 9 points, après l'essai du jeu par Charles sous Windows et un questionnaire. S51 le launcher refait : nom de compte, mot de passe et « Se connecter » seulement (la connexion met à jour puis lance le jeu), mémoriser le nom et le mot de passe (gestionnaire d'identifiants du système), inscription, mise à jour automatique à l'ouverture, serveur caché, une présentation moins austère. S52 dans le jeu, comme Dofus : choix du serveur (direct tant qu'il n'y en a qu'un), puis liste visuelle des personnages (pseudo, classe, niveau, icône) et création s'il y en a moins de cinq. S53 un écran de création à part, plein écran : plusieurs couleurs (tenue, cheveux, peau), taille et carrure, et le choix sur pièces d'un pack libre de modèles à pièces détachables (cheveux, yeux, vêtements, accessoires). Ensuite, après un nouveau questionnaire : les classes retravaillées (quatre éléments, effets, sorts de classe débloqués par niveau, lisibilité et animations du combat), la progression (expérience, caractéristiques, sorts à monter, équipement), puis le monde (cartes reliées, caméra).

## Osmose : éléments, classes, progression et monde (décision de Charles, 9 octobre 2026, D56)
Huit sprints, 24 points, après un second questionnaire. S54 les quatre éléments (une caractéristique chacun, résistances) et les effets (soin, bouclier, poussée et attirance, effets dans la durée, zones) ; S55 les invocations, les sorts à rangs débloqués par niveau, les caractéristiques dans les dégâts ; S56 les trois classes refaites, une vingtaine de sorts chacune ; S57 un combat lisible (survol, frise de l'ordre de jeu, effets visuels) ; S58 l'expérience et les niveaux 1 à 100, 10 points de caractéristique et 1 point de sort par niveau ; S59 les 14 emplacements d'équipement, les panoplies, l'inventaire en pages ; S60 de grandes zones avec une caméra qui suit le joueur, la carte du monde ; S61 (70 depuis D58, D59, D61 et D62) une première famille de monstres en groupes visibles. Ensuite, après un nouveau questionnaire : donjons, quêtes, groupes, guildes, métiers, succès, bestiaire, cosmétiques, tutoriel.

## Un schéma d'architecture sur chaque fiche (demande de Charles, 9 octobre 2026, D57)
Un sprint de 5 points, la vitrine passe de 11 à 16. Sprint 62 (63 depuis D58 et D59), avant la reprise d'Osmose : sur chaque fiche qui a au moins trois morceaux qui se parlent, un schéma « Comment les morceaux tiennent ensemble », comme celui d'Osmose (boîtes, technologies, flèches nommées), décrit en données, dessiné en SVG par le générateur, en français et en anglais, lisible à 375 px et dans les deux thèmes.

## Un assistant documentaire avec RAG ; Osmose en pause (décision de Charles, 10 octobre 2026, D58)
Osmose s'arrête après le sprint 60. Les sprints sont renumérotés dans l'ordre où ils se font : deux sprints de 5 points, 61 et 62, pour l'assistant ; les premiers monstres d'Osmose deviennent le sprint 63 (qui attend sa reprise), les schémas d'architecture le sprint 64. L'assistant : un assistant qui répond aux questions sur une documentation technique (celle du portfolio : README, décisions, OpenAPI) en citant fichier et lignes, qui refuse quand les sources ne répondent pas, avec recherche hybride dans Qdrant, API FastAPI, Docker et une évaluation sur des questions de référence.

## Les schémas avant la reprise d'Osmose (décision de Charles, 10 octobre 2026, D59)
Après l'assistant documentaire, les schémas « Comment les morceaux tiennent ensemble » de chaque fiche passent en premier (sprint 63), puis Osmose reprend avec ses premiers monstres (sprint 64). Aucun point de plus.

## Osmose : le personnage, les sorts et l'inventaire refaits (décision de Charles, 10 octobre 2026, D61)
Après avoir essayé le jeu, Charles demande des écrans à la hauteur des jeux du genre. Deux sprints de 3 points avant les monstres (qui deviennent le sprint 66) : S64 les caractéristiques qui comptent (10 points = +2 dégâts dans leur élément), les coups critiques, un sort tous les cinq niveaux environ et la fiche de caractéristiques ; S65 l'écran des sorts (couleur d'élément, fourchettes normales et critiques, fiche au survol) et l'inventaire (personnage entouré de ses emplacements, onglets, grille d'objets).

## Osmose : éléments, sorts, deck, menus et déplacement (décision de Charles, 10 octobre 2026, D62)
Après un second essai du jeu, quatre sprints de 3 points avant les monstres (qui deviennent le sprint 70) : S66 les caractéristiques Vitalité, Terre, Eau, Feu, Air (une couleur et un effet chacune, bases communes 8 PA, 4 PM, 10 d'initiative, réinitialisation à tout moment) ; S67 vingt-quatre sorts par classe (quatre par élément, huit neutres, cinq au niveau 1 puis un tous les cinq niveaux), avec leurs icônes ; S68 un deck de douze sorts et sa barre de combat ; S69 le menu Échap, les sons, une barre latérale d'icônes et un déplacement libre hors combat.

## Osmose : un monde de cartes, vu du ciel, et huit familles de monstres (décision de Charles, 14 octobre 2026, D63)
Les premiers monstres (sprint 70, 3 points) deviennent sept sprints de 3 points (18 de plus) : S70 le monde en cartes (une zone = 5 × 5 cartes reliées par leurs bords, le bois d'abord) ; S71 la carte du monde vue du ciel, comme une seule grande carte ; S72 les monstres (groupes qui errent, combat au clic, réapparition, XP, butin) et les sangliers ; S73 les loups ; S74 le bois moisi, ses champignons et ses zombies ; S75 le lac, ses grenouilles et ses crabes ; S76 les champs, ses abeilles et ses taupes. Chaque famille compte cinq monstres de la même espèce, un par rôle (DPS mêlée, DPS distance, tank, support, boss).

## Osmose : des cartes plus grandes, la frise du tour, le combat au clic droit (décision de Charles, 14 octobre 2026, D64)
Après avoir parcouru le monde de D63, deux sprints de 3 points : S77 les cartes des zones en 48 × 32 cases (au lieu de 16 × 12), les passages sans points cardinaux (le nom de la zone seulement quand on en change) ; S78 la frise du combat limitée au tour en cours, qui recommence à chaque tour, le combat lancé par un clic droit puis « Attaquer », et plus de jeu hors ligne proposé au joueur.

## Osmose : retours d'essai, donjons, métier, chat de proximité, quêtes (décision de Charles, 14 octobre 2026, D65)
Après l'essai du sprint 78, six sprints : S79 (3 points) la connexion mémorisée dans le jeu, les morts qui quittent le plateau, le numéro du tour près d'une horloge, une commande d'administration ; S80 (5) un donjon par famille, trois salles (affaiblie, pleine force, avec le chef), une clef consommée à l'entrée, un trousseau d'administrateur ; S81 (3) le métier de serrurier qui fabrique les clefs, les ressources et la clef laissées par les monstres ; puis, après D66 et D67, S84 (3) le chat de proximité ; S88 (3) les quêtes et leur journal ; S89 (3) le début de la quête principale et les quêtes annexes.

## Osmose : le deck et le passage d'une carte à l'autre (décision de Charles, 15 octobre 2026, D66)
Après l'essai du sprint 80, deux sprints avant le chat et les quêtes : S82 (5 points) un deck de 0 à 12 sorts glissés-déposés, vidé d'un bouton, partagé par un code, enregistré sans erreur (le jeu se reconnecte seul) ; S83 (3) les passages à la manière des jeux du genre (une flèche au survol du bord, un chemin qui contourne le décor), le zoom gardé, plus de rotation, 20 % plus vite, les donjons sur la carte du monde.

## Osmose : des cartes plus lisibles et de vrais donjons (décision de Charles, 15 octobre 2026, D67)
Après l'essai du sprint 82, deux sprints avant les quêtes : S85 (3 points) un quadrillage léger à afficher sur la carte du monde, un clic près du bord ou hors de la carte qui mène au passage de ce côté, deux fois moins de décor ; S86 (5) des donjons dont l'entrée est un bâtiment imposant et dont chaque salle est une carte où l'on marche, parle, change d'équipement et se soigne avant d'attaquer le groupe de la salle.

## Osmose : les icônes, le chat discret, les fenêtres mobiles, la fin du combat (décision de Charles, 15 octobre 2026, D68)
Pendant le sprint 85 (qui passe à 4 points) : des bordures ouvertes, les icônes des sorts réparées, le chat caché sauf quand une ligne arrive, les fenêtres déplacées par leur en-tête. Puis S87 (3 points) : la fenêtre de fin de combat (gagnants, perdants, expérience, Éclats et butin de chacun, statistiques, « Fermer » sans changer de place) et la monnaie.

## Osmose : des familles de monstres qui se jouent chacune autrement (15 octobre 2026, D69)
D'après la conception `projects/rpg/docs/design-familles.md` (game designer, lancée par Charles) : les huit familles suivaient le même gabarit. Cinq sprints : S90 (5 points) le moteur, des passifs (début de tour, coup reçu, PV bas, allié tombé, mort), un saut et un échange de place, plusieurs cartes de combat par famille, la correction des états PA et PM posés sur soi ; puis, zone par zone, chaque famille avec sa leçon, son passif, sa faiblesse, un tank qui protège, un soutien qui replace et un chef à deux phases, et une nouvelle carte de combat : S91 le Bois, S92 le Bois moisi, S93 le Lac, S94 les Champs (3 points chacun).

## Osmose : les pseudos, le compte d'essai, les fenêtres en combat (15 octobre 2026, D70)
Après l'essai du sprint 93, pendant le sprint 94 (qui passe à 5 points) : des pseudos d'une majuscule et de lettres seulement, la création de personnages par la commande d'administration (le compte d'essai de Charles, un personnage de chaque classe au niveau 100), les caractéristiques et l'inventaire consultables en combat (C et I).

## Osmose : la suite après D69, du butin au chapitre 1 (15 octobre 2026, D71)
D'après la proposition du game designer `projects/rpg/docs/suite-apres-d69.md`, validée par Charles avec ses valeurs par défaut : S95 (5 points) la quête de la Bauge débloquée, l'initiative par l'équipement et les panoplies du Bois et du Bois moisi ; S96 (3) celles du Lac et des Champs ; S97 (3) les Éclats chez Rose ; les métiers, S98 (5) la récolte (bûcheron, mineur, pêcheur, paysan, herboriste), S99 (3) le quartier des artisans, S100 (3) forgeron et tailleur, S101 (3) l'alchimiste et les potions ; le jeu à plusieurs, S102 (3) se voir, S103 (5) le groupe et l'échange, S104 et S105 (5 chacun) le combat à plusieurs ; la fin du chapitre 1, S106 à S108 (3 chacun), jusqu'au Disjoint.

## Osmose : les icônes, la fenêtre Métiers, une ressource tous les 5 niveaux (16 octobre 2026, D72)
Demandes de Charles intégrées par le game designer (v3 de `projects/rpg/docs/suite-apres-d69.md`), avec ses valeurs par défaut : S98 (3 points) les icônes des objets ; les métiers de 1 à 100, une nouvelle ressource de récolte tous les 5 niveaux (S99, 40 ressources des niveaux 1 à 35), la fenêtre Métiers sur la touche J (S100) ; le reste décalé : artisans S101 à S103, jeu à plusieurs S104 à S107, chapitre 1 S108 à S110.

## Osmose : la vie qui se garde entre les combats (16 octobre 2026, D73)
Règle de Charles, conçue par le game designer (v4 de `projects/rpg/docs/suite-apres-d69.md`) : S101 (5 points) la vie gardée d'un combat à l'autre, qui remonte hors combat, la défaite à 1 PV, le pain ; le boulanger rejoint l'alchimiste en S104 (5 points) ; la suite décalée d'un cran, jusqu'à S111.

## Osmose : un fil conducteur, Clairval en douze cartes, le tuto, la mini-carte (16 octobre 2026, D74)
Demandes de Charles, conçues par le game designer (v5 de `projects/rpg/docs/suite-apres-d69.md`), textes par le scénariste (`projects/rpg/docs/textes-v5.md`) : S102 (5) le nouveau Clairval et le dialogue réparé, S103 (5) le moteur des quêtes, S104 (3) le tuto, S105 (3) la mini-carte, puis l'artisanat (S106-S107), les quêtes des zones jusqu'au Disjoint (S108-S110) et le jeu à plusieurs (S111-S114).

## Osmose : après l'essai du sprint 102, une récolte qu'on voit, un vrai village, le suivi d'une quête (16 octobre 2026, D75)
Retours de Charles : S103 (3) la ressource visible sur chaque point de récolte, une barre de récolte, la ressource qui disparaît et revient au bout d'une minute, les Éclats dans l'inventaire ; S104 (5) un vrai village, avec maisons, habitants, ateliers hors des routes, poulailler, écurie et animaux ; S106 (3) suivre une quête, affichée à gauche avec une flèche et sa place sur la carte, après le moteur des quêtes (S105) ; la suite décalée de trois crans, jusqu'à S117.

## Osmose : un métier pour chaque pièce d'équipement (17 octobre 2026, D76)
Demande de Charles, conçue par le game designer (v6 de `projects/rpg/docs/suite-apres-d69.md`), textes par le scénariste (`projects/rpg/docs/textes-v6.md`) : S109 (5) le forgeron (armes, boucliers) et le tailleur (plastron, épaulettes, coiffe, cape), l'arme à deux mains comptée double, les objets de départ remis au budget ; S110 (3) le bijoutier (anneaux, amulette) et le cordonnier (bottes, ceinture), deux nouveaux artisans ; pièces fabriquées sans initiative ; la suite décalée d'un cran, jusqu'à S118.

## Osmose : après l'essai du build 1.0.92 (17 octobre 2026, D77)
Retours de Charles : S115 (3) plus de consommables en combat (les potions soignent hors combat, les huiles retirées), une palissade qui ne ressemble plus à des fleurs, plus de maisons au village, 10 % de vitesse en plus ; S116 (5) un journal de quêtes comme dans les jeux du genre ; S117 (5) la fiche d'un objet au survol partout et son clic droit (équiper, utiliser, recettes, bestiaire, panoplie, partage dans le chat) ; le jeu à plusieurs décalé de trois crans, jusqu'à S121.

## Osmose : la suite après le jeu à plusieurs (22 octobre 2026, D79)
Conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11), valeurs par défaut validées par Charles : S122 (3) une IA de héros qui joue en équipe et des mesures sur 100 combats, S123 (3) l'équilibre à plusieurs réglé (la reine des abeilles, les hordes), S124 (5) la minuterie du tour, les absents, rejoindre, exclure, S125 (3) les combats à plusieurs gardés en base et un playtest à plusieurs ; S126 (3) les amis et les messages privés, S127 (3) le rappel et les pierres de passage ; une tranche du chapitre 2 : S128 (3) le socle après 30, S129 (5) Quatre-Chemins, S130 (5) les glyphes, S131 (5) la Forêt des Aïeux, S132 et S133 (3 + 3) deux familles, S134 (5) deux donjons, S135 (3) les recettes 40 et 45, S136 (5) les quêtes et un playtest ; S137 (5) les guildes (passées en S145 par D80).

## Osmose : l'arbre des talents commun aux classes (24 octobre 2026, D80)
Conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md`, éléments dans `projects/rpg/docs/elements.md`), valeurs par défaut validées par Charles : S137 (5) la tranche verticale, Enlisé et Au plus près pour la Sentinelle, avec un go ou pas go ; S138 (5) les éléments composés et la Foudre, S139 (3) la Foudre pour toutes les classes, S140 (5) la fenêtre « Ma route » et les réactions, S141 (3) Distance et Rythme, S142 (4) Terrain et la Rémanence, S143 (4) le Soutien, S144 (3) l'équilibre et un playtest ; les guildes passent en S145. Les 57 autres points (cinq paires, Lumière et Ténèbres, triplets, spécialisations) viendront avec les chapitres, Unisson avec la Saga II.

## Osmose : après le playtest du sprint 144 (25 octobre 2026, D81)
Retours de Charles sur la 1.0.131, dans le sprint 144 (2 points de plus, 5 en tout) : une fenêtre d'amis à la manière de Dofus (onglets, colonnes, cartes, icônes), un panneau « Menus » en grille au bord droit à la place de la colonne d'icônes, pas de Foudre dans le filtre des sorts sans son talent. L'arbre des talents en vrai arbre (tronc, paires d'éléments, styles de jeu) attend les réponses au questionnaire du game designer (`projects/rpg/docs/talents-arbre-questions.md`) et sera chiffré ensuite.

## Gestion de projet
- Scrum, sprints de deux semaines, Definition of Done dans `data/scrum.json`.
- Registre des risques : `data/scrum.json`, affiché sur la page Gestion de projet, revu à chaque rétro.
- Priorisation MoSCoW : Must = vitrine, topologie, C, ML, SQL, Qt. Should = les sept jeux. Could = Angular, lancer de rayons. Won't = microservices Spring, API ASP.NET (retirés le 6 octobre), migration Bootstrap (retirée le 7 octobre).

## Reporté
- Agent conversationnel.
- Mascotte.

## Questions ouvertes
- Situation professionnelle actuelle (fin de l'ATER ?) pour mettre `data/cv.json` à jour.
- Libellé unique entre le CV industriel et le CV analytique.
- Nom définitif du compte ou de l'organisation GitHub.
