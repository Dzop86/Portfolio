# Osmose : relecture des textes des sprints 145 à 151 (le scénariste)

> **Relecture du scénariste, 27 octobre 2026, avant celle de Charles (règle M10 b).** Ces textes ont été écrits par Claude, le développeur, pendant S145 (les guildes, T112) et S149 à S151 (l'arbre des talents, T116 à T118).
> **Ce qui a été relu :** `data/dialogues/registrar.json` et l'entrée d'Edmée dans `data/towns/clairval.json` ; les clés `guild.*` et `tree.*` de `src/Rpg.Client/Texts.cs`, en FR et en EN ; dans `data/talents.json`, les 56 lignes d'ambiance (`lore`) et les 56 contreparties (`counterpart`).
> **Lu avant d'écrire :** `docs/bible.md` et la bible complète, `docs/talents-canon.md` (mes propositions du 23 octobre), `docs/talents-arbre.md` (§9, le canon à trancher), `docs/talents-arbre-questions.md` (M1 à M10), `docs/elements.md`, `docs/textes-v5.md` (les voix de Clairval), `docs/textes-v9.md`, `DECISIONS.md` (T106 à T118), `data/dialogues/odalric.json`.
> **Rien n'est modifié dans les données ni dans le code.** Le développeur reporte les corrections lui-même. **[P]** marque ce qui n'est ni dans la bible ni dans le jeu.
> **Reporté le 27 octobre 2026 par le développeur (D83) :** toutes les corrections C1 à C111 et le dialogue d'Edmée, avec les valeurs par défaut des questions 1, 2, 4 et 5 du §9. Restent à Charles : les renommages Harmonie et Sceau (§8.1, question 3) et la règle du tutoiement (question 6) ; les signalements 2, 3, 5, 6, 8 et 9 du §8 ne sont pas traités (le 7 est couvert par un test).
> **Comment lire une correction :** l'identifiant, le texte actuel, la version proposée en FR et en EN, puis la raison en une ligne. Quand seul l'anglais change, le français n'est pas recopié.

## Sommaire
1. Les trois corrections qui comptent le plus
2. Edmée et les guildes
3. L'arbre : les noms des couches et les 13 bandeaux
4. Les 56 lignes d'ambiance
5. Les 56 contreparties
6. Les autres textes `tree.*`
7. Ce qui est bon tel quel
8. Signalements hors du périmètre (rien n'est corrigé)
9. Questions pour Charles

---

## 1. Les trois corrections qui comptent le plus

1. **La couche « Fusion » contredit l'indice central du héros.** Le canon du jeu dit que le héros est le seul être chez qui les quatre éléments **ne se mélangent pas** (Garance dans `well-equipped`, `talents-canon.md` §1.1). Dans le monde, **la fusion forcée, c'est le Disjoint** et les zombies (`talents-canon.md` §3.1 et §3.3 : « la fusion, c'est le Disjoint »). Un composé se lit comme une **tresse**, deux éléments qui courent ensemble sans se confondre (`talents-arbre.md` §9.2). Le moteur l'appelle déjà `Braids`, et l'onglet dit déjà « Tresse : Foudre ». Il faut donc renommer la couche **« Tresse » / "Braid"** et réécrire son bandeau et la ligne de la Conversion (§3 et §4).
2. **La ligne d'ambiance de la Conversion dit le contraire du canon** : « Deux éléments n'en font plus qu'un, et l'on ne sait plus lequel parle. » C'est la grande bascule de l'arbre, celle que le joueur lit au moment le plus marquant. Elle doit dire que les deux éléments restent deux, dans le même geste (§4, `convert-lightning`).
3. **Deux noms de talents sont sur la liste des noms interdits** de `talents-canon.md` §5 : **Harmonie** (le vocabulaire de l'Accord ; avec sa ligne « Deux voix qui chantent juste », il évoque aussi l'accord musical) et **Sceau** (le vocabulaire de la Disjonction et de ce qu'on scelle). Les noms des talents ne faisaient pas partie de la commande, et Charles n'a pas encore validé cette liste. Je le signale donc et je propose des remplacements (§8.1) ; pour Harmonie, je propose aussi une ligne d'ambiance de rechange (§4).

Viennent ensuite deux fautes d'anglais visibles au premier coup d'œil (« Trunk, no point », qui se lit « le tronc, ça ne sert à rien », et « Send {0} out »). Il y a aussi cinq lignes qui supposent un héros femme (« Tu es partie », « Voir avant d'être vue »…) alors que le joueur choisit « Homme » ou « Femme » à la création.

---

## 2. Edmée et les guildes

### 2.1 Qui est Edmée [P]

Le personnage tient. Une greffière, sur la place, à côté de la maison commune, qui inscrit les guildes dans un registre : c'est un bon écho au registre d'Odalric, le fil de la Saga I (« Le registre ne ment pas. Il lui arrive seulement de se taire. »). Sa voix ne doit pas se confondre avec celle d'Odalric, qui vouvoie aussi, mais lentement et avec des phrases complètes.

- **Ton [P] :** vive, exacte, des formules de greffe ; elle cite le règlement par articles et elle a un humour sec. Elle vouvoie.
- **Trait [P] :** elle barre, elle ne gratte jamais. Une guilde dissoute reste sur sa page, rayée d'un trait.
- **Ce qu'elle sait [P] :** rien du mystère. Elle a seulement horreur d'une page grattée, comme d'un mensonge, sans savoir qu'Odalric en a trouvé une. Un joueur qui a fait « Ce que le registre tait » y verra un clin d'œil, et les autres une manie de greffière. Cela reste au niveau 1 d'information (des archives incomplètes), sans rien révéler.
- **« La mairie » :** le mot est nouveau dans le jeu (aucun maire n'a de nom). Il passe dans un village de fantaisie à la française, et je le garde. Si Charles le trouve trop moderne, « la maison commune » le remplace sans rien changer d'autre.

### 2.2 Le dialogue (`data/dialogues/registrar.json`)

**C1. `hello`** (le FR change à peine, l'EN davantage)
- Actuel fr : « Edmée, greffière de la mairie. C'est moi qui tiens le registre des guildes de Clairval. Pour en fonder une, il me faut un nom que personne n'a pris, et mille Éclats pour le sceau et l'encre. »
- Actuel en : "Edmée, clerk of the town hall. I keep the register of Clairval's guilds. To found one I need a name nobody has taken, and a thousand Éclats for the seal and the ink."
- Proposé fr : « Edmée, greffière de la mairie. Je tiens le registre des guildes de Clairval. Pour en inscrire une, il me faut un nom que personne n'a pris, et mille Éclats pour le sceau et l'encre. »
- Proposé en : "Edmée, town clerk. I keep Clairval's register of guilds. To enter a new one, I need a name nobody has taken, and a thousand Éclats for the seal and the ink."
- Raison : c'est le joueur qui fonde et Edmée qui inscrit ; "town clerk" est le mot britannique naturel.

**C2. `why`** (trop long : quatre phrases et cinq règles dans une bulle ; on le coupe en deux répliques, voir C3)
- Actuel fr : « On porte son nom sous le sien, on se parle d'où qu'on soit (/gu devant la phrase) et on voit qui est en jeu. Le meneur et ses officiers invitent, cinquante au plus, et chacun part quand il veut. Si le meneur s'en va, le plus ancien officier reprend le registre. »
- Actuel en : "You wear its name under your own, you talk to each other wherever you are (/gu before the line) and you see who is in play. The leader and the officers invite, fifty at most, and anyone may leave when they like. If the leader goes, the oldest officer takes the register over."
- Proposé fr : « Le nom de la guilde s'inscrit sous le vôtre, et vous vous parlez d'où que vous soyez : commencez la phrase par /gu. Vous savez aussi qui est sur les routes. Et chacun s'en va quand il veut. »
- Proposé en : "The guild's name is written under yours, and you can talk wherever you are: start the line with /gu. You'll also know who's out on the roads. And anyone may leave when they please."
- Raison : « son nom sous le sien » est ambigu ; « en jeu » n'est pas un mot de personnage ; en anglais, "the oldest officer" désigne l'âge, pas l'ancienneté. Le « /gu » reste, comme le « (J) » de Perrine.

**Réponses de `why` (nouvelles) :** « Et le règlement ? » / "And the rules?" → `rules` ; « Je veux fonder une guilde. » (`"guild": true`) ; « Merci, Edmée. ».

**C3. `rules` (nouvelle réplique, la seconde moitié de l'ancienne `why`) [P]**
- fr : « Article premier : le meneur et ses officiers invitent. Article deux : cinquante membres au plus, la salle des fêtes n'en tient pas davantage. Article trois : si le meneur s'en va, l'officier le plus ancien reprend la plume. »
- en : "Article one: the leader and the officers send the invitations. Article two: fifty members at most, as the village hall holds no more. Article three: if the leader leaves, the longest-serving officer takes up the pen."
- Réponses : « Je veux fonder une guilde. » (`"guild": true`) ; « Merci, Edmée. » / "Thank you, Edmée."
- Raison : la limite de 50 reçoit une raison dans le monde, et la voix d'Edmée (le règlement) se distingue de celle d'Odalric. La salle des fêtes est **[P]**, cohérente avec la fête de la pierre de Basile.

**C4. `registers` (nouvelle, pour ancrer Edmée dans le lore de Clairval) [P]**
- Réponse qui y mène, dans `hello` : « Un registre ? » / "A register?"
- fr : « Le grand registre, c'est Odalric, à la chapelle : naissances, mariages, morts. Moi, je tiens le petit, celui des gens qui choisissent de faire route ensemble. »
- en : "The great register is Odalric's, up at the chapel: births, marriages, deaths. Mine is the little one, for people who choose to travel together."
- Réponses : « Et quand une guilde disparaît ? » / "And when a guild comes to an end?" → `struck` ; « Je veux fonder une guilde. » (`"guild": true`) ; « Merci, Edmée. ».

**C5. `struck` (nouvelle) [P]**
- fr : « Je la barre d'un trait, et son nom redevient libre. Barrer, jamais gratter : une page grattée, c'est un mensonge qui sèche. »
- en : "I strike it through, and the name is free again. Strike, never scrape: a scraped page is a lie left to dry."
- Réponses : « Je veux fonder une guilde. » (`"guild": true`) ; « Merci, Edmée. ».
- Raison : un écho discret à la page grattée du registre (`what-the-registry-leaves-out`), sans rien révéler. **À vérifier par le dev :** le nom d'une guilde dissoute redevient-il bien libre (`GuildRules`, unicité parmi les guildes existantes) ? Sinon, il faut retirer « et son nom redevient libre ».

**Le graphe proposé** (5 répliques ; chacune est atteignable, et chacune a une réponse qui ferme) :

```json
{
  "id": "registrar",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Edmée, greffière de la mairie. Je tiens le registre des guildes de Clairval. Pour en inscrire une, il me faut un nom que personne n'a pris, et mille Éclats pour le sceau et l'encre.", "en": "Edmée, town clerk. I keep Clairval's register of guilds. To enter a new one, I need a name nobody has taken, and a thousand Éclats for the seal and the ink." },
      "answers": [
        { "text": { "fr": "Je veux fonder une guilde.", "en": "I want to found a guild." }, "guild": true },
        { "text": { "fr": "À quoi sert une guilde ?", "en": "What is a guild for?" }, "next": "why" },
        { "text": { "fr": "Un registre ?", "en": "A register?" }, "next": "registers" },
        { "text": { "fr": "Au revoir.", "en": "Goodbye." } }
      ]
    },
    "why": {
      "text": { "fr": "Le nom de la guilde s'inscrit sous le vôtre, et vous vous parlez d'où que vous soyez : commencez la phrase par /gu. Vous savez aussi qui est sur les routes. Et chacun s'en va quand il veut.", "en": "The guild's name is written under yours, and you can talk wherever you are: start the line with /gu. You'll also know who's out on the roads. And anyone may leave when they please." },
      "answers": [
        { "text": { "fr": "Et le règlement ?", "en": "And the rules?" }, "next": "rules" },
        { "text": { "fr": "Je veux fonder une guilde.", "en": "I want to found a guild." }, "guild": true },
        { "text": { "fr": "Merci, Edmée.", "en": "Thank you, Edmée." } }
      ]
    },
    "rules": {
      "text": { "fr": "Article premier : le meneur et ses officiers invitent. Article deux : cinquante membres au plus, la salle des fêtes n'en tient pas davantage. Article trois : si le meneur s'en va, l'officier le plus ancien reprend la plume.", "en": "Article one: the leader and the officers send the invitations. Article two: fifty members at most, as the village hall holds no more. Article three: if the leader leaves, the longest-serving officer takes up the pen." },
      "answers": [
        { "text": { "fr": "Je veux fonder une guilde.", "en": "I want to found a guild." }, "guild": true },
        { "text": { "fr": "Merci, Edmée.", "en": "Thank you, Edmée." } }
      ]
    },
    "registers": {
      "text": { "fr": "Le grand registre, c'est Odalric, à la chapelle : naissances, mariages, morts. Moi, je tiens le petit, celui des gens qui choisissent de faire route ensemble.", "en": "The great register is Odalric's, up at the chapel: births, marriages, deaths. Mine is the little one, for people who choose to travel together." },
      "answers": [
        { "text": { "fr": "Et quand une guilde disparaît ?", "en": "And when a guild comes to an end?" }, "next": "struck" },
        { "text": { "fr": "Je veux fonder une guilde.", "en": "I want to found a guild." }, "guild": true },
        { "text": { "fr": "Merci, Edmée.", "en": "Thank you, Edmée." } }
      ]
    },
    "struck": {
      "text": { "fr": "Je la barre d'un trait, et son nom redevient libre. Barrer, jamais gratter : une page grattée, c'est un mensonge qui sèche.", "en": "I strike it through, and the name is free again. Strike, never scrape: a scraped page is a lie left to dry." },
      "answers": [
        { "text": { "fr": "Je veux fonder une guilde.", "en": "I want to found a guild." }, "guild": true },
        { "text": { "fr": "Merci, Edmée.", "en": "Thank you, Edmée." } }
      ]
    }
  }
}
```

`data/towns/clairval.json` : l'entrée d'Edmée (nom identique en FR et en EN, apparence, place devant la maison de la place) est bonne telle quelle.

### 2.3 Les textes de la guilde (`Texts.cs`, `guild.*`)

**C6. `guild.none`**
- Actuel fr : « Vous n'êtes dans aucune guilde. Edmée, la greffière de la place de Clairval, en fonde une pour {0} Éclats ; un meneur ou un officier peut aussi vous inviter. »
- Actuel en : "You are in no guild. Edmée, the clerk on Clairval's square, founds one for {0} Éclats; a leader or an officer may also invite you."
- Proposé fr : « Vous n'êtes dans aucune guilde. Edmée, la greffière, sur la place de Clairval, en inscrit une nouvelle pour {0} Éclats ; un meneur ou un officier peut aussi vous inviter. »
- Proposé en : "You are not in a guild. Edmée, the town clerk on Clairval's square, will enter a new one for {0} Éclats; a leader or an officer can also invite you."
- Raison : Edmée ne fonde pas de guilde à la place du joueur, elle l'inscrit ; "You are in no guild" sonne traduit.

**C7. `guild.col.where`** : « En jeu / vu » / "In play / seen" → fr « Où / vu » ; en "Where / last seen". Raison : la colonne montre la zone ou la dernière venue ; « en jeu » répète la pastille.

**C8. `guild.name`** (en seul) : "A character in play" → "Name of a character in play". Raison : c'est le texte indicatif d'un champ, et le FR dit « Nom d'un… ».

**C9. `guild.demote`** (en seul) : "Make {0} a plain member" → "Demote {0} to member". Raison : la formule courante en anglais.

**C10. `guild.kick`** (en seul) : "Send {0} out" → "Remove {0} from the guild". Raison : "send out" veut dire « envoyer en mission » ou « faire sortir de la pièce ».

**C11. `guild.hand-over`** (en seul) : "Hand the title of leader to {0}" → "Hand leadership to {0}". Raison : plus court et naturel.

**C12. `guild.invited`** (en seul) : "{0} invites you into the guild {1}." → "{0} invites you to join {1}." Raison : "invite into" est un calque du français.

**C13. `guild.found.name`**
- Actuel : « Nom de la guilde (3 à 20 lettres) » / "The guild's name (3 to 20 letters)"
- Proposé : « Nom de la guilde (3 à 20 caractères) » / "Guild name (3 to 20 characters)"
- Raison : les espaces, les tirets et les apostrophes comptent (`GuildRules`) ; « lettres » ment au joueur.

**C14. `guild.founded`** (en seul) : "The guild {0} is founded." → "The guild {0} has been founded." Raison : le présent passif sonne faux en anglais.

**C15. `guild.joined`** (en seul) : "You are in the guild {0} now." → "You have joined {0}." Raison : naturel et plus court.

**C16. `guild.needs-server`**
- Actuel : « Les guildes demandent le serveur. » / "Guilds need the server."
- Proposé : « Les guildes ne fonctionnent qu'en ligne. » / "Guilds only work online."
- Raison : le joueur ne pense pas « serveur », il pense « en ligne ».

---

## 3. L'arbre : les noms des couches et les 13 bandeaux

### 3.1 Les couches (M1 a)

**Mon avis sur M1 a :** validé, sauf « Fusion ».

**C17. `tree.layer.Fusion`** : « Fusion » / "Fusion" → **« Tresse » / "Braid"**.
Raison : la fusion est l'image du Disjoint, et le héros est celui chez qui les éléments ne se mêlent pas ; « Tresse » est déjà le mot de l'onglet (`talents.branch.Braids`).

**C18. `tree.layer.Affinities`** (facultatif) : « Affinités » / "Affinities" → « Paires » / "Pairs".
Raison : l'onglet dit déjà « Paires », et dans la bible, « plusieurs affinités » est le signe des créatures anormales. Ce n'est pas une faute, mais deux noms pour la même couche brouillent la lecture. Si Charles tient à « Affinités », c'est acceptable.

« Fondations », « Élément pur » et « Styles » sont bons. « Fondations » fait un peu écho à Dhoram (« toute chose doit posséder des fondations solides »), mais le mot est assez courant pour ne rien révéler.

### 3.2 Les bandeaux (M2 a)

**C19. `tree.banner.Foundations`** (en seul) : "The trunk lights up by itself with the level: nothing to choose, all to gain." → "The trunk lights up on its own as you level: nothing to choose, everything to gain." Raison : "with the level" et "all to gain" sont des calques.

**C20. `tree.banner.Affinities`**
- Actuel : « Une paire lie deux éléments et donne sa réaction. Deux paires au plus, sans élément commun. » / "A pair binds two elements and gives its reaction. Two pairs at most, sharing no element."
- Proposé fr : « Une paire associe deux éléments : frapper l'un sur l'empreinte de l'autre déclenche sa réaction. Deux paires au plus, sans élément commun. »
- Proposé en : "A pair teams up two elements: strike one on the other's mark to set off its reaction. Two pairs at most, sharing no element."
- Raison : « lier » est un mot de l'Accord (`talents-canon.md` §5 : Lien, Liant, Relation), et le bandeau doit dire comment la réaction se déclenche.

**C21. `tree.banner.Pure`**
- Actuel : « Un seul élément, plus fort : un Cœur ouvre les styles sans prendre d'élément. » / "One element, stronger: a Heart opens the styles without taking an element."
- Proposé fr : « Un élément poussé à fond, les autres un peu moins : un Cœur ouvre les styles sans toucher à tes éléments. »
- Proposé en : "One element pushed hard, the others a little less: a Heart opens the styles without touching your elements."
- Raison : « un seul élément… sans prendre d'élément » se contredit à la lecture, et le prix (−5 % ailleurs) manquait.

**C22. `tree.banner.Fusion`** (la clé reste `Fusion` dans le code)
- Actuel : « Les deux éléments d'une paire se fondent en un composé : une Transmutation, puis la Conversion. » / "A pair's two elements melt into a compound: a Transmutation, then the Conversion."
- Proposé fr : « Les deux éléments d'une paire courent ensemble sans se confondre : d'abord une Transmutation, puis la Conversion. »
- Proposé en : "A pair's two elements run together without blending: first a Transmutation, then the Conversion."
- Raison : canon, voir §1 point 1.

**C23. `tree.banner.Styles`**
- Actuel : « Une manière de se battre, partagée par toutes les paires : un étage après l'autre. » / "A way of fighting, shared by every pair: one floor after the other."
- Proposé fr : « Une manière de se battre, ouverte à toutes les portes : on la monte un étage après l'autre. »
- Proposé en : "A way of fighting, open to every door: climb it one floor at a time."
- Raison : un Cœur ouvre aussi les styles, pas seulement les paires ; "one floor after the other" est un calque.

**C24. `tree.banner.Melee`** (en seul) : "Up close: stronger near, at the cost of range." → "Up close: harder hits at short range, at the cost of reach." Raison : "stronger near" n'est pas de l'anglais.

**C25. `tree.banner.Support`** (en seul) : "For the others: your spells help an ally instead of hitting it. Made for playing together." → "For the others: your spells help an ally instead of hitting them. Made for group play." Raison : on ne dit pas "it" d'un allié, et "playing together" est plat.

**C26. `tree.banner.Terrain`**
- Actuel : « Le sol : glyphes, obstacles et pas comptés. » / "The ground: glyphs, obstacles and counted steps."
- Proposé : « Le terrain : glyphes, obstacles, et chaque pas qui compte. » / "The ground: glyphs, obstacles, and every step that counts."
- Raison : « pas comptés » se lit aussi « qui ne sont pas comptés ».

**C27. `tree.banner.Alchemy`** (en seul) : "The reactions: marks that last, are shared or spread. Asks for a pair." → "The reactions: marks that last, are shared or spread. Needs a pair." Raison : "asks for" est un calque de « demande ».

**C28. `tree.banner.Key`** (en seul) : "One key a style, at level 45: the strongest, and it costs 2 points." → "One key per style, from level 45: the strongest node, and it costs 2 points." Raison : "a style" pour « par style » est un calque.

**C29. `tree.banner.Choice`** (en seul) : "A choice: only one of the framed nodes." → "Pick one: only one framed node can be taken." Raison : naturel.

---

## 4. Les 56 lignes d'ambiance (`lore`)

Ces lignes sont la meilleure part de S149 : elles sont courtes et imagées, et elles disent souvent le prix du talent sans un chiffre. Les corrections portent sur le canon (2), sur le genre du héros (5), sur des ambiguïtés en français (3), sur les répétitions et sur un anglais trop guindé ("one's", "one does not").

**C30. `convert-lightning`** (canon, §1 point 2)
- Actuel : « Deux éléments n'en font plus qu'un, et l'on ne sait plus lequel parle. » / "Two elements become one, and one no longer knows which speaks."
- Proposé : « Deux éléments dans le même trait, et aucun ne se tait. » / "Two elements in a single stroke, and neither falls silent."
- Raison : le héros garde les deux éléments dans le composé (`talents-arbre.md` §9.5) ; « n'en font plus qu'un », c'est la fusion.

**C31. `harmony`** (lié au nom, §8.1)
- Actuel : « Deux voix qui chantent juste se protègent. » / "Two voices singing true shield each other."
- Proposé : « Un toit pour deux, et la pluie tombe à côté. » / "One roof for two, and the rain falls elsewhere."
- Raison : des voix qui chantent juste ensemble, c'est l'image même de l'Accord ; la nouvelle ligne garde l'idée d'un abri partagé.

**C32. `echo`** (genre)
- Actuel : « Tu es partie, ton geste est resté. » / "You have gone, your gesture stayed."
- Proposé : « Tu es déjà loin, et ton sort est resté. » / "You are long gone, but your spell stayed behind."
- Raison : le héros peut être un homme ; « geste » revient déjà dans deux autres talents.

**C33. `long-sight`** (genre) : « Voir avant d'être vue. » → « Voir avant d'être vu. » (l'EN "See before being seen." reste). Raison : l'accord au féminin vise une héroïne.

**C34. `virtuoso`** (genre)
- Actuel : « Elle n'écoute que sa propre musique. » / "She listens to her own music only."
- Proposé : « On ne danse jamais si bien que sur sa propre musique. » / "Nobody dances better than to their own tune."
- Raison : « elle » ne désigne personne, et "She" impose un genre en anglais.

**C35. `devotion`** (genre)
- Actuel : « Elle a cessé de compter ce qu'elle donne. » / "She stopped counting what she gives."
- Proposé : « À force de donner, on ne compte plus. » / "Give long enough, and you stop counting."
- Raison : même raison que C34.

**C36. `long-breath`** (genre implicite)
- Actuel : « Lente à se lever, longue à s'éteindre. » / "Slow to rise, long to fade."
- Proposé : « Comme la braise : lente à prendre, longue à s'éteindre. » / "Like embers: slow to catch, slow to die."
- Raison : sans sujet, le féminin se lit comme le héros ; "long to fade" n'est pas idiomatique.

**C37. `close-quarters`** (ambiguïté)
- Actuel : « Plus de distance, plus de doute. » / "No more distance, no more doubt."
- Proposé : « Au contact, plus de place pour le doute. » / "Up close, there is no room for doubt."
- Raison : « plus de distance » se lit aussi « davantage de distance », le contraire du talent.

**C38. `nomad`** (ambiguïté)
- Actuel : « Moins de bagages, plus de chemin. » / "Less baggage, more road."
- Proposé : « Moins de bagages, et la route s'allonge. » / "Travel light, travel far."
- Raison : « plus de chemin » se lit aussi « il n'y a plus de chemin ».

**C39. `heart-water`**
- Actuel : « L'eau qui ne se partage pas finit par creuser la roche. » / "Water that is not shared ends up carving the rock."
- Proposé : « Goutte après goutte, au même endroit, l'eau creuse la roche. » / "Drop by drop, always in the same place, water wears through stone."
- Raison : « qui ne se partage pas » sonne comme un égoïsme, à rebours de Névara ; l'image de la constance est plus juste pour un Cœur.

**C40. `seal`**
- Actuel : « Un signe sur le sol, et l'ennemi se marque lui-même. » / "A sign on the ground, and the enemy marks itself."
- Proposé : « Un glyphe au sol, et l'ennemi se marque lui-même. » / "A glyph on the ground, and the enemy marks itself."
- Raison : « signe » est le mot du mystère (le cinquième signe) ; on le garde pour lui.

**C41. `one-gesture`**
- Actuel : « Un geste, une fois, jusqu'au bout. » / "One gesture, once, all the way."
- Proposé : « Une seule fois, mais jusqu'au bout. » / "Only once, but all the way through."
- Raison : « geste » est déjà dans le nom de ce talent et dans celui de Premier geste.

**C42. `artillery`** (facultatif)
- Actuel : « De si loin, le bruit arrive après le coup. » / "From that far, the sound comes after the blow."
- Proposé : « De si loin, ils ne voient pas d'où ça vient. » / "From that far, they never see where it came from."
- Raison : un son qui arrive après l'impact, c'est l'image d'un obus, pas d'une catapulte ni d'un sort.

**C43. `banner`**
- Actuel : « On se bat mieux quand on voit la couleur à côté de soi. » / "One fights better seeing the colour beside one."
- Proposé : « On se bat mieux quand ses couleurs flottent tout près. » / "You fight better with your colours flying close by."
- Raison : « la couleur » est vague ; l'anglais n'est pas idiomatique.

**C44. `relief`** (facultatif)
- Actuel : « Au dernier moment, une main se tend. » / "At the last moment, a hand reaches out."
- Proposé : « Au dernier souffle, une épaule se présente. » / "At the last gasp, a shoulder is there."
- Raison : « main tendue » est le nom de la porte du Soutien, et la branche a déjà trois « main » (Main tendue, Double main, Longue main).

**Lignes où seul l'anglais change :**

| # | id | Actuel en | Proposé en | Raison |
|---|---|---|---|---|
| C45 | `chilling` | Wind runs over water, and the water sets. | Wind skims the water, and the water sets hard. | "sets" seul évoque une gelée |
| C46 | `fogging` | Between fire and water hangs a mist one gets lost in. | Between fire and water hangs a mist to lose your way in. | "one gets" est guindé |
| C47 | `heart-earth` | One ground under one's feet, and one falls no more. | One ground beneath your feet, and you never fall. | trois "one" en une ligne |
| C48 | `strong-signature` | One knows the hand by the trace it leaves. | You can tell a hand by the trace it leaves. | guindé |
| C49 | `lasting-mark` | A trace that does not wash off at once. | A trace that won't wash off in a hurry. | plat |
| C50 | `up-close` | Close enough to see the eyes. | Close enough to see the whites of their eyes. | l'expression anglaise existe, autant la prendre |
| C51 | `heavy-push` | One does not push a wall: one makes it give way. | You don't push a wall: you make it give way. | guindé |
| C52 | `clear-line` | Straight ahead, nothing stands between. | Straight ahead, nothing stands in the way. | phrase incomplète |
| C53 | `backed` | Back to the wall, one gives no more ground. | Back to the wall, there's no ground left to give. | guindé |
| C54 | `scorched-earth` | Where you marked the earth, it remembers. | Where you have marked the ground, it remembers. | le temps du verbe |
| C55 | `heavy-blows` | Take one's time, strike once. | Take your time, strike once. | guindé |
| C56 | `short-cooldown` | More often, less hard. | Sooner, but softer. | "less hard" n'est pas idiomatique |
| C57 | `reserve` | What one does not spend today serves tomorrow. | What you don't spend today will serve tomorrow. | guindé |

---

## 5. Les 56 contreparties (`counterpart`)

Le fond est juste et honnête. Les corrections portent sur des mots de développeur (« prend la Terre »), sur des ellipses (« font 10 % de moins ») et sur l'anglais "hit X% softer", qui revient partout.

**C58 à C63. Les six paires** (`bogging`, `chilling`, `cracking`, `electrifying`, `fogging`, `blinding`)
- Actuel (Enlisement) : « Aucun sort : la paire ne donne que sa réaction, et prend la Terre et l'Eau. » / "No spell: the pair only gives its reaction, and takes Earth and Water."
- Proposé (Enlisement) : « Ni sort ni bonus, seulement la réaction ; la Terre et l'Eau ne peuvent plus entrer dans une autre paire. » / "No spell, no bonus, only the reaction; Earth and Water can no longer go into another pair."
- Même modèle pour les cinq autres, avec leurs éléments : Eau et Air ; **Terre et Feu** (aujourd'hui « le Feu et la Terre », dans l'ordre inverse de la description et de `talents-arbre.md` §6.2) ; Feu et Air ; Feu et Eau ; Terre et Air.
- Raison : « prend la Terre » est un mot du moteur ; le joueur doit lire la règle C2 (deux paires sans élément commun).

**C64 à C67. Les quatre Cœurs** (en seul) : "Your other element spells hit 5% softer." → "Your other elemental spells deal 5% less damage." Raison : "hit softer" n'est pas idiomatique pour des dégâts.

**C68. `transmute-fire-lightning`**
- Actuel : « Tu perds le Feu : ni son empreinte, ni ses réactions, ni ses bonus ; Électrisé ne se déclenche plus, sauf avec Prisme. » / "You lose Fire: neither its mark, nor its reactions, nor its bonuses; Electrified no longer goes off, except with Prism."
- Proposé : « Tu perds le Feu, son empreinte et ses réactions, sans aucun bonus de dégâts ; Électrisé ne se déclenche plus, sauf avec Prisme. » / "You lose Fire, its mark and its reactions, with no damage bonus; Electrified no longer goes off unless you have Prism."
- Raison : « ni ses bonus » laisse croire qu'on perd des bonus du Feu ; la description dit seulement que la Transmutation n'en ajoute aucun.

**C69. `transmute-air-lightning`** : même modèle (« Tu perds l'Air, son empreinte et ses réactions, sans aucun bonus de dégâts ; Électrisé ne se déclenche plus, même avec Prisme. » / "You lose Air, its mark and its reactions, with no damage bonus; Electrified no longer goes off, even with Prism."). Raison : la même que C68.

**C70. `prism`**
- Actuel : « Tes coups de paire ne déclenchent toujours aucune réaction. » / "Your pair's hits still set off no reaction."
- Proposé : « Ces coups posent une empreinte, mais n'en déclenchent aucune. » / "Those hits leave a mark but never set one off."
- Raison : « toujours » se lit « jamais, en aucun cas » ou « pas plus qu'avant » ; "your pair's hits" ne dit pas la même chose que la description ("your pair hits").

**C71 à C75. Les ellipses « font X % de moins »** (`lingering-reaction`, `wave`, `remanence`, `helping-hand`, `devotion`) : « de moins » devient « de dégâts en moins ».
- `lingering-reaction` : « Tes coups qui déclenchent une réaction font 10 % de dégâts en moins. » (l'EN est bon).
- `wave` : « Tes zones font 15 % de dégâts en moins. » / "Your areas deal 15% less damage."
- `remanence` : « Tes coups font 5 % de dégâts en moins ; un seul glyphe à la fois. » / "Your hits deal 5% less damage; one glyph at a time."
- `helping-hand` : « Tes coups font 5 % de dégâts en moins ; un soutien par tour. » / "Your hits deal 5% less damage; one support a turn."
- `devotion` : « Tes coups font 20 % de dégâts en moins. » / "Your hits deal 20% less damage."
- Raison : une contrepartie se lit seule, sans la description : elle doit dire de quoi on perd X %.

**C76. `artillery`**
- Actuel : « Plus rien à 1 ou 2 cases. » / "Nothing at all at 1 or 2 cells."
- Proposé : « Tes sorts de dégâts ne touchent plus rien à 1 ou 2 cases. » / "Your damage spells can no longer hit anything 1 or 2 cells away."
- Raison : la phrase se lit seule, et les sorts de soutien ne sont pas touchés.

**C77. `dancing-step`** (en seul) : "The bonus falls back to nothing after each spell." → "The bonus resets to zero after each spell." Raison : naturel.

**C78. `seal`**
- Actuel : « Aucun dégât ; un sort de plus prend une place de deck. » / "No damage; one more spell takes a place of the deck."
- Proposé : « Aucun dégât, et chaque glyphe mis dans le deck y prend une place. » / "No damage, and each glyph you put in your deck takes up a slot."
- Raison : le talent donne quatre sorts, pas un ; "a place of the deck" est fautif.

**C79. `decoy-talent`**
- Actuel : « Une place de deck. » / "A place of the deck."
- Proposé : « Il prend une place dans ton deck. » / "It takes up a slot in your deck."
- Raison : la phrase n'en est pas une ; l'anglais est fautif.

**C80 et C81. `heavy-blows`, `light-blows`** (en seul) : "Your spells of 3 AP or less hit 15% softer." → "Your spells of 3 AP or less deal 15% less damage." (et l'inverse pour 4 PA ou plus). Raison : C64.

**C82. `first-gesture`** (en seul) : "The turn's next spells hit 5% softer." → "Your later spells that turn deal 5% less damage." Raison : C64.

**C83. `long-breath`** : « −1 PA tes 3 premiers tours. » → « −1 PA pendant tes 3 premiers tours. » / "−1 AP your first 3 turns." → "−1 AP on your first 3 turns." Raison : une préposition manque.

**C84. `relief`** (en seul) : "Your next turn, 2 AP less." → "2 AP fewer on your next turn." Raison : idiomatique.

---

## 6. Les autres textes `tree.*`

**C85. `tree.trunk`** (en seul) : "Trunk, no point" → "Trunk, free". En français, « Tronc, sans point » peut devenir « Tronc, gratuit » pour que les deux langues disent la même chose. Raison : "no point" veut dire « ça ne sert à rien ».

**C86. `tree.trunk.detail`** (en seul) : "The trunk lights up by itself, with no point: {0}." → "The trunk lights up on its own, without spending a point: {0}." Raison : la même que C85.

**C87. `tree.closed`** (en seul) : "…its trunk lights up by itself." → "…its trunk lights up on its own." Raison : naturel, et cohérent avec C19.

**C88. `tree.left`** (en seul) : "{0} point(s) to spend of {1}" → "{0} of {1} point(s) left to spend". Raison : l'ordre des mots est français.

**C89. `tree.styles`** et **C90. `tree.term.key.def`** (en seul) : "one key a style" → "one key per style". Raison : C28.

**C91. `tree.fold.detail`** (en seul) : "click to deploy this style" → "click to expand this style". Raison : "deploy" se dit de troupes.

**C92. `tree.plan.on`** (en seul) : "Planning at level {0}: the nodes dotted in pistachio are a plan, nothing is saved." → "Planning at level {0}: nodes ringed in dotted pistachio are only a plan; nothing is saved." Raison : le sens.

**C93. `tree.suggested.loaded`** et **C94. `tree.code.loaded`** (en seul) : "loaded in the draft" → "loaded as a draft". Raison : naturel.

**C95. `tree.code.OldVersion`** (en seul) : "This code comes from an older tree ({0})" → "This code comes from an older version of the tree ({0})". Raison : fidèle au français.

**C96. `tree.code.Level`** (en seul) : "This build asks for level {0}." → "This build needs level {0}." Raison : "asks for" est un calque.

**C97. `tree.before-after`** (en seul) : "On a typical hit (2 cells away, without moving, a first spell of 3 AP)" → "On a typical hit (2 cells away, standing still, first spell of 3 AP)". Raison : plus court ; la ligne est longue pour le panneau.

**C98. `tree.counterpart`** (en seul) : "In exchange: {0}" → "In return: {0}". Raison : l'usage britannique ; « en échange » reste en FR.

**C99. `tree.title.converted`** (FR)
- Actuel : « {0} convertie »
- Proposé : « Conversion : {0} » (EN "converted {0}" → "{0}, converted", ou "Conversion: {0}" pour suivre le FR)
- Raison : l'accord n'est juste aujourd'hui que parce que la Foudre est féminine ; « Magma convertie » et « Sable convertie » seront fautifs quand ces paires arriveront.

**C100. `tree.summary.ceiling`** (en seul) : "…beyond +{1}%, the rest is lost." → "…anything above +{1}% is lost." Raison : naturel.

**C101. `tree.advice`**
- Actuel : « Conseil : {0} et {1}, tes caractéristiques les plus hautes, vont à {2}. » / "Advice: {0} and {1}, your highest characteristics, go with {2}."
- Proposé : « Conseil : {0} et {1}, tes caractéristiques les plus hautes, désignent {2}. » / "Tip: {0} and {1}, your highest characteristics, point to {2}."
- Raison : « vont à » se lit comme « sont versées à ».

**C102. `tree.help.Water`** (en seul) : "+5% resistance an AP (20 at most)" → "+5% resistance per AP (20 at most)". Raison : C28.

**C103. `tree.group`** (en seul) : "For playing together." → "Made for group play." Raison : la même que C25.

**C104. `tree.term.reaction.def`** (FR)
- Actuel : « frapper une empreinte d'un autre élément de la paire déclenche son état »
- Proposé : « frapper une empreinte avec l'autre élément de la paire déclenche son état »
- Raison : la phrase actuelle dit « une empreinte d'un autre élément », ce qui est faux ; l'EN est juste.

**C105. `tree.switch`, `tree.term.switch`, `tree.term.switch.def`** (en seul) : "Big switch" → "Great shift". Raison : "big switch" fait penser à un interrupteur électrique ; « bascule », c'est le poids qui change de côté.

**C106. `tree.tuto.3`** (en seul) : "Hover a node to learn it all; the legend is behind "?"." → "Hover over a node for every detail; the legend is behind "?"." Raison : naturel.

**C107. `tree.choice.transmute`** (en seul) : "Transmutation (one of)" → "Transmutation (pick one)". Raison : "one of" est incomplet.

**C108. `tree.state.Level`**
- Actuel : « hors niveau » / "out of level"
- Proposé : « niveau trop bas » / "level too low"
- Raison : le joueur comprend du premier coup.

**C109. `tree.coming`** (en seul) : "to come (another pair)" → "coming later (another pair)". Raison : naturel.

**C110. `tree.can`** (en seul) : "Available: a click or Enter takes it." → "Available: click or press Enter to take it." Raison : naturel.

**C111. `tree.confirm`** (en seul) : "Giving {0} back gives back too: {1}." → "Giving back {0} also gives back: {1}." Raison : l'ordre des mots.

---

## 7. Ce qui est bon tel quel

- **Edmée :** son nom, son métier, sa place sur la place de Clairval, les quatre réponses existantes et le prix annoncé.
- **Les guildes :** `guild.title`, `room`, les trois rangs (Meneur, Officier, Membre : masculins génériques acceptables pour des titres), les colonnes sauf « où », les durées « il y a », `online`/`offline`, `invite`, `invite-into`, `leave`, `promote`, `join`, `decline`, `found.title`, `found`, `cancel`, `left`, et tout le français non cité au §2.3.
- **Les couches :** « Fondations », « Élément pur », « Styles ».
- **Les bandeaux :** le français de Foundations, Melee, Distance, Support, Tempo, Alchemy, Key et Choice ; l'anglais de Distance et de Tempo.
- **Les lignes d'ambiance**, en FR et en EN : `bogging`, `cracking`, `electrifying`, `blinding`, `heart-fire`, `heart-air`, `transmute-fire-lightning`, `transmute-air-lightning`, `prism` (« Dans la lumière la plus blanche, une couleur reste » : le parent survit dans le composé, c'est exactement le canon), `catalysis`, `lingering-reaction`, `wave`, `shared-mark`, `firm-footing`, `tight-zone`, `side-step`, `relay-talent`, `dancing-step`, `remanence`, `decoy-talent`, `light-blows`, `first-gesture`, `tight-deck`, `thrift`, `helping-hand`, `double-hand`, `shared-momentum`, `long-hand`. Le français seul pour les treize lignes de la table C45 à C57.
- **Les contreparties :** `strong-signature`, `convert-lightning`, `lasting-mark` (voir §8.6), `catalysis`, `shared-mark`, `virtuoso`, `up-close`, `firm-footing`, `heavy-push`, `close-quarters`, `long-sight`, `tight-zone`, `clear-line`, `side-step`, `backed`, `relay-talent`, `scorched-earth`, `nomad`, `echo`, `short-cooldown`, `reserve`, `tight-deck`, `one-gesture`, `thrift`, `banner`, `double-hand`, `shared-momentum`, `long-hand`, `harmony`, et le français de celles dont seul l'anglais change.
- **Les textes `tree.*`** non cités au §6 : le titre, les boutons, le glossaire (sauf C104), les trois bulles du tutoriel en français, les états, les messages d'annulation, les raccourcis, `tree.helps` et les aides par élément.

---

## 8. Signalements hors du périmètre (rien n'est corrigé)

1. **Les noms Harmonie et Sceau** sont sur la liste des noms interdits de `talents-canon.md` §5 (proposée par moi le 23 octobre, et pas encore validée par Charles). Voici des remplacements **[P]** : **Harmonie → Abri / Shelter** (avec la ligne C31) ; **Sceau → Marque au sol / Ground Mark**, ou **Glyphe-témoin / Tell-tale Glyph**. Les quatre sorts « Sceau de… » suivraient. Le « sceau » d'Edmée (C1) est un objet ordinaire, et il peut rester.
2. **L'Écho de Terrain** (G9) et les échos des ennemis tombés de la future spécialisation Nécromancien : `talents-arbre.md` §9.6 demande déjà deux noms différents. Je garde « Écho » pour le talent et je proposerai un autre mot pour la spécialisation.
3. **« Fureur d'orage »**, le jumeau de Foudre de l'Ouragan de la Garde (`textes-v9.md`), emploie un mot de la colère pour la Garde, ce que `talents-canon.md` §3.2 et §5 excluent (au chapitre 2, la colère est un symptôme). Je propose **« Œil du grain » / "Squall's Eye"** [P], à reporter avec la relecture de `textes-v9.md`.
4. **Le tutoiement et le vouvoiement :** l'arbre et les talents tutoient (« tes nœuds », « enregistre »), le reste de l'interface vouvoie (`guild.*`, `life.low`, `waystone.*`). Les deux coexistaient déjà avant S149. Je ne corrige rien : c'est à Charles de choisir une règle (ma valeur par défaut : le vouvoiement pour le système, le tutoiement seulement dans les PNJ qui tutoient).
5. **Les calques anglais dans les descriptions des talents** (hors de la commande, mais du même auteur) : "Asks for…" (Transmutations, Signature appuyée, Prisme, Conversion) devrait être "Needs…" ; "hit X% softer" devrait être "deal X% less damage" ; "one's deck", "one's spells" devraient être "your deck", "your spells". Il y en a une quinzaine.
6. **Empreinte tenace n'a pas de contrepartie** (« Aucune : un nœud de passage vers les clés »). C'est honnête, mais cela va contre le principe « chaque gain a un prix » (`talents-arbre.md` §2.1, point 4). C'est au game designer de trancher, pas à moi.
7. **Le prix de la guilde est écrit en dur** dans le dialogue d'Edmée (« mille Éclats »), alors que `guild.none` et `guild.found.title` le lisent dans `GuildRules`. Si le prix change, Edmée mentira. C'est pour le dev : un test qui compare les deux, ou un `{0}` dans les dialogues.
8. **Trois « main » dans le Soutien** (Main tendue, Double main, Longue main) et **deux « Élan »** (le sort de la Garde et Élan partagé) : ce n'est pas faux, mais à surveiller quand les spécialisations arriveront.
9. **Le genre de « glyphe »** : les données disent « un glyphe » (c'est le bon usage), et `talents-arbre.md` dit « une glyphe ». Il faut garder le masculin partout dans le jeu.

---

## 9. Questions pour Charles

| # | Question | Ma valeur par défaut |
|---|---|---|
| 1 | La couche « Fusion » devient « Tresse » / "Braid" (C17, C22) ? | **Oui** : le canon le demande |
| 2 | La couche « Affinités » devient « Paires » (C18) ? | **Oui**, pour un seul nom par couche ; « Affinités » reste acceptable |
| 3 | Harmonie → Abri, Sceau → Marque au sol (§8.1) ? | **Oui** ; la liste des noms interdits de `talents-canon.md` §5 est à valider |
| 4 | Edmée cite le règlement et parle d'Odalric (C3 à C5) ; la salle des fêtes de Clairval existe [P] ? | **Oui** |
| 5 | « Mairie » ou « maison commune » ? | **Mairie** |
| 6 | Le système vouvoie, et l'arbre tutoie ; faut-il une seule règle (§8.4) ? | **Vouvoiement pour le système**, à décider avant la prochaine passe de textes |
