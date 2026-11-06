# Le Guerrier à quatre rangs : ses 12 classes finales *(game designer, 6 novembre 2026, sprint 197, à valider par Charles)*

Ce document prépare la story du sprint 197 : « En tant que Guerrier, je deviens Chevalier, Maître d'armes ou Gladiateur, d'après la Palissade, l'Étau et la Brèche, puis leurs lignées et leurs finales ; xUnit, bornes du banc ». Il part de `classes-conception.md` (§2.2, §2.3, §4.2, §7), de `classes-liste.md` §1, de `data/specs.json`, du code du Cœur (`Specs.cs`, `Talents.cs`, `Fight.cs`, `Data.cs`, `Effects.cs`), de `tools/spells_gen.py`, de `mesures-classes.md` et de T149 à T159. Il ne modifie ni données ni code.

**Légende.** **[V]** : vérifié dans le code ou les données, ligne citée. **[S]** : supposé, à confirmer par le banc ou le jeu. Aucun chiffre d'équilibre de ce document n'a été mesuré : je n'ai lancé ni le jeu, ni les tests, ni `Rpg.Sim`.

---

## 0. En bref

Chaque lignée du Guerrier perd son évolution de 100 et gagne 2 finales. La **finale reprise** reprend cette évolution à l'identique (passifs, `replaces`, règles) : un héros converti qui la reçoit se bat exactement comme avant. La **finale neuve** utilise un autre gabarit, et uniquement des mécanismes que le moteur lit déjà. **3 passifs neufs** suffisent, aucune ligne de C#.

| Branche (id) | Lignée (id) | Finale reprise | Gabarit | Finale neuve | Gabarit | Ce que le joueur choisit |
|---|---|---|---|---|---|---|
| Chevalier (`palisade`) | Gardien (`rampart`) | Croisé `crusader` | Saves | Sentinelle `sentry` | StayOrMove | **Suivre l'allié** pour le sauver, ou **tenir un poste** où l'allié vient se mettre à l'abri |
| | Champion (`bastion`) | Duelliste `duelist` | Extends | Seigneur de guerre `warlord` | FirstTurn | **Rendre chaque coup reçu**, ou **lancer la charge** de tout le groupe |
| Maître d'armes (`grip`) | Bretteur (`jailer`) | Maître des lames `blade-master` | TargetState | Lame fantôme `ghost-blade` | Extends | **Immobiliser puis écraser** à 0 PM, ou **frapper à 2 cases** sans condition |
| | Tacticien (`pincer`) | Stratège `strategist` | Faster | Général `general` | Trace | **Être la cage** (tout autour de toi, chaque tour), ou **poser la cage** ailleurs, qui reste |
| Gladiateur (`breach`) | Briseur (`ram`) | Exécuteur `executioner` | Kill | Destructeur `destroyer` | Extends | **Achever une cible et enchaîner**, ou **frapper toute la ligne** |
| | Combattant (`harpooner`) | Champion d'arène `arena-champion` | Faster | Pugiliste `pugilist` | LowHp | **Ramener et écraser contre les murs** à distance, ou **cogner au contact** et repousser quand ça tourne mal |

Gabarits employés : 9 sur 12 (Saves, StayOrMove, Extends ×3, FirstTurn, TargetState, Faster ×2, Trace, Kill, LowHp). Second, Death et Form ne vont pas au Guerrier : il n'a ni créature ni piège, et la forme demande du code (§3 de la conception). Les trois Extends sont dans trois branches différentes, jamais entre sœurs ni cousines.

---

## 1. Ce que le moteur lit vraiment, et les pièges trouvés en lisant

### 1.1 Les règles d'une voie, au combat

Les règles d'un palier arrivent au combat comme un talent de plus : `Fight.TalentsOf` ajoute `Specs.AsTalent(spec.SpecRules)` [V, `Fight.cs` l. 354-355], et `Specs.TiersOf` y met le palier de la finale [V, `Specs.cs` l. 136]. Tout ce que `Talents.Compile`, `Talents.Percent`, `Talents.TurnPoints`, `Talents.Collision`, `Rally`, etc. lisent sur `Fighter.Talents` marche donc pour une finale.

Les règles que j'emploie, et l'endroit où elles sont lues :

| Règle | `kind` | Lue par | Note |
|---|---|---|---|
| `PatchRule` | `patch` | `Talents.Compile` l. 567-584 | `cooldown`, `maxRange`, `perTurn`, `area`, `damagePercent`, `addEffects` ; le sort est comparé à `RankKey` |
| `TargetStateRule` | `target-state` | `Talents.Percent` l. 645 | hors du plafond de 35 % |
| `PushRule` | `push` | `Compile` l. 534 et `Talents.Collision` l. 607 | `collision` : le plus fort l'emporte (4 par défaut, `Fight.CollisionDamage`) |
| `PointsRule` | `points` | `Talents.TurnPoints`, au constructeur pour le premier joueur (l. 275) et à `NextTurn` (l. 1498) | `Turn` part de 1 pour chacun |
| `DistanceRule` | `distance` | `Talents.Percent` l. 634 | sous le plafond de 35 % |

**Les pièges, à connaître avant d'écrire une finale :**

