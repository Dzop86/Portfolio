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
