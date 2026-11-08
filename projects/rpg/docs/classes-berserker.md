# Le Berserker, première moitié : le sang, la Rage et le Ravageur *(8 novembre 2026, sprint 206, à valider par Charles)*

Ce document prépare la story du sprint 206 : « En tant que Berserker, mon kit paie en PV et change de forme, et je deviens Ravageur ; xUnit. PloufBerserker créé. » Le Barbare, le Gladiateur et leurs finales viennent au sprint 207. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §1.4 ce qui le sépare du Guerrier et du Druide, §3 le sang et la forme) et de `classes-liste.md` §9.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Troisième classe neuve**, id `berserker` (celui de la page de création depuis le sprint 195). 90 PV + 5 par niveau, 8 PA, 5 PM, initiative 20, **budget de dégâts 1,1**, au contact. La question de son tour : « Combien de mes PV je dépense ce tour ? »
- **Son moteur** (règle de classe, sprint 204) : **la fureur**, la moitié de la part de PV qui lui manque en dégâts de plus, 40 % au plus, **au-delà du plafond des talents** comme la cible en état de l'Inquisiteur.
- **Trois briques** : **le sang** (un sort coûte des PV, jamais le dernier), **le vol de vie** (une part des dégâts rendue en PV) et **la forme** (la Rage : +25 % de dégâts pour tout le combat, ouvre 5 sorts du kit, aucune purge ne l'enlève ; le Berserker est dessiné plus grand).
- **24 sorts** et leurs 96 jumeaux ; **le Ravageur** (zones, PV dépensés), ses 2 lignées et 4 finales dès ce sprint, comme l'Assassin et le Malandrin au 204.

## 1. Le sang

| Brique | Données | Ce qu'elle fait | IA |
|---|---|---|---|
| **Sort payé en PV** | `"hpCost"` sur le sort | les PV d'abord (le coup sent déjà la fureur), jamais le dernier (`ActionError.NotEnoughHp`) ; événement `HpPaid` | les PV payés comptent pour la moitié de leur valeur |
| **Fureur** | `{"kind": "fury", "ratio": 50, "max": 40}` | + (part de PV manquants × ratio) % de dégâts, plafonnée | lue comme toute règle vivante, au choix de la cible |
| **Vol de vie** | `{"kind": "drain", "percent": 30}` | après les coups, le lanceur reprend la part des dégâts faits aux ennemis | la part des coups, au plus ce qui lui manque |

**Le barème** compte le sang : un PV payé vaut un dixième de PA, un sort qui demande la Rage 15 % de plus, le vol de vie 15 % de moins.

## 2. La Rage

`status` `Rage` de 25, 99 tours, posé par le sort Rage (niveau 5, 2 PA, 10 PV). La valeur s'ajoute aux dégâts comme un bonus ; les sorts marqués `"form": true` la demandent (`ActionError.NoForm`). **Elle ne se quitte pas** : aucune purge ne la prend (§1.4 de la conception : la forme du Druide est réversible, celle du Berserker se paie et reste). L'IA la vaut sur 8 tours. Le client grandit le modèle d'un cinquième (`FighterView.RageScale`) : aucun corps neuf, la direction artistique est reportée.

## 3. Les 24 sorts

| Niv. | Sort (id) | Élément | PA | Portée | Zone | Effets | PV | Rage | Relance |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Fendoir `cleaver` | Terre | 4 | 1 | | | | | |
| 1 | Hache de sang `blood-axe` | Feu | 3 | 1 | | | 6 | | |
| 1 | Coup de ressac `tide-blow` | Eau | 3 | 1-2 | | | | | |
| 1 | Moulinet `whirl` | Air | 4 | 1 | cercle 1 | | | | |
| 1 | Ruée `rush` | — | 2 | 2-4 en ligne | | bond | | | 1 |
| 5 | Rage `rage` | — | 2 | 0 | | **Rage +25 %, tout le combat** | 10 | | |
| 10 | Brise-os `bone-breaker` | Terre | 4 | 1 | | | 6 | | |
| 15 | Furie sanglante `blood-fury` | Feu | 5 | 1 | | | 10 | oui | |
| 20 | Déferlante `breaker` | Eau | 4 | 1-2 en ligne | ligne 2 | | | | |
| 25 | Tourbillon de guerre `war-whirl` | Air | 5 | 1 | cercle 1 | | | oui | |
| 30 | Hurlement `war-howl` | — | 3 | 0 | cercle 2 | ennemis −15 % de dégâts, 2 tours | | | 3 |
| 35 | Déchirure `rend` | Terre | 5 | 1 | | vol de vie 30 % | | | |
| 40 | Fendoir ardent `ember-cleave` | Feu | 5 | 1 | croix 1 | | 8 | | |
| 45 | Marée de sang `blood-tide` | Eau | 5 | 1-2 | | vol de vie 30 % | | oui | |
| 50 | Hache-tempête `storm-axe` | Air | 5 | 1-3 en ligne | | | | | |
| 55 | Pacte de sang `blood-pact` | — | 2 | 0 | | +2 PA, 1 tour | 15 | | 3 |
| 60 | Ébranlement `earthshaker` | Terre | 6 | 1 | cercle 1 | | 10 | oui | 1 |
| 65 | Hache infernale `inferno-axe` | Feu | 6 | 1 | | | 12 | | 1 |
| 70 | Maelström `maelstrom` | Eau | 6 | 1-2 | croix 1 | vol de vie 30 % | | | |
| 75 | Coup de tempête `tempest-blow` | Air | 6 | 1 | | | 12 | oui | 2 |
| 80 | Ivresse du sang `blood-rush` | — | 3 | 0 | | soin 15-20 | | | 4 |
| 85 | Inébranlable `unbreakable` | — | 3 | 0 | | +20 % de résistance, 2 tours | 15 | | 4 |
| 90 | Frénésie `frenzy` | — | 2 | 0 | | +30 % de dégâts, 2 tours | | | 3 |
| 95 | Élan furieux `onrush` | — | 1 | 0 | | +2 PM, 1 tour | | | 2 |

**Ce qui le sépare du Guerrier** (§1.4) : aucun bouclier, aucune attirance, aucun retrait de PM chez lui ; vérifié par un test.

## 4. Le Ravageur

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Ravageur** (`ravager`) | fureur +25 % du manque, 20 % de plus au plus ; **Ravage** (cercle 1, 10 PV), **Charge sanglante** (bond, croix 1, 6 PV) | Brise-crâne (`skullcracker`) | **Coup écrasant** (12 PV) | Destructeur (`ravager-destroyer`, Extends : Ravage en cercle 2) ; Titan brutal (LowHp : « Dans les cordes ») |
| | | Carnassier (`carnivore`) | **Dévorer** (vol de vie 50 %) | Boucher (Kill : « Prime touchée ») ; Dévoreur (Faster : Dévorer sans relance) |

## 5. Mesures

- **Les exigences en solo de la CI** (Disjoint seul à 30, grande salle de la Bauge, hordes de la Forêt) : tenues sans retouche du budget de 1,1. Elles jouent désormais 300 combats partagés entre les classes (50 chacune à 6 classes) : à 6 classes, la suite passait à 20 469 combats, au-delà des 20 000 de D97.
- **Les groupes de référence** (hordes de synthèse au niveau 45, 40 combats, nus) : Guerrier + Berserker 62 %, Archer + Berserker 72 %, Prêtre + Berserker 60 %, Mage + Voleur + Berserker 100 %, Guerrier + Prêtre + Berserker 100 %, Archer + Mage + Voleur + Berserker 100 %.
- **Les autres classes** jouent les mêmes combats qu'avant (empreintes en solo inchangées).
- **Les zones autour de soi** : au premier jet, l'IA ne jouait jamais le Ravage ni le Moulinet. Lancées sur une case voisine, ces zones touchaient le Berserker lui-même (le Fendage du Guerrier fait de même). Elles se lancent maintenant sur sa propre case, portée 0, et l'épargnent (`Fight.Spares`) ; une zone qui peut porter plus loin touche toujours son lanceur sur sa case.
- **La fumée** : les trois sorts du Ravageur payés en PV (Ravage, Charge sanglante, Coup écrasant) ne sont pas joués : un sort de voie au barème de son niveau reste sous les sorts du kit et leurs rangs, par PA. Même constat que le Coup bas du Voleur (Q5 de `classes-voleur.md`) : **à reprendre au jalon 2, pour toutes les classes (Q5)**. Le Dévoreur ne change aucun combat de la fumée.

## 6. À trancher par Charles

- **Q1. Les noms** des 24 sorts et des 4 sorts de voie : noms de travail, au scénariste.
- **Q2. La Rage pour tout le combat** : a) **oui, elle ne se quitte pas (Reco)**, comme la conception le veut ; b) une durée, et un sort pour la reprendre.
- **Q3. La fureur au-delà du plafond des talents** : a) **oui (Reco)**, sinon les 40 % et le bonus du Ravageur ne valent rien au-delà de 35 ; b) sous le plafond.
- **Q4. Le corps en Rage** : a) **grandi d'un cinquième en attendant la direction artistique (Reco)** ; b) un modèle de monstre (`orc`).
- **Q5. Les sorts de voie sous les sorts du kit** (Ravageur, Coup bas du Voleur) : a) **au jalon 2, monter les sorts de voie d'un rang pour toutes les classes et remesurer (Reco)** ; b) les régler un à un.
