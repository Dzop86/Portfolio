# Sprint 50 : le RPG tactique, le launcher et la distribution

**Objectif :** un launcher en Rust (Tauri) qui connecte le joueur, met le jeu à jour en ne téléchargeant que les fichiers modifiés, puis le lance ; les exécutables des trois systèmes ; la fiche du projet.

**Goal:** a Rust (Tauri) launcher that logs the player in, updates the game by downloading only the changed files, then starts it; the executables for the three systems; the project page.

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'installe et je mets à jour le jeu par un launcher (rpg) : connexion au serveur, manifeste versionné (empreinte SHA-256 de chaque fichier), téléchargement et vérification des seuls fichiers modifiés, reprise après coupure, lancement du jeu avec le jeton ; tests Rust (cargo test) ; paquets Windows, macOS et Linux par la CI, non signés et documentés comme tels. | 3 | Fait |
| En tant que recruteur, je comprends le jeu sans l'installer (rpg) : fiche avec captures, courte vidéo et explication de l'architecture (client, règles partagées, serveur, launcher) ; Playwright. | 2 | Fait |

**Résultat :** le launcher en Rust (Tauri 2) : connexion, mise à jour fichier par fichier à partir d'un manifeste versionné (SHA-256), reprise des téléchargements coupés, lancement du jeu avec le jeton, en français et en anglais ; paquets Windows, macOS et Linux par la CI, non signés. Le serveur publie les exports du jeu sous `/updates`. La fiche montre une vidéo de 36 secondes enregistrée par le jeu, le schéma de l'architecture et le launcher (T17, T18).

**Tests :** 13 tests Rust (dont la reprise contre un vrai serveur HTTP et le refus d'un manifeste dangereux), 3 du serveur ; dans la CI, le launcher compilé, testé et empaqueté sur les trois systèmes, puis le jeu exporté installé, mis à jour, réparé et joué depuis le dossier du launcher ; Playwright sur la fiche. **Trouvé en route :** le déploiement de 718253f en échec (une date de licence prise pour un numéro de téléphone, 80a6b70), un `.pck` embarqué qui aurait tout fait retélécharger, une règle `.gitignore` qui cachait des sources, une description de vidéo écrite avant de la voir, une fiche absente des tests d'accessibilité.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
