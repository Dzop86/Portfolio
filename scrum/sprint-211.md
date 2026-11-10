# Sprint 211 : Osmose : l'Invocateur, les ordres

**Objectif :** Dix classes remplacent la Mage, la Garde et la Sentinelle : 220 classes en tout, 3 choix à 30, 2 à 50, 2 à 100, livrées par vagues de 4, 7 et 10 classes (D96).

**Goal:** Ten classes replace the Mage, the Guard and the Sentinel: 220 classes in all, 3 choices at 30, 2 at 50, 2 at 100, delivered in waves of 4, 7 and 10 classes (D96).

| Story | Points | État |
|---|---|---|
| En tant qu'Invocateur, l'IA joue mes créatures et un sort marque leur cible, 3 créatures au plus (rpg) ; xUnit. PloufInvocateur créé. | 5 | Fait, sauf PloufInvocateur (Docker indisponible dans WSL) : classe `summoner`, 24 sorts, Molosse, Feu follet et Colosse nourris de ses PV et caractéristiques, l'ordre (+25 %, une cible), 3 créatures au plus par héros, l'IA lève ses créatures à tout niveau ; Q1-Q4 de `docs/classes-invocateur.md` à trancher (T175) |
| En tant que Charles, l'IA donne une valeur aux pièges, aux retraits de PM, aux tractions et aux sorts d'alliés de mes voies : Arrachement, Cercle de fer, Rune, Mâchoires, Résurrection, Cri du clan, Bouclier de la foi (rpg) ; xUnit, banc hors CI (D99). | 3 | Fait : retrait de PM au coup empêché, piège sous un ennemi qui reste, traction au coup mis à portée, bonus aux alliés à une part de leurs coups ; la Trouée du Bélier pousse de 2 ; le Disjoint devient trop facile (Q4) ; la Résurrection et le Bouclier de la foi restent des sorts de groupe (T176) |

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
