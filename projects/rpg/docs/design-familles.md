# Osmose : conception des familles, des sorts, des passifs et des zones

> **Version 2, 15 octobre 2026. Document de conception à remettre à Claude Code** pour le découper en sprints et l'implémenter dans `projects/rpg`.
> Rédigé après lecture de l'état réel du dépôt : `data/monsters.json`, `data/spells.json` (écrit par `tools/spells_gen.py`), `data/summons.json`, `data/classes.json`, `data/maps/`, `src/Rpg.Core/Effects.cs`, `Fight.cs`, `Monsters.cs`, `Data.cs`, `Ai.cs`, `DECISIONS.md` (T25 à T56), `data/quests.json`, `data/dungeons.json`, `docs/bible.md`. Revérifié au commit `40ffc91` (sprint 89).

## 0. Mode d'emploi pour Claude Code

**État du dépôt sur lequel ce document s'appuie :** commit `40ffc91` (sprint 89), décisions jusqu'à D68 et T56. Depuis la première lecture, les sprints 85 à 89 ont ajouté :
- des cartes plus lisibles (T52) ;
- de vrais donjons dont on parcourt les salles, avec PV conservés d'une salle à l'autre et une fontaine (T53) ;
- la fenêtre de fin de combat et les Éclats (T54) ;
- les quêtes et leur journal (T55) ;
- les premières quêtes, avec la quête principale d'Ysaline (T56).

**Aucun de ces sprints n'a touché aux monstres, aux sorts ni aux invocations** : les chiffres de §4 partent toujours des données actuelles. **Ne renommer aucun identifiant** de famille, de monstre, d'objet ni de donjon : les quêtes de `data/quests.json` en dépendent (« vaincre des monstres d'une famille », « finir un donjon »).

- **Ce document est une conception validée par le game designer, pas encore par Charles.** Les points marqués **[À TRANCHER]** au §9 ont une **valeur par défaut** : l'utiliser tant que Charles n'a pas dit autre chose, et la noter dans `DECISIONS.md`.
- **Ne rien inventer qui n'est pas écrit ici.** Si un cas n'est pas couvert, choisir la solution la plus simple, la noter dans `DECISIONS.md` (Limites) et la signaler dans la rétro.
- **Sources à modifier :** les sorts des monstres sont écrits par `tools/spells_gen.py` (fonction `S(...)`, `ranks=False`), jamais à la main dans `spells.json`. Les monstres sont dans `data/monsters.json`, les invocations dans `data/summons.json`. Les passifs vont dans un nouveau `data/passives.json`.
- **Les chiffres sont des points de départ.** Chaque sprint de données se termine par une **simulation `Rpg.Sim`** selon la règle de Charles (T41) : groupes de 1 à 3 gagnés seul au niveau de la zone (≥ 96 % en moyenne par classe), groupes de 6 et plus et chefs à ≤ 10 % seul. On règle d'abord les **PV**, ensuite les dégâts, et on ne touche jamais aux mécaniques sans le noter.
- **Combats enregistrés :** la correction du §2.1 change les combats où un héros s'est donné des PA ou des PM. Réenregistrer le combat d'entraînement (graine 7) si son rejeu ne va plus au bout, comme en T32.
- Le §7 (le monde de 30 à 100) est un **concept : ne rien implémenter** sans une décision de Charles.

---

## 1. Le constat

**Ce qui est solide :** les règles, l'IA qui planifie son tour, 4 zones de 25 cartes, 303 groupes, 8 donjons, le serrurier. Tout est en données et testé.

**Le problème de design n° 1 : les 40 monstres sont un seul monstre repeint.** Toutes les familles suivent le même gabarit :
- un tank avec **un bouclier sur lui-même de 10-12 PV pendant 2 tours, relance 2** (8 fois le même sort : `mud-wallow`, `thick-fur`, `hard-cap`, `rotting-hide`, `toad-skin`, `shell-up`, `wax-coat`, `burrow`) ;
- un soutien qui soigne ou protège un allié ;
- un boss avec un gros coup et une zone de rayon 1.

Résultat : on joue 8 fois le même combat avec de plus gros chiffres. Aucun monstre n'a de passif, et aucune famille n'utilise l'attirance, le retrait de PA, l'invocation ou la zone en ligne, que le moteur sait déjà faire. Il n'y a pas non plus de faiblesse élémentaire, donc rien ne pousse à changer de deck d'une zone à l'autre.

**Un bug trouvé pendant l'audit :** voir §2.1.

**Une correction pour la trame narrative** (`osmose-trame-narrative.md`) : elle s'appuie sur des « classes bi-élémentaires », ce qui n'est plus vrai depuis T34 (4 sorts par élément et par classe). Le fil « relation ≠ fusion » reste valable, et il est même plus fort : le héros est le seul être du monde qui manie les quatre éléments sans qu'ils se mélangent.

---

## 2. Spécification moteur (`Rpg.Core`)

Ces ajouts tiennent dans **un seul sprint**. Ils restent déterministes et rejouables par le serveur.

### 2.1 Correction : un état PA ou PM posé sur le combattant qui joue s'applique tout de suite
**Bug actuel :** `Fight.NextTurn` ne calcule `Ap` et `Mp` qu'au début d'un tour. Un état PA ou PM de 1 tour posé sur soi (`sidestep` Dérobade, `momentum` Élan, `mana-surge` Afflux, tous `turns: 1`) est retiré à la fin du tour où il est posé, sans avoir jamais compté. **Ces trois sorts de classe n'ont aujourd'hui aucun effet.**
**Règle :** quand un état `Ap` ou `Mp` est ajouté (sort ou passif) au combattant dont c'est le tour (`Current`), `Current.Ap` (ou `Mp`) change aussi immédiatement de `Value`, sans descendre sous 0. Le décompte de fin de tour est inchangé.
**Conséquence pour lire ce document :** « `turns` N » = le nombre de tours **du porteur** où l'état compte, **y compris le tour en cours si c'est lui qui joue**. Un état posé pendant le tour d'un autre compte à partir du prochain tour du porteur.
**Tests :** Dérobade donne 2 PM tout de suite ; un `Mp −1 turns 1` posé sur l'ennemi qui joue lui retire 1 PM tout de suite ; le même état posé hors de son tour lui retire 1 PM à son prochain tour seulement.

### 2.2 Les passifs
**Données :** nouveau fichier `data/passives.json`. Champ optionnel `"passives": ["id", ...]` sur chaque monstre (`MonsterSpec`) et chaque invocation (`SummonSpec`). Les `FighterSpec` portent la liste et la **famille** du monstre (`Family`, null pour les héros), qui servent au déclencheur `allyDies`.

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<Trigger>))]
public enum Trigger { TurnStart, Hit, LowHp, AllyDies, Death }

