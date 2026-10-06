# Sprint 3 : la bibliothèque C lit aussi le PLY et tourne dans le navigateur

**Objectif :** lib-c lit OBJ et PLY sans fuite mémoire, et un visiteur l'essaie sur la fiche du projet, compilée en WebAssembly.

| Story | Points | État |
|---|---|---|
| En tant que Charles, je prouve l'absence de fuite de lib-c : Valgrind passe en CI sur les tests et la CLI. | 1 | Fait |
| En tant qu'utilisateur de lib-c, je lis un maillage PLY (ASCII et binaire little-endian). | 2 | Fait |
| En tant que recruteur, j'essaie lib-c dans le navigateur : je dépose un OBJ ou un PLY et j'obtiens ses statistiques. | 2 | Fait |
| En tant que Charles, lib-c est aussi testée par GitLab CI sur un miroir. | 1 | Bloqué : compte GitLab et jeton à créer par Charles |

**Hors sprint, à la demande de Charles :** barres d'avancement temporaires sur l'accueil (D14).

**Tests :** site 121 unitaires et d'intégration, 185 end-to-end (5 navigateurs, deux thèmes) ; lib-c 38 tests Unity et 5 tests CLI sur 3 OS, ASan + UBSan, Valgrind, fuzzing 60 s, WebAssembly vérifié.

**Report :** miroir GitLab (bloqué, en attente du compte et du jeton) ; écriture OBJ et PLY.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
