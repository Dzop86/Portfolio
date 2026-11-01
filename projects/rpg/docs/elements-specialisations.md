# Les pages Éléments et Voie *(game designer, 1er novembre 2026 ; v3 en tête, v2 et v1 en historique)*

> **La demande de Charles (1er novembre)** : « Dans les caractéristiques, j'ai une page Talent, qui ne sert à rien actuellement. On va la remplacer par une page "Spécialisation de classe" (ou un autre nom) avec un arbre de spécialisation de classe (en gros un mage qui peut choisir 2/3 spécialisations qui auront chacune 2/3 évolutions et on verra plus tard). L'arbre doit se lire de gauche à droite. Il faudra rajouter une page "Éléments" (ou un autre nom) pour que le personnage choisisse ses éléments ici (feu, eau, terre, air, foudre etc.) et retirer les éléments de l'arbre des talents (on refera l'arbre des talents de A à Z plus tard). »
>
> **Ses choix** : je propose les règles des éléments et il les valide ; les spécialisations déjà conçues (passif, règle changée, 3 sorts, quête « Une route à soi » au niveau 50, `talents-arbre.md` §7) deviennent les **branches** de l'arbre de chaque classe, et je conçois 2 ou 3 **évolutions** par branche ; la refonte complète de l'arbre des talents est reportée ; un sprint de gains rapides (inventaire, familier) passe avant ces deux pages.
>
> **Ce document remplace**, pour la suite, les pistes de `arbre-refonte-questions.md` (elles serviront à la refonte complète de l'arbre, plus tard). Rien n'est planifié, et aucun fichier de code ni de données n'a été modifié.
>
> **Lu dans le code :** `Hero.cs` (`Talents`, `Problem`, `DeckOf`, `DeckLimit`), `Talents.cs` (`Points`, `Problem`, `Clash`, `MaxOpened`, `GrimoireOf`, `With`, `Without`, `ChangeCost`, `Version`), `Data.cs` (`Characteristics.Bonus`, `Threshold`, `Effective`, `For`), `Fight.cs` (la `FighterSpec` reçoit `hero.Talents`, et les PV des talents sont lus sur le héros), `CharacterEndpoints.cs` (`PUT …/points`, la remise de la version), `Entities.cs` (`Talents`, `TalentsVersion`), `PointsPanel.cs` (les onglets, la fenêtre de 1140 × 620), `data/talents.json`, `data/classes.json`. La branche locale `s163-specialisations` (1f8fe18) contient déjà le moteur des voies et la Sentinelle, avec des tests verts (mémoire du projet) : on la reprend, on ne repart pas de zéro.

---

## v3, la décision de Charles : trois paliers *(1er novembre 2026, validée)*

> **Ses mots** : « 30 on peut créer toutes les paires et on a la première évolution de classe. 50 on peut créer les trios (par exemple eau/feu/air) et une nouvelle évolution de classe. 100 ensuite la dernière évolution de classe. » ; « 50 c'est l'accès aux éléments qui en fusionnent 3 de base. » ; « Un mage peut rester mage en soi, mais niveau 30, il peut évoluer dans 3 branches différentes. »
>
> **Ce qui en découle** : tout le monde a les quatre éléments de base dès le niveau 1, et il n'y a que **trois paliers : 30, 50 et 100**. Les emplacements, le « second élément » et les évolutions à 60, 75 et 90 (v1, v2) disparaissent. La v2 et la v1, plus bas, sont de l'historique ; on en garde le contenu des 9 voies (§2.2), la maquette de la page Voie (§2.3, avec 3 colonnes) et ce que l'arbre des talents perd (§3).
>
> **Ses réponses aux deux dernières questions (§v3.8)** : Q9 b, « rester mage », c'est **simplement ne pas encore avoir choisi** (il n'y a pas de voie généraliste) ; Q10 a, les composés se **créent un à un**, gratuitement et de façon réversible ; les onglets s'appellent « Éléments » et « Voie ». Le plan est validé : c'est la décision D93.

### v3.1 La table des paliers

| Palier | Éléments | Voie (évolution de classe) | Titre (Mage, en exemple) |
|---|---|---|---|
| **1** | Terre, Feu, Eau, Air, pour tous | la classe de base | Mage |
| **30 · l'Éveil** | **Les 6 paires** : les **6 réactions** s'activent pour tous, et chaque **composé de paire** peut être **créé** sur la page Éléments (gratuit, sans limite, réversible), ce qui ajoute ses 8 sorts au grimoire | **Évolution I** : par la quête « Une route à soi », on prend **1 des 3 branches**. Tant qu'on n'a pas choisi, on reste dans sa classe de base, sans évolution, et le premier choix reste gratuit | Archimage, Sorcier, Nécromancien (Mage tant qu'il n'a pas choisi) |
| **50 · l'Affirmation** | **Les 4 trios** : leurs **réactions à trois** s'activent, et chaque trio peut être **créé** (4 sorts par classe) | **Évolution II** de la voie prise | Haut archimage… |
| **100 · l'Accomplissement** | rien de plus dans la Saga I (Unisson, le quadruplet, reste à la Saga II) | **Évolution III**, la dernière | Archimage souverain… |

**L'arbre des talents ne fait pas partie des paliers** : il garde son rythme (un point tous les 3 niveaux) en attendant sa refonte (§v3.5).

### v3.2 Les éléments : ce qu'on garde, renomme ou retire

