# Osmose : les textes des succès du sprint 159 (D84)

> Scénariste, 28 octobre 2026. Ces textes accompagnent `succes-recompenses-s159.md` (§2 et §3).
> Lu avant d'écrire : `data/achievements.json` (63 succès), `data/quests.json` (les quêtes visées, leurs donneurs et leurs répliques), `data/dungeons.json`, `data/towns/*.json` (noms des zones), `src/Rpg.Client/Texts.cs` (groupes et phrases de la fenêtre des succès).
> `achievements.json` n'est pas modifié ici : le dev reporte ces textes.

## Principes suivis

- **Les noms viennent des quêtes elles-mêmes.** Chaque nom reprend une image ou une réplique que le joueur lit pendant la suite (« Les haches chantent », « Nous tiendrons »). Rien n'y dépasse ce que les quêtes disent déjà. Les noms restent vagues sur la fin d'une suite, parce qu'une tuile verrouillée est visible avant qu'on la joue : « Tout fuit à la fois » plutôt que le nom du Disjoint.
- **Pas d'accord en genre.** Le personnage peut être un homme ou une femme : aucun nom ni aucune phrase n'accorde un participe au joueur (« Clairval vous adopte », pas « Adopté par Clairval »).
- **Les phrases désignent les donneurs.** Chaque suite secondaire n'a qu'un seul donneur : Mathurin, Barnabé, Liane, Églantine ou Berthe. Si on ajoute plus tard une quête secondaire donnée par un autre PNJ, il faudra revoir la phrase. Le test du §2 (chaque quête rangée dans exactement un succès) le signalera.
- **Un rappel discret.** « Le pain rompu en cinq » et « Relier sans mélanger » reprennent deux phrases déjà présentes dans les quêtes. C'est le niveau d'indice voulu par la bible pour la Saga I : rien n'est expliqué.

## 1. Les 19 nouveaux succès

| id | nom fr | nom en | phrase fr | phrase en |
|---|---|---|---|---|
| clairval-first-steps | Du grenier à la palissade | From Loft to Palisade | Terminez vos premières quêtes à Clairval, de l'accueil d'Aubin à l'étal de Rose. | Complete your first quests in Clairval, from Aubin's welcome to Rose's stall. |
| clairval-four-trades | Quatre poinçons | Four Makers' Marks | Terminez les quatre premières commandes de la rue des artisans. | Complete the first four commissions on Craftsmen's Row. |
| clairval-story | La page grattée | The Scraped Page | Terminez « La pierre qui murmurait » et « Ce que le registre tait ». | Complete "The Whispering Stone" and "What the Registry Leaves Out". |
| clairval-all | Clairval vous adopte | Clairval Takes You In | Obtenez les succès suivants. | Earn the following achievements. |
| woods-main | Ce qui dort sous le bois | What Sleeps Beneath the Woods | Terminez les quêtes principales du Bois de Clairval. | Complete the main quests in Clairval Woods. |
| woods-side | Les haches chantent | The Axes Sing Again | Terminez les quêtes secondaires de Mathurin, au Bois de Clairval. | Complete Mathurin's side quests in Clairval Woods. |
| woods-all | De la lisière à la bauge | From the Edge to the Wallow | Obtenez les succès suivants. | Earn the following achievements. |
| mouldy-main | Relier sans mélanger | Linked, Not Mixed | Terminez les quêtes principales du Bois moisi. | Complete the main quests in the Mouldy Woods. |
| mouldy-side | Les genoux de Barnabé | Barnabé's Bad Knees | Terminez les quêtes secondaires de Barnabé, l'ermite du Bois moisi. | Complete the side quests of Barnabé, the hermit of the Mouldy Woods. |
| mouldy-all | Ça colle aux bottes | It Sticks to Your Boots | Obtenez les succès suivants. | Earn the following achievements. |
| lake-main | Sur le même air | To the Same Tune | Terminez les quêtes principales du Lac des Joncs. | Complete the main quests at Reed Lake. |
| lake-side | Plus de mailles que de trous | More Mesh Than Holes | Terminez les quêtes secondaires de Liane, la passeuse du Lac des Joncs. | Complete the side quests of Liane, the ferrywoman of Reed Lake. |
| lake-all | Faux, mais vrai | Off-Key, but True | Obtenez les succès suivants. | Earn the following achievements. |
| fields-main | Tout fuit à la fois | Everything Flees at Once | Terminez les quêtes principales des Champs de Clairval. | Complete the main quests in Clairval Fields. |
| fields-side | Le pain rompu en cinq | Bread Broken in Five | Terminez les quêtes secondaires d'Églantine, aux Champs de Clairval. | Complete Églantine's side quests in Clairval Fields. |
| fields-all | Après la moisson | After the Harvest | Obtenez les succès suivants. | Earn the following achievements. |
| ancestors-main | La route continue | The Road Carries On | Terminez les quêtes principales de Quatre-Chemins et de la Forêt des Aïeux. | Complete the main quests of Four Ways and the Forest of the Ancestors. |
| ancestors-side | Ceux qui descendent | Those Who Go Down | Terminez les quêtes secondaires de Berthe, au camp des mineurs. | Complete Berthe's side quests at the miners' camp. |
| ancestors-all | Nous tiendrons | We Shall Hold | Obtenez les succès suivants. | Earn the following achievements. |

