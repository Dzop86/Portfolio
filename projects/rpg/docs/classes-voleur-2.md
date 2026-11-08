# Le Voleur, seconde moitié : les poisons et l'Ombremage *(8 novembre 2026, sprint 205, à valider par Charles)*

Ce document prépare la story du sprint 205 : « En tant que Voleur, les poisons se cumulent, je deviens aussi Ombremage, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.2 le moteur : « ses poisons se cumulent » ; §1.3 l'Ombremage : disparaître, frapper depuis l'ombre ; §1.4 aucune zone large ; §3 la brique Q), de `classes-liste.md` §5.3 (les noms) et de `classes-voleur.md` (le kit, la Discrétion, l'Assassin et le Malandrin, dont les 8 finales sont déjà écrites).

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Deux briques** : le **cumul des poisons** (règle de classe `stack`) et le **coup depuis l'ombre** (règle de voie `shadow`).
- **L'Ombremage** : 2 sorts à 30, 1 par lignée à 50, **4 finales** dont une avec un passif neuf, « Évanouissement ». Le Voleur a désormais ses 3 branches, 6 lignées et 12 finales, comme les autres classes.
- **La consommation des poisons** (l'autre moitié de la brique Q) n'est pas faite : elle n'a pas de sort dans l'arbre du Voleur ; l'Occultiste du Démoniste (vague 3) en aura un (Q2).

## 1. Les poisons qui se cumulent

`{"kind": "stack", "max": 3}` dans les règles du Voleur, à côté de la cible isolée (règles de classe, sprint 204). Un poison du Voleur posé sur un combattant qui porte déjà **son** poison du **même élément** s'y ajoute : la dose grandit de la nouvelle, **jusqu'à 3 fois** la dose du sort, et dure le plus long des deux. Un autre élément est un autre poison. Sans la règle, rien ne change : le même état du même lanceur se renouvelle [V, `Fight.SetStatus`].

**L'IA** compte ce qu'un poison ajoute vraiment : la part de la dose qui s'ajoute à ce qui est là, sous le plafond (`Ai.Renewed`). Sans cela, elle prenait un second poison pour un simple renouvellement et ne le jouait pas.

**Le relevé des combats.** Seul le Voleur porte la règle : un combat enregistré avant se rejoue à l'identique.

## 2. L'Ombremage

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Règle et sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Ombremage** (`shadowmage`) | +20 % sur un coup porté caché ; **Pas de l'ombre**, **Lame d'ombre** | Traqueur nocturne (`night-stalker`) | Discrétion +1 tour ; **Guet-apens** | Spectre (Extends : Pas de l'ombre à 6 cases) ; Seigneur des ombres (FirstTurn : +2 PA au premier tour) |
| | | Illusionniste (`shadow-illusionist`) | **Faux-semblant** | Maître des illusions (Second : deux images) ; Fantôme (LowHp : « Évanouissement ») |

| Sort | Voie | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|
| Pas de l'ombre `path-shadow-step` | Ombremage | 2 | 1-4 | bond, **Discrétion 1 tour** | 2 |
| Lame d'ombre `path-shadow-blade` | Ombremage | 3 | 1-2 | dégâts | 1 |
| Guet-apens `path-waylay` | Traqueur nocturne | 5 | 1 | dégâts | 2 |
| Faux-semblant `path-false-front` | Illusionniste | 2 | 1-4, sans vue | une **Image** à l'apparence du Voleur (celle de l'Illusionniste du Mage), **Discrétion 2 tours** | 3 |

**Le coup depuis l'ombre** (`ShadowRule`, `shadow`) : le contexte d'un coup sait si le lanceur est caché quand il frappe ; la Discrétion tombe après le coup, qui en profite [V, `Fight.Cast`]. Le jeu de l'Ombremage : se cacher (Fondu, Fumigène, Pas de l'ombre, Faux-semblant), puis frapper fort une fois.

**La Discrétion qui dure** : la règle « +1 tour d'état » (`status-turns`) ne rallongeait que les états posés sur les ennemis ; elle rallonge aussi la Discrétion posée sur soi, et aucun autre bonus (les combats des autres voies ne changent pas).

**Le Fantôme** reçoit « Évanouissement » : une fois par combat, sous 30 % de ses PV, Discrétion 2 tours et +2 PM 1 tour. Le passif est une donnée (`trigger: LowHp`), comme « Dans les cordes ».

**Ce qui sépare les deux Illusionnistes** (`classes-conception.md` §1.4) : celle du Mage déplace l'attention des monstres et frappe avec son Double ; celui du Voleur laisse l'Image à sa place pour sortir caché.

## 3. Mesures

- **Le Voleur seul** : même empreinte qu'au sprint 204 au niveau 10 (l'entraînement ne lui fait pas poser deux fois le même poison) ; les quatre autres classes, inchangées.
- **Les exigences en solo de la CI** (Disjoint seul à 30, grande salle de la Bauge, hordes de la Forêt) et **les 6 groupes de référence du Voleur** : tenus, sans retouche.
- **La fumée de la CI** : l'IA ne joue pas le Pas de l'ombre (un bond, comme la Cabriole : `Unplayed`) ; le Traqueur nocturne préfère le Guet-apens à la Lame d'ombre (`UnplayedOn`) ; Spectre et Maître des illusions ne changent aucun des combats d'un héros de 100 (`Unchanged`). L'IA des bonds est à reprendre au jalon 2 (Q5 de `classes-voleur.md`).

## 4. À trancher par Charles

- **Q1. Les noms** des 4 sorts de voie et du passif : noms de travail, au scénariste.
- **Q2. Consommer les poisons** (brique Q, seconde moitié) : a) **à l'Occultiste, vague 3 (Reco)**, le Voleur n'a pas de place dans son arbre ; b) un sort de plus au Voleur, en remplaçant un neutre du kit.
- **Q3. Le plafond du cumul** : a) **3 doses (Reco)** ; b) sans plafond, au risque d'un Voleur qui empoisonne à l'infini un boss.
