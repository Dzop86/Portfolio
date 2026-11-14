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

## 5 novembre 2026 : après l'IA de S194 (T154)

**Conditions.** Les mêmes que ci-dessus : 480 combats par ligne, mêmes graines, intervalle d'environ ±4,5 points. Ce qui a changé : ce que l'IA d'un héros compte pour ses créatures, ses boucliers et ses résistances, le sort que le paquet automatique retire pour faire place aux sorts de voie, et le banc qui ignore les échanges avec un allié. Le Mage ne bouge pas d'un dixième sur aucune ligne, paquets compris : son héros sans voie ne doit donc rien à ces changements.

### Ce qu'on peut en dire

1. **Le ±22 de la Sentinelle venait surtout de l'IA et du paquet, pas des sorts, pour ses lignes basses.** Avec le paquet automatique : `ambush` à 35 passe de 24,8 à 60,0, `ambush` à 45 de 23,1 à 54,2, `falconer` de 23,3 à 60,6, `trapper` de 19,4 à 64,4. Le garde-fou de CI tient maintenant ±15, comme la Garde.
2. **La question de T153 (`trapper`, 19,4 contre 44,0) a sa réponse, pour l'essentiel.** Le paquet automatique retirait Tir lourd sur toutes les voies d'une Sentinelle de Terre. L'écart entre paquets se resserre pour `trapper` (64,4 et 58,8), `leaper` (37,9 et 43,1) et `ambush` (54,2 et 54,8). **Il se creuse pour `falconer`** (60,6 et 31,7), sans explication à ce jour.
3. **Les lignes ne sont pas plus concluantes.** 7 sur 24 avec le paquet automatique, comme avant, mais pas les mêmes : `palisade` à 35 et 45 et `bastion` à 50 entrent, `sightline` à 35, `marksman` et `jailer` à 50 sortent, juste sous 0,5. 8 sur 24 avec les paquets de référence, contre 5. Mâchoires, Cabriole, Coup de talon, Assaut et Arrachement restent à 0,5 fois par combat ou moins : **les lignes hautes de `trapper`, `falconer` et `ambush` disent ce que vaut leur Faucon et leurs passifs, pas leur piège.**
4. **±8, avec le paquet automatique.** La Sentinelle ne les tient qu'à 35 : 4,7 à 35, 11,1 à 45 (`trail`), 15,8 à 50 (`leaper`). La Garde les tient à 35 (5,7) et 45 (7,2), **plus à 50** (`bastion` 14,8), où elle les tenait avant.
5. **Plafond de +15 sur le héros sans voie : `barrage` à 50 le dépasse dans les deux cas** (+22,1 et +20,0), et ces lignes sont concluantes : c'est le cas le plus net à imputer aux sorts. `jailer` à 50 le dépasse aussi avec les paquets de référence (+16,7, concluant) mais pas avec le paquet automatique. `trapper` (+20,2) et `falconer` (+16,5) le dépassent sans être concluants. Rien n'est réglé ici : la Garde et la Sentinelle sont reprises en Guerrier (S197) et Archer (S198).
6. **Le plancher de −3 sur le héros sans voie reste non tenu** : `grip` −7,7 à 35 et −6,7 à 45, `pincer` −10,8 à 50, `trail` −10,0 à 45. Seuls `grip` sont concluants.

### Deux essais retirés : l'IA qui joue ces sorts y perd

| Essai | Lignes | Sans | Avec | Le sort est-il joué ? |
|---|---|---|---|---|
| Glyphe du héros : le retrait de PM vaut le coup qu'il évite | `trapper` à 50 | 64,4 | 45,2 | oui, Mâchoires passe au-dessus de 0,5 |
| | `falconer` à 50 | 60,6 | 45,6 | oui |
| | `ambush` à 45 | 54,2 | 43,5 | oui |
| | `ambush` à 35 | 60,0 | 56,5 | oui |
| Valeur du saut | `breach` | 22 | 13 | |
| | `ram` | 29 | 17 | |

Une IA qui saurait s'en servir ne ferait jamais moins bien qu'en ne les jouant pas. Ces baisses mesurent donc son jeu glouton, et non ces sorts. Rien de cela ne touche un joueur : en jeu, cette IA ne joue que les monstres et les créatures. Les chiffres de saut viennent de mes notes d'essai, je n'ai pas gardé leur sortie complète.

### Avec le paquet automatique

