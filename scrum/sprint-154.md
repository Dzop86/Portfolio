# Sprint 154 : Osmose : le bestiaire

**Objectif :** la suite d'Osmose après S151, conçue par le game designer (v8 de `projects/rpg/docs/suite-apres-d69.md`, §12) et prise avec ses recommandations (D83).

**Goal:** Osmose after S151, designed by the game designer (v8 of `projects/rpg/docs/suite-apres-d69.md`, §12) and taken with its recommendations (D83).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'ouvre le Bestiaire (panneau Menus) : chaque monstre en silhouette jusqu'au combat, puis ses PV, résistances et faiblesse, ses sorts et sa leçon, le butin vu tomber, selon mes victoires comptées par le serveur (rpg) ; xUnit, intégration à deux clients, auto-test. | 3 | Fait (playtest à faire par Charles) |

## Fait
- Le serveur compte les victoires de chaque personnage sur chaque monstre, seul ou à plusieurs (T121) ; `admin … --bestiary full` pour Plouf.
- La fenêtre Bestiaire (touche B, panneau Menus) : les familles par zone, chaque monstre en silhouette, puis rôle, niveau et zone ; PV, résistances et faiblesse ; sorts et leçon ; butin en mots.
- xUnit (paliers, faiblesse, rareté, une carte par monstre), intégration PostgreSQL (les sangliers battus comptés dans le résumé).
- Reporté : le bestiaire depuis la fiche d'objet et par clic droit sur un monstre ; le butin « vu tomber » (aujourd'hui d'après les données).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
