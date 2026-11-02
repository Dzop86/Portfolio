# Sprint 187 : Osmose : les composés consomment leurs parents

**Objectif :** les composés consomment leurs parents, sur la décision de Charles du 2 novembre 2026 (D94).

**Goal:** compounds consume their parents, on Charles's decision of 2 November 2026 (D94).

| Story | Points | État |
|---|---|---|
| En tant que joueur, créer la Foudre retire les sorts de Feu et d'Air de mon grimoire et rend ces deux éléments indisponibles pour une autre fusion, jusqu'à ce que je la dissolve : 2 paires disjointes ou 1 trio au plus (rpg) ; xUnit : grimoire de 24 sorts avec 2 paires, fusion en conflit refusée par le serveur et par la page, dissoudre rend les sorts, combat rejoué à l'identique. | 2 | Fait (T147) |
| En tant que joueur, un sort d'un composé pose son empreinte, et le suivant du même composé déclenche sa réaction ; les réactions entre éléments libres restent (rpg) ; xUnit par paire et par trio, enregistrement en version 6, anciens enregistrements rejoués à l'identique. | 1 | Fait (T147) |
| En tant que joueur déjà sur le serveur, mes composés devenus illégaux sont ramenés à un état légal, mon deck rendu valide et le changement annoncé une fois ; `admin` et Plouf suivent (rpg) ; intégration PostgreSQL : Plop et ses 10 composés, Plif et sa Foudre. | 1 | Fait (T147) |
| En tant que Charles, les plafonds de T41, les hordes et la Disjointe sont remesurés avec les builds découpés de nouveau (rpg) ; mesure hors CI dans `docs/mesures-arbre.md`. | 1 | Mesuré : plafond dépassé, à trancher par Charles |

**Ce qui est fait.** La règle de D94 : créer un composé retire les sorts de ses parents et bloque les fusions qui les utilisent. Le deck suit sort pour sort, la page affiche l'état Bloqué et la migration garde ce qui tient. Un composé déclenche sa réaction sur sa propre empreinte.

**Ce qui a bougé.** La mesure a dépassé le plafond : la limite d'une réaction par lanceur et par tour, écrite d'avance dans la spec, est appliquée, et Wave y échappe. Les jumeaux ne sont plus des aperçus. La remesure avec cette limite ne bouge presque pas (`docs/mesures-arbre.md`, v4) : les dépassements viennent des composés sans Terre, pas des réactions. **À trancher par Charles :** garder ou non cette limite.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
