# Sprint 50 : le RPG tactique, le launcher et la distribution

**Objectif :** un launcher en Rust (Tauri) qui connecte le joueur, met le jeu à jour en ne téléchargeant que les fichiers modifiés, puis le lance ; les exécutables des trois systèmes ; la fiche du projet.

**Goal:** a Rust (Tauri) launcher that logs the player in, updates the game by downloading only the changed files, then starts it; the executables for the three systems; the project page.

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'installe et je mets à jour le jeu par un launcher (rpg) : connexion au serveur, manifeste versionné (empreinte SHA-256 de chaque fichier), téléchargement et vérification des seuls fichiers modifiés, reprise après coupure, lancement du jeu avec le jeton ; tests Rust (cargo test) ; paquets Windows, macOS et Linux par la CI, non signés et documentés comme tels. | 3 | À faire |
| En tant que recruteur, je comprends le jeu sans l'installer (rpg) : fiche avec captures, courte vidéo et explication de l'architecture (client, règles partagées, serveur, launcher) ; Playwright. | 2 | À faire |

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
