# Sprint 43 : un repère dans la visionneuse

**Objectif :** demande de Charles (D52) : un petit repère d'orientation dans la visionneuse de topologie, x en rouge, y en vert et z en bleu, qui tourne avec la caméra, pour savoir où l'on regarde et le relier à l'axe de la hauteur.

**Goal:** Charles's request (D52): a small orientation gizmo in the topology viewer, x in red, y in green and z in blue, turning with the camera, to know where one is looking and relate it to the height axis.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je vois dans quel sens je regarde le maillage (topologie) : un repère dans un coin de la visionneuse, x rouge, y vert, z bleu, avec ses lettres (la couleur n'est pas seule), qui suit la rotation de la caméra ; couleurs de la charte, thèmes clair et sombre, mobile ; Node et Playwright (pixels du coin lus dans la capture, avant et après rotation). | 1 | Fait |

**Tests :** 1 test Node (taille du repère entre 56 et 110 px ; jetons `--axis-x`, `--axis-y`, `--axis-z` dominés par leur canal rouge, vert, bleu) ; 1 test Playwright (4 navigateurs à WebGL, Firefox sauté faute de WebGL en CI) : les pixels rouges, verts et bleus du coin, lus dans la capture, puis un coin différent après une rotation à la souris ; vérifié en retirant le dessin du repère (0 pixel). **Fait :** repère dessiné par three.js dans une petite vue orthographique du coin bas-gauche, dans la direction de la caméra, flèches et lettres cerclées de noir, lisible dans les deux thèmes ; aide de la fiche complétée.

## Rétro (Charles)
- Ce qui a marché : Repère dessiné dans le même canvas, qui suit la caméra, lisible dans les deux thèmes ; testé par les pixels de la capture, avant et après rotation.
- Ce que l'IA a mal fait : Rien de notable ; le test ne peut pas tourner sous Firefox en CI, faute de WebGL.
- À changer au prochain sprint : Rien de prévu : tous les projets sont terminés, la vitrine continue au fil des demandes.