/// <summary>A monster's passive (data/passives.json): when it fires, and the spell effects it applies.</summary>
public sealed record PassiveSpec(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    Trigger Trigger,
    IReadOnlyList<SpellEffect> Effects,
    Area? Area = null,          // centred on the bearer; null = the bearer's cell only
    bool Melee = false,         // Hit only: the attacker stands next to the bearer
    int Percent = 0,            // LowHp only: 1 to 99
    int Range = 0,              // AllyDies only: the dead ally within that many steps (0 = anywhere)
    int DamageMin = 0, int DamageMax = 0, Element Element = Element.Neutral);
```

**Déclencheurs, exactement :**

| Déclencheur | Se déclenche quand | Ne se déclenche pas quand |
|---|---|---|
| `TurnStart` | au début du tour du porteur, **après** l'événement `TurnStarted` et après les poisons, s'il est encore vivant | le combat est fini |
| `Hit` | un **sort** inflige des dégâts au porteur (PV ou bouclier) et il survit. `Melee: true` : seulement si l'attaquant est à 1 case au moment du coup. **Une seule fois par sort lancé** | les dégâts viennent d'un poison, d'une collision ou d'un passif (pas de boucle possible) |
| `LowHp` | **une seule fois par combat**, la première fois que `Hp × 100 < Percent × Spec.Hp` après une perte de PV, quelle qu'en soit la source, s'il est vivant | — |
| `AllyDies` | un combattant de la **même équipe et de la même `Family`** (non nulle, pas une invocation) meurt, à `Range` pas ou moins du porteur vivant | le porteur est mort |
| `Death` | à la mort du porteur, centré sur sa case | la mort termine le combat (son équipe n'a plus personne) : le combat finit normalement |

**Résolution :**
- Les déclenchements sont **mis en file** pendant une action (un sort, un tic de poison, un début de tour) et **résolus à la fin de cette action**, dans l'ordre où ils ont été mis en file. Les passifs déclenchés par la résolution d'un passif s'ajoutent à la fin de la file (une explosion qui tue un autre bouffi le fait exploser à son tour). Chaque combattant ne meurt qu'une fois, donc la file se termine toujours.
- Les effets réutilisent **exactement** le code de `Fight.Cast` (soin, bouclier, poussée, attirance, état, invocation), avec le porteur dans le rôle du lanceur : la `Source` d'un état est l'`Id` du porteur, et la règle « renouvelé si même lanceur, sinon cumulé » s'applique.
- Les combattants touchés : `Affects.Caster` = le porteur. `Enemies`, `Allies` et `All` = les vivants sur les cases de `Area.Cells(porteur.Cell, porteur.Cell)` (avec `Circle`, la case du porteur n'en fait pas partie, sauf pour `Allies` et `All` qui **incluent le porteur** s'il est vivant). Nouvelle valeur `Affects.Attacker` (seulement avec `Hit`) = le lanceur du sort qui a déclenché.
- **Dégâts d'un passif** (si `DamageMax > 0`) : un seul tirage par déclenchement, sans critique, `Fight.Damage(tirage, porteur, cible, Element)` (donc avec la `Power` du porteur, ses bonus et la résistance de la cible). Ils touchent **tous les vivants des cases de la zone**, alliés compris, comme les zones de sorts. Sans `Area`, la zone est la seule case du porteur : les dégâts le touchent lui-même (c'est ce qui sert au Disjoint, §4.9). Ces dégâts ne déclenchent pas `Hit`.
- **Invocation par un passif** : `SummonEffect` gagne un champ optionnel `Count` (1 par défaut). Un passif pose `Count` créatures sur les cases libres **voisines du porteur**, dans l'ordre de `Cell.Steps`, puis, s'il n'y en a pas assez, à 2 pas ; il en pose moins s'il n'y a pas de place. Le `Max` du sort ne s'applique pas aux passifs. Les invocations meurent avec le porteur (règle existante).
- **Événements nouveaux :** `PassiveTriggered(int Fighter, string Passive)` avant les effets du passif. Le client affiche le nom du passif au-dessus du porteur, comme un chiffre de dégâts.
- **Statistiques de fin de combat** (`FightStats`, T54) : ce qui suit un `PassiveTriggered` est compté au **porteur** du passif, comme ce qui suit un sort est compté à son lanceur.

**Validation au chargement (`Data.cs`) :** passif connu ; `Percent` entre 1 et 99 seulement pour `LowHp` (0 sinon) ; `Melee` seulement pour `Hit` ; `Affects.Attacker` seulement pour `Hit` ; `Range` ≥ 0 seulement pour `AllyDies` ; effets dans les bornes de `Check(Spell)` ; `Count` de 1 à 4 ; au plus **3 passifs** par monstre.

### 2.3 Deux effets de déplacement
```csharp
[JsonDerivedType(typeof(LeapEffect), "leap")]
[JsonDerivedType(typeof(SwapEffect), "swap")]
/// <summary>The caster lands on the target cell, which must be a free floor cell.</summary>
public sealed record LeapEffect(Affects Affects) : SpellEffect(Affects);   // Affects = Caster
/// <summary>The caster and the fighter on the target cell trade places.</summary>
public sealed record SwapEffect(Affects Affects) : SpellEffect(Affects);   // Allies, Enemies or All
```
- `CheckCast` : un sort qui a un `leap` exige une case visée libre (nouvelle erreur `ActionError.TargetNotFree`). Un sort qui a un `swap` exige un combattant visé qui correspond à `Affects`, et qui n'est pas le lanceur (nouvelle erreur `ActionError.NoValidTarget`).
- Validation : `leap` et `swap` seulement dans un sort de zone `Point`. `leap` avec `Affects.Caster`, `swap` sans.
- Événements : `Leaped(int Fighter, Cell From, Cell To)` et `Swapped(int A, int B)`. Le client anime un saut (arc) et un échange (glissé croisé).
- `Affects = Allies` est déjà possible pour `pull` dans le moteur, mais `tools/spells_gen.py` force `Enemies` dans `pull()` et `push()` : ajouter un paramètre `affects`.

### 2.4 Les faiblesses
Une résistance négative est **déjà acceptée** par `Data.cs` et `Fighter.Resistance` (le plafond de 90 % est le seul garde-fou) : aucun code. À vérifier : la fiche de survol (`FightController.Info`) affiche `−15 %` et la prévision (`Foresee`) compte bien des dégâts plus forts.

### 2.5 L'IA (`Ai.cs`)
Trois ajouts, simples et testables :
1. **`leap`** : l'IA le traite comme un déplacement. Elle évalue chaque case libre à portée et prend le saut si le meilleur plan depuis cette case (avec les PA restants) vaut **1,5 fois** le meilleur plan sans sauter. C'est la règle qui existe déjà pour la marche.
2. **`swap` et `pull` sur un allié** : à lancer seulement si l'allié visé a **moins de 50 % de ses PV** et **un ennemi au contact**. Pour `swap`, il faut en plus que le lanceur ait plus de PV que l'allié.
3. **Les passifs ne sont pas prévus par l'IA** (ni par `Foresee`) : c'est accepté pour ce sprint. Le noter dans les Limites.

### 2.6 Le client (`Rpg.Client`, Godot)
- La fiche de survol d'un monstre liste ses passifs (nom, puis description au survol), comme les états.
- `PassiveTriggered` : le nom du passif flotte au-dessus du porteur (couleur neutre).
- `Leaped` et `Swapped` : animations simples.
- **Les invocations de monstres** utilisent des `look` de monstres (`wolf-melee`, etc.) : `Creatures` doit savoir les construire pour une invocation. Aujourd'hui, les invocations ont des looks de personnages (`female-a`, `male-e`). Les louveteaux sont dessinés à **70 %** de la taille (nouveau champ optionnel `scale` dans `SummonSpec`, 100 par défaut).

### 2.7 Tests attendus (xUnit, `Rpg.Core.Tests`)
Un test par ligne :
- Chaque déclencheur se déclenche une fois, au bon moment.
- `Hit` ne se déclenche ni par un poison, ni par une collision, ni par un passif.
- `Melee` est respecté.
- `LowHp` ne se déclenche qu'une fois, même soigné puis blessé de nouveau.
- `AllyDies` respecte `Range` et `Family`.
- `Death` ne se déclenche pas si le combat est fini, et les explosions en chaîne se résolvent dans l'ordre.
- Les auras de deux porteurs différents se cumulent, celle d'un même porteur se renouvelle.
- Une invocation par passif se pose sur des cases voisines libres, dans l'ordre.
- Saut sur une case occupée : refusé. Échange avec un ennemi quand `Affects = Allies` : refusé.
- Un combat avec passifs se rejoue à l'identique (`FightRecord`).
- La correction de §2.1.

---

## 3. La grammaire des familles

Chaque famille a :
1. **une leçon tactique** ;
2. **un passif de famille** partagé ;
3. **une faiblesse élémentaire** : −15 % pour les monstres, −5 % pour les chefs [À TRANCHER, §9] ;
4. **un tank qui protège ou contrôle**, au lieu du bouclier sur lui-même ;
5. **un soutien qui repositionne ou renforce** en plus de soigner ;
6. **un boss à deux phases** (passif `LowHp`) avec un signe narratif.

| Famille | Zone (niv.) | Élément | Faible à | Leçon | Passif de famille |
|---|---|---|---|---|---|
| Sangliers | Bois (3) | Terre | Air | ne pas s'aligner, se méfier des murs | `herd-grudge` (`AllyDies`) |
| Loups | Bois (3) | Terre / Air | Feu | séparer la meute | `pack-instinct` (aura) |
| Champignons | Bois moisi (10) | Terre / Eau | Feu | le poison, finir à distance | `final-spores` (`Death`) |
| Zombies | Bois moisi (10) | les quatre | Eau | varier ses éléments, ne pas rester au contact | `contagion-*` (`Hit` mêlée) |
| Grenouilles | Lac (18) | Eau / Air | Terre | le burst contre la régénération, les attirances | `damp-skin` (`TurnStart`) |
| Crabes | Lac (18) | Terre / Eau | Air | contourner une armure | `molt` (`LowHp`) |
| Abeilles | Champs (26) | Air / Feu | Terre | le tempo, ne pas réveiller l'essaim | `alarm` (`Hit`) |
| Taupes | Champs (26) | Terre | Eau | le terrain, l'ennemi qui surgit | `bury` (`LowHp`) |

Chaque élément est la faiblesse de deux familles exactement.

---

## 4. Les données, famille par famille

**Conventions des tableaux de sorts** (dans l'ordre des arguments de `S(...)`) : id · nom fr · nom en · PA · portée min-max · ligne de vue · en ligne · dégâts min-max · lancers par tour · élément · relance (`cd`) · zone · effets. Tous les sorts de monstres sont au **niveau 1**, `ranks=False`, critique 10. Les effets s'écrivent `status(Affects, Stat, valeur, tours[, élément])`.
- **(mod)** = un sort existant modifié : seules les colonnes indiquées changent.
- **(new)** = un sort nouveau.
- Un sort existant qui n'est plus utilisé par aucun monstre est **supprimé** de `spells_gen.py` : `mud-wallow`, `thick-fur`, `hard-cap`, `rotting-hide`, `plague-wave`, `toad-skin`, `wax-coat`, `burrow`. Vérifier d'abord qu'aucune classe ni invocation ne s'en sert.

**Conventions des tableaux de monstres :** les PV, PA, PM, l'initiative, le niveau et le butin **ne changent pas**, sauf mention. La colonne Résistances **remplace** l'objet `resistances` entier.

### 4.1 Sangliers (`boar`), Bois de Clairval
*Narratif : ils mâchent des éclats de pierre gravée qui vibrent ; le roi en a un fiché dans la défense (premier indice de la trame).*

| Monstre | Sorts (dans l'ordre) | Résistances | Passifs |
|---|---|---|---|
| `boar-charger` | `gore`, `charge` | Earth 10, Air −15 | `herd-grudge` |
| `acorn-piglet` | `acorn-throw`, `nip` | Air −15 | `herd-grudge`, `scamper` |
| `old-loner` | `gore`, `trample` | Earth 25, Air −15 | `herd-grudge`, `mud-hide` |
| `boar-sow` | `soothing-grunt`, `sow-call`, `nip` | Air −15 | `herd-grudge` |
| `boar-king` | `royal-gore`, `stampede`, `royal-charge` | Earth 20, Fire 10, Water 10, Air −5 | `herd-grudge`, `etched-shard` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `charge` (mod) | Charge / Charge | 4 | **2-4** | oui | oui | **6-9** | 1 | Earth | 0 | — | push(Enemies, **2**) |
| `trample` (new) | Piétinement / Trample | 3 | 1-1 | oui | non | 3-5 | 1 | Earth | 1 | Line 1 | status(Enemies, Mp, −1, 1) |
| `sow-call` (new) | Rappel des petits / Call the Young | 2 | 2-4 | oui | non | 0-0 | 1 | Neutral | 2 | — | pull(**Allies**, 2) |
| `royal-charge` (new) | Charge royale / Royal Charge | 4 | 2-5 | oui | oui | 8-11 | 1 | Earth | 2 | — | push(Enemies, 3) |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `herd-grudge` | Rancune de la harde / Herd Grudge | AllyDies | Range 3 | status(Caster, Mp, +1, 2) ; status(Caster, Damage, +15, 2) |
| `scamper` | Détale / Scamper | Hit | Melee | status(Caster, Mp, +2, 1) |
| `mud-hide` | Peau de boue / Mud Hide | Hit | Melee | status(Attacker, Mp, −1, 1) |
| `etched-shard` | Éclat gravé / Etched Shard | LowHp | Percent 50 | status(Caster, Ap, +1, 3) ; status(Caster, Resistance, +20, 3, Earth) |

Descriptions fr (pour la fiche) :
- `herd-grudge` : « Quand un sanglier tombe à 3 cases ou moins, gagne 1 PM et 15 % de dégâts pendant 2 tours. »
- `scamper` : « Frappé au contact, gagne 2 PM pour fuir. »
- `mud-hide` : « Qui le frappe au contact perd 1 PM. »
- `etched-shard` : « Sous la moitié de ses PV, le signe de sa défense s'allume : +1 PA et +20 % de résistance Terre pendant 3 tours. »

### 4.2 Loups (`wolf`), Bois de Clairval
*Narratif : ils gardent depuis toujours le cercle de racines ; la fourrure du chef ressemble à de l'écorce.*

| Monstre | Sorts | Résistances | Passifs |
|---|---|---|---|
| `grey-wolf` | `bite`, `lunge` | Air 10, Fire −15 | `pack-instinct` |
| `howling-wolf` | `howl`, `bite` | Fire −15 | `pack-instinct` |
| `great-wolf` | `bite`, `intercept` | Earth 15, Water 20, Fire −15 | `pack-instinct` |
| `she-wolf` | `pack-guard`, `rally-howl`, `bite` | Fire −15 | `pack-instinct` |
| `pack-leader` | `savage-bite`, `pack-howl`, `lunge` | Earth 30, Water 10, Air 20, Fire −5 | `pack-instinct`, `pack-answers` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `lunge` (mod) | Bond / Lunge | **2** | **2-4** | oui | **non** | **0-0** | 1 | **Neutral** | **1** | — | **leap(Caster)** |
| `howl` (mod) | Hurlement glaçant / Chilling Howl | 3 | 2-5 | oui | non | **3-6** | **1** | Air | 0 | — | status(Enemies, Mp, −1, 1) |
| `intercept` (new) | S'interposer / Intercept | 2 | 1-3 | oui | non | 0-0 | 1 | Neutral | 2 | — | swap(Allies) ; shield(Caster, 10, 2) |
| `rally-howl` (new) | Appel de la louve / Rallying Howl | 3 | 0-0 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 2 | status(Allies, Mp, +1, 1) |
| `cub-bite` (new, invocation) | Mordillage de louveteau / Cub Bite | 3 | 1-1 | oui | non | 3-5 | 2 | Earth | 0 | — | — |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `pack-instinct` | Instinct de meute / Pack Instinct | TurnStart | Area Circle 2 | status(Allies, Damage, +10, 1) |
| `pack-answers` | La meute répond / The Pack Answers | LowHp | Percent 50 | summon(wolf-cub, Count 2) |

- `pack-instinct` : « Au début de son tour, les loups à 2 cases ou moins gagnent 10 % de dégâts. Plusieurs loups cumulent leurs instincts. »
- `pack-answers` : « Sous la moitié de ses PV, appelle deux louveteaux. »

Invocation `wolf-cub` : Louveteau / Wolf Cub, look `wolf-melee`, scale 70, PV 12, PA 6, PM 4, initiative 90, sorts [`cub-bite`].

### 4.3 Champignons (`mushroom`), Bois moisi
*Narratif : la relation. Le mycélium relie sans fusionner ; ils défendent le réseau.*

| Monstre | Sorts | Résistances | Passifs |
|---|---|---|---|
| `bruiser-toadstool` | `cap-slam`, `sticky-spore` | Earth 10, Fire −15 | `final-spores` |
| `spore-puffer` | `spore-burst` | Water 10, Fire −15 | `puffball-burst` |
| `great-cap` | `cap-slam`, `umbrella` | Earth 15, Water 10, Fire −15 | `final-spores` |
| `healing-morel` | `mycelium-mend`, `cap-slam` | Water 10, Fire −15 | `final-spores` |
| `elder-fungus` | `elder-slam`, `rot-spores`, `cutting` | Earth 20, Water 20, Air 10, Fire −5 | `final-spores`, `network-retracts` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `sticky-spore` (new) | Spore collante / Sticky Spore | 3 | 1-2 | oui | non | 5-7 | 1 | Water | 0 | — | status(Enemies, Mp, −1, 1) |
| `spore-burst` (mod) | Nuage de spores / Spore Cloud | 3 | 2-5 | oui | non | **4-6** | 2 | Water | 0 | **Circle 1** | status(Enemies, Poison, 3, 2, Water) |
| `umbrella` (new) | Ombrelle / Umbrella | 3 | 0-0 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 1 | shield(Allies, 8, 2) |
| `mycelium-mend` (mod) | Réseau mycélien / Mycelial Web | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | **Circle 1** | heal(Allies, **7, 10**) |
| `cutting` (new) | Bouture / Cutting | 3 | 1-2 | oui | non | 0-0 | 1 | Neutral | 3 | — | summon(spore-sprout, Max 2) |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `final-spores` | Sporée finale / Final Spores | Death | Area Circle 1 | status(Enemies, Poison, 3, 2, Water) |
| `puffball-burst` | Éclate / Puffball Burst | Death | Area Circle 2 | status(Enemies, Poison, 4, 2, Water) |
| `network-retracts` | Le réseau se rétracte / The Web Retracts | LowHp | Percent 30 | heal(Caster, 30, 30) ; status(Caster, Resistance, +30, 2) ; status(Caster, Mp, −2, 2) |

- `final-spores` : « À sa mort, empoisonne ceux qui sont au contact (3 par tour, 2 tours). »
- `puffball-burst` : « À sa mort, éclate et empoisonne à 2 cases (4 par tour, 2 tours). »
- `network-retracts` : « Sous 30 % de ses PV, s'enracine : soigné de 30, +30 % de résistances, −2 PM pendant 2 tours. »

Invocation `spore-sprout` : Pousse de vesse / Puffball Sprout, look `mushroom-ranged`, scale 70, PV 20, PA 6, PM 2, initiative 80, sorts [`spore-burst`], passifs [`final-spores`].

### 4.4 Zombies (`zombie`), Bois moisi
*Narratif : la fusion forcée. Les morts de la Crypte oubliée, enterrés séparés par élément, se relèvent mêlés. Chaque zombie frappe dans un élément différent.*

| Monstre | Élément | Sorts | Résistances | Passifs |
|---|---|---|---|---|
| `shambling-brute` | Feu | `claw`, `grip` | Fire 10, Water −15 | `contagion-fire` |
| `bile-spitter` | Eau | `bile-spit` | Water 10 | `contagion-water` |
| `bloated-zombie` | Terre | `rot-fist`, `foul-breath` | Earth 15, Fire 10, Water −15 | `contagion-earth`, `explosion` |
| `bonesetter` | Air | `stitch`, `gust-claw` | Air 10, Water −15 | `contagion-air` |
| `zombie-overlord` | les quatre | `grave-smash`, `tomb-vomit`, `stone-fist`, `wing-gust` | Earth 15, Fire 25, Air 10, Water −5 | `contagion-fire`, `four-wings` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `grip` (new) | Étreinte / Grip | 3 | 1-1 | oui | non | 4-6 | 1 | Fire | 1 | — | status(Enemies, Mp, −2, 1) |
| `bile-spit` (mod) | Bile / Bile | 3 | 2-5 | oui | non | **5-8** | 2 | Water | 0 | — | status(Enemies, Poison, 3, 2, Water) |
| `rot-fist` (new) | Poing pourri / Rotten Fist | 3 | 1-1 | oui | non | 8-12 | 2 | Earth | 0 | — | — |
| `foul-breath` (new) | Haleine fétide / Foul Breath | 2 | 0-0 | oui | non | 0-0 | 1 | Neutral | 1 | Circle 1 | status(Enemies, Damage, −10, 2) |
| `stitch` (mod) | Recousu de travers / Crooked Stitch | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | — | heal(Allies, **12, 16**) ; status(Allies, Mp, −1, 1) |
| `gust-claw` (new) | Griffe du vent / Gust Claw | 3 | 1-1 | oui | non | 7-10 | 2 | Air | 0 | — | — |
| `tomb-vomit` (new) | Vomi de tombe / Grave Vomit | 4 | 2-3 | oui | non | 10-14 | 1 | Water | 1 | Cross 1 | — |
| `stone-fist` (new) | Poing de pierre / Stone Fist | 3 | 1-1 | oui | non | 12-16 | 1 | Earth | 0 | — | push(Enemies, 1) |
| `wing-gust` (new) | Souffle des ailes / Wing Gust | 4 | 1-3 | oui | non | 10-14 | 1 | Air | 1 | Line 2 | — |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `contagion-fire` | Contagion / Contagion | Hit | Melee | status(Attacker, Poison, 2, 2, Fire) |
| `contagion-water` | Contagion / Contagion | Hit | Melee | status(Attacker, Poison, 2, 2, Water) |
| `contagion-earth` | Contagion / Contagion | Hit | Melee | status(Attacker, Poison, 2, 2, Earth) |
| `contagion-air` | Contagion / Contagion | Hit | Melee | status(Attacker, Poison, 2, 2, Air) |
| `explosion` | Explosion / Explosion | Death | Area Circle 2, Damage 10-14 Earth | — (les dégâts touchent tout le monde, alliés compris) |
| `four-wings` | Les quatre ailes / The Four Wings | LowHp | Percent 50 | summon(bloated-spawn, Count 1) |

- `contagion-*` : « Qui le frappe au contact est empoisonné (2 par tour, 2 tours). »
- `explosion` : « À sa mort, explose : 10 à 14 dégâts Terre à 2 cases, alliés compris. »
- `four-wings` : « Sous la moitié de ses PV, un bouffi se détache de lui. »

Invocation `bloated-spawn` : Bouffi recousu / Stitched Bloater, look `zombie-tank`, scale 85, PV 40, PA 6, PM 2, initiative 40, sorts [`rot-fist`], passifs [`explosion`].

### 4.5 Grenouilles (`frog`), Lac des Joncs
*Narratif : la nuit, elles chantent une berceuse que Clairval connaît sans l'avoir apprise ; le crapaud-roi en chante le dernier couplet.*

| Monstre | Sorts | Résistances | Passifs |
|---|---|---|---|
| `brawler-frog` | `tongue-lash`, `frog-leap` | Water 10, Earth −15 | `damp-skin` |
| `spitting-frog` | `venom-spit` | Water 10, Earth −15 | `damp-skin` |
| `bullfrog` | `tongue-lash`, `lasso-tongue` | Water 15, Air 10, Earth −15 | `damp-skin`, `bounce` |
| `healing-treefrog` | `dew-mend`, `frog-leap`, `tongue-lash` | Water 10, Earth −15 | `damp-skin` |
| `toad-king` | `tongue-crush`, `royal-croak` | Water 25, Air 15, Fire 10, Earth −5 | `damp-skin`, `lullaby` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `frog-leap` (new) | Bond / Leap | 2 | 2-4 | oui | non | 0-0 | 1 | Neutral | 1 | — | leap(Caster) |
| `venom-spit` (mod) | Crachat venimeux / Venom Spit | 3 | 2-5 | oui | non | **7-10** | 2 | Water | 0 | — | status(Enemies, Poison, 4, 2, Water) |
| `lasso-tongue` (new) | Langue-lasso / Lasso Tongue | 3 | 2-4 | oui | non | 6-9 | 1 | Water | 1 | — | pull(Enemies, 2) |
| `tongue-crush` (mod) | Langue écrasante / Tongue Crush | 4 | 1-2 | oui | non | 22-28 | 1 | Water | 0 | — | **pull(Enemies, 2)** |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `damp-skin` | Peau humide / Damp Skin | TurnStart | — | heal(Caster, 4, 4) |
| `bounce` | Rebond / Bounce | Hit | Melee | push(Attacker, 1) — poussée depuis le porteur, collision comprise |
| `lullaby` | Berceuse / Lullaby | LowHp | Percent 50, Area Circle 3 | status(Enemies, Ap, −2, 1) |

- `damp-skin` : « Se soigne de 4 PV au début de son tour. »
- `bounce` : « Qui le frappe au contact est repoussé d'une case. »
- `lullaby` : « Sous la moitié de ses PV, chante : les ennemis à 3 cases perdent 2 PA à leur prochain tour. »

### 4.6 Crabes (`crab`), Lac des Joncs
*Narratif : ils ramassent ce que le lac rejette ; le géant porte une proue au nom effacé.*
**Changement fort :** des résistances hautes partout, plus une faiblesse Air. Les PV seront sans doute à **baisser** à la simulation.

| Monstre | Sorts | Résistances | Passifs |
|---|---|---|---|
| `pincer-crab` | `pinch`, `holding-pinch` | Earth 20, Fire 20, Water 20, Air −10 | `molt` |
| `bubble-crab` | `bubble-shot`, `pinch` | Earth 20, Fire 20, Water 20, Air −10 | `molt` |
| `armoured-crab` | `pinch`, `shell-up` | Earth 30, Fire 30, Water 30, Air −10 | `molt` |
| `hermit-crab` | `shell-guard`, `shell-swap`, `pinch` | Earth 20, Fire 20, Water 20, Air −10 | `molt` |
| `giant-crab` | `giant-pinch`, `tide-sweep` | Earth 30, Fire 20, Water 25, Air −5 | `molt`, `figurehead` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `holding-pinch` (new) | Pince qui tient / Holding Pinch | 3 | 1-1 | oui | non | 8-11 | 1 | Earth | 1 | — | status(Enemies, Mp, −2, 1) |
| `bubble-shot` (mod) | Tir de bulles / Bubble Shot | 3 | 2-5 | oui | non | **9-13** | 2 | Water | 0 | — | **push(Enemies, 1)** |
| `shell-up` (mod) | Rentrer dans sa coquille / Shell Up | 2 | 0-0 | oui | non | 0-0 | 1 | Neutral | **3** | — | shield(Caster, **20**, 2) ; status(Caster, Mp, −4, 2) |
| `shell-swap` (new) | Changer de coquille / Shell Swap | 2 | 1-4 | oui | non | 0-0 | 1 | Neutral | 2 | — | swap(Allies) |
| `tide-sweep` (mod) | Balayage de marée / Tide Sweep | 4 | 1-2 | oui | non | 15-20 | 1 | Water | 1 | Cross 1 | **push(Enemies, 1)** |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `molt` | Mue / Molt | LowHp | Percent 40 | status(Caster, Resistance, −30, 10) ; status(Caster, Mp, +2, 10) ; status(Caster, Damage, +20, 10) |
| `figurehead` | Proue gravée / Carved Figurehead | LowHp | Percent 50 | shield(Caster, 40, 2) ; summon(hermit-spawn, Count 1) |

- `molt` : « Sous 40 % de ses PV, mue : −30 % de résistances, mais +2 PM et +20 % de dégâts jusqu'à la fin du combat. »
- `figurehead` : « Sous la moitié de ses PV, se couvre de sa proue (bouclier de 40) et un bernard-l'ermite le rejoint. »

Invocation `hermit-spawn` : Jeune bernard / Young Hermit, look `crab-support`, scale 70, PV 30, PA 6, PM 3, initiative 55, sorts [`shell-guard`, `pinch`].

### 4.7 Abeilles (`bee`), Champs de Clairval
*Narratif : les ruches surchauffent ; les abeilles sont devenues Air et Feu sans se détruire, parce qu'elles coopèrent.*

| Monstre | Élément | Sorts | Résistances | Passifs |
|---|---|---|---|---|
| `worker-bee` | Air | `sting` | Air 10, Earth −15 | `alarm` |
| `archer-bee` | Air | `stinger-shot` | Air 10, Earth −15 | `alarm` |
| `drone` | Feu | `fire-sting`, `hot-wax` | Fire 10, Air 10, Earth −15 | `alarm` |
| `nurse-bee` | — | `royal-jelly`, `foraging`, `sting` | Air 10, Earth −15 | `alarm` |
| `queen-bee` | Air / Feu | `queen-sting`, `swarm`, `egg-laying` | Air 25, Fire 15, Water 15, Earth −5 | `alarm`, `overheat` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `fire-sting` (new) | Piqûre brûlante / Burning Sting | 3 | 1-2 | oui | non | 16-22 | 2 | Fire | 0 | — | — |
| `hot-wax` (new) | Cire brûlante / Hot Wax | 3 | 1-1 | oui | non | 6-9 | 1 | Fire | 1 | Line 1 | status(Enemies, Mp, −1, 1) |
| `foraging` (new) | Butinage / Foraging | 2 | 0-4 | oui | non | 0-0 | 1 | Neutral | 2 | — | status(Allies, Ap, +1, 1) |
| `egg-laying` (new) | Ponte / Egg-Laying | 3 | 1-2 | oui | non | 0-0 | 1 | Neutral | 3 | — | summon(worker-spawn, Max 2) |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `alarm` | Alarme / Alarm | Hit | Area Circle 3 | status(Allies, Damage, +20, 1) |
| `overheat` | Ruche en surchauffe / Overheated Hive | LowHp | Percent 50 | status(Caster, Damage, +30, 10) ; status(Caster, Resistance, −15, 10) |

- `alarm` : « Frappée, alerte les abeilles à 3 cases : +20 % de dégâts pendant 1 tour. »
- `overheat` : « Sous la moitié de ses PV, la ruche surchauffe : +30 % de dégâts, −15 % de résistances jusqu'à la fin. »

Invocation `worker-spawn` : Jeune ouvrière / Young Worker, look `bee-melee`, scale 70, PV 30, PA 6, PM 4, initiative 95, sorts [`sting`], passifs [`alarm`].

### 4.8 Taupes (`mole`), Champs de Clairval
*Narratif : elles creusent toutes vers un débris de métal inconnu (la machine de la Disjonction).*

| Monstre | Sorts | Résistances | Passifs |
|---|---|---|---|
| `digger-mole` | `dig-claw`, `surface` | Earth 10, Water −15 | `bury` |
| `clod-mole` | `clod-throw`, `dig-claw` | Earth 10, Water −15 | `bury` |
| `old-mole` | `dig-claw`, `cave-in` | Earth 15, Water −15 | `bury` |
| `mole-mother` | `earth-ward`, `surface`, `dig-claw` | Earth 10, Water −15 | `bury` |
| `mole-king` | `crushing-claws`, `tremor`, `surface` | Earth 25, Fire 10, Air 10, Water −5 | `bury`, `the-debris` |

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `surface` (new) | Surgir / Surface | 2 | 2-5 | **non** | non | 0-0 | 1 | Neutral | 2 | — | leap(Caster) |
| `clod-throw` (mod) | Motte dans l'œil / Clod in the Eye | 3 | 2-5 | oui | non | **10-14** | **1** | Earth | 0 | — | status(Enemies, Ap, −1, 1) |
| `cave-in` (new) | Effondrement / Cave-in | 3 | 2-3 | oui | non | 6-9 | 1 | Earth | 2 | Circle 1 | status(Enemies, Mp, −2, 1) |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `bury` | S'enfouir / Burrow Down | LowHp | Percent 50 | status(Caster, Resistance, +40, 1) ; status(Caster, Mp, +2, 1) |
| `the-debris` | Le débris / The Debris | LowHp | Percent 50 | status(Caster, Ap, +2, 2) ; summon(digger-spawn, Count 2) |

- `bury` : « Sous la moitié de ses PV, s'enfouit : +40 % de résistances et +2 PM pendant 1 tour. »
- `the-debris` : « Sous la moitié de ses PV, déterre un éclat de métal inconnu : +2 PA pendant 2 tours, et deux fouisseuses surgissent. »

Invocation `digger-spawn` : Jeune fouisseuse / Young Digger, look `mole-melee`, scale 70, PV 35, PA 6, PM 3, initiative 60, sorts [`dig-claw`, `surface`].

### 4.9 Le Disjoint (hors des sprints A à E : pour une prochaine quête)
C'est un boss de quête, pas une famille : un **scénario** dans `data/scenarios/` (pas dans `monsters.json`, qui exige cinq rôles). La chaîne principale réelle (T56) se termine au niveau 8 avec « Quatre pistes ». Le Disjoint deviendrait donc la suite de cette chaîne dans les Champs (niveau 27 à 30), lancée depuis la porte des Galeries des taupes. Les textes de la quête sont à écrire par l'agent `scenariste`, et à faire valider par Charles.
- **Caractéristiques :** niveau 30, 450 PV, 8 PA, 3 PM, initiative 70, résistances 15 partout. Look : assemblage `mole-boss` (à défaut, `mole-boss` teinté).
- **Sorts** (1 par tour chacun) : `dig-claw` (Terre), `lasso-tongue` (Eau), `fire-sting` (Feu, + poison) et `pack-howl` (Air).
- **Passifs :**
  - `unstable` (TurnStart, sans `Area`, Damage 15-15 Neutral) : il perd 15 PV au début de chaque tour (les dégâts d'un passif sans zone touchent le porteur, §2.2).
  - `rupture` (Death) : Circle 2, 15-20 neutres.
- **Butin :** l'objet de quête « Débris gravé ».

---

## 5. Les cartes de combat

### 5.1 Plusieurs cartes par famille
**Moteur 🟡 (petit) :** `MonsterFamily` gagne `Battles` (liste de cartes) en plus de `Battle`. `Battle` reste la première, pour la compatibilité. Le combat d'un groupe prend `Battles[Seed(group.Id) % Battles.Count]`, et il est déterministe. La validation existante (≥ 10 places B, ≥ 1 place A) s'applique à chaque carte. Les donjons gardent leurs salles.

| Famille | `battles` |
|---|---|
| boar, wolf | `woods-glade`, `woods-thicket` |
| mushroom, zombie | `mouldy-hollow`, `mouldy-pools` |
| frog, crab | `lakeshore`, `lakeshore-pontoon` |
| bee, mole | `meadow`, `meadow-hedges` |

### 5.2 Les quatre nouvelles cartes (11 × 11, 4 places A, 10 places B, toutes reliées, vérifié par script)
Légende du format existant : `.` sol, `#` obstacle, `~` trou (on tire par-dessus, on ne passe pas), `A` place des héros, `B` place des monstres.

