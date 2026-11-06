# L'Archer à quatre rangs : ses 3 branches, 6 lignées et 12 classes finales *(6 novembre 2026, sprint 198, à valider par Charles)*

Ce document prépare la story du sprint 198 : « En tant qu'Archer, je deviens Tireur d'élite, Rôdeur ou Artilleur, puis leurs lignées et leurs finales ; 2 sorts neufs, Cabriole et Feinte réservés au Voleur ; xUnit, bornes du banc ». Il part de `classes-conception.md` §4.3, de `classes-liste.md` §6, de `classes-guerrier.md` (le format de T160) et de `data/specs.json`. La conception lancée chez le game designer s'était arrêtée sans rien écrire : celle-ci a été faite d'une traite, en lisant le moteur.

**Légende.** **[V]** : vérifié dans le code ou les données. **[S]** : supposé, à confirmer par le banc ou le jeu. Aucun chiffre d'équilibre n'est mesuré ici.

---

## 0. En bref

L'Archer change plus que le Guerrier : la Sente (sauts, Cabriole, Feinte) n'a pas d'équivalent dans l'arbre de Charles, et Artilleur demande une branche neuve. Les **identifiants ne changent pas** quand le contenu reste (Q2 de T160) ; une lignée change de branche, une disparaît, une naît.

| Branche (id) | Lignée (id) | Avant | Finale reprise | Gabarit | Finale neuve | Gabarit |
|---|---|---|---|---|---|---|
| Tireur d'élite (`sightline`) | Sniper (`marksman`) | Longue-vue | Œil du dragon `dragon-eye` (ex-Dernière semonce) | Extends | Tir parfait `perfect-shot` | Faster |
| | Chasseur de primes (`bounty-hunter`) | **neuve** | — | | Traqueur légendaire `legendary-tracker` ; Exécuteur `bounty-killer` | TargetState ; Kill |
| Rôdeur (`ambush`) | Éclaireur (`trapper`) | Tend-pièges | Maître forestier `forest-master` (ex-Chausse-trape) | Second | Pisteur `tracker` | Extends |
| | Chasseur sauvage (`falconer`) | Porte-faucon | Seigneur des bêtes `beast-lord` (ex-Deux-Ailes) | Second | Prédateur ancestral `ancestral-predator` | Faster |
| Artilleur (`trail`) | Arbalétrier (`barrage`) | Herse, **venue du Tireur d'élite** | Baliste vivante `living-ballista` (ex-Route barrée) | Trace | Maître arbalétrier `crossbow-master` | Extends |
| | Tireur mobile (`runner`) | Court-sentes | Voltigeur `vaulter` (ex-Cours-toujours) | StayOrMove | Faucon de guerre `war-hawk` | Faster |

- **Ce qui part :** Saute-haie (`leaper`) et son évolution Hors d'atteinte. **Cabriole** et **Feinte** restent dans `spells.json`, sans voie : elles attendent le Malandrin du Voleur. La Cabriole était jouée 0,1 fois par combat [V, T152].
- **Les 2 sorts neufs :** **Désigner la proie** (Chasseur de primes, 50) et **Carreau lourd** (Arbalétrier, 50).
- **Tir de barrage descend de 50 à 30** (Artilleur) : son budget est refait par `budget()` au niveau 30 [V, `spells_gen.py`].
- **Passif neuf :** 1 seul, `spec-bounty` (l'Exécuteur). Aucune ligne de C# : chaque règle employée est déjà lue au combat.
- **Les identifiants de finales sont uniques dans le jeu** (Q3 de T160, vérifié par `Data.CheckSpecs`) : l'Exécuteur de l'Archer est `bounty-killer`, `executioner` étant au Guerrier.

Gabarits : 8 sur 12. Les deux `Second` sont cousins (Rôdeur) : ce sont deux reprises, et c'est littéralement « un second piège » et « un second Faucon ». Les trois `Extends` et les trois `Faster` sont chacun dans trois branches différentes. Jamais deux sœurs du même gabarit (la validation l'exige).

---

## 1. Ce que le moteur lit, et ce que j'emploie

| Règle | Lue par | Emploi |
|---|---|---|
| `target-state` avec `states` | `Talents.Percent` : `c.TargetStates >= r.States`, hors du plafond [V] ; `TargetStates` = `Fighter.States` = le nombre d'états posés sur la cible [V, `Fight.cs`] | Chasseur de primes, Traqueur légendaire |
| `still` (`StillRule`) | `Talents.Percent` : `Walked` [V] | Arbalétrier |
| `first-cast` | `Talents.Percent` : `DamageCasts == 0` [V] | Tir parfait |
| `distance` | `Talents.Percent` [V] | Pisteur |
| `push` | `Talents.Compile` et `Collision` [V] | Artilleur à 30 : les reculs portent d'1 case de plus |
| `summon-share` | le plus fort l'emporte [V, `SummonRule`] | Prédateur ancestral |
| `patch` (`maxRange`, `cooldown`, `perTurn`, `addEffects`, `maxSummons`, `maxGlyphs`) | `Talents.Compile` [V] | partout |

