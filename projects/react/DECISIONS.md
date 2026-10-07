# Décisions, react

## R1. Graphiques en SVG écrits à la main plutôt qu'une bibliothèque
**Choix :** deux composants (barres, courbes) et des échelles en fonctions pures (`src/charts/`).
**Pourquoi :** les graphiques de la vitrine sont déjà en SVG à la main, avec la même charte ; une bibliothèque (Recharts, Chart.js) aurait ajouté 100 à 200 ko pour deux formes, et des couleurs à surcharger une à une. Les échelles se testent sans navigateur.
**Limite :** pas d'animation ni de zoom ; chaque graphique est doublé d'un tableau pour la lecture exacte et les lecteurs d'écran.

## R2. Pas de routeur ni de gestionnaire d'état
**Choix :** la vue vient du fragment d'URL (`#results`), l'état des filtres reste local aux vues, l'API est chargée une fois par `useApi`.
**Pourquoi :** trois vues et des données en lecture seule : React Router ou un store n'apporteraient que des dépendances. Le fragment garde des URL partageables et marche sur GitHub Pages sans réécriture côté serveur.
**Limite :** à revoir si le dashboard gagne des vues imbriquées.

## R3. Les tests d'intégration rendent le dashboard sur la vraie API
**Choix :** les tests appellent `buildApi` du site et servent son résultat par un faux `fetch`, au lieu de données inventées.
**Pourquoi :** un champ renommé dans `src/api.mjs` casse alors les tests du dashboard, pas la page en ligne ; les tests vérifient aussi des faits sur les vraies données (cinq versions mesurées, cinq tailles de maillage).
**Limite :** ces tests dépendent des données du dépôt ; les cas limites (série plate, fichier en erreur) ont leurs propres tests.

## R4. Le sprint en cours n'est pas tracé comme un sprint qui ne livre rien
**Choix :** dans la vélocité, les points livrés ne couvrent que les sprints terminés ; le tableau marque le sprint ouvert « en cours ».
**Pourquoi :** vu sur les captures : la courbe plongeait à 0 au dernier sprint, comme une panne, alors qu'il venait de commencer.

## R5. three.js directement, pas react-three-fiber
**Choix :** la visionneuse crée sa scène three.js dans un `useEffect` et la libère au démontage (rendu, contrôles, géométrie, matériau, observateur) ; React ne tient que ce qui est autour (mesh lu, invariants, erreur, infobulle).
**Pourquoi :** une seule forme, sans scène déclarative à décrire ; react-three-fiber ajouterait une dépendance et un moteur de rendu React de plus pour un seul objet ; la vitrine fait la même scène à la main, et le cycle de vie (créer, mettre à jour, libérer) est ce qu'il faut savoir maîtriser.
**Limite :** une scène plus riche (plusieurs objets, sélection, animations) gagnerait à passer à react-three-fiber.

## R6. Le WebAssembly de topologie est celui du site, pas une copie
**Choix :** la visionneuse importe `topo-api.js` de la vitrine (compilé dans le dashboard) et charge à l'exécution le module `assets/wasm/topo.js` que le site publie, à l'adresse donnée par l'API (`meshes.json`).
**Pourquoi :** une seule version du code C++ compilé et de sa lecture : un correctif de topologie arrive dans la vitrine et le dashboard en même temps ; le dashboard n'embarque pas une seconde copie du module (100 ko) déjà servi.
**Limite :** le dashboard dépend des chemins du site ; l'API les publie, et un test vérifie qu'ils existent dans le site construit.
