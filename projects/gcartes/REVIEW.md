# Relecture humaine, gcartes (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/gmap.js` : conventions (orbites des cellules, couture α2 brin à brin au même sommet), boucles de bord, genre non orientable.
- [x] Le vocabulaire du cours correspond-il à celui de ma thèse ?
- [x] `course.json` : leçons et quiz justes et à mon goût ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide la review »).

- [ ] Sprint 40 : `src/decompose.js` (objet, α0, α1, α2, G-carte, dans l'ordre des dimensions) et `src/links.js` (α0 trait court, α1 arc au coin, α2 double trait) : est-ce bien la convention de tes manuels ? Textes des cinq étapes (`gcartes.step.*` dans `data/i18n`).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/gcourse/course.js` | (Claude) Ligne des comptes écrite en mélangeant les langues (« S/V 8 · A/E 12 ») | Modèle traduit |
| 2026-10-06 | `../../tests/e2e/site.spec.js` | (Claude) Test du cours au-delà de 30 s sous Firefox quand 5 navigateurs tournent ensemble (13 s seul) | Délai porté à 60 s |
| 2026-10-06 | `../../src/assets/style.css` | Signalé par Charles : patron du cube « totalement noir », illisible en thème sombre | (Claude) Figures sur fond blanc dans les deux thèmes, α0 noir, α1 rouge, α2 bleu, liaisons toujours dessinées ; ajout de la décomposition en quatre étapes |
| 2026-10-06 | `src/net.js` | (Claude) Vu sur capture : aux coins, les extrémités des deux brins se confondaient, la liaison α1 mesurait 1 px | Extrémités à 20 % du côté et 10 px du bord ; test qui exige 10 px au moins pour chaque α1 et chaque écart α0 |
| 2026-10-08 | `src/decompose.js` | Signalé par Charles (D49) : la décomposition coupait dans l'ordre α2, α1, α0, l'inverse de la définition, et les liaisons ne suivaient pas la convention des manuels | (Claude) Construction par dimensions croissantes (objet, α0, α1, α2, G-carte) ; `src/links.js` partagé par les étapes, le patron du cube et la liaison du brin choisi ; tests de chaque liaison (bons brins, bon endroit, sans se confondre avec un brin) |
| 2026-10-08 | `src/net.js` | (Claude) Vu sur capture : sur le patron, les arcs α1 sortaient du carré, car leur point de contrôle était le vrai coin alors que les brins sont décalés de 10 px vers l'intérieur | Point de contrôle au croisement des deux brins (`corner`), testé identique pour les deux brins d'une liaison α1 |
| 2026-10-08 | `src/decompose.js` | (Claude) Vu sur capture : l'écart α0 entre les deux moitiés d'une arête se voyait à peine ; un test exige maintenant qu'il reste plus court qu'un demi-brin | Écart porté de 15 à 18 % de la demi-arête (la limite du test est 20 %) |
| | | | |
