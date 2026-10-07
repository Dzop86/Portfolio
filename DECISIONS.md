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
**Choix :** le projet parallele (`projects/parallele`) calcule la courbure de Gauss de topologie en deux passes indépendantes (par face, puis par sommet sur un voisinage compressé rangé dans l'ordre des faces), en C++ séquentiel, OpenMP et OpenCL (double précision), CUDA au sprint 29. Les résultats séquentiels et OpenMP sont identiques au bit à ceux de topologie ; OpenCL est à 1e-12. La CI exécute OpenCL sur le processeur (PoCL) sous Linux.
**Limite :** pas de carte graphique en CI ; sous WSL, OpenCL n'atteint pas la carte NVIDIA (CUDA si). Les benchmarks et la fiche du projet viennent au sprint 29.
