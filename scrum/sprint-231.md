# Sprint 231 : Osmose : B16 mesurée en série

**Objectif :** Le seuil d'une seconde par tour (B16) se mesure en temps de calcul, un combat à la fois, et la mesure se répète (D103).

**Goal:** The one-second-per-turn threshold (B16) is measured in CPU time, one fight at a time, and the measure repeats (D103).

| Story | Points | État |
|---|---|---|
| En tant que Charles, `Rpg.Sim --durations` joue les combats en série et compte le temps de calcul du processus, pas l'horloge : trois mesures du même code à moins de 15 % l'une de l'autre, le seuil d'une seconde gardé, B16 jugée à 45 et à 60 (rpg) ; xUnit, mesure hors CI. | 1 | Fait : B16 tenue à 45 et à 60 (3 Invocateurs à 60 : 97, 95, 95 ms la machine au repos), T196 |

## Fait
- `--durations` joue les combats d'une ligne en série et compte le temps de calcul du processus (`Program.CpuTime`), le premier combat joué une fois avant, hors compte.
- Mesures à 60, la machine au repos : 3 Invocateurs 97, 95, 95 ms par tour (2 %) ; 182 à 192 ms avec `DOTNET_TieredCompilation=0`. À 45, sous la charge d'une autre suite : 108 à 138 ms (243 à 255 sans les tiers). B16 (1 000 ms) tenue partout ; l'ancienne mesure (520 à 1 336) comptait l'attente du parallèle.
- Limite : sous charge, le temps de calcul gonfle encore de 20 à 33 % ; les lignes légères bougent avec la recompilation du JIT. L'en-tête dit si les tiers sont coupés ; tiers non coupés dans le projet (le banc doublerait).
- Tests : 4 cas de plus dans `ProgramTests` ; le verdict de B16 n'est plus asserté (le temps des autres tests compte). Sim 36/36 verte en Release.
- Build 1.0.252 (CI rpg 45997106032, toute verte) livrée, l'API locale reconstruite.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
