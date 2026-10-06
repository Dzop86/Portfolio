# Décisions, sql

## Q1. Des mesures réelles, vérifiées avant d'être enregistrées
**Choix :** la base contient des durées prises par `bench/run.mjs` sur les WebAssembly commités de lib-c et topologie, et chaque réponse est comparée aux invariants connus avant d'être écrite.
**Pourquoi :** des chiffres inventés auraient rendu les analyses décoratives ; mesurés, ils ont révélé un vrai goulot (le `qsort` de lib-c). La vérification empêche de chronométrer une bibliothèque qui répondrait faux.
**Alternatives :** données générées au hasard (aucune information), mesures natives (trois chaînes de compilation à piloter depuis un script, alors que le WebAssembly tourne partout avec Node).
**Limite :** une machine, une campagne ; le WebAssembly sous Node n'est pas le natif.

## Q2. Un CSV plat normalisé par SQL
**Choix :** le script écrit une ligne par mesure avec tout son contexte ; `schema/04_load.sql` la charge dans une table temporaire et la répartit dans les tables normalisées, en une transaction.
**Pourquoi :** le script n'a pas à connaître les clés de la base ; la normalisation est faite là où les contraintes la vérifient. Le CSV sert aussi la copie SQLite du navigateur.
**Alternatives :** le script insère directement (dépendance à un pilote PostgreSQL dans Node), un fichier par table (clés à gérer à la main).

## Q3. Médiane plutôt que moyenne
**Choix :** les vues comparent des médianes ; moyenne, p90 et écart type restent affichés.
**Pourquoi :** une répétition ralentie par le ramasse-miettes ou un autre processus déplace la moyenne, pas la médiane (test « the median ignores the outlier repetition »).

## Q4. pgTAP dans l'image PostgreSQL, tests annulés
**Choix :** l'image `postgres:17-bookworm` reçoit pgTAP et `pg_prove` ; chaque fichier de test s'exécute dans une transaction annulée à la fin.
**Pourquoi :** les tests tournent sur la base réellement chargée, peuvent y ajouter des données fabriquées (ou 126 000 lignes pour l'index) sans la modifier, et la même image sert en local et en CI.
**Limite :** PostgreSQL tourne sous Linux uniquement en CI (conteneur) ; seul le script de mesure, en Node, passe la matrice à trois systèmes. Le SQL n'est pas compilé, la règle multi-OS ne s'applique pas à lui.

## Q5. Un index justifié par un test de plan
**Choix :** un seul index secondaire, `measurement (file_id, implementation_id, campaign_id)`, avec un test qui lit le plan d'exécution avec et sans lui.
**Pourquoi :** un index sans requête qui le justifie coûte à chaque insertion ; le test garantit qu'il sert, et qu'il servira encore si la requête change.
**Limite :** le test dépend des choix du planificateur ; il ajoute assez de lignes (126 000) pour que la décision ne soit pas serrée.
