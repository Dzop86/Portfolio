# Sprint 190 : Osmose : l'arbre des spécialisations

**Objectif :** l'onglet Talents devient Spécialisation, un arbre lu de gauche à droite qui se divise : 3 choix à 30, 2 choix pour chacun à 50, une dernière évolution à 100 (D95).

**Goal:** the Talents tab becomes Specialisation, a tree read from left to right that divides: 3 choices at 30, 2 choices each at 50, a last evolution at 100 (D95).

| Story | Points | État |
|---|---|---|
| En tant que Charles, les 6 évolutions à 50 et leurs 6 évolutions à 100 de chaque classe sont conçues par le game designer et relues par le scénariste, et je valide la liste (rpg) ; document `docs/elements-specialisations.md`, v4. | 1 | Fait (`docs/elements-specialisations.md` v4, `docs/textes-specialisations.md`) |
| En tant que joueur, je choisis à 50 une des 2 évolutions de ma branche, et la suivante vient seule à 100 ; un héros de 60 qui n'a jamais choisi passe par ses deux choix (rpg) ; xUnit : choix refusé avant 50 ou hors de sa branche, évolution III de la bonne ligne, combat rejoué à l'identique ; intégration PostgreSQL : choisir, changer et payer, rangs transférés. | 4 | Fait (T149) |

**Ce qui est fait.** La conception (v4) validée par Charles, les noms du scénariste, et le socle : lignées, prix, règles vivantes, `PatchRule`, limites des passifs, `admin --spec branche:lignée`.

**Ce qui a bougé.** Le game designer a montré qu'aucune évolution n'était jouable : les 45 sont à écrire de S191 à S193, dans les 21 points prévus mais sans marge. Le mot « lignée » reste hors de l'écran.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
