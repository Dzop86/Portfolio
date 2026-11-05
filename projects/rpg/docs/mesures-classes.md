# Osmose : les mesures des classes (le banc, D96)

Les résultats de `Rpg.Sim --matrix` (`classes-conception.md` §7.2, niveau N2). À compléter à chaque classe et à chaque jalon. Ce que le banc mesure, c'est l'IA et ses paquets, pas ce qu'une voie vaut entre de bonnes mains.

## 5 novembre 2026 : l'état de départ, à 3 classes (S193, avant l'IA de S194)

**Conditions.** Héros nus, famille de synthèse au niveau voulu, 16 groupes de 4 et 5 monstres, 30 combats par groupe, soit 480 par ligne, mêmes graines. L'intervalle à 95 % d'une ligne est d'environ ±4,5 points autour de 50 %. Le Mage n'a encore aucune voie : ses lignes ne disent rien de ses voies.

```
dotnet run -c Release --project src/Rpg.Sim -- --matrix --simulate 30
dotnet run -c Release --project src/Rpg.Sim -- --matrix --simulate 30 --decks tools/bench-decks.json
```

### Ce qu'on peut en dire

Les noms entre guillemets inversés sont les identifiants de `data/specs.json`.

1. **La plupart des lignes ne sont pas concluantes.** Sur les 24 lignes de voies de la Sentinelle et de la Garde, 7 le sont avec le paquet automatique (`sightline` à 35 et 45, `grip` à 35 et 45, `marksman`, `barrage` et `jailer` à 50) et 5 avec les paquets de référence. L'IA ne joue presque jamais Interposition, Tenir bon, Assaut, Arrachement, Cercle de fer, Cabriole, Mâchoires ni Piqué (0,0 à 0,2 par combat le plus souvent).
2. **Les ±8 de la Garde (S192) valent moins que ce que S192 disait.** La Garde les tient avec le paquet automatique (au plus 7,8), mais, sauf pour `grip` et `jailer`, l'IA ne joue pas ses sorts de voie : ces lignes ne jugent pas ces sorts. Avec les paquets de référence, elle dépasse ±8 à 35 (`grip` −10,0) et à 50 (`jailer` +9,4, `pincer` −9,6, `harpooner` −8,3).
3. **La Sentinelle ne tient pas ±8** avec le paquet automatique : −9,4 à 35 (`ambush`), +11,6 à 45 (`sightline`), +20,8 à 50 (`barrage`). Avec les paquets de référence, `barrage` dépasse aussi le plafond de +15 sur le héros sans voie (+23,8), et cette ligne-là est concluante. `ambush` à 45 (+16,5) le dépasse aussi, mais n'est pas concluant.
4. **Le choix du paquet pèse autant que la voie, et la cause n'est pas établie.** Pour la Sentinelle et la Garde, le paquet de référence change le héros sans voie de moins d'un point à 35 et 45, mais il fait passer `trapper` de 19,4 à 44,0 et `leaper` de 17,9 à 41,5 à 50, alors que leurs sorts de voie restent peu joués. À comprendre en S194 avant de conclure sur ces voies. L'ordre des sorts dans le paquet est une piste, non vérifiée.
5. **Les paquets de référence ne sont pas des optimums.** Ils sont écrits à la main et non réglés : le Mage y perd 8,3 points à 45 (57,7 contre 49,4), la Sentinelle sans voie 7,5 à 50.

### Avec le paquet automatique

