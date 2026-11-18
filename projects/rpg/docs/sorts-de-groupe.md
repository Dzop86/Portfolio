# L'IA des sorts de groupe *(18 novembre 2026, sprint 223, D102)*

La story : « En tant que joueur, l'IA joue le Bouclier de la foi, la Haie, le Sol grondant, le Rappel, la Feinte, la Cabriole, le Cercle de fer et la Fiole de peste quand ils servent ; chacun à 0,5 lancer par combat au banc de groupes, **ou la raison écrite**. » Elle vient de la Q1 de `docs/banc-groupes.md` : les 8 sorts restés muets en duo.

**En bref : aucun des 8 n'atteint 0,5.** L'IA en comptait mal quatre, c'est corrigé ; la Haie passe de 0,0 à 0,3. Pour tous, la raison est mesurée et écrite plus bas. L'IA les voit bien à chaque tour, mais ils valent par PA **deux à six fois moins qu'un coup**. Leurs montants fixes (25 de bouclier, 12 de poison, −2 PM) ne grandissent pas avec le niveau, alors que les coups grandissent avec les caractéristiques. Ce n'est plus l'IA, ce sont les nombres : la Q1 plus bas.

## 1. Ce que l'IA comptait mal, corrigé

| Sort | Avant | Maintenant (`Ai.cs`) |
|---|---|---|
| Bouclier sur un allié (Bouclier de la foi, Écorce) | le montant de base | le montant que les règles donnent vraiment : celui d'un allié grandi par les règles de bouclier du lanceur (`Fight.ShieldOn`, partagé avec le combat), comme le Gardien du bosquet (+25 %) |
| Glyphe qui accueille les alliés (Haie, Sol grondant) | son statut à 0,05 par point et par tour : 1 pour +20 % de résistance | le même statut lancé sur l'allié (`Ai.AllyBoon`) : une résistance vaut sa part des coups à portée, un bonus de dégâts une part du tour de l'allié |
| Rappel (échange avec le Familier) | lancé seulement pour secourir le Familier, à moitié mort au contact d'un ennemi | lancé quand il sort le lanceur des coups : ce qui le frappe où il est, moins ce qui le frapperait chez le Familier (`Ai.Escape`) |
| Feinte (statut sur soi) | 0,05 par point et par tour : 0,2 | les PM qu'elle prendrait à l'ennemi qui peut venir frapper au contact, au moins 2 par PM (`Ai.FeintWorth`) |
| Cabriole (bond sans coup) | prise seulement pour frapper d'où elle retombe : jamais | un héros qui ne peut plus payer aucun coup ce tour bondit hors des coups avec ses PA restants, qui seraient perdus sinon (`Ai.EscapeLeap`) |

Cinq tests (`PartySpellsAiTests`) les lisent sur des combats posés à la main, sans jouer de combat (le budget de la suite est presque plein, D96).

**Un essai retiré.** Je pensais que la Fiole de peste ne valait rien après la Lame enduite, son statut « renouvelé » ne comptant que les tours ajoutés. C'était faux : les poisons du Voleur s'empilent (3 doses, règle de classe), et l'IA comptait déjà la dose entière. Le changement, inutile pour la Fiole, aurait touché tous les statuts renouvelés de toutes les classes : retiré.

## 2. La mesure

`Rpg.Sim --matrix --casts --party-size 2`, 20 combats par groupe, 9 hordes, aux niveaux 35 et 50, pour les 5 classes qui ont ces sorts. Lancers par combat, sur la voie et le niveau qui les ont :

| Sort | Voie, niveau | Avant | Après |
|---|---|---|---|
| Haie | Gardien de la nature 35 ; Sylvain, Gardien du bosquet 50 | 0,0 | **0,3** |
| Bouclier de la foi | Défenseur 50 | 0,3 | 0,3 |
| Fiole de peste | Empoisonneur 50 | 0,2 | 0,2 |
| Sol grondant | Chaman 35 ; Invocateur spirituel 50 | 0,1 ; 0,0 | 0,2 ; 0,1 |
| Rappel | Dompteur 50 | 0,1 | 0,2 |
| Feinte | Bretteur 50 | 0,1 | 0,1 |
| Cabriole | Malandrin 35 ; Bretteur 50 | 0,0 | 0,1 ; 0,0 |
| Cercle de fer | Tacticien 50 | 0,0 | 0,0 |

**Les victoires ne bougent pas** : la moyenne des voies passe de 66,3 à 66,1 au niveau 35, de 70,9 à 70,7 au niveau 50. Seul le Gardien de la nature perd 6,7 points au niveau 35 (70,0 à 63,3) : ses 0,3 Haies par combat prennent la place de coups. C'est dans l'intervalle de la mesure (±7 points à 180 combats), mais dans le sens du reste : la Haie vaut moins qu'un coup.

