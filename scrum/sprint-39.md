# Sprint 39 : la persistance repensée

**Objectif :** relecture de Charles (D49) : le diagramme de persistance n'est pas clair. Le rendre lisible sans connaître la persistance : un code-barres à côté du nuage, chaque paire reliée à ses deux sommets sur le maillage, et une explication guidée qui suit le seuil de hauteur.

**Goal:** Charles's review (D49): the persistence diagram is not clear. Make it readable without knowing persistence: a barcode beside the scatter plot, each pair tied to its two vertices on the mesh, and a guided explanation that follows the height threshold.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je lis la persistance en code-barres (topologie) : une barre par paire, de sa naissance à sa mort sur l'axe des hauteurs, triée et groupée par dimension, les classes sans fin jusqu'au bord, le seuil de persistance qui grise les barres courtes comme dans le nuage ; le seuil de hauteur tracé en travers, qui montre les classes vivantes à ce niveau ; FR/EN, clavier, mobile ; Node et Playwright. | 1 | À faire |
| En tant que visiteur, je relie une paire au maillage (topologie) : survoler, toucher ou atteindre au clavier une barre, un point du nuage ou une ligne du tableau marque sur le maillage le sommet où la classe naît et celui où elle meurt, et place le seuil de hauteur entre les deux ; lecteur d'écran compris ; Node et Playwright. | 1 | À faire |
| En tant que visiteur, je suis une explication guidée (topologie) : quelques étapes (« le niveau monte : une composante naît au minimum », « à la selle, une boucle naît », etc.) qui placent le seuil de hauteur et mettent en avant la paire concernée, des axes et des légendes nommés en clair (« hauteur où elle naît », « hauteur où elle meurt ») ; FR/EN, clavier, mobile ; Playwright. | 1 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
