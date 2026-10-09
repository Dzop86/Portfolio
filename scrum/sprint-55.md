# Sprint 55 : Osmose, invocations, rangs de sorts et caractéristiques

**Objectif :** Les invocations, les sorts à rangs débloqués par niveau, et les caractéristiques dans le calcul des dégâts.

**Goal:** Summons, spells with ranks unlocked by level, and characteristics in the damage formula.

| Story | Points | État |
|---|---|---|
| En tant que joueur, mes caractéristiques et le rang de mes sorts comptent (rpg) : Vitalité, Force, Intelligence, Chance, Agilité (une par élément) dans les PV et les dégâts ; sorts à rangs 1 à 5 et niveau de déblocage ; invocations (créatures jouées par l'IA du côté de l'invocateur) ; xUnit, simulation. | 3 | Fait |

**Résultat :** les cinq caractéristiques comptent dans les PV, les dégâts de leur élément et les soins ; les sorts ont un niveau de déblocage et jusqu'à cinq rangs ; les invocations rejoignent l'équipe, jouent après leur invocateur et meurent avec lui (T26). Combats enregistrés et auto-tests inchangés.

**Tests :** 14 tests de plus ; 4 mutations, 3 attrapées, 1 équivalente retirée. **Trouvé en route :** une IA qui n'invoquait jamais, une condition inutile.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