1. **`hp` est refusé dans une voie.** `Data.CheckSpecs` rejette `GrantRule`, `DeckRule` et `HpRule` dans un palier [V, `Data.cs` l. 792], et `Fight.AsHero` ne lit `HpRule` que sur `hero.Talents` [V, l. 298]. La commande la citait comme disponible : elle ne l'est pas. Aucune finale ne s'en sert.
2. **`AllyDies` ne joue pas pour un héros.** Le déclencheur exige `Spec.Family` [V, `Fight.cs` l. 1401] ; un héros n'en a pas. Aucune finale ne s'en sert.
3. **Une relance de N fait sauter N tours** : `Cooldowns[id] = Cooldown + 1` [V, l. 1038]. Relance 1 : un tour sur deux. « Relance 0, une fois par tour » : chaque tour. Les reprises gardent donc tout leur sens.
4. **Un même état d'un même lanceur remplace l'ancien, il ne s'ajoute pas** [V, `SetStatus` l. 1218]. Une glyphe de −1 PM remplacerait un −2 PM du même Guerrier : la glyphe du Général retire donc 2 PM, comme le Cercle de fer.
5. **Un état d'1 tour posé sur un autre combattant finit à la fin de son tour à lui** [V, `NextTurn` l. 1457-1461]. Un état qui doit protéger pendant le tour des ennemis doit durer 2 tours.
6. **Une glyphe épargne au tour suivant ceux qui étaient déjà dessus quand elle a été posée** (`Spared`, l. 789 et 1528). Ils ont déjà reçu l'effet du sort lui-même : c'est sans conséquence ici.
7. **`Saves` ne se voit pas en solo.** `AllyFalls` ne sauve qu'un autre combattant de l'équipe [V, l. 1342-1344]. Le banc d'aujourd'hui joue un héros seul : il ne verra jamais la moitié du Croisé (§7).
8. **Un passif `TurnStart` avec `perFight: 1` ne joue qu'au premier tour du porteur** : `MayFire` compte par combat [V, l. 87-94], et rien ne l'en empêche au premier tour (`IsClear` ne lit que `clear` et `still`). C'est ce qui donne le gabarit FirstTurn sans code.
9. **`still` sur un passif `TurnStart`** lit `WalkedLastTurn`, c'est-à-dire des pas réels [V, l. 286 et 1502]. Un échange (Interposition), une attirance, un recul ne comptent pas comme marcher.

### 1.2 Les sorts concernés [V, `spells_gen.py` l. 410-423 et 97-123]

| Sort | PA | Portée | Zone | Effets | Relance |
|---|---|---|---|---|---|
| Interposition `path-interpose` | 2 | 1-3 | — | échange avec un allié, bouclier de 15 sur soi | 2 |
| Tenir bon `path-stand-fast` | 2 | 0 | — | Ancré, +15 % de résistance, 2 tours | 2 |
| Mur de boucliers `path-shield-wall` | 4 | 0 | croix de 1 | bouclier de 30 sur les alliés | 3 |
| Rétorsion `path-retaliation` | 3 | 1 | — | dégâts, consomme jusqu'à 19 de bouclier | 1 |
| Prise `path-hold` | 3 | 1 | — | dégâts, −2 PM 2 tours | 0 |
| Verrou `path-lock` | 2 | 1 | — | −9 PM 1 tour | 3 |
| Écrasement `path-crush` | 5 | 1 | — | dégâts | 0 |
| Cercle de fer `path-iron-ring` | 3 | 0 | cercle de 1 | −2 PM 1 tour aux ennemis | 2 |
| Assaut `path-assault` | 3 | 2-4, en ligne | croix de 1 | dégâts, bond | 2 |
| Trouée `path-breakthrough` | 4 | 1 | ligne de 2 | dégâts, recul de 1 | 1 |
| Harpon `path-harpoon` | 3 | 2-6, en ligne | — | dégâts, attire de 3 | 1 |
| Cri de guerre `war-cry` (classe, niv. 55) | 3 | 0 | cercle de 2 | +20 % de dégâts aux alliés, 2 tours | 3 |

---

## 2. Les 12 finales, en données

Chaque bloc est l'objet `SpecFinal` tel qu'il entrera dans `"finals"` de sa lignée (`id`, `template`, `tier`, `lore`, `icon`). Le `fr` des titres et des descriptions est donné ; l'`en`, le `lore` et l'icône sont au scénariste et à la planche des silhouettes (T159, Q14) : d'ici là, `icon` reste absent et la page reprend l'icône de la lignée. Pour les **reprises**, l'anglais de l'ancienne évolution de 100 peut être repris tel quel (sauf Stratège, §2.4).

