# Relecture humaine, rogue (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/Rogue.Core/Game.cs` : déroulé d'un tour, combat, poursuite des monstres, niveaux.
- [ ] `src/Rogue.Core/Replay.cs` : format de partie et refus des parties invalides (base de la vérification des scores).
- [ ] Le jeu ressemble-t-il à celui de mes études ? Difficulté (le pilote automatique sort du donjon une fois sur quatre) ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `tests/*.csproj` | (Claude) Détecté au premier lancement : le SDK .NET 10 refuse l'adaptateur VSTest de xUnit v3 | `global.json` qui choisit Microsoft.Testing.Platform, adaptateur retiré |
| 2026-10-07 | `src/Rogue.Core/Replay.cs` | (Claude) Détecté par un test : le JSON écrivait `>` sous la forme `>` (encodeur par défaut, prévu pour du HTML), parties illisibles | Encodeur `UnsafeRelaxedJsonEscaping` : JSON toujours valide, caractères lisibles |
| 2026-10-07 | `src/Rogue.Core/Game.cs` | (Claude) Détecté par un test : le pilote automatique gagnait les 200 parties d'essai (niveau 8 en moyenne), le soin complet à chaque niveau rendait le jeu trop facile | Niveau : +5 PV sans soin complet, défense tous les 3 niveaux, expérience 30 × niveau, monstres plus robustes ; 27,6 % de sorties sur 1 000 parties |
| 2026-10-07 | `src/Rogue.Cli/Program.cs` | (Claude) Détecté par un test : avec `--lang de --lang en`, l'erreur s'affichait en français (la première option `--lang` était lue, même invalide) | La dernière option `--lang` valide l'emporte |
| | | | |
