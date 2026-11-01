# Les pages Éléments et Spécialisation *(game designer, 1er novembre 2026)*

> **La demande de Charles (1er novembre)** : « Dans les caractéristiques, j'ai une page Talent, qui ne sert à rien actuellement. On va la remplacer par une page "Spécialisation de classe" (ou un autre nom) avec un arbre de spécialisation de classe (en gros un mage qui peut choisir 2/3 spécialisations qui auront chacune 2/3 évolutions et on verra plus tard). L'arbre doit se lire de gauche à droite. Il faudra rajouter une page "Éléments" (ou un autre nom) pour que le personnage choisisse ses éléments ici (feu, eau, terre, air, foudre etc.) et retirer les éléments de l'arbre des talents (on refera l'arbre des talents de A à Z plus tard). »
>
> **Ses choix** : je propose les règles des éléments et il les valide ; les spécialisations déjà conçues (passif, règle changée, 3 sorts, quête « Une route à soi » au niveau 50, `talents-arbre.md` §7) deviennent les **branches** de l'arbre de chaque classe, et je conçois 2 ou 3 **évolutions** par branche ; la refonte complète de l'arbre des talents est reportée ; un sprint de gains rapides (inventaire, familier) passe avant ces deux pages.
>
> **Ce document remplace**, pour la suite, les pistes de `arbre-refonte-questions.md` (elles serviront à la refonte complète de l'arbre, plus tard). Rien n'est planifié, et aucun fichier de code ni de données n'a été modifié.
>
> **Lu dans le code :** `Hero.cs` (`Talents`, `Problem`, `DeckOf`, `DeckLimit`), `Talents.cs` (`Points`, `Problem`, `Clash`, `MaxOpened`, `GrimoireOf`, `With`, `Without`, `ChangeCost`, `Version`), `Data.cs` (`Characteristics.Bonus`, `Threshold`, `Effective`, `For`), `Fight.cs` (la `FighterSpec` reçoit `hero.Talents`, et les PV des talents sont lus sur le héros), `CharacterEndpoints.cs` (`PUT …/points`, la remise de la version), `Entities.cs` (`Talents`, `TalentsVersion`), `PointsPanel.cs` (les onglets, la fenêtre de 1140 × 620), `data/talents.json`, `data/classes.json`. La branche locale `s163-specialisations` (1f8fe18) contient déjà le moteur des voies et la Sentinelle, avec des tests verts (mémoire du projet) : on la reprend, on ne repart pas de zéro.

---

## 0. En bref

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
