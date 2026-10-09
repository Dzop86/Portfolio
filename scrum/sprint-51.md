# Sprint 51 : Osmose, le launcher refait

**Objectif :** un launcher qui se résume à se connecter : il met le jeu à jour tout seul, mémorise le compte, permet de s'inscrire, et lance le jeu.

**Goal:** a launcher that comes down to signing in: it updates the game by itself, remembers the account, lets the player sign up, and starts the game.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je me connecte et le jeu se lance (rpg) : nom de compte, mot de passe et « Se connecter » seulement ; la connexion vérifie la mise à jour, télécharge si besoin, puis lance le jeu avec le jeton ; mise à jour vérifiée dès l'ouverture ; adresse du serveur cachée (réglage avancé) ; présentation avec une image du jeu ; tests Rust et capture. | 2 | Fait |
| En tant que joueur, le launcher se souvient de moi et je peux m'inscrire (rpg) : cases « mémoriser le nom » et « mémoriser le mot de passe » (gestionnaire d'identifiants du système, jamais en clair sur le disque) ; inscription depuis le launcher ; tests. | 1 | Fait |

**Résultat :** le launcher se résume à se connecter : mise à jour automatique à l'ouverture, nom de compte, mot de passe, « Se connecter » qui attend la mise à jour et lance le jeu, cases pour mémoriser le nom et le mot de passe (gestionnaire d'identifiants du système), création de compte, serveur caché, bannière du village (T20).

**Tests :** 7 tests Rust de plus (20 en tout), 2 tests Playwright de la page sur les cinq navigateurs avec un faux Tauri. **Trouvé en route :** un champ caché qui s'affichait, une fenêtre trop courte, des décimales à l'anglaise, une page sans aucun test, une bannière avec du texte anglais (mode de capture `--shot banner` ajouté au jeu).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
