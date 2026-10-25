# Sprint 146 : Osmose : le tronc, les points et la Foudre arbitrée

**Objectif :** l'arbre des talents en vrai arbre (tronc, paires d'éléments, styles de jeu), d'après les réponses de Charles au questionnaire et les arbitrages du game designer (`projects/rpg/docs/talents-arbre-questions.md`, D82).

**Goal:** the talent tree as a real tree (trunk, element pairs, play styles), from Charles's answers to the questionnaire and the game designer's rulings (`projects/rpg/docs/talents-arbre-questions.md`, D82).

| Story | Points | État |
|---|---|---|
| En tant que joueur, mon tronc s'allume tout seul à 30, 35 et 40 (+5 % de dégâts, +3 % de résistances, +5 % de PV), visible dans les caractéristiques, et je reçois 2 points à 30 puis 1 tous les 3 niveaux (rpg) ; xUnit, mesures d'équilibre. | 3 | Fait |
| En tant que joueur, je ne vois de Foudre dans mes sorts qu'une fois une Transmutation ou une Conversion enregistrée, avec un aperçu en pointillés dans le brouillon ; mes deux paires ne partagent aucun élément ; mes talents sont rendus gratuitement une fois (rpg) ; xUnit, intégration PostgreSQL, auto-test du village. | 2 | Fait |
| En tant que Charles, je valide une maquette statique de la vue focalisée, rendue hors écran, avant S148 (rpg). | 1 | À valider par Charles |

## Fait
- Le tronc automatique (+3 % de dégâts à 30, baissé de 5 par la mesure du Disjoint, +3 % de résistances à 35, +5 % de PV à 40), 2 points à 30 puis 1 tous les 3 niveaux.
- La Foudre arbitrée : plus d'Ouverture ni de Plein grimoire, la Transmutation à choix après Électrisation, la Conversion à 45 qui la remplace sans frais ; l'aperçu des sorts du brouillon, hors du filtre et du deck.
- Deux paires sans élément commun, la raison dite en FR et en EN ; les talents de l'ancien arbre rendus une fois, gratuitement, à la connexion ; `--builds` pour Plouf.
- L'onglet « Ma route » renommé « Talents » (Charles).
- La maquette statique de la vue focalisée : `projects/rpg/docs/maquette-arbre.png`.
- Tests : 15 de plus ; Rpg.Core 589 (mesures d'équilibre comprises), Rpg.Client 205, Rpg.Api 103, Rpg.Sim 20 ; auto-test du village en headless.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
