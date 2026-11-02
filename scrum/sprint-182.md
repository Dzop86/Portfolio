# Sprint 182 : Osmose : le cœur des éléments

**Objectif :** les pages Éléments et Voie à la place de la page Talents, conçues par le game designer (`projects/rpg/docs/elements-specialisations.md`, v3) et validées par Charles : trois paliers à 30, 50 et 100 (D93).

**Goal:** the Elements and Path pages in place of the Talents page, designed by the game designer (`projects/rpg/docs/elements-specialisations.md`, v3) and approved by Charles: three tiers at 30, 50 and 100 (D93).

| Story | Points | État |
|---|---|---|
| En tant que joueur de niveau 30 ou plus, les six réactions sont actives sans talent, et je crée une paire d'éléments (un trio dès 50), ce qui ajoute ses sorts jumeaux à mon grimoire sans en retirer ; les nœuds d'éléments quittent l'arbre des talents, qui donne `1 + (niveau − 30) / 3` points avec des styles sans porte (rpg) ; xUnit : aucune réaction à 29, les six à 30, un trio refusé à 49, un composé non créé sans sort, un combat de Foudre identique avant et après la migration. | 3 | Fait (T142) |
| En tant que joueur, mon personnage passe en version 4 sans rien perdre (la Foudre créée d'office si je l'avais, mes points rendus, mon deck valide), et le serveur crée, dissout et vérifie les composés ; `admin --create` pour Plouf (rpg) ; intégration PostgreSQL : migration d'un personnage en version 3, rejeu refusé pour un sort de composé non créé. | 2 | Fait (T142) |

**Ce qui est fait.** Les réactions viennent du niveau (`FighterSpec.Reacts`, un héros de 30 ou plus), plus des talents ; un enregistrement de version 4 ou moins se rejoue sans elles (`FightRecord` version 5). Le héros a ses composés créés (`Hero.Compounds`, de 30 pour une paire, de 50 pour un trio, une fois chacun), et le grimoire leur ajoute les jumeaux de leurs parents, sans plus rien remplacer : Transmutation, Conversion, Prisme, Signature appuyée, les six paires et les quatre Cœurs quittent l'arbre (41 talents, leurs icônes retirées), qui donne `1 + (niveau − 30) / 3` points, avec des styles ouverts sans porte. Le serveur garde les composés (colonne `Compounds`, migration EF), les reçoit avec les points, et migre un personnage de version 3 à la connexion : ses éléments retirés, la Foudre créée s'il avait Électrisation, son deck gardé, ses styles tant que les points les paient. `admin --compounds all|none|NOM` pour Plouf. Les succès de paire, de Cœur et de Conversion se gagnent en créant des composés.

**Ce qui a bougé.** L'espalier sans portes (prévu en E3, S183) est fait ici : le client et le jeu ne compilaient plus sans les portes. S183 garde la page Éléments, ses textes et la remesure. Le joueur ne peut pas encore créer la Foudre lui-même : la page Éléments vient en S183 ; d'ici là, seuls la migration et la commande d'administration la créent. Le succès « Deux fusions » attend la deuxième paire (S184).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
