# Sprint 63 : un schéma d'architecture sur chaque fiche

**Objectif :** demande de Charles (D57) : sur chaque fiche, un schéma « Comment les morceaux tiennent ensemble », comme celui d'Osmose, quand le projet a au moins trois morceaux qui se parlent.

**Goal:** Charles's request (D57): on each project page, a "How the pieces fit together" diagram, like Osmose's, when the project has at least three pieces that talk to each other.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je vois d'un coup d'œil comment chaque projet est construit (vitrine) : le schéma d'Osmose décrit en données et dessiné en SVG par le générateur ; un schéma pour chaque fiche qui a au moins trois morceaux (boîtes avec leur technologie, flèches nommées), en français et en anglais ; tests Node (traductions, flèches vers des boîtes connues, textes qui tiennent dans leur boîte) ; Playwright (375 px sans défilement horizontal, deux thèmes, axe) ; les fiches sans schéma listées ici avec la raison. | 5 | Fait |

**Résultat :** les 23 fiches ont leur schéma, décrit dans `data/architecture.json` et dessiné par `src/arch.mjs` (D60) : celui d'Osmose y est passé tel quel, les 22 autres sont nouveaux, de 5 à 7 boîtes chacun ; sous chaque dessin, la même chose en mots. Aucune fiche sans schéma : même les petits jeux ont trois morceaux qui se parlent (règles, IA ou terminal, page web).

**Tests :** 7 tests Node (chaque projet a son schéma, aucun défaut ; les vérifications attrapent chaque erreur ; géométrie des flèches et des étiquettes ; section dans les deux langues sur chaque fiche) ; Playwright sur les 23 fiches (axe, 375 px, deux thèmes, 5 navigateurs) ; 5 mutations, 5 attrapées. **Trouvé en route :** des étiquettes posées sur leur trait ou coupées au bord, trois schémas aux flèches croisées, refaits ; les cases du morpion sans nom tant que le plateau n'est pas à l'écran.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