Dans chaque lignée, le palier de 100 actuel **disparaît de `tiers`** (la validation l'exige, `Data.cs` l. 777) : il ne reste que le palier de 50.

### 2.1 Chevalier, Gardien (`rampart`)

Ce qu'a déjà la lignée à 50 : bouclier de 12 au début du tour sur soi et les alliés à 2 cases (`spec-close-ranks-12`), +25 % sur les boucliers d'alliés, Interposition, Tenir bon, Mur de boucliers.

**Croisé** (`crusader`, Saves), reprise de Muraille :

```json
{
  "id": "crusader",
  "template": "Saves",
  "tier": {
    "level": 100,
    "title": { "fr": "Croisé", "en": "…" },
    "description": { "fr": "Une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV. Le passif vaut 17. Interposition se relance en 1 tour.", "en": "…" },
    "passives": [ "spec-close-ranks-17", "spec-not-today" ],
    "replaces": [ "spec-close-ranks-12" ],
    "rules": [ { "kind": "patch", "spell": "path-interpose", "cooldown": 1 } ]
  }
}
```

**Sentinelle** (`sentry`, StayOrMove), neuve :

```json
{
  "id": "sentry",
  "template": "StayOrMove",
  "tier": {
    "level": 100,
    "title": { "fr": "Sentinelle", "en": "…" },
    "description": { "fr": "Si tu n'as pas marché à ton tour d'avant : bouclier de 12 de plus sur toi et tes alliés à 2 cases. Tenir bon se relance en 1 tour.", "en": "…" },
    "passives": [ "spec-sentry" ],
    "rules": [ { "kind": "patch", "spell": "path-stand-fast", "cooldown": 1 } ]
  }
}
```

- **La décision.** Le Croisé **va vers l'allié** : Interposition un tour sur deux, et le sauvetage une fois. La Sentinelle **ne bouge plus** : son bouclier double (12 + 12, et 15 sur un allié avec les +25 %) tant qu'elle ne marche pas, et Tenir bon la garde ancrée. C'est aux alliés de venir à elle, et à elle d'attirer l'ennemi (Provocation) ou d'échanger de place (Interposition, qui n'est pas un pas). La question du Guerrier, « où est-ce que je fais finir l'ennemi ? », devient « à mes pieds ».
- **Le moteur.** `still: true` sur un passif `TurnStart` [V, `Effects.cs` l. 324, `Fight.IsClear` l. 286]. Les deux boucliers s'ajoutent : `Shields` est une liste [V, l. 1197]. Il n'y a pas de `replaces` : la Sentinelle garde le bouclier de 12 de sa lignée.
- **La force.** En marchant : 12 par tour, contre 17 au Croisé. Sans marcher : 24, contre 17. Le Croisé a en plus le sauvetage et une Interposition plus fréquente, la Sentinelle un Tenir bon plus fréquent [S]. **En solo, la Sentinelle sera devant** : le sauvetage du Croisé n'y joue jamais (§1.1, piège 7). Les deux se jugent en duo (§7.3).

### 2.2 Chevalier, Champion (`bastion`)

Ce qu'a la lignée à 50 : Tenir bon donne aussi un bouclier de 15, et Rétorsion consomme le bouclier pour frapper.

**Duelliste** (`duelist`, Extends), reprise de Citadelle d'épines :

```json
{
  "id": "duelist",
  "template": "Extends",
  "tier": {
    "level": 100,
    "title": { "fr": "Duelliste", "en": "…" },
    "description": { "fr": "Rétorsion frappe aussi la case derrière la cible (dégâts × 0,8). Qui te frappe au contact est empoisonné : 8 pendant 2 tours, neutre.", "en": "…" },
    "passives": [ "spec-thorns" ],
    "rules": [ { "kind": "patch", "spell": "path-retaliation", "area": { "shape": "Line", "radius": 1 }, "damagePercent": 80 } ]
  }
}
```

**Seigneur de guerre** (`warlord`, FirstTurn), neuve :

```json
{
  "id": "warlord",
  "template": "FirstTurn",
  "tier": {
    "level": 100,
    "title": { "fr": "Seigneur de guerre", "en": "…" },
    "description": { "fr": "Au premier tour du combat : +2 PA. Toi et tes alliés à 2 cases : +2 PM pendant 1 tour, +15 % de dégâts pendant 2 tours.", "en": "…" },
    "passives": [ "spec-charge" ],
    "rules": [ { "kind": "points", "ap": 2, "fromTurn": 1, "untilTurn": 1 } ]
  }
}
```

- **La décision.** Le Duelliste **attend les coups et les rend** : il veut être frappé au contact par beaucoup, et aligner deux ennemis pour Rétorsion. Le Seigneur de guerre **ouvre le combat pour tout le groupe** : premier tour à 10 PA, le groupe avance de 2 cases de plus et frappe plus fort pendant 2 tours. L'un est un combattant seul, l'autre un chef. En solo contre 1 à 3 monstres, le premier tour décide souvent de tout, et le Seigneur y brille ; contre une horde qui dure, le Duelliste.
- **Le moteur.** `PointsRule` de `fromTurn` 1 à `untilTurn` 1 [V, `Talents.TurnPoints` l. 611-615]. Le passif `TurnStart` à `perFight: 1` ne joue qu'au premier tour (§1.1, piège 8). Ses effets `Allies` en cercle de 2 touchent aussi le porteur [V, `Touches` l. 1025], et ses PM comptent tout de suite pour lui, au tour suivant pour les alliés [V, `SetStatus` l. 1226-1231 et `NextTurn` l. 1500]. Les +15 % de la charge et les +20 % du Cri de guerre sont le même état du même lanceur : le dernier posé remplace l'autre (§1.1, piège 4). C'est voulu : rien ne s'empile.
- **La force.** Duelliste : permanente, surtout contre les monstres au contact. Seigneur : très forte au premier tour, nulle ensuite [S]. Le risque est un Seigneur trop faible dans les combats longs : la mesure à 100 en groupe le dira (§7.3).

### 2.3 Maître d'armes, Bretteur (`jailer`)

Ce qu'a la lignée à 50 : qui frappe au contact perd 1 PM, et aussi 1 PA une fois par tour ; Écrasement fait +30 % sur une cible à 0 PM ; retraits de PA et de PM prolongés d'un tour.

**Maître des lames** (`blade-master`, TargetState), reprise d'Oubliette :

```json
{
  "id": "blade-master",
  "template": "TargetState",
  "tier": {
    "level": 100,
    "title": { "fr": "Maître des lames", "en": "…" },
    "description": { "fr": "Écrasement fait +50 % sur un ennemi à 0 PM, au lieu de +30 %. Verrou se relance en 2 tours.", "en": "…" },
    "rules": [
      { "kind": "target-state", "percent": 20, "spell": "path-crush" },
      { "kind": "patch", "spell": "path-lock", "cooldown": 2 }
    ]
  }
}
```

Les 20 s'ajoutent aux 30 du palier de 50 : `Talents.Percent` additionne les `TargetStateRule` [V, l. 645]. C'est bien +50 %, comme aujourd'hui.

**Lame fantôme** (`ghost-blade`, Extends), neuve :

```json
{
  "id": "ghost-blade",
  "template": "Extends",
  "tier": {
    "level": 100,
    "title": { "fr": "Lame fantôme", "en": "…" },
    "description": { "fr": "Prise et Écrasement portent à 2 cases au lieu d'1 : tu frappes ta cible sans être à son contact.", "en": "…" },
    "rules": [
      { "kind": "patch", "spell": "path-hold", "maxRange": 2 },
      { "kind": "patch", "spell": "path-crush", "maxRange": 2 }
    ]
  }
}
```

- **La décision.** Le Maître des lames **mise tout sur la cible immobilisée** : Verrou plus souvent, Écrasement à +50 %. Rien sans 0 PM. La Lame fantôme **frappe une case plus loin**, sans condition : elle tient sa cible à 2 cases (Prise lui retire toujours ses PM), hors de portée de ses coups au contact. Elle renonce aux retraits de « Qui s'y frotte », qui demandent d'être frappé au contact. Le joueur choisit donc : rester collé pour l'écraser, ou rester à distance pour ne pas être touché.
- **La séparation avec le Bretteur du Voleur** (`classes-conception.md` §1.4) tient : ce Bretteur immobilise puis perce, il ne saute pas.
- **Le moteur.** `PatchRule.MaxRange` [V, `Talents.Compile` l. 573] ; les deux sorts gardent leur ligne de vue. Les +30 % sur 0 PM du palier de 50 jouent aussi à 2 cases.
- **La force.** Comparable [S]. L'IA du banc joue Écrasement à 0,5 fois par combat seulement (`mesures-classes.md`, `jailer` à 50) : la mesure des deux sœurs sera tout juste concluante.

### 2.4 Maître d'armes, Tacticien (`pincer`)

Ce qu'a la lignée à 50 : Provocation à 7 de portée, Cercle de fer.

**Stratège** (`strategist`, Faster), reprise d'Étau vivant :

```json
{
  "id": "strategist",
  "template": "Faster",
  "tier": {
    "level": 100,
    "title": { "fr": "Stratège", "en": "…" },
    "description": { "fr": "Les ennemis à ton contact au début de ton tour perdent 1 PM à leur tour suivant. Cercle de fer se relance en 1 tour.", "en": "…" },
    "passives": [ "spec-no-way-out" ],
    "rules": [ { "kind": "patch", "spell": "path-iron-ring", "cooldown": 1 } ]
  }
}
```

**Défaut trouvé dans le texte actuel.** L'évolution d'Étau vivant dit « perdent 1 PM au début de leur tour ». Le passif `spec-no-way-out` agit au début du tour **du Guerrier**, sur les ennemis à son contact à ce moment-là, pour leur tour suivant [V, `spells_gen.py` l. 554 ; T151]. Je corrige la description ci-dessus : l'anglais de la reprise doit suivre la correction. Le contenu, lui, ne change pas.

**Général** (`general`, Trace), neuve :

```json
{
  "id": "general",
  "template": "Trace",
  "tier": {
    "level": 100,
    "title": { "fr": "Général", "en": "…" },
    "description": { "fr": "Cercle de fer se pose jusqu'à 3 cases et laisse au sol, pendant 3 tours, une glyphe de −2 PM.", "en": "…" },
    "rules": [
      {
        "kind": "patch", "spell": "path-iron-ring", "maxRange": 3,
        "addEffects": [ { "kind": "glyph", "affects": "Enemies", "turns": 3, "status": "Mp", "value": -2, "statusTurns": 1 } ]
      }
    ]
  }
}
```

- **La décision.** Le Stratège **est la cage** : tout ce qui le touche perd des PM, et il relance le Cercle autour de lui un tour sur deux. Le Général **pose la cage** : le Cercle part jusqu'à 3 cases, et la zone reste au sol 3 tours. Il peut alors attirer un ennemi dedans (Provocation à 7, le recul de Coup de bouclier) et aller tenir un autre point. Le premier tient un point, le second tient le terrain.
- **Le moteur.** `PatchRule.AddEffects` ajoute la glyphe aux effets du sort [V, `Compile` l. 578] ; `Cast` pose toute `GlyphEffect` sur la zone du sort [V, l. 1115 et 1171-1173] ; la glyphe ne frappe que les ennemis [V, `Strikes` l. 482-487]. Champs de `GlyphEffect` [V, `Effects.cs` l. 224]. Pourquoi 3 tours : le Cercle (relance 2) revient au 3e tour ; à 2 tours, la cage s'ouvrirait un tour sur trois. Pourquoi −2 : un −1 du même Guerrier remplacerait le −2 du sort (§1.1, piège 4). La prolongation d'Étau ne touche pas la glyphe : `StatusTurnsRule` ne lit que les `StatusEffect` et passe avant les `patch` [V, `Compile` l. 558-584].
- **Non vérifié :** aucune donnée n'ajoute encore de glyphe par `addEffects`. Le chemin du code existe de bout en bout, mais il n'a jamais été joué. **Un test unitaire est indispensable** (§7.5).
- **La force.** Comparable [S]. **Le banc ne pourra pas juger le Général** : l'IA d'un héros ne joue jamais Cercle de fer (`CiLevelsTests.Unplayed`, T158). Sa ligne sera « non concluante » ; le Stratège, lui, vaudra au moins son passif.

### 2.5 Gladiateur, Briseur (`ram`)

Ce qu'a la lignée à 50 : un ennemi achevé donne +1 PM et +1 PA (`spec-carried-through-2`), les attirances portent d'une case de plus, Assaut, Arrachement, Trouée.

**Exécuteur** (`executioner`, Kill), reprise de Brise-rangs :

```json
{
  "id": "executioner",
  "template": "Kill",
  "tier": {
    "level": 100,
    "title": { "fr": "Exécuteur", "en": "…" },
    "description": { "fr": "Un ennemi achevé : +1 PM et +2 PA. Assaut se relance en 1 tour.", "en": "…" },
    "passives": [ "spec-carried-through-3" ],
    "replaces": [ "spec-carried-through-2" ],
    "rules": [ { "kind": "patch", "spell": "path-assault", "cooldown": 1 } ]
  }
}
```

**Destructeur** (`destroyer`, Extends), neuve :

```json
{
  "id": "destroyer",
  "template": "Extends",
  "tier": {
    "level": 100,
    "title": { "fr": "Destructeur", "en": "…" },
    "description": { "fr": "Trouée frappe une ligne de 4 cases au lieu de 3. Assaut frappe en croix de 2 (dégâts × 0,8).", "en": "…" },
    "rules": [
      { "kind": "patch", "spell": "path-breakthrough", "area": { "shape": "Line", "radius": 3 } },
      { "kind": "patch", "spell": "path-assault", "area": { "shape": "Cross", "radius": 2 }, "damagePercent": 80 }
    ]
  }
}
```

- **La décision.** L'Exécuteur **choisit une cible, l'achève et enchaîne** : PA rendus, Assaut plus souvent pour sauter sur la suivante. Le Destructeur **frappe tout ce qui est aligné** : plus de dégâts au total, moins d'ennemis achevés, donc moins de PA rendus par sa lignée. Concentrer ou étaler.
- **Le moteur.** `PatchRule.Area` et `DamagePercent` [V, `Compile` l. 575-577]. Une ligne de rayon 3 compte 4 cases, la cible comprise [V, `Effects.cs` l. 77-78]. `PatchRule` ne recalcule pas le budget de zone (`AreaFactor` ne sert qu'à `ZoneRule`) : Trouée garde ses dégâts sur une case de plus ; Assaut passe de la croix de 1 (facteur 0,8) à la croix de 2 (0,6), d'où le × 0,8, comme Citadelle d'épines [S sur le chiffre].
- **La force.** Comparable [S]. L'IA ne joue jamais Assaut (T158) : au banc, seule la Trouée du Destructeur sera jugée.

### 2.6 Gladiateur, Combattant (`harpooner`)

Ce qu'a la lignée à 50 : +15 % de dégâts au contact, Harpon.

**Champion d'arène** (`arena-champion`, Faster), reprise de Cloue-au-mur :

```json
{
  "id": "arena-champion",
  "template": "Faster",
  "tier": {
    "level": 100,
    "title": { "fr": "Champion d'arène", "en": "…" },
    "description": { "fr": "Tes reculs contre un obstacle font 15 dégâts par case. Harpon n'a plus de relance, une fois par tour.", "en": "…" },
    "rules": [
      { "kind": "push", "cells": 0, "apCost": 0, "collision": 15 },
      { "kind": "patch", "spell": "path-harpoon", "cooldown": 0, "perTurn": 1 }
    ]
  }
}
```

Le gabarit nommé est Faster (Harpon à chaque tour au lieu d'un sur deux, §1.1, piège 3). Les 15 par case de collision n'ont pas de gabarit à eux : c'est la seconde moitié de la signature.

**Pugiliste** (`pugilist`, LowHp), neuve :

```json
{
  "id": "pugilist",
  "template": "LowHp",
  "tier": {
    "level": 100,
    "title": { "fr": "Pugiliste", "en": "…" },
    "description": { "fr": "Au contact, +25 % au lieu de +15 %. Une fois par combat, sous la moitié de tes PV : tes voisins reculent de 2 cases, et +2 PA pendant 1 tour.", "en": "…" },
    "passives": [ "spec-on-the-ropes" ],
    "rules": [ { "kind": "distance", "minCells": 1, "maxCells": 1, "percent": 10 } ]
  }
}
```

- **La décision.** Le Champion d'arène **travaille à distance** : il harponne à chaque tour et écrase ce qu'il repousse contre les murs ; ce qui compte pour lui, c'est où se trouve l'obstacle. Le Pugiliste **reste au contact** et y frappe plus fort ; quand il est acculé, il fait le vide autour de lui, puis ramène un ennemi à la fois (Harpon, Provocation). L'un déplace l'ennemi contre le décor, l'autre contre lui-même. Les deux restent du côté du Guerrier dans la paire Guerrier et Berserker (§1.4 : « l'un fait venir, l'autre y va ») : aucun ne coûte de PV, et le Pugiliste repousse.
- **Le moteur.** Les deux `DistanceRule` s'additionnent, 25 % sous le plafond de 35 [V, `Talents.Percent` l. 634 et 646]. Le passif `LowHp` joue une fois par combat [V, `Hurt` l. 1360] et pousse comme le passif `bounce` des grenouilles [V, `spells_gen.py` l. 501 ; `Apply` l. 1200-1201]. Ses collisions sont celles par défaut, 4 par case : seul le Champion d'arène les porte à 15.
- **La force.** Le +10 % joue sur presque tous les coups ; le passif une fois [S]. Le Champion d'arène dépend du terrain, le Pugiliste non.

---

## 3. Les passifs neufs, pour `tools/spells_gen.py`

Trois lignes, à placer après `spec-no-way-out` (l. 554), sous un commentaire `# The Warrior's finals (sprint 197).`. Les noms et l'anglais sont des noms de travail, à reprendre par le scénariste ; le générateur exige les deux langues, d'où leur présence.

```python
 P("spec-sentry","Poste tenu","Held Post","Au début de ton tour, si tu n'as pas marché au tour d'avant : bouclier de 12 sur toi et tes alliés à 2 cases.","At the start of your turn, if you did not walk on your turn before: a shield of 12 on you and allies within 2 cells.","TurnStart",[shield("Allies",12,2)],area=("Circle",2),still=True),
 P("spec-charge","Sonner la charge","Sound the Charge","Au premier tour du combat : toi et tes alliés à 2 cases, +2 PM pendant 1 tour et +15 % de dégâts pendant 2 tours.","On the first turn of the fight: you and allies within 2 cells, +2 MP for 1 turn and +15% damage for 2 turns.","TurnStart",[status("Allies","Mp",2,1),status("Allies","Damage",15,2)],area=("Circle",2),perFight=1),
 P("spec-on-the-ropes","Dans les cordes","On the Ropes","Une fois par combat, sous la moitié de tes PV : tes voisins reculent de 2 cases, et +2 PA pendant 1 tour.","Once per fight, below half your HP: your neighbours are pushed back 2 cells, and +2 AP for 1 turn.","LowHp",[push(2),status("Caster","Ap",2,1)],area=("Circle",1),percent=50),
```

Ils passent `Data.Check(PassiveSpec)` [V, l. 809-814] : `still` sur un `TurnStart`, `percent` de 1 à 99 sur un `LowHp`, aucun effet `Attacker` hors `Hit`, aucun bond ni échange. `perFight` est un champ déjà émis par `P` [V, l. 472].

**Les passifs existants repris par les finales** : `spec-close-ranks-17`, `spec-not-today`, `spec-thorns`, `spec-carried-through-3`, `spec-no-way-out`. Ils restent tels quels, et ne servent plus qu'aux finales.

---

## 4. Les descriptions (Q12 : 160 signes au plus)

Comptées à la main, espaces comprises, à ±3 près.

| Finale | Description | Signes |
|---|---|---|
| Croisé | Une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV. Le passif vaut 17. Interposition se relance en 1 tour. | 120 |
| Sentinelle | Si tu n'as pas marché à ton tour d'avant : bouclier de 12 de plus sur toi et tes alliés à 2 cases. Tenir bon se relance en 1 tour. | 129 |
| Duelliste | Rétorsion frappe aussi la case derrière la cible (dégâts × 0,8). Qui te frappe au contact est empoisonné : 8 pendant 2 tours, neutre. | 133 |
| Seigneur de guerre | Au premier tour du combat : +2 PA. Toi et tes alliés à 2 cases : +2 PM pendant 1 tour, +15 % de dégâts pendant 2 tours. | 119 |
| Maître des lames | Écrasement fait +50 % sur un ennemi à 0 PM, au lieu de +30 %. Verrou se relance en 2 tours. | 90 |
| Lame fantôme | Prise et Écrasement portent à 2 cases au lieu d'1 : tu frappes ta cible sans être à son contact. | 95 |
| Stratège | Les ennemis à ton contact au début de ton tour perdent 1 PM à leur tour suivant. Cercle de fer se relance en 1 tour. | 116 |
| Général | Cercle de fer se pose jusqu'à 3 cases et laisse au sol, pendant 3 tours, une glyphe de −2 PM. | 94 |
| Exécuteur | Un ennemi achevé : +1 PM et +2 PA. Assaut se relance en 1 tour. | 63 |
| Destructeur | Trouée frappe une ligne de 4 cases au lieu de 3. Assaut frappe en croix de 2 (dégâts × 0,8). | 92 |
| Champion d'arène | Tes reculs contre un obstacle font 15 dégâts par case. Harpon n'a plus de relance, une fois par tour. | 101 |
| Pugiliste | Au contact, +25 % au lieu de +15 %. Une fois par combat, sous la moitié de tes PV : tes voisins reculent de 2 cases, et +2 PA pendant 1 tour. | 141 |

Les noms tiennent tous en 2 morceaux de 13 signes au plus (Q12) : le plus long morceau est « Seigneur » / « de guerre » (8 et 9).

---

## 5. Les noms des branches et des lignées, et leurs titres

Les **identifiants ne changent pas** (`palisade`, `rampart`...) : ce sont les colonnes `Spec` et `Fork` des personnages en base, et aucun ne se confond avec un nom (voir Q2, §8). Seuls `name`, les titres et les finales changent.

| Id | Avant : `name` | Avant : titre du palier | Après : `name` et titre du palier |
|---|---|---|---|
| `palisade` (branche, 30) | Palissade | Garde de la Palissade | **Chevalier** |
| `grip` (branche, 30) | Étau | Garde de l'Étau | **Maître d'armes** |
| `breach` (branche, 30) | Brèche | Garde de la Brèche | **Gladiateur** |
| `rampart` (lignée, 50) | — | Garde-corps | **Gardien** |
| `bastion` (lignée, 50) | — | Bastion d'épines | **Champion** |
| `jailer` (lignée, 50) | — | Carcan | **Bretteur** |
| `pincer` (lignée, 50) | — | Tenaille | **Tacticien** |
| `ram` (lignée, 50) | — | Bélier | **Briseur** |
| `harpooner` (lignée, 50) | — | Croc-de-fer | **Combattant** |

**Ce qui change dans ces titres :**
- **À 30, le titre devient le nom de la branche**, comme pour l'Archimage : « Garde de la Palissade » disparaît, il n'y a plus de « Garde » (T157 l'annonçait pour S197).
- **À 50, le titre devient le nom de la lignée**, de Charles. Les anciens titres (Garde-corps, Carcan...) ne restent nulle part ; les noms des sorts et des passifs (Serrez les rangs, Qui s'y frotte, Sur la lancée...) restent.
- **Les descriptions de 30 et 50 ne changent pas** : leurs effets non plus. Le `lore` des branches et des lignées reste, sauf avis du scénariste : celui de la Palissade (« le pieu du milieu ») va moins bien à un Chevalier.
- **Les titres de 100 disparaissent** avec les évolutions : Muraille, Citadelle d'épines, Oubliette, Étau vivant, Brise-rangs, Cloue-au-mur.
- **Le féminin** (D96, facultatif) : Gardienne, Championne, Combattante, Sentinelle... Le modèle de données n'a pas encore de champ pour lui ; il n'entre pas dans cette story.
- Le nom d'Écrasement, lourd pour un Bretteur (§4.2 de la conception), reste au scénariste.

---

## 6. Les héros convertis

`Specs.FinalOf` ne rend une finale qu'à un héros qui l'a choisie : un héros de 100 sans `Final` ne garde que ses paliers de 30 et 50 [V, `FinalsTests.WithoutAChoice_ALineageWithFinals_StopsAtItsEvolutionAt50`]. **Sans rien de plus, chaque Guerrier de 100 perd son évolution de 100 le jour de la conversion**, jusqu'à ce qu'il choisisse (gratuitement, puisque c'est son premier choix).

Ma recommandation : **la migration de S197 écrit la finale reprise** de sa lignée pour tout Guerrier de 100 qui a une lignée, dans le même geste que les données. Il se bat alors exactement comme la veille, et la finale neuve lui reste ouverte au prix d'un changement (500 Éclats). Plouf est restauré par `admin --spec branche:lignée:finale`, jamais en SQL (mémoire du projet). À trancher, Q1 (§8).

Les **combats enregistrés** ne changent pas pour un héros qui reçoit sa reprise : mêmes passifs, mêmes règles. Ils changent pour un héros qui prend une finale neuve. T155 l'annonçait : `FightRecord` devra changer de version quand une finale changera un combat, donc dès ce sprint (Q5).

---

## 7. Le banc : mesures, bornes, fumée et budget

### 7.1 Ce qui manque au banc aujourd'hui [V]

- `Bench.PathsOf` ne connaît que les branches et les lignées [V, `Bench.cs` l. 49-56] : il faut une ligne par finale au niveau 100 et plus, plus une ligne par lignée **sans** finale au même niveau, qui sert de référence (« rang 3 à 100 »).
- `Rpg.Sim --matrix` joue 35, 45 et 50, ou le seul `--level` donné [V, `Program.cs` l. 175] : `--level 100` suffira une fois `PathsOf` étendu.
- `SyntheticFamily(100)` n'a jamais été construite. Les gabarits sont des formules sans plafond [V, `Templates.cs`], mais les monstres y auront environ 100 à 105 niveaux : **à vérifier qu'aucune validation de `GameData` ne les refuse** [S].
- Le banc joue un héros seul : le sauvetage du Croisé, les boucliers d'alliés de la Sentinelle et la charge du Seigneur n'y comptent pas ou peu.

### 7.2 Les mesures

| Mesure | Outil | Volume | Quand |
|---|---|---|---|
| **M100-solo** : le Guerrier à 100, sans voie, ses 6 lignées sans finale, ses 12 finales | `Rpg.Sim --matrix --simulate 30 --level 100 --class warrior`, avec et sans `--decks tools/bench-decks.json` ; et un test `Explicit` qui pose les bornes, sur le modèle de `TemplateTests.AClasssPaths_AreWorthAlike…` mais sans échantillon | 19 lignes × 16 groupes × 30 = **9 120 combats** par paquet | à ce sprint, puis aux jalons ; résultats dans `mesures-classes.md` |
| **M100-duo** : chaque finale et sa lignée sans finale, en duo avec l'Archer de 100 sans voie, contre les hordes de 6 et plus de la famille de synthèse | test `Explicit`, sur le modèle de `CiLevelsTests.Won` | 18 lignes × 300 = **5 400 combats** [S sur le nombre de hordes] | idem |

Les deux tests rejoignent la liste fixe des tests explicites de T158, qui passe de 9 à 11 : le test qui la vérifie est à mettre à jour.

### 7.3 Les bornes

À 480 combats, une ligne a ±4,5 points d'intervalle à 95 %, et l'écart entre deux lignes ±6,3. D'où des bornes de 8 points et plus, pas moins.

| # | Mesure | Borne | Statut |
|---|---|---|---|
| **F1** | Une finale contre sa lignée sans finale, à 100 | **+10 au plus** (B8) et **−3 au moins** (le plancher de B7, provisoire comme lui : Charles veut qu'un choix ne fasse pas perdre) | indicatif, hors CI (B8) |
| **F2** | Deux sœurs, solo | **8 points au plus** d'écart, pour Bretteur, Tacticien, Briseur et Combattant | indicatif |
| **F3** | Deux sœurs, duo | **8 points au plus** pour Gardien et Champion, jugés en duo et non en solo : une finale de groupe ne se juge pas seule. Leur écart solo est imprimé, sans borne | indicatif |
| **F4** | Entre lignées à 100 | la moyenne des deux finales de chaque lignée à **±8 de la moyenne du Guerrier** à 100 (B6 porté à 100) | indicatif |
| **F5** | Ce qui n'est pas concluant | une finale est « non concluante » si son passif neuf se déclenche moins de 0,5 fois par combat (`PassiveTriggered`), ou si le sort qu'elle modifie est joué moins de 0,5 fois (comme `BenchRow.LeastCasts`). Pour un `Saves` : déclenché dans moins d'1 combat sur 5 en duo. **Une ligne non concluante ne règle rien** : on répare l'IA, pas la finale (§7.2 de la conception) | règle de lecture |

**Ce que je prévois, sans l'avoir mesuré :** le Général sera non concluant (Cercle de fer jamais joué), le Destructeur à moitié (Assaut jamais joué), le Croisé et le Seigneur ne se liront qu'en duo, la Lame fantôme et le Maître des lames seront à la limite (Écrasement à 0,5). Sur 12 finales, 4 ou 5 lignes concluantes est un résultat attendu. **Aucune zone n'existe au-delà de 60** : ces bornes restent indicatives jusqu'aux zones de 60 à 100, et le playtest tranche (T152).

### 7.4 La fumée N0 de `CiLevelsTests`, dans le budget de 20 000

Ce que la fumée doit couvrir en plus : **chaque finale jouée**, plus le Guerrier de 100 sans voie et ses 6 lignées sans finale, qui servent de référence.

- **Ce qu'elle vérifie, pour chaque finale :** le combat finit ; les sorts de sa feuille sont joués, sauf ceux de `Unplayed` (règle actuelle) ; et **la finale change au moins un combat** : sur les mêmes graines, la suite d'événements diffère au moins une fois de celle de sa lignée sans finale. C'est ce qui aurait attrapé un `hp` posé dans une voie, et ce qui attrapera la prochaine règle morte. Une finale dont tout l'effet passe par un sort de `Unplayed` ne peut rien changer : une liste `Unchanged`, sur le modèle de `Unplayed`, la nomme (le Général aujourd'hui), et la fumée échoue dès qu'elle change un combat, pour que la liste ne fasse que raccourcir.
- **Le budget.** Aujourd'hui : 33 héros × 4 groupes × 7 combats = 924 [V, `SmokeFightsEach(33, 4)`]. Avec 19 héros de plus à 100 sur 4 groupes : 52 × 4 = 208 couples, 4 combats chacun, **832**. Mais à 10 classes, 10 × 30 héros × 4 groupes = 1 200 couples, et le plancher d'1 combat par couple dépasse les 960. **Recommandation : les héros de 100 jouent 2 groupes au lieu de 4**, et `SmokeFightsEach` partage le budget sur le nombre de couples héros et groupe, pas sur le seul nombre de héros. Avec le seul Guerrier converti : 33 × 4 + 19 × 2 = 170 couples, 5 combats chacun, **850**. À 10 classes : 440 + 380 = 820 couples, 1 combat chacun, **820**. Le budget de 960 tient dans les deux cas.
- **Le reste de la CI** ne bouge pas : N1 joue à 45, les finales n'y sont pas. Un run passerait d'environ 17 496 à 17 420 combats [calculé, non mesuré], sous les 20 000 de `FightBudget.Limit`. Les tests unitaires de §7.5 font des combats à la main, sans `Ai.PlayOut`, donc sans coût au compteur.
- `TheSmoke_EveryClassAndPath…` compte ses héros (`Real.Classes.Count * 11`) : ce compte devient « 11 par classe, plus 19 par classe convertie ». `TheSmoke_StaysWithinItsBudget…` suit le nouveau partage.

### 7.5 Les tests xUnit de la story

- **Données** (`DataTests` ou `FinalsTests`) : chaque lignée du Guerrier a 2 finales et plus de palier de 100 ; chaque reprise porte exactement les passifs, `replaces` et règles de l'ancienne évolution, écrits en dur dans le test, puisque l'évolution disparaît ; un Guerrier de 100 avec sa reprise a les mêmes `PassivesOf` et `RulesOf` qu'avant. `FinalsTests.ALineageWithoutFinals_KeepsItsEvolutionAt100…` est à réécrire : ce n'est plus vrai du Guerrier, ça le reste de l'Archer et du Mage.
- **Un test par finale neuve**, sur l'`Arena` : la Sentinelle protège après un tour sans marcher, pas après un pas, et pas après un échange ; la charge ne joue qu'au premier tour du porteur, le porteur compris ; Dans les cordes repousse les voisins une fois, sous 50 % ; le Général pose une glyphe de −2 PM à 3 cases, qui dure 3 tours du porteur et ne frappe que les ennemis ; la Lame fantôme frappe de 2 cases ; la Trouée du Destructeur touche une 4e case ; le Pugiliste fait +25 % au contact.
- **Intégration API** : le choix réussi d'une finale par `PUT …/spec`, et `admin --spec warrior…:rampart:crusader`. T155 et T156 les reportaient « à S197, avec les premières finales ».
- **Godot** : l'auto-test dessine une colonne 100 à finales réelles (T159 le reportait aussi à S197).

---

## 8. Les points à trancher par Charles

**Q1. Les Guerriers de 100 déjà en base.**
- a) **La migration leur écrit la finale reprise de leur lignée (Reco)** : rien ne change pour eux ; la finale neuve leur coûte 500 Éclats
- b) On leur laisse choisir, gratuitement : ils jouent sans palier de 100 jusqu'au choix, mais le point de l'onglet les prévient (T159)
- c) a), avec un changement gratuit en plus : il faut du code pour un « premier changement offert »

**Q2. Les identifiants des branches et des lignées.**
- a) **On garde `palisade`, `grip`, `breach`... (Reco)** : aucune migration, et aucun ne se confond avec un nom
- b) On les renomme (`warrior-knight`...), comme le suggère §4.5 de la conception : une migration de plus, pour des identifiants plus lisibles

**Q3. L'identifiant de la Sentinelle.** Je propose `sentry`, pas `sentinel` : `sentinel` était l'identifiant de l'ancienne classe, et `DeckCode` lit encore `SENTINEL` (T157). Une recherche dans le code confondrait les deux. Même logique pour la suite : faut-il exiger des identifiants de finale uniques dans tout le jeu ? Ce n'est pas obligatoire, puisqu'une finale se cherche dans sa lignée, mais « Exécuteur » existe aussi chez l'Archer, et une icône `spec-executioner` serait ambiguë. **Reco : uniques dans tout le jeu**, vérifié par `DataTests`.

**Q4. Deux lignées jugées en duo.** Gardien et Champion se mesurent en duo (F3), pas en solo. **Reco : oui** ; sinon le Croisé, qui ne sauve personne seul, perd toujours contre la Sentinelle sur le banc.

**Q5. Le relevé des combats.** Une finale neuve change un combat : **Reco : `FightRecord` change de version à ce sprint** (T155). Un héros qui a sa reprise rejoue à l'identique.

**Q6. Le plancher de −3 pour les finales (F1).** B7 n'est accepté qu'à titre provisoire. **Reco : l'appliquer aussi aux finales**, toujours indicatif, puisqu'une finale ne devrait jamais faire perdre.

**Q7. Le texte d'Étau vivant était faux** (§2.4) : je le corrige dans la description du Stratège sans changer le contenu. Rien à décider, sauf si Charles préfère changer le passif pour qu'il corresponde à l'ancien texte : je le déconseille, ce serait toucher un chiffre que T151 a choisi.

---

## 9. Ce qui a été vérifié, et ce qui ne l'a pas été

**Vérifié dans le code et les données :** les 6 évolutions de 100 et les paliers de 30 et 50 du Guerrier (`specs.json` l. 273-730) ; `SpecFinal`, `FinalTemplate`, `Specs.TiersOf`, `PassivesOf`, `Transferred` ; la validation des voies et des passifs (`Data.cs` l. 770-814), dont le refus de `hp` ; la lecture de chaque règle employée (`Talents.Compile`, `Percent`, `TurnPoints`, `Collision`) ; les déclencheurs `TurnStart`, `Hit`, `LowHp`, `Kill`, `AllyFalls`, `AllyDies` dans `Fight.cs` ; `MayFire`, `IsClear`, `SetStatus`, `Apply`, `Lay`, `StrikeAt`, `Touches`, les relances ; les sorts et passifs de `spells_gen.py` ; `Bench`, `SyntheticFamily`, `CiLevelsTests`, `TemplateTests`, `FightBudget`, `Rpg.Sim --matrix` ; T149 à T159 ; `mesures-classes.md` ; la maquette §14 (Q12, Q13) et les noms de gabarits de `Texts.cs`.

**Supposé, non mesuré :** toute la force relative des finales (§2) et toutes les bornes du §7.3 ; les volumes de combats du §7.4, calculés et non chronométrés ; le nombre de hordes de la famille de synthèse ; que `SyntheticFamily(100)` se construise sans erreur ; qu'une glyphe ajoutée par `addEffects` se pose comme celle d'un sort (le code le permet, aucune donnée ne l'a encore fait).

**Non lu en entier :** `Ai.cs` (seule la ligne de valeur des coups l'a été) : je ne sais pas comment l'IA d'un héros valorise les nouveaux passifs, ce qui ne touche que la mesure, pas le jeu (T154).
