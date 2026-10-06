# Décisions, ml

## M1. Données synthétiques générées par le code
**Choix :** six classes de formes paramétriques (surfaces de révolution fermées, sphère projetée sur une boîte, tore), avec taille, proportions, résolution, rotation et bruit tirés au hasard ; nuages de 512 points tirés selon l'aire.
**Pourquoi :** règle de confidentialité du portfolio (aucune donnée du laboratoire) ; la vérité terrain est connue et la difficulté se règle (la gélule ressemble au cylindre et à la sphère).
**Limite :** des formes simples ; un jeu public (ModelNet, sous licence de recherche) serait un prolongement, avec sa licence vérifiée.

## M2. DVC sans stockage distant
**Choix :** DVC décrit le pipeline et versionne les empreintes (`dvc.lock`) ; les données restent dans le cache local et la CI les régénère depuis la graine.
**Pourquoi :** les données se reconstruisent en quelques secondes ; un stockage distant (S3, Drive) demanderait des secrets et un compte pour un gain nul ici.
**Limite :** si un jour les données ne sont plus régénérables (vrais maillages), il faudra un remote DVC.