| Class | Level | Path | Points | Over no path | From the mean | Reads |
|---|---|---|---|---|---|---|
| sentinel | 35 | (none) | 44.6 | | | |
| sentinel | 35 | sightline | 41.9 | -2.7 | +7.6 | conclusive |
| sentinel | 35 | ambush | 24.8 | -19.8 | -9.4 | NOT CONCLUSIVE: path-jaws 0.2, path-dive 0.0 |
| sentinel | 35 | trail | 36.0 | -8.5 | +1.8 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.2 |
| guard | 35 | (none) | 19.8 | | | |
| guard | 35 | palisade | 26.0 | +6.3 | +7.2 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0 |
| guard | 35 | breach | 17.3 | -2.5 | -1.5 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 35 | grip | 13.1 | -6.7 | -5.7 | conclusive |
| mage | 35 | (none) | 41.5 | | | |
| mage | 35 | archmage | 41.5 | 0.0 | 0.0 | no spell yet |
| mage | 35 | sorcerer | 41.5 | 0.0 | 0.0 | no spell yet |
| mage | 35 | necromancer | 41.5 | 0.0 | 0.0 | no spell yet |
| sentinel | 45 | (none) | 35.6 | | | |
| sentinel | 45 | sightline | 43.8 | +8.1 | +11.6 | conclusive |
| sentinel | 45 | ambush | 23.1 | -12.5 | -9.0 | NOT CONCLUSIVE: path-jaws 0.1, path-dive 0.0 |
| sentinel | 45 | trail | 29.6 | -6.0 | -2.6 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.2 |
| guard | 45 | (none) | 22.9 | | | |
| guard | 45 | palisade | 29.2 | +6.3 | +7.8 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0 |
| guard | 45 | breach | 18.1 | -4.8 | -3.2 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 45 | grip | 16.7 | -6.3 | -4.7 | conclusive |
| mage | 45 | (none) | 57.7 | | | |
| mage | 45 | archmage | 57.7 | 0.0 | 0.0 | no spell yet |
| mage | 45 | sorcerer | 57.7 | 0.0 | 0.0 | no spell yet |
| mage | 45 | necromancer | 57.7 | 0.0 | 0.0 | no spell yet |
| sentinel | 50 | (none) | 43.8 | | | |
| sentinel | 50 | marksman | 49.8 | +6.0 | +18.8 | conclusive |
| sentinel | 50 | barrage | 51.9 | +8.1 | +20.8 | conclusive |
| sentinel | 50 | falconer | 23.3 | -20.4 | -7.7 | NOT CONCLUSIVE: path-jaws 0.2, path-dive 0.0, path-changeover 0.0 |
| sentinel | 50 | trapper | 19.4 | -24.4 | -11.7 | NOT CONCLUSIVE: path-jaws 0.2, path-dive 0.0 |
| sentinel | 50 | leaper | 17.9 | -25.8 | -13.1 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.1 |
| sentinel | 50 | runner | 24.0 | -19.8 | -7.1 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.2, path-running-shot 0.3 |
| guard | 50 | (none) | 30.8 | | | |
| guard | 50 | rampart | 34.8 | +4.0 | +1.6 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0, path-shield-wall 0.0 |
| guard | 50 | bastion | 40.8 | +10.0 | +7.6 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.2 |
| guard | 50 | ram | 29.0 | -1.9 | -4.2 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | harpooner | 31.9 | +1.0 | -1.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0, path-harpoon 0.4 |
| guard | 50 | jailer | 37.3 | +6.5 | +4.1 | conclusive |
| guard | 50 | pincer | 25.4 | -5.4 | -7.8 | NOT CONCLUSIVE: path-iron-ring 0.0 |
| mage | 50 | (none) | 55.4 | | | |
| mage | 50 | high-archmage | 55.4 | 0.0 | 0.0 | no spell yet |
| mage | 50 | patient | 55.4 | 0.0 | 0.0 | no spell yet |
| mage | 50 | plague | 55.4 | 0.0 | 0.0 | no spell yet |
| mage | 50 | hexer | 55.4 | 0.0 | 0.0 | no spell yet |
| mage | 50 | binder | 55.4 | 0.0 | 0.0 | no spell yet |
| mage | 50 | reaper | 55.4 | 0.0 | 0.0 | no spell yet |

### Avec les paquets de référence (`tools/bench-decks.json`)