| Class | Level | Path | Points | Over no path | From the mean | Reads |
|---|---|---|---|---|---|---|
| sentinel | 35 | (none) | 53.5 | | | |
| sentinel | 35 | sightline | 54.8 | +1.3 | -0.6 | NOT CONCLUSIVE: path-weak-spot 0.5 |
| sentinel | 35 | ambush | 60.0 | +6.5 | +4.7 | NOT CONCLUSIVE: path-jaws 0.1 |
| sentinel | 35 | trail | 51.3 | -2.3 | -4.1 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.1 |
| guard | 35 | (none) | 24.0 | | | |
| guard | 35 | palisade | 27.3 | +3.3 | +5.3 | conclusive |
| guard | 35 | breach | 22.3 | -1.7 | +0.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 35 | grip | 16.3 | -7.7 | -5.7 | conclusive |
| mage | 35 | (none) | 41.5 | | | |
| mage | 35 | archmage | 41.5 | 0.0 | 0.0 | no spell yet |
| mage | 35 | sorcerer | 41.5 | 0.0 | 0.0 | no spell yet |
| mage | 35 | necromancer | 41.5 | 0.0 | 0.0 | no spell yet |
| sentinel | 45 | (none) | 47.7 | | | |
| sentinel | 45 | sightline | 54.6 | +6.9 | +5.8 | conclusive |
| sentinel | 45 | ambush | 54.2 | +6.5 | +5.3 | NOT CONCLUSIVE: path-jaws 0.0 |
| sentinel | 45 | trail | 37.7 | -10.0 | -11.1 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.2 |
| guard | 45 | (none) | 25.4 | | | |
| guard | 45 | palisade | 32.7 | +7.3 | +6.8 | conclusive |
| guard | 45 | breach | 26.3 | +0.8 | +0.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 45 | grip | 18.8 | -6.7 | -7.2 | conclusive |
| mage | 45 | (none) | 57.7 | | | |
| mage | 45 | archmage | 57.7 | 0.0 | 0.0 | no spell yet |
| mage | 45 | sorcerer | 57.7 | 0.0 | 0.0 | no spell yet |
| mage | 45 | necromancer | 57.7 | 0.0 | 0.0 | no spell yet |
| sentinel | 50 | (none) | 44.2 | | | |
| sentinel | 50 | marksman | 53.8 | +9.6 | 0.0 | NOT CONCLUSIVE: path-aimed-shot 0.5 |
| sentinel | 50 | barrage | 66.3 | +22.1 | +12.5 | conclusive |
| sentinel | 50 | falconer | 60.6 | +16.5 | +6.9 | NOT CONCLUSIVE: path-jaws 0.0, path-changeover 0.1 |
| sentinel | 50 | trapper | 64.4 | +20.2 | +10.6 | NOT CONCLUSIVE: path-jaws 0.0, path-dive 0.4 |
| sentinel | 50 | leaper | 37.9 | -6.3 | -15.8 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.3 |
| sentinel | 50 | runner | 39.6 | -4.6 | -14.2 | NOT CONCLUSIVE: path-vault 0.1, path-heel-kick 0.3, path-running-shot 0.3 |
| guard | 50 | (none) | 32.3 | | | |
| guard | 50 | rampart | 27.1 | -5.2 | -5.2 | NOT CONCLUSIVE: path-shield-wall 0.2 |
| guard | 50 | bastion | 47.1 | +14.8 | +14.8 | conclusive |
| guard | 50 | ram | 29.0 | -3.3 | -3.3 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | harpooner | 33.3 | +1.0 | +1.0 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0, path-harpoon 0.5 |
| guard | 50 | jailer | 35.8 | +3.5 | +3.5 | NOT CONCLUSIVE: path-crush 0.5 |
| guard | 50 | pincer | 21.5 | -10.8 | -10.8 | NOT CONCLUSIVE: path-iron-ring 0.0 |
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
| sentinel | 35 | (none) | 52.7 | | | |
| sentinel | 35 | sightline | 55.6 | +2.9 | -0.2 | NOT CONCLUSIVE: path-weak-spot 0.5 |
| sentinel | 35 | ambush | 60.0 | +7.3 | +4.2 | NOT CONCLUSIVE: path-jaws 0.1 |
| sentinel | 35 | trail | 51.9 | -0.8 | -4.0 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.2 |
| guard | 35 | (none) | 24.4 | | | |
| guard | 35 | palisade | 27.1 | +2.7 | +5.3 | conclusive |
| guard | 35 | breach | 22.5 | -1.9 | +0.7 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 35 | grip | 15.8 | -8.5 | -6.0 | conclusive |
| mage | 35 | (none) | 40.4 | | | |
| mage | 35 | archmage | 40.4 | 0.0 | 0.0 | no spell yet |
| mage | 35 | sorcerer | 40.4 | 0.0 | 0.0 | no spell yet |
| mage | 35 | necromancer | 40.4 | 0.0 | 0.0 | no spell yet |
| sentinel | 45 | (none) | 41.3 | | | |
| sentinel | 45 | sightline | 50.6 | +9.4 | +0.4 | conclusive |
| sentinel | 45 | ambush | 54.8 | +13.5 | +4.6 | NOT CONCLUSIVE: path-jaws 0.0, path-dive 0.4 |
| sentinel | 45 | trail | 45.2 | +4.0 | -5.0 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.3 |
| guard | 45 | (none) | 19.2 | | | |
| guard | 45 | palisade | 16.3 | -2.9 | +3.5 | conclusive |
| guard | 45 | breach | 12.3 | -6.9 | -0.5 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 45 | grip | 9.8 | -9.4 | -3.0 | conclusive |
| mage | 45 | (none) | 49.4 | | | |
| mage | 45 | archmage | 49.4 | 0.0 | 0.0 | no spell yet |
| mage | 45 | sorcerer | 49.4 | 0.0 | 0.0 | no spell yet |
| mage | 45 | necromancer | 49.4 | 0.0 | 0.0 | no spell yet |
| sentinel | 50 | (none) | 41.0 | | | |
| sentinel | 50 | marksman | 43.1 | +2.1 | -1.8 | NOT CONCLUSIVE: path-aimed-shot 0.4 |
| sentinel | 50 | barrage | 61.0 | +20.0 | +16.1 | conclusive |
| sentinel | 50 | falconer | 31.7 | -9.4 | -13.3 | NOT CONCLUSIVE: path-jaws 0.2, path-changeover 0.1 |
| sentinel | 50 | trapper | 58.8 | +17.7 | +13.8 | NOT CONCLUSIVE: path-jaws 0.0 |
| sentinel | 50 | leaper | 43.1 | +2.1 | -1.8 | NOT CONCLUSIVE: path-vault 0.2, path-heel-kick 0.5 |
| sentinel | 50 | runner | 32.1 | -9.0 | -12.9 | NOT CONCLUSIVE: path-vault 0.2, path-running-shot 0.3 |
| guard | 50 | (none) | 22.5 | | | |
| guard | 50 | rampart | 21.9 | -0.6 | -5.3 | NOT CONCLUSIVE: path-shield-wall 0.3 |
| guard | 50 | bastion | 30.6 | +8.1 | +3.5 | conclusive |
| guard | 50 | ram | 29.6 | +7.1 | +2.4 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | harpooner | 21.5 | -1.0 | -5.7 | NOT CONCLUSIVE: path-assault 0.0, path-wrench 0.0 |
| guard | 50 | jailer | 39.2 | +16.7 | +12.0 | conclusive |
| guard | 50 | pincer | 20.2 | -2.3 | -6.9 | NOT CONCLUSIVE: path-iron-ring 0.0 |
| mage | 50 | (none) | 52.3 | | | |
| mage | 50 | high-archmage | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | patient | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | plague | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | hexer | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | binder | 52.3 | 0.0 | 0.0 | no spell yet |
| mage | 50 | reaper | 52.3 | 0.0 | 0.0 | no spell yet |

## 6 novembre 2026 : le Guerrier à 100 et ses finales (S197, T160)

`WarriorBenchTests` (test `Explicit`, `-- --explicit only`, 2 min 40) : le Guerrier de 100 seul, contre les groupes de 4 et 5 de la famille de synthèse au niveau 100, 30 combats par groupe, mêmes graines, paquet automatique. Une ligne par lignée sans finale (la référence), puis par finale ; l'écart entre parenthèses est pris sur la lignée.

| Lignée | Sans finale | Reprise | Neuve |
|---|---|---|---|
| (sans voie) | 20,8 | | |
| Gardien `rampart` | 18,1 | Croisé 19,4 (+1,3) | Sentinelle 20,8 (+2,7) |
| Champion `bastion` | 24,8 | Duelliste 31,0 (+6,3) | Seigneur de guerre 39,4 (+14,6) |
| Bretteur `jailer` | 32,7 | Maître des lames 32,9 (+0,2) | Lame fantôme 27,1 (−5,6) |
| Tacticien `pincer` | 18,8 | Stratège 19,4 (+0,6) | Général 18,8 (0,0) |
| Briseur `ram` | 13,5 | Exécuteur 17,9 (+4,4) | Destructeur 15,6 (+2,1) |
| Combattant `harpooner` | 26,5 | Champion d'arène 31,3 (+4,8) | Pugiliste 29,4 (+2,9) |

