# Sprint 23 : le roguelike, les règles en C#

**Objectif :** premier des trois sprints du roguelike (D32) : les règles du jeu dans une bibliothèque C# déterministe, partagée plus tard par le client Godot et l'API de scores, jouable dès maintenant dans le terminal ; une partie s'enregistre (graine et actions) et se rejoue à l'identique, base de la vérification des scores au sprint 24.

**Goal:** first of the three roguelike sprints (D32): the game rules in a deterministic C# library, later shared by the Godot client and the score API, playable now in the terminal; a run is recorded (seed and actions) and replays identically, the basis of score checking in sprint 24.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue au roguelike (rogue) dans le terminal : donjon de cinq étages généré à partir d'une graine (salles reliées par des couloirs, toutes atteignables), champ de vision, déplacements et combats au tour par tour, monstres qui poursuivent le joueur, potions, or, escalier, score ; règles dans une bibliothèque C# sans dépendance, en français ou en anglais côté client ; tests xUnit, CI Linux, Windows et macOS. | 3 | Fait |
| En tant que joueur, j'enregistre ma partie et je la rejoue : format versionné (graine et actions), rejeu qui recalcule l'issue et le score, refus des parties invalides (action impossible, action après la fin, format inconnu) ; tests de rejeu sur des centaines de graines. | 2 | Fait |

**Résultat :** sur 1 000 parties, le pilote automatique sort du donjon 276 fois (27,6 %) et meurt surtout aux étages 4 et 5 ; une partie de 1 500 tours s'enregistre en 1,5 Ko.

**Tests :** 693 tests xUnit (500 étages générés, règles sur des étages dessinés à la main, rejeu de 100 parties complètes, parties de référence identiques sur Linux, Windows et macOS, 13 formats invalides, client en deux langues) ; analyseurs .NET, `dotnet format`. **Trouvé par les tests :** le jeu était trop facile (le pilote automatique gagnait toutes les parties d'essai), `>` était échappé dans le JSON, une option `--lang` invalide masquait la suivante.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
