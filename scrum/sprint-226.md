# Sprint 226 : Osmose : la zone de 46 à 60, sa seconde famille

**Objectif :** La seconde famille de monstres de la zone, réglée pour les dix classes (D102).

**Goal:** The zone's second monster family, balanced for the ten classes (D102).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'affronte la seconde famille de la zone, son chef, son butin et sa panoplie ; ses groupes gagnables seul et nu par chacune des dix classes (rpg) ; xUnit, T41 et `--party`. | 4 | Fait |

## Fait
- Les Vers de roche (`borer`) des Mines de Sous-Racine, conçus par le game designer (`docs/familles-mines.md` §4), textes du scénariste (`docs/familles-mines-textes.md` §3) : Ronge-pierre, Cracheur de gravier, Ver-meule, Ver tunnelier et leur chef le Ronge-montagne ; 11 sorts, 3 passifs (le Cuir de roche qui se referme au début du tour et s'ouvre quand le ver frappe, les Anneaux, les Couches à l'envers), le vermisseau, la panoplie du ver de roche (4 pièces), deux succès, la carte « Chambres et piliers » partagée avec les Fouille-rocs, le dessin `Borer`, les icônes, l'emblème et les portraits.
- 11 tests `BorerTests` (le cuir fermé, ouvert, refermé ; soigner ou plonger ne l'ouvre pas ; le poison frappe le ver encore ouvert ; la purge ôte le cuir, pas la blessure ; les Anneaux une fois par tour, à 2 cases, pas sur soi, de nouveau après le tour du Ver-meule ; la Galerie seulement pour un ver blessé ; le chef ; la panoplie ; le rejeu) ; une étape de la CI joue le combat du chef dans le client.
- Mesure à dix classes de toute la zone au niveau 46 : PV des Vers à −15 % du gabarit ; huit classes dans les bornes ; les hordes sous leurs plafonds pour neuf classes (le Berserker nu de 3,7 à 0,3 sur 10). Chef des Vers en groupe : 91 % sur 8 trios, 40 sur 40 à quatre.
- Limites et questions (T191) : le Prêtre a un petit groupe de trop sous 18 (Ver-meule et deux Ronge-pierre) ; l'Invocateur avec les pièces d'artisan gagne 2,5 hordes sur 10 (borne 2) ; le chef des Vers n'est battu que 10 % du temps à deux (l'Écoute-roche 95 %) ; pas d'échantillon en CI (budget plein).
- Arbitrage de Charles (24 nov.) : Prêtre et Invocateur craft acceptés hors bornes ; le chef des Vers reste un combat de groupe (PV inchangés).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
