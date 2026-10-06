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
