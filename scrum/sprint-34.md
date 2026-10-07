# Sprint 34 : le lancer de rayons, à la main du visiteur

**Objectif :** second sprint du lancer de rayons (D45) : le visiteur rend son propre maillage, le calcul se répartit sur tous les cœurs, il règle la lumière et les matières et enregistre l'image.

**Goal:** second ray tracer sprint (D45): visitors render their own mesh, the work is spread over every core, they adjust the light and the materials and save the image.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je rends mon propre maillage (raytracer) : fichier OBJ, PLY ou STL déposé ou choisi (32 Mo au plus, comme la visionneuse de topologie), lu par lib-c dans le worker ; erreurs affichées avec leur ligne, en français et en anglais ; Node (erreurs, maillage déposé) et Playwright. | 1 | À faire |
| En tant que visiteur, je profite de tous les cœurs (raytracer) : bandes réparties sur plusieurs web workers (selon `navigator.hardwareConcurrency`), même image octet pour octet qu'avec un seul grâce au générateur par pixel ; gain mesuré et affiché sur la fiche ; Node (répartition) et Playwright. | 1 | À faire |
| En tant que visiteur, je règle la scène et j'enregistre l'image (raytracer) : position et couleur de la lumière, rugosité du métal, indice du verre, en curseurs accessibles ; bouton « Enregistrer l'image » (PNG) ; captures haute définition rendues par le moteur natif sur la fiche ; GoogleTest (paramètres), Node et Playwright. | 1 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
