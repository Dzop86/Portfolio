# Sprint 191 : Osmose : la page Spécialisation, la Sentinelle

**Objectif :** l'onglet Talents devient Spécialisation, un arbre lu de gauche à droite qui se divise : 3 choix à 30, 2 choix pour chacun à 50, une dernière évolution à 100 (D95).

**Goal:** the Talents tab becomes Specialisation, a tree read from left to right that divides: 3 choices at 30, 2 choices each at 50, a last evolution at 100 (D95).

| Story | Points | État |
|---|---|---|
| En tant que joueur, l'onglet « Spécialisation » remplace l'onglet Talents : la racine de ma classe, 3 branches, 6 évolutions, lues de gauche à droite en colonnes 30, 50 et 100, la fiche de l'évolution et la frise commune ; l'arbre des talents reste sur la touche T ; textes FR et EN (rpg) ; xUnit client (aucun chevauchement, nœuds de 44 px), auto-test Godot : choisir une branche à 30 puis une évolution à 50. | 3 | Fait |
| En tant que Sentinelle, je deviens Mire, Affût ou Sente, puis une de leurs 2 évolutions, avec leurs sorts, leurs passifs et leurs titres (rpg) ; xUnit par passif, règle et évolution, ±8 points de la moyenne contre les hordes à 35, 45 et 50. | 4 | Fait, sauf le seuil : ±13 tenus, ±8 non (T150) |

**Ce qui est fait.** La page Spécialisation, sa confirmation et son branchement sur le serveur ; les briques E à I ; les 12 sorts, les passifs et les règles de la Sentinelle ; les 27 icônes de l'arbre. Garde et Mage lisent leur arbre, avec un bouton « Bientôt ».

**Ce qui n'est pas tenu.** Le seuil de ±8 : la Mire à 35 et la Herse à 50 sont à +11 et +12 après les leviers écrits, et cinq voies sur six jouent sous le héros sans voie, en partie à cause de l'IA. À reprendre avec la brique S en S193 (T150).

**Ce qui a bougé.** La Herse a perdu son passif à 2 cases (levier 3). La mesure joue les groupes de 4 et 5 : contre les hordes solides, elle était à 0 point partout et ne prouvait rien.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