**Aucune ligne n'est concluante** : à 100, l'IA d'un héros seul ne joue presque jamais Écrasement, Harpon ni Rétorsion, et toujours pas Assaut, Cercle de fer ni Arrachement (T158). Les bornes F1 à F4 de `docs/classes-guerrier.md` §7.3 ne jugent donc rien, et le test passe sans rien vérifier. Ce qu'on peut lire quand même : le Seigneur de guerre (+14,6) dépasse la borne de +10, et son effet passe par un passif et des PA, pas par un sort que l'IA néglige ; la Lame fantôme (−5,6) passe sous le plancher de −3. Le Général ne change aucun combat de la fumée (Cercle de fer jamais joué) : il est dans `CiLevelsTests.Unchanged`. La mesure en duo (M100-duo), qui jugerait le Croisé et le Seigneur, n'est pas faite.

## 6 novembre 2026 : l'Archer à quatre rangs (S198, T161)

**À 50**, `Rpg.Sim --matrix --simulate 30 --level 50 --class archer`, paquet automatique, 16 groupes de 4 et 5 monstres, mêmes graines :

| Lignée | Points | Sur l'Archer sans voie | Lecture |
|---|---|---|---|
| (sans voie) | 44,2 | | |
| Sniper `marksman` | 53,8 | +9,6 | non concluant : Tir visé 0,5 |
| Chasseur de primes `bounty-hunter` | 50,8 | +6,7 | non concluant : Point faible 0,2 |
| Éclaireur `trapper` | 64,4 | +20,2 | non concluant : Mâchoires 0,0 |
| Chasseur sauvage `falconer` | 60,6 | +16,5 | non concluant : Chassé-croisé 0,1 |
| Arbalétrier `barrage` (+5 % sans marcher) | 62,5 | +18,3 | non concluant : Coup de talon 0,1 |
| Tireur mobile `runner` | 53,5 | +9,4 | non concluant : Tir en course 0,3 |

L'Arbalétrier, selon son bonus « sans marcher » : 0 % → 56,5 ; 3 % → 62,7 ; 5 % → 62,5 ; 8 % → 69,4 ; 15 % → 74,0. À 15 %, la barrière de la CI (`TemplateTests`, 8 groupes, 15 combats) le lisait à 84,2 et refusait l'écart. Retenu : 5 %.

**À 100**, `FinalsBenchTests.AtHundred_TheArchersFinals…` (explicite, 3 min) :

| Lignée | Sans finale | Reprise | Neuve |
|---|---|---|---|
| (sans voie) | 37,7 | | |
| Sniper `marksman` | 26,7 | Œil du dragon 44,4 (+17,7) | Tir parfait 51,0 (+24,4) |
| Chasseur de primes `bounty-hunter` | 42,3 | (pas de reprise) Traqueur légendaire 42,3 (0,0) | Exécuteur 60,2 (+17,9) |
| Éclaireur `trapper` | 35,2 | Maître forestier 35,2 (0,0) | Pisteur 39,4 (+4,2) |
| Chasseur sauvage `falconer` | 34,6 | Seigneur des bêtes 34,6 (0,0) | Prédateur ancestral 34,6 (0,0) |
| Arbalétrier `barrage` | 49,2 | Baliste vivante 55,6 (+6,5) | Maître arbalétrier 49,4 (+0,2) |
| Tireur mobile `runner` | 47,5 | Voltigeur 51,0 (+3,5) | Faucon de guerre 47,5 (0,0) |

**Aucune ligne n'est concluante**, comme pour le Guerrier : à 100, l'IA d'un héros seul ne joue presque pas les sorts de lignée. Sept finales ne changent rien au banc (0,0) : leur effet passe par un sort que l'IA ne joue pas ici (la proie, Mâchoires, Chassé-croisé, le second Faucon, Carreau lourd, Tir en course). Ce qu'on peut lire : Tir parfait, l'Exécuteur et Œil du dragon dépassent la borne de +10, et leur effet passe par une règle ou un passif, pas par un sort négligé ; le Sniper seul fait moins bien que l'Archer sans voie, à vérifier quand l'IA jouera Coup au but.

## 6 novembre 2026 : le Mage, Arcaniste et Chronomancien (S199, T162)