**`woods-thicket`** « Fourré » / « Thicket » : des troncs en couloirs, pour les Charges et les collisions.
```
..#....#.B.
.A.#..#...B
...#..#.B..
..........B
.A.##.##.B.
......#...B
.A.##.##.B.
..........B
...#..#.B..
.A.#..#...B
..#....#...
```
**`mouldy-pools`** « Mares moisies » / « Mouldy Pools » : une grande mare au centre. On se tire dessus par-dessus, la Contagion oblige à contourner.
```
......~..B.
.A...~~...B
....~~..B..
..........B
.A..~~~..B.
...~~~~...B
.A..~~~..B.
..........B
....~~..B..
.A...~~...B
......~....
```
**`lakeshore-pontoon`** « Pontons » / « Pontoons » : l'eau sépare les camps, deux pontons de 3 cases. Le cuirassé bouche un ponton, les bulles poussent vers l'eau.
```
....~~~..B.
.A..~~~...B
..........B
....~~~.B..
.A..~~~..B.
....~~~...B
.A..~~~..B.
....~~~.B..
..........B
.A..~~~...B
....~~~....
```
**`meadow-hedges`** « Haies » / « Hedgerows » : terrain ouvert pour les archères, des haies derrière lesquelles les taupes surgissent.
```
.........B.
.A..#....#B
....#..B.#.
....#....#B
.A.......B.
..........B
.A.......B.
....#....#B
....#..B.#.
.A..#....#B
...........
```

