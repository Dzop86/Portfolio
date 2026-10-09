# Sprint 48 : le RPG tactique, la création de personnage

**Objectif :** un écran de création de personnage personnalisable, validé et sauvegardé par le serveur.

**Goal:** a customisable character creation screen, validated and saved by the server.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée mon personnage à mon goût (rpg) : classe (deux ou trois, chacune avec ses sorts, décrites en données), nom, apparence (couleurs, coiffure ou accessoires selon les visuels choisis), aperçu qui tourne ; le serveur refuse une apparence ou une classe inconnue ; français et anglais ; xUnit et tests de scène. | 3 | Fait |

**Résultat :** trois classes décrites en données (Sentinelle, Garde, Mage), chacune avec ses PV, PA, PM et sorts, réglées par simulation (69 à 74 % de victoires à l'entraînement) ; 12 apparences et 7 couleurs de tenue (un shader qui fait tourner la palette des modèles), avec un aperçu 3D qui tourne. Le serveur refuse une classe ou une couleur inconnue ; les personnages du sprint 47 deviennent Sentinelles (T13, T14). Pas de coiffures ni d'accessoires : les modèles de Kenney n'en ont pas de détachables.

**Tests :** 18 tests de plus (règles, simulateur, serveur, client) ; l'auto-test de l'écran crée une Mage de couleur 3 contre le serveur et vérifie en combat ses sorts et sa couleur ; 3 mutations, 3 attrapées. **Trouvé en route :** deux classes injouables à la première simulation, des pastilles qui ne montraient pas la couleur obtenue, un test de migration qui ne prouvait rien, deux mutations qui ne compilaient pas.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