D'où viennent les noms (pour la relecture) :

| id | source dans `quests.json` |
|---|---|
| clairval-first-steps | les rats du grenier (`cellar-rats`) et les pieux de la palissade (`palisade-stakes`) |
| clairval-four-trades | chaque artisan marque la pièce du joueur : le poinçon d'Anselme, le trait gravé d'Eudes, le clou d'Apolline, les points repris de Perrine |
| clairval-story | Odalric : « la page d'avant a été grattée » |
| clairval-all | le joueur arrive en nouveau venu ; à la fin du village, il en fait partie |
| woods-main | Ysaline : « Si quelque chose dort sous ce bois » (`the-root-circle`) |
| woods-side | Mathurin : « Les haches vont pouvoir chanter de nouveau » (`the-pack`) |
| woods-all | de « La lisière » (`edge-of-the-woods`) à la bauge du Roi sanglier |
| mouldy-main | Ysaline : « Ils relient sans mélanger » (`what-grows-between`) |
| mouldy-side | Barnabé : « mes genoux ne plient plus » (`spores-for-the-stall`) |
| mouldy-all | Mathurin : « ça sent la cave et ça colle aux bottes » (`four-signs`) |
| lake-main | Ysaline : « trois chansons sur le même air » (`the-unlearned-lullaby`) |
| lake-side | Liane : « Mes filets ont plus de trous que de mailles » (`lianes-nets`), retourné |
| lake-all | Barnabé : « elle chante faux, mais elle chante vrai » (`the-cracked-seal`) |
| fields-main | Ysaline : « les bêtes du pays fuient toutes à la fois » (`the-disjoint`) |
| fields-side | Églantine : « On le rompt en cinq morceaux » (`harvest-bread`) |
| fields-all | la moisson d'Églantine, qui clôt les Champs |
| ancestors-main | Anatole : « c'est que la route continue » (`roots-of-the-mother-tree`) |
| ancestors-side | Berthe : « c'est ce que je donne à ceux qui descendent » (`antler-wedges`) |
| ancestors-all | Bérold : « Nous tiendrons. », les premiers mots des serments (`mossy-keepers`) |

## 2. Les six groupes de zone

Ce sont les noms que le jeu affiche déjà sur les cartes (`data/towns/*.json`, champ `name`) et dans les succès des pierres de passage. Clés à ajouter dans `Texts.cs` : `achievements.group.<id>`.

| groupe | fr | en |
|---|---|---|
| clairval | Clairval | Clairval |
| woods | Bois de Clairval | Clairval Woods |
| mouldy | Bois moisi | Mouldy Woods |
| lake | Lac des Joncs | Reed Lake |
| fields | Champs de Clairval | Clairval Fields |
| ancestors | Forêt des Aïeux | Forest of the Ancestors |