### 5.3 Les donjons (données seules)
**Depuis le sprint 86 (T53)**, on marche dans chaque salle (`Dungeon.Halls`), mais le **combat** de la salle se joue toujours sur sa carte de combat `Dungeon.Rooms[n]` (`dungeon-<famille>-1` à `-3`). C'est cette carte de combat que la proposition ci-dessous change, pas les salles où l'on marche. Les PV sont conservés d'une salle à l'autre (`DungeonHp`) : les passifs `LowHp` se calculent sur `Spec.Hp`, pas sur les PV d'entrée, donc rien à adapter.
La structure en 3 salles (60 %, 100 %, chef) reste. Sans code de plus, la **salle 2** de chaque donjon utilise la carte variante de sa zone (`dungeon-*-2` réécrites par `tools/dungeon_gen.py` sur le dessin des cartes du §5.2). Les salles 1 et 3 gardent leurs piliers.

---

## 6. Ce que le joueur doit ressentir (critères de playtest)

À vérifier avec 2 ou 3 joueurs après chaque zone, en plus de la simulation. **Question clé :** « Avez-vous changé votre façon de jouer entre ces deux familles ? »

| Famille | Le joueur a compris si… |
|---|---|
| Sangliers | il évite de rester dos à un arbre face à un chargeur |
| Loups | il pousse ou attire un loup hors du groupe avant de frapper |
| Champignons | il achève les champignons à distance |
| Zombies | il change d'élément selon le zombie, ou il recule |
| Grenouilles | il concentre ses coups sur une seule grenouille |
| Crabes | il utilise un sort neutre, du poison ou une poussée contre un mur |
| Abeilles | il tue les isolées d'abord et évite de frapper au milieu de l'essaim |
| Taupes | il ne se croit plus à l'abri derrière une haie |

