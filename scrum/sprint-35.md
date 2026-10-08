# Sprint 35 : finitions

**Objectif :** finitions demandées par Charles après le sprint 34 : une interface pour le carrefour en Ada, dont la logique reste celle du programme Ada, le rôle de chaque technologie sur chaque fiche, et une gestion de projet qui dit que le portfolio est livré.

**Goal:** finishing touches Charles asked for after sprint 34: an interface for the Ada crossroads whose logic stays the Ada program's, what each technology does on every project page, and project management pages that say the portfolio is delivered.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je fais fonctionner le carrefour sur la fiche (ada) : le programme Ada calcule tout l'automate du contrôleur (états atteignables, effet d'une seconde et d'une demande sur chaque axe) en JSON commité et vérifié par la CI ; la fiche le rejoue en direct (carrefour animé, demandes de passage, pause, vitesse, chronogramme), FR/EN, accessible au clavier et sur mobile ; AUnit (automate sûr), Node (le rejeu redonne la simulation du programme) et Playwright. | 3 | Fait |
| En tant que recruteur, je sais à quoi sert chaque technologie de chaque projet (vitrine) : sous la liste des technologies de chaque fiche, une ligne par technologie qui dit ce qu'elle fait dans le projet, en français et en anglais ; un test exige un rôle pour chaque technologie. | 1 | Fait |
| En tant que recruteur, je vois une gestion de projet à jour (vitrine) : tous les projets terminés, backlog vide, bilan de chaque risque du registre, plan et feuille de route au sprint 35 ; tests. | 1 | Fait |

**Tests :** 1 test AUnit de plus (8 : l'automate est fermé et sûr, jamais deux axes ouverts) et la preuve SPARK inchangée (15 vérifications) ; job CI qui recalcule l'automate et la simulation et les compare aux fichiers commités ; 5 tests Node du rejeu (crossroads-core.js redonne la simulation du programme Ada seconde par seconde, fichier invalide refusé) ; 4 tests Node de la vitrine (un rôle par technologie dans les deux langues, affiché sur chaque fiche ; bilan de chaque risque ; backlog vide) ; 15 tests Playwright (5 navigateurs : feux, demande de passage au clavier pas à pas, accessibilité, pas de défilement horizontal). **Trouvé en route :** la voiture en attente toujours visible puis jamais (`hidden` ne s'applique pas aux éléments SVG, ni comme règle du navigateur ni comme propriété JavaScript) ; deux tests du backlog qui confondaient deux tableaux.

## Rétro (Charles)
- Ce qui a marché : L'interface du carrefour rejoue exactement l'automate calculé par Ada ; chaque technologie a son rôle sur chaque fiche.
- Ce que l'IA a mal fait : `hidden` sans effet sur les éléments SVG ; deux tests qui confondaient deux tableaux.
- À changer au prochain sprint : Ne pas compter sur `hidden` hors du HTML.
