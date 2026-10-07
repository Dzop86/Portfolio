# Relecture humaine, ada (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/traffic.adb`, `Tick` et `Duration_Of` : une demande arrivée pendant l'orange ou le rouge intégral est-elle traitée comme attendu ?
- [x] `src/traffic.ads` : les postconditions disent-elles tout ce qui compte pour la sûreté ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide juste la review ada »).
- [ ] Sprint 35 : `src/automaton.adb` (exploration des états atteignables, JSON) et l'interface de la fiche (`src/assets/crossroads-core.js`, `adaplay.js`).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `alire.toml` | (Claude) Alire exige un e-mail dans `maintainers`, interdit dans le dépôt (règle de confidentialité) | Champ omis (A2) |
| 2026-10-06 | `tests/src/traffic_tests.adb` | (Claude) Une réécriture du test des durées hors bornes le faisait passer même sans exception | Assertion d'échec dans le bloc ; vérifié en élargissant l'intervalle : le test échoue |
| 2026-10-06 | `src/traffic.ads` | (Claude) Détecté par `gnatprove` : la postcondition de `Light` appelait `Light`, terminaison non prouvable | Sûreté énoncée par un lemme fantôme prouvé (A3) |
| 2026-10-07 | `src/assets/adaplay.js` | (Claude) Relecture : une demande sur l'axe déjà au vert, que le contrôleur ignore, était annoncée « en attente » au lecteur d'écran | Deux annonces : demande enregistrée, ou axe déjà au vert ; testé par Playwright |
| | | | |
