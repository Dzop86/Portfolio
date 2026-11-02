# Sprint 184 : Osmose : les cinq paires d'éléments

**Objectif :** les pages Éléments et Voie à la place de la page Talents, conçues par le game designer (`projects/rpg/docs/elements-specialisations.md`, v3) et validées par Charles : trois paliers à 30, 50 et 100 (D93).

**Goal:** the Elements and Path pages in place of the Talents page, designed by the game designer (`projects/rpg/docs/elements-specialisations.md`, v3) and approved by Charles: three tiers at 30, 50 and 100 (D93).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée la Glace, la Boue, le Magma, la Vapeur et le Sable : 120 sorts jumeaux avec leurs icônes et leurs signatures (−PA, −PM, −résistance, −dégâts, −PO), noms à relire par le scénariste (rpg) ; xUnit : budget × 0,8, signatures appliquées, plus haute caractéristique des parents, chaque paire à 1,5 victoire sur 30 de la moyenne des autres (retirée de `TemplateTests` en S182, faute de sorts à mesurer). | 4 | Fait (T144) |

**Ce qui est fait.** La Glace, la Boue, le Magma, la Vapeur et le Sable : 24 sorts jumeaux chacun (8 par classe), au budget × 0,8, qui frappent sur la plus haute caractéristique de leurs parents. Leur signature est un état posé sur les ennemis touchés : −1 PA, −1 PM, −8 % de résistance, −10 % de dégâts, −1 PO, pendant 1 tour. La page Éléments les propose dès 30 ; seuls les trios restent « À venir ». 360 icônes en relief, leurs couleurs dans les tokens, et l'égalité des paires à 45 de retour dans `TemplateTests`.

**Ce qui a bougé.** Les noms sont des noms de travail, formés d'un geste et d'une matière (« Carreau de glace », « Ice Bolt ») : 240 noms que le scénariste n'a pas relus, comme le plan le prévoyait (« rien ne bloque un sprint »). Les jumeaux reprennent la silhouette de leur sort d'origine, dans les couleurs de leur composé, au lieu des 24 silhouettes propres de la Foudre : à revoir avec le graphiste si elles se confondent en jeu. Un sort qui retire déjà la même chose garde son effet, plus fort, sans s'ajouter à la signature (Entrave de boue reste à −2 PM).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