**La règle des composés** (reprise d'`elements.md` §0, point 8) : un composé frappe sur la **plus haute** caractéristique de ses parents ; ses sorts sont des **jumeaux** transposés par `spells_gen.py` (même forme, nouvel élément, budget × 0,8 pour une paire, × 0,75 pour un trio, signature comptée comme un effet). « Créer » un composé ne remplace aucun sort : il **ajoute** ses jumeaux au grimoire. Le deck reste le frein (12 places). Avec tout créé, un Mage a 24 + 48 + 16 = **88 sorts** : c'est pour cela que la création est un geste, pour que le livre de sorts reste lisible.

**Les 6 paires** (ouvertes à 30 ; réactions et signatures d'`elements.md` §2, déjà dans `data/reactions.json` pour les réactions) :

| Paire | Parents | Signature des sorts | Réaction (frapper l'un sur l'empreinte de l'autre) | État |
|---|---|---|---|---|
| **Foudre** | Feu + Air | Arc : 35 % sautent sur l'ennemi le plus proche | Électrisé : 35 % de chaque coup reçu sautent, 1 tour | sorts **existent** (24 jumeaux) |
| **Glace** | Eau + Air | Morsure : −1 PA, 1 tour | Transi : −2 PA, 1 tour | conçue (S184), à générer |
| **Boue** | Terre + Eau | Engluement : −1 PM, 1 tour | Enlisé : −2 PM, 1 tour | à générer |
| **Magma** | Terre + Feu | Fonte : −8 % de résistance, 1 tour | Fêlé : −15 % de résistance, 2 tours | à générer |
| **Vapeur** | Feu + Eau | Voile : −10 % de dégâts infligés, 1 tour | Embué : −25 % de dégâts infligés, 1 tour | à générer |
| **Sable** | Terre + Air | Grain : −1 PO, 1 tour | Aveuglé : −2 PO, 1 tour | à générer |

**Les 4 trios** (ouverts à 50 ; noms et signatures d'`elements.md` §2, réactions nouvelles). Chacun se nomme par l'élément absent. **La réaction d'un trio** : un sort du trio qui frappe un ennemi portant l'empreinte d'**un de ses trois parents** la consomme et déclenche la réaction du trio (à la place de celle de la paire). C'est plus fort qu'une paire, mais cela demande d'avoir créé le trio et de le mettre dans son deck.

| Trio | Parents (sans…) | Signature | Réaction (nouvelle) |
|---|---|---|---|
| **Éther** | Feu + Eau + Air (sans Terre) | Traversée : le sort ne demande pas la ligne de vue | **Dispersé** : la cible perd ses boucliers, et sa case ne bloque plus ni le passage ni la vue pendant 1 tour |
| **Cendre** | Terre + Feu + Air (sans Eau) | Retombée : la case de la cible devient une glyphe de cendre 2 tours | **Enseveli** : la case de la cible **et ses 4 voisines** deviennent une glyphe de cendre 2 tours (dégâts à qui y commence son tour, au barème) |
| **Sève** | Terre + Feu + Eau (sans Air) | Bourgeon : l'allié le plus blessé à 2 cases est soigné de 20 % des dégâts | **Enraciné** : la cible a 0 PM pendant 1 tour, et les alliés à 2 cases sont soignés de 10 % des dégâts |
| **Sel** | Terre + Eau + Air (sans Feu) | Salure : −50 % de soins reçus, 2 tours | **Desséché** : ni soin ni bouclier reçu pendant 2 tours |

**Ce qu'on garde, renomme ou retire**

| Existant | Sort |
|---|---|
| Les 6 réactions (`reactions.json`) | **gardées**, mais elles ne sont plus des talents : elles sont actives pour tous dès 30 |
| Les 6 nœuds de paire de l'arbre (Enlisement…) | **retirés** de l'arbre |
| Les 4 Cœurs (Élément pur) | **retirés** (un « +x % » de plus ; la refonte de l'arbre en décidera) |
| La tresse de Foudre : Transmutations, Conversion, Prisme, Signature appuyée | **retirée**. Le grimoire ne remplace plus rien (tout composé créé s'ajoute) ; la Conversion n'a plus de sens quand tout est ouvert. Prisme et Signature appuyée sont des idées à reprendre dans la refonte de l'arbre |
| Les 24 jumeaux de Foudre | **gardés** tels quels (noms, icônes, rangs partagés avec leur sort d'origine) |
| La règle « 2 éléments ouverts au plus », C2 (paires disjointes), « une seule grande bascule » | **retirées** |
| Lumière et Ténèbres (D80, branche Pôles, Crépuscule) | **hors des paliers** : reportées à la refonte de l'arbre ou à la Saga II. Ce ne sont pas des fusions de base, et Charles ne les cite pas |
| Unisson (le quadruplet) | **Saga II**, inchangé (D80) |
| Les points d'élément, le rendement décroissant (D85) | **inchangés** ; la page Éléments les affiche |

**L'équilibre à surveiller.** Les réactions, gratuites pour tous à 30, sont **un gain de puissance pour tout le monde**. Contre les petits groupes, rien ne bouge, c'est déjà saturé ; contre les hordes, il faut mesurer (§v3.7, E4). Règle écrite d'avance : si les hordes de la Forêt passent au-dessus du plafond de T41 (3 sur 10 sans kit), alors **une réaction par lanceur et par tour** ; si cela ne suffit pas, les durées sont retirées d'un tour.

### v3.3 La page Éléments (inchangée dans l'esprit, simplifiée)

À gauche, les 4 éléments de base avec leurs points et le seuil de D85. Au centre, une **roue des fusions** : les 6 paires (verrouillées avant 30), puis les 4 trios (verrouillés avant 50), chacune avec sa réaction, sa signature et un bouton **Créer** ou **Dissoudre**. Ces deux actions sont gratuites, se font hors combat, et le deck est revu par le serveur. En haut, la **frise des paliers 30, 50, 100**, commune avec la page Voie.

### v3.4 L'arbre de voie, par classe

**La structure.** À gauche la racine (la classe), puis **3 lignes**, les **3 branches**. Les colonnes sont **30, 50 et 100**. On ne prend qu'une ligne. Les évolutions II et III viennent seules avec le niveau. Le prix est inchangé : la première voie est gratuite, les suivantes coûtent 30 × niveau Éclats, chez l'habitant des voies, et les rangs sont transférés.

**« Rester mage » (Q9 b).** Ce n'est pas une ligne de l'arbre : c'est l'état d'un héros qui n'a pas encore choisi. Il garde sa classe de base et ses 24 sorts, sans évolution, et la page Voie lui montre les 3 branches ouvertes. Son premier choix reste gratuit, quel que soit son niveau, et il reçoit d'un coup les évolutions de son niveau (I à 30, II à 50, III à 100). *(La voie généraliste proposée en v3, Q9 a, n'est pas retenue : ses lignes sont retirées des tableaux ci-dessous.)*

Notation des sorts : PA, PO, relance ; « au barème » = `spells_gen.py`. Les noms sont **des noms de travail**, à reprendre par le scénariste.

**Mage**

| Voie | 30 (évolution I) | 50 (évolution II) | 100 (évolution III) |
|---|---|---|---|
| **Archimage** | passif : +1 PA au tour suivant s'il n'a pas bougé ; règle : sorts de 6 PA ou plus, relance −1 ; sorts **Comète** (7 PA, PO 3 à 7, cercle de 1, relance 3) et **Recueillement** (2 PA, le prochain sort +25 %, relance 3) | **Rayon** (5 PA, ligne de 4, sans vue, relance 2) ; le passif tient même après un pas | une fois par combat, un sort de 6 PA ou plus coûte 3 PA de moins ; Comète laisse une zone de −1 PM |
| **Sorcier** | passif : ses états durent +1 tour ; règle : 2 poisons cumulables ; **Malédiction** (3 PA, PO 1 à 6, −15 % de dégâts infligés 2 tours) et **Fiel** (3 PA, poison au barème 3 tours) | **Contagion** (4 PA, copie les états de la cible sur les ennemis à 1 case, relance 3) ; 3 poisons cumulables | un ennemi sous 2 états ou plus reçoit +15 % de ses coups ; Malédiction en croix de 1 |
| **Nécromancien** *(nom au scénariste)* | passif : une ombre d'un ennemi tombé, une fois par combat ; règle : 2 invocations ; **Lever une ombre** (4 PA, ombre de 25 % de ses PV, relance 3) et **Lien funeste** (2 PA, une invocation lui cède 20 % de ses PV) | **Éclat d'ombre** (3 PA, une invocation explose, au barème, en croix de 1) ; ombres +25 % de PV | 3 invocations ; le passif joue deux fois par combat |

**Garde**

| Voie | 30 | 50 | 100 |
|---|---|---|---|
| **Palissade** | passif : bouclier de 6 (+1 tous les 10 niveaux) aux alliés à 1 case ; règle : boucliers sur alliés +25 % ; **Interposition** (2 PA, PO 1 à 3, échange de place et bouclier sur soi, relance 2) et **Ancrage** (2 PA, indéplaçable et +15 % de résistance 1 tour, relance 2) | **Mur de boucliers** (4 PA, croix de 1, bouclier aux alliés, relance 3) ; le passif porte à 2 cases | une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV ; Interposition se relance en 1 tour |
| **Brèche** | passif : ennemi achevé, +1 PM (déclencheur `Kill`) ; règle : attirances +1 case ; **Charge** (3 PA, ligne de 2 à 4, saut au contact et dégâts au barème, relance 2) et **Arrachement** (3 PA, PO 1 à 4, attire de 2, −1 PM) | **Fauchage** (4 PA, contact, croix de 1, au barème × 0,7) ; ennemi achevé : +1 PM et +1 PA | une attirance contre un obstacle fait 6 dégâts par case ; Charge se relance en 1 tour |
| **Étau** | passif : qui la frappe au contact perd 1 PM ; règle : retraits de PA et de PM +1 tour ; **Prise** (3 PA, contact, au barème, −2 PM 2 tours) et **Verrou** (2 PA, contact, plus de déplacement volontaire 1 tour, relance 3) | **Écrasement** (5 PA, contact, au barème, +30 % sur un ennemi à 0 PM) ; le passif retire aussi 1 PA, une fois par tour | les ennemis au contact perdent 1 PM au début de leur tour ; Verrou se relance en 2 tours |

**Sentinelle**

| Voie | 30 | 50 | 100 |
|---|---|---|---|
| **Mire** | passif : aucun ennemi à 3 cases, +15 % pendant 1 tour ; règle : sorts en vue +1 PO ; **Tir visé** (5 PA, PO 4 à 9, relance 2) et **Visée** (2 PA, le prochain sort ignore 15 % de résistance, relance 3) | **Tir de barrage** (4 PA, PO 3 à 8, ligne de 3, −1 PM, relance 2) ; règle à +2 PO pour les sorts de 6 PO ou plus | le passif monte à +25 % sur 2 tours ; Tir visé traverse (une case de plus, à 50 %) |
| **Affût** | passif : le Faucon reste 2 tours de plus ; règle : ses glyphes durent +1 tour ; **Mâchoires** (3 PA, glyphe 2 tours, −3 PM et dégâts au barème, relance 2) et **Piqué** (3 PA, le Faucon frappe à 4 cases de lui, relance 1) | **Appât** (2 PA, glyphe 2 tours, −1 PA à qui y commence son tour, relance 3) ; Faucon +30 % de PV | deux Mâchoires à la fois ; un ennemi piégé prend +15 % de ses coups pendant 2 tours |
| **Sente** | passif : frappée au contact, +2 PM 1 tour ; règle : sauts sans la vue ; **Bond** (2 PA, PO 2 à 4, saut, relance 1) et **Coup de talon** (3 PA, contact, au barème, puis recul de 2) | **Feinte** (2 PA, le prochain qui la frappe au contact perd 2 PM, relance 3) ; Bond sans relance (une fois par tour) | passif à +3 PM ; une fois par combat, le premier coup au contact est esquivé ; après un saut, +10 % au prochain sort |

**Équilibre.** Chacune des 9 voies reste à **±8 points** de la moyenne de sa classe contre les hordes (300 combats), à **35, 45 et 50** ; les planchers T41 des petits groupes tiennent dès 35 ; à 30, une voie vaut au plus +10 % de puissance par rapport à un héros qui n'a pas encore choisi.

### v3.5 L'arbre des talents en attendant sa refonte, et la quête

- **Il perd les 15 nœuds d'éléments** (6 paires, 4 Cœurs, 5 de la tresse) : il en reste **41**, les 6 styles. Ils partent du tronc. L'Alchimie, qui demandait une paire, s'ouvre comme les autres, puisque tout le monde a les réactions dès 30.
- **Les points** : `1 + (niveau − 30) / 3`, soit 1 à 30, 6 à 45, 24 à 100. C'est **un point de moins** : celui que coûtait la porte. Les clés restent à 45. Rien d'autre ne change (le plafond, les « % » et les doublons attendent la refonte, `arbre-refonte-questions.md`).
- **L'espalier** perd la ligne des portes et la colonne Fusion : 6 couloirs depuis le tronc.
- **La quête « Une route à soi »** revient à **30**, son niveau d'origine, à Quatre-Chemins, « là où les routes se séparent ». Elle ouvre la page Voie, et l'habitant des voies (scénariste) présente les 3 chemins ; on peut repartir sans choisir et revenir plus tard. **À 50 et à 100**, il n'y a pas de quête : une annonce de palier (bandeau, journal, titre) et l'évolution qui s'allume. Une quête par palier serait du contenu à écrire au-delà de 60, où il n'y a pas encore de zone.

### v3.6 La migration

1. `Talents.Version` passe à **4**. À la connexion, les nœuds d'éléments sont **retirés** de `Talents`, et leurs points reviennent. Les nœuds de style restent s'ils tiennent avec la nouvelle formule (`Talents.Problem`) ; sinon, les derniers pris tombent d'abord (`Talents.Without`). Un mot l'annonce, et le changement de talents est gratuit pendant 7 jours.
2. **Les composés** : un héros qui avait Électrisation (avec ou sans Transmutation ou Conversion) a la **Foudre créée** d'office. Ses sorts de base reviennent (plus rien n'est remplacé), et les jumeaux restent dans le grimoire et dans le deck (`Grimoire.Replaced` sert une dernière fois). Les rangs ne bougent pas : un jumeau partage le rang de son sort d'origine.
3. **La voie** : `Spec` est vide pour tout le monde. Un héros de 30 ou plus voit la page Voie s'allumer et la quête s'ouvrir ; son premier choix est gratuit.
4. **Plouf** (3 × niveau 100) et **Plop** : la commande d'administration gagne `--spec` et `--create all`, pour les restaurer sans SQL.

### v3.7 Le découpage

| Sprint | Story | Tests | Pts |
|---|---|---|---|
| **SE1** (5) | **E1. Le cœur des éléments** : réactions actives pour tout héros de 30 ou plus (au lieu du talent de paire) ; `Hero.Created` (les composés créés) et `GrimoireOf` qui **ajoute** leurs jumeaux (paires dès 30, trios dès 50) ; retrait des nœuds Fork, Pure et Braids et de leurs règles (`Clash`, `MaxOpened`, bascule) ; points `1 + (niveau − 30) / 3` ; styles sans porte | xUnit : à 29 aucune réaction, à 30 les six ; créer un trio refusé à 49, accepté à 50 ; un composé non créé n'a aucun sort dans le grimoire ; un combat de Foudre identique avant et après pour un héros migré (même graine) ; `Talents.Problem` accepte un style sans porte | **3** |
| | **E2. Serveur et migration** : colonnes `Created` et version 4 (migration EF), créer et dissoudre (`PUT …/points`), `admin --create` | intégration PostgreSQL : un personnage en version 3 (Foudre convertie, Boue, Mêlée) passe en version 4 avec la Foudre créée, son deck valide et ses points rendus ; le rejeu serveur accepte un combat avec un composé créé et refuse un sort de composé non créé | **2** |
| **SE2** (5) | **E3. La page Éléments** (base, roue des 6 paires et 4 trios, Créer et Dissoudre, frise 30-50-100) et **l'espalier sans portes** ; textes FR et EN | xUnit client : `PointsEditor` (créer, dissoudre, verrou par niveau), `TreeLayout` G1 à G13 sans portes ; clés FR et EN ; auto-test Godot : créer la Glace à 30, la page tient à 1280 × 720 | **3** |
| | **E4. Remesurer** : réactions pour tous, T41 à 35, 40 et 45, hordes, builds de référence découpés, `mesures-arbre.md` v3, la règle écrite d'avance (§v3.2) appliquée si besoin | `TemplateTests`, J3, `BalanceTests` (Disjoint à 30) verts ; mesure hors CI écrite | **2** |
| **SC1** (4) | **C1. Les 5 paires restantes** : Glace, Boue, Magma, Vapeur, Sable, soit 120 jumeaux (`spells_gen.py`, icônes `spell_art.py`), 5 signatures (des états existants : −PA, −PM, −résistance, −dégâts, −PO), noms proposés par le générateur puis relus par le scénariste | xUnit : chaque jumeau au budget × 0,8 de son sort d'origine ; chaque signature appliquée ; les jumeaux frappent sur la plus haute caractéristique des parents | **4** |
| **SC2** (5) | **C1 bis.** Remesure des 6 paires contre les hordes (aucune à plus de 8 points de la médiane des autres) | mesure hors CI | **1** |
| | **C2. Les 4 trios** : 48 sorts (4 par classe et par trio, budget × 0,75), 4 signatures (Traversée, Retombée, Bourgeon, Salure) et **4 réactions à trois** (Dispersé, Enseveli, Enraciné, Desséché), dont 3 effets nouveaux (case qui ne bloque plus, zone de glyphe, soins et boucliers refusés) | xUnit : chaque réaction et chaque signature ; un sort de trio sur l'empreinte d'un parent déclenche la réaction du trio et pas celle de la paire ; mesure à 50 | **4** |
| **SV1** (5) | **V1. Le moteur des voies** (reprise de `s163-specialisations`) : `data/specs.json` (3 branches par classe, évolutions à 30, 50 et 100), `Hero.Spec`, passifs des héros, déclencheur `Kill`, un héros sans voie (aucune évolution, choix gratuit qui donne d'un coup les évolutions de son niveau), rejeu | xUnit : voie refusée avant 30 ou d'une autre classe ; évolution II absente à 49, présente à 50 ; un héros de 60 sans voie qui choisit reçoit les évolutions I et II ; `Kill` qui finit le combat sans effet ; combat rejoué à l'identique | **3** |
| | **V2. Serveur et quête** : colonne `Spec` (migration EF), `PUT …/spec`, prix 30 × niveau, rangs transférés, quête « Une route à soi » à 30, annonces de palier à 50 et 100, `admin --spec` | intégration PostgreSQL : choisir, changer et payer, rangs transférés ; un combat joué avec une autre voie est refusé ; la quête ouvre le choix | **2** |
| **SV2** (6) | **V3. La page Voie** : racine de classe, 3 lignes, colonnes 30, 50 et 100, fiche à droite, frise commune ; remplace l'onglet Talents (l'arbre reste sur la touche T et dans le panneau Menus) ; textes FR et EN | xUnit client : disposition (aucun chevauchement, nœuds ≥ 44 px, lignes dans un ordre fixe) ; auto-test Godot : choisir une branche à 30, évolutions allumées à 100 (Plouf) | **3** |
| | **V4. La Sentinelle** : ses 3 branches, 9 sorts, évolutions, titres | xUnit par passif, règle et évolution ; ±8 points à 35, 45 et 50 | **3** |
| **SV3** (7) | **V5. La Garde** (et un duo Palissade et dégâts à moins de 1,5 victoire sur 30 de plus qu'un duo dégâts et dégâts) ; **V6. La Mage** (le nom du Nécromancien vient du scénariste) | les mêmes | **3 + 3** |
| | **V7. La mesure croisée** : les 9 voies × 2 builds à 35, 45 et 50, `Rpg.Sim --spec`, et le playtest de Charles à 30 (la quête, une voie, une paire créée) | mesure hors CI ; résultat dans `REVIEW.md` | **1** |
| | **Total** | | **37 points, 7 sprints** |

**Le plan** (D93) : ces 37 points remplacent S182 à S185 (12 points de spécialisations et 4 de la Glace, qui est absorbée par C1), soit **+21 points** et **3 sprints de plus**, de S182 (SE1) à S188 (SV3). Le sprint de gains rapides (S181, D92) est fait. *(La v3 comptait 38 points avec la voie généraliste : V1 perd les places de deck et l'Emprunt. La mesure croisée des voies et le playtest, V7, passent de SC2 à la fin de SV3 : en SC2, les voies n'existaient pas encore.)*

**Les risques** : **les noms**, avec 168 sorts nouveaux (120 jumeaux de paires et 48 de trios), soit 336 noms FR et EN : le générateur propose (« Flèche de Glace »), le scénariste relit par lots, et rien ne bloque un sprint. **La puissance des réactions pour tous**, traitée par la règle écrite d'avance (§v3.2). **Un livre de 88 sorts**, traité par la création comme geste, et le filtre par élément qui existe déjà.

### v3.8 Les questions restantes *(répondues : Q9 b, Q10 a)*

**Q9. « Rester mage » à 30.**
- a) La voie généraliste : +1 place de deck à 30 et +1 à 50, un petit passif polyvalent, et à 100 un sort emprunté à une branche de sa classe (Reco)
- b) **Rester mage, c'est ne pas encore choisir : aucune évolution, mais une branche reste choisissable gratuitement plus tard (choix de Charles)**
- c) La classe de base reçoit les mêmes évolutions chiffrées qu'une branche, sur ses 24 sorts actuels (un sort amélioré par palier)

**Q10. Les composés à 30 et 50.**
- a) **On les crée un à un sur la page Éléments**, gratuitement et de façon réversible : leurs sorts n'arrivent dans le grimoire qu'une fois créés (le livre reste lisible, et c'est ton verbe « créer ») **(Reco, choix de Charles)**
- b) Tous créés d'office au palier : les 48 sorts de paires (puis les 16 de trios) arrivent d'un coup, le deck fait le tri
- c) Créer un composé coûte quelque chose (Éclats, ressources de récolte, une petite quête par composé) : un vrai geste, mais du contenu en plus (environ 3 points)

---

## v2, après les réponses de Charles *(1er novembre 2026, remplacée par la v3)*

> **Ses réponses** : « de base les persos ont tous les éléments. Il faut harmoniser avec la spécialisation de classe. Par exemple, niveau 30, on débloque la possibilité du bi-élément (foudre par exemple) et on débloque le passage de mage en archimage et un autre truc, par exemple. Puis 50 » (la phrase s'arrête là). **3 voies par classe** (Q4 a), **3 évolutions automatiques** (Q3 a, niveaux à revoir pour l'harmonisation), onglets « **Éléments** » et « **Voie** » (Q5 a). Q1 a est confirmée : tout le monde a les quatre éléments de base, sans choix. Q2 (les points de talent) n'a pas eu de réponse : je la reprends en v2.1.
>
> **Ce qui reste valable de la v1** (sections 1 à 5 plus bas, gardées comme historique) : les règles des liens (§1.3, sauf les niveaux), la migration sans perte (§1.5), les changements dans le cœur (§1.6), le contenu des 9 voies (§2.2, réparti autrement ci-dessous), la maquette de la page Voie (§2.3, avec d'autres en-têtes de colonnes), et ce que perd l'arbre des talents (§3). **Les niveaux et le chiffrage de la v1 sont remplacés** par ceux de cette section.

### v2.1 Une échelle de paliers commune

**L'idée.** Un seul calendrier pour tout le personnage : **un palier tous les 20 niveaux, à 30, 50, 70 et 90, puis 100**. Chaque palier ouvre **une chose côté Éléments, une chose côté Voie, et une chose dans l'arbre des talents** (« l'autre truc »). Entre deux paliers, on joue avec ce qu'on vient de recevoir. Au palier, on reçoit trois nouveautés d'un coup, avec une annonce, un titre et une ligne dans les deux pages : c'est un moment fort, à la manière d'une promotion. Les deux pages affichent la même **frise des paliers** en haut (« prochain palier : 50, dans 6 niveaux »).

**Pourquoi tous les 20 niveaux** : Charles pose 30 puis 50. Garder le même pas jusqu'à 90 donne 4 paliers, donc 1 voie et 3 évolutions, ce qui colle exactement à ses « 3 évolutions ». L'écart laisse 20 niveaux pour profiter de chaque palier, et 100 reste le palier final.

| Palier | Éléments | Voie | Arbre des talents (l'autre truc) | Titre (Mage / Archimage, en exemple) |
|---|---|---|---|---|
| **30 · l'Éveil** | **Le bi-élément** : un premier **lien** (une paire et sa réaction, ou la prédilection). Le lien Feu + Air ouvre la **Foudre** : Transmutation (Feu → Foudre ou Air → Foudre) | **La voie** : la quête « Une route à soi » (ramenée de 50 à 30, son niveau d'origine) fait choisir 1 voie sur 3. Le Mage devient **Archimage**, Sorcier ou Nécromancien : **passif, règle changée, 1er sort** | **L'arbre s'ouvre** : le premier cerne du tronc (+3 % de dégâts) et **1 point de talent** | Archimage |
| **50 · l'Affirmation** | **Le second lien** et la **Conversion** (la grande bascule, de 45 à 50) ; un **premier affinage** de la tresse (Prisme ou Signature appuyée) | **Évolution I** et le **2e sort** de la voie | **Les clés** de l'arbre (de 45 à 50, 2 points chacune) | Haut archimage |
| **70 · la Maîtrise** | **Le second affinage** (les deux) | **Évolution II** et le **3e sort** | un **cerne du tronc** (le « tronc à 60 et 80 » de D82, ramené à 70 : +5 % de résistance) | Arcaniste majeur |
| **90 · l'Apogée** | une case **« à venir »** : les triplets, la Lumière et les Ténèbres (D80, avec les chapitres) | **Évolution III** | un **cerne du tronc** (+5 % de PV) | Archimage souverain |
| **100 · l'Accomplissement** | à venir (Unisson, Saga II) | **Accomplissement** : 1 choix sur 2 (un sort majeur et une seconde règle), plus tard | aucun | à venir |

**Les cernes du tronc existants** (35 : +3 % de résistance ; 40 : +5 % de PV) restent où ils sont : les déplacer changerait l'équilibre mesuré de 30 à 45 sans rien apporter. Les deux nouveaux cernes (70, 90) sont du contenu futur, et ce sont les seuls ajouts de puissance pure de la frise.

**Les points de talent (Q2 de la v1, revue)** : **`1 + (niveau − 30) / 3`**, soit 1 à 30, 6 à 45, 7 à 50, 24 à 100, **un point de moins qu'aujourd'hui** (et non deux, comme le proposait la v1). Ce point unique à 30 est le « troisième déblocage » du palier. Un build Manière qui payait 1 point de porte retrouve son compte ; la Foudre gagne ses nœuds de tresse gratuitement, mais sa Conversion recule de 45 à 50. La règle des leviers de la v1 (§1.4) reste prête si la mesure la met devant.

**Les titres de rang** (noms de travail, à reprendre par le scénariste avec leurs formes au féminin) ; ils s'affichent sous le pseudo (le titre de succès choisi reste prioritaire si le joueur le préfère) :

| Classe | Voie | 30 | 50 | 70 | 90 |
|---|---|---|---|---|---|
| Mage | Archimage | Archimage | Haut archimage | Arcaniste majeur | Archimage souverain |
| Mage | Sorcier | Sorcier | Envoûteur | Maître des fléaux | Grand sorcier |
| Mage | Nécromancien *(nom au scénariste)* | Nécromancien | Lieur d'ombres | Seigneur des tombes | Nécromancien ancien |
| Garde | Palissade | Rempart | Bouclier vivant | Bastion | Muraille |
| Garde | Brèche | Brise-ligne | Bélier | Fléau des rangs | Avant-garde |
| Garde | Étau | Geôlier | Tenaille | Poigne de fer | Étau |
| Sentinelle | Mire | Viseur | Tireur d'élite | Œil sûr | Mire absolue |
| Sentinelle | Affût | Trappeur | Fauconnier | Maître piégeur | Affût |
| Sentinelle | Sente | Coureur des sentes | Bondisseur | Ombre légère | Sente |

**Les 3 sorts de chaque voie, répartis sur les paliers** (les contenus de §2.2 ne changent pas, seul le moment change) : à 30, le sort qui porte la fantaisie (Tir visé, Mâchoires, Bond, Interposition, Charge, Prise, Comète, Malédiction, Lever une ombre) ; à 50, le deuxième de la liste du §2.2, avec l'Évolution I ; à 70, le troisième, avec l'Évolution II. Ce découpage **rend la voie moins forte à 30** qu'un bloc de 3 sorts d'un coup, ce qui compte puisqu'elle arrive au début du contenu de la Forêt (31 à 45).

### v2.2 Ce qui change par rapport à la v1

| Sujet | v1 | v2 | Conséquence |
|---|---|---|---|
| Choix de la voie | 50 | **30** (la quête « Une route à soi » revient à son niveau d'origine) | les voies se mesurent dès **35, 40 et 45** (T96, `TemplateTests`, sur 9 voies au lieu de 3 classes), en plus de 50 ; le Disjoint (30) reste mesuré sans voie |
| Sorts de voie | 3 à 50 | **1 à 30, 1 à 50, 1 à 70** | moins de puissance à l'entrée, une récompense à chaque palier |
| Évolutions | 60, 75, 90 | **50, 70, 90** | la page Voie a 4 colonnes : 30, 50, 70, 90, plus 100 grisé |
| Second lien | 45 | **50** | un héros de 45 à 49 n'a qu'un lien : la migration retire le second (annoncé, changement gratuit 7 jours) |
| Conversion | 45 | **50** | les builds de référence Matière à 45 redeviennent des Transmutations ; l'avance de la Foudre à 45 devrait baisser |
| Affinages | 40 et 55 | **50 et 70** | |
| Clés de l'arbre | 45 | **50** | 16 valeurs `level` à changer dans `talents.json` ; les builds de référence à 45 perdent leur clé, et les mesures de 45 sont refaites. **Avantage** : au niveau 45 du chef de la Forêt, le build n'est plus « accompli juste quand le contenu s'arrête » (`suite-apres-d69.md` §12.1, point 1). Le socle 45-60 le porte |
| Points de talent | −2 (0 à 30) | **−1** (1 à 30) | à 30, l'arbre a un point à poser, comme dans l'esquisse de Charles |
| Nouveau | | une **frise des paliers** commune aux pages Éléments et Voie, des **titres de rang**, l'annonce au passage d'un palier | une story de plus (H1, 2 points) |

### v2.3 Le nouveau chiffrage

| Sprint | Stories | Pts |
|---|---|---|
| **SE1** (6) | E1, le cœur des éléments (aux niveaux v2 : liens à 30 et 50, Conversion 50, affinages 50 et 70), 4 ; E2, serveur et migration (le second lien retiré entre 45 et 49), 2 | 6 |
| **SE2** (5) | E3, la page Éléments et l'arbre sans portes, 3 ; E4, remesurer (clés à 50, points `1 + (niveau − 30) / 3`, builds de référence découpés), 2 | 5 |
| **SV1** (6) | V1, le moteur des voies (sorts et évolutions par palier : 30, 50, 70, 90), 4 ; V2, serveur, quête « Une route à soi » à 30, prix, `admin --spec`, 2 | 6 |
| **SV2** (5) | V3, la page Voie (colonnes 30, 50, 70, 90, et 100 grisé), 3 ; **H1, l'harmonisation** : la frise commune aux deux pages, les titres de rang, l'annonce du palier (bandeau et ligne du journal), les cernes de 70 et 90 (xUnit : chaque palier ouvre ses trois choses au bon niveau, et rien avant ; auto-test : la frise de chaque page dit le même prochain palier), 2 | 5 |
| **SV3** (6) | V4, la Sentinelle, 3 ; V5, la Garde, 3 (chacune mesurée à **35, 40, 45 et 50** : ±8 points de la moyenne de la classe contre les hordes, les planchers T41 des petits groupes tenus dès 35) | 6 |
| **SV4** (4) | V6, la Mage, 3 ; V7, la mesure croisée des 18 builds (9 voies × 2) à 45 et à 70, `mesures-arbre.md` v4, et le playtest de Charles à 30 (choisir sa voie et son lien dans la Forêt), 1 | 4 |
| **Total** | | **32 points, 6 sprints** |

Ces 32 points remplacent S181 à S183 (12 points), soit **+20 points au plan** (+17 en v1 : +2 pour l'harmonisation, +1 pour les mesures de 35 à 45). La Glace (S184) passe après SV4 et se pose dans la page Éléments, au palier 30 (lien Eau + Air) puis 50 (sa Conversion). Le sprint de gains rapides (inventaire, familier) reste avant SE1.

### v2.4 Les questions pour compléter « Puis 50… »

**Q6. Le rythme des paliers.**
- a) **30, 50, 70, 90, puis 100** : un pas de 20, 1 voie et 3 évolutions **(Reco)**
- b) 30, 50, 65, 80, puis 100 : le pas se resserre, les dernières évolutions viennent plus vite
- c) 30, 45, 60, 75, 90 : un pas de 15, qui suit les tranches de contenu (Forêt 30-45, Mines 45-60), mais 50 disparaît

**Q7. Ce que le palier 50 ouvre côté Éléments.**
- a) **Le second lien, la Conversion et le premier affinage de la tresse** : la « grande bascule » est le cadeau de 50 **(Reco)**
- b) Seulement la Conversion (le second lien passe à 70)
- c) Seulement le second lien (la Conversion passe à 70 : la Foudre convertie devient une récompense tardive)

