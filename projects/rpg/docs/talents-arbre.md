# Osmose : classes, sorts, arbre des talents et spécialisations *(proposition v3, 24 octobre 2026)*

> **Ce qui change depuis la v2** (la v2 reste dans l'historique git) :
> - **Les éléments suivent `docs/elements.md`, validé par Charles le 24 octobre.** Il y a 4 éléments de base, 6 paires, 4 triplets, Unisson (Saga II), et Lumière et Ténèbres en indépendants. Lumière n'est plus Terre + Feu et Ténèbres n'est plus Terre + Eau.
> - **L'arbre se range en deux familles.** La famille **A, Matière**, modifie son deck d'éléments. La famille **B, Manière**, ajoute des mécaniques sans changer d'élément.
> - On passe de **quatre à sept branches**. Trois sont nouvelles : **Pôles** (Lumière, Ténèbres, Pureté, Crépuscule), **Terrain** (glyphes, déplacement, placement, leurre) et **Soutien** (un sort lancé sur un allié l'aide au lieu de le frapper, selon son élément).
> - **Deux idées de Charles sont intégrées** : le **Soutien** (§4.3 et §4.6) et la **glyphe sur sort raté**, appelée **Rémanence** (G5, §4.6).
> - Il y a **trois nouveautés en Matière** : la **Transmutation** (remplacement partiel : « tes sorts d'Eau deviennent de la Glace »), la **Tresse triple** (clé) et **Crépuscule** (clé).
> - **Les réactions sont alignées sur les signatures des paires.** Attisé devient **Électrisé**.
> - Il y a **une seule clé de Matière** dans tout l'arbre, et deux éléments ouverts au plus.
> - Trois exemples de builds complets sont donnés (§4.5), le chiffrage est refait (§10) et seules les questions ouvertes restent (§11).
>
> **Demande de Charles** (v3) : « Il faut donc que l'arbre commun aux classes permette de choisir de modifier son deck d'éléments avec ces combinaisons d'éléments, ou d'ajouter des mécaniques, etc. » Puis : « L'arbre permet de débloquer de nouveaux éléments ou d'ajouter des mécaniques. »
>
> Ses deux idées, en substance :
> - **Des talents de support.** Un sort lancé sur un allié l'aide au lieu de le taper. Feu : soigne ; Terre : donne de l'armure ; Eau : donne des résistances ; Air : donne des PM ou de la PO. Variante : tout sort lancé sur un allié lui donne de la résistance dans l'élément du sort, plus un bonus de dégâts ou autre chose.
> - **Une glyphe sur sort raté.** Un sort lancé dans le vide pose une glyphe pendant 2 tours. Sa taille suit le coût du sort (1 PA : la case visée +1 case, 2 PA : +2 cases…). Plus un monstre est proche du centre, plus il prend de dégâts.
>
> **Demandes précédentes** (v2) : « pour l'arbre des talents, je veux un truc par exemple où tu peux choisir de nouveaux éléments (foudre, glace, lumière, ténèbres) ou que tu puisses ajouter des effets à des sorts (par exemple si tu as un talent débloqué, tu tapes un monstre eau puis terre, il devient "enlisé" et perd des pm pour x tour). Des talents qui donnent l'occasion de jouer multi élément par exemple. Ou des talents qui disent "tu perds 2PO sur tes sorts mais tu gagnes 10% dégâts de 0 à 3 cases", ou des talents qui disent "tu perds tes sorts feu et air mais tu gagnes 8 sorts foudre à la place" ou des combinaisons de ce genre. Un arbre de talents similaire à toutes les classes, qui influence les mécaniques. Par contre une sentinelle peut par exemple avoir accès à l'élément ténèbres comme le mage mais ils auront leurs sorts de classes ofc. » Puis : « je veux les classes, les sorts de classes, l'arbre des talents (communs aux classes) ainsi que les spécialisations (mage devient sorcier ou archimage ou nécromencien par exemple, au niveau 50, puis autre chose au niveau 100 etc.) ».
>
> **C'est une proposition de game design, pas un plan validé.** Rien n'est implémenté ni planifié. Les noms sont des **noms de travail**. Leur sens dans le monde revient au scénariste (`docs/talents-canon.md`, §9).

---

## Sommaire

1. Les quatre couches
2. Principes, fun et risques
3. Couches 1 et 2 : les classes et leurs sorts
4. Couche 3 : l'arbre des talents commun (deux familles, six branches)
5. Les éléments dans le moteur (renvoi à `elements.md`)
6. Les réactions élémentaires
7. Couche 4 : les spécialisations
8. Ce que ça change dans le moteur et le client
9. Le canon : conflits à trancher par le scénariste
10. Chiffrage, tranche verticale et place dans le plan
11. Questions ouvertes

---

## 1. Les quatre couches

| Couche | Ce que c'est | Quand | Ce qu'elle change | Ce qu'elle ne touche **jamais** |
|---|---|---|---|---|
| **1. La classe** | Sentinelle, Garde, Mage (inchangées) | à la création | PV, sorts de base, rôle de départ | |
| **2. Les sorts de classe** | les 24 sorts de la classe, **plus** ses sorts dans chaque élément ouvert (générés), **plus** ses sorts de spécialisation | niveaux 1 à 95 ; éléments ouverts par l'arbre ; spécialisation à 50, 75 et 100 | ce que le héros peut lancer | |
| **3. L'arbre des talents** | **le même arbre pour les trois classes**, à points, en deux familles : **Matière** (les éléments) et **Manière** (les règles) | dès **30** | les éléments, les réactions, la portée, le rythme, le terrain | aucun talent ne nomme une classe ni un sort précis |
| **4. La spécialisation** | une branche **de la classe**, choisie parmi 3 | **50**, approfondie à **75**, seconde étape à **100** | le **métier** : invocations, soins, boucliers, poussées, pièges | **aucun élément**, aucune réaction |

**La règle de séparation tient :** l'arbre décide avec quoi je frappe (Matière) et selon quelles règles (Manière). La spécialisation décide de mon métier.

---

## 2. Principes, fun et risques

### 2.1 Les principes

1. **Des talents qui changent des règles, pas des chiffres** : un monstre qui s'enlise, un sort qui perd 2 PO, des sorts d'Eau qui deviennent de la Glace.
2. **Deux façons de construire, lisibles d'un coup d'œil.** **Matière** : « je change ce que je frappe ». **Manière** : « je garde mes éléments et je change comment je joue ». Un héros peut rester tout en Manière, avec ses quatre éléments de base, et avoir un build complet.
3. **Le multi-élément paie, le mono-élément aussi.** Les composés et les réactions récompensent l'hybride. Pureté, Lumière, Ténèbres et Crépuscule récompensent celui qui se concentre (`elements.md`, §3.2).
4. **Chaque gain a un prix.** Il n'y a aucun talent sans contrepartie, sauf ceux qui ne donnent que des sorts (qu'on paie en points et en places de deck).
5. **On ne peut pas tout prendre** : des points rares, une clé par branche, une seule clé de Matière, deux éléments ouverts au plus.
6. **Le groupe compte** : les empreintes se complètent entre alliés.

### 2.2 Ce qui est fun

- **Le combo à deux temps** (Eau puis Terre : « Enlisé ! ») : une décision par tour, un retour visible.
- **L'échelle de la Matière** : **ouvrir** (j'ajoute de la Foudre), puis **transmuter** (mes sorts d'Air deviennent de la Foudre), puis **convertir** (je suis une Sentinelle de la Foudre). Chaque marche est une décision, et la bascule d'identité arrive au bout.
- **Les échanges de règles** (une Sentinelle au contact, une Mage qui ne frappe qu'en lumière et en ombre) : de l'expression personnelle et du bouche-à-oreille.
- **À plusieurs** : la Garde pose l'Eau, la Mage déclenche la Terre.

### 2.3 Ce qui est risqué, franchement

| Risque | Parade |
|---|---|
| **L'explosion du contenu** : 249 sorts d'élément à terme (`elements.md`, §4.2) | des sorts **transposés** par `spells_gen.py` ; un élément par chapitre, Foudre d'abord |
| **L'explosion combinatoire** : six branches, 3 classes × 3 spécialisations | des **builds de référence** (§4.5, 2 à 3 par classe) mesurés en CI ; le reste hors CI ; un plafond global des bonus (§4.4) |
| **La lisibilité** : 17 éléments, deux familles, sept branches | des familles **colorées** dans la fenêtre « Ma route » ; les triplets invisibles tant qu'ils ne sont pas ouverts ; la réaction annoncée dans la prévision **avant** le sort |
| **L'IA** : le héros simulé doit jouer les combos pour que les mesures veuillent dire quelque chose | `BestCast` valorise une réaction déclenchée et une empreinte posée (gourmand à un coup) |
| **L'équilibre T41** : une réaction est un statut gratuit | elle ne touche que la **cible visée** ; l'empreinte se consomme ; la cible reste « l'arbre à 45 vaut au plus +10 % de victoires » contre les petits groupes |
| **Crépuscule, le build « sûr » dominant** (aucune faiblesse de famille à exploiter, aucun mur) | des résistances Lumière et Ténèbres données aux familles au fil des chapitres, un build de référence en T41, et le repli **Demi-crépuscule** (`elements.md`, §3.4) |
| **Terrain empiète sur les spécialisations** (Affût et ses glyphes, Nécromancien et ses invocations) | Terrain donne des **règles de placement**, jamais un métier : le leurre n'attaque pas, les glyphes de Terrain posent une empreinte et ne font pas de piège |
| **Le soigneur gratuit** (Soutien) : des combats qui s'allongent sans fin | un soutien par tour, un soin plafonné à 15 % des PV max, pas de soutien sur soi ; un duo avec soutien mesuré contre un duo de dégâts (§4.6) |
| **Les glyphes géantes** (Rémanence) | un rayon plafonné à 3, une Rémanence par lanceur, des dégâts décroissants et toujours inférieurs à un coup (§4.6) |
| **Le rejeu serveur** | tout est déterministe ; talents et spécialisation voyagent avec le combat (T35) |
| **Le canon** | §9 |

---

## 3. Couches 1 et 2 : les classes et leurs sorts

**Les classes ne changent pas** : Sentinelle (distance, poussées, entraves), Garde (contact, attirance, boucliers), Mage (zones, soins, invocation). Un point reste ouvert : la Mage est dite « fragile » mais a le plus de PV (86 contre 74 et 62).

| Étage | Contenu | D'où viennent les chiffres |
|---|---|---|
| Les 24 sorts actuels | 4 par élément de base, 8 neutres | inchangés (règle de budget) |
| **Les sorts d'élément ouverts** | par classe : **8 par paire**, **4 par triplet**, **8 pour Lumière** et **8 pour Ténèbres**. Ils sont **transposés** des sorts de base : même portée, même coût, même zone, nouvel élément, signature (`elements.md`, §2 et §3.3) | `spells_gen.py` : la forme du sort de base, l'élément, la signature, les dégâts recalculés au budget (la signature coûte comme un effet) |
| **Les sorts de spécialisation** | 3 à 50, 1 à 75, 1 à 100 (§7) | règle de budget |

**Le deck reste à 12.** Ouvrir un élément ajoute des sorts au grimoire, pas des places. **Les 8 neutres ne sont jamais retirés** : ils portent l'identité de la classe.

---

## 4. Couche 3 : l'arbre des talents commun

### 4.1 La forme

```
FAMILLE A : MATIÈRE (modifier son deck d'éléments)    FAMILLE B : MANIÈRE (ajouter des mécaniques)

        TRESSES             PÔLES              RÉACTIONS     DISTANCE        RYTHME           TERRAIN          SOUTIEN
E1      T1 Ouverture ×6     P1 Cœur            R1-R6 paires  D1 Au plus près Y1 Coups appuyés G1 Pas de côté   S1 Main tendue
                            P2 Constance                     D2 Longue vue   Y2 Petits coups  G2 Adossé        S2 Bannière
                                                             D3 Pied ferme   Y3 Premier geste G3 Relais
E2      T2 Plein grimoire   P3 Ouverture L|T   R7 Tenace     D4 Zone serrée  Y4 Relance courte G4 Sceau        S3 Double main
        T3 Transmutation    P4 Plein pôle      R8 Catalyse   D5 Poussée lourde Y5 Réserve      G5 Rémanence    S4 Élan partagé
        T4 Signature app.   P5 Résonance       R9 Prolongée  D6 Pas dansant  Y6 Deck serré    G6 Leurre        S5 Longue main
        T5 Prisme
Clé     T6 Conversion  ┐    P6 Pureté     ┐    R10 Onde      D7 Corps à corps Y7 Un geste     G7 Terre brûlée  S6 Dévouement
        T7 Tresse triple┘   P7 Crépuscule ┘    R11 Partagée  D8 Artillerie   Y8 Économie      G8 Nomade        S7 Harmonie
         └── UNE SEULE CLÉ DE MATIÈRE ──┘      R12 Virtuose  D9 Ligne claire Y9 Souffle long  G9 Écho          S8 Relève

E2 : 2 points déjà dépensés dans la branche.   Clé : 3 points dans la branche, et elle coûte 2.
```

| Règle | Valeur |
|---|---|
| **Branches** | sept : **Tresses** et **Pôles** (Matière) ; **Réactions**, **Distance**, **Rythme**, **Terrain** et **Soutien** (Manière) |
| **Étages** | **E1** ouvert ; **E2** demande **2 points** dépensés dans la branche ; la **clé** en demande **3** |
| **Coûts** | 1 point par talent, **2 points par clé** |
| **Points** | **2 au niveau 30**, puis **1 tous les 5 niveaux** : 3 à 35, 4 à 40, **5 à 45** (première clé possible), 6 à 50… **16 à 100** |
| **Une clé par branche** | toujours |
| **Une seule clé de Matière** | une seule parmi T6 Conversion, T7 Tresse triple, P6 Pureté et P7 Crépuscule. C'est **la** décision d'identité du build |
| **Deux éléments ouverts au plus** | paires, triplets, Lumière ou Ténèbres. **Crépuscule compte pour deux** : c'est pour ça qu'il exclut les composés |
| **Exclusions locales** | D1 et D2 ; D7 et D8 ; Y1 et Y2 ; P1 Cœur et P2 Constance ne s'excluent pas |
| **Rien avant 30** | le chapitre 1 et `BalanceTests` ne bougent pas |
| **Ce qu'on prend à 100** | 16 points pour 66 tuiles : environ un quart de l'arbre, au plus 3 clés |
| **Prérequis** | les talents d'étage 2 et les clés de Soutien demandent S1 *Main tendue* |

**Un héros n'est jamais obligé de toucher à la Matière.** Le build « quatre éléments de base et tout en Manière » est complet (exemple de la Garde, §4.5).

### 4.2 Le changement de talents et son prix

- **Gratuit jusqu'au niveau 40**, puis **10 × niveau Éclats** pour tout refaire, **dans un village**, hors combat.
- Une conversion ou une transmutation retirée rend les sorts de base. Les sorts perdus **rendent leurs points de sort** (`RankList`, T34).

### 4.3 Les talents, chiffrés

Notation : « existe » = le moteur sait déjà le faire ; « nouveau » = une pièce à écrire (§8). Les éléments, leurs caractéristiques et leurs signatures sont dans `elements.md` ; on ne les recopie pas ici.

#### Famille A, Matière : modifier son deck d'éléments

##### Branche Tresses (les composés)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| T1 | **Ouverture : <paire>** *(une tuile par paire : Foudre, Glace, Boue, Magma, Vapeur, Sable)* | E1 | 1 | ajoute au grimoire **4 sorts** de la paire, transposés des 2 premiers sorts de chaque parent | une place de deck à trouver ; compte dans la limite de deux éléments | nouveau (élément) |
| T2 | **Plein grimoire** | E2 | 1 | les **4 autres** sorts de chaque composé ouvert (8 par paire), et les 4 sorts d'un triplet ouvert par T7 | aucune, sinon le point | existe (liste de sorts) |
| T3 | **Transmutation : <parent> → <paire ouverte>** *(le remplacement partiel)* | E2 | 1 | les **4 sorts d'un élément de base** deviennent leurs jumeaux du composé : « tes sorts d'Eau deviennent de la Glace ». Ils frappent sur la caractéristique du composé (Eau + Air) et portent sa signature | tu **perds cet élément de base** : plus d'empreinte, plus de réaction qui le demande, un recours de moins contre les résistances ; **aucun bonus de dégâts** ; un élément de base ne se transmute qu'une fois ; incompatible avec la Conversion de la même paire | existe (substitution de sorts) |
| T4 | **Signature appuyée** | E2 | 1 | les signatures de tes sorts composés valent **+50 %** (Arc 35 → 50 %) | tes sorts composés font **−10 %** de dégâts directs | nouveau (paramètre) |
| T5 | **Prisme** | E2 | 1 | tes coups de **paire** posent l'empreinte de leur premier parent (Foudre pose Feu, Glace pose Eau…) | ils ne déclenchent toujours pas ; jamais pour un triplet | nouveau |
| T6 | **Conversion : <paire>** | Clé | 2 | tu **perds les 8 sorts des deux parents** et tu gagnes les **8 sorts de la paire**, à **+15 %** et signature +50 % | il te reste 2 éléments de base ; clé de Matière ; paires seulement | existe (substitution) |
| T7 | **Tresse triple : <triplet>** *(Éther, Cendre, Sève ou Sel)* | Clé | 2 | ouvre un triplet dont tu as **ouvert une paire** : 4 sorts, caractéristique = 2 × la moyenne des parents, signature forte (`elements.md`, §2) | compte dans la limite de deux éléments ; clé de Matière | nouveau (élément) |

**L'échelle :** T1 ajoute, T3 remplace un parent, T6 remplace les deux. Un héros qui a pris T3 et veut convertir fait refaire sa transmutation : c'est voulu (deux marches, une seule identité).

##### Branche Pôles (Lumière, Ténèbres, un élément à fond)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| P1 | **Cœur : <élément>** *(n'importe quel élément que tu possèdes, Lumière et Ténèbres compris)* | E1 | 1 | ses sorts : **+10 %** | tous tes autres sorts d'élément : **−5 %** | nouveau (bonus par élément) |
| P2 | **Constance** | E1 | 1 | chaque coup **du même élément que ton coup précédent** sur la même cible : +5 % (au plus +15 %) | un changement d'élément remet à zéro ; c'est l'inverse exact des réactions | nouveau |
| P3 | **Ouverture : Lumière** ou **Ouverture : Ténèbres** *(l'une ou l'autre)* | E2 | 1 | 4 sorts de la polarité, sur la **plus haute** de tes quatre caractéristiques d'élément | compte dans la limite de deux éléments ; exclues entre elles | nouveau (élément) |
| P4 | **Plein pôle** | E2 | 1 | les 4 autres sorts de ta polarité (8) | | existe |
| P5 | **Résonance** | E2 | 1 | contre une cible **faible** à l'élément du coup : +10 % | contre une cible qui **résiste** : −10 % | nouveau (selon la résistance) |
| P6 | **Pureté** | Clé | 2 | deck d'**un seul** élément (neutres permis, Lumière ou Ténèbres tolérée comme neutre) : **+20 %** | **aucune réaction** ; clé de Matière | nouveau (condition sur le deck) |
| P7 | **Crépuscule** | Clé | 2 | tu **perds tes 16 sorts d'élément** et tu gagnes **8 sorts de Lumière et 8 de Ténèbres**, sur la **somme** de tes quatre caractéristiques d'élément (`elements.md`, §3.4) | **0 élément de base** : aucune réaction, aucun composé ; **aucun bonus** ; compte pour deux éléments ; exclut P3 et P4 (il les contient) ; clé de Matière | existe (substitution) |

#### Famille B, Manière : ajouter des mécaniques sans changer d'élément

##### Branche Réactions (alignée sur les paires, §6)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| R1 | **Enlisement** (Terre + Eau) | E1 | 1 | déclencher **Enlisé** (la grande Boue) | deux éléments dans le deck | nouveau (réactions) |
| R2 | **Transissement** (Eau + Air) | E1 | 1 | **Transi** (la grande Glace) | idem | nouveau |
| R3 | **Fêlure** (Terre + Feu) | E1 | 1 | **Fêlé** (le grand Magma) | idem | nouveau |
| R4 | **Électrisation** (Feu + Air) | E1 | 1 | **Électrisé** (la grande Foudre, remplace Attisé) | idem | nouveau (saut de dégâts) |
| R5 | **Buée** (Feu + Eau) | E1 | 1 | **Embué** (la grande Vapeur) | idem | nouveau |
| R6 | **Poudroiement** (Terre + Air) | E1 | 1 | **Aveuglé** (le grand Sable) | idem | nouveau (`Stat.Range`) |
| R7 | **Empreinte tenace** | E2 | 1 | tes empreintes durent **+1 tour** | | nouveau (paramètre) |
| R8 | **Catalyse** | E2 | 1 | tes réactions infligent en plus **20 %** des dégâts du coup qui les déclenche, en neutre | **une réaction au plus par tour** | nouveau |
| R9 | **Réaction prolongée** | E2 | 1 | les états que tu déclenches : **+1 tour** | tes coups qui déclenchent : **−10 %** | nouveau |
| R10 | **Onde** | Clé | 2 | tes zones déclenchent sur **2 cibles marquées de plus** | tes zones : **−15 %** | nouveau |
| R11 | **Empreinte partagée** | Clé | 2 | ton empreinte porte tes talents : un allié qui la complète déclenche **même sans le talent** | tu perds Catalyse | nouveau |
| R12 | **Virtuose** | Clé | 2 | compléter **ta propre** empreinte : **+20 %** | les empreintes de tes alliés ne réagissent plus pour toi | nouveau |

##### Branche Distance (inchangée depuis la v2)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| D1 | **Au plus près** | E1 | 1 | **+10 %** sur une cible à 0 à 3 cases | **−2 PO** à tous tes sorts (jamais sous la portée minimale ni sous 1) ; exclu avec D2 | existe, nouveau (selon la distance) |
| D2 | **Longue vue** | E1 | 1 | **+2 PO** aux sorts de portée max 4 ou plus | **−10 %** à 1 ou 2 cases | idem |
| D3 | **Pied ferme** | E1 | 1 | **+15 %** si tu n'as pas dépensé de PM ce tour | **−5 %** sinon | nouveau |
| D4 | **Zone serrée** | E2 | 1 | tes zones rétrécissent d'un cran, dégâts recalculés au budget | moins de cibles | existe (budget) |
| D5 | **Poussée lourde** | E2 | 1 | poussées **+1 case**, choc **8 par case** | sorts qui poussent **+1 PA** | existe |
| D6 | **Pas dansant** | E2 | 1 | chaque case parcourue : +4 % au prochain sort (au plus +16 %) | remis à zéro après le sort | nouveau |
| D7 | **Corps à corps** | Clé | 2 | portée max ramenée à **2**, **+25 %** | exclu avec D2 et D8 | existe |
| D8 | **Artillerie** | Clé | 2 | portée min **3** sur les sorts de dégâts, **+2 PO**, **+15 %** | rien à 1 ou 2 cases ; exclu avec D1 et D7 | existe |
| D9 | **Ligne claire** | Clé | 2 | tes sorts **en ligne** ne demandent plus la vue | les autres : −1 PO | existe |

##### Branche Rythme (inchangée depuis la v2)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| Y1 | **Coups appuyés** | E1 | 1 | sorts de 4 PA ou plus : +15 % | 3 PA ou moins : −15 % ; exclu avec Y2 | nouveau |
| Y2 | **Petits coups** | E1 | 1 | l'inverse | | |
| Y3 | **Premier geste** | E1 | 1 | premier sort de dégâts du tour : +20 % | les suivants : −5 % | nouveau |
| Y4 | **Relance courte** | E2 | 1 | −1 tour de relance | −25 % par tour retiré (budget) | existe |
| Y5 | **Réserve** | E2 | 1 | 2 PA non dépensés au plus passent au tour suivant | premier tour : −1 PA | nouveau |
| Y6 | **Deck serré** | E2 | 1 | **+1 PA** par tour | deck de **10** places | existe |
| Y7 | **Un geste par sort** | Clé | 2 | **+20 %** | chaque sort une fois par tour au plus | existe (`PerTurn`) |
| Y8 | **Économie** | Clé | 2 | sorts **−1 PA** (jamais sous 2) | deck de **8** places | existe |
| Y9 | **Souffle long** | Clé | 2 | **+1 PM et +1 PA** dès ton 4e tour | **−1 PA** tes 3 premiers tours | nouveau |

##### Branche Terrain (nouvelle : sol, déplacement, placement)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| G1 | **Pas de côté** | E1 | 1 | après un sort, **1 case de déplacement gratuite**, une fois par tour | **−1 PM** au début de chaque tour | nouveau |
| G2 | **Adossé** | E1 | 1 | adjacent à un obstacle : **+10 %** de dégâts et **+10 %** de résistance | sinon **−10 %** de dégâts | nouveau (voisinage) |
| G3 | **Relais** | E1 | 1 | **échanger de place** avec un allié à 2 cases, pour 1 PM, une fois par tour | −1 PO à tous tes sorts le tour où tu l'utilises | existe (échange) |
| G4 | **Sceau** | E2 | 1 | un sort neutre de plus (3 PA, relance 3) : une glyphe d'**un élément de ton deck**, au choix au lancer, qui **pose son empreinte** sur l'ennemi qui y commence son tour | une place de deck ; aucun dégât | existe (glyphes), nouveau (empreinte de glyphe) |
| G5 | **Rémanence** *(l'idée de Charles : la glyphe sur sort raté)* | E2 | 1 | un sort de dégâts lancé sur une **case vide** pose une glyphe de son élément pendant **2 tours**. Sa taille suit le coût du sort et ses dégâts décroissent du centre vers le bord (§4.6) | **une seule** glyphe de Rémanence par lanceur (la nouvelle remplace l'ancienne) ; tes coups sur une cible font **−5 %** | existe (glyphes), nouveau (décroissance) |
| G6 | **Leurre** | E2 | 1 | un sort neutre de plus (2 PA, relance 4) : un **leurre** (10 % de tes PV) qui **n'agit pas** et que les monstres à 2 cases préfèrent frapper | une place de deck ; il compte comme ton invocation (la Mage choisit entre sa braise et lui) | existe (invocation), nouveau (cible préférée de l'IA) |
| G7 | **Terre brûlée** | Clé | 2 | tes coups directs sur une cible **debout dans une de tes glyphes** : **+20 %** | sinon : **−10 %** | nouveau (condition) |
| G8 | **Nomade** | Clé | 2 | **+2 PM** | **−15 %** de PV max | existe (`Stat.Mp`, PV) |
| G9 | **Écho** | Clé | 2 | à la fin de ton tour, si tu as bougé d'au moins 3 cases, tu laisses sur ta case de départ un **écho** qui relance ton dernier sort de dégâts à **40 %** au début de ton prochain tour (sur la même cible si elle est à portée) | tu perds tes PM restants à la fin du tour ; l'écho ne pose pas d'empreinte | nouveau |

##### Branche Soutien (nouvelle : l'idée de Charles, chiffres au §4.6)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| S1 | **Main tendue** | E1 | 1 | tes sorts d'élément lancés sur un **allié** (invocations comprises) l'**aident** au lieu de le frapper, selon l'élément (Feu : soin ; Terre : bouclier ; Eau : résistance ; Air : PM et PO ; table au §4.6) | **un seul** sort de soutien par tour ; le sort garde son coût et sa relance ; tes sorts de dégâts font **−5 %** | nouveau (effet par élément sur allié) |
| S2 | **Bannière** | E1 | 1 | au début de ton tour, les alliés à 2 cases : **+5 %** de dégâts pendant 1 tour | toi : **−5 %** de dégâts | existe (`Stat.Damage`) |
| S3 | **Double main** | E2 | 1 | **deux** sorts de soutien par tour | chacun à **75 %** | nouveau (paramètre) |
| S4 | **Élan partagé** | E2 | 1 | l'allié soutenu fait **+10 %** sur son prochain coup | ton propre prochain coup : **−10 %** | existe (`Stat.Damage`) |
| S5 | **Longue main** | E2 | 1 | **+2 PO** aux sorts lancés sur un allié | **−1 PO** aux sorts lancés sur un ennemi | existe (portée) |
| S6 | **Dévouement** | Clé | 2 | effets de soutien **+50 %** | tes sorts sur les ennemis : **−20 %** | nouveau (paramètre) |
| S7 | **Harmonie** *(la variante uniforme de Charles, en clé)* | Clé | 2 | en plus de son effet, chaque soutien donne à l'allié **+15 % de résistance dans l'élément du sort** pendant 1 tour | tes soutiens coûtent **+1 PA** | existe (`Stat.Resistance`) |
| S8 | **Relève** | Clé | 2 | une fois par combat, un soutien sur un allié **sous 25 % de PV** a un effet **doublé** et ne coûte **aucun PA** | ton tour suivant : **−2 PA** | nouveau |

### 4.4 Les garde-fous de l'arbre

- **Plafond des bonus conditionnels** : la somme des pourcentages venus de l'arbre et de la spécialisation sur un même coup est plafonnée à **+35 %**. Les statuts de sorts (Concentration, Cri de guerre) restent hors plafond.
- **Le talent dominant** : un talent qui donne **plus de 1,5 victoire sur 30** de plus que ses voisins d'étage contre les petits groupes, ou qui fait passer une horde au-dessus de son plafond, est retouché avant livraison.
- **La fausse décision** : un talent pris par moins de 5 % des builds générés hors CI est revu.
- **L'équilibre des familles** : le meilleur build tout-Matière et le meilleur build tout-Manière à 45 doivent rester à **±1,5 victoire sur 30** l'un de l'autre. Sinon, une des deux familles est une fausse décision.

### 4.5 Trois builds complets

Les points : 6 à 50, 8 à 60, 16 à 100.

**Sentinelle « Orage fêlé », niveau 50, 6 points. Matière + Manière.**

| Branche | Talents | Points |
|---|---|---|
| Tresses | T1 Ouverture : Foudre, T4 Signature appuyée, T5 Prisme, **T6 Conversion : Foudre** (clé de Matière) | 1 + 1 + 1 + 2 |
| Réactions | R3 Fêlure | 1 |

*Comment elle joue :* ses sorts de Feu et d'Air sont devenus 8 sorts de Foudre (+15 %, Arc à 50 %). Elle a gardé la Terre et l'Eau. Avec Prisme, un coup de Foudre pose l'empreinte Feu. Un coup de Terre déclenche alors Fêlé (−15 % de résistance, 2 tours), et la Foudre qui suit en profite. *Ce qu'elle a payé :* plus de réaction Feu + Air ni Eau + Air, et un recours de moins contre une famille qui résiste à la Foudre (les gardiens moussus).

**Garde « Marée montante », niveau 50, 6 points. Tout en Manière, quatre éléments de base.**

| Branche | Talents | Points |
|---|---|---|
| Réactions | R1 Enlisement, R2 Transissement, R9 Réaction prolongée, **R11 Empreinte partagée** | 1 + 1 + 1 + 2 |
| Distance | D1 Au plus près | 1 |

*Comment elle joue :* au contact, elle pose de l'Eau. Ses alliés qui frappent en Terre ou en Air déclenchent Enlisé ou Transi, même sans le talent, pendant 2 tours. C'est la poseuse d'un groupe. *Ce qu'elle a payé :* −2 PO partout, −10 % sur ses propres déclenchements, aucun élément nouveau.
*À 100 (+10 points), une piste :* T1 Ouverture : Boue, T2 Plein grimoire, T5 Prisme, **T7 Tresse triple : Sel** (clé de Matière), G2 Adossé, G4 Sceau, G6 Leurre, Y3 Premier geste. Elle a alors deux éléments ouverts, Boue et Sel. Le Sel coupe les soins des monstres qui se soignent.

**Mage « Crépuscule », niveau 60, 8 points. Pôles + Rythme + Terrain.**

| Branche | Talents | Points |
|---|---|---|
| Pôles | P1 Cœur : Ténèbres, P2 Constance, P5 Résonance, **P7 Crépuscule** (clé de Matière) | 1 + 1 + 1 + 2 |
| Rythme | Y3 Premier geste, Y6 Deck serré | 1 + 1 |
| Terrain | G1 Pas de côté | 1 |

*Comment elle joue :* plus aucun sort d'élément de base. Elle a ses 8 neutres (dont les soins), 8 sorts de Lumière et 8 de Ténèbres, sur la somme de ses quatre caractéristiques. Les Ténèbres frappent à +10 % et la Ponction la soigne. La Lumière dissipe les boucliers des gardiens. Elle a 7 PA, un deck de 10, et fait un pas après chaque sort. *Ce qu'elle a payé :* aucune réaction, aucun composé, −5 % en Lumière, −1 PM par tour, deux places de deck. C'est aussi le build de référence qui surveille le risque « Crépuscule dominant ».

*Variante de groupe de la même Mage :* à 60, elle remplace Y6 et G1 par S1 Main tendue et S2 Bannière. La Lumière lancée sur un allié lui retire un état négatif, les Ténèbres le « voilent ».

### 4.6 Les deux idées de Charles : Soutien et Rémanence, chiffrées

#### Le Soutien

**Les deux variantes de Charles, comparées :**

| | **Par élément** (Feu soigne, Terre arme, Eau protège, Air donne des PM et de la PO) | **Uniforme** (résistance dans l'élément du sort + un bonus) |
|---|---|---|
| Décision à chaque tour | **réelle** : quel élément sur quel allié (soigner la Garde ou donner 2 PM à la Sentinelle) | faible : le sort importe peu |
| Lisibilité | une table de 4 lignes, le reste s'en déduit | une seule phrase |
| Équilibre | difficile : le soin du Feu tire tout vers lui | facile |
| Composés, Lumière, Ténèbres | une règle de calcul simple (ci-dessous) | automatique |
| Fantaisie | forte (« mon Eau protège ») | faible |

**Recommandation : la variante par élément**, avec plafonds. La variante uniforme n'est pas jetée : elle devient la clé **S7 Harmonie**, qui s'ajoute à l'effet de l'élément.

**La table**, où *D* est la moyenne des dégâts que le sort aurait faits, caractéristique comprise, avant résistance :

| Élément du sort | Effet sur l'allié (1 tour) |
|---|---|
| **Feu** | soin de **40 % de D**, au plus **15 % des PV max** de l'allié |
| **Terre** | bouclier de **60 % de D** |
| **Eau** | **+5 % de résistance à tous les éléments par PA** du sort (au plus +20 %) |
| **Air** | **+1 PM par tranche de 2 PA** du sort (au plus +2) et **+1 PO** |
| **Paire** | les effets **des deux parents à 60 %**. Exemples : Foudre = petit soin + PM ; Glace = résistance + PM ; Boue = bouclier + résistance ; Magma = soin + bouclier ; Vapeur = soin + résistance ; Sable = bouclier + PM et PO |
| **Triplet** | les effets **des trois parents à 40 %** |
| **Lumière** | retire **un état négatif** (réaction, retrait de PA ou de PM, poison) et soigne de 15 % de D |
| **Ténèbres** | **Voilé** : les monstres le ciblent **en dernier** pendant 1 tour, s'ils ont une autre cible |
| **Neutre** | inchangé : les sorts neutres gardent leurs propres effets |

Un sort de soutien ne pose **pas d'empreinte** et ne déclenche rien. Un héros Crépuscule ne soutient qu'en Lumière et en Ténèbres.

**Ce que ça change :**
- **En groupe**, des rôles naissent sans qu'on les impose : la Garde Terre qui arme, la Sentinelle Air qui donne 2 PM à la Garde pour qu'elle atteigne le chef, la Mage qui alterne Feu et Eau. C'est le plaisir propre du MMORPG.
- **En solo**, la branche ne vaut presque rien : seules les invocations (Faucon, braise, Leurre G6) sont des alliés. **C'est voulu, et je le dis franchement** : c'est une branche de groupe, et le joueur solo a six autres branches. J'ai écarté le **soutien sur soi-même** : un Feu qui se soigne seul ferait de chaque héros un soigneur gratuit. Le prix à payer est le changement de talents (gratuit jusqu'à 40, puis payant) pour qui passe du solo au groupe (question 4).
- **Les spécialisations** : la v2 n'avait aucune spécialisation soigneuse, et la Mage garde ses soins de base. **Le Soutien donne à tous un soin léger et toujours moins efficace, à coût égal, que les soins de la Mage** : un sort de soin dédié doit rester meilleur qu'un sort de dégâts retourné, ce que les mesures vérifieront sur `spells_gen.py`. Une spécialisation Baume, si Charles la veut, serait le soigneur de métier. Il y a une synergie à nommer : la règle de Palissade (+25 % aux boucliers sur les alliés) s'applique aux boucliers de Terre.

**Le risque du soigneur sans coût, et ses garde-fous :**
- **un soutien par tour** (deux avec S3, à 75 %) ;
- un soin **plafonné à 15 % des PV max** ;
- le sort de soutien **ne fait pas de dégâts**, ce qui coûte un tour de frappe ;
- **−5 %** de dégâts en permanence avec S1 ;
- les buffs ne durent qu'**un tour** ;
- les clés fortes (S6 Dévouement) coûtent **−20 %** de dégâts.

**La mesure** : dans `PartyBalanceTests`, un duo « dégâts + soutien » ne doit pas gagner plus qu'un duo « dégâts + dégâts » de plus de 1,5 victoire sur 30. Aucun groupe dur (6 à 10 monstres) ne doit **exiger** un soutien pour passer son plancher, sinon le soutien devient obligatoire.

**Mon avis :** c'est une bonne idée pour un MMORPG tactique (Wakfu et Dofus le prouvent avec leurs sorts à double usage), à condition que le soin reste **petit et plafonné**. Le vrai danger n'est pas un soin trop fort, c'est un combat qui s'allonge sans fin. Les plafonds ci-dessus sont là pour ça.

#### La Rémanence (glyphe sur sort raté)

| Paramètre | Valeur proposée | Pourquoi |
|---|---|---|
| **Quand** | un **sort de dégâts** qui **ne touche aucun combattant** (case vide, ou zone entièrement vide), lancé selon les règles habituelles (portée, vue) | « raté » s'entend sans ambiguïté |
| **Forme** | **losange** (la forme `Circle` du moteur), pas un carré | le carré n'existe pas dans le moteur et grandit trop vite (rayon 3 : 49 cases contre 25) |
| **Taille** | **rayon = coût du sort en PA, plafonné à 3** : 1 PA donne 5 cases, 2 PA 13 cases, 3 PA et plus 25 cases | l'idée de Charles jusqu'au plafond ; au-delà, un sort à 6 PA couvrirait la moitié d'une carte |
| **Au-delà de 3 PA** | chaque PA de plus donne **+10 %** aux dégâts de la glyphe (au lieu de l'agrandir) | les gros sorts gardent leur intérêt |
| **Dégâts** | à chaque déclenchement : **30 % des dégâts moyens du sort**, dans son élément, avant résistance | un raté doit valoir **moins** qu'un coup |
| **Décroissance** | selon la distance au centre (en pas) : **100 % au centre, 70 % à 1 case, 45 % à 2, 25 % à 3**, soit 30 %, 21 %, 13,5 % et 7,5 % d'un coup | « plus près du centre, plus de dégâts », sans mur au bord |
| **Durée** | **2 tours** du lanceur, fixes (la règle d'Affût ne la prolonge pas) | |
| **Déclenchement** | celui du moteur : entrer, atterrir ou commencer son tour dessus, **une fois par tour de jeu** et par combattant ; ennemis seulement | existe |
| **Cumul** | **une seule Rémanence par lanceur** (la nouvelle remplace l'ancienne). Celles de plusieurs héros coexistent, et chacune frappe une fois | un groupe de quatre a quatre glyphes au plus, chacune payée par un sort raté |
| **Élément et réactions** | l'élément du sort (composés, Lumière et Ténèbres compris), **sans signature**, **sans empreinte**, **sans réaction** | la règle du §6.1 (une glyphe ne pose pas d'empreinte, sauf Sceau) ; pas de champs qui retirent des PA |
| **Synergies** | G7 Terre brûlée compte ses glyphes (+20 % sur une cible dedans) | voulu, mais à mesurer |

**Ordre de grandeur :** un monstre qui commence deux tours au centre prend 60 % d'un coup, et 15 % s'il est au bord. Mais l'IA des monstres **évite déjà** les glyphes (`Ai.GlyphRisk`). La vraie valeur de la Rémanence est donc le **contrôle du terrain** : interdire un couloir, pousser un monstre hors de sa ligne de vue. C'est ce qui la rend intéressante.

**Ce que le moteur sait déjà faire** (S130, les cerfs et les gardiens moussus) : poser une glyphe sur les cases d'une zone (`Fight.Lay`), la frapper à l'entrée, à l'atterrissage et en début de tour une fois par tour (`StrikeAt`), la faire disparaître avec son lanceur, remplacer celle du même sort, et la faire éviter par l'IA (`GlyphRisk`, `GlyphWorth`). **Ce qui manque :**
1. le **centre** de la glyphe et la **décroissance** (aujourd'hui les dégâts sont les mêmes sur toutes les cases) ;
2. la clé de remplacement « Rémanence » plutôt que « même sort » ;
3. la **valeur d'une glyphe posée** pour le héros simulé (`BestCast`) ;
4. un `GlyphWorth` qui connaît la décroissance ;
5. **à vérifier** : qu'un sort à cible unique puisse viser une case vide.

C'est environ **2 points**, dans le sprint de Terrain.

**Mon avis :** c'est une très bonne idée. Elle transforme une frustration (un PA perdu) en décision : « je vise la case où il devra passer ». Mes réserves :
- **La lisibilité** : quatre losanges de 25 cases qui se chevauchent sur une carte. Parade : un contour à la couleur de l'élément et une teinte qui pâlit vers le bord.
- **La tentation de « rater exprès »** : elle est voulue, mais bornée par le −5 %, la glyphe unique et des dégâts toujours inférieurs à un coup.

---

## 5. Les éléments dans le moteur

**La référence est `docs/elements.md`** (validé par Charles le 24 octobre) : le tableau des 11 composés, leurs signatures, Lumière et Ténèbres, Crépuscule. Ce qui compte pour l'arbre :

| Question | Règle | Où |
|---|---|---|
| Caractéristique d'un composé | **2 × la moyenne des parents** (la somme pour une paire) | `elements.md`, §4.2 |
| Caractéristique de Lumière et Ténèbres | la **plus haute** des quatre (Ouverture) ; la **somme** des quatre (Crépuscule) | §3.2, §3.4 |
| Résistance des monstres | moyenne des parents pour un composé ; colonnes Lumière et Ténèbres à 0 par défaut ; une famille peut porter sa propre valeur | §3.2 |
| Statuts d'élément | un statut d'un parent ne vaut pas pour le composé ; un statut neutre vaut pour tous | v2, inchangé |
| Empreintes | seuls les coups de base en posent, plus les paires avec Prisme ; jamais les triplets, Lumière ni Ténèbres | §4.2 |
| Unisson | Saga II, hors de cet arbre | décision 4 |
| Ordre de livraison | Foudre, Glace, les autres paires, Lumière et Ténèbres, puis les triplets | §4.3 |

**Les doublons de noms (inchangés depuis la v2) :** « Éclair » (Air, Mage), « Chute céleste » (icône d'éclair) et les sorts de givre de l'Eau sont **renommés à l'arrivée du composé** qui porte leur mot.

---

## 6. Les réactions élémentaires

### 6.1 La règle (inchangée depuis la v2)

1. **L'empreinte** : un coup direct d'un sort de Terre, Feu, Eau ou Air (ou d'une paire avec Prisme, ou une glyphe de Sceau) laisse sur la cible l'empreinte de son élément. Il n'y a **qu'une empreinte** par combattant.
2. **Sa durée** : jusqu'à la fin du prochain tour de celui qui l'a posée.
3. **Le déclenchement** : un coup direct d'un **autre** élément de base déclenche la réaction de la paire, **si le lanceur a le talent**. L'empreinte peut venir de n'importe quel allié, invocations comprises.
4. **La consommation** : la réaction efface l'empreinte et n'en laisse pas de nouvelle.
5. **Sans le talent**, le nouveau coup **remplace** l'empreinte.
6. **Une zone** ne réagit que sur la **cible visée**, sauf avec Onde.
7. **Le cumul** : un même état se rafraîchit, des états différents coexistent.
8. **Les chefs** subissent les réactions ; leurs pertes de PA et de PM sont plafonnées à −2 en tout.
9. Les paires sont **non ordonnées** : 6 réactions, pas 12.

### 6.2 La table, alignée sur les paires (`elements.md`, §2.1)

| Paire | Composé (signature, chaque coup) | Réaction (deux coups, gratuite) | Durée |
|---|---|---|---|
| Terre + Eau | Boue, −1 PM | **Enlisé**, −2 PM | 1 tour |
| Eau + Air | Glace, −1 PA | **Transi**, −2 PA | 1 tour |
| Terre + Feu | Magma, −8 % de résistance | **Fêlé**, −15 % de résistance à tous les éléments | 2 tours |
| Feu + Air | Foudre, Arc 35 % | **Électrisé** : chaque coup reçu par la cible saute à 35 % sur l'ennemi le plus proche à 2 cases ; un saut par coup, une seule cible électrisée par lanceur | 1 tour |
| Feu + Eau | Vapeur, −10 % de dégâts | **Embué**, −25 % de dégâts infligés | 1 tour |
| Terre + Air | Sable, −1 PO | **Aveuglé**, −2 PO (jamais sous 1) | 1 tour |

**Repère de puissance :** Enlisé vaut le Pieu de la Sentinelle. Une réaction tous les deux coups vaut environ +10 % « en effets ».

### 6.3 Le jeu à plusieurs, et les monstres

- Celui qui complète déclenche avec **son** talent (ou celui du poseur, avec Empreinte partagée). Des rôles de « poseur » et de « déclencheur » apparaissent sans que personne ne les impose. `PartyBalanceTests` passe sur une composition qui se complète.
- **Les monstres ne posent ni ne déclenchent d'empreinte dans cette version.** Plus tard, ce sera une leçon de famille.

---

## 7. Couche 4 : les spécialisations (inchangées depuis la v2)

### 7.1 Les paliers

| Niveau | Palier | Choix | Ce qu'il donne |
|---|---|---|---|
| **50** | Spécialisation | 1 parmi 3 | un titre, un passif, 3 sorts, une règle de classe changée |
| **75** | Approfondissement | automatique | 1 sort de plus ; le passif monte d'un cran |
| **100** | Accomplissement | 1 parmi 2 | un sort majeur et une seconde règle changée |

Au-delà de 100 : rien dans la Saga I ; un palier 150 si la Saga II relève le niveau ; pas de prestige.

### 7.2 Les neuf spécialisations (noms de travail)

| Classe | Spécialisation | Métier | Passif (50) | Règle changée |
|---|---|---|---|---|
| Sentinelle | **Mire** | tir long | aucun ennemi à 3 cases : +15 % pendant 1 tour | sorts en vue : +1 PO |
| Sentinelle | **Affût** | pièges et Faucon | le Faucon joue deux fois de plus | ses glyphes durent +1 tour |
| Sentinelle | **Sente** | mobilité | frappée au contact : +2 PM | sauts sans la vue |
| Garde | **Palissade** | protéger | bouclier de 6 aux alliés à 1 case | boucliers sur alliés +25 % |
| Garde | **Brèche** | ouvrir la ligne | ennemi achevé : +1 PM (`Kill`) | attirances +1 case |
| Garde | **Étau** | tenir | qui la frappe au contact perd 1 PM | retraits de PM et de PA +1 tour |
| Mage | **Archimage** | grands sorts | +1 PA si elle n'a pas bougé | sorts de 6 PA ou plus : −1 tour de relance |
| Mage | **Sorcier** | états | ses états durent +1 tour | poisons cumulables (2) |
| Mage | **Nécromancien** *(canon, §9)* | invocations | une ombre d'un ennemi tombé, une fois par combat | 2 invocations |

Les sorts de spécialisation et les 18 accomplissements sont ceux de la v2 (historique git, §7.3 et §7.4).

### 7.3 Arbre et spécialisation sans explosion

1. **Des axes séparés** : aucun talent ne cible une spécialisation, aucune spécialisation ne cible un élément.
2. **Un seul plafond** de +35 %.
3. **Des synergies nommées et mesurées** : Sorcier et Réactions, Archimage et Rythme, Sente et Distance ou Terrain, Affût et Terrain. **Affût et Terrain** se chevauchent sur les glyphes : la règle d'Affût (+1 tour) vaut aussi pour les glyphes de Sceau, c'est une synergie, pas un doublon. Elle ne vaut pas pour la Rémanence (2 tours fixes). **Palissade et Soutien** : sa règle de boucliers vaut pour les boucliers de Terre (§4.6).
4. **Les mesures** : 3 classes × 3 spécialisations × **2 builds de référence** (un Matière, un Manière) = 18 builds à 50.
5. **Le deck de 12** est le frein naturel.

### 7.4 Le changement de spécialisation

La première fois est gratuite, ensuite **30 × niveau Éclats**. Changer d'accomplissement est gratuit dans un village. La quête « Une route à soi » passe de 30 à 50.

---

## 8. Ce que ça change dans le moteur et le client

| Pièce | Changement | S'appuie sur |
|---|---|---|
| **Éléments** | `Element` gagne les 6 paires, les 4 triplets, `Light` et `Dark` ; une table des parents ; `Characteristics.For` rend 2 × la moyenne des parents, la plus haute (Ouverture) ou la somme (Crépuscule) ; `FighterSpec.Resistance` rend la moyenne des parents ; deux colonnes de résistance facultatives | `Effects.cs`, `Data.cs` |
| **Empreintes et réactions** | une empreinte par combattant ; `data/reactions.json` ; `ReactionTriggered` ; un statut nommé qui se rafraîchit | statuts, file des passifs |
| **Statuts et effets nouveaux** | `Stat.Range` ; le saut de dégâts (Arc, Électrisé) ; le soin proportionnel (Ponction, Bourgeon) ; la dissipation (Lumière) ; la réduction des soins reçus (Sel) ; le sort sans ligne de vue (Éther) | `StatusEffect`, `HealEffect` |
| **Talents** | `data/talents.json` généré ; `Hero.Talents` ; `Hero.Problem` vérifie les points, les étages, une clé par branche, **une seule clé de Matière**, **deux éléments ouverts** (Crépuscule compte pour deux), le niveau ≥ 30 | `Hero.Problem` |
| **Talents « compilés »** | Ouverture, Transmutation, Conversion, Crépuscule, portée, zone, coût, relance : traduits avant le combat en grimoire et en sorts modifiés | règle de budget, `Spell` (`with`) |
| **Talents « vivants »** | distance, PM dépensés, premier sort, Constance, Résonance, Adossé, Terre brûlée, Écho : une liste de règles sur le `Fighter` lue par `Fight.Damage` | `Fight.Damage` |
| **Terrain** | glyphes posées par les héros (moteur S130) ; une glyphe qui pose une empreinte ; la **Rémanence** (centre et décroissance des dégâts, une par lanceur, un sort lancé sur une case vide) ; un leurre (invocation sans sort, cible préférée de l'IA) ; un écho (sort différé) | `Fight.Lay`, `StrikeAt`, `GlyphEffect`, `Ai.GlyphRisk` et `GlyphWorth` |
| **Soutien** | un sort d'élément lancé sur un allié se traduit en effet de soutien (soin, bouclier, résistance, PM et PO ; parents à 60 % ou 40 % ; dissipation ; Voilé) ; le compteur « un par tour » ; le plafond de soin ; un IA qui sait soutenir (valeur d'un soin ou d'un bouclier dans `BestCast`) et qui respecte Voilé dans le choix de cible | `HealEffect`, les boucliers, `StatusEffect`, `Ai.BestCast` |
| **Spécialisations** | `data/specs.json` ; `Hero.Spec`, `Hero.Feat` ; les passifs des héros ; `Kill` | `Fight.PassivesOf` |
| **IA** | `BestCast` valorise les réactions et les empreintes ; une seule fonction d'estimation des dégâts | `Ai.BestCast` |
| **Héros simulé** | `Progression.Built(…, build)` avec les builds de référence du §4.5 | `Progression.Built` |
| **Rejeu et serveur** | talents, spécialisation et accomplissement voyagent avec le combat ; `PUT …/talents` ; `--talents` et `--spec` pour Plouf | `FightRecord`, `Battles.Rebuild` |
| **Client Godot** | la fenêtre **« Ma route »** : deux familles colorées, sept colonnes, la prévision d'un soutien sur un allié, le losange dégradé de la Rémanence, des tuiles, le compteur de points, la clé de Matière signalée, les exclusions grisées ; la pastille d'empreinte ; le nom de la réaction en texte flottant et dans la prévision ; les couleurs d'élément ; la valeur de la caractéristique sur la fiche (« Glace : Eau + Air = 450 ») | `PointsPanel`, `Texts.SpellCard` |

---

## 9. Le canon : conflits à trancher par le scénariste

Je signale, je ne tranche pas.

1. **« Aucune voie ne porte un élément »** : la règle reste vraie pour les spécialisations, pas pour l'arbre.
2. **« Le héros est celui chez qui les éléments ne se mêlent pas »** : un composé est une **tresse** (deux ou trois éléments qui courent ensemble sans se confondre), et seul le héros sait la tresser. Les réactions ont lieu **dans la cible**.
3. **Lumière et Ténèbres** sont des **polarités** du héros (révéler, cacher), ni bien ni mal, qui ne viennent d'aucun Primordial (décision 6 de `elements.md`, à valider). Ténèbres ne touche jamais le métal sombre ni la Disjonction. **Éther** évoque un cinquième élément : le nom est à confirmer. **Unisson** est l'image de l'Accord, réservée à la Saga II.
4. **Les Fragments** ne donnent ni élément ni réaction.
5. **La conversion et Crépuscule** retirent des éléments de base. Avec la lecture du point 2, le héros les garde **dans** le composé. Pour Crépuscule, il les tient **sous** ses polarités. C'est la lecture la plus fragile, au scénariste de la juger.
6. **La nécromancie** : des ombres ou des échos des ennemis tombés, jamais des corps contraints. L'**Écho** de Terrain (G9) est un écho du héros, à ne pas confondre avec ceux-là : il faudra deux noms différents.
7. **Les noms de spécialisations** : une seule règle pour les neuf (métiers ou choses).
8. **Le moment** : la quête de passage passe à 50.

---

## 10. Chiffrage, tranche verticale et place dans le plan

### 10.1 La tranche verticale (5 points, inchangée)

**Enlisé (R1) et Au plus près (D1), pour la Sentinelle seule**, de bout en bout : l'empreinte, la réaction, deux talents (un compilé, un vivant), le serveur et le rejeu, une liste des talents dans le client, la pastille, « Enlisé ! » et la prévision, avec les tests unitaires, d'intégration (API) et T41 à 35. Les tests prévus sont ceux de la v2. À la fin, Charles joue la Sentinelle de Plouf et décide go ou pas go.

### 10.2 Le chiffrage

| Sprint | Pts | Contenu |
|---|---|---|
| **A1, tranche verticale** | 5 | §10.1 |
| **A2** | 5 | les composés dans le moteur (parents, 2 × moyenne, résistances, couleurs, l'Arc) ; **Foudre pour la Sentinelle** ; Ouverture, Transmutation, Conversion |
| **A3** | 3 | Foudre pour la Garde et la Mage, ses textes, les sorts de base renommés |
| **A4** | 5 | la fenêtre « Ma route » (deux familles, sept branches) ; les 5 autres réactions dont Électrisé ; `Stat.Range` ; le prix du changement ; la clé de Matière et la limite de deux éléments dans `Hero.Problem` |
| **A5** | 3 | Distance et Rythme ; la fonction unique d'estimation de l'IA |
| **A6** | 4 | **Terrain** : glyphes de héros, **Rémanence** (centre, décroissance, case vide ; ~2 points), Sceau, Leurre, Pas de côté, Adossé, Relais, Nomade, Écho |
| **A7** | 4 | **Soutien** : la table par élément, l'algèbre des composés, la dissipation (Lumière), Voilé (Ténèbres) et la cible choisie par l'IA, les plafonds, l'IA qui soutient ; les huit talents |
| **A8** | 3 | l'équilibre : les builds de référence du §4.5 dans `TemplateTests` (35 à 45), `PartyBalanceTests` avec empreintes partagées **et un duo avec soutien**, l'équilibre des deux familles ; un court playtest |
| **Arbre, sous-total** | **32** | 8 sprints (v2 : 24) |
| Les 5 autres paires | **16** | Glace 4 (avec ses textes et ses renommages), puis Boue, Magma, Vapeur et Sable à 3 chacune (signatures sur des statuts qui existent), un ou deux par chapitre |
| Pôles | **5** | Lumière et Ténèbres (Dissipation, Ponction, colonnes de résistance), Cœur, Constance, Résonance, Pureté, Crépuscule ; 16 sorts par classe |
| Triplets | **12** | 3 chacun (Éther : sans ligne de vue ; Cendre : glyphe ; Sève : soin ; Sel : soins réduits), après 50 |
| Spécialisations à 50 | **12** | moteur 3, puis une classe par sprint |
| Approfondissement à 75 | 3 | avec le chapitre 60 à 75 |
| Accomplissements à 100 | 9 | avec le chapitre 90 à 100 |
| **Total Saga I** | **89** | dont **32** maintenant, le reste étalé avec le contenu (v2 : 60) |
| Unisson | ~3 | Saga II, hors total |

L'écart avec la v2 (+29 points) vient surtout des **éléments** (5 paires, Pôles, 4 triplets contre 3 éléments en v2) et des branches **Terrain** et **Soutien**. La tranche verticale permet toujours de s'arrêter après 5 points.

### 10.3 La place dans le plan (proposition, rien n'est planifié)

- S135 (recettes) et S136 (quêtes de la Forêt et playtest) ne bougent pas.
- **S137 : la tranche verticale** (A1), avec un go ou pas go de Charles à la fin.
- **S138 à S144 : A2 à A8** si go. **Les guildes passent en S145** (le Soutien les rend plus utiles : autant qu'il arrive avant elles).
- Spécialisations et triplets avec les Mines (45 à 60) ; les autres paires et les Pôles dans les sprints de socle des chapitres suivants.
- **Planification**, par rapport à la base de la v1 (551 points, 137 sprints, Osmose 330) : l'arbre seul porte le projet à **583 points et 145 sprints**, Osmose à **362**. Les 57 autres points entrent dans les chapitres concernés. `PLAN.md`, `data/scrum.json`, les fichiers de sprint, R2, R8, `scopeDecisions` et `DECISIONS.md` sont **à mettre à jour après le go de Charles**. Rien n'est fait ici.

---

## 11. Questions ouvertes

Les sept questions sur les éléments sont tranchées (`elements.md`, « Décisions de Charles »). Restent :

| # | Question | Valeur par défaut |
|---|---|---|
| 1 | Les deux familles et les sept branches (Tresses, Pôles ; Réactions, Distance, Rythme, Terrain, Soutien), 3 étages, 2 points à 30 puis 1 tous les 5 niveaux (16 à 100), clés à 2 points | **oui** |
| 2 | **Une seule clé de Matière** dans tout l'arbre (Conversion, Tresse triple, Pureté ou Crépuscule) | **oui** ; *alternative :* deux à partir de 75 |
| 3 | La Transmutation (« tes sorts d'Eau deviennent de la Glace ») sans bonus de dégâts, après l'Ouverture de la paire, une fois par élément de base | **oui** |
| 4 | Soutien : la variante **par élément** (soin du Feu plafonné à 15 % des PV max, un soutien par tour), la variante uniforme en clé (Harmonie), **pas de soutien sur soi** (donc presque rien en solo, sauf sur les invocations) | **oui** ; *alternative pour le solo :* soutien sur soi à 50 %, sans le Feu |
| 5 | Rémanence : losange plutôt que carré, rayon = PA plafonné à 3, 30 % d'un coup au centre puis 70, 45 et 25 %, une par lanceur, sans empreinte | **oui** ; *alternative :* le carré de ta proposition, plafonné au rayon 2 (25 cases) |
| 6 | La branche Terrain, et en particulier le Leurre (qui n'attaque pas) et l'Écho : t'intéressent-ils, ou empiètent-ils trop sur Affût et Nécromancien ? | **oui, tels quels** ; *repli :* Terrain sans G6 ni G9 |
| 7 | Le niveau d'ouverture de l'arbre | **30** |
| 8 | Les monstres déclenchent-ils des réactions sur les héros ? | **pas maintenant** ; plus tard comme leçon de famille |
| 9 | Le prix des changements (le Soutien rend le passage du solo au groupe plus fréquent) | **arbre : gratuit jusqu'à 40, puis 10 × niveau ; spécialisation : la première gratuite, puis 30 × niveau** |
| 10 | Les spécialisations (3 à 50, 75 automatique, 1 parmi 2 à 100) et les six noms de la Sentinelle et de la Garde | **oui**, noms de travail |
| 11 | Le soin de la Mage : le Soutien donne à tous un soin léger ; faut-il en plus une spécialisation soigneuse ? | **non, les soins de base de la Mage et le Soutien suffisent** ; *alternative :* une quatrième, Baume |
| 12 | « Nécromancien » : des ombres des ennemis tombés plutôt que des morts relevés | **oui**, le nom au scénariste |
| 13 | La place dans le plan | **S137 tranche verticale avec go ou pas go ; S138 à S144 l'arbre ; guildes en S145** |
| 14 | Plouf | ses trois personnages de niveau 100 reçoivent leurs 16 points et choisissent en jeu ; `--talents` et `--spec` |

**Décision de Charles (24 octobre 2026) :** « go », les quatorze valeurs par défaut sont retenues. L'arbre est planifié (D80) : S137 tranche verticale avec un go ou pas go, S138 à S144 le reste de l'arbre, les guildes en S145.
