# Sprint 182 : Osmose : le cœur des éléments

**Objectif :** les pages Éléments et Voie à la place de la page Talents, conçues par le game designer (`projects/rpg/docs/elements-specialisations.md`, v3) et validées par Charles : trois paliers à 30, 50 et 100 (D93).

**Goal:** the Elements and Path pages in place of the Talents page, designed by the game designer (`projects/rpg/docs/elements-specialisations.md`, v3) and approved by Charles: three tiers at 30, 50 and 100 (D93).

| Story | Points | État |
|---|---|---|
| En tant que joueur de niveau 30 ou plus, les six réactions sont actives sans talent, et je crée une paire d'éléments (un trio dès 50), ce qui ajoute ses sorts jumeaux à mon grimoire sans en retirer ; les nœuds d'éléments quittent l'arbre des talents, qui donne `1 + (niveau − 30) / 3` points avec des styles sans porte (rpg) ; xUnit : aucune réaction à 29, les six à 30, un trio refusé à 49, un composé non créé sans sort, un combat de Foudre identique avant et après la migration. | 3 | À faire |
| En tant que joueur, mon personnage passe en version 4 sans rien perdre (la Foudre créée d'office si je l'avais, mes points rendus, mon deck valide), et le serveur crée, dissout et vérifie les composés ; `admin --create` pour Plouf (rpg) ; intégration PostgreSQL : migration d'un personnage en version 3, rejeu refusé pour un sort de composé non créé. | 2 | À faire |

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