## 3. Pourquoi ils restent muets

Pour le savoir, une sonde jetable (non gardée dans le dépôt) a lu, à chaque tour du héros en duo contre les hordes (3 combats par horde), la valeur que l'IA donne au sort quand il est prêt, et celle du meilleur sort du tour :

| Sort | Tours où il est prêt | Valeur moyenne quand il sert | Par PA | Le meilleur coup du tour, par PA | La raison |
|---|---|---|---|---|---|
| Bouclier de la foi (2 PA) | 377 | 25 au plus, l'allié à portée | 12,5 | 12,3 | **portée 2** : le Guerrier n'est à 2 cases du Paladin que 47 tours sur 377 (12 %). Alors il vaut un coup et l'IA le lance parfois ; le reste du temps, rien à protéger |
| Fiole de peste (3 PA) | 250 | 23,9 | 8,0 | 12,9 | 12 de poison par tour, **non grandi par les caractéristiques** : au niveau 50, un coup du Voleur fait 48 |
| Sol grondant (3 PA) | 241 | 12,6 | 4,2 | 8,6 | 6 à 9 de dégâts de glyphe et +10 % de dégâts aux alliés pour 1 tour |
| Haie (3 PA) | 245 | 10,4 | 3,5 | 8,6 | −1 PM d'un tour et +20 % de résistance d'un tour : le moins des deux glyphes, mais celui que l'IA joue maintenant |
| Rappel (2 PA) | 346 | 7,6, et seulement 47 tours sur 346 | 3,8 | 10,3 | l'Invocateur se tient derrière ses créatures : **il est rarement menacé**, il n'a rien à fuir |
| Feinte (2 PA) | 254 | 3,9 | 2,0 | 12,6 | le monstre qui frappe au contact perd ses PM, **mais il est déjà au contact** : il frappe encore au tour suivant |
| Cercle de fer (3 PA) | 360 | 5,3 | 1,8 | 14,6 | même raison : il retire 2 PM aux ennemis **déjà au contact** du Guerrier, qui le frappent sans bouger. Il ne servirait qu'à un Guerrier qui recule ensuite, ou qui garde un allié fragile derrière lui (le duo a deux combattants au contact) |
| Cabriole (2 PA) | 281 | 0 sauf en fin de tour | – | – | le Voleur a presque toujours de quoi payer un coup : il ne bondit pour fuir que les rares tours où il n'en a plus |

**Ce que ça dit.** Pour les sorts de groupe (Bouclier, glyphes, Rappel), l'IA les joue quand ils servent : ils servent peu, à cause de leur portée ou de leur place dans le groupe. Pour les sorts de contrôle (Feinte, Cercle de fer), l'IA a raison : retirer des PM à un monstre déjà au contact ne change pas son coup. Pour tous, au niveau 50, leurs nombres de base sont à la moitié d'un coup ou moins.

**Un détail vu en passant, non corrigé :** l'IA donne au Bouclier de la foi lancé sur un ennemi une valeur positive mais minuscule (moins de 0,05), trop petite pour l'emporter sur un autre sort (7 lancers du Bouclier en 377 tours où il était prêt). Noté dans `REVIEW.md`.

## 4. À trancher par Charles

- **Q1. Les 8 sorts sous 0,5** : a) **faire grandir avec le niveau leurs montants fixes : boucliers, poisons, bonus des glyphes, comme les soins depuis le sprint 203 (×1,5 au niveau 50, ×2 à 100), puis les remesurer. Le même défaut grandira avec les zones de 46 à 100 : à 100, un bouclier de 25 ne protège de rien. Une story de 3 points, au jalon 4 (Reco)** ; b) les retoucher un par un au barème (portée du Bouclier à 4, Cercle de fer qui immobilise au lieu de retirer 2 PM…), comme les sorts de voie à S220 et S221 ; c) les laisser comme ils sont : le playtest dira s'ils manquent.
- **Q2. `CiLevelsTests.Unplayed`** (la liste des sorts que le banc seul ne joue jamais) : la Cabriole, le Bouclier de la foi, le Rappel ou la Haie en sortiront peut-être, maintenant que l'IA les lance à l'occasion ; seul le banc complet, hors CI (`-- --explicit`, long), le dit. a) **le relancer au jalon 4 et mettre la liste à jour (Reco)** ; b) le relancer maintenant.

*Rien de ce sprint ne change le plan : les questions ne bloquent pas la zone de 46 à 60 (S224 à S228).*
