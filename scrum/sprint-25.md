# Sprint 25 : le roguelike, le client Godot 4

**Objectif :** dernier des trois sprints du roguelike (D32) : le jeu dans une fenêtre Godot 4 en C#, sur la même bibliothèque de règles, avec des parties classées sur l'API de scores ; builds Windows, Linux et macOS par la CI, captures sur la fiche du projet.

**Goal:** last of the three roguelike sprints (D32): the game in a Godot 4 window in C#, on the same rules library, with ranked runs on the score API; Windows, Linux and macOS builds by the CI, screenshots on the project page.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue au roguelike (rogue) dans une fenêtre Godot 4 : carte dessinée, cases hors de vue assombries, panneau d'état et messages en français ou en anglais, clavier (flèches, zqsd, wasd), menu, pilote automatique à regarder ; scripts C# minces sur Rogue.Core ; test sans écran du client sur Linux, Windows et macOS ; exécutables Windows, Linux et macOS produits par la CI ; captures sur la fiche du projet. | 2 | Fait |
| En tant que joueur, je joue une partie classée depuis le client Godot : connexion ou création de compte, graine tirée par le serveur, envoi automatique de la partie, verdict et classement ; client HTTP dans une bibliothèque C# testée contre la vraie API. | 1 | Fait |

**Tests :** test sans écran du client Godot sur Linux, Windows et macOS (partie entière par le chemin du clavier, vérifiée par rejeu ; même issue que la partie de référence de la graine 9) ; exécutable Linux exporté relancé ; `ScoresClient` testé contre la vraie API ; fiche vérifiée par Playwright et axe dans cinq navigateurs ; 727 tests .NET. **Trouvé en route :** sans fichier `.sln`, Godot exporte sans le code C# en annonçant un succès ; le sol des cases déjà vues était invisible sur la première capture.

## Rétro (Charles)
- Ce qui a marché : Client Godot 4 testé sans écran sur trois OS, parties classées de bout en bout.
- Ce que l'IA a mal fait : Sans `.sln`, Godot exportait sans le C# en annonçant un succès ; le sol des cases vues était invisible sur la capture.
- À changer au prochain sprint : Relancer chaque export produit au lieu de croire le code de retour.
