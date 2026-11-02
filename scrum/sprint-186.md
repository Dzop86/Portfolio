# Sprint 186 : Osmose : le moteur des voies

**Objectif :** les pages Éléments et Voie à la place de la page Talents, conçues par le game designer (`projects/rpg/docs/elements-specialisations.md`, v3) et validées par Charles : trois paliers à 30, 50 et 100 (D93).

**Goal:** the Elements and Path pages in place of the Talents page, designed by the game designer (`projects/rpg/docs/elements-specialisations.md`, v3) and approved by Charles: three tiers at 30, 50 and 100 (D93).

| Story | Points | État |
|---|---|---|
| En tant que joueur de niveau 30, je choisis une des trois branches de ma classe (évolution I), et les évolutions II et III s'allument seules à 50 et 100 ; tant que je n'ai pas choisi, je reste dans ma classe de base et mon premier choix reste gratuit (rpg) ; xUnit : voie refusée avant 30, évolution II absente à 49, un héros de 60 qui choisit reçoit I et II, déclencheur `Kill`, combat rejoué à l'identique. | 3 | Fait (T146) |
| En tant que joueur, la quête « Une route à soi » à Quatre-Chemins ouvre la page Voie ; changer de voie coûte 30 × niveau Éclats, les rangs sont transférés, et une annonce marque les paliers 50 et 100 ; `admin --spec` pour Plouf (rpg) ; intégration PostgreSQL : choisir, changer et payer, combat refusé avec une autre voie. | 2 | Fait (T146) |

**Ce qui est fait.** Le moteur des voies, repris de la branche `s163-specialisations` et récrit pour les trois paliers : `data/specs.json` porte les 9 branches avec leurs titres et leurs trois évolutions décrites, `Hero.Spec` en fait un autre héros pour le serveur, les évolutions de son niveau arrivent d'un coup, à 30, 50 et 100. Passifs des héros, déclencheur `Kill`, passif « aucun ennemi à N cases », sorts adaptatifs, règles compilées, deck automatique qui garde les sorts de voie. Côté serveur, `PUT …/spec`, colonne `Spec`, 30 × niveau pour changer (dans un village, hors combat), rangs et deck transférés un pour un, `admin --spec`. La quête « Une route à soi » à Quatre-Chemins (Anatole, puis Josette), et l'annonce de palier dans le résumé de combat et en ville.

**Ce qui a bougé.** Le moteur est testé sur deux voies écrites dans les tests, car les sorts et passifs réels des 9 branches arrivent de S191 à S193 (D95). Choisir une voie aujourd'hui donne donc un titre et rien d'autre, et la page Spécialisation n'existe pas encore : le rendu de la quête y renvoie déjà. La quête se donne et se rend à Anatole, avec un passage chez Josette pour qu'elle ne se réduise pas à un clic. **Textes de travail** (quête, titres, descriptions, noms anglais des sorts) à relire par le scénariste.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
