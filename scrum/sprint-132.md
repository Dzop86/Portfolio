# Sprint 132 : Osmose : les Sylvecerfs

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'affronte les Sylvecerfs, leurs glyphes, leur chef, leur butin et leur panoplie (rpg) ; xUnit, T41 et `--party`. | 3 | Fait |

## Fait
- Conception du game designer (`docs/familles-foret.md`), textes relus par le scénariste (`docs/familles-foret-textes.md`) ; T100.
- `spells_gen.py` : `glyph()`, 12 sorts et 2 passifs (Ombrageux, Ramure fendue) ; la famille `stag` dans `monsters.json` (communs 31 à 34, chef 45) ; la Futaie ; le dessin du cerf.
- `sets_gen.py` : familles à 4 pièces, icônes gardées, pièces avant les objets de quête ; la panoplie du sylvecerf.
- T41 à 31 : PV des communs +17 % (le kit d'artisan battait les hordes) ; chef ≤ 10 % seul, échantillons à trois et quatre dans `PartyBalanceTests`.
- Godot : contrôle du survol corrigé pour les cases piégées ; combat des cerfs dans la CI (« Glyphs checked »).
- Limite : le premier coup d'un glyphe posé sous un héros ne s'évite pas (option moteur à proposer après S136).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