Voir `docs/classes-mage.md` §4 : premier jet, recherche de la cause, réglage. Retenu, à 35 : sans voie 41,5, Arcaniste 58,5 (conclusif), Chronomancien 58,8 (conclusif), Illusionniste 41,5 (sans sort). À 50 : sans voie 55,4, Manipulateur arcanique 63,3 (non concluant : Rune 0,0), Érudit mystique 62,3 (conclusif), Manipulateur temporel 63,3 (non concluant : Accélération 0,1), Distorsionniste 61,9 (non concluant : Point d'orgue 0,2).

## 7 novembre 2026 : le Mage, Illusionniste et finales (S200, T163)

Voir `docs/classes-mage-2.md` §5 : premier jet, valeur du Leurre pour l'IA, réglage. Retenu, à 35 : sans voie 41,5, Arcaniste 58,5, Illusionniste 52,7, Chronomancien 58,8 (tous conclusifs). À 50 : sans voie 55,4, Manipulateur arcanique 63,3 (non concluant : Rune 0,0), Érudit mystique 62,3, Maître des illusions 70,0 (non concluant : Éblouissement 0,5), Mentaliste 67,5 (non concluant : Éblouissement 0,5), Manipulateur temporel 63,3 (non concluant), Distorsionniste 61,9 (non concluant). À 100 (banc des finales, indicatif) : Seigneur des illusions +15,4 sur sa lignée, hors de F1 ; Mentaliste +12 de la moyenne, hors de F4.

## 7 novembre 2026 : le Prêtre, et le Mage sans soins (S201, T164)

Voir `docs/classes-pretre.md` §6. Seul, sans voie : Prêtre 24,4 à 35 et 35,6 à 50 (Guerrier 24,0 et 32,3, Mage 41,5 et 55,4 inchangés, Archer 53,5 et 44,2). Disjoint seul à 30 : 4 sur 100 au premier jet, 35 avec des soins qui grandissent. Groupes de référence à 45 : les 12 d'avant inchangés ; Guerrier + Prêtre 57, Archer + Prêtre 72, Mage + Prêtre 62, Guerrier + Archer + Prêtre 95, Guerrier + Mage + Prêtre 87, les quatre 100.

## 7 novembre 2026 : le Prêtre, Clerc, Inquisiteur et Oracle (S202, T165)

Voir `docs/classes-pretre-2.md` §4. Retenu, à 35 : sans voie 24,4, Clerc 39,2, Inquisiteur 40,8, Oracle 40,4. À 50 : sans voie 35,6, Guérisseur 37,5, Médecin sacré 41,9, Exorciste 40,0, Fanatique 42,7, Prophète 39,0, Mystique 40,0. À 100 (indicatif) : sans voie 6,7, finales de 8,5 à 26,3 ; le Prêtre seul de 100 est à reprendre au jalon 1.

## 7 novembre 2026 : jalon 1, les PV balayés (S203, T166)

Voir `docs/jalon-1.md`. Guerrier à 74 + 5 : 37,9 / 50,2 / 47,3 à 35 / 50 / 100 (24,0 / 32,3 / 20,8 avant). Mage à 72 + 3 essayé et rejeté (entraînement 320 sur 1000). Archer à 62 + 4 essayé et rejeté (31,9 à 50). Prêtre de 100 : environ 7 sans voie, quelque réglage qu'on essaie.

## 8 novembre 2026 : le Voleur, la discrétion (S204, T167)

Voir `docs/classes-voleur.md` §6. Disjoint seul au niveau 30 : 0 sur 100 au budget 1,05 (21 PV laissés au boss sur 449), 17 à 1,1, 25 à 1,12 et 1,13, 20 sur 20 à 1,15 (retenu) ; 58 + 5 PV essayé et rejeté (aussi solide que le Guerrier). Groupes de référence : Guerrier + Voleur 60 %, Archer + Voleur 60 %, Mage + Voleur 57 %, trios 100 et 97 %, à quatre 100 %. Les quatre autres classes, mêmes combats en solo qu'avant (empreinte E6B69159D0E58685).

## 8 novembre 2026 : le Voleur, les poisons et l'Ombremage (S205, T168)

Voir `docs/classes-voleur-2.md` §3. Exigences en solo, groupes de référence et empreintes inchangés ; l'IA ne joue pas le Pas de l'ombre (un bond).

## 8 novembre 2026 : le Berserker (S206, T169)

Voir `docs/classes-berserker.md` §5. Exigences en solo tenues ; groupes : Guerrier + Berserker 62 %, Archer + Berserker 72 %, Prêtre + Berserker 60 %, trios et groupe de quatre 100 %. Disjoint seul et grande salle passés à un échantillon de 300 combats partagés (D97).

## 8 novembre 2026 : le Barbare et le Gladiateur (S207, T170)

Voir `docs/classes-berserker-2.md` §3. Exigences, groupes et empreintes inchangés. La fumée ne laisse plus que 3 combats par héros et par groupe : une quinzaine de sorts de voie non joués, au jalon 2.

## 8 novembre 2026 : le Paladin (S208, T171)

Voir `docs/classes-paladin.md` §4. Exigences en solo tenues ; groupes : Mage + Paladin 55 %, Archer + Paladin 60 %, Voleur + Paladin 60 %, trios 100 et 95 %, quatre 100 %. Groupes et hordes de la Forêt sur un budget fixe partagé : 19 629 combats.

## 9 novembre 2026 : le Templier et le Vengeur (S209, T172)

Voir `docs/classes-paladin-2.md` §2. Exigences, groupes et hordes inchangés ; 19 507 combats. Le Vœu de vengeance et quatre finales de plus non concluants à la fumée, au jalon 2.

## 9 novembre 2026 : jalon 2, les sorts de voie rangés (S210, T173)

Voir `docs/jalon-2.md`. `Rpg.Sim --matrix --simulate 30`, 84 voies à 35, 45 et 50. Défaut : 25 concluantes, +5,9 en moyenne sur le héros sans voie, 16 au-delà de +15, aucune au-delà de +24. `--ranked-paths` : 32 concluantes, +14,9, 45 au-delà de +15, 17 au-delà de +24 (Geôlier à 50 : +44,2). Héros sans voie à 35 : Prêtre 18,8 (24,4 au jalon 1), Berserker 66,9. Voleur et Berserker : aucune voie concluante dans les deux modèles.

## 10 novembre 2026 : les suites du jalon 2 (S210, T174)

Voir `docs/jalon-2.md` §5. `Rpg.Sim --matrix --simulate 30`, sorts de voie rangés (le défaut désormais), l'IA des bonds avec risque d'atterrissage : 33 voies concluantes sur 84, +14,3 sur le héros sans voie, 42 au-delà de +15, 17 au-delà de +24, 12 sorts à 0,0. Guerrier sans voie à 50 : 61,3 (50,2), son Fauchage l'épargnant. Écart le plus large entre voies d'une classe : Vengeur à 45, 15,0. Voleur + Paladin face aux hordes à 45 : 55 sur 100 (60 mesuré au sprint 208).

## 10 novembre 2026 : l'Invocateur et l'IA des effets différés (S211, T175, T176)

`Rpg.Sim --matrix --simulate 30`, sorts de voie rangés, l'IA de D99 : **36 voies concluantes sur 84** (33 au sprint 210), +13,6 en moyenne sur le héros sans voie (+14,3), 38 au-delà de +15 (42), 16 au-delà de +24 (17), 10 sous −3 (8), **9 sorts à 0,0** (12). Sortis de 0,0 : les Mâchoires (jouées), la Rune (0,1), le Cri du clan (0,1 à 0,3) ; ces deux-là restent sous les 0,5 lancer par combat d'un sort jugé. Encore à 0,0 : Arrachement (0,0 à 0,4 selon la voie), Cercle de fer, Résurrection, Bouclier de la foi, Cabriole, Feinte, Lame enduite, Fiole de peste, Coup d'arène. Écart entre voies d'une classe : le Vengeur à 45 et le Bastion à 50 à 15,0 de leur moyenne, à la limite.

L'Invocateur sans voie (la matrice saute une classe sans voie ; `--class summoner`) : 7,5 au niveau 10, 34 au 30, 57 au 35, 62 au 45 et au 50, 54 au 70, 29 au 100. Les autres classes à 50 : de 30 (Prêtre) à 73 (Berserker).

## 10 novembre 2026 : le Familieriste (S212, T177)

Voir `docs/classes-invocateur-2.md` §4. `--class summoner --simulate 30` : Familieriste +19,0 au niveau 35 ; Dompteur +24,6 et Fusionniste +4,6 au 50 (±10,0 de leur moyenne) ; au 100, de +11,9 (Chimériste) à +27,1 (Maître des familiers), toutes à ±10 de leur moyenne.

## 11 novembre 2026 : le Démoniste et le Conjurateur (S213, T178)

Voir `docs/classes-invocateur-3.md` §4. `--class summoner --simulate 30` : Démoniste +24,6 et Conjurateur +17,9 au niveau 35 (Familieriste +19,0) ; à 50, Invocateur démoniaque +21,9, Pactisant +17,5, Invocateur élémentaire +16,3, Invocateur astral +10,0, de −5,8 à +6,1 de la moyenne ; au 100, de +23,5 (Maître cosmique) à +39,0 (Pactisant). B5 tenue ; B6 tenue pour les deux branches neuves, pas pour le Fusionniste (−11,2).

## 12 novembre 2026 : le Nécromancien et le Maître des morts (S214, T179)

Voir `docs/classes-necromancien.md` §6. `--class necromancer --simulate 30` : sans voie 24,6 au niveau 30, 37,9 au 35, 44,4 au 50, 21,7 au 60, 12,9 au 100 ; Maître des morts +25,6 au 35 ; Liche +24,2 et Seigneur des os +17,5 au 50 (6,6 points d'écart, B6 tenue) ; au 100, de +23,5 à +25,8. Groupes : Guerrier 55 %, Archer 61 %, Paladin 61 % avec le Nécromancien, trios et groupe de quatre 100 %. Empreintes des autres classes inchangées.

## 12 novembre 2026 : l'Invocateur macabre et l'Occultiste (S215, T180)

Voir `docs/classes-necromancien-2.md` §5. Banc à 40 combats par groupe (35 à 60), 30 au niveau 100 : B5 tenue (−1,7 à +2,1) ; à 50, Liche +5,9, Commandant des morts −6,3, Sanguinomancien +6,6, Thanaturge −4,2 (10,9 points entre les lignées de l'Occultiste) ; à 60, Sanguinomancien +8,4, Thanaturge +8,0, Commandant des morts −8,1 ; au 100, les finales à 2,3 points de leur lignée au plus, sauf celles de l'Occultiste (+3,5 à +5,8). Ripaille 0,15, Sacrifice 0,14, Malédiction 0,05 à 0,2 par combat.

## 13 novembre 2026 : le Druide sans voie (S216, T181)

Voir `docs/classes-druide.md` §4. `--class druid --simulate 30`, sans voie : au barème de la conception (0,85), 20,6 au niveau 35, 6,9 au 60, 0,2 au 100 ; **au barème 1 (gardé)**, 36,5 au 35, 17,1 au 60, 15,6 au 100 (Nécromancien 37,9, 21,7 et 12,9 ; Prêtre 23,8, 9,2 et 8,3). Groupes de référence à 45 : Guerrier + Druide 55 %, Voleur + Druide 55 %, Berserker + Druide 66 %, Paladin + Nécromancien + Druide 88 %, les deux autres 100 %.

## 13 novembre 2026 : le Gardien de la nature et le Métamorphe (S217, T182)

Voir `docs/classes-druide-2.md` §4. `--matrix --class druid --simulate 30`, sorts de voie rangés : B5 tenue (±1,7 à 35, ±1,3 à 45) ; à 50, Sylvain +6,8, Protecteur +1,8, Changeforme −7,5, Druide sauvage −1,1 ; à 60, de −2,7 à +3,3 ; à 100, de −7,9 à +6,2, les finales à 7,5 points au plus de leur lignée. Forme sauvage et Griffes jouées au-dessus de 0,5 après la correction de l'IA ; Haie 0,0, Bond du fauve et Piétinement 0,3 à 0,4.

## 14 novembre 2026 : le Chaman (S218, T183)

Voir `docs/classes-chaman.md` §4. `--matrix --class druid --simulate 30 --casts`, sorts de voie rangés : B5 tenue (Chaman +1,3 à 35, +2,6 à 45) ; à 50, Invocateur spirituel +5,7, Élémentaliste naturel +0,7 ; à 60, −0,6 et +4,8 ; au 100, de −7,8 à +0,1, les finales à 3,9 points de leur lignée au plus. Le Changeforme passe à −9,1 au 50 (la moyenne de la classe relevée par le Chaman). Esprit 1,0 à 1,3 par combat jusqu'au 60, Communion 1,2 dès le 60, Tempête vivante 0,3 à 0,8, Sol grondant 0,0 à 0,1.

## 14 novembre 2026 : jalon 3, dix classes (S219)

Voir `docs/jalon-3.md`. Le banc complet, puis les durées.

```
dotnet run -c Release --project src/Rpg.Sim -- --matrix --simulate 30 --casts
dotnet run -c Release --project src/Rpg.Sim -- --durations --simulate 60
```

| Class | Level | Path | Points | Over no path | From the mean | Reads |
|---|---|---|---|---|---|---|
| archer | 35 | (none) | 51.9 | | | |
| archer | 35 | sightline | 55.8 | +4.0 | +0.7 | path-aimed-shot 0.9, path-weak-spot 0.4 |
| archer | 35 | ambush | 57.3 | +5.4 | +2.2 | path-jaws 0.8, path-dive 1.5 |
| archer | 35 | trail | 52.3 | +0.4 | -2.8 | path-barrage 1.0, path-heel-kick 0.9 |
| warrior | 35 | (none) | 45.6 | | | |
| warrior | 35 | palisade | 71.0 | +25.4 | +11.3 | path-stand-fast 0.6 |
| warrior | 35 | grip | 51.3 | +5.6 | -8.5 | path-hold 2.6, path-lock 1.0 |
| warrior | 35 | breach | 57.1 | +11.5 | -2.7 | path-assault 0.5, path-wrench 0.1 |
| mage | 35 | (none) | 39.8 | | | |
| mage | 35 | arcanist | 66.3 | +26.5 | +1.5 | path-comet 1.3, path-composure 1.0 |
| mage | 35 | illusionist | 61.7 | +21.9 | -3.1 | path-image 0.9, path-dazzle 2.4 |
| mage | 35 | chronomancer | 66.3 | +26.5 | +1.5 | path-borrowed-time 0.8, path-offbeat 2.4 |
| priest | 35 | (none) | 20.8 | | | |
| priest | 35 | cleric | 49.4 | +28.5 | +2.3 | path-resurrect 0.0, path-laying-hands 2.3 |
| priest | 35 | inquisitor | 40.8 | +20.0 | -6.3 | path-censure 2.1, path-brand 1.9 |
| priest | 35 | oracle | 51.0 | +30.2 | +4.0 | path-omen 2.3, path-foresight 0.9 |
| rogue | 35 | (none) | 39.4 | | | |
| rogue | 35 | assassin | 47.5 | +8.1 | +4.9 | path-backstab 0.9, path-envenom 0.1 |
| rogue | 35 | scoundrel | 44.0 | +4.6 | +1.4 | path-vault 0.0, path-hamstring 0.7 |
| rogue | 35 | shadowmage | 36.3 | -3.1 | -6.3 | path-shadow-step 0.3, path-shadow-blade 1.3 |
| berserker | 35 | (none) | 66.9 | | | |
| berserker | 35 | ravager | 90.0 | +23.1 | +5.5 | path-ravage 0.4, path-gore-charge 0.6 |
| berserker | 35 | barbarian | 80.8 | +14.0 | -3.7 | path-clan-cry 0.1, path-savage-blow 0.6 |
| berserker | 35 | berserker-gladiator | 82.7 | +15.8 | -1.8 | path-challenge 0.4, path-arena-strike 0.6 |
| paladin | 35 | (none) | 49.0 | | | |
| paladin | 35 | protector | 78.3 | +29.4 | +0.8 | path-hallowed-ground 0.3, path-guardian-strike 2.6 |
| paladin | 35 | templar | 73.5 | +24.6 | -4.0 | path-seal-of-light 1.9, path-sacred-return 2.1 |
| paladin | 35 | avenger | 80.8 | +31.9 | +3.3 | path-retribution 1.5, path-vow 0.4 |
| summoner | 35 | (none) | 56.7 | | | |
| summoner | 35 | demonist | 81.3 | +24.6 | +4.1 | path-demon 1.1, path-blood-pact 1.4 |
| summoner | 35 | conjurer | 74.6 | +17.9 | -2.6 | path-pillar 1.5, path-anchor 1.6 |
| summoner | 35 | familiarist | 75.6 | +19.0 | -1.5 | path-familiar 1.2, path-titbit 0.6 |
| necromancer | 35 | (none) | 37.9 | | | |
| necromancer | 35 | dead-master | 63.5 | +25.6 | -1.0 | path-stitch 0.6, path-bone-rampart 0.2 |
| necromancer | 35 | macabre-caller | 65.2 | +27.3 | +0.7 | path-ghoul 0.7, path-quarry 0.7 |
| necromancer | 35 | occultist | 64.8 | +26.9 | +0.3 | path-hex 0.2, path-blight 1.8 |
| druid | 35 | (none) | 36.5 | | | |
| druid | 35 | nature-warden | 63.8 | +27.3 | +1.0 | path-hedge 0.0, path-barkskin 0.7 |
| druid | 35 | shapeshifter | 60.4 | +24.0 | -2.3 | path-wild-shape 1.1, path-claws 1.4 |
| druid | 35 | shaman | 64.0 | +27.5 | +1.3 | path-spirit 1.3, path-rumbling-ground 0.0 |
| archer | 45 | (none) | 52.9 | | | |
| archer | 45 | sightline | 66.9 | +14.0 | +7.3 | path-aimed-shot 1.1, path-weak-spot 0.6 |
| archer | 45 | ambush | 62.9 | +10.0 | +3.3 | path-jaws 0.7, path-dive 1.5 |
| archer | 45 | trail | 49.0 | -4.0 | -10.6 | path-barrage 1.1, path-heel-kick 0.9 |
| warrior | 45 | (none) | 39.8 | | | |
| warrior | 45 | palisade | 77.5 | +37.7 | +7.3 | path-stand-fast 0.7 |
| warrior | 45 | grip | 66.3 | +26.5 | -4.0 | path-hold 3.4, path-lock 0.9 |
| warrior | 45 | breach | 66.9 | +27.1 | -3.3 | path-assault 0.5, path-wrench 0.1 |
| mage | 45 | (none) | 54.8 | | | |
| mage | 45 | arcanist | 66.5 | +11.7 | -1.9 | path-comet 1.2, path-composure 1.0 |
| mage | 45 | illusionist | 65.6 | +10.8 | -2.7 | path-image 1.0, path-dazzle 2.1 |
| mage | 45 | chronomancer | 72.9 | +18.1 | +4.6 | path-borrowed-time 0.9, path-offbeat 2.3 |
| priest | 45 | (none) | 29.8 | | | |
| priest | 45 | cleric | 41.5 | +11.7 | -4.4 | path-resurrect 0.0, path-laying-hands 2.5 |
| priest | 45 | inquisitor | 53.5 | +23.8 | +7.7 | path-censure 1.9, path-brand 2.0 |
| priest | 45 | oracle | 42.5 | +12.7 | -3.3 | path-omen 2.5, path-foresight 0.6 |
| rogue | 45 | (none) | 46.0 | | | |
| rogue | 45 | assassin | 48.1 | +2.1 | +4.7 | path-backstab 0.9, path-envenom 0.0 |
| rogue | 45 | scoundrel | 41.0 | -5.0 | -2.4 | path-vault 0.0, path-hamstring 0.8 |
| rogue | 45 | shadowmage | 41.3 | -4.8 | -2.2 | path-shadow-step 0.3, path-shadow-blade 1.1 |
| berserker | 45 | (none) | 72.7 | | | |
| berserker | 45 | ravager | 84.2 | +11.5 | +10.8 | path-ravage 0.6, path-gore-charge 0.3 |
| berserker | 45 | barbarian | 66.7 | -6.0 | -6.7 | path-clan-cry 0.1, path-savage-blow 0.5 |
| berserker | 45 | berserker-gladiator | 69.2 | -3.5 | -4.2 | path-challenge 0.4, path-arena-strike 0.5 |
| paladin | 45 | (none) | 54.0 | | | |
| paladin | 45 | protector | 76.5 | +22.5 | +7.3 | path-hallowed-ground 0.3, path-guardian-strike 2.4 |
| paladin | 45 | templar | 76.9 | +22.9 | +7.7 | path-seal-of-light 2.1, path-sacred-return 1.9 |
| paladin | 45 | avenger | 54.2 | +0.2 | -15.0 | path-retribution 1.2, path-vow 0.5 |
| summoner | 45 | (none) | 62.1 | | | |
| summoner | 45 | demonist | 89.2 | +27.1 | +5.3 | path-demon 1.2, path-blood-pact 1.5 |
| summoner | 45 | conjurer | 79.0 | +16.9 | -4.9 | path-pillar 1.3, path-anchor 1.5 |
| summoner | 45 | familiarist | 83.3 | +21.3 | -0.5 | path-familiar 1.3, path-titbit 0.6 |
| necromancer | 45 | (none) | 44.8 | | | |
| necromancer | 45 | dead-master | 65.8 | +21.0 | -0.3 | path-stitch 0.6, path-bone-rampart 0.2 |
| necromancer | 45 | macabre-caller | 64.0 | +19.2 | -2.2 | path-ghoul 0.7, path-quarry 0.8 |
| necromancer | 45 | occultist | 68.8 | +24.0 | +2.6 | path-hex 0.2, path-blight 1.9 |
| druid | 45 | (none) | 32.7 | | | |
| druid | 45 | nature-warden | 65.0 | +32.3 | -0.1 | path-hedge 0.0, path-barkskin 0.9 |
| druid | 45 | shapeshifter | 62.5 | +29.8 | -2.6 | path-wild-shape 1.1, path-claws 1.3 |
| druid | 45 | shaman | 67.7 | +35.0 | +2.6 | path-spirit 1.2, path-rumbling-ground 0.0 |
| archer | 50 | (none) | 46.9 | | | |
| archer | 50 | marksman | 60.2 | +13.3 | -7.6 | path-aimed-shot 0.8, path-weak-spot 0.6, path-bullseye 0.6 |
| archer | 50 | bounty-hunter | 68.8 | +21.9 | +0.9 | path-aimed-shot 1.1, path-weak-spot 0.2, path-prey 1.3 |
| archer | 50 | trapper | 66.5 | +19.6 | -1.4 | path-jaws 0.7, path-dive 1.2, path-bait 0.8 |
| archer | 50 | falconer | 75.6 | +28.8 | +7.8 | path-jaws 0.6, path-dive 1.4, path-changeover 0.2 |
| archer | 50 | barrage | 77.3 | +30.4 | +9.5 | path-barrage 0.8, path-heel-kick 1.3, path-heavy-bolt 1.5 |
| archer | 50 | runner | 58.5 | +11.7 | -9.3 | path-barrage 1.0, path-heel-kick 1.6, path-running-shot 1.5 |
| warrior | 50 | (none) | 55.4 | | | |
| warrior | 50 | rampart | 63.5 | +8.1 | -1.7 | path-stand-fast 0.8, path-shield-wall 0.3 |
| warrior | 50 | bastion | 80.2 | +24.8 | +15.0 | path-stand-fast 1.5, path-retaliation 2.1 |
| warrior | 50 | jailer | 69.0 | +13.5 | +3.8 | path-hold 3.0, path-lock 0.8, path-crush 1.5 |
| warrior | 50 | pincer | 50.8 | -4.6 | -14.4 | path-hold 3.5, path-lock 1.1, path-iron-ring 0.0 |
| warrior | 50 | ram | 52.3 | -3.1 | -12.9 | path-assault 0.4, path-wrench 0.4, path-breakthrough 1.9 |
| warrior | 50 | harpooner | 75.4 | +20.0 | +10.2 | path-assault 0.3, path-wrench 0.0, path-harpoon 1.9 |
| mage | 50 | (none) | 54.0 | | | |
| mage | 50 | rune-weaver | 68.5 | +14.6 | -4.5 | path-comet 1.2, path-composure 0.9, path-rune 0.1 |
| mage | 50 | scholar | 76.0 | +22.1 | +3.0 | path-comet 1.1, path-composure 0.9, path-ray 1.1 |
| mage | 50 | mirage-master | 76.5 | +22.5 | +3.4 | path-image 0.9, path-dazzle 2.4, path-double 1.1 |
| mage | 50 | mentalist | 77.1 | +23.1 | +4.0 | path-image 1.1, path-dazzle 1.9, path-grip 1.9 |
| mage | 50 | time-weaver | 68.5 | +14.6 | -4.5 | path-borrowed-time 0.9, path-offbeat 2.1, path-quicken 0.1 |
| mage | 50 | distorter | 71.7 | +17.7 | -1.4 | path-borrowed-time 1.0, path-offbeat 2.1, path-crescendo 0.2 |
| priest | 50 | (none) | 29.6 | | | |
| priest | 50 | healer | 43.5 | +14.0 | -6.6 | path-resurrect 0.0, path-laying-hands 2.8, path-healing-wave 1.5 |
| priest | 50 | sacred-medic | 43.8 | +14.2 | -6.4 | path-resurrect 0.0, path-laying-hands 2.6, path-remedy 0.9 |
| priest | 50 | exorcist | 60.8 | +31.3 | +10.7 | path-censure 1.6, path-brand 2.0, path-exorcism 1.4 |
| priest | 50 | fanatic | 53.8 | +24.2 | +3.6 | path-censure 1.8, path-brand 1.9, path-zeal 0.4 |
| priest | 50 | prophet | 52.9 | +23.3 | +2.8 | path-omen 1.7, path-foresight 0.8, path-prophecy 1.6 |
| priest | 50 | mystic | 46.0 | +16.5 | -4.1 | path-omen 2.0, path-foresight 0.7, path-vision 0.9 |
| rogue | 50 | (none) | 43.8 | | | |
| rogue | 50 | black-blade | 52.5 | +8.8 | +0.5 | path-backstab 0.9, path-envenom 0.1, path-execute 1.3 |
| rogue | 50 | poisoner | 59.0 | +15.2 | +7.0 | path-backstab 0.9, path-envenom 0.2, path-plague 0.0 |
| rogue | 50 | swashbuckler | 54.0 | +10.2 | +2.0 | path-vault 0.0, path-hamstring 0.7, path-feint 0.0 |
| rogue | 50 | saboteur | 41.5 | -2.3 | -10.5 | path-vault 0.0, path-hamstring 0.4, path-caltrops 1.0 |
| rogue | 50 | night-stalker | 45.4 | +1.7 | -6.6 | path-shadow-step 0.6, path-shadow-blade 1.3, path-waylay 1.1 |
| rogue | 50 | shadow-illusionist | 59.6 | +15.8 | +7.6 | path-shadow-step 0.4, path-shadow-blade 1.5, path-false-front 1.2 |
| berserker | 50 | (none) | 73.3 | | | |
| berserker | 50 | skullcracker | 77.9 | +4.6 | +6.6 | path-ravage 0.5, path-gore-charge 0.3, path-crushing-blow 0.6 |
| berserker | 50 | carnivore | 82.9 | +9.6 | +11.6 | path-ravage 0.4, path-gore-charge 0.4, path-devour 1.4 |
| berserker | 50 | tribal-warrior | 63.1 | -10.2 | -8.2 | path-clan-cry 0.3, path-savage-blow 0.3, path-war-standard 0.4 |
| berserker | 50 | savage | 62.1 | -11.3 | -9.2 | path-clan-cry 0.2, path-savage-blow 0.4, path-feral-leap 0.1 |
| berserker | 50 | blood-champion | 70.6 | -2.7 | -0.7 | path-challenge 0.1, path-arena-strike 0.0, path-blood-duel 1.1 |
| berserker | 50 | executor | 71.3 | -2.1 | -0.1 | path-challenge 0.2, path-arena-strike 0.0, path-behead 0.7 |
| paladin | 50 | (none) | 51.3 | | | |
| paladin | 50 | paladin-rampart | 72.3 | +21.0 | +6.0 | path-hallowed-ground 0.1, path-guardian-strike 2.6, path-wall-of-faith 0.3 |
| paladin | 50 | defender | 68.5 | +17.3 | +2.3 | path-hallowed-ground 0.2, path-guardian-strike 2.5, path-shield-of-faith 0.0 |
| paladin | 50 | paladin-inquisitor | 57.7 | +6.5 | -8.5 | path-seal-of-light 2.1, path-sacred-return 1.5, path-sentence 2.0 |
| paladin | 50 | paladin-crusader | 58.8 | +7.5 | -7.5 | path-seal-of-light 2.4, path-sacred-return 1.9, path-crusade 0.4 |
| paladin | 50 | justiciar | 74.6 | +23.3 | +8.3 | path-retribution 0.8, path-vow 0.5, path-verdict-blow 1.1 |
| paladin | 50 | paladin-fanatic | 65.6 | +14.4 | -0.6 | path-retribution 1.5, path-vow 0.6, path-holy-fury 0.6 |
| summoner | 50 | (none) | 62.1 | | | |
| summoner | 50 | demon-caller | 84.0 | +21.9 | +6.1 | path-demon 1.2, path-blood-pact 1.5, path-unleash 0.4 |
| summoner | 50 | pactbearer | 79.6 | +17.5 | +1.7 | path-demon 1.1, path-blood-pact 1.4, path-blood-price 2.1 |
| summoner | 50 | elemental-caller | 78.3 | +16.3 | +0.5 | path-pillar 1.2, path-anchor 1.5, path-overload 1.5 |
| summoner | 50 | astral-caller | 72.1 | +10.0 | -5.8 | path-pillar 1.2, path-anchor 1.3, path-constellation 1.2 |
| summoner | 50 | tamer | 86.7 | +24.6 | +8.8 | path-familiar 1.3, path-titbit 0.5, path-recall 0.0 |
| summoner | 50 | fusionist | 66.7 | +4.6 | -11.2 | path-familiar 1.3, path-titbit 0.2, path-fusion 1.1 |
| necromancer | 50 | (none) | 44.4 | | | |
| necromancer | 50 | lich | 68.5 | +24.2 | +6.3 | path-stitch 0.4, path-bone-rampart 0.0, path-unlife 1.0 |
| necromancer | 50 | bone-lord | 61.9 | +17.5 | -0.4 | path-stitch 0.8, path-bone-rampart 0.3, path-marrow 0.5 |
| necromancer | 50 | ghoul-master | 61.0 | +16.7 | -1.3 | path-ghoul 0.7, path-quarry 0.8, path-gorging 0.2 |
| necromancer | 50 | dead-commander | 55.6 | +11.3 | -6.7 | path-ghoul 0.8, path-quarry 0.8, path-offering 0.2 |
| necromancer | 50 | sanguimancer | 68.5 | +24.2 | +6.3 | path-hex 0.2, path-blight 1.2, path-boiling-blood 1.7 |
| necromancer | 50 | thanaturge | 58.1 | +13.8 | -4.2 | path-hex 0.1, path-blight 1.4, path-reaping 1.5 |
| druid | 50 | (none) | 42.1 | | | |
| druid | 50 | sylvan | 73.1 | +31.0 | +5.2 | path-hedge 0.0, path-barkskin 0.5, path-woods-wrath 1.2 |
| druid | 50 | grove-keeper | 68.1 | +26.0 | +0.2 | path-hedge 0.0, path-barkskin 0.5, path-guarding-bough 1.7 |
| druid | 50 | changeform | 58.8 | +16.7 | -9.1 | path-wild-shape 1.0, path-claws 0.9, path-pounce 0.4 |
| druid | 50 | wild-druid | 65.2 | +23.1 | -2.7 | path-wild-shape 1.0, path-claws 0.7, path-trample 0.3 |
| druid | 50 | spirit-caller | 73.5 | +31.5 | +5.7 | path-spirit 1.0, path-rumbling-ground 0.0, path-communion 0.4 |
| druid | 50 | nature-elementalist | 68.5 | +26.5 | +0.7 | path-spirit 1.0, path-rumbling-ground 0.0, path-living-storm 0.3 |

| Who | Against | Won | Median rounds | Most | At the limit | Fighters at most | ms a round |
|---|---|---|---|---|---|---|---|
| archer | 4 and 5 | 58 % | 6 | 8 | 0 | 7 | 94.8 |
| warrior | 4 and 5 | 42 % | 5 | 7 | 0 | 6 | 46.7 |
| mage | 4 and 5 | 58 % | 5 | 7 | 0 | 6 | 73.1 |
| priest | 4 and 5 | 35 % | 6 | 8 | 0 | 6 | 55.4 |
| rogue | 4 and 5 | 50 % | 4 | 6 | 0 | 6 | 97.9 |
| berserker | 4 and 5 | 73 % | 5 | 6 | 0 | 6 | 32.9 |
| paladin | 4 and 5 | 58 % | 6 | 8 | 0 | 6 | 47.4 |
| summoner | 4 and 5 | 65 % | 5 | 7 | 0 | 12 | 140.3 |
| necromancer | 4 and 5 | 43 % | 5 | 7 | 0 | 8 | 75.2 |
| druid | 4 and 5 | 30 % | 5 | 6 | 0 | 6 | 52.2 |
| 3 × archer | 6 and more | 100 % | 4 | 9 | 0 | 18 | 172.2 |
| 3 × warrior | 6 and more | 100 % | 4 | 9 | 0 | 13 | 32.7 |
| 3 × mage | 6 and more | 100 % | 3 | 9 | 0 | 13 | 123.3 |
| 3 × priest | 6 and more | 98 % | 5 | 17 | 0 | 13 | 84.4 |
| 3 × rogue | 6 and more | 90 % | 4 | 10 | 0 | 13 | 194.6 |
| 3 × berserker | 6 and more | 100 % | 3 | 6 | 0 | 13 | 111.9 |
| 3 × paladin | 6 and more | 83 % | 5 | 11 | 0 | 13 | 107.0 |
| 3 × summoner | 6 and more | 100 % | 4 | 10 | 0 | 27 | 810.0 |
| 3 × necromancer | 6 and more | 100 % | 4 | 9 | 0 | 17 | 522.5 |
| 3 × druid | 6 and more | 87 % | 4 | 9 | 0 | 13 | 235.4 |
| priest + paladin | 6 and more | 52 % | 6 | 11 | 0 | 11 | 122.1 |
