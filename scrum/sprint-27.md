# Sprint 27 : la visionneuse Qt/OpenGL, sélectionner et livrer

**Objectif :** second sprint de la visionneuse (projet qt) : désigner un sommet ou une face pour en lire les propriétés locales, enregistrer la vue en image, et livrer l'application prête à installer sur Windows, Linux et macOS.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur, je clique sur le maillage pour sélectionner le sommet ou la face sous le curseur : élément mis en évidence, panneau de ses propriétés (indice, position, valence, courbure de Gauss et défaut angulaire, bord ; aire et normale d'une face) ; sélection au clavier aussi ; lancer de rayon testé sans OpenGL ; j'enregistre la vue en image PNG. | 4 | Fait |
| En tant qu'utilisateur, je télécharge la visionneuse prête à lancer : archive Windows (windeployqt), image disque macOS (macdeployqt), AppImage Linux, produites par la CI, chacune lancée une fois après emballage pour vérifier qu'elle démarre ; limites (applications non signées) documentées. | 3 | Fait |

**Tests :** six programmes Qt Test (dont le lancer de rayon, sans OpenGL, et la sélection dessinée en chocolat) ; chaque paquet lancé une fois par la CI ; l'AppImage lancée en plus sur une Ubuntu 24.04 vierge. **Trouvé en route :** linuxdeploy ne trouvait pas un Qt installé hors du système ; mon test d'image visait le trou du tore.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
