# Journal des décisions

## D1. Générateur statique maison plutôt qu'un framework
**Choix :** un petit script Node (`src/build.mjs`) qui produit du HTML pur.
**Pourquoi :** la vitrine doit prouver la maîtrise de HTML, CSS et JS sans framework ; React et Angular ont leurs propres projets. Zéro dépendance de production, build en moins d'une seconde.
**Limite :** pas de rechargement à chaud. Acceptable pour un site de cette taille.

## D2. Contenu dans des fichiers JSON
**Choix :** `data/cv.json`, `projects.json`, `scrum.json`, `i18n/*.json`.
**Pourquoi :** une seule source par information, réutilisable par les dashboards React et Angular.

## D3. Une page par langue (`/fr/`, `/en/`)
**Choix :** pages pré-générées plutôt qu'une traduction en JavaScript.
**Pourquoi :** fonctionne sans JS, bien référencé, URL partageables dans la bonne langue. La racine redirige selon la langue du navigateur.

## D4. Scan de données privées dans les tests
**Choix :** motifs génériques (téléphone, e-mail, date de naissance, adresse) plus un secret `PRIVATE_TERMS` facultatif.
**Pourquoi :** le CV original contient des données privées ; le test empêche toute fuite, sans écrire ces données dans le dépôt.

## D5. Données de thèse publiques uniquement
**Choix :** jury, direction, ORCID et HAL sont affichés (déjà publics via theses.fr et HAL). Les noms des responsables de cours et des étudiants encadrés ne le sont pas.
**À valider par Charles.**

## D6. Chrome local en option pour Playwright
**Choix :** `CHROME_PATH` permet de réutiliser un Chrome installé ; la CI télécharge ses propres navigateurs.

## D7. Le projet vit à la racine du dépôt
**Choix :** plus de sous-dossier `portfolio/` ; `package.json`, `src/`, `data/` et `.github/` sont à la racine.
**Pourquoi :** GitHub Actions ne lit les workflows qu'à la racine, les badges du README et `compose.yaml` supposent cette structure, et les futurs projets iront dans `projects/<id>/` à côté du site.

## D8. Node 22 minimum
**Choix :** `engines.node` passe de `>=20` à `>=22` ; la CI teste Node 22 et 24.
**Pourquoi :** `node --test "tests/unit/*.test.mjs"` utilise les motifs glob, apparus avec Node 21 ; sous Node 20 aucun test ne tournait. Node 20 n'est plus maintenu depuis avril 2026.

## D9. Déploiement par GitHub Actions, conditionné aux tests
**Choix :** `deploy.yml` relance `npm test` (scan de confidentialité compris) avant de publier `dist/` sur GitHub Pages ; `ci.yml` couvre la matrice multi-OS, Playwright et l'image Docker.
**Pourquoi :** aucune version dont les tests échouent ne peut être mise en ligne, même si la CI tourne en parallèle.
**Limite :** les tests end-to-end ne bloquent pas le déploiement (ils tournent dans `ci.yml`), pour garder un déploiement rapide.

## D10. Page 404 ancrée à la racine du site par `<base href>`
**Choix :** `404.html` porte `<base href="${BASE_PATH}">` ; `BASE_PATH` vaut `/` par défaut et `/<dépôt>/` sur GitHub Pages (fourni par `configure-pages`). Dans Docker, nginx sert `404.html` via `error_page`.
**Pourquoi :** Pages renvoie `404.html` à l'URL manquante ; sans `<base>`, `./assets/style.css` était cherché sous `/Portfolio/a/assets/`. Les autres pages gardent des liens relatifs, qui fonctionnent quel que soit l'hébergement.
**Alternatives :** CSS en ligne (corrige le style mais pas les liens), redirection JavaScript vers l'accueil (inutilisable sans JS, perd le statut 404).

## D11. Thème gris sombre façon VS Code par défaut
**Choix :** fond gris sombre (`#1f1f1f`, barre `#181818`, panneaux `#252526`) quelle que soit la préférence du système ; la pistache reste l'accent (liens, boutons, onglet actif), le chocolat passe en touches (texte sur pistache, sceau, puces de la frise, barres du Gantt). Le bouton bascule vers un thème clair neutre façon VS Code Light, mémorisé dans `localStorage`. Les variantes de couleur passent par des jetons (`--seal-bg`, `--pressed-bg`, `--dot`...) au lieu de sélecteurs par thème dans `style.css`.
**Pourquoi :** choix de Charles : trop de chocolat sur fond crème, un gris d'éditeur colle mieux au message « je code ».
**Alternatives :** suivre `prefers-color-scheme` (le site serait crème pour la majorité des visiteurs, en clair par défaut).
**Limite :** un visiteur qui préfère le clair doit cliquer une fois ; axe vérifie les contrastes des deux thèmes en end-to-end.

