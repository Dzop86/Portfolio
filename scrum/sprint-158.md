# Sprint 158 : Osmose : la fenêtre des succès

**Objectif :** une refonte graphique de l'interface d'Osmose demandée par Charles après S156, glissée avant le socle 45-60 (D84).

**Goal:** a graphic overhaul of Osmose's interface asked for by Charles after S156, slipped in before the 45-60 groundwork (D84).

| Story | Points | État |
|---|---|---|
| En tant que joueur, la fenêtre des succès a ses catégories et sous-catégories à gauche avec leur pourcentage, une progression générale, le total de mes points dans un bandeau et des tuiles illustrées par catégorie (rendus de nos scènes) (rpg) ; xUnit, auto-test de la fenêtre. | 3 | Fait (bannières de repli dessinées ; rendus de scènes à venir) |
| En tant que joueur, je cherche un succès, je masque ceux déverrouillés, et un succès montre ses objectifs cochés et ses récompenses (rpg) ; xUnit, auto-test. | 2 | Fait |

## Fait
- Maquette du graphiste (`projects/rpg/docs/graphisme/s158-succes.md`).
- Sous-catégories des succès (`group` dans `achievements.json`) : les pierres, les familles, les donjons par tranche de niveaux, la maîtrise et la polyvalence des métiers, l'histoire et les contrats (T125).
- La fenêtre refaite : total en bandeau doré, arbre dépliable avec les pourcentages, recherche sans accents, tuiles de catégorie avec bannière, cartes de succès (doré si obtenu, gris sinon), détail (objectif, récompenses, titre à porter ou ôter), case « Masquer les succès déverrouillés », titre porté en pied.
- `ui_art.py` : icône Donjons, variantes grises, bannières de repli, cadres d'icône, cases à cocher du thème.
- xUnit (parts, groupes nommés dans les deux langues, recherche, masquage, milliers, icônes et bannières), auto-test du village (vue générale, groupe des donjons, masquage, recherche) : verts.

- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