Si une leçon ne passe pas, **on grossit l'effet ou on le montre mieux** (texte du passif, effet visuel). On ne la remplace pas.

---

## 7. Le monde de 30 à 100 (concept : ne pas implémenter sans décision de Charles)

Principe : après la région de Clairval (1 à 30), une **ville carrefour**, puis **quatre régions élémentaires au même niveau** (30 à 60). Le joueur les fait dans l'ordre qu'il veut **sans mise à l'échelle**, ce qui respecte la bible par le niveau plutôt que par le code. Viennent ensuite les régions de convergence. Chaque zone suit le modèle actuel : 25 cartes, 2 familles, 1 donjon par famille.

| Chap. | Région | Zone | Niv. | Familles (concept) | Donjon / jalon |
|---|---|---|---|---|---|
| 1 | Clairval | Bois, Bois moisi, Lac, Champs | 1-30 | les 8 existantes | le Disjoint (fin du ch. 1) |
| — | Hub | **Quatre-Chemins** (ville) | 30 | aucune : métiers, banque, 4 routes | l'érudit ouvre les 4 pistes |
| 2 | Terre | Forêt des Aïeux | 30-45 | Sylvecerfs (bois de pierre) · Gardiens moussus (petits golems) | Racines de l'Arbre-Mère |
| 2 | Terre | Mines de Sous-Racine | 45-60 | Fouille-roc (mineurs obsédés) · Vers de roche | Ruines sous l'Arbre → Fragment de Dhoram |
| 2 | Eau | Côte des Brisants | 30-45 | Goélands pilleurs · Noyés (marins de souvenirs) | Épave des Souvenirs |
| 2 | Eau | Grottes de la Basse-Marée | 45-60 | Anguilles d'écho · Reflets (copient vos sorts) | Sanctuaire englouti → Fragment de Névara |
| 2 | Feu | Plaines de Cendre | 30-45 | Salamandres · Ferrailleurs (automates) | Fonderie abandonnée |
| 2 | Feu | Ateliers de la Forge-Haute | 45-60 | Lames vives (armes nourries de colère) · Gueules de lave | Atelier interdit → Fragment de Kaor |
| 2 | Air | Falaises des Vents | 30-45 | Harpies · Chèvres des cimes | Nid des tempêtes |
| 2 | Air | Archipel dérivant | 45-60 | Nuées (invisibles un tour sur deux) · Cartographes égarés | Observatoire abandonné → Fragment d'Élyon |
| 3 | Convergence | Marches disjointes | 60-70 | Hybrides instables · Sentinelles des Quatre | Sanctuaire caché (dialogue) |
| 3 | Convergence | Terres de la Fracture | 70-80 | Fragments errants · Chimères | Le Seuil |
| 4 | Effacés | Désert des Noms perdus | 80-90 | Effacés (sans élément : seul le neutre les touche) · Archivistes de pierre | — |
| 4 | Effacés | La Chambre sans nom | 85-95 | (grand donjon) | 4 portes, 5 ombres, le gardien |
| 5 | Climax | Le Cœur de la Disjonction | 95-100 | Rouages de séparation · Disjoints adultes | boss final de la Saga I |