**Les pièges :**
1. **Un même état d'un même lanceur remplace l'ancien** [V, `SetStatus`]. Désigner la proie pose donc des états que Point faible ne pose pas : −1 PM et −10 % de dégâts, pas de résistance. Avec Point faible (résistance), la proie porte 3 états.
2. **Les états comptés sont tous ceux de la cible**, de tous les lanceurs, poisons compris. Un allié qui affaiblit aide le Chasseur de primes : c'est voulu.
3. **`PushRule` passe avant les `patch`** dans `Compile` [V, l. 534 puis 567] : le recul ajouté par Maître arbalétrier n'est pas allongé par la règle de l'Artilleur. La description dit donc « 2 cases », ce qui est exact.
4. **Les sorts de voie hors de la classe** sont déjà retirés du deck et des rangs lus en base (`Character.DeckList`, `RankList`) : un Archer qui avait la Cabriole la perd sans erreur [V, `Entities.cs`].

## 2. Les deux sorts neufs

| Sort | Niveau | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|
| Désigner la proie `path-prey` | 50 | 2 | 1-8 | −1 PM et −10 % de dégâts aux ennemis, 3 tours | 2 |
| Carreau lourd `path-heavy-bolt` | 50 | 5 | 3-8 | dégâts au budget | 1 |

Carreau lourd est le tir lourd de §4.3 : cher, sans effet, qui récompense l'Arbalétrier de ne pas marcher (+5 %, §3.3). Désigner la proie est un sort sans dégâts, comme Point faible, qui ouvre le combo du Chasseur de primes : Point faible, la proie, puis tirer.

## 3. Les nœuds, rang par rang

### 3.1 Tireur d'élite (`sightline`, 30) : un tir, une cible
Inchangé : Œil calme, +1 PO aux sorts en vue, Tir visé, Point faible. Titre : « Tireur d'élite ».

- **Sniper** (`marksman`, 50), inchangé (ex-Longue-vue) : +2 PO aux sorts de 6 PO et plus, Coup au but.
  - **Œil du dragon** (Extends, reprise de Dernière semonce) : le passif monte à +25 % pendant 2 tours ; Tir visé touche la case derrière.
  - **Tir parfait** (Faster, neuve) : ton premier sort de dégâts du tour fait +20 % ; Coup au but se relance en 2 tours.
  - *La décision :* garder la distance tour après tour pour le passif renforcé (Œil du dragon), ou ouvrir chaque tour par un tir lourd, même au contact (Tir parfait).
- **Chasseur de primes** (`bounty-hunter`, 50, neuve) : +15 % sur une cible sous 3 états ou plus ; Désigner la proie.
  - **Traqueur légendaire** (TargetState) : +30 % au lieu de +15 % ; Désigner la proie se relance en 1 tour.
  - **Exécuteur** (Kill) : un ennemi achevé donne +2 PA pendant 1 tour et +15 % de dégâts pendant 2 tours (`spec-bounty`).
  - *La décision :* une grosse proie qu'on traque longtemps (un chef), ou une proie après l'autre (une horde).

### 3.2 Rôdeur (`ambush`, 30) : Faucon et pièges
Inchangé : Faucon à 12 % des PV, glyphes +1 tour, Mâchoires, Piqué. Titre : « Rôdeur ».

- **Éclaireur** (`trapper`, 50), inchangé (ex-Tend-pièges) : Mâchoires se relance en 1 tour, Appât.
  - **Maître forestier** (Second, reprise de Chausse-trape) : deux Mâchoires à la fois, +15 % sur un ennemi dans une de tes glyphes.
  - **Pisteur** (Extends, neuve) : Mâchoires porte à 8 cases, Appât à 7 ; +10 % sur une cible à 6 cases ou plus.
  - *La décision :* tenir un terrain piégé où l'on se bat (Maître forestier), ou piéger loin devant et tirer de loin (Pisteur).
- **Chasseur sauvage** (`falconer`, 50), inchangé (ex-Porte-faucon) : Faucon à 20 %, Chassé-croisé.
  - **Seigneur des bêtes** (Second, reprise de Deux-Ailes) : deux Faucons, Piqué sans relance une fois par tour.
  - **Prédateur ancestral** (Faster, neuve) : un seul Faucon, à 35 % de tes PV et 75 % de tes caractéristiques ; Chassé-croisé sans relance, une fois par tour.
  - *La décision :* deux oiseaux fragiles, ou un seul, solide, avec qui tu échanges de place à chaque tour.

