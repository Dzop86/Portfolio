# Relecture humaine, aventure (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `Game.java` : commandes, sorties, combat.
- [ ] `Texts.java` : l'histoire et le ton me conviennent-ils, en français et en anglais ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `Game.java` | (Claude) Détecté par un test : combat mal équilibré, la partie se gagnait parfois à mains nues, l'indice du gardien ne servait à rien | Vie et coup du robot fixés pour que le parapluie décide, quel que soit le hasard (calcul en commentaire) |
| 2026-10-07 | `Game.java` | (Claude) Trois `replaceFirst` à expression régulière, qui donnaient des phrases fautives (« pas de le badge ») et gêneraient TeaVM | Phrases construites avec l'article |
| 2026-10-07 | `GameTest.java` | (Claude) Mes parties scriptées attaquaient « jusqu'à ce que le robot s'éteigne » : une mutation a fait boucler les tests sans fin | Combat borné à 10 coups ; la mutation fait maintenant échouer 3 tests |
| | | | |