Les mécaniques nouvelles que chaque région apporterait (chacune est un ajout moteur à chiffrer le moment venu) :
- **Terre** : glyphes au sol.
- **Eau** : copie du dernier sort reçu.
- **Feu** : un état de colère qui se cumule (`Hit` cumulatif).
- **Air** : invisibilité.
- **Convergence** : changement d'élément à chaque tour.

Au rythme mesuré (une famille ≈ un sprint de 3 points, S72 à S76), cela représente **plusieurs mois** de travail.

---

## 8. Découpage proposé en sprints

| Sprint | Points | Contenu | Fini quand |
|---|---|---|---|
| **A. Moteur des familles** | 5 | §2 en entier : correction PA/PM, passifs (5 déclencheurs, file, `Attacker`, `Count`), `leap`, `swap`, validation, événements, IA (§2.5), fiche et animations (§2.6), tests (§2.7). §5.1 : `Battles`. `spells_gen.py` : `affects` pour `push()` et `pull()`, fonctions `leap()` et `swap()`. | Tests verts ; un combat d'essai avec chaque déclencheur se rejoue à l'identique ; la classe la plus touchée par la correction PA/PM est re-simulée ; l'entrée T57 dans `DECISIONS.md` |
| **B. Le Bois** | 3 | §4.1, §4.2, `wolf-cub`, `woods-thicket` | Simulation dans les bornes de T41 ; auto-test ; playtest de Charles sur la question du §6 |
| **C. Le Bois moisi** | 3 | §4.3, §4.4, `spore-sprout`, `bloated-spawn`, `mouldy-pools` | idem |
| **D. Le Lac** | 3 | §4.5, §4.6, `hermit-spawn`, `lakeshore-pontoon` | idem (attention : les PV des crabes, §4.6) |
| **E. Les Champs** | 3 | §4.7, §4.8, `worker-spawn`, `digger-spawn`, `meadow-hedges`, salles 2 des donjons (§5.3) | idem ; suppression des 8 sorts inutilisés |

