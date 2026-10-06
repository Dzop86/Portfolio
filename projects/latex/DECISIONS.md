# Décisions, latex

## X1. Un analyseur écrit à la main, qui ne lève jamais d'exception
**Choix :** descente récursive sur la source, avec une table du nombre d'arguments de chaque commande connue ; toute anomalie devient un diagnostic situé et l'analyse continue.
**Pourquoi :** LaTeX n'a pas de grammaire hors contexte (le nombre d'arguments dépend des définitions) ; dans un éditeur en direct, le document est faux la moitié du temps pendant la frappe, et le rendu doit suivre.
**Alternatives :** compiler du vrai LaTeX (TeX en WebAssembly, plusieurs Mo et lent), une bibliothèque existante (LaTeX.js, moins de contrôle sur les positions et les diagnostics).

## X2. Deux passes, comme deux compilations LaTeX
**Choix :** une première passe numérote sections, équations et bibliographie et enregistre les étiquettes ; la seconde produit le HTML.
**Pourquoi :** `\ref` vers une section plus loin se résout sans écrire de fichier `.aux`.

## X3. TypeScript exécuté directement par Node
**Choix :** sources en TypeScript strict limité à la syntaxe effaçable ; `tsc` vérifie les types, Node 22 et 24 exécutent les tests sur les sources (`--experimental-strip-types`), esbuild empaquette la démo.
**Pourquoi :** pas d'étape de compilation ni de fichiers générés à commiter pour les tests ; le typage reste vérifié en CI.
**Limite :** pas d'`enum` ni de `namespace` (non effaçables).

## X4. Sûreté du HTML produit
**Choix :** échappement de tout texte source, liens limités à http, https et mailto, KaTeX avec `trust: false`.
**Pourquoi :** l'éditeur rend ce que le visiteur tape ; aucun texte ne doit pouvoir devenir du HTML ou un lien `javascript:`.
