# Sprint 2 : chaque projet a sa page, le premier projet technique démarre

**Objectif :** un recruteur ouvre la fiche de n'importe quel projet ; la bibliothèque C a son squelette testé sur trois OS.

**Goal:** a recruiter opens the page of any project; the C library has its skeleton tested on three operating systems.

| Story | Points | État |
|---|---|---|
| En tant que recruteur, j'ouvre la fiche détaillée d'un projet (stack, état, liens, Definition of Done) en français et en anglais. | 2 | Fait |
| En tant que Charles, je démarre `projects/lib-c/` : CMake, lecteur OBJ, tests Unity, CI Linux, Windows et macOS. | 3 | Fait |

**Tests :** site 96 unitaires et d'intégration, 180 end-to-end (5 navigateurs, deux thèmes) ; lib-c 15 tests Unity et 3 tests CLI sous Linux, Windows, macOS et ASan + UBSan.

**Report au sprint 3 :** PLY, Valgrind, WebAssembly et miroir GitLab de lib-c.

**Report du sprint 1 :** néant. Thème gris façon VS Code et lien HAL livrés en début de sprint (D11).

## Rétro (Charles)
- Ce qui a marché : Chaque projet a sa fiche générée depuis `projects.json` ; lib-c compile sans avertissement sur trois OS, sous ASan et UBSan.
- Ce que l'IA a mal fait : Le sprint était trop chargé : PLY, Valgrind, WebAssembly et le miroir GitLab ont glissé au sprint 3.
- À changer au prochain sprint : Estimer plus serré : moins de stories, finies.
