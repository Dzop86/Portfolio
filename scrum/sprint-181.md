# Sprint 181 : Osmose : l'inventaire trié, le familier qui suit

**Objectif :** les retours de Charles sur la 1.0.176 (1er novembre), en gains rapides avant les pages Éléments et Spécialisation : un inventaire qu'on lit et qu'on trie, un objet choisi depuis son emplacement, un familier qu'on voit (D92).

**Goal:** Charles's feedback on 1.0.176 (1 November), quick wins before the Elements and Specialisation pages: an inventory one can read and sort, an item picked from its slot, a pet one can see (D92).

| Story | Points | État |
|---|---|---|
| En tant que joueur, les onglets de l'inventaire (Équipement, Consommables…) se lisent en entier, et je trie l'équipement par panoplie, par niveau, par emplacement, par rareté ou par nom (rpg) ; xUnit sur le tri, auto-test du client. | 2 | Fait (T141) |
| En tant que joueur, un clic sur un emplacement de mon personnage, vide ou occupé, me propose les objets de mon inventaire qui vont dans cet emplacement (rpg) ; xUnit, auto-test du client. | 1 | Fait (T141) |
| En tant que joueur, mon familier équipé me suit en courant à côté de moi, à pied comme sur ma monture (rpg) ; xUnit sur la place du suiveur, auto-test du client. | 2 | Fait (T141) |

## Ce qui est fait
- Onglets de l'inventaire lisibles en entier, menu « Trier par » (type, panoplie, niveau, emplacement, nom ; pas de tri par rareté : les objets n'en ont pas dans les données, T141) ; clic sur un emplacement : les objets qui y vont, « Retirer » s'il est occupé ; le chaton suit en courant à côté du héros, à pied et monté.
- xUnit (`InventoryTests`, `PetsTests`), auto-tests du client (onglets mesurés, tri, choix par emplacement, familier) verts en français et en anglais ; deux défauts trouvés en route corrigés (mini-carte sur l'inventaire, journal trop large en français).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
