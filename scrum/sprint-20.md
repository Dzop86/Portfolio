# Sprint 20 : une aventure textuelle en Java

**Objectif :** le projet aventure réécrit de zéro un jeu de mes études (l'original, écrit à deux, n'est pas repris) : « Le laboratoire de nuit », une aventure en mode texte où un doctorant enfermé dans son laboratoire doit retrouver la clé USB de sa thèse et sortir, en français ou en anglais, jouable dans le terminal puis dans le navigateur.

**Goal:** the aventure project rewrites from scratch a game from my studies (the original, written by two, is not reused): “The lab at night”, a text adventure in which a PhD student locked in the lab must find the USB stick holding the thesis and get out, in French or English, playable in the terminal then in the browser.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue à l'aventure (aventure) dans le terminal : lieux et sorties (certaines verrouillées), objets à prendre, poser et utiliser, un gardien qui donne des indices, un combat, victoire et défaite ; commandes en français et en anglais ; tests JUnit 5 (dont une partie complète scriptée dans chaque langue), CI sur trois systèmes. | 3 | Fait |
| En tant que visiteur, je joue à l'aventure (aventure) sur la fiche du projet : le moteur Java compilé en JavaScript par TeaVM, un terminal dans la page ; tests Node et Playwright. | 2 | Fait |

**Tests :** 10 tests JUnit (dont 200 combats et deux parties complètes), 4 tests Node sur le JavaScript compilé par TeaVM, un scénario Playwright dans 5 navigateurs.

## Rétro (Charles)
- Ce qui a marché : Aventure réécrite de zéro, jouable en terminal et sur la fiche grâce à TeaVM.
- Ce que l'IA a mal fait : Rien de notable.
- À changer au prochain sprint : La bataille en Ada.