Remarque : `ancestors-main` compte deux quêtes de Clairval (`four-trails`) et de Quatre-Chemins (`where-roads-part`), rangées dans le groupe de la Forêt des Aïeux (§2 du document de game design). La phrase de ce succès nomme donc Quatre-Chemins, pour que le joueur ne cherche pas ces deux quêtes dans la forêt.

## 3. Les 10 succès de donjon (but `Dungeon`)

Les noms ne changent pas : seule la phrase change. J'ai gardé la forme courte. La version longue proposée au §3 (« … : son chef vous attend au fond de la 3e salle ») explique une règle du donjon et ne ressemble à aucune autre phrase du fichier. La liste des objectifs suffit à montrer le chef.

| id | phrase fr | phrase en |
|---|---|---|
| dungeon-boar-wallow | Finissez la Bauge du Roi sanglier. | Finish the Boar King's Wallow. |
| dungeon-wolf-den | Finissez la Tanière de la meute. | Finish the Pack's Den. |
| dungeon-mushroom-cellar | Finissez la Champignonnière. | Finish the Mushroom Cellar. |
| dungeon-zombie-crypt | Finissez la Crypte oubliée. | Finish the Forgotten Crypt. |
| dungeon-frog-grotto | Finissez la Grotte du Roi grenouille. | Finish the Frog King's Grotto. |
| dungeon-crab-reef | Finissez le Récif des pinces. | Finish the Pincer Reef. |
| dungeon-royal-hive | Finissez la Ruche royale. | Finish the Royal Hive. |
| dungeon-mole-burrows | Finissez les Galeries des taupes. | Finish the Mole Burrows. |
| dungeon-bellowing-combe | Finissez la Combe du brame. | Finish the Bellowing Combe. |
| dungeon-mother-tree-roots | Finissez les Racines de l'Arbre-Mère. | Finish the Mother Tree's Roots. |

**À changer aussi (non demandé, mais lié) :** `dungeons-all` dit encore « Vainquez les chefs des 10 donjons. ». Le §3 lui donne le même prédicat que les dix autres. Je propose :

| id | phrase fr | phrase en |
|---|---|---|
| dungeons-all | Finissez les 10 donjons. | Finish all 10 dungeons. |

Le nom « Dix chefs tombés » et le titre « Fléau des donjons » restent justes.

## 4. Les quatre phrases d'interface

| clé | actuelle | verdict |
|---|---|---|
| achievements.title-locked | Obtenez ce succès pour porter son titre. / Earn this achievement to wear its title. | Gardée telle quelle. |
| achievements.pick | Choisissez un succès pour voir ses objectifs. / Pick an achievement to see its objectives. | Gardée telle quelle. |
| achievements.empty.search | Aucun succès ne correspond à « {0} ». / No achievement matches “{0}”. | Gardée telle quelle. Les guillemets anglais “ ” sont ceux de `achievements.reward.title` et `achievements.worn` : la fenêtre reste cohérente. |
| achievements.empty.all-earned | Tout est déverrouillé ici. Décochez la case pour revoir ces succès. / Everything here is unlocked. Untick the box to see them again. | **Une retouche proposée** : « la case » ne dit pas laquelle. Je propose de nommer la case par son libellé (`achievements.hide-earned`) : **« Tout est déverrouillé ici. Décochez « Masquer les succès déverrouillés » pour les revoir. »** / **"Everything here is unlocked. Untick “Hide unlocked achievements” to see them again."** Si la phrase est jugée trop longue à 375 px, la version actuelle reste correcte. |

## 5. Incohérences signalées (non corrigées)

1. **Garance et la porte.** Le brief du scénariste place Garance « près de la porte est ». Or `quests.json` (`welcome`) l'installe à la cour d'armes de la **porte de l'Ouest**, et la porte de l'Est n'est ouverte qu'au niveau 30, par Hortense (`where-roads-part`). Les données ont raison : c'est le brief qu'il faut mettre à jour.
2. **« Déverrouillé » et « Obtenu ».** La tuile affiche « Obtenu » (`achievements.unlocked`), mais la case et le message vide parlent de « déverrouillés ». Les deux mots se comprennent, mais il vaudrait mieux n'en garder qu'un. Je n'y touche pas : c'est un choix d'interface.
