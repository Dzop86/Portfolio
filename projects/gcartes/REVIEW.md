# Relecture humaine, gcartes (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/gmap.js` : conventions (orbites des cellules, couture α2 brin à brin au même sommet), boucles de bord, genre non orientable.
- [x] Le vocabulaire du cours correspond-il à celui de ma thèse ?
- [x] `course.json` : leçons et quiz justes et à mon goût ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide la review »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/gcourse/course.js` | (Claude) Ligne des comptes écrite en mélangeant les langues (« S/V 8 · A/E 12 ») | Modèle traduit |
| 2026-10-06 | `../../tests/e2e/site.spec.js` | (Claude) Test du cours au-delà de 30 s sous Firefox quand 5 navigateurs tournent ensemble (13 s seul) | Délai porté à 60 s |
| 2026-10-06 | `../../src/assets/style.css` | Signalé par Charles : patron du cube « totalement noir », illisible en thème sombre | (Claude) Figures sur fond blanc dans les deux thèmes, α0 noir, α1 rouge, α2 bleu, liaisons toujours dessinées ; ajout de la décomposition en quatre étapes |
| 2026-10-06 | `src/net.js` | (Claude) Vu sur capture : aux coins, les extrémités des deux brins se confondaient, la liaison α1 mesurait 1 px | Extrémités à 20 % du côté et 10 px du bord ; test qui exige 10 px au moins pour chaque α1 et chaque écart α0 |
| | | | |