## D12. Une page par projet, à plat dans chaque langue
**Choix :** chaque entrée de `projects.json` génère `<lang>/project-<id>.html`, au même niveau que les autres pages. La fiche reprend l'état, le sprint, la stack, la Definition of Done de `scrum.json`, des liens facultatifs (`links.code`, `links.demo`) et les projets voisins. Toute la carte est cliquable (lien étendu par `::after`), le titre restant le seul lien pour les lecteurs d'écran.
**Pourquoi :** les liens relatifs (`./`, `../assets/`) et le changement de langue (`../en/<page>.html`) marchent sans changement ; ajouter un projet reste une seule entrée JSON.
**Alternatives :** sous-dossier `<lang>/projects/<id>.html` (tous les chemins relatifs à recalculer), une seule page avec ancres (pas d'URL partageable propre ni de titre par projet).
**Limite :** les fiches des projets prévus restent courtes tant que leur README n'existe pas ; la Definition of Done affichée est commune à tous les projets.

## D13. Un workflow par projet technique, filtré par chemin
**Choix :** chaque projet de `projects/<id>/` a son workflow (`.github/workflows/<id>.yml`) déclenché seulement quand ses fichiers changent ; le scan de confidentialité du site couvre aussi `projects/` (hors dossiers de build).
**Pourquoi :** la matrice multi-OS d'un projet compilé ne ralentit pas le déploiement du site, et un badge par projet montre son état.
**Limite :** un changement du site qui casserait un projet ne relance pas son workflow ; les projets restent indépendants du site pour l'éviter.

## D14. Barres d'avancement temporaires, calculées depuis les fichiers de sprint
**Choix :** l'accueil affiche trois barres : projets terminés et en cours sur 17, points faits du dernier sprint, et pour chaque projet en cours les points faits des stories qui le nomment (son identifiant, par exemple `lib-c`), rapportés à son estimation ou aux points prévus s'ils la dépassent. Les chiffres viennent de `scrum/sprint-NN.md` (tableau « Story | Points | État », « Fait » = terminé), lu au build.
**Pourquoi :** une seule source de vérité, le fichier de sprint déjà tenu à jour ; aucune donnée en double à oublier.
**Limite :** une story qui ne nomme pas son projet n'est pas comptée. Pour retirer le bandeau : supprimer `progressPanel` et son appel dans `home` (`src/templates.mjs`), ses clés `progress.*` et ses styles.

## D15. Démo WebAssembly de lib-c : build commité, vérifié par la CI
**Choix :** `projects/lib-c/scripts/build-wasm.sh` compile la bibliothèque avec Emscripten 6.0.11 (image Docker figée) vers `src/assets/wasm/meshlib.{js,wasm}`, qui sont commités. Le job `wasm` de `lib-c.yml` recompile et échoue si le résultat diffère (build vérifié reproductible octet pour octet). La page `project-lib-c` charge le module au premier clic ; `src/assets/meshlib-api.js` sert à la fois au navigateur et aux tests Node.
**Pourquoi :** `npm run build`, Docker et la CI du site restent sans Emscripten ; le site ne peut pas servir un WebAssembly périmé sans que la CI le signale.
**Alternatives :** compiler dans le workflow de déploiement (Emscripten requis partout où l'on construit le site, démo absente en local), `-sSINGLE_FILE` (wasm en base64 dans le JS, un tiers plus lourd).
**Limite :** les fichiers générés (45 Ko) vivent dans le dépôt ; il faut relancer le script après toute modification de la bibliothèque. Fichiers limités à 32 Mo.

## D16. Topologie en WebAssembly, même principe que lib-c
**Choix :** `projects/topologie/scripts/build-wasm.sh` compile le C++ et le lecteur de lib-c avec le même Emscripten figé, exceptions WebAssembly natives (`-fwasm-exceptions`) pour que les maillages invalides remontent une erreur au lieu d'arrêter le module. Sortie commitée dans `src/assets/wasm/topo.{js,wasm}`, recompilée et comparée par le job `wasm` de `topologie.yml`, qui se relance aussi quand lib-c change.
**Pourquoi :** mêmes raisons que D15 ; un changement de lib-c qui modifie le module de topologie est détecté.
**Limite :** les exceptions WebAssembly natives demandent un navigateur de 2022 ou plus récent (Chrome 95, Firefox 100, Safari 15.2).

## D17. Viewer de topologie : three.js empaqueté par esbuild, échelle divergente
**Choix :** `src/viewer/topoviewer.js` importe three.js (r186) et `OrbitControls` ; le build les empaquette et les minifie avec esbuild en un seul module, `assets/topoviewer.js` (560 Ko, 140 Ko compressé), chargé seulement sur la fiche Topologie 3D. three.js et esbuild sont des dépendances de développement : seul `dist/` est publié, et l'étape de build Docker les installe. La courbure est colorée sur une échelle divergente bleu (selle) ↔ gris ↔ rouge (dôme) ; depuis le sprint 6, l'intensité suit le rang de |K| parmi les sommets intérieurs (échelle par quantiles, légende graduée à 50 % et 90 %). Couleurs dans `tokens.css` (`--curv-*`), pas dans le JS.
**Pourquoi :** three.js ne publie plus de build minifié (2,1 Mo bruts) ; l'empaquetage garde le site sans CDN. Au bord, le défaut angulaire mesure le virage du bord, pas la courbure de Gauss : les sommets du bord prennent la valeur de leurs voisins intérieurs, sinon une selle paraissait bordée de dômes. Le gris central est plus clair que le gris neutre habituel, pour que l'éclairage reste lisible.
**Alternatives :** three.js depuis un CDN (dépendance à un tiers à l'exécution), servir les sources non minifiées (2,1 Mo), Babylon.js (plus lourd).
**Limite :** sans WebGL (navigateurs anciens, Firefox sans accélération dans la CI), la 3D est remplacée par un message ; les invariants restent calculés et affichés, ce que teste Playwright. Sur un ruban de Möbius, les normales ne peuvent pas être cohérentes : une couture d'ombrage est visible.

## D18. Échelle de courbure par quantiles
**Choix :** la couleur d'un sommet vaut le rang de son |K| (part des sommets intérieurs de courbure plus faible), signé comme K. La légende indique les valeurs de |K| à 50 % et 90 %, et précise qu'il s'agit d'une échelle par quantiles.
**Pourquoi :** sur des modèles sculptés réels, |K| a une queue très lourde (99ᵉ centile = 1 000 fois la médiane) : avec une échelle linéaire bornée au 95ᵉ centile, presque tout restait gris. Sur les exemples lisses, le rendu change peu.
**Alternatives :** échelle logarithmique (paramètre arbitraire), lissage de la courbure sur un voisinage (modifie les valeurs affichées au survol).
**Limite :** les écarts de couleur ne sont plus proportionnels aux écarts de courbure ; la valeur exacte reste lisible au survol.

## D19. Roadmap par sprints, état calculé depuis les fichiers de sprint
**Choix :** la page Méthode montre les sprints S1 à S27 (S24 depuis D25) au lieu d'un calendrier de septembre 2025 à octobre 2026. Chaque phase de `data/scrum.json` est « faite », « en cours » ou « prévue » selon `scrum/sprint-NN.md` (un sprint est clos quand toutes ses stories sont « Fait » ou « Abandonné »). Le registre des risques ajoute R9 (droits sur les modèles 3D) et le workflow GenAI l'étape « vérifier qu'un test échoue quand on casse le code ».
**Pourquoi :** le calendrier illustratif ne correspondait plus à rien après la réorganisation du 6 octobre ; l'état calculé ne peut pas diverger des fichiers de sprint.
**Limite :** la durée réelle d'un sprint n'est pas affichée ; elle se lit dans les dates des commits.

## D20. Bac à sable SQL : copie SQLite construite dans un web worker
**Choix :** la fiche du projet sql charge sql.js (SQLite en WebAssembly) dans un web worker, qui construit la base depuis `projects/sql/sqlite/schema.sql` et le CSV de la campagne, les deux fichiers étant copiés au build. Le worker est empaqueté avec sql.js par esbuild (`assets/sqlworker.js`) ; la page l'arrête si une requête dépasse 5 s, et en démarre un nouveau à la requête suivante. Au plus 200 lignes sont renvoyées, mais le total compté est exact. La base se charge quand le bac à sable devient visible.
**Pourquoi :** le CSV reste la seule source des données (pas de fichier `.sqlite` binaire commité qui pourrait diverger) ; un worker permet d'arrêter une requête sans fin (CTE récursive) sans figer la page, ce que sql.js ne permet pas dans le fil principal.
**Alternatives :** base `.sqlite` générée au build (le build est synchrone et sql.js ne l'est pas), PostgreSQL compilé en WebAssembly (PGlite, environ 3 Mo contre 0,7), interrogation de l'API PostgreSQL (un serveur à héberger).
**Limite :** dialecte SQLite et non PostgreSQL : la médiane est réécrite avec des fonctions de fenêtrage, la régression avec les sommes des moindres carrés, `ln` et `sqrt` sont fournies par JavaScript, et `regressions(seuil)` devient la vue `campaign_change`. Un test Node vérifie que les médianes et les exposants de SQLite égalent ceux calculés indépendamment en JavaScript.

## D21. Ordres d'affichage calculés, pas hérités des fichiers
**Choix :** `loadData` trie les projets par sprint (`bySprint`) et chaque liste datée du CV du plus récent au plus ancien (`newestFirst` : dernière année, puis la période qui commence le plus tard). Le registre des risques s'affiche dans l'ordre des numéros, et des boutons en en-tête le retrient par probabilité, impact ou score (`app.js`, avec `aria-sort`). Chaque risque a sa matrice probabilité × impact de 3 × 3.
**Pourquoi :** l'ordre des fichiers dérivait dès qu'un projet changeait de sprint ; trié par le code, l'affichage suit les données sans retouche à la main, et un test le vérifie.
**Limite :** à période égale, l'ordre du fichier départage. Le parcours professionnel ne reprend que ce que contient `cv.json`.

## D22. Maille dans le navigateur : analyseur en WebAssembly, interpréteur par js_of_ocaml
**Choix :** la fiche du projet langage charge l'analyseur C compilé par Emscripten (`wasm/maillec.wasm`, 66 Ko) et l'interpréteur OCaml compilé par js_of_ocaml (`wasm/maille-interp.js`, 110 Ko), qui se passent l'arbre en S-expression, comme en ligne de commande. Les deux sont produits par `projects/langage/scripts/build-web.sh` dans des images Docker aux versions figées (Flex 2.6.4, Bison 3.8.2, OCaml 5.2, js_of_ocaml 6.4.1, Emscripten 6.0.11), commités, et la CI vérifie qu'ils sont identiques à une reconstruction (comme D15).
**Pourquoi :** le visiteur exécute le vrai code des deux langages, pas une réécriture en JavaScript. Un test Node passe chaque exemple par la version web et compare au résultat de la chaîne native : il a révélé que l'`int` d'OCaml n'a que 32 bits sous js_of_ocaml (`fact 20` faux dans le navigateur), d'où des entiers `Int64` partout.
**Alternatives :** wasm_of_ocaml (entiers de 63 bits, mais une chaîne d'outils de plus), un interpréteur réécrit en JavaScript (deux implémentations à maintenir).
**Limite :** l'évaluation tourne dans le fil principal ; les budgets du navigateur (2 millions d'étapes, 1 000 appels imbriqués) la gardent sous la seconde. Chaque appel Maille coûte plusieurs cadres JavaScript : 5 000 appels imbriqués débordaient la pile de Node 22 sous macOS, d'où 1 000, et un débordement éventuel est rattrapé en erreur d'exécution.

## D23. Éditeur LaTeX : un volet source, un volet rendu, sans éditeur de code tiers
**Choix :** la fiche du projet latex montre l'article du portfolio dans une simple zone de texte et son rendu à côté (onglets sous 900 px), redessiné 150 ms après la dernière frappe ; la bibliothèque TypeScript et KaTeX sont empaquetés par esbuild (`assets/latexeditor.js`), la feuille de style et les polices de KaTeX copiées au build. Diagnostics et plan sont des boutons qui placent le curseur ou font défiler l'aperçu. Le titre de l'article est rendu en `h3` (option `headingLevel`), sous le `h1` de la page et le `h2` de l'éditeur.
**Pourquoi :** une zone de texte native fonctionne au clavier, au lecteur d'écran et sur mobile sans dépendance ; le rendu complet d'un article prend quelques millisecondes, inutile de le mettre dans un worker.
**Alternatives :** CodeMirror ou Monaco (coloration et numéros de ligne, mais plusieurs centaines de Ko de plus et une accessibilité à vérifier), rendu à chaque frappe sans délai.
**Limite :** pas de coloration syntaxique ni de numéros de ligne dans la source ; un `.tex` sans `\begin{document}` est rendu comme un fragment.

## D24. Mini-cours G-cartes : figure dessinée au build, interactivité en plus
**Choix :** la fiche du projet gcartes affiche six leçons (`projects/gcartes/course.json`, en français et en anglais), le patron du cube en SVG avec ses 48 brins, généré au build depuis la bibliothèque (`net.js`), puis un quiz. `assets/gcourse.js`, empaqueté avec la bibliothèque, ajoute le choix d'un brin, les déplacements par α0, α1, α2 (boutons utilisables au clavier), le tracé des liaisons du brin courant, les orbites mises en évidence et la correction du quiz.
**Pourquoi :** les leçons et la figure se lisent sans JavaScript et sont indexables ; la figure et les comptes affichés viennent du même code que celui testé, ils ne peuvent pas diverger.
**Alternatives :** un cube 3D (three.js) où les brins se chevauchent, des images figées.
**Limite :** les brins de la figure se choisissent à la souris ; au clavier, on s'y déplace avec les boutons α depuis le brin 0.
**Révision (remarque de Charles) :** la première version, en couleurs du thème sombre, était illisible. Les figures sont désormais dessinées comme dans un manuel, sur fond blanc dans les deux thèmes (couleurs `--gm-*` de `tokens.css`) : faces blanches, brins noirs, α0 en noir, α1 en rouge, α2 en bleu, toutes les liaisons dessinées en permanence (α2 seulement entre carrés voisins dans le patron, les autres à la sélection). Une seconde figure décompose deux carrés en G-carte en quatre étapes : l'objet, puis les coupes selon α2, α1 et α0 (`src/decompose.js`, testée).

## D25. Périmètre réduit : Spring et ASP.NET retirés
**Choix (décision de Charles, 6 octobre 2026) :** les projets « Microservices Spring » et « API ASP.NET » sont retirés des données, du plan, de la roadmap et des textes ; il reste 15 projets et 130 points sur 24 sprints. La visionneuse Qt/OpenGL est gardée, en S17-S18, juste après la suite du ML (S16) ; les projets suivants avancent de trois sprints.
**Conséquences :** plus de Java, de C#, de Jenkins ni de Testcontainers dans la pile annoncée (le C# reste dans l'enseignement du CV) ; l'accueil ne parle plus d'API Java et C# ; le risque R2 note le retrait. Les fichiers de sprint passés gardent leur texte d'origine (historique).
**Limite :** la priorisation MoSCoW gagne une catégorie « Won't » pour garder trace des deux projets.

## D26. Une section « Jeux de mes études », réécrits de zéro
**Choix (décision de Charles, 6 octobre 2026) :** quatre jeux programmés pendant ses études (Othello en C, bataille navale en Java/JavaFX, aventure textuelle en Java, bataille en Ada) deviennent quatre projets du groupe `games`, affichés dans leur propre section de la page Projets, après la suite du ML (S17 à S21). Ils sont réécrits avec l'IA et suivent les mêmes règles que les autres projets (tests, CI multi-OS, relecture) ; l'Othello sera jouable sur le site (moteur C en WebAssembly).
**Pourquoi réécrire :** le code d'origine du jeu textuel (« Robert Bizarre Adventure », retrouvé dans une archive) a deux auteurs ; le réécrire évite de publier le travail d'un camarade sans son accord, et montre la méthode du portfolio sur un sujet connu.
**Conséquences :** 19 projets, 151 points, 29 sprints ; Qt/OpenGL en S22-S23, calcul parallèle, React, Angular et Bootstrap décalés de cinq sprints.

## D27. Résultats du ML lus au build dans les sorties du pipeline
**Choix :** la fiche du projet ml affiche les précisions (référence, PointNet, PointNet exporté), le seuil de la CI et les deux matrices de confusion, lus au build dans `metrics.json`, `confusion.json`, `export/pointnet.json` et `params.yaml`.
**Pourquoi :** ce sont les fichiers que le pipeline DVC écrit et que la CI vérifie : la page ne peut pas afficher un chiffre que le modèle n'a pas obtenu.
**Limite :** les cases des matrices ne sont pas teintées selon leur effectif : une teinte forte sous un texte clair échouait au contraste (axe, thème sombre). Bonnes réponses en vert, erreurs en rouge, zéros discrets, avec les couleurs déjà validées des scores de risque.

## D28. Othello jouable : le moteur C en WebAssembly, un plateau de boutons
**Choix :** le moteur et l'IA en C (`projects/othello`) sont compilés par Emscripten 6.0.11 (`scripts/build-wasm.sh`, 4,5 Ko), avec une petite API qui garde la partie et son historique (annulation) ; le build est commité et vérifié par la CI, comme lib-c (D15). La page dessine 64 boutons avec un seul arrêt de tabulation (les flèches le déplacent, Entrée ou Espace joue), chacun nommé pour les lecteurs d'écran (« d3, vide, coup possible »).
**Pourquoi :** le visiteur joue contre le vrai code testé par perft ; des boutons natifs fonctionnent à la souris, au toucher et au clavier sans canevas.
**Limite :** sous 560 px, les coordonnées autour du plateau sont masquées pour garder des cases de 44 px au moins (à 375 px, le plateau prend la largeur de l'écran moins 8 px) ; chaque case garde son nom dans son libellé.

## D29. Bataille navale : une capture plutôt qu'une démo
**Choix :** la fiche du projet naval montre une capture de partie (en français ou en anglais selon la page), produite sans écran par les tests de l'application (JavaFX avec Monocle), et la commande pour lancer le jeu.
**Pourquoi :** une application JavaFX ne tourne pas dans un navigateur ; une capture faite par le code testé, à partir d'une partie reproductible, montre l'interface réelle.
**Limite :** l'image est commitée (`src/assets/images/naval-*.png`) ; elle se régénère à la main (`mvn test -Dnaval.screenshots=...`) quand l'interface change.

## D30. L'aventure textuelle dans la page : Java compilé par TeaVM
**Choix :** le moteur Java de `projects/aventure` est compilé en module JavaScript par TeaVM (`mvn -Pweb package`, 150 Ko) et commité dans `src/assets/wasm/aventure.js` ; la CI vérifie qu'il correspond aux sources. La fiche l'affiche dans un petit terminal : journal lu par les lecteurs d'écran (`role="log"`), ligne de commande avec historique aux flèches, raccourcis pour les commandes courantes.
**Pourquoi :** le visiteur joue avec le vrai moteur Java testé, sans machine virtuelle Java dans le navigateur.
**Limite :** le moteur doit rester dans ce que TeaVM sait compiler (pas de `String.format`, de `ResourceBundle` ni d'expressions régulières) ; une nouvelle partie à chaque visite, avec une graine au hasard.

## D31. Deux jeux de plus : un morpion en Python, un roguelike 2D en C#
**Choix (décision de Charles, 7 octobre 2026) :** deux autres jeux de ses études rejoignent la section Jeux, réécrits avec l'IA comme les autres : un morpion en Python (S22, 3 points) et un roguelike 2D en C# (S23-S24, 8 points), juste après la bataille en Ada.
**Conséquences :** 21 projets, 162 points, 32 sprints ; Qt/OpenGL en S25-S26, puis calcul parallèle, React, Angular et Bootstrap décalés de trois sprints. Le C# revient dans la pile annoncée (il avait disparu avec l'API ASP.NET, D25).

## D32. Le roguelike : Godot 4 en C# sur le bureau, et une API qui rejoue les parties
**Choix (décision de Charles, 7 octobre 2026) :** client Godot 4 en C# (builds Windows, Linux et macOS par la CI), règles du jeu dans une bibliothèque C# partagée (donjon généré à partir d'une graine, combats au tour par tour, déterministe), API ASP.NET Core de scores avec comptes (JWT), EF Core et PostgreSQL, documentée par Swagger/OpenAPI, en Docker, testée en CI. L'API ne croit pas un score sur parole : le client envoie la graine et la liste des actions, le serveur rejoue la partie avec la même bibliothèque et calcule lui-même le score.
**Pourquoi :** la pile demandée par Charles ; elle ramène ASP.NET Core et EF Core retirés avec l'ancien projet (D25), dans un usage concret. Le rejeu côté serveur règle la question de la triche sans confiance dans le client.
**Limite :** Godot 4 ne sait pas exporter un projet C# vers le web (documentation officielle de Godot, octobre 2026) : pas de partie dans le navigateur, la fiche montrera des captures ou une courte vidéo. Estimation portée de 8 à 13 points (S23-S25) ; Qt/OpenGL passe en S26-S27, la roadmap compte 33 sprints.

## D33. La bataille : des statistiques plutôt qu'une démo
**Choix :** la fiche du projet bataille affiche ce que disent 100 000 parties jouées par le programme Ada (parties sans fin, durée, batailles, avantage du premier joueur), lues dans `projects/bataille/data/stats.json` ; la CI recalcule ce fichier et échoue s'il change.
**Pourquoi :** compiler Ada pour le navigateur demanderait une chaîne de compilation de plus pour un jeu qui se regarde plus qu'il ne se joue ; les statistiques montrent un résultat réel (42 % de parties sans fin avec un ramassage dans un ordre fixe).
**Limite :** les blocs de commandes, qui peuvent défiler sur mobile, sont focalisables pour rester utilisables au clavier (constaté par axe).

## D34. Le morpion dans la page : un livre de coups calculé en Python
**Choix :** la fiche du projet morpion se joue contre l'IA dans la page. Les règles, une vingtaine de lignes, sont réécrites en JavaScript (`src/assets/morpion-api.js`) ; les coups de l'IA viennent de `projects/morpion/data/book.json`, la valeur et les meilleurs coups des 5 478 positions, calculés par le programme Python et copiés dans le site au build. pytest vérifie que le fichier commité correspond au programme ; les tests Node, que les règles JavaScript voient les mêmes positions et que l'IA jouée depuis le livre ne perd aucune partie possible.
**Pourquoi :** une seule IA, celle qui est testée en Python, sans faire tourner Python dans le navigateur (Pyodide pèse plusieurs mégaoctets) ; le livre pèse 124 Ko.
**Limite :** les règles existent en deux langages ; les tests Node les comparent position par position.

## D35. Le roguelike, premier volet : des règles déterministes et des parties rejouables
**Choix :** les règles du roguelike (D32) vivent dans une bibliothèque C# sans dépendance (`projects/rogue/src/Rogue.Core`, net8.0 pour Godot 4), avec son propre générateur aléatoire (SplitMix64) ; une partie s'enregistre sous la forme graine + actions (JSON versionné) et se rejoue au même score. Le jeu se joue d'abord dans le terminal (net10.0). Tests xUnit v3 sur Microsoft.Testing.Platform, analyseurs .NET au niveau recommandé, CI Linux, Windows et macOS, dont des parties de référence (même graine, même score sur les trois systèmes).
**Pourquoi :** c'est ce que l'API du sprint 24 rejouera pour calculer elle-même les scores ; le terminal permet de jouer et de tester les règles sans attendre le client Godot.
**Limite :** la fiche du projet n'a pas encore de démonstration : des captures du client Godot viendront au sprint 25 (D32).

## D36. Le roguelike, deuxième volet : une API de scores qui rejoue les parties
**Choix :** API minimale ASP.NET Core (`projects/rogue/src/Rogue.Api`) : comptes (PBKDF2), jetons JWT, parties classées dont le serveur tire la graine (R6, validé par Charles), parties rejouées par la bibliothèque de règles pour calculer le score, classement ; EF Core et PostgreSQL avec migrations, OpenAPI et Swagger UI, limitation du débit. Image Docker « chiseled » (sans shell, utilisateur non root) et services `rogue-api` (port 8001) et `rogue-db` dans `compose.yaml`.
**Tests :** 32 tests d'intégration sur un vrai PostgreSQL (service de la CI sous Linux), test de fumée de bout en bout sur l'image construite par `compose.yaml`.
**Limite :** l'API n'est pas hébergée (le site est statique) ; ses tests ne tournent que sous Linux (pas de conteneurs Linux sur les machines Windows et macOS de GitHub) ; la base de `compose.yaml` n'a pas de mot de passe et n'est joignable que depuis les services du fichier.

## D37. Le roguelike, troisième volet : le client Godot 4
**Choix :** client de bureau Godot 4.7 en C# (`projects/rogue/godot`), mince : l'interface est construite en code, les règles, textes, touches et le client HTTP de l'API viennent des bibliothèques C# (`Rogue.Core`, `Rogue.Client`). Parties libres, parties classées sur l'API (D36), pilote automatique à regarder. La CI le teste sans écran sur Linux, Windows et macOS et produit les exécutables des trois systèmes ; la fiche du projet montre une capture faite par le client lui-même (D29 pour la bataille navale, même principe).
**Limite :** pas de partie dans le navigateur (Godot 4 n'exporte pas le C# vers le Web) ; exécutables non signés, gardés 14 jours dans les artefacts de la CI ; tout le roguelike passe en net10.0 (R3 de `projects/rogue/DECISIONS.md`).

## D38. La visionneuse Qt/OpenGL au bout du fil rouge
**Choix :** application de bureau Qt 6 en C++20 (`projects/qt`) : lecture par lib-c, analyse par topologie (intégrés par CMake, comme topologie intègre lib-c), dessin OpenGL 3.3 core, interface Qt Widgets en français et en anglais (Qt Linguist). Tests Qt Test, dont un rendu hors écran relu pixel par pixel ; CI Linux, Windows et macOS avec OpenGL logiciel. La fiche du projet montre une capture faite par l'application (`--screenshot`).
**Suite (sprint 27) :** sélection d'un sommet ou d'une face par lancer de rayon, vue enregistrée en image ; exécutables Windows (windeployqt), macOS (macdeployqt, image disque) et Linux (AppImage) produits et lancés une fois par la CI, liés depuis la fiche.
**Limite :** exécutables non signés par un certificat, gardés 30 jours dans les artefacts de la CI.

## D39. Un accueil plus court, des onglets renommés, un filtre par langage
**Choix (remarques de Charles, 7 octobre 2026) :** l'accueil ne garde que la présentation, l'avancement, les chiffres, le fil rouge et le workflow GenAI : plus de boutons vers les projets et le CV (déjà dans le menu), plus de cartes de projets (déjà sur la page Projets), et plus de barre pour un sprint terminé. Le nombre de projets et de langages du texte d'accroche est calculé depuis les données (il annonçait encore « dix-sept projets »). Les onglets deviennent « CV et expériences » et « Gestion de projet » (les adresses `research.html` et `method.html` restent, pour ne pas casser les liens). La page Projets se filtre aussi par techno (C, C++, C#, Java, Python… et HTML/CSS, d'où « techno » plutôt que « langage », à la demande de Charles), combiné au filtre par catégorie, à partir d'un champ `techs` de chaque projet.
**Pourquoi un champ à part :** la pile technique mélange langages, bibliothèques et outils (PyTorch, Docker) ; le projet ML est en Python sans que « Python » figure dans sa pile.

## D40. La page Gestion de projet montre tout le cycle Scrum
**Choix (remarque de Charles, 7 octobre 2026) :** la page, renommée, ajoute au plan, aux risques et à la Definition of Done : le product backlog (projets restants, estimation en points, sprints prévus), le journal des sprints (objectif en français et en anglais, points livrés sur engagés, lien vers le compte rendu complet), deux métriques (vélocité par sprint avec sa moyenne, burndown des points de projets face à un rythme régulier jusqu'au sprint 33), l'estimation et la capacité (deux échelles, capacité observée, prévision) et le principe des rétrospectives. Tout est calculé au build depuis `data/projects.json` et `scrum/sprint-NN.md` ; chaque fichier de sprint porte désormais son objectif en anglais (ligne `**Goal:**`), sans quoi le build échoue.
**Graphiques :** SVG dessinés au build, une seule série de données chacun (pistache en thème sombre, vert bambou en clair, contraste vérifié), une référence grise en pointillés, infobulles natives et tableau des données pour les lecteurs d'écran.
**Limites :** les comptes rendus détaillés restent en français (lien vers le fichier) ; les rétrospectives sont écrites par Charles dans les fichiers de sprint, la page n'en montre que le principe ; les sprints ont été menés plus vite que les deux semaines du plan, la vélocité se lit donc par sprint et non par semaine.

## D41. Une empreinte sur chaque feuille de style et chaque script
**Choix :** le build ajoute à chaque lien CSS et JS des pages une empreinte de son contenu (`style.css?v=` suivie de 10 caractères de son SHA-256) ; un test vérifie qu'elle correspond au fichier.
**Pourquoi :** GitHub Pages autorise les navigateurs à garder les fichiers dix minutes ; juste après un déploiement, Charles a vu la nouvelle page Gestion de projet avec l'ancien `style.css` (graphiques tout noirs) et l'ancien `app.js` (filtre par techno sans effet). Un fichier modifié change désormais d'adresse.
**Limite :** les modules importés par d'autres scripts (`import './othello-api.js'`) n'ont pas d'empreinte ; ils changent rarement sans le script qui les importe, dont l'empreinte change aussi, mais le navigateur peut encore garder l'ancien module dix minutes.

## D42. Le calcul parallèle sur la courbure du fil rouge
**Choix :** le projet parallele (`projects/parallele`) calcule la courbure de Gauss de topologie en deux passes indépendantes (par face, puis par sommet sur un voisinage compressé rangé dans l'ordre des faces), en C++ séquentiel, OpenMP, OpenCL (double précision) et CUDA (double et simple précision). Les résultats séquentiels et OpenMP sont identiques au bit à ceux de topologie ; OpenCL et CUDA en double sont à 1e-12. La CI exécute OpenCL sur le processeur (PoCL) sous Linux et compile CUDA dans l'image de NVIDIA. Les benchmarks de `parbench` sont versionnés (`projects/parallele/data/bench.json`) et affichés sur la fiche (graphique, tableau, décomposition copies/calcul).
**Limite :** pas de carte graphique en CI : les tests CUDA ne tournent que sur la GTX 1660 de Charles ; sous WSL, OpenCL n'atteint pas la carte NVIDIA. Les mesures viennent d'une seule machine. Détails dans `projects/parallele/DECISIONS.md` (P5, P6).

## D43. Le dashboard React lit une API JSON statique produite par le site
**Choix :** le projet react (`projects/react`) est une application Vite + React 19 + TypeScript strict, avec son propre `package.json` : React n'entre pas dans les dépendances de la vitrine (D1). Le build du site écrit une API JSON statique versionnée (`dist/api/v1/` : projets, sprints et vélocité, benchmarks de parallele, mesures de lib-c de la base SQL, résultats du modèle ML), tirée des mêmes fichiers que les pages (D2) ; le dashboard la lit au chargement et est publié sous `dist/dashboard/`, sur GitHub Pages comme le reste. Langue par `?lang=fr|en` (sinon celle du navigateur), couleurs de `src/assets/tokens.css` importées telles quelles. Sprint 30 : API, vues Projets, Sprints et Résultats ; sprint 31 : visionneuse 3D, tests Playwright et fiche.
**Pourquoi :** une seule source de vérité pour la vitrine et les deux dashboards (React puis Angular, qui lira la même API) ; une API statique coûte zéro serveur et se teste comme un fichier ; le dashboard montre un vrai client qui charge, attend et gère l'erreur, ce que des données importées à la compilation ne montreraient pas.
**Limite :** l'API n'est mise à jour qu'au déploiement ; le dashboard demande JavaScript (la vitrine, elle, s'en passe, D3) ; deux arbres `node_modules` à maintenir.

## D44. Le dashboard Angular, même API, comparé au React
**Choix :** le projet angular (`projects/angular`) est une application Angular 22 (composants autonomes, signaux, sans zone.js), avec son propre `package.json` et TypeScript 6.0 (Angular 22 n'accepte pas encore TypeScript 7, que prennent le site et le dashboard React). Il lit la même API statique `api/v1/` (D43) par un service injecté, et montre deux vues du dashboard React (Projets et Résultats) avec le routeur d'Angular. Publié sous `dist/angular/`. Tests : Jest (jest-preset-angular, TestBed) pour les unitaires et l'intégration sur la vraie API, Cypress pour le bout en bout, comme annoncé dans le plan. La fiche du projet compare les deux dashboards sur des mesures (taille des paquets, temps de build, lignes de code, tests), produites par un script.
**Pourquoi :** les deux applications rendent la même chose à partir des mêmes données : la comparaison porte sur les frameworks, pas sur le contenu ; le plan annonçait deux vues (8 points), pas une copie complète.
**Limite :** deux versions de TypeScript dans le dépôt ; pas de visionneuse 3D côté Angular (elle resterait la même scène three.js) ; les mesures de la comparaison dépendent de la machine qui les prend.

## D45. Bootstrap retiré, un lancer de rayons à sa place
**Choix (décision de Charles, 7 octobre 2026) :** le projet « Migration Bootstrap » (S33, 5 points) est retiré des données, du plan et de la roadmap. À sa place, un lancer de rayons en C++ compilé en WebAssembly par Emscripten (`projects/raytracer`, S33-S34, 8 points), utilisable en ligne sur sa fiche : sphères, plans et maillages OBJ du fil rouge accélérés par une BVH, matériaux diffus, métal et verre, ombres, anticrénelage, rendu progressif dans un canevas, choix de la scène et de la caméra. Tests GoogleTest, image de référence comparée, CI Linux, Windows et macOS.
**Pourquoi :** un rendu par lancer de rayons tient au cœur de l'informatique graphique et au fil rouge des maillages, là où une migration Bootstrap n'apprenait rien de plus que les dashboards React et Angular ; C++ et Emscripten sont déjà en place (topologie, lib-c).
**Conséquences :** 21 projets, 170 points, 34 sprints ; plus de Bootstrap ni de jQuery dans la pile annoncée ; la phase « React, Angular, Bootstrap, version finale » devient « React et Angular » (S30-S32) puis « Lancer de rayons, version finale » (S33-S34). Les fichiers de sprint passés gardent leur texte d'origine (historique).

## D46. Un sprint 35 de finitions : interface du carrefour en Ada, gestion de projet soldée
**Choix (demande de Charles, 7 octobre 2026) :** après le sprint 34, un sprint 35 de 5 points. Le carrefour en Ada reçoit une interface sur sa fiche (3 points, le projet passe de 5 à 8 points, sur S9+S35) ; la vitrine passe à « terminé », avec 2 points de plus (rôle de chaque technologie, gestion de projet soldée : de 5 à 7, sur S1-S9+S35 ; réestimée le jour même, à la remarque de Charles que la gestion de projet doit suivre chaque ajout) ; le registre des risques reçoit le bilan de chaque risque ; le backlog vide le dit.
**Interface :** Ada ne se compile pas simplement en WebAssembly (pas de chaîne GNAT pour wasm dans Alire). Plutôt que de réécrire le contrôleur en JavaScript, le programme Ada exporte son automate complet (`carrefour --automaton` : tous les états atteignables depuis `Start`, avec l'état après une seconde et après une demande sur chaque axe) ; la fiche ne fait que suivre ces transitions. Comme pour les statistiques de la bataille (D33), le JSON est commité et la CI vérifie qu'il est celui que le programme produit aujourd'hui.
**Limites :** les durées de vert de la fiche sont celles de la simulation (30 s et 20 s) ; d'autres durées demandent de regénérer l'automate.

## D47. La hauteur et ses points critiques, par la théorie de Morse discrète
**Choix (demande de Charles, 7 octobre 2026) :** sprint 36, topologie passe de 13 à 17 points. `topo::elevation` calcule la hauteur de chaque sommet selon une direction, l'ordre de la filtration (égalités départagées par l'indice du sommet, « simulation de simplicité »), et l'indice de chaque sommet, 1 − χ(lien inférieur) (Banchoff) : +1 pour un minimum ou un maximum intérieur, 1 − k pour une selle dont le lien inférieur a k morceaux. La visionneuse colore par la hauteur, ne garde que les triangles sous un seuil (filtration par sous-niveau, comme le filtre Elevation puis Threshold de ParaView), marque les points critiques et trace χ du sous-niveau.
**Pourquoi :** la formule par le lien inférieur vaut sur tout maillage, bords et surfaces non orientables compris, et les indices somment exactement à χ : c'est ce que vérifient les tests, sur chaque maillage et plusieurs directions. Un sommet au bord dont tous les voisins sont plus bas a un indice 0 (le sous-niveau ne change pas de type) : il n'est pas compté comme maximum.
**Alternatives :** gradient discret de Forman ou complexe de Morse-Smale (TTK) : plus riche (paires de persistance), mais plus lourd que ce qu'une fiche peut expliquer.
**Limites :** la direction est un axe (x, y ou z) sur la fiche ; une surface plate selon cet axe (un tore couché) donne des points critiques dus au départage des égalités, dont les indices somment quand même à χ.

## D48. Diagrammes de persistance et graphe de Reeb de la hauteur
**Choix (demande de Charles, 8 octobre 2026) :** sprints 37 et 38, 4 points chacun ; topologie passe de 17 à 25 points (187 en tout, 38 sprints). Sprint 37 : `topo::persistence`, persistance de la filtration « lower-star » de la hauteur (un sommet, puis les arêtes et les faces dont il est le plus haut), par réduction de la matrice de bord sur Z/2 ; paires (naissance, mort) en dimensions 0, 1 et 2, et classes sans fin. Sprint 38 : `topo::reeb_graph`, un nœud par point critique, et un arc par composante connexe des lignes de niveau entre deux valeurs critiques consécutives.
**Pourquoi :** c'est la suite directe du sprint 36 (même filtration, mêmes points critiques), et les deux se vérifient par des invariants connus d'avance : classes sans fin égales aux nombres de Betti (1, 2g, 1), stabilité (une perturbation de ε déplace chaque point du diagramme d'au plus ε), boucles du graphe de Reeb égales au genre, degrés des nœuds (1 aux extremums, 3 à une selle simple).
**Alternatives :** TTK (Topology ToolKit) ou GUDHI : complets et rapides, mais la règle 3 demande d'écrire de zéro et ce sont de grosses dépendances pour WebAssembly. Persistance par union-find (H0) et dualité (H2) seulement : plus rapide mais sans H1, alors que H1 (les anses) est le plus parlant sur un tore. Graphe de Reeb par l'algorithme en ligne de Pascucci et al. : meilleure complexité, plus difficile à vérifier.
**Limites :** la réduction est cubique au pire, quasi linéaire en pratique : mesuré 0,7 s pour 360 000 triangles et 2,5 s pour un million en natif, 0,5 s pour 300 000 en WebAssembly. Le calcul tourne sur le fil principal, donc la fiche plafonne la persistance à 300 000 triangles et le dit au-delà ; un maillage bruité donne plus de 100 000 paires, donc le diagramme n'en dessine que les 2 000 plus persistantes (le reste est compté) et le tableau 100. Les paires de persistance nulle (deux sommets de même hauteur, départagés par l'indice) sont dans la sortie C++ mais pas sur la fiche : elles viennent du départage, pas de la forme. Le graphe de Reeb par tranches (coupé aux nœuds et à 32 hauteurs régulières) coûte à peu près O(T log T + k √T) : une ligne de niveau ne croise que peu de triangles. Mesuré 0,23 s pour 360 000 triangles et 0,75 s pour un million en natif, 0,22 s pour 300 000 en WebAssembly, 0,86 s sur un tore bruité à 2 300 nœuds ; la fiche le refuse au-delà de 2 000 nœuds, illisibles. Les arcs passent par les barycentres des lignes de niveau : sur une surface très courbée, le tracé peut sortir du volume. Sur une surface non orientable, la persistance sur Z/2 compte les classes de Z/2 (le ruban de Möbius a une classe H1, comme attendu).

## D49. Relecture de la persistance, temps de calcul, limite des fichiers, cours G-cartes
**Choix (demande de Charles, 8 octobre 2026) :** sprints 39 et 40, 9 points (196 en tout, 40 sprints) ; topologie passe de 25 à 30, G-cartes de 5 à 7, la vitrine de 7 à 9. Sprint 39 : le diagramme de persistance repensé (code-barres, paires reliées au maillage, explication guidée). Sprint 40 : temps CPU mesurés et affichés pour la persistance et le graphe de Reeb ; limite de 32 Mo expliquée ; cours G-cartes corrigé (décomposition par dimensions croissantes : objet, α0, α1, α2, G-carte ; liaisons αi redessinées) ; nombre de projets et de points tenu à jour partout, captures comprises ; burndown compté en stories livrées (choix de Charles, sur recommandation de Claude).
**Pourquoi :** relecture de Charles : le diagramme seul ne se lit pas sans connaître la persistance ; un code-barres et le lien aux sommets disent ce que chaque point représente. Les temps de calcul montrent le coût réel des algorithmes ; l'ordre α0, α1, α2 suit la définition d'une G-carte (dimension croissante), et les captures des tableaux de bord affichaient encore 19 projets terminés sur 21 et 170 points.
**Alternatives :** une version GPU (WebGPU ou CUDA) de la persistance ou du graphe de Reeb : écartée avec Charles, car la réduction de la matrice de bord est séquentielle colonne après colonne et le graphe de Reeb repose sur des union-find ; la fiche dit ce que le GPU accélérerait (la hauteur, le tri, la persistance H0 par fusion en parallèle). Lever la limite de 32 Mo (lecture dans un Web Worker, mémoire WebAssembly à 4 Go, lecture en flux ou Memory64) : écarté avec Charles, la limite est expliquée chiffres à l'appui.
Le burndown comptait par projet entier, brûlé à son dernier sprint : réouvrir un projet faisait remonter tous les sprints passés et affichait 45 points restants pour 8 de stories ouvertes ; compté en stories livrées, l'historique ne bouge plus et chaque ajout de périmètre apparaît comme une marche au sprint où il arrive. Alternative écartée : garder le comptage par projet, plus simple mais faux à chaque ajout.
**Limites :** mesuré pour la limite : un OBJ de 36 Mo (un million de triangles) se lit en 3,7 s et occupe 190 Mo de mémoire WebAssembly ; 150 Mo (4 millions) en 11 s et 750 Mo. Ce qui bloque au-delà de 32 Mo : la constante `MAX_BYTES`, le plafond de mémoire WebAssembly (1 Go, 4 Go au plus en wasm32), le calcul sur le fil principal qui fige la page plusieurs secondes, la copie du fichier dans la mémoire WebAssembly, et la mémoire des téléphones.

**Réalisation du sprint 39 :** le code-barres et le nuage sont deux listes (`role="listbox"`) dont chaque paire est une option, avec une seule tabulation par liste et les flèches pour aller d'une paire à l'autre : des centaines de paires ne deviennent pas des centaines d'arrêts de tabulation ; la paire choisie est la même partout (index dans les paires). L'explication guidée est calculée sur les paires du maillage affiché (`tour` dans `topo-api.js`), pas écrite pour le tore seul : elle raconte la classe la plus persistante de chaque sorte et saute ce qui n'existe pas. Alternative écartée : des étapes figées pour les exemples, fausses dès qu'on dépose un fichier. Limite : au doigt, barres et points font moins de 44 px ; le tableau des paires, aux boutons de 44 px, est le chemin conforme.

**Réalisation du sprint 40 :** les temps viennent d'un banc versionné (`tools/topo_bench.cpp` natif, `scripts/bench.mjs` en WebAssembly sur le même tore) plutôt que de mesures à la main : relançable, et un test refuse un tableau incomplet. Le burndown lit ses marches dans `data/scrum.json` (`scopeSteps` : après quel sprint, quels sprints, quelles décisions) ; alternative écartée : les déduire des dates de commit, fragile. Les changements d'avant la livraison (D25, D31, D32, D45) restent dans le périmètre de départ : la courbe n'en montre pas de marche, le risque R2 les raconte. Limite : les points de stories (203) diffèrent des estimations des projets (196), chaque échelle garde son usage (D49). La capture Angular enregistre les chiffres photographiés (`projects/angular/data/screenshots.json`) : tout changement d'état d'un projet exige de la régénérer, un test le rappelle.

## D50. Persistance étendue
**Choix (demande de Charles, 8 octobre 2026) :** sprint 41, 5 points (201 en tout, 41 sprints) ; topologie passe de 30 à 35. À la relecture, Charles attendait que la boucle du tore debout naisse à la première selle et meure à la seconde, comme la boucle du graphe de Reeb. La persistance ordinaire regarde les sous-niveaux {f ≤ h} : entre les selles, c'est un cylindre d'un seul tenant, la seconde selle ajoute une anse, et les deux boucles du tore ne meurent jamais (b1 = 2). Le graphe de Reeb regarde les lignes de niveau {f = h} : deux cercles entre les selles, d'où sa boucle. La persistance étendue (Cohen-Steiner, Edelsbrunner et Harer, 2009) prolonge la filtration par les sur-niveaux et apparie aussi les classes sans fin : sur le tore, une paire de dimension 1 de la selle du bas à celle du haut, celle de la boucle du graphe de Reeb.
**Pourquoi :** l'intuition de Charles est celle d'un lecteur qui connaît le graphe de Reeb ; la page doit montrer que les deux figures disent la même chose une fois la persistance étendue.
**Alternatives :** une note seulement, sous le diagramme (proposée, plus petite) : Charles a choisi le calcul.
**Limites :** le calcul double la filtration (cône sur le maillage) ; même plafond de triangles que la persistance ordinaire dans le navigateur.
**Après relecture (8 octobre 2026) :** Charles demandait un diagramme principal sans infini, pas une figure de plus. Le code-barres, le nuage et le tableau ferment donc chaque classe sans fin à sa mort en redescendant, et la figure étendue à part est retirée ; les paires relatives restent calculées (API C, tests de dualité) mais ne sont plus dessinées, car elles ne font que refléter les ordinaires sur une surface fermée. Alternative écartée : garder l'infini et une figure à part (première version du sprint 41), refusée par Charles.

## D51. Comptage d'arêtes de lib-c par tri radix
**Choix (décision de Charles, 8 octobre 2026) :** sprint 42, 2 points (203 en tout, 42 sprints) ; lib-c passe de 8 à 10. Le goulot trouvé au sprint 10 par les mesures du projet SQL (noté dans `projects/sql/REVIEW.md`, en attente depuis) : `qsort` sur trois clés de 64 bits par triangle, un appel indirect par comparaison, coûte deux fois la lecture du fichier. Remplacé par un tri par base (LSD, chiffres de 11 bits) sur des clés compactées (petit sommet × nombre de sommets + grand sommet), qui ne fait que les passes utiles.
**Pourquoi :** linéaire et sans appel indirect ; le comptage se fait toujours sur des clés triées, donc mêmes résultats, faciles à comparer à l'ancienne version.
**Alternatives :** une table de hachage (linéaire aussi, mais accès aléatoires et taille à prévoir, plus de code à tester) ; garder `qsort` (le plus simple, mais lib-c restait plus lente que topologie, qui fait pourtant plus).
**Limites :** un tampon de la taille des clés en plus (24 octets par triangle pendant le comptage).

## D52. Repère d'orientation dans la visionneuse
**Choix (demande de Charles, 8 octobre 2026) :** sprint 43, 1 point (204 en tout, 43 sprints) ; topologie passe de 35 à 36. Un repère x rouge, y vert, z bleu (la convention des logiciels 3D), dessiné par three.js dans un coin du même canvas, dans une petite vue qui reprend l'orientation de la caméra (comme ParaView ou Blender). Les lettres accompagnent les couleurs, qui viennent de jetons de la charte.
**Pourquoi :** la hauteur se choisit selon x, y ou z ; sans repère, le visiteur ne sait pas lequel est lequel une fois le maillage tourné.
**Alternatives :** un `AxesHelper` au centre du maillage (caché par lui, et à l'échelle du maillage) ; un repère en HTML par-dessus le canvas (une projection à recalculer à chaque image, en double de three.js).
**Limites :** sans WebGL (Firefox en CI), pas de repère, comme pas de maillage.

## D53. Chaque projet expliqué sans jargon
**Choix (demande de Charles, 8 octobre 2026) :** sprint 44, 2 points (206 en tout, 44 sprints) ; la vitrine passe de 9 à 11. En haut de chaque fiche, sous le titre, un encart `<details>` « En bref, sans jargon », fermé par défaut : trois phrases (ce que c'est, à quoi ça sert, ce que ça montre des compétences de Charles), rangées dans `data/projects.json` (champ `plain`), en français et en anglais.
**Pourquoi :** une personne des ressources humaines lit d'abord la fiche ; le résumé technique (`pitch`) parle aux informaticiens. `<details>` s'ouvre au clic, au clavier et au doigt sans JavaScript, se lit par les lecteurs d'écran et ne cache rien sur mobile.
**Alternatives :** une fenêtre modale (bouton et `<dialog>`) : plus voyante, mais il faut du JavaScript, gérer le focus et la fermeture sur téléphone, pour le même contenu ; un encart toujours ouvert : il repousse la démonstration pour les visiteurs techniques.
**Limites :** un test refuse une liste de mots techniques dans les résumés ; il ne garantit pas qu'un texte soit clair, seulement qu'il évite le jargon le plus courant.


## D54. Un RPG tactique à la manière de Dofus
**Choix (décision de Charles, 9 octobre 2026) :** un 22e projet, 26 points sur six sprints (S45 à S50, 232 points en tout, 50 sprints). Un RPG tactique au tour par tour à télécharger, dans l'esprit de Dofus et de Wakfu, écrit de zéro. Client Godot 4 en C# ; règles (grille isométrique, points d'action et de mouvement, ligne de vue, sorts, IA) dans une bibliothèque C# déterministe partagée avec le serveur ; serveur ASP.NET Core de comptes et de personnages (JWT, EF Core, PostgreSQL) ; launcher en Rust (Tauri) qui ne télécharge que les fichiers modifiés ; exécutables Windows, macOS et Linux. Sprites libres de Kenney (CC0). Solo d'abord : le compte et les personnages vivent sur le serveur, le combat se joue en local ; le serveur pourra rejouer un combat pour le vérifier, base du multijoueur.
**Pourquoi :** Charles voulait d'abord une rétro-ingénierie de Dofus ou de Wakfu. Écartée sur recommandation de Claude : les conditions d'utilisation d'Ankama interdisent la décompilation et la modification du client, les sprites leur appartiennent, et les publier sur un site public serait une contrefaçon ; la règle 3 demande aussi de tout écrire de zéro. Le même jeu, écrit de zéro avec des sprites libres, montre les mêmes compétences sans ce risque. Charles a aussi annoncé des extensions (caractéristiques, monstres, paysages, décor, multijoueur) : les sorts, monstres, cartes et PNJ sont décrits en données, pour en ajouter sans toucher au moteur. Godot en C# plutôt qu'en GDScript, car la démo web n'est pas exigée (Charles : des captures, une vidéo ou un texte suffisent) : les règles s'écrivent une fois pour le client et le serveur, typées et testées par xUnit sans le moteur.
**Alternatives :** Unity (le moteur de Dofus 3, meilleur signal pour un studio, mais licence à activer en CI, pas d'éditeur dans le WSL, scènes difficiles à relire) ; Rust et Bevy (une seule langue, démo web possible, mais pas d'éditeur pour un jeu riche en contenu) ; Godot en GDScript (démo web possible, mais règles à écrire deux fois avec le serveur) ; TypeScript et Phaser (rapide, rien de nouveau) ; launcher Avalonia (tout en C#, mais Rust apporte une langue de plus au portfolio, sur un programme où elle est à sa place) ; multijoueur dès le début (serveur temps réel, synchronisation, déconnexions : plusieurs sprints avant la ville d'accueil).
**Limites :** même pile que le roguelike, ce qui est voulu (outillage rodé : image Godot, exports trois systèmes, API JWT) mais apporte moins de nouveauté ; exécutables non signés (avertissement de Windows et de macOS, documenté) ; avec des sprites libres, le jeu pèsera quelques centaines de Mo au plus, pas plusieurs Go ; les packs Kenney n'ont pas tous des personnages isométriques en quatre directions : le choix entre sprites 2D et petits modèles 3D vus en caméra isométrique se fera au début du sprint 46, sur pièces.
**Sprint 46 :** comparés sur une même scène, Charles a choisi les modèles 3D vus en caméra isométrique (`projects/rpg/DECISIONS.md`, T7).

## D55. Osmose : le launcher, le serveur et la création refaits (9 octobre 2026)
**Décision de Charles**, après avoir essayé le launcher et le jeu sous Windows et répondu à un questionnaire : refaire d'abord l'entrée dans le jeu, dans cet ordre, en trois sprints (S51 à S53, 9 points, 241 en tout) : le launcher (connexion seule qui met à jour et lance, mémoriser nom et mot de passe, inscription, mise à jour automatique, serveur caché, présentation), le choix du serveur et la liste visuelle des personnages dans le jeu (comme Dofus), puis un écran de création à part (couleurs, carrure, pack libre de pièces détachables choisi sur captures). Les classes (quatre éléments à la Dofus, avec des noms et des chiffres à nous ; effets ; sorts par niveau), la progression (expérience, caractéristiques, niveaux de sort, équipement) et le monde (cartes reliées, caméra) viendront après un nouveau questionnaire. Rythme : sprints enchaînés, relecture à la fin.
**Alternatives écartées :** commencer par le combat ou la progression (Charles préfère une entrée dans le jeu propre d'abord) ; choisir le personnage dans le launcher (Charles : dans le jeu, comme Dofus).

## D56. Osmose : éléments, classes, progression et monde (9 octobre 2026)
**Décision de Charles**, après un second questionnaire : huit sprints (S54 à S61, 24 points, 265 en tout), dans l'ordre classes, progression, monde. Combat : un élément par caractéristique (Terre/Force, Feu/Intelligence, Eau/Chance, Air/Agilité), une vingtaine de sorts par classe, effets de soin, bouclier, poussée/attirance, durée, zones et invocations ; d'abord les infos au survol, la frise de l'ordre de jeu et les effets visuels. Progression : 10 points de caractéristique par niveau (Vitalité, Force, Intelligence, Chance, Agilité ; pas de Sagesse), points de sort et rangs, XP des combats, des quêtes et bonus de groupe, objets et panoplies. Monde : de grandes zones, une caméra qui suit le joueur, des groupes de monstres visibles. Rythme : sprints enchaînés, relecture à la fin.
**Alternatives écartées :** une dizaine de sorts par classe (Charles en veut une vingtaine) ; des cartes d'un écran reliées par leurs bords (Charles préfère de grandes zones) ; des combats aléatoires.