| Class | Level | Path | Points | Over no path | From the mean | Reads |
|---|---|---|---|---|---|---|
| sentinel | 35 | (none) | 45.4 | | | |
| sentinel | 35 | sightline | 55.4 | +10.0 | +7.5 | NOT CONCLUSIVE: path-weak-spot 0.5 |
| sentinel | 35 | ambush | 43.3 | -2.1 | -4.6 | NOT CONCLUSIVE: path-jaws 0.1, path-dive 0.0 |
| sentinel | 35 | trail | 45.0 | -0.4 | -2.9 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.3 |
| guard | 35 | (none) | 19.8 | | | |
| guard | 35 | palisade | 30.4 | +10.6 | +7.7 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0 |
| guard | 35 | breach | 25.0 | +5.2 | +2.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 35 | grip | 12.7 | -7.1 | -10.0 | conclusive |
| mage | 35 | (none) | 40.4 | | | |
| mage | 35 | archmage | 40.4 | 0.0 | 0.0 | no spell yet |
| mage | 35 | sorcerer | 40.4 | 0.0 | 0.0 | no spell yet |
| mage | 35 | necromancer | 40.4 | 0.0 | 0.0 | no spell yet |
| sentinel | 45 | (none) | 34.8 | | | |
| sentinel | 45 | sightline | 45.8 | +11.0 | -3.1 | conclusive |
| sentinel | 45 | ambush | 51.3 | +16.5 | +2.3 | NOT CONCLUSIVE: path-jaws 0.2, path-dive 0.0 |
| sentinel | 45 | trail | 49.8 | +15.0 | +0.8 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.3 |
| guard | 45 | (none) | 23.8 | | | |
| guard | 45 | palisade | 25.8 | +2.1 | +6.3 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0 |
| guard | 45 | breach | 18.8 | -5.0 | -0.8 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 45 | grip | 14.2 | -9.6 | -5.4 | conclusive |
| mage | 45 | (none) | 49.4 | | | |
| mage | 45 | archmage | 49.4 | 0.0 | 0.0 | no spell yet |
| mage | 45 | sorcerer | 49.4 | 0.0 | 0.0 | no spell yet |
| mage | 45 | necromancer | 49.4 | 0.0 | 0.0 | no spell yet |
| sentinel | 50 | (none) | 36.3 | | | |
| sentinel | 50 | marksman | 43.1 | +6.9 | +3.8 | NOT CONCLUSIVE: path-aimed-shot 0.5 |
| sentinel | 50 | barrage | 60.0 | +23.8 | +20.7 | conclusive |
| sentinel | 50 | falconer | 17.7 | -18.5 | -21.6 | NOT CONCLUSIVE: path-jaws 0.4, path-dive 0.0, path-changeover 0.0 |
| sentinel | 50 | trapper | 44.0 | +7.7 | +4.7 | NOT CONCLUSIVE: path-jaws 0.2, path-dive 0.0 |
| sentinel | 50 | leaper | 41.5 | +5.2 | +2.2 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.4 |
| sentinel | 50 | runner | 29.6 | -6.7 | -9.7 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.5, path-running-shot 0.4 |
| guard | 50 | (none) | 27.1 | | | |
| guard | 50 | rampart | 31.9 | +4.8 | +2.1 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0, path-shield-wall 0.0 |
| guard | 50 | bastion | 36.5 | +9.4 | +6.7 | NOT CONCLUSIVE: path-interpose 0.0, path-stand-fast 0.0 |
| guard | 50 | ram | 29.6 | +2.5 | -0.2 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | harpooner | 21.5 | -5.6 | -8.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | jailer | 39.2 | +12.1 | +9.4 | conclusive |
| guard | 50 | pincer | 20.2 | -6.9 | -9.6 | NOT CONCLUSIVE: path-iron-ring 0.0 |
| mage | 50 | (none) | 52.3 | | | |
| mage | 50 | high-archmage | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | patient | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | plague | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | hexer | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | binder | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | reaper | 52.3 | 0.0 | 0.0 | no spell yet |
