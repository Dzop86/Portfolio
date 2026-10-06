# Relecture humaine, naval (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `model/Board.java` : règles de placement, tirs.
- [ ] `model/Computer.java` : chasse et cible.
- [ ] Le jeu ressemble-t-il à celui de mes études (règle de non-contact, taille de la flotte) ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `model/Board.java` | (Claude) Détecté par un test : un navire qui en chevauchait un autre était signalé comme « touche un autre navire », la première anomalie trouvée en parcourant le voisinage | Chevauchement vérifié d'abord sur toutes les cases, contact ensuite |
| 2026-10-06 | `ui/ViewTest.java` | (Claude) Les captures échouaient dans l'image Maven : JavaFX a besoin des bibliothèques de polices du système pour dessiner le texte | Captures faites dans un conteneur avec Pango et FreeType ; les tests de l'interface, sans rendu de texte, tournent partout |
| 2026-10-06 | `ui/ViewTest.java` | (Claude) Signalé par la CI sur les trois systèmes : sans `-Dnaval.screenshots`, Maven transmettait la chaîne `${naval.screenshots}` et le test tentait d'écrire les captures ; je ne l'avais lancé qu'avec la propriété | Propriété vide par défaut dans le `pom`, test qui ignore une valeur vide ; `mvn verify` vérifié sans la propriété |
| | | | |