**Q8. Le « passage de mage à archimage » à 30.**
- a) **À 30, on choisit sa voie parmi les 3** (Archimage, Sorcier ou Nécromancien) : passif, règle et 1er sort ; les évolutions suivent à 50, 70 et 90 **(Reco)**
- b) À 30, tous les mages deviennent « Archimage » (un rang commun, un passif léger) ; **le choix des 3 voies se fait à 50**
- c) Tout à 30 : la voie avec ses 3 sorts d'un coup, et des évolutions seules ensuite

---

## 0. En bref (v1)

| | Recommandation |
|---|---|
| **Page Éléments** | Les **quatre éléments de base restent ouverts** pour tous, du niveau 1 à 100 : le chapitre 1 ne bouge pas. À 30, le héros reçoit un **premier emplacement d'élément**, à 45 un **second**. Chaque emplacement accueille un **lien** (une paire, qui donne sa réaction) ou **la prédilection** (l'ancien Cœur, une seule). Le lien Feu + Air ouvre la **Foudre** : Transmutation dès 30, Conversion à 45, des affinages à 40 et 55. Aucun point n'est dépensé : seul le niveau débloque. Changer est gratuit jusqu'à 40, puis coûte 10 × niveau Éclats, dans un village |
| **Arbre des talents** | Il perd 15 nœuds (6 paires, 4 Cœurs, 5 de la tresse) et passe à 41. Les styles partent du tronc et l'Alchimie demande une réaction choisie dans la page Éléments. **Points de talent : 2 de moins** (0 à 30, 5 à 45, 23 à 100) |
| **Page Spécialisation** | On l'appellerait « **Voie** ». Pour chaque classe, **3 branches** (celles déjà conçues), choisies à 50 par la quête « Une route à soi », exclusives. Chacune a **3 évolutions linéaires à 60, 75 et 90**, qui s'ouvrent seules avec le niveau. Elle se lit de gauche à droite. La première voie est gratuite, les suivantes coûtent 30 × niveau Éclats |
| **Coût** | **29 points en 5 sprints**, qui remplacent S181 à S183 (12 points) : **17 de plus** au plan. La Glace (S184) attend après |

