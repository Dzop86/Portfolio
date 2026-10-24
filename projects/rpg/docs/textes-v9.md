# Osmose : les textes de l'arbre des talents (v9, noms de travail)

> Écrits par Claude pendant les sprints 137 à 139 (D80), **sans le scénariste** : noms de travail, **à relire par Charles** (et par le scénariste s'il le souhaite). Ils sont déjà dans les données (`data/talents.json`, `data/reactions.json`, `tools/spells_gen.py`) ; une correction ici se reporte là.
> Règles suivies : aucun nom de lieu ni de personne ; pas de « Cyclone » ni d'« Éclair » en double ; le mot de la Foudre (« éclair », « foudre ») réservé aux sorts de Foudre : l'« Éclair » de la Mage (Air) devient « Trait de vent », la « Cyclone » de la Garde devient « Lame cyclone ».

## 1. Les sorts de Foudre (jumeaux des sorts de Feu et d'Air)

| Classe | Sort d'origine | Jumeau fr | Jumeau en |
|---|---|---|---|
| Sentinelle | Flèche ardente | Flèche d'orage | Storm Arrow |
| Sentinelle | Tir de recul | Tir tonnant | Thunder Shot |
| Sentinelle | Salve ardente | Salve d'éclairs | Lightning Volley |
| Sentinelle | Flèche perçante | Flèche filante | Streaking Arrow |
| Sentinelle | Tir brûlant | Tir fulgurant | Flash Shot |
| Sentinelle | Rafale | Grain orageux | Storm Squall |
| Sentinelle | Pluie de braises | Pluie d'éclairs | Rain of Bolts |
| Sentinelle | Chute céleste | Chute de foudre | Lightning Fall |
| Garde | Lame ardente | Lame d'orage | Storm Blade |
| Garde | Taillade | Taillade fulgurante | Flash Slash |
| Garde | Fente ardente | Fente foudroyante | Thunder Lunge |
| Garde | Lame du vent | Lame crépitante | Crackling Blade |
| Garde | Châtiment | Coup de tonnerre | Thunderclap |
| Garde | Lame cyclone | Tourbillon d'orage | Storm Whirl |
| Garde | Lame infernale | Lame de foudre | Lightning Blade |
| Garde | Ouragan | Fureur d'orage | Storm Fury |
| Mage | Flammèche | Crépitement | Crackle |
| Mage | Bourrasque | Souffle d'orage | Storm Breath |
| Mage | Brûlure | Électrocution | Electrocution |
| Mage | Trait de vent | Éclair | Lightning Bolt |
| Mage | Météore | Foudre céleste | Heaven's Lightning |
| Mage | Cyclone | Œil de l'orage | Eye of the Storm |
| Mage | Cataclysme | Fulgurance | Fulguration |
| Mage | Tempête | Orage | Thunderstorm |

## 2. Les talents

| id | Nom fr | Nom en | Description fr | Description en |
|---|---|---|---|---|
| `bogging` | Enlisement | Bogging | Frapper en Terre un ennemi marqué par l'Eau, ou en Eau un ennemi marqué par la Terre, le rend Enlisé : −2 PM pendant 1 tour. Il faut deux éléments dans son deck. | Striking in Earth an enemy marked by Water, or in Water one marked by Earth, leaves it Bogged: −2 MP for 1 turn. It takes two elements in one's deck. |
| `up-close` | Au plus près | Up close | +10 % de dégâts sur une cible à 3 cases ou moins ; en échange, −2 PO à tous ses sorts (jamais sous leur portée minimale ni sous 1). | +10% damage on a target 3 cells away or closer; in exchange, −2 range on all one's spells (never under their minimum range nor under 1). |
| `open-lightning` | Ouverture : Foudre | Opening: Lightning | Ajoute à ton grimoire 4 sorts de Foudre, jumeaux des deux premiers sorts de Feu et d'Air de ta classe. La Foudre frappe sur la somme de tes caractéristiques de Feu et d'Air ; son Arc fait sauter 35 % du coup sur l'ennemi le plus proche à 2 cases. Il faut leur trouver une place dans le deck. | Adds 4 Lightning spells to your grimoire, twins of your class's first two Fire and Air spells. Lightning strikes on the sum of your Fire and Air characteristics; its Arc makes 35% of the hit jump to the nearest enemy within 2 cells. They need a place in the deck. |
| `full-grimoire` | Plein grimoire | Full Grimoire | Les 4 autres sorts de chaque composé ouvert : 8 sorts de Foudre au lieu de 4. | The 4 other spells of each compound opened: 8 Lightning spells instead of 4. |
| `transmute-fire-lightning` | Transmutation : Feu → Foudre | Transmutation: Fire → Lightning | Tes sorts de Feu deviennent leurs jumeaux de Foudre. En échange, tu perds le Feu : plus d'empreinte de Feu ni de réaction qui le demande, aucun bonus de dégâts. Demande l'Ouverture de la Foudre. | Your Fire spells become their Lightning twins. In exchange, you lose Fire: no more Fire mark nor any reaction that needs it, no damage bonus. Asks for the Opening of Lightning. |
| `transmute-air-lightning` | Transmutation : Air → Foudre | Transmutation: Air → Lightning | Tes sorts d'Air deviennent leurs jumeaux de Foudre. En échange, tu perds l'Air : plus d'empreinte d'Air ni de réaction qui le demande, aucun bonus de dégâts. Demande l'Ouverture de la Foudre. | Your Air spells become their Lightning twins. In exchange, you lose Air: no more Air mark nor any reaction that needs it, no damage bonus. Asks for the Opening of Lightning. |
| `strong-signature` | Signature appuyée | Pressed Signature | Les signatures de tes sorts composés valent 50 % de plus (l'Arc passe de 35 à 52 %) ; en échange, tes sorts composés font 10 % de dégâts directs en moins. | The signatures of your compound spells are worth 50% more (the Arc goes from 35 to 52%); in exchange, your compound spells deal 10% less direct damage. |
| `prism` | Prisme | Prism | Tes coups de paire posent l'empreinte de leur premier parent : la Foudre pose celle du Feu. Ils ne déclenchent toujours aucune réaction. | Your pair hits leave the mark of their first parent: Lightning leaves Fire's. They still set off no reaction. |
| `convert-lightning` | Conversion : Foudre | Conversion: Lightning | Tu perds tes 8 sorts de Feu et d'Air et tu gagnes les 8 sorts de Foudre, qui frappent 15 % plus fort et dont l'Arc vaut 50 % de plus. Il te reste la Terre et l'Eau. Clé de Matière : une seule dans tout l'arbre. | You lose your 8 Fire and Air spells and gain the 8 Lightning spells, which hit 15% harder and whose Arc is worth 50% more. You keep Earth and Water. A key of Matter: only one in the whole tree. |

## 3. Les réactions

| id | Nom fr | Nom en | Description fr | Description en |
|---|---|---|---|---|
| `bogged` | Enlisé | Bogged | La Terre et l'Eau mêlées dans la cible : elle perd 2 PM pendant 1 tour. | Earth and Water mixed in the target: it loses 2 MP for 1 turn. |

## 4. Questions pour Charles

1. Les 24 noms de Foudre te vont-ils ? « Orage » (Mage) et « Fureur d'orage » (Garde) sont les plus génériques.
2. « Trait de vent » pour l'ancien « Éclair » de la Mage, « Lame cyclone » pour la Garde ?
