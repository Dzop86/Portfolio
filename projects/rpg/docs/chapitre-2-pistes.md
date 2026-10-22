# Osmose : le chapitre 2, les Quatre Héritages (cadre narratif)

> Proposition du scénariste, à relire par Charles avant toute fiche de quête ou tout dialogue. Elle part de « Quatre pistes » (`four-trails`, niveau 30), qui clôt le chapitre 1.
> **Convention :** **[P]** = proposition, absente de la bible et du jeu. Le canon de la bible n'est pas modifié. Les identifiants de quêtes, de lieux et de personnages sont proposés **[P]** ; ceux qui existent déjà sont cités tels quels.
> **Ce document n'écrit pas les dialogues.** Il fixe pour chaque piste : la région, le Primordial et ce qu'on y apprend, le fil principal (une ligne par étape), le PNJ relais, une quête faite pour le groupe. Les fiches complètes (bible §16) et les textes viendront piste par piste, une fois ce cadre validé.

## Sommaire
1. Ce dont on part (le canon livré)
2. Les principes du chapitre 2
3. Piste du soleil levant : Kaor, la Forge des serments
4. Piste de la côte : Névara, les Souvenirs noyés
5. Piste du grand arbre : Dhoram, les Racines du passé
6. Piste des cartes du nord : Élyon, les Cartes impossibles
7. L'ordre conseillé et les niveaux
8. Ce que le joueur sait à la fin du chapitre (niveaux d'information)
9. Ce qui demande du code ou une décision (pour le dev et le game designer)
10. Points de canon et questions pour Charles

---

## 1. Ce dont on part (le canon livré)

**La fin du chapitre 1** (données de `quests.json`) :
- Le Disjoint, « quatre bêtes forcées en une seule », a dessiné le cinquième signe en tombant (`the-disjoint`).
- La pierre s'est tue, et ses quatre signes se sont allumés, « chacun tourné vers une route qu'on n'a jamais prise » (`four-trails`).
- Trois habitants ont dit ce qu'ils savaient de ces routes :
  - **Rose** : une ville sur la côte où « personne ne raconte deux fois la même histoire » ;
  - **Garance** : des forgerons « très loin vers le soleil levant », un atelier fermé l'an passé, des armes « qui se mettaient en colère avant leur porteur » ;
  - **Aubin** : des cartes du nord qui « changent d'une nuit à l'autre », et des cartes du sud qui montrent « un arbre si grand qu'il porte une ville ».
- **Ysaline** reste avec la pierre : « quelqu'un doit l'écouter quand tu n'es pas là ».

**Ce que le joueur a en main** (niveau 1 d'information, et un pied dans le niveau 2) :
- quelqu'un a effacé avec soin : la page grattée, la tombe grattée, le nom d'Isaure lavé ;
- séparer de force ne tient pas : le sceau de la crypte se fissure, le Disjoint se déchire ;
- relier sans mélanger existe : le mycélium, les abeilles, et, sans que personne le dise, le héros lui-même, qui manie les quatre éléments sans qu'ils se mêlent ;
- un métal sombre que personne ne sait nommer : le sceau de la crypte et le débris des taupes, qui vibrent tous deux comme la pierre.

**Les quatre pistes correspondent aux quatre arcs de la bible** (§8, chapitre 2) :

| Piste | Qui l'a donnée | Primordial | Arc de la bible |
|---|---|---|---|
| Le soleil levant | Garance | Kaor (Feu) | la Forge des serments |
| La côte | Rose | Névara (Eau) | les Souvenirs noyés |
| Le grand arbre (sud) | Aubin | Dhoram (Terre) | les Racines du passé |
| Les cartes du nord | Aubin | Élyon (Air) | les Cartes impossibles |

---

## 2. Les principes du chapitre 2

1. **Ordre libre, sans mise à l'échelle.** Les quatre régions commencent toutes au niveau 30 (bible §1.8 ; `design-familles.md` §7). Chaque région compte **deux zones de 25 cartes** : la première pour les niveaux 30 à 45, la seconde pour 45 à 60. Les huit étapes d'un arc ont les mêmes niveaux d'une région à l'autre.
2. **Clairval reste le point de départ [P].** Chaque piste commence chez l'habitant qui l'a racontée (Garance, Rose, Aubin deux fois) : leur offre suit `four-trails`. Chaque arc se termine par un retour à Ysaline, près de la pierre. Le village ne devient pas une impasse : on y revient avec du neuf. *(Écart avec la ville carrefour « Quatre-Chemins » de `design-familles.md` §7 : voir §10.)*
3. **Un relais par région** : comme Mathurin, Barnabé, Liane et Églantine au chapitre 1, il donne les quêtes secondaires et une ou deux étapes principales. Le **représentant** du Primordial (bible §10) est un autre personnage : on le rencontre au milieu de l'arc, il n'est pas d'accord avec tout le monde, et il porte la faille de son Primordial.
4. **Une révélation par arc**, qui se suffit à elle-même : peu importe l'ordre, chaque arc fait avancer le joueur d'un cran vers le niveau 2 (bible §15). Ysaline interprète, et elle se trompe parfois.
5. **Le moteur actuel d'abord.** Une quête suit ses objectifs « tous à la fois » (`Data.cs`) : chaque étape ci-dessous est donc une quête, et ses objectifs n'ont pas d'ordre. Les objectifs utilisés : `Talk` (avec une réplique), `Interact` (un indice posé, y compris dans une salle de donjon), `Defeat` (une famille ou un monstre), `Bring`, `Dungeon` (avec une salle), `Win` (un scénario), `Wear`. Ce qui demande plus est signalé au §9.
6. **Jouable seul, meilleur à plusieurs.** Les étapes principales se finissent seul (règle de T81 : les objets de quête tombent sur toute la famille, pas sur le chef seul) ; leurs boss sont « groupe conseillé », gagnables seul comme le Disjoint. Chaque piste a en plus **une quête secondaire faite pour le groupe** (§3 à §6, « À plusieurs ») : chaque personnage la valide pour lui-même, rien ne dépend de ce qu'un autre a fait (bible §12).
7. **Un fil de la bible par arc**, pas plus : on n'explique jamais l'Accord, on ne le nomme pas, on ne le ressuscite pas, et aucun enfant de Primordial n'est montré.

Lecture d'une ligne d'étape : `identifiant` **[P]** · titre · niveau · type d'activité · ce qu'on fait (objectifs du moteur).

---

## 3. Piste du soleil levant : Kaor, la Forge des serments

**Région [P] :** à l'est de Clairval. Le village n'a pas de porte à l'est (« nos trois portes regardent toutes vers le Bois », Aubin) : Garance fait ouvrir **la porte de l'Est**, que la palissade cache depuis toujours **[P]**. La porte la plus évidente est celle qu'on n'a jamais percée : c'est un petit écho du thème.
- Zone 1 (30-45) : **Plaines de Cendre** (`design-familles.md` §7) ; familles : salamandres, ferrailleurs ; donjon : la Fonderie abandonnée.
- Zone 2 (45-60) : **Ateliers de la Forge-Haute** ; familles : lames vives (armes nourries de colère), gueules de lave ; donjon : l'Atelier interdit.
- Ville **[P]** : **Forge-Haute**, une cité d'artisans adossée à la cendre.

**Primordial :** Kaor, le Forgeron des Destins. Faille : il détruit ce qu'il pourrait réparer.
**Ce qu'on apprend (révélation) :** le matériau inconnu de la Forge-Haute est **le même métal** que le sceau de la crypte et le débris des Champs. Il n'est d'aucun des quatre éléments, et il amplifie les émotions parce qu'il a été fait **pour contraindre**. Le joueur soupçonne qu'une structure fabriquée existe quelque part (trame §5).
**Indice discret (Saga III, trame indice 6) :** une seule description d'objet, jamais commentée : la lingotière de l'atelier porte un poinçon que les forgerons attribuent à Kaor lui-même, sur une pièce coulée bien après la Fracture.

**PNJ relais [P] : Ferrand**, le vieux maître forgeron qui a appris l'épée à Garance. Bourru, il parle en coups de marteau et en températures ; il ne fait confiance qu'aux mains. Il a fermé l'atelier lui-même, et il s'en veut.
**Représentant de Kaor [P] : Kesh Brasemain** (nom de la trame), maître de l'atelier interdit. Brillant, impatient, généreux avec le joueur ; il veut rouvrir l'atelier et exploiter le métal, « quelques ouvriers malades, c'est le prix d'une lame qui change le monde ». Il soutient le joueur sincèrement, et il a tort sur le prix.

**Le fil principal :**
1. `east-gate` · La porte de l'Est · 31 · dialogue, exploration · Garance fait percer la palissade et donne une lettre pour Ferrand ; traverser les Plaines de Cendre et parler à Ferrand (`Talk` Ferrand).
2. `sick-workers` · Les ouvriers malades · 33 · enquête · interroger trois ouvriers de la Forge-Haute (trois `Talk` avec réplique) et examiner trois lames mises sous clef (trois `Interact`) : toutes ont été trempées dans le même bac.
3. `slag-samples` · Ce qui brûle dans la plaine · 36 · combat, collecte · rapporter cinq scories prises aux ferrailleurs, que Ferrand compare au débris des Champs (`Defeat` ferrailleurs, `Bring` scorie de métal sombre) : même métal, « et il est tiède, comme s'il attendait quelque chose ».
4. `abandoned-foundry` · La Fonderie abandonnée · 40 · donjon · descendre jusqu'à la salle 2 de la Fonderie, d'où vient le métal, et examiner la lingotière (`Dungeon` salle 2, `Interact`) ; la clef est offerte une fois.
5. `hammer-trial` · L'épreuve du marteau · 46 · artisanat · Ferrand n'ouvrira pas la Forge-Haute sans voir le joueur forger : fabriquer une pièce au niveau 45 avec le métal de la zone et la lui apporter (`Bring` de la pièce, rendue marquée comme en v6). La pièce tremble quand Ferrand se met en colère.
6. `forbidden-workshop` · L'atelier interdit · 50 · exploration, dialogue · Kesh fait entrer le joueur ; examiner trois postes de l'atelier (`Interact`) et écouter Kesh (`Talk`) : il veut recommencer, Ferrand veut tout fondre.
7. `born-of-anger` · La colère faite armure · 55 · boss (groupe conseillé) · vaincre la créature née d'une trempe ratée, une armure vide que la colère fait tenir debout (`Win`, scénario de quête).
8. `kaors-fragment` · Le Fragment de Kaor · 58 · dialogue, récompense · Ferrand montre les marques des lames : aucune ne correspond aux quatre éléments ; il remet le Fragment de Kaor (puissance, risque). Retour à Ysaline (`Talk` Ferrand, `Talk` Ysaline).

**À plusieurs [P] : `the-casting`, « La coulée »** (secondaire, 52, groupe conseillé, donneur Ferrand). Une coulée de métal sombre attire les lames vives de tout l'atelier ; il faut tenir le creuset en vie pendant un nombre de tours fixé, attaqué de quatre côtés à la fois. C'est une **protection** (type d'activité que le chapitre 1 n'a pas), pensée pour trois ou quatre joueurs qui se partagent les côtés. Seul, c'est possible mais serré. Le creuset sauvé donne à chacun un lingot pour son métier.

---

## 4. Piste de la côte : Névara, les Souvenirs noyés

**Région [P] :** la côte, à l'ouest, au bout de la rivière qui quitte le Lac des Joncs et longe le Bois moisi jusqu'à la mer. Liane l'a dit sans le savoir : « Il n'y a pas de mer, ici. Il n'y a que le lac » ; la piste lui répond.
- Zone 1 (30-45) : **Côte des Brisants** ; familles : goélands pilleurs, noyés (marins faits de souvenirs) ; donjon : l'Épave des Souvenirs.
- Zone 2 (45-60) : **Grottes de la Basse-Marée** ; familles : anguilles d'écho, reflets (ils copient les sorts) ; donjon : le Sanctuaire englouti.
- Ville **[P]** : **Port-Écume**, un port de commerce où les voyageurs de Rose vendent bien et repartent vite.

**Primordial :** Névara, la Mère des Marées. Faille : elle n'accepte pas l'oubli.
**Ce qu'on apprend (révélation) :** la mémoire de Port-Écume n'a pas été perdue, elle a été **triée**. Quelqu'un a choisi ce dont la ville devait se souvenir, comme on a lavé le nom d'Isaure. La gardienne des marées sait ce qu'est le cinquième signe, et elle le cache **pour protéger** quelque chose (bible §8, §10). Ce qu'elle cache reste au niveau 3.
**Indice discret (Saga III, trame indice 7) :** la gardienne fredonne la berceuse du Lac et s'arrête un vers avant « toi que la mer a caché ». Personne ne le relève ; le joueur qui a fait le Lac l'entendra.

**PNJ relais [P] : Solène**, la crieuse de la halle aux poissons, correspondante des voyageurs de Rose. Elle parle en prix et en marées, se souvient de chaque prix et d'aucun visage. Elle est la seule de la ville dont les souvenirs ne bougent pas, parce qu'elle note tout à la craie sur son ardoise.
**Représentante de Névara [P] : Ilse des Basses-Marées** (nom de la trame), la gardienne des marées, qui garde au sanctuaire les souvenirs des morts anciens. Chaleureuse, attentive, elle aide le joueur à retrouver la vérité de la ville, et elle ment sur une autre.

**Le fil principal :**
1. `a-town-that-forgets` · Une ville qui oublie · 31 · dialogue · Rose donne une recommandation pour Solène ; descendre la rivière jusqu'à Port-Écume et parler à Solène (`Talk`).
2. `three-versions` · Trois versions · 33 · enquête · trois témoins racontent la nuit où la cloche du port a sonné seule : un naufrage, une fête, rien du tout (trois `Talk` avec réplique) ; examiner le registre de la capitainerie (`Interact`).
3. `what-the-gulls-take` · Ce que prennent les goélands · 36 · combat, collecte · les goélands pilleurs volent des objets sur les quais ; en rapporter cinq (`Bring`), qui appartiennent aux témoins et montrent lequel ne pouvait pas avoir vu ce qu'il raconte.
4. `wreck-of-memories` · L'Épave des Souvenirs · 40 · donjon · l'épave de la nuit de la cloche, qui « n'a jamais pris la mer » ; descendre jusqu'à la salle 2 et y trouver la cloche du navire (`Dungeon` salle 2, `Interact`) ; la clef est offerte une fois.
5. `low-tide` · À marée basse · 46 · exploration · trois grottes ne s'ouvrent qu'à marée basse ; examiner leurs parois gravées (trois `Interact`). *(Sans marée dans le moteur, les grottes restent ouvertes, voir §9.)*
6. `the-tide-keeper` · La gardienne · 50 · dialogue, énigme d'information · Ilse dit n'avoir jamais vu le cinquième signe ; lui montrer le calque de la vieille pierre et la gravure des grottes (`Talk`, `Bring` du calque, rendu) : elle se tait trop longtemps, puis aide quand même.
7. `sunken-sanctuary` · Le Sanctuaire englouti · 55 · donjon, boss (groupe conseillé) · finir le sanctuaire, dont le chef est un reflet de la gardienne elle-même, qui copie le dernier sort reçu (`Dungeon`).
8. `nevaras-fragment` · Le Fragment de Névara · 58 · dialogue, récompense · Ilse remet le Fragment de Névara (adaptation, flux) et prononce la devise de Névara, « Rien ne disparaît. Tout trouve un autre chemin. » (la seule fois du chapitre). Retour à Ysaline (`Talk` Ilse, `Talk` Ysaline).

**À plusieurs [P] : `voices-at-the-quay`, « Les voix du quai »** (secondaire, 48, groupe conseillé, donneuse Solène). Un **combat contre les reflets**, qui copient le dernier sort reçu : à plusieurs, chaque joueur nourrit un reflet différent, et le groupe doit choisir qui frappe quoi pour ne pas armer l'adversaire. Le même combat se gagne seul en variant ses éléments (le héros en a quatre). La quête apprend la mécanique de la zone 2 avant le sanctuaire.

---

## 5. Piste du grand arbre : Dhoram, les Racines du passé

**Région [P] :** au sud, au-delà des Champs de Clairval, là où montent les collines. Les cartes du sud d'Aubin montrent l'arbre ; la vieille haie d'Églantine, « que son grand-père défendait d'arracher », est la dernière racine de ce côté.
- Zone 1 (30-45) : **Forêt des Aïeux** ; familles : sylvecerfs (bois de pierre), gardiens moussus (petits golems) ; donjon : les Racines de l'Arbre-Mère.
- Zone 2 (45-60) : **Mines de Sous-Racine** ; familles : fouille-roc (mineurs obsédés), vers de roche ; donjon : les Ruines sous l'Arbre.
- Ville **[P]** : **Ormeval**, bâtie dans les branches de l'Arbre-Mère.

**Primordial :** Dhoram, le Père des Racines. Faille : il supporte mal le changement.
**Ce qu'on apprend (révélation) :** sous l'arbre, un serment gravé plus ancien que toutes les archives parle de **cinq** parties, et le cinquième nom a été retiré de la pierre avec le même soin que sur la vieille pierre de Clairval. Les montagnes ont été **déplacées** pour couvrir les ruines. Le joueur soupçonne pour la première fois que ceux qui ont prêté serment, et pas des voleurs ni le temps, ont effacé le cinquième (niveau 2 : « les Primordiaux ont joué un rôle »). On ne dit pas qui a juré.

**PNJ relais [P] : Berthe**, la contremaîtresse des mineurs, dont l'équipe a ouvert le passage absent des cartes. Franche, pressée, elle pense en filons et en salaires ; elle veut exploiter ce qu'elle a trouvé pour faire vivre la ville.
**Représentant de Dhoram [P] : Bérold Serment-Fixe** (nom de la trame), juge des serments d'Ormeval. Digne, lent, rigide ; il veut refermer le passage, au nom d'un serment qu'il applique sans le comprendre. Il rappelle Mathurin, en plus haut placé et en plus dur.

**Le fil principal :**
1. `the-tree-town` · La ville dans l'arbre · 31 · dialogue · Aubin donne la carte du sud ; monter à Ormeval, parler à Bérold, qui refuse l'accès aux mines, puis à Berthe, qui demande de l'aide (deux `Talk`).
2. `the-unmapped-passage` · Le passage qui n'existait pas · 33 · exploration, enquête · suivre le passage des mineurs et examiner trois inscriptions plus anciennes que les archives d'Ormeval (trois `Interact`) ; l'une porte quatre signes autour d'un creux gratté, comme la tombe de Clairval.
3. `mossy-keepers` · Les gardiens moussus · 36 · combat, collecte · les gardiens de pierre attaquent les mineurs depuis l'ouverture du passage ; en vaincre six et rapporter trois cœurs gravés que Bérold seul sait lire (`Defeat`, `Bring`).
4. `roots-of-the-mother-tree` · Les Racines de l'Arbre-Mère · 40 · donjon · descendre sous l'arbre jusqu'à la salle 2, où les racines enserrent une dalle (`Dungeon` salle 2, `Interact`) ; la clef est offerte une fois.
5. `keep-or-dig` · Garder ou creuser · 46 · choix de dialogue · Bérold veut murer le passage, Berthe veut l'élargir ; le joueur prend parti (une réponse de dialogue). Les deux mènent à l'étape 6 ; le choix change les répliques suivantes et la récompense secondaire, jamais la suite (bible §12).
6. `moved-mountains` · Les montagnes déplacées · 50 · exploration · dans les Mines de Sous-Racine, examiner trois coupes de roche où les couches sont à l'envers (trois `Interact`) : on a posé une montagne sur quelque chose. Gaspard, à Clairval, disait que la carrière « sonne » ; ici, elle sonne aussi.
7. `the-ancient-keeper` · Le gardien ancien · 55 · boss (groupe conseillé) · vaincre le gardien des Ruines sous l'Arbre, qui applique encore un serment (`Win`, scénario de quête).
8. `dhorams-fragment` · Le Fragment de Dhoram · 58 · dialogue, récompense · Bérold lit le serment : cinq parties, un nom retiré « par ceux qui ont juré » ; il remet le Fragment de Dhoram (protection, ancrage). Retour à Ysaline (`Talk` Bérold, `Talk` Ysaline).

**À plusieurs [P] : `four-standing-stones`, « Les quatre pierres levées »** (secondaire, 53, groupe conseillé, donneur Bérold). Un gardien de pierre se protège tant que les **quatre glyphes** du sol ne sont pas tenus en même temps (la mécanique de la Terre, `design-familles.md` §7). À quatre, chacun tient sa pierre ; seul, le joueur casse le bouclier glyphe par glyphe, plus lentement, pendant qu'il se reforme. La quête montre sans un mot ce que le serment protège : quatre qui tiennent ensemble.

---

## 6. Piste des cartes du nord : Élyon, les Cartes impossibles

**Région [P] :** au nord, au-delà du Lac des Joncs, sur les falaises, puis en l'air. Les cartes qu'Aubin a vues passer viennent de là.
- Zone 1 (30-45) : **Falaises des Vents** ; familles : harpies, chèvres des cimes ; donjon : le Nid des tempêtes.
- Zone 2 (45-60) : **Archipel dérivant** ; familles : nuées (invisibles un tour sur deux), cartographes égarés ; donjon : l'Observatoire abandonné.
- Ville **[P]** : **le Belvédère**, un village de cartographes accroché à la falaise.

**Primordial :** Élyon, le Voyageur des Vents. Faille : il fuit les responsabilités.
**Ce qu'on apprend (révélation) :** les cartes ne sont pas fausses, c'est le terrain qui a été **dessiné de façon à ne pas montrer** un endroit. Une ancienne carte de l'observatoire trace **cinq routes**, et la cinquième a été déchirée : elle menait à un lieu retiré de la géographie. Le joueur sait désormais qu'un lieu est caché ; il ne sait pas lequel (ce sera la Chambre sans nom, jamais nommée ici).
**Indice discret (trame indice 5) :** le voyageur appelle le héros « celui de la vieille pierre » avant de l'avoir rencontré. Il ne dit pas comment il le sait.

**PNJ relais [P] : Corwin**, le colporteur de cartes de la trame, celui qui en vend aux voyageurs de Clairval. Bavard, joueur, de mauvaise foi par principe ; il redessine ses cartes chaque matin et les vend « en avance ». Les gens du Belvédère le trouvent pénible et l'écoutent quand même.
**Représentant d'Élyon [P] : Sorrel Vent-Vif** (nom de la trame), le voyageur insaisissable. Drôle, rapide, jamais au même endroit deux fois ; c'est le premier à comprendre que la cinquième route existe, et le premier à refuser de la prendre.

**Le fil principal :**
1. `northern-maps` · Les cartes du nord · 31 · exploration · Aubin donne la carte du nord ; suivre le chemin du Lac jusqu'aux falaises et trouver Corwin, dont trois bornes ont bougé dans la nuit (`Talk`, trois `Interact`).
2. `three-cartographers` · Trois cartographes · 33 · dialogue, enquête · retrouver trois cartographes du Belvédère, qui reviennent du même trajet avec des souvenirs incompatibles (trois `Talk` avec réplique).
3. `storm-nest` · Le Nid des tempêtes · 37 · donjon · les harpies ont emporté les instruments du Belvédère dans leur nid ; descendre jusqu'à la salle 2 et rapporter le compas (`Dungeon` salle 2, `Bring`) ; la clef est offerte une fois.
4. `the-pieced-map` · La carte reconstituée · 42 · énigme d'information · rapporter quatre morceaux de carte pris aux cartographes égarés (`Bring`), puis dire à Corwin laquelle des routes est vraie (une réponse de dialogue). Une mauvaise réponse se rejoue, sans perte.
5. `drifting-islands` · Les îles qui dérivent · 46 · exploration · traverser l'Archipel dérivant jusqu'à l'observatoire, en notant trois îles qui ne sont jamais deux fois au même endroit (trois `Interact`).
6. `the-one-who-knew-you` · Celui qui te connaissait · 50 · dialogue · Sorrel attend le héros au bord de l'archipel, comme s'il le connaissait déjà (`Talk`). Il sait que l'observatoire garde une carte « que personne n'a envie de lire ».
7. `abandoned-observatory` · L'Observatoire abandonné · 55 · donjon, boss (groupe conseillé) · finir l'observatoire (`Dungeon`).
8. `elyons-fragment` · Le Fragment d'Élyon · 58 · dialogue, récompense · examiner la carte aux cinq routes (`Interact`) ; Sorrel remet le Fragment d'Élyon (mobilité, placement) et ne viendra pas. Retour à Ysaline (`Talk` Sorrel, `Talk` Ysaline).

**À plusieurs [P] : `two-anchors`, « Les deux ancres »** (secondaire, 52, groupe conseillé, donneur Corwin). Une île dérive avec un cartographe dessus ; elle ne tient que par deux ancres, aux deux bouts de la carte de combat, attaquées en même temps par les nuées. Le groupe doit **se séparer** pour tenir les deux, ce qu'aucun combat du jeu ne demande encore. Seul, on court d'une ancre à l'autre : c'est plus dur, pas impossible.

---

## 7. L'ordre conseillé et les niveaux

**Les niveaux du chapitre 2 :** 30 à 60. Chaque arc : étapes 1 à 4 dans la zone 1 (niveaux 31 à 42), étapes 5 à 8 dans la zone 2 (46 à 58). Les quêtes secondaires et les donjons entiers comblent les trous, comme au chapitre 1. Un joueur qui fait les quatre arcs fait la zone 1 du dernier bien au-dessus de son niveau : la règle des 20 niveaux (aucune XP sur un monstre trop faible) s'applique, l'histoire reste entière.

**L'ordre conseillé (pas imposé) :**
1. **Kaor**, le soleil levant. C'est la suite la plus directe du Disjoint : le métal qui vibre est la seule chose concrète que le joueur rapporte du chapitre 1. Il apporte aussi le Feu, presque absent de la région de Clairval (risque noté dans la trame §7). Garance, la voix la plus active du chapitre 1, l'ouvre.
2. **Névara**, la côte. Elle reprend Isaure et le Lac, et elle approfondit l'effacement : on ne perd pas la mémoire, on la trie.
3. **Dhoram**, le grand arbre. C'est la révélation la plus lourde (ceux qui ont juré ont retiré le cinquième nom) : elle porte mieux quand le joueur a déjà vu le métal et la mémoire triée.
4. **Élyon**, les cartes du nord. La cinquième route déchirée pointe vers un lieu caché : c'est le pont naturel vers le chapitre 3.

Chaque arc reste lisible seul. Ysaline dit au retour de chaque arc une réplique qui dépend du nombre d'arcs finis (« une piste », « deux pistes »…), pas de l'arc lui-même : c'est elle qui relie les morceaux, quel que soit l'ordre (§9, point 3).

**Ordre de production (pour Charles et le game designer) :** même ordre que l'ordre conseillé. La « proposition minimale » de `suite-apres-d69.md` (une seule région de deux zones d'abord) devient **la piste de Kaor** ; tant que les trois autres régions n'existent pas, leurs offres ne s'affichent pas, et les répliques de `four-trails` restent vraies.

**Après les quatre arcs [P] :** `four-fragments`, « Quatre fragments » (niveau 60, Ysaline), demande les quatre étapes 8 (`After` sur les quatre) : la pierre réagit aux fragments réunis, et les quatre représentants convoquent le joueur. C'est la porte du chapitre 3 (la convergence) ; elle n'est pas écrite ici.

---

## 8. Ce que le joueur sait à la fin du chapitre (niveaux d'information)

| Arc | Ce qu'il **sait** (niveau 1) | Ce qu'il **soupçonne** (niveau 2) | Ce qui reste **à découvrir** (niveau 3) |
|---|---|---|---|
| Kaor | le métal sombre vient d'une même fabrication, aucun élément ne le marque | il existe une structure faite pour contraindre | qui l'a construite, et pourquoi le poinçon de Kaor y figure |
| Névara | la mémoire d'une ville a été triée, la gardienne reconnaît le signe | effacer a été un choix, fait pour protéger | ce que la gardienne cache |
| Dhoram | un serment ancien comptait cinq parties | ceux qui ont juré ont retiré le cinquième | qui a juré, et ce qu'était le cinquième |
| Élyon | une ancienne carte trace cinq routes, la cinquième est déchirée | un lieu a été retiré de la géographie | où mène la route, et pourquoi Sorrel connaît le héros |

Ysaline interprète à mesure, et elle se trompe : jusqu'au troisième arc, elle croit que les pierres sont des **avertissements** laissés pour qu'on n'approche pas ; le serment de Dhoram la fait douter (« et si on ne voulait pas nous prévenir, mais nous faire oublier ? »). Elle ne nomme jamais l'Accord, ni la Disjonction (nommée au chapitre 3).

---

## 9. Ce qui demande du code ou une décision (pour le dev et le game designer)

1. **Le monde au-delà de 30** : quatre régions de deux zones, quatre villes, la porte de l'Est de Clairval, les familles nouvelles et leurs mécaniques (`design-familles.md` §7 : glyphes, copie de sort, colère cumulée, invisibilité). C'est le gros du travail, et c'est au game designer.
2. **Les Fragments** (bible §9) : un système persistant par personnage, un seul actif, changé hors combat. Leurs effets sont au game designer. La récompense d'une quête doit pouvoir donner un fragment.
3. **Une réplique qui dépend de l'avancement** : le retour à Ysaline change selon le nombre d'arcs finis (§7). Il faut une condition « nombre de ces quêtes faites » sur une réplique, ou quatre variantes de la même quête.
4. **Des réponses de dialogue qui comptent** : `keep-or-dig` (un choix gardé sur le personnage, qui change des répliques et une récompense) et `the-pieced-map` (une bonne réponse valide l'objectif, une mauvaise se rejoue). Le format des dialogues a des réponses, pas encore d'effet sur les quêtes ni de drapeaux.
5. **Trois combats d'un genre nouveau**, tous en scénario de quête : tenir un objet en vie pendant N tours (`the-casting`), quatre cases à tenir ensemble pour ouvrir un bouclier (`four-standing-stones`), deux objets à protéger aux deux bouts de la carte (`two-anchors`). Le combat à plusieurs existe (T88, T89) ; ces conditions de victoire et de défaite, non.
6. **La marée** (`low-tide`) : des grottes ouvertes selon l'horloge du serveur, comme l'errance des groupes (T86). Facultatif : sans elle, les grottes restent ouvertes et le texte dit « à marée basse » sans rien imposer.
7. **Les îles qui dérivent** (`drifting-islands`) : des passages entre cartes qui changent avec l'horloge. Facultatif aussi : la dérive peut n'être qu'un décor et des `Interact`.
8. **Un objet qui suit le joueur d'arc en arc** : le calque de la vieille pierre **[P]**, donné par la première étape de chaque arc s'il ne l'a pas (`Grants`), montré à Ilse (`Bring` puis rendu, comme les premières pièces de la v6).
9. **Rien ne bloque** (bible §17) : chaque arc ne dépend que de `four-trails` ; les clefs des donjons d'étape sont offertes une fois, puis viennent du serrurier ; une réponse de dialogue ratée se rejoue ; les objets de quête tombent sur toute la famille, pas seulement sur un chef.

---

## 10. Points de canon et questions pour Charles

### 10.1 Ce que je signale (rien n'est corrigé)
- **Les signes de la pierre ne regardent plus les mêmes côtés.** Dans `four-signs`, la graine regarde le Bois (ouest), la goutte le Lac (nord), la plume les Champs (sud), le marteau un endroit inconnu. Dans `four-trails`, Aubin place l'arbre (Dhoram) au sud et les cartes changeantes (Élyon) au nord : la graine et la plume ont changé de côté. Ma proposition **[P]** : c'est voulu, la pierre s'est **tournée** en se taisant, et Ysaline le remarque à la première étape d'un arc (une anomalie de plus, « des cartes changent sans raison », bible §2). Sinon, il faut retoucher l'une des deux répliques.
- **Clairval n'a pas de porte à l'est** (« nos trois portes regardent toutes vers le Bois »), alors que la piste de Garance va « vers le soleil levant ». Je propose une porte de l'Est percée au chapitre 2 **[P]** ; la trame parlait déjà de « Garance ouvre la porte est ».
- **Pas de ville carrefour au chapitre 2.** `design-familles.md` §7 prévoit Quatre-Chemins entre Clairval et les quatre régions. Les quatre routes de `four-trails` partent de Clairval même, dans quatre directions ; une ville carrefour unique les contredirait. Je propose de garder le nom pour le lieu de la convergence (chapitre 3), ou de l'abandonner.
- **La bible place le chapitre 2 sur les niveaux 15 à 60** ; le jeu le fait commencer à 30, puisque le chapitre 1 va jusqu'à 30 (option A de la trame, déjà vécue par le jeu, à inscrire dans `DECISIONS.md`).
- **Le représentant de Kaor est un homme dans la bible** (« celui de Kaor ») ; la trame en fait une forgeronne, Kesh. J'ai suivi la bible.
- **La gardienne des marées et la représentante de Névara sont une seule personne** (Ilse) : la bible les décrit séparément, mais leurs traits se recouvrent (aider tout en cachant, reconnaître le signe).
- **Les deux indices de la Saga III** (le poinçon de Kaor, le vers où s'arrête Ilse) restent des détails non commentés. Aucun enfant de Primordial n'est nommé ni montré. L'Accord n'est ni nommé ni ressuscité. La réplique du gardien de la Chambre sans nom n'apparaît pas.
- **Le héros aux quatre éléments** : une seule touche possible, dans l'arc de Kaor **[P]** : le métal sombre tremble dans toutes les mains en colère, sauf dans celles du héros. Personne ne le commente. Je ne l'écris que si vous la voulez.

### 10.2 Les questions
1. **La géographie** : Kaor à l'est (porte de l'Est), Névara à l'ouest au bout de la rivière du Lac, Dhoram au sud au-delà des Champs, Élyon au nord au-delà du Lac. Ça vous va ?
2. **Les signes qui ont tourné** : on en fait une anomalie voulue (ma proposition), ou on retouche une réplique livrée ?
3. **Les noms nouveaux** : Ferrand, Solène, Berthe (relais) ; Forge-Haute, Port-Écume, Ormeval, le Belvédère (villes). **Les noms de la trame** : Kesh Brasemain, Ilse des Basses-Marées, Bérold Serment-Fixe, Sorrel Vent-Vif, Corwin. Lesquels gardez-vous ? À vérifier de votre côté qu'aucun n'existe chez Ankama : je n'en connais pas, sans pouvoir le garantir.
4. **Kesh** : un homme comme dans la bible, ou une femme comme dans la trame ?
5. **L'ordre de production** : Kaor d'abord (ma recommandation), puis Névara, Dhoram, Élyon ?
6. **Le premier vrai choix du jeu** (`keep-or-dig`, Bérold ou Berthe) : le voulez-vous dès le chapitre 2 ? Il demande des drapeaux par personnage.
7. **Quatre-Chemins** : gardé pour le chapitre 3, ou abandonné ?
8. **La touche sur le héros aux quatre éléments** (§10.1, dernier point) : oui ou non ?

### 10.3 Réponses de Charles (22 octobre 2026, D79)
1. Géographie : **oui**, telle quelle.
2. Les signes qui ont tourné : **une anomalie voulue**, aucune réplique livrée n'est retouchée.
3. Les noms : **à relire** ; seuls Berthe, Bérold et Ormeval servent dans la tranche de la Forêt des Aïeux.
4. Kesh : **plus tard**, avec la piste du Feu.
5. L'ordre : **la Terre d'abord** (Forêt des Aïeux, 30 à 45), comme dans le plan du game designer (`suite-apres-d69.md` §11.6).
6. « Garder ou creuser » : **plus tard**, l'étape (niveau 46) est hors de la tranche.
7. Quatre-Chemins : **gardé**, comme l'étape où les routes parties de Clairval se séparent ; les quatre habitants donnent toujours leur piste à Clairval, et Quatre-Chemins est l'endroit où elles bifurquent.
8. Le métal sombre qui ne tremble pas dans les mains du héros : **oui**.