---

## 1. La page Éléments

### 1.1 Ce que le joueur doit ressentir

« Mes éléments, c'est **qui je suis en combat**. » Une page qui dit d'un coup d'œil avec quoi je frappe (mes points par élément), ce qui se passe quand je les mélange (mes réactions), et ce que je peux devenir (la Foudre). Aujourd'hui, tout cela est éparpillé : les points sont dans Caractéristiques, les réactions et la Foudre sont des nœuds de l'arbre (fourche, Cœurs, tresse), et la Foudre change le livre de sorts.

### 1.2 La question de fond : choisir ses éléments de base, ou pas ?

C'est le seul choix vraiment ouvert. Voici trois variantes.

| | **E1. Les quatre ouverts, des liens à 30 et 45** (Reco) | E2. Deux éléments maîtrisés, choisis tôt | E3. Un élément principal, choisi à 30 |
|---|---|---|---|
| Règle | Terre, Feu, Eau et Air restent ouverts pour tous. Les choix portent sur les **liens** (paires, prédilection, Foudre) | à 10, on choisit **2 éléments** sur 4 ; le grimoire ne garde que leurs sorts et les 8 neutres ; un 3e à 30 | les quatre restent ouverts ; à 30, un élément « principal » donne +10 % et −5 % aux autres (c'est l'ancien Cœur, rendu obligatoire) |
| Chapitre 1 et T41 | **inchangés** | à refaire : `BalanceTests`, T41 de 1 à 30, le Disjoint, le deck automatique | inchangés |
| Identité | moyenne avant 30, forte après | **forte dès 10** | faible (un pourcentage) |
| Deck de 12 | inchangé | 8 sorts d'élément et 8 neutres pour 12 places : c'est juste mais ça tient | inchangé |
| Coût | 11 points (§4) | environ **17 points** (+6 pour rééquilibrer le chapitre 1) | 9 points |
| Risque | le choix arrive tard (30) | un débutant se trompe à 10 sans connaître le jeu ; il faut un changement gratuit jusqu'à 30 | un « +x % » de plus, exactement ce qu'on veut sortir de l'arbre |