### 3.3 Artilleur (`trail`, 30) : tenir ou courir
Neuf : Talons vifs (+2 PM si on te frappe au contact, venu de la Sente), **tes reculs portent d'une case de plus** (remplace « tes sauts n'ont pas besoin de la vue », sans saut à servir), Tir de barrage (descendu de 50), Coup de talon (repousse donc de 3). Titre : « Artilleur ».

- **Arbalétrier** (`barrage`, 50, change de branche) : +5 % de dégâts si tu n'as pas marché ce tour ; Carreau lourd. *Mesuré :* à +15 %, il gagnait 74,0 % des combats au banc de 50 (+14,4 sur la moyenne des lignées) et 84,2 dans la barrière de la CI ; à +8 %, 69,4 ; à +5 %, 62,5 (+4,9). L'IA de l'Archer ne marche presque jamais : le bonus joue à chaque tour. L'ancien passif de la Herse (Œil calme à 2 cases) ne vient pas : il appartient au Tireur d'élite.
  - **Baliste vivante** (Trace, reprise de Route barrée) : Tir de barrage se relance en 1 tour et laisse une glyphe de −1 PM sur sa ligne.
  - **Maître arbalétrier** (Extends, neuve) : Carreau lourd porte à 10 cases et repousse sa cible de 2 cases.
  - *La décision :* fermer une ligne entière (Baliste), ou garder une cible à distance, d'un tir lourd par tour (Maître arbalétrier). Les deux répondent à la question de la classe : « comment je garde la distance encore un tour ? ».
- **Tireur mobile** (`runner`, 50), inchangé (ex-Court-sentes) : +3 % par case marchée depuis le dernier sort (15 % au plus), Tir en course.
  - **Voltigeur** (StayOrMove, reprise de Cours-toujours) : +5 % par case (25 % au plus), l'écho après 3 cases.
  - **Faucon de guerre** (Faster, neuve) : Tir en course se lance deux fois par tour. C'est un simple titre (§4.3 de la conception) : il fond et tire, sans oiseau.
  - *La décision :* courir loin entre deux tirs (Voltigeur), ou tirer, courir, tirer (Faucon de guerre).

## 4. Les Archers déjà en base (migration `ArcherFinals`)

1. Un Archer de 100 sur une lignée **qui reste** reçoit sa finale reprise : `marksman` → `dragon-eye`, `trapper` → `forest-master`, `falconer` → `beast-lord`, `runner` → `vaulter`. Il se bat comme la veille [V par test].
2. **On garde la branche** quand la lignée part (Q1 ci-dessous) : `sightline:barrage` → `sightline:bounty-hunter`, `trail:leaper` → `trail:runner`. Leur finale reste à choisir, gratuitement (premier choix). Le point de l'onglet les prévient (T159).
3. Retour arrière : les finales des Archers sont effacées et `sightline:bounty-hunter` revient à `sightline:barrage` ; un ancien `leaper` reste `runner` (on ne sait plus qui l'était).

## 5. Banc et fumée

- `FinalsBenchTests.AtHundred_TheArchersFinals…` (explicite, ex-`WarriorBenchTests`, qui joue aussi le Guerrier) : l'Archer à 100, ses 6 lignées sans finale et ses 12 finales, bornes F1 à F5 de `classes-guerrier.md` §7.3, indicatives.
- La fumée N0 joue chaque finale de l'Archer et vérifie qu'elle change au moins un combat ; `Unchanged` nomme celles dont tout l'effet passe par un sort que l'IA ne joue pas.
- **Ce que je prévois, sans l'avoir mesuré :** le Chasseur de primes dépend de l'IA pour jouer la proie avant de tirer ; Prédateur ancestral et Seigneur des bêtes dépendent de l'invocation du Faucon ; la Baliste et le Maître arbalétrier, de Tir de barrage et de Carreau lourd.

## 6. À trancher par Charles

- **Q1. Les héros d'une lignée qui part.** a) **on garde la branche, la lignée sœur la plus proche, finale au choix gratuit (Reco)** ; b) on suit le sort : `sightline:barrage` → `trail:barrage`, avec sa reprise, mais le héros change de sorts de 30.
- **Q2. Les deux `Second` cousins** (Maître forestier, Seigneur des bêtes) : acceptés, ce sont deux reprises. **Reco : oui.**
- **Q3. Œil calme à 2 cases** (`spec-calm-eye-near`) n'est plus porté par personne. **Reco : on le garde dans `passives.json`**, sans voie, comme la Cabriole.
- **Q4. Les noms de travail** (Désigner la proie, Carreau lourd, « Proie marquée ») et les anglais sont au scénariste.
