# Sprint 194 : Osmose : l'IA, le moteur à quatre rangs

**Objectif :** Dix classes remplacent la Mage, la Garde et la Sentinelle : 220 classes en tout, 3 choix à 30, 2 à 50, 2 à 100, livrées par vagues de 4, 7 et 10 classes (D96).

**Goal:** Ten classes replace the Mage, the Guard and the Sentinel: 220 classes in all, 3 choices at 30, 2 at 50, 2 at 100, delivered in waves of 4, 7 and 10 classes (D96).

| Story | Points | État |
|---|---|---|
| En tant que joueur, l'IA sait valoir une glyphe posée, un saut, une réserve, un bouclier et une invocation, et la mesure dit enfin si la Sentinelle à ±22 vient de l'IA ou des sorts (rpg) ; xUnit, résultat dans `DECISIONS.md`. | 3 | Fait en partie : bouclier et invocation faits, glyphe et saut essayés puis retirés (l'IA y perd jusqu'à 19 points), réserve non faite, elle attend le Mage ; diagnostic partiel (T154) |
| En tant que joueur, je choisis à 100 une des 2 classes finales de ma lignée : données, validation et serveur (rpg) ; xUnit, un test d'intégration de l'API. | 2 | Fait en partie : moteur, validation, colonne et serveur faits, aucune finale dans les données avant S197, donc le choix réussi n'est testé qu'au niveau du Cœur (T155) |

**Ce qui est fait.** `Ai.Incoming`, valeur des créatures, boucliers et résistances pour un héros, `Hero.LeastWorth` selon ses points, garde-fou de la Sentinelle ramené de ±22 à ±15, `docs/mesures-classes.md` (T154). `SpecFinal`, `Hero.Final`, validation, `PUT …/spec`, migration `AddFinal`, `FinalsTests` (T155).

**Ce qui n'est pas fait.** Valeur de la glyphe et du saut (essayées, retirées : l'IA y perd jusqu'à 19 points), réserve (attend le Mage). 7 lignes sur 24 concluantes avec le paquet automatique, comme en S193. En jeu, ces valeurs d'IA ne servent pas : l'IA n'y joue pas les héros.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