**Planification :** les sprints 84 à 89 sont faits. A à E deviennent une nouvelle décision, **D69**, et les sprints **90 à 94** (A = 90, B = 91, C = 92, D = 93, E = 94), avec leurs fichiers `scrum/sprint-NN.md`, comme pour les décisions précédentes (`PLAN.md`, `data/scrum.json`). Les entrées de `DECISIONS.md` commencent à **T57**.
**Les quêtes existantes restent valables :** elles comptent des monstres par famille et des donjons finis, et ces identifiants ne changent pas. Après chaque sprint de zone, relire les quêtes de cette zone (« La meute », « Les morts qui marchent »…). Si un texte décrit un comportement que le monstre n'a plus, demander la correction à l'agent `scenariste`.

---

## 9. Décisions à trancher (avec la valeur par défaut à utiliser en attendant)

| # | Question | Valeur par défaut |
|---|---|---|
| 1 | Force des faiblesses | **−15 %** pour les monstres, **−5 %** pour les chefs |
| 2 | Initiative : les monstres jouent toujours avant les héros (initiative 40-100 contre 10, T34). Faut-il donner aux héros une initiative de base de 60 à 80, pour que l'initiative compte ? | **Ne rien changer** dans ces sprints (le changement toucherait tout l'équilibre) ; en reparler après E |
| 3 | Invocations des chefs (louveteaux, bouffi, bernard, ouvrières, fouisseuses) | **Toutes gardées** ; si un chef dépasse 10 % de victoires seul, baisser ses PV, pas retirer l'invocation |
| 4 | La correction PA/PM (§2.1) rend Dérobade, Élan et Afflux efficaces : il faut peut-être rééquilibrer les classes | **Corriger le bug**, re-simuler, ajuster les PV des héros si un duel sort de 38 à 61 % (T32) |
