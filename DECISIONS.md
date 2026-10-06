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
