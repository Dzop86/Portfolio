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
