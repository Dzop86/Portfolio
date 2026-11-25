# Sprint 228 : Osmose : l'artisanat des niveaux 50 à 60

**Objectif :** Les métiers suivent la zone de 46 à 60 (D102).

**Goal:** The trades follow the zone from 46 to 60 (D102).

| Story | Points | État |
|---|---|---|
| En tant qu'artisan, je fabrique les pièces, potions, pains et clefs des niveaux 50, 55 et 60 (rpg) ; xUnit, budget et kit `craft`. | 3 | Fait |

## Fait
- Douze pièces d'artisan aux niveaux 50, 55 et 60 (objets de niveau 47, 50, 53), trois par métier, les quatre éléments trois fois chacun ; à 60, sans ressource récoltée de ce niveau, les ressources de 55 et une de chaque famille des Mines.
- L'élixir de soin supérieur (50) et suprême (60), le pain d'orge (50), la galette de millet (55), la miche des mineurs (60) ; les clefs des donjons étaient venues au S227.
- Tests : `ItemBudgetTests` (13 niveaux, 52 pièces, les recettes de 60 à quatre ingrédients, une ressource par famille), `ConsumableTests`, `ShopTests` ; théorie hors CI du kit `craft` sur les hordes des Mines à 60 : gains de −0,8 à +1,0 sur 10, moyenne +0,2 (T193).
- Limites : noms non relus par le scénariste ; dessins déjà au dépôt ; pas d'échantillon en CI (budget plein).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
