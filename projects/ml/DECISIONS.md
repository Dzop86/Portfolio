# Décisions, ml

## M1. Données synthétiques générées par le code
**Choix :** six classes de formes paramétriques (surfaces de révolution fermées, sphère projetée sur une boîte, tore), avec taille, proportions, résolution, rotation et bruit tirés au hasard ; nuages de 512 points tirés selon l'aire.
**Pourquoi :** règle de confidentialité du portfolio (aucune donnée du laboratoire) ; la vérité terrain est connue et la difficulté se règle (la gélule ressemble au cylindre et à la sphère).
**Limite :** des formes simples ; un jeu public (ModelNet, sous licence de recherche) serait un prolongement, avec sa licence vérifiée.

## M2. DVC sans stockage distant
**Choix :** DVC décrit le pipeline et versionne les empreintes (`dvc.lock`) ; les données restent dans le cache local et la CI les régénère depuis la graine.
**Pourquoi :** les données se reconstruisent en quelques secondes ; un stockage distant (S3, Drive) demanderait des secrets et un compte pour un gain nul ici.
**Limite :** si un jour les données ne sont plus régénérables (vrais maillages), il faudra un remote DVC.

## M3. Une référence classique face au réseau
**Choix :** chaque entraînement compare un boosting sur descripteurs invariants (D2, distances au centre, valeurs propres) et un PointNet ; le seuil de la CI porte sur le meilleur des deux.
**Pourquoi :** un réseau ne se juge qu'à côté d'une référence simple. Ici la référence gagne (97,2 % contre 95,2 %) : c'est un résultat, pas un échec.
**Limite :** les poids ne sont pas reproductibles au bit près d'une machine à l'autre ; la CI vérifie l'empreinte des données et le seuil, pas celle des modèles.

## M4. MLflow en base SQLite locale
**Choix :** suivi des expériences dans `mlflow.db` (SQLite), hors de git, joint comme artefact à chaque exécution de la CI.
**Alternatives :** serveur MLflow hébergé (compte et secrets), stockage fichiers (déconseillé par MLflow 3).
