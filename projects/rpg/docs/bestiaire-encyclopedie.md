# Le bestiaire, première page de l'encyclopédie *(proposition du game designer, 30 octobre 2026)*

> **Demandes de Charles.** Le 29 octobre (5 captures de Wakfu), une encyclopédie à onglets, avec en premier le bestiaire : l'icône du monstre, son nom, sa famille, ses ressources en icônes avec le **% au survol**, son niveau, une **loupe qui montre la zone**, et des cases « butins conditionnés » et « seulement ce que je peux obtenir ». Le 30 octobre, sur la 1.0.157 : refonte de l'interface **et du contenu** (aujourd'hui « Loups (1/5) », une fiche en texte brut, et « ??? Jamais combattu : une silhouette » répété).
>
> **Périmètre.** Ce document fixe le **contenu** : ce que le joueur apprend, quand il l'apprend, et comment cela sert la boucle de jeu. Il chiffre les règles et la logique sans moteur (`Rpg.Core`, `Rpg.Client`) et le branchement Godot. La **mise en page** (tableau, fiche, filtres, icônes) est chiffrée à part par le graphiste.
>
> **Existant relu :** `Rpg.Core/Bestiary.cs` (T121, S154), `Rpg.Client/BestiaryPage.cs`, `godot/BestiaryPanel.cs`, `Rpg.Client/ItemInfo.cs` et `godot/ItemSheet.cs`, `data/monsters.json` (10 familles, 50 monstres, 5 zones, niveaux 1 à 45), `data/dungeons.json` (un donjon et une clé par famille), `data/achievements.json` (3 succès de bestiaire : 10, 25 et 50 monstres).

---

## 1. Pourquoi le bestiaire actuel ne marche pas

### 1.1 Le contenu

| Constat | Conséquence |
|---|---|
| Un monstre inconnu affiche « ??? » et « Jamais combattu : une silhouette. » : la **même phrase**, 5 fois par famille, sans aucune information | la page des inconnus ne dit pas **où aller** : elle est inutile au moment où elle pourrait donner envie |
| La fiche est une **liste de phrases** (« Résistances : Terre +10 %, Air −15 % ») | on ne compare pas deux monstres d'un coup d'œil ; on ne voit pas la faiblesse avant de l'avoir lue |
| Le butin est caché jusqu'à **25 victoires**, puis donné en mots (« rare »)… | … alors que la **fiche d'un objet** (`ItemSheet`, action Bestiaire) montre déjà, à tout le monde, quel monstre le lâche et **à quel pourcentage**. Le palier le plus long du bestiaire révèle **moins** que l'infobulle d'un objet : c'est une **fausse récompense** |
| Les sorts arrivent à **10 victoires** | la faiblesse (1 victoire) est utile, mais les sorts, qui disent comment le monstre frappe, arrivent tard : on les a déjà subis 10 fois |
| Le chef suit les mêmes paliers (25 victoires pour son butin) | un chef se combat en groupe, sur sa carte ou au fond du donjon : 25 victoires sur lui, c'est des heures pour lire ce qu'il lâche |
| Rien sur **où** le trouver précisément (cartes, taille des groupes), rien sur le **donjon** de la famille ni sur sa clé | le bestiaire ne sert pas à préparer une session de chasse |
| Aucune histoire | la famille n'a pas d'identité au-delà de son nom |

### 1.2 Ce qu'on garde

Le comptage côté serveur (un client ment), les victoires par monstre (une par présence dans un combat gagné), le palier « rencontré » à 0 victoire, l'ordre du monde, la touche B, `--bestiary full` pour Plouf, les 3 succès de collection, et le lien depuis la fiche d'un objet.

---

## 2. L'intention

**Le bestiaire est un carnet de chasse.** Il sert trois moments de la boucle :

| Boucle | Question du joueur | Ce que le bestiaire répond |
|---|---|---|
| **Micro** (le combat) | « Comment je le bats ? » | sa faiblesse, ses résistances, ses sorts, la leçon de sa famille, et **lesquels de mes sorts** le frappent bien |
| **Méso** (la session) | « Où je farme ceci ? » | qui lâche quoi, à quel %, sur quelles cartes, en groupes de quelle taille (solo ou à plusieurs), et la clé du donjon |
| **Macro** (les semaines) | « Qu'est-ce qu'il me reste à découvrir ? » | 50 monstres à rencontrer, à étudier puis à maîtriser, 10 familles et leur histoire, des succès de chasseur |

**La règle d'or du contenu :** un palier révèle une information **qu'on ne trouve nulle part ailleurs** et **qui sert au moment où on l'obtient**. Ce qu'une infobulle d'objet montre déjà gratuitement n'est jamais une récompense.

---

## 3. Les paliers de connaissance

### 3.1 Les cinq paliers

Les seuils comptent les victoires, comme aujourd'hui. **Le compteur ne change pas** : la refonte est rétroactive, et chaque personnage retrouve son avancée au palier que ses victoires lui donnent.

| Palier | Monstre ordinaire | **Chef** | Ce qu'on apprend (en plus du palier précédent) | À quoi ça sert |
|---|---|---|---|---|
| **0. À trouver** | jamais combattu | | une **silhouette** (le portrait en ombre), son **rôle** (icône), son **niveau**, sa **zone** avec la loupe, et le nombre d'objets qu'il lâche (en icônes « ? ») | savoir **où aller** : c'est une piste, pas un mur |
| **1. Rencontré** | combattu au moins une fois, même perdu | | nom, portrait en couleur, famille ; PV, PA, PM, initiative ; les **cartes** où vivent ses groupes, avec leur taille (« seul à 3 » ou « 6 à 10 ») | savoir s'il est à sa portée, seul ou en groupe |
| **2. Étudié** | **1** victoire | **1** | les **résistances** aux 6 éléments (une barre par élément), la **faiblesse** mise en avant, « tes sorts » (lesquels de ton deck frappent sa faiblesse ou butent sur une résistance) ; le **butin** en icônes, avec le **% au survol** (le même que la fiche d'objet) ; la clé de donjon de sa famille | préparer le combat suivant, et décider s'il vaut la peine d'être farmé |
| **3. Connu** | **5** victoires (au lieu de 10) | **2** | ses **sorts** (icône, élément, PA, portée, effet en une ligne), la **leçon de sa famille** (ses passifs), son **comportement** en une phrase selon son rôle (« fonce sur le plus proche », « reste à distance et recule », « soigne le plus blessé »…) | anticiper ses coups ; 5 victoires arrivent naturellement en suivant les quêtes de la zone |
| **4. Maîtrisé** | **25** victoires (inchangé) | **5** | sa **ligne d'histoire** (une phrase, par le scénariste), le **cadre doré** de son portrait, et il compte pour le succès de chasseur de sa famille | la collection : un objectif long, sans aucun gain de puissance |

**Butin conditionné.** Un objet de quête (`LootDrop.Quest`, 26 lignes dans les données, par exemple le Croc marqué des Loups pour « Bêtes agitées ») apparaît dans la liste du butin **seulement si la quête est en cours ou finie**, avec la mention « pour la quête … ». Avant, il n'existe pas pour le joueur : c'est une surprise de la quête, pas un spoiler du bestiaire. La case « butins conditionnés » du filtre (§4.2) les montre ou les cache.

### 3.2 Pourquoi ces seuils

Hypothèses (à vérifier sur les journaux de combat) : un combat solo dure 2 à 3 minutes ; un groupe solo compte 1 à 3 monstres d'une famille, tirés parmi ses 4 rôles ordinaires (le soutien jamais seul). Un monstre ordinaire donné apparaît donc environ **0,5 fois par combat** dans sa zone.

| Palier | Combats dans la zone (ordinaire) | Temps | Moment de la boucle |
|---|---|---|---|
| Étudié (1) | ~2 | 5 min | le premier groupe battu |
| Connu (5) | ~10 | 25 min | la première quête de la zone finie |
| Maîtrisé (25) | ~50 | ~2 h, **pour les 4 ordinaires de la famille à la fois** | un objectif de collection : ~20 h pour les 10 familles |

Le chef (sur sa carte, avec 5 à 9 des siens, ou au fond du donjon) : 1, 2 et 5 victoires. On le combat de toute façon une douzaine de fois pour obtenir une pièce de sa panoplie (8 % chacune) : 5 victoires arrivent avant.

### 3.3 Ce qui n'est **pas** une récompense de palier

- **Aucun bonus de puissance** (pas de « +5 % de dégâts contre les Loups ») : cela pousserait à farmer le bestiaire au lieu de jouer, et fausserait les mesures T41.
- **Pas de taux caché derrière un palier lointain** : le % est le même que celui de la fiche d'objet (question 80 a, voir §7, question 1).

---

## 4. Ce que montre la page

Le graphiste en décide la forme ; voici le contenu de chaque zone.

### 4.1 La liste (le tableau de l'encyclopédie)

Une ligne par monstre (50), dans l'ordre du monde par défaut :

| Colonne | Contenu | Palier minimum |
|---|---|---|
| Portrait | silhouette (0), en couleur (1), cadre doré (4) | 0 |
| Nom | « ??? » (0), puis son nom | 1 |
| Famille | le nom de la famille (connu dès qu'un de ses monstres est rencontré) | 0 ou 1 |
| Rôle | icône (Mêlée, Distance, Rempart, Soutien, Chef) | 0 |
| Niveau | « niv. 17 » | 0 |
| Zone | son nom et la **loupe** (ouvre la carte du monde sur la zone) | 0 |
| Butin | les icônes des objets (« ? » avant le palier 2), % au survol, la mention de la quête pour un objet conditionné | 2 |
| Palier | une pastille de 0 à 4, et au survol « encore 3 victoires : ses sorts » | 0 |

Le pied de liste : « 32 monstres sur 50 rencontrés · 12 maîtrisés ».

### 4.2 Les filtres et la recherche

- **Recherche** sur le nom du monstre **et sur le nom de ses objets** : taper « croc » donne les monstres qui lâchent un croc. C'est la question de farm la plus fréquente.
- **Zone** (5 cases), **niveau** de… à…, **rôle** (5 icônes), **palier** (« à trouver », « à étudier », « à maîtriser »).
- Cases : **« Butins conditionnés »** (les objets de quête), **« À mon niveau »** (niveau du héros ± 5).
- « Réinitialiser ». Tri par niveau, nom, zone ou victoires.

### 4.3 La fiche d'un monstre

Cinq blocs, de haut en bas ; un bloc pas encore débloqué affiche une seule ligne grisée, « Étudié à 1 victoire : ses résistances » (et non la même phrase répétée partout) :

1. **Identité** : portrait, nom, famille, rôle, niveau, palier et victoires.
2. **Au combat** : PV, PA, PM, initiative ; la barre des 6 résistances et la faiblesse ; « Tes sorts : 3 sur 8 frappent l'Air (sa faiblesse), 1 bute sur la Terre » ; puis ses sorts, sa leçon de famille et son comportement.
3. **Butin** : icônes, nom, %, quantité, conditions ; un clic ouvre la fiche de l'objet ; la clé du donjon avec son %.
4. **Où le trouver** : zone (loupe), cartes et taille des groupes, carte du chef, donjon de la famille.
5. **Histoire** : la ligne du monstre (palier 4).

### 4.4 La page d'une famille

En tête de la liste filtrée sur une famille (ou au clic sur son nom) : le **paragraphe d'histoire** de la famille (débloqué quand ses 5 monstres sont rencontrés), son **donjon** (nom, clé, qui la lâche), les **quêtes** qui demandent ses objets (connues seulement), et sa progression (« 5 rencontrés, 3 étudiés, 1 maîtrisé »).

### 4.5 Les liens avec le reste du jeu

| Depuis | Vers | Pourquoi |
|---|---|---|
| La loupe d'une zone | la carte du monde, centrée sur la zone, ses cartes à monstres marquées | « où » en un clic |
| Une icône de butin | la fiche de l'objet (et ses recettes) | farm → artisanat |
| La fiche d'un objet (action Bestiaire, qui existe) | la fiche du monstre, au lieu d'une ligne de texte | les deux sens |
| Un clic droit sur un groupe en exploration | la fiche de ses monstres | se préparer avant d'attaquer (limite notée en T121) |
| La faiblesse | ton deck de sorts | relie le bestiaire au build et à l'arbre (le choix des éléments) |
| Les succès | 10 succès « Chasseur de… » (les 5 monstres d'une famille maîtrisés) avec Éclats et XP (système S159) | la collection récompensée par le système existant |

---

## 5. Le plan, chiffré

Chaque story arrive avec ses tests xUnit et la mise à jour de l'auto-test du client. Le contenu se livre **avec** la mise en page du graphiste : B2 et B3 remplacent les phrases par des données structurées dont son tableau a besoin.

| # | Story | Fichiers | Tests | Pts |
|---|---|---|---|---|
| **B1** | **Les paliers.** `Knowledge` devient `Unknown`, `Met`, `Studied`, `Known`, `Mastered` ; seuils 1 / 5 / 25 et, pour un chef, 1 / 2 / 5 ; `Bestiary.Next(monstre, victoires)` (le prochain palier et ce qu'il reste) ; compteur inchangé, donc rétroactif | `Rpg.Core/Bestiary.cs` | xUnit : chaque seuil, ordinaire et chef ; `Full` donne `Mastered` ; un ancien compteur relu donne le bon palier ; contrat de l'API inchangé | **1** |
| **B2** | **La fiche structurée.** `BestiaryPage` rend un `BestiaryEntry` par blocs (identité, combat, butin, lieux, histoire) au lieu de phrases : les 6 résistances, la faiblesse, les sorts (élément, PA, portée, effet court), les comportements par rôle, le butin avec son %, ses conditions et la clé, les cartes et la taille des groupes (tirées de `MonsterGroups`, déterministes), la carte du chef, le donjon ; le butin conditionné selon les quêtes du héros | `Rpg.Client/BestiaryPage.cs`, `Texts` (FR et EN) | xUnit : ce qui est visible et caché à chaque palier ; un objet de quête absent sans la quête, présent avec ; le % égal à celui de `ItemInfo.Drops`. Auto-test : la fiche d'un inconnu (silhouette, niveau, zone) et celle d'un monstre maîtrisé (Plouf) | **3** |
| **B3** | **La liste, la recherche, les filtres.** `BestiaryQuery` : recherche sur les monstres et leurs objets, zone, niveau, rôle, palier, « butins conditionnés », « à mon niveau », tri, compteurs du pied | `Rpg.Client/BestiaryQuery.cs` | xUnit : « croc » trouve les Sangliers et les Loups ; chaque filtre seul et combiné ; le tri stable. Auto-test : une recherche réduit la liste | **2** |
| **B4** | **Les liens.** Loupe vers la carte du monde, icône de butin vers la fiche d'objet, fiche d'objet vers la fiche du monstre, clic droit sur un groupe vers ses fiches, « tes sorts » contre la faiblesse | `godot/BestiaryPanel.cs`, `WorldMapPanel`, `ItemSheet`, `Rpg.Client` (calcul des sorts efficaces) | xUnit : le compte des sorts efficaces sur des decks construits pour. Auto-test : la loupe ouvre la carte sur la bonne zone | **2** |
| **B5** | **L'histoire et la chasse.** 10 paragraphes de famille et 50 lignes de monstre en FR et en EN (à relire par le scénariste), débloqués selon §3.1 et §4.4 ; 10 succès « Chasseur de… » avec récompenses ; cadre doré du portrait | `data/monsters.json` (champs `lore`), `data/achievements.json`, `Rpg.Core/Achievements.cs` | xUnit : clés FR et EN complètes ; le succès se débloque au 5ᵉ monstre maîtrisé, pas avant ; l'histoire cachée avant son palier | **2** |
| | **Total** | | | **10** |

**Options, sur décision de Charles :**

| # | Option | Pts |
|---|---|---|
| B6 | **« Déjà obtenu »** : le serveur compte les objets réellement tombés, par personnage (migration) ; les icônes de butin obtenues sont cochées, et un filtre « ce qui me manque » s'ajoute. C'est la vraie option 80 a notée en limite de T121 | 2 |
| B7 | **Bestiaire par compte** : la connaissance d'un monstre est la meilleure de tous les personnages du compte (calculée par le serveur à la lecture, sans migration). Plus juste pour un MMORPG où l'on joue plusieurs personnages (Plouf en a 3) | 1 |

**Découpage proposé :** un sprint « le carnet de chasse » de **6 points** (B1, B2, B3), avec la mise en page du graphiste, puis un sprint de **4 points** (B4, B5), ou 5 à 7 avec B7 et B6.

---

## 6. Les risques

| # | Risque | Gravité | Parade |
|---|---|---|---|
| R1 | **Le volume de texte** : 60 textes d'histoire en deux langues, plus 5 comportements et les lieux | moyenne | B5 en dernier ; des lignes courtes (une phrase par monstre) ; le scénariste relit ; le test des clés casse la CI si une langue manque |
| R2 | **Le butin se lit dans les données**, pas dans ce qui tombe vraiment (limite de T121) | faible | B6 si Charles le veut |
| R3 | **Les hypothèses de temps du §3.2** | moyenne | mesurer sur Plouf et sur les journaux de combat avant de figer les seuils ; ils sont des constantes |
| R4 | **Le périmètre de l'encyclopédie** : Équipements, Consommables, Ressources, Cosmétique | moyenne | hors de ce document : le bestiaire est le premier onglet ; `BestiaryQuery` sert de modèle aux autres onglets, à chiffrer plus tard |
| R5 | **Les spoilers** | faible | le palier 0 ne montre que la silhouette, le niveau, le rôle et la zone ; les objets de quête restent cachés jusqu'à la quête |
| R6 | **Le monde s'agrandit** (le socle 45-60, les zones suivantes) | faible | tout est tiré des données ; seuls les textes d'histoire sont à écrire pour chaque nouvelle famille |

**Test de jeu** (Charles, sur la version livrée, avec un joueur qui n'a pas vu la fenêtre) : « Où trouver des crocs de loup ? » (moins de 15 s, par la recherche) ; « Avec quel élément frapper le Crapaud-roi ? » (moins de 10 s, par la faiblesse) ; « Quel monstre te reste-t-il à rencontrer autour du lac ? » (par le filtre du palier).

---

## 7. Les questions pour Charles

1. **Le % exact.** En S154, on avait choisi « jamais le taux, des mots » (question 80 a). Mais la fiche d'un objet montre déjà le %, et le 29 octobre tu as demandé le **% au survol**. Je recommande de montrer le % dès le palier « étudié » (1 victoire), partout pareil. D'accord ?
2. **Les seuils** : 1 / 5 / 25 victoires (et 1 / 2 / 5 pour un chef), à la place de 1 / 10 / 25 pour tous, et rétroactifs. D'accord ?
3. **Les inconnus** : la silhouette avec le niveau, le rôle et la zone (reco), ou rien du tout jusqu'au premier combat ?
4. **Par personnage ou par compte** (option B7, 1 point) ? Cette question n'avait jamais été posée ; je recommande **par compte**.
5. **L'histoire** (B5, 2 points, avec le scénariste) : oui pour une phrase par monstre et un paragraphe par famille ?
6. **« Déjà obtenu »** (option B6, 2 points) : veux-tu voir les objets déjà tombés, cochés, comme dans Wakfu ?

## 8. Recommandation

**Faire B1 à B5 (10 points), avec B7 (bestiaire par compte, 1 point), et garder B6 pour plus tard.** B1 à B3 règlent ce qui gêne aujourd'hui : les « ??? » répétés deviennent des pistes, la fiche se lit d'un coup d'œil et la recherche par objet répond à la question de farm. B4 et B5 relient le bestiaire au combat (la faiblesse contre ton deck), à l'artisanat (le butin vers les recettes), à la carte (la loupe) et aux succès. Et aucun palier ne révèle plus quelque chose que le jeu montrait déjà gratuitement ailleurs.

## 9. Décisions de Charles (30 octobre 2026, D87)
Paliers et seuils pris tels quels (1, 5, 25 ; un chef 1, 2, 5), le taux de butin visible dès « étudié », la silhouette avec niveau, rôle et zone pour un inconnu ; B7 (bestiaire par compte) pris, B6 (« déjà obtenu ») non pris. Sprints : B1 et B7 en S168, B2 en S169, B3 en S170, B4 et B5 en S171, avec la mise en page du graphiste (S169, S170).
