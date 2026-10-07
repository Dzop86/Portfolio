# Décisions, angular

## A1. Le code sans framework est partagé avec le React, pas recopié
**Choix :** les types de l'API, les calculs des vues, les échelles des graphiques, les textes et les styles viennent de `projects/react/src`, importés tels quels.
**Pourquoi :** la comparaison porte alors sur ce qui diffère vraiment (état, données, navigation, tests) ; une correction (comme `makeT`, qui plantait sur une clé absente) profite aux deux.
**Limite :** le dashboard Angular dépend du dossier du React ; l'étape Docker copie ce dossier et un test vérifie qu'elle copie tout ce qui est importé.

## A2. Signaux et httpResource, sans zone.js
**Choix :** composants autonomes en `OnPush`, état en `signal`, `computed`, `linkedSignal` (le choix d'un menu suit les données à leur arrivée, puis le visiteur), un `httpResource` par fichier de l'API.
**Pourquoi :** c'est l'Angular actuel (zoneless par défaut depuis la version 21) ; chaque vue charge ce dont elle a besoin, avec chargement, erreur et rechargement fournis.
**Limite :** une page fait trois requêtes là où le React en fait une par fichier au démarrage pour toutes ses vues ; sur une API statique, l'écart est négligeable.

## A3. Routeur à fragment
**Choix :** `withHashLocation()`, URL `?lang=fr#/results` ; pas de `<base href>`.
**Pourquoi :** GitHub Pages ne réécrit pas les chemins vers `index.html`. Avec `<base href="./">`, la navigation résolvait `#/results` par rapport à la base et perdait `?lang=fr` (vu en vérifiant l'adresse) : la langue changeait au rechargement.

## A4. Jest et Cypress, comme annoncé
**Choix :** Jest avec jest-preset-angular (l'outil par défaut d'Angular est maintenant Vitest), Cypress pour le bout en bout, avec une commande axe écrite ici.
**Pourquoi :** le plan annonçait cette pile, courante dans les équipes Angular ; le dashboard React montre déjà Vitest et Playwright.
**Limite :** cypress-axe n'accepte pas Cypress 16 : la commande `cy.checkA11y` injecte axe-core elle-même.
