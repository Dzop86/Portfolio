# Sprint 44 : chaque projet expliqué sans jargon

**Objectif :** demande de Charles (D53) : sur chaque fiche, un résumé pour une personne qui n'est pas informaticienne (une RH, par exemple) : ce qu'est le projet, à quoi il sert, ce qu'il montre des compétences de Charles, en trois phrases courtes, dans un encart dépliable en haut de la fiche.

**Goal:** Charles's request (D53): on each project page, a summary for someone who is not a developer (an HR person, for instance): what the project is, what it is for, what it shows of Charles's skills, in three short sentences, in a collapsible box at the top of the page.

| Story | Points | État |
|---|---|---|
| En tant que recruteuse ou recruteur non informaticien, je comprends chaque projet en trente secondes (vitrine) : encart « En bref, sans jargon » en haut de chaque fiche, fermé par défaut, qui s'ouvre au clic, au clavier ou au doigt, sans JavaScript ; trois phrases (ce que c'est, à quoi ça sert, ce que ça montre) pour les 21 projets, en français et en anglais ; un test qui exige chaque résumé dans les deux langues, court, et sans mots techniques ; Playwright (ouverture, clavier, mobile, accessibilité). | 2 | Fait |

**Tests :** 2 tests Node (`tests/unit/plain.test.mjs` : les trois phrases de chaque projet dans les deux langues, de 30 à 260 caractères, sans une liste de 32 mots techniques, vérifiée sur « API » et « rapide » ; l'encart en haut de chaque fiche, fermé, avant les liens) ; 1 test Playwright (5 navigateurs : ouverture au clic, au clavier par Entrée et Espace, cible de 44 px, pas de défilement horizontal, axe dans les deux thèmes, en français et en anglais). **Fait :** l'encart « En bref, sans jargon » en haut des 21 fiches, et les 42 résumés. **Trouvé en route :** le bloc inséré d'abord dans la carte de la liste des projets (même ligne repère que la fiche) ; un test Firefox hors délai une fois sur trois passages, cinq navigateurs en parallèle (60 s comme les autres tests lourds).

## Rétro (Charles)
- Ce qui a marché : Les 21 projets se lisent en trente secondes sans être informaticien ; un test refuse le jargon, la troisième personne et un résumé manquant dans une langue.
- Ce que l'IA a mal fait : Des résumés d'abord écrits à la troisième personne, une accroche de la topologie trop technique et un encart qui ne semblait pas cliquable : trois retours de ma part pour une story de 2 points.
- À changer au prochain sprint : Pour un texte destiné à un non-informaticien, me montrer deux ou trois exemples avant de les écrire tous ; regarder une capture de chaque nouvel élément d'interface avant de me le soumettre.
