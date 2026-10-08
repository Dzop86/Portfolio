# Sprint 40 : temps de calcul, cours G-cartes, chiffres et burndown à jour

**Objectif :** demandes de Charles (D49) : les temps de calcul de la persistance et du graphe de Reeb, avec ce qu'apporterait un GPU, et la limite de 32 Mo des fichiers expliquée ; le cours G-cartes corrigé ; le nombre de projets et de points à jour partout, et un burndown compté en stories livrées.

**Goal:** Charles's requests (D49): the computing times of persistence and of the Reeb graph, with what a GPU would bring, and the 32 MB file limit explained; the G-maps course corrected; the number of projects and points up to date everywhere, and a burndown counted in delivered stories.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je vois combien de temps prend chaque calcul (topologie) : temps CPU mesuré dans le navigateur (WebAssembly) pour la hauteur, la persistance et le graphe de Reeb, affiché sous chaque résultat ; un tableau de temps natif et WebAssembly mesurés de 10 000 à un million de triangles, produit par un banc d'essai versionné ; une note sur le GPU (ce qu'il accélérerait, pourquoi la réduction et les union-find s'y prêtent mal) et une sur la limite de 32 Mo, chiffres à l'appui ; FR/EN ; GoogleTest, Node et Playwright. | 2 | Fait |
| En tant qu'étudiant, je suis la décomposition d'un objet par dimensions croissantes (G-cartes) : objet, puis α0 (chaque arête en deux brins), α1 (les brins d'une face autour de chaque sommet), α2 (les faces cousues), puis la G-carte ; liaisons αi redessinées selon la convention des manuels (α0 en trait court entre les deux brins d'une arête, α1 en arc au coin, α2 en double trait entre deux faces), dans les étapes et sur le patron du cube ; tests unitaires de la géométrie (chaque liaison relie les bons brins, sans se confondre avec un brin), Playwright. | 2 | Fait |
| En tant que recruteur, je lis partout le bon nombre de projets et de points (vitrine) : captures des tableaux de bord React et Angular régénérées, textes alternatifs calculés depuis `data/projects.json`, et un test qui refuse un nombre de projets ou de points périmé dans les textes et les données. | 1 | À faire |
| En tant que recruteur, je lis un burndown juste (vitrine) : le reste compté en points de stories livrées, sprint après sprint, d'après `scrum/sprint-NN.md` ; chaque ajout de périmètre (D46 à D49) apparaît comme une marche au sprint où il arrive, sans réécrire les sprints passés ; « points restants » du backlog égal aux stories ouvertes (8 aujourd'hui, pas 45) ; textes de fin et tests unitaires suivent. | 1 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
