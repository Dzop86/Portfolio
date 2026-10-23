# Sprint 135 : Osmose : les recettes des niveaux 40 et 45

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant qu'artisan, je fabrique les pièces, potions, pains et clefs des niveaux 40 et 45 (rpg) ; xUnit, budget et kit `craft`. | 3 | Fait |

## Fait
- `craft_gen.py` monte aux niveaux 40 et 45, avec les ressources de la Forêt des Aïeux et le butin de ses deux familles (Andouiller de pierre et Peau de cerf des Sylvecerfs à 40, Moellon moussu et Mousse de gardien des Gardiens moussus à 45).
- Huit pièces, une par métier et par niveau, comme la règle « une recette tous les 5 niveaux » (le plan disait 16, compté deux fois) : Marteau de granit et Pavois d'orme (forgeron), Pourpoint d'épeautre et Capuche de sorgho (tailleur), Anneau de granit et Amulette de malachite (bijoutier), Bottes en peau de truite et Ceinture en cuir de silure (cordonnier). Les quatre éléments deux fois chacun, comme dans les autres zones.
- Une potion, le Grand élixir de soin (niveau 40 : les potions vont de 10 en 10 depuis S111, le plan en comptait deux), et deux pains, le Pain d'épeautre et la Galette de sorgho. Rose vend l'élixir comme les autres potions. Les clefs des niveaux 40 sont déjà faites en S134.
- Tests : `ItemBudgetTests` (40 pièces, recettes de 1 à 45, la Forêt couvre les quatre éléments), `ConsumableTests`, `ShopTests`, et dans `BalanceTests` les hordes de la Forêt au niveau 45 avec le kit `craft`.
- **Question pour Charles :** le contrôle prévu (au plus 2 hordes sur 10 seul au niveau 45) ne tient pas, mais les nouvelles pièces n'y sont presque pour rien. Avec les pièces de 35 et moins, un héros de 45 gagne déjà 3,1 à 4,6 hordes sur 10, parce que les monstres de la Forêt sont de niveau 31 à 37 ; les pièces de 40 et 45 ajoutent de −0,1 à +1,8. Le test vérifie donc leur apport (au plus 2,5 par classe). Faut-il des hordes plus fortes au fond de la Forêt ?

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
