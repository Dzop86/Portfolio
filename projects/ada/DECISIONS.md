# Décisions, ada

## A1. L'état est la phase, les couleurs en sont déduites
**Choix :** le contrôleur stocke une phase parmi six (`NS_Green`, `NS_Yellow`, `Red_Before_EW`, `EW_Green`, `EW_Yellow`, `Red_Before_NS`) ; `Light (Phase, Axis)` calcule chaque couleur.
**Pourquoi :** stocker deux couleurs indépendantes rendrait l'état « vert et vert » représentable, donc possible après un bogue ; ici il n'existe pas.
**Alternatives :** deux variables de couleur protégées par des assertions (vérifiées seulement à l'exécution, ou par une preuve plus lourde).

## A2. Alire pour la chaîne de compilation et les dépendances
**Choix :** Alire installe GNAT et gprbuild, et fournit AUnit ; la CI utilise `alire-project/setup-alire` sur les trois systèmes.
**Pourquoi :** même version du compilateur partout, dépendances déclarées dans `alire.toml`.
**Limite :** le champ `maintainers` d'Alire exige un e-mail ; il est omis (le dépôt n'en contient aucun), ce qui empêche seulement de publier la crate dans l'index Alire.
