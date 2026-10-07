# Sprint 23 : le roguelike, les règles en C#

**Objectif :** premier des trois sprints du roguelike (D32) : les règles du jeu dans une bibliothèque C# déterministe, partagée plus tard par le client Godot et l'API de scores, jouable dès maintenant dans le terminal ; une partie s'enregistre (graine et actions) et se rejoue à l'identique, base de la vérification des scores au sprint 24.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue au roguelike (rogue) dans le terminal : donjon de cinq étages généré à partir d'une graine (salles reliées par des couloirs, toutes atteignables), champ de vision, déplacements et combats au tour par tour, monstres qui poursuivent le joueur, potions, or, escalier, score ; règles dans une bibliothèque C# sans dépendance, en français ou en anglais côté client ; tests xUnit, CI Linux, Windows et macOS. | 3 | À faire |
| En tant que joueur, j'enregistre ma partie et je la rejoue : format versionné (graine et actions), rejeu qui recalcule l'issue et le score, refus des parties invalides (action impossible, action après la fin, format inconnu) ; tests de rejeu sur des centaines de graines. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