**Recommandation : E1.** Elle donne à Charles sa page Éléments, avec ses choix (feu, eau, terre, air, foudre), sans toucher aux 30 premiers niveaux, déjà réglés et mesurés. E2 est plus marquante, mais elle demande de rééquilibrer tout le chapitre 1 pour un gain que les joueurs ressentiraient surtout avant 30. On pourra y revenir avec la refonte complète de l'arbre.

### 1.3 Les règles (E1)

| Règle | Valeur | D'où ça vient |
|---|---|---|
| **Éléments de base** | Terre, Feu, Eau, Air, toujours ouverts, pour toutes les classes | inchangé |
| **Points d'élément** | ils restent dans l'onglet **Caractéristiques** (10 points par niveau, Vitalité ou un élément). La page Éléments les **affiche** (bonus, seuil du rendement), avec un lien vers Caractéristiques | `Characteristics`, inchangé |
| **Rendement décroissant (D85)** | inchangé : au-delà de **3 × niveau + 5** points dans un élément (155 à 50, 305 à 100), un quart seulement compte. La page le montre sur une barre par élément (« 120 / 155 : plein rendement ») | `Characteristics.Threshold`, `Effective` |
| **Emplacements** | **1 à 30, 2 à 45** | les portes coûtaient 1 point, et 2 paires au plus (C2) |
| **Un emplacement contient** | un **lien** (une des 6 paires) **ou** la **prédilection** (un élément : +10 %, −5 % aux autres) | les 6 paires et les 4 Cœurs de `talents.json` |
| **Contraintes** | deux liens ne partagent aucun élément (C2) ; une seule prédilection ; une prédilection et un lien peuvent cohabiter | `Talents.Clash`, `excludes` des Cœurs |
| **Le composé : la Foudre** | elle s'ouvre avec le lien **Feu + Air** (Électrisation). **Transmutation** (Feu → Foudre **ou** Air → Foudre) dès que le lien est posé ; **Conversion** à **45** (elle remplace la Transmutation) | la tresse de `talents.json`, mêmes niveaux qu'aujourd'hui |
| **Les affinages de la tresse** | **Prisme** ou **Signature appuyée** : **un des deux à 40, les deux à 55** | ils coûtaient 1 point chacun : sans prix, il faut un frein, et c'est le niveau |
| **Une seule grande bascule** | une seule Conversion dans toute la page ; 2 éléments ouverts au plus (paires transmutées ou converties) | `IsSwitch`, `MaxOpened` |
| **Les autres composés** | les 5 autres liens affichent une case « tresse à venir » (Glace d'abord, S184 décalé) | `elements.md` §4.3 |
| **Coût** | **aucun point** : le niveau débloque, le joueur choisit | voir §1.4 pour l'effet sur les points de talent |
| **Changer** | gratuit jusqu'à 40, puis **10 × niveau** Éclats (450 à 45), **dans un village**, hors combat. Retirer un lien retire sa tresse en cascade, et la page dit ce qui tombe avant de confirmer | `Talents.ChangeCost`, `Talents.Without` |
| **Le grimoire** | inchangé : une Transmutation remplace les sorts d'un élément par leurs jumeaux, la Conversion remplace ceux des deux parents ; les sorts perdus rendent leurs points de sort | `Talents.GrimoireOf`, `GivesBack` |

### 1.4 Les points de talent, une fois les éléments sortis de l'arbre

À 45, un build « Manière » dépensait 1 point en portes, et un build « Matière » (la Foudre convertie) en dépensait environ 6 (Électrisation 1, Conversion 2, Signature 1, Prisme 1, un second lien 1). Si les éléments deviennent gratuits et que les points de talent restent les mêmes, **tout le monde gagne des points**, la Foudre plus que les autres.

| | V1. Points inchangés | **V2. Deux points de moins** (Reco) | V3. Les éléments se paient en points de talent |
|---|---|---|---|
| Formule | `2 + (niveau − 30) / 3` | **`(niveau − 30) / 3`** | inchangée, un pot commun aux deux pages |
| À 30 / 45 / 100 | 2 / 7 / 25 | **0 / 5 / 23** | 2 / 7 / 25 |
| Effet | +1 à +6 points de puissance selon le build : hordes et plafonds de T41 à remesurer, risque fort | Manière : −1 point ; Foudre convertie : environ +3. Les niveaux et le second emplacement freinent la Foudre ; on mesure (§4, story E4) | aucun changement d'équilibre, mais deux pages pour un seul compteur : c'est déroutant, et ce n'est pas le « je choisis mes éléments » de Charles |

**À 30, l'arbre n'a plus de points** : la décision du niveau 30 devient le premier emplacement d'élément, et l'arbre s'ouvre à 33. C'est voulu, car à 30 l'arbre ne proposait déjà rien d'autre qu'une porte.

**Les leviers si la Foudre reste devant** (règle écrite d'avance, comme en S152) : si la mesure dépasse la médiane des autres liens de plus de 8 points, la Conversion passe à 50 ; si l'écart dépasse encore 8 points, la Transmutation passe à 35.

### 1.5 La migration des héros existants

Elle se fait **sans perte**, parce qu'on **déplace** les ids au lieu de les rendre :

1. `Talents.Version` passe à **4**. À la connexion, pour un personnage en version 3 ou moins : les ids des **paires, Cœurs et nœuds de la tresse** passent de `Talents` à `Elements` (même id, même règle) ; le reste de `Talents` est gardé s'il tient encore avec les nouvelles règles (`Talents.Problem` avec la nouvelle formule de points). Sinon, on retire **les derniers pris d'abord** (`Talents.Without`), et un mot l'annonce à la connexion (comme I5 a aujourd'hui).
2. **Les emplacements** : un héros de 30 à 44 qui avait **deux** paires garde la première et **perd la seconde**, puisqu'il n'a qu'un emplacement avant 45 ; c'est annoncé. Un héros qui avait une paire **et** un Cœur avant 45 garde la paire. À 45 et plus, tout tient.
3. **Les affinages** : un héros de 40 à 54 qui avait Prisme **et** Signature garde le premier pris.
4. **Le deck** est revu par `Grimoire.Replaced`, comme aujourd'hui.
5. **Plouf** (trois personnages de niveau 100) et **Plop** : tout tient au niveau 100. La commande d'administration gagne `--elements` pour les restaurer sans SQL (règle du compte de test).
6. Le **changement est gratuit pendant 7 jours** après la mise à jour, une fois (proposition : les joueurs découvrent deux pages nouvelles).

### 1.6 Ce qui change dans le cœur (vérifié dans le code)

| Fichier | Changement |
|---|---|
| `data/talents.json` | les 15 nœuds `Fork`, `Pure` et `Braids` gagnent un champ `page: "elements"`. Leurs règles, textes et ids ne changent pas. Les `requiresAny` qui listent les portes, sur les 10 nœuds d'étage 1 des cinq styles (Mêlée 2, Distance 1, Terrain 3, Tempo 3, Soutien 1), sont **retirés** ; ceux des 3 nœuds d'étage 1 de l'Alchimie (qui listent les paires) deviennent `requiresReaction: true` |
| `Hero.cs` | un champ `Elements` (liste d'ids, facultatif) ; une vue `Rules` = règles de `Talents` ∪ `Elements`, utilisée partout où l'on lit aujourd'hui `hero.Talents` pour combattre ; `Problem` appelle `Elements.Problem` ; `Equals` et `TalentsTaken` comptent les éléments |
| `Elements.cs` (nouveau) | `Slots(niveau)` (0, 1 à 30, 2 à 45) ; `Problem(ids, niveau)` : emplacements, C2 (`Talents.Clash` déplacé), une prédilection, la tresse et ses niveaux (Transmutation 30, affinage 40 et 55, Conversion 45), `MaxOpened`, une seule bascule ; `With` et `Without` (cascade, la Conversion qui remplace la Transmutation, repris de `Talents`) ; `ChangeCost` |
| `Talents.cs` | `Points` : `(niveau − 30) / 3` ; `Problem` refuse un nœud de la page Éléments, et vérifie `requiresReaction` contre `hero.Elements` ; `GrimoireOf` lit `hero.Rules` ; `Version = 4` ; `Clash`, `ElementsOpened`, `GivesBack` vont dans `Elements` ; `SecondFloor` est retiré (la règle ne s'appliquait plus) |
| `Fight.cs` | la `FighterSpec` reçoit `hero.Rules` au lieu de `hero.Talents` (ligne 272), ainsi que les PV des talents (ligne 253). Les réactions, la signature, le Prisme et la prédilection marchent alors tels quels |
| `TalentReasons.cs` | les refus `Door` et `Pair` passent à la page Éléments ; nouveau refus « il te faut une réaction (page Éléments) » |
| `TreeDraws.cs`, `Progression.Builds`, `Rpg.Sim` | un build devient un couple (éléments, talents) ; les 9 builds de référence sont découpés ; `--talents` tire d'abord les éléments |
| `Achievements.cs` | les succès qui visent une paire ou un Cœur (« a-heart »…) lisent `Talents` ∪ `Elements` |
| API (`Entities.cs`, `CharacterEndpoints.cs`, `Admin.cs`) | une colonne `Elements` (migration EF `AddElements`) ; `PUT …/points` reçoit les éléments ; la remise de version **déplace** au lieu de rendre ; `admin --elements` |
| Client | l'onglet Éléments dans `PointsPanel`, `PointsEditor`, `Texts` (FR et EN) ; `TreeLayout` sans la ligne des portes ni la colonne Fusion ; `BuildCode` en version 4 (les anciens codes sont refusés avec un message) |

### 1.7 La maquette de la page Éléments (dans la fenêtre de 1140 × 620)

```
┌ Élise · Mage niv. 52 ──────────── [Caractéristiques] [Sorts] [ÉLÉMENTS] [Voie] ──────────────────────────────┐
│ MES ÉLÉMENTS                                   │ MES LIENS                                                    │
│ Terre  ▓▓▓▓▓▓▓░░░░  110 / 161   +22            │ ① niv. 30  (■ Foudre · Feu + Air)  → Électrisé               │
│ Feu    ▓▓▓▓▓▓▓▓▓▓░  150 / 161   +30            │      Transmutation : (■ Feu → Foudre)   Conversion (·· 45 ✓)  │
│ Eau    ▓░░░░░░░░░░   20 / 161    +4            │      Affinage 40 : (■ Prisme)   Affinage 55 : (·· Signature)  │
│ Air    ▓▓▓▓▓▓▓▓░░░  130 / 161   +26            │ ② niv. 45  (■ Boue · Terre + Eau)  → Enlisé                   │
│ Au-delà du trait, un quart seulement compte.   │      tresse à venir                                          │
│ [Répartir dans Caractéristiques →]             │ Prédilection : aucune (un emplacement libre la prendrait)    │
│                                                │ Changer : 520 Éclats, dans un village                         │
└──────────────────────────────────────── [Enregistrer] [Fermer] ──────────────────────────────────────────────┘
```

---

## 2. L'arbre de spécialisation (la page « Voie »)

### 2.1 La structure

| Élément | Règle |
|---|---|
| **La racine** | la classe (Mage, Garde, Sentinelle), à gauche |
| **Les branches** | **3 par classe**, celles de `talents-arbre.md` §7.2 (9 en tout), une par ligne |
| **Le choix** | à **50**, après la quête « Une route à soi » ; **une seule branche** (elles s'excluent) |
| **Ce qu'une branche donne à 50** | un **passif**, une **règle de classe changée**, **3 sorts** (générés au barème par `spells_gen.py`), un **titre** |
| **Les évolutions** | **3 par branche, à 60, 75 et 90**, **linéaires** et **automatiques** : le niveau les ouvre, elles s'allument seules. Chacune fait **une** chose : monter le passif d'un cran, étendre la règle, ou améliorer un des 3 sorts |
| **Plus tard** | à 100, un **accomplissement** (1 parmi 2, v3 §7.1), qu'on dessine comme une 4e colonne grisée « plus tard » |
| **Coût** | aucun point : le niveau et la quête suffisent |
| **Changer de voie** | la première fois gratuite, ensuite **30 × niveau** Éclats (1 500 à 50), chez l'habitant des voies à Quatre-Chemins ; les rangs des sorts de voie sont **transférés** (décision de Charles, branche `s163-specialisations`) ; les évolutions suivent le niveau, il n'y a rien à racheter |
| **Équilibre** | chaque voie, avec ses évolutions, reste à **±8 points** de la moyenne de sa classe contre les hordes (300 combats), et gagne seule la salle 2 des donjons de la Forêt au moins 90 fois sur 100 ; au plus **+10 % de puissance** à 50 par rapport à un héros sans voie (S158 v8, T41) |
| **Séparation** | une voie ne nomme **aucun élément** (la règle de la v3) : la page Éléments décide avec quoi je frappe, la Voie décide mon métier |

**Pourquoi des évolutions linéaires.** Une évolution à choix double le contenu (54 évolutions au lieu de 27) au moment où le jeu n'a pas encore de contenu au-delà de 60. La seule vraie décision, c'est la branche. Les évolutions sont des **récompenses de montée** qu'on voit venir sur la page (« à 75 : Interposition se relance en 1 tour »). C'est la boucle macro que l'arbre des talents n'offre plus après 45. Le choix pourra arriver à 100, avec l'accomplissement.

### 2.2 Le contenu par classe

Notation : PA, PO (portée), relance ; « au barème » = les dégâts calculés par `spells_gen.py`. Les noms sont **des noms de travail** : le scénariste les revoit. Les passifs et les règles à 50 sont ceux de la v3 (§7.2), déjà validés en D80 ; les sorts et les évolutions sont nouveaux.

#### Sentinelle

| Branche | Niv. | Nœud | Effet |
|---|---|---|---|
| **Mire** (tir long) | 50 | Passif « Œil calme » | aucun ennemi à 3 cases au début du tour : +15 % de dégâts pendant 1 tour |
| | 50 | Règle | sorts lancés en vue : +1 PO |
| | 50 | Sorts | **Tir visé** (5 PA, PO 4 à 9, une cible, relance 2, au barème) ; **Visée** (2 PA, sur soi : le prochain sort ignore 15 % de résistance, relance 3) ; **Tir de barrage** (4 PA, PO 3 à 8, ligne de 3 cases, −1 PM, relance 2) |
| | 60 | Évolution I | la règle passe à **+2 PO** pour les sorts qui portent déjà à 6 cases ou plus |
| | 75 | Évolution II | **Tir visé** traverse sa cible : une case de plus derrière elle, à 50 % |
| | 90 | Évolution III | Œil calme : **+25 %**, pendant **2 tours** |
| **Affût** (pièges et Faucon) | 50 | Passif | le **Faucon** (`hawk`) reste 2 tours de plus |
| | 50 | Règle | ses glyphes durent +1 tour |
| | 50 | Sorts | **Mâchoires** (3 PA, PO 1 à 5, glyphe d'une case pendant 2 tours : −3 PM et dégâts au barème à qui y entre, relance 2) ; **Appât** (2 PA, PO 1 à 4, glyphe de 2 tours : −1 PA à qui y commence son tour, relance 3) ; **Piqué** (3 PA : le Faucon frappe une cible à 4 cases de lui, au barème, relance 1) |
| | 60 | Évolution I | le Faucon a **+30 % de PV** |
| | 75 | Évolution II | **deux Mâchoires** posées à la fois au lieu d'une |
| | 90 | Évolution III | un ennemi pris dans un piège est **marqué** : les coups de la Sentinelle lui font +15 % pendant 2 tours |
| **Sente** (mobilité) | 50 | Passif | frappée au contact : +2 PM pendant 1 tour |
| | 50 | Règle | ses sauts ne demandent plus la ligne de vue |
| | 50 | Sorts | **Bond** (2 PA, PO 2 à 4, saut, sans dégâts, relance 1) ; **Coup de talon** (3 PA, contact, au barème, puis elle recule de 2 cases) ; **Feinte** (2 PA, sur soi : le prochain ennemi qui la frappe au contact perd 2 PM, relance 3) |
| | 60 | Évolution I | **Bond** n'a plus de relance (une fois par tour) |
| | 75 | Évolution II | après un saut : +10 % au prochain sort du tour |
| | 90 | Évolution III | passif : **+3 PM**, et une fois par combat, le premier coup au contact est esquivé |

#### Garde

| Branche | Niv. | Nœud | Effet |
|---|---|---|---|
| **Palissade** (protéger) | 50 | Passif | au début de son tour, bouclier de **6** (+1 tous les 10 niveaux) aux alliés à 1 case |
| | 50 | Règle | ses boucliers sur des alliés valent **+25 %** |
| | 50 | Sorts | **Interposition** (2 PA, PO 1 à 3 : échange de place avec un allié, bouclier sur soi, relance 2) ; **Mur de boucliers** (4 PA, croix de 1 autour d'elle : bouclier aux alliés, relance 3) ; **Ancrage** (2 PA, sur soi : ne peut être déplacée, +15 % de résistance pendant 1 tour, relance 2) |
| | 60 | Évolution I | le passif porte à **2 cases** |
| | 75 | Évolution II | **Interposition** se relance en **1** tour |
| | 90 | Évolution III | une fois par combat, un allié à 2 cases qui tomberait reste à **1 PV** |
| **Brèche** (ouvrir la ligne) | 50 | Passif | ennemi achevé : +1 PM (déclencheur `Kill`, nouveau) |
| | 50 | Règle | ses attirances portent 1 case de plus |
| | 50 | Sorts | **Charge** (3 PA, ligne de 2 à 4 : saut jusqu'au contact et dégâts au barème, relance 2) ; **Fauchage** (4 PA, contact, croix de 1, au barème × 0,7) ; **Arrachement** (3 PA, PO 1 à 4 : attire de 2, −1 PM) |
| | 60 | Évolution I | ennemi achevé : **+1 PM et +1 PA** |
| | 75 | Évolution II | **Charge** se relance en **1** tour |
| | 90 | Évolution III | un ennemi attiré contre un obstacle subit **6 dégâts par case** de choc |
| **Étau** (tenir) | 50 | Passif | qui la frappe au contact perd 1 PM |
| | 50 | Règle | ses retraits de PA et de PM durent +1 tour |
| | 50 | Sorts | **Prise** (3 PA, contact, au barème, −2 PM pendant 2 tours) ; **Verrou** (2 PA, contact : l'ennemi ne peut plus se déplacer de lui-même pendant 1 tour, relance 3) ; **Écrasement** (5 PA, contact, au barème, +30 % contre un ennemi à 0 PM) |
| | 60 | Évolution I | le passif retire aussi **1 PA**, une fois par tour |
| | 75 | Évolution II | **Verrou** se relance en **2** tours |
| | 90 | Évolution III | les ennemis au contact perdent 1 PM au début de leur tour |

#### Mage

| Branche | Niv. | Nœud | Effet |
|---|---|---|---|
| **Archimage** (grands sorts) | 50 | Passif | +1 PA au tour suivant si elle n'a pas bougé |
| | 50 | Règle | ses sorts de 6 PA ou plus : −1 tour de relance |
| | 50 | Sorts | **Comète** (7 PA, PO 3 à 7, cercle de 1, relance 3, au barème) ; **Recueillement** (2 PA, sur soi : le prochain sort +25 %, relance 3) ; **Rayon** (5 PA, ligne de 4, sans ligne de vue, relance 2, au barème) |
| | 60 | Évolution I | le passif tient même si elle a fait **un pas** |
| | 75 | Évolution II | **Comète** laisse au sol une zone de 1 tour qui retire 1 PM à qui y entre |
| | 90 | Évolution III | une fois par combat, un sort de 6 PA ou plus coûte **3 PA de moins** |
| **Sorcier** (états) | 50 | Passif | ses états durent +1 tour |
| | 50 | Règle | ses poisons se cumulent (2 au plus) |
| | 50 | Sorts | **Malédiction** (3 PA, PO 1 à 6 : −15 % de dégâts infligés pendant 2 tours, relance 2) ; **Fiel** (3 PA, PO 1 à 5, poison au barème pendant 3 tours) ; **Contagion** (4 PA, PO 1 à 5 : copie les états de la cible sur les ennemis à 1 case, relance 3) |
| | 60 | Évolution I | poisons cumulables : **3** |
| | 75 | Évolution II | **Malédiction** touche une croix de 1 |
| | 90 | Évolution III | un ennemi sous 2 états ou plus reçoit **+15 %** de ses coups |
| **Nécromancien** *(nom au scénariste)* (invocations) | 50 | Passif | une ombre d'un ennemi tombé, une fois par combat |
| | 50 | Règle | 2 invocations en même temps |
| | 50 | Sorts | **Lever une ombre** (4 PA, PO 1 à 3 : une ombre de 25 % de ses PV, relance 3) ; **Lien funeste** (2 PA : une de ses invocations lui cède 20 % de ses PV) ; **Éclat d'ombre** (3 PA : une invocation explose, au barème, en croix de 1) |
| | 60 | Évolution I | ses ombres ont **+25 % de PV** |
| | 75 | Évolution II | **3** invocations |
| | 90 | Évolution III | le passif joue **deux fois** par combat |

**Les doublons avec l'arbre des talents** (Sorcier et Réaction prolongée, Mire et Artillerie, Archimage et Pied ferme…, voir `arbre-refonte-questions.md` P5) restent jusqu'à la refonte complète. Ils se cumulent sous le plafond de +35 % des talents, ce qui limite le risque. La mesure de chaque voie est faite **avec** les builds de référence qui les contiennent, pour que le cumul ne passe pas inaperçu.

### 2.3 La maquette de la page Voie, lue de gauche à droite

La fenêtre fait 1140 × 620 (`PointsPanel`, elle tient dans 1280 × 720). La zone de page fait environ **1 108 × 410 px**. Les nœuds font 64 px (les cibles tactiles en demandent 44), sur 3 lignes de 120 px, avec une fiche à droite de 300 px.

```
┌ Élise · Mage niv. 78 ─────────────── [Caractéristiques] [Sorts] [Éléments] [VOIE] ──────────────────────────────┐
│          50 · La voie          60 · Évol. I      75 · Évol. II     90 · Évol. III   100 │ ARCHIMAGE · à toi      │
│                                                                                         │ « Les grands sorts »   │
│        ┌(■■■■)═══════════════(■■■■)════════════(■■■■)┄┄┄┄┄┄┄┄┄┄┄(····)┄┄┄┄┄┄┄┄┄┄(░░) │ Passif : +1 PA si…     │
│        │ ARCHIMAGE            Un pas            Comète            Sort léger    plus   │ Règle : 6 PA et plus,  │
│        │ titre, 3 sorts       permis            qui reste         (niv. 90)     tard   │ −1 tour de relance     │
│  ┌───┐ │                                                                              │ Sorts : Comète,        │
│  │MAG│─┼(░░░░)┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄(░░░░)┄┄┄┄┄┄┄┄┄┄┄┄(░░░░)┄┄┄┄┄┄┄┄┄┄┄(░░░░)              │ Recueillement, Rayon   │
│  └───┘ │ SORCIER              Poisons ×3        Malédiction       Plus d'états       │ ────────────────────── │
│        │                                         en croix                            │ SURVOL : Évol. III     │
│        └(░░░░)┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄(░░░░)┄┄┄┄┄┄┄┄┄┄┄┄(░░░░)┄┄┄┄┄┄┄┄┄┄┄(░░░░)              │ niv. 90 · dans 12 niv. │
│          NÉCROMANCIEN         Ombres +25 %      3 invocations     Deux ombres        │ Une fois par combat,   │
│                                                                                         │ un sort de 6 PA ou     │
│  Changer de voie : 2 340 Éclats, chez l'habitant des voies (Quatre-Chemins)             │ plus coûte 3 PA de     │
│                                                                                         │ moins.                 │
└───────────────────────────────────────────────────── [Enregistrer] [Fermer] ────────────────────────────────────┘
```

Signes (les 4 aspects de l'arbre, D86) : `(■■)` pris ou allumé, `(··)` pas encore (un niveau), `(░░)` fermé (une autre voie est prise), `═` bois vivant, `┄` bois nu. **Avant 50**, la page montre les trois branches en « pas encore », avec la ligne « La quête "Une route à soi" ouvre ta voie au niveau 50 » ; **de 50 à la fin de la quête**, les trois têtes sont ouvertes et un clic choisit, avec une confirmation qui annonce le prix des changements suivants.

---

## 3. L'arbre des talents sans les éléments

### 3.1 Ce qu'il perd

| Perdu | Nœuds | Conséquence |
|---|---|---|
| **La fourche** (6 paires) | 6 | plus de portes : les styles n'ont plus de parent |
| **L'Élément pur** (4 Cœurs) | 4 | la prédilection passe à la page Éléments |
| **La tresse de Foudre** | 5 | la colonne Fusion disparaît |
| **La famille Matière** | toute | l'arbre n'est plus que la **Manière** : 6 styles, **41 nœuds** (dont 15 clés), plus le tronc |
| **La décision du niveau 30** | | elle passe à la page Éléments ; l'arbre a 0 point à 30 et s'ouvre à 33 |

### 3.2 Le minimum pour qu'il reste cohérent en attendant la refonte complète

1. **Les styles partent du tronc** : on retire de leurs nœuds d'étage 1 les `requiresAny` qui listent les portes. Dans l'espalier, la couronne est nourrie par le tronc.
2. **L'Alchimie demande une réaction** (un lien posé dans la page Éléments) : `requiresReaction`, vérifié dans `Hero.Problem` ; refus expliqué (« Il te faut un lien : page Éléments »).
3. **Les points** : `(niveau − 30) / 3` (§1.4).
4. **`TreeLayout`** : on retire la ligne des portes et la colonne Fusion, il reste 6 couloirs. Les garanties G1 à G13 restent testées sur le corpus.
5. **Les textes** qui citent les portes (bulles du tutoriel, gouttière « Portes », l'infobulle des styles, la description des Cœurs) sont revus en FR et en EN.
6. **On ne touche à rien d'autre** : ni le plafond de +35 %, ni les nœuds « % », ni les doublons avec les voies. Tout cela attend la refonte complète (`arbre-refonte-questions.md`).

---

## 4. Le découpage

**Ordre :** le sprint de gains rapides (inventaire, familier) d'abord, puis les Éléments (ils changent l'arbre, et les voies se mesurent avec les nouveaux builds), puis les Voies. Numéros à attribuer par le coordinateur ; ici, **SE1, SE2, SV1, SV2, SV3**.

| Sprint | Story | Tests | Pts |
|---|---|---|---|
| **SE1** (6) | **E1. Le cœur des éléments.** `Elements.cs` (emplacements, C2, prédilection, tresse et ses niveaux, cascade, prix) ; `Hero.Elements` et `Hero.Rules` ; `Fight` et `GrimoireOf` sur `Rules` ; `Talents.Points` à `(niveau − 30) / 3` ; `talents.json` : `page`, styles sans porte, `requiresReaction` | xUnit : un emplacement à 30 et deux à 45 ; deux liens qui partagent un élément refusés ; Transmutation sans lien refusée ; Conversion à 44 refusée, à 45 acceptée et remplaçant la Transmutation ; un seul affinage à 40, deux à 55 ; Alchimie sans lien refusée ; un combat identique avant et après pour un héros dont les ids ont été déplacés (même graine, même résultat) ; `GrimoireOf` inchangé pour un héros de la Foudre convertie | **4** |
| | **E2. Serveur et migration.** Colonne `Elements` (migration EF `AddElements`), `PUT …/points` avec éléments, version 4 qui **déplace** les ids (règles du §1.5), prix du changement, `admin --elements`, Plouf restauré | intégration PostgreSQL : un personnage en version 3 (Foudre convertie, Boue, Mêlée) passe en version 4 sans perte ; un héros de 40 avec deux paires garde la première ; refus d'un changement payant hors village ; le rejeu serveur accepte un combat joué avec les éléments | **2** |
| **SE2** (5) | **E3. La page Éléments et l'arbre sans portes.** Onglet Éléments dans `PointsPanel` (barres par élément et seuil, emplacements, tresse, prix, confirmation de cascade) ; `TreeLayout` à 6 couloirs ; bulles du tutoriel ; textes FR et EN | xUnit client : `PointsEditor` (poser, retirer avec cascade), `TreeLayout` G1 à G13 sans portes ; test des clés FR et EN ; auto-test Godot : l'onglet s'ouvre, un lien posé puis enregistré, l'arbre s'ouvre sans ligne de portes, aucun texte tronqué à 1280 × 720 | **3** |
| | **E4. Remesurer.** `Progression.Builds` découpés en (éléments, talents), `TreeDraws` et `Rpg.Sim --talents` ; T41 à 35, 40 et 45 ; J3 ; `mesures-arbre.md` v3 ; la règle des leviers de la Foudre (§1.4) appliquée si besoin | xUnit : `TemplateTests` et J3 verts ; la mesure hors CI est écrite dans `mesures-arbre.md` | **2** |
| **SV1** (6) | **V1. Le moteur des voies** (reprise de la branche `s163-specialisations`) : `data/specs.json` (branche, passif, règle, 3 sorts, 3 évolutions par niveau), `Hero.Spec`, passifs des héros (`Fight.PassivesOf`), déclencheur `Kill`, règles compilées comme un talent, évolutions appliquées par le niveau, rejeu | xUnit : une voie refusée avant 50 ou d'une autre classe ; une évolution absente à 59 et présente à 60 ; un `Kill` qui finit le combat ne se déclenche pas ; un combat avec une voie se rejoue à l'identique | **4** |
| | **V2. Serveur et quête.** Colonne `Spec`, migration EF, `PUT …/spec`, la première voie gratuite puis 30 × niveau, rangs transférés, la quête « Une route à soi » à 50, `admin --spec` | intégration PostgreSQL : choisir, changer et payer, rangs transférés, combat refusé avec une autre voie que celle du serveur | **2** |
| **SV2** (6) | **V3. La page Voie, de gauche à droite.** Remplace l'onglet Talents (le bouton de l'arbre des talents reste dans le panneau Menus et sur la touche T) ; trois lignes, 4 colonnes de niveaux, fiche à droite, confirmation et prix, état avant 50 | xUnit client : disposition (aucun chevauchement, nœuds ≥ 44 px, ordre fixe des branches), aspects ; auto-test Godot : la page s'ouvre, choisir une voie à 50, les évolutions allumées à 78 | **3** |
| | **V4. La Sentinelle** : Mire, Affût, Sente, 9 sorts (`spells_gen.py`, icônes `spell_art.py`), 9 évolutions, textes FR et EN | xUnit : chaque passif, règle et évolution ; mesure : chaque voie à ±8 points de la moyenne de la Sentinelle contre les hordes (300 combats), salle 2 des donjons de la Forêt ≥ 90 % | **3** |
| **SV3** (6) | **V5. La Garde** : Palissade, Brèche, Étau | les mêmes, plus un duo Palissade et dégâts à moins de 1,5 victoire sur 30 de plus qu'un duo dégâts et dégâts | **3** |
| | **V6. La Mage** : Archimage, Sorcier, Nécromancien (nom du scénariste) ; `Rpg.Sim --spec` : les 18 builds (9 voies × 2) à 50 et à 75 | les mêmes ; aucune des 18 sous le plancher T41 des petits groupes | **3** |
| | **Total** | | **29** |

**La gestion de projet** (à faire au moment du « go », hors de ce document) : 29 points en 5 sprints, à la place de S181 à S183 (12 points), soit **+17 points** ; la Glace (S184, 4 points) passe après SV3 et se pose dans la page Éléments ; D92, `PLAN.md`, `data/scrum.json`, R2, R8, `scopeDecisions`.

**Les risques**

| Risque | Parade |
|---|---|
| La Foudre gratuite devient trop forte (la Conversion ne coûte plus de points) | les niveaux 45 et 55, deux points de talent en moins, et la règle des leviers écrite d'avance (§1.4), mesurée en E4 |
| La migration perd quelque chose à un joueur | on déplace au lieu de rendre ; test d'intégration sur un personnage réel en version 3 ; changement gratuit pendant 7 jours |
| Le contenu des voies au-delà de 60 ne se teste qu'avec Plouf | les mesures à 75 se font sur les familles de synthèse (`Templates`), comme T96 |
| Deux onglets de plus dans une fenêtre déjà pleine | 4 onglets courts (« Caractéristiques », « Sorts », « Éléments », « Voie ») ; vérifié à 1280 × 720 par l'auto-test |
| Les textes : 9 voies × (titre, passif, règle, 3 sorts, 3 évolutions) ≈ 80 textes FR et EN, plus la page Éléments | les demander au scénariste dès SE1, et faire relire Charles en bloc |

---

## 5. Les questions pour Charles

**Q1. Les éléments de base.**
- a) Les quatre restent ouverts pour tous ; on choisit des **liens** à 30 et 45 (paires, prédilection, Foudre) **(Reco)**
- b) On choisit **2 éléments maîtrisés** à 10 (le grimoire ne garde que ceux-là), un 3e à 30 ; il faut rééquilibrer le chapitre 1 (environ +6 points)
- c) Un élément **principal** obligatoire à 30 (+10 %, −5 % aux autres)

**Q2. Les points de l'arbre des talents, une fois les éléments sortis.**
- a) **Deux de moins** : 0 à 30, 5 à 45, 23 à 100 **(Reco)**
- b) Inchangés : 2 à 30, 7 à 45, 25 à 100 (plus de puissance, il faut remesurer les plafonds)
- c) Les choix d'éléments se paient en points de talent (un pot commun aux deux pages)

**Q3. Les évolutions.**
- a) **3 par voie, à 60, 75 et 90, linéaires et automatiques** ; l'accomplissement de 100 (1 sur 2) plus tard **(Reco)**
- b) 2 par voie, à 65 et 85
- c) Chaque évolution est un choix entre 2 (54 évolutions au lieu de 27, environ +6 points)

**Q4. Le nombre de voies par classe.**
- a) **3**, celles déjà conçues **(Reco)**
- b) 2 (on garde Mire et Affût, Palissade et Brèche, Archimage et Sorcier), soit 3 points de moins

**Q5. Les noms des deux onglets.**
- a) « **Éléments** » et « **Voie** » : courts, et « Voie » rejoint la quête « Une route à soi » **(Reco)**
- b) « Éléments » et « Spécialisation »
- c) « Affinités » et « Spécialisation de classe »
