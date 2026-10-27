# Osmose : les textes du tableau des contrats (sprint 153)

> Proposition du scénariste pour S153 (`suite-apres-d69.md` §12.4), **à relire par Charles**. **[P]** = proposition, absente de la bible et du jeu. Rien ici ne touche au mystère : les avis sont la vie ordinaire des deux places.
> **Convention des avis :** pas de « tu » ni de « vous » (un avis s'adresse à n'importe qui), aucun accord sur le héros ni sur `{target}`, jamais « de {target} » (élision impossible devant une voyelle : « de abeilles ») ni « un/une {target} » (genre inconnu).

## 1. Le panneau

- **id [P] :** `notice-board` (aspect `notice-board`, nouveau), un sur `clairval` (place de la Pierre) et un sur `fourways-0-1` (place du relais).
- **Nom :** Le tableau d'avis / The notice board
- **Qui affiche [P] :**
  - à Clairval, **Edmée**, la greffière de la mairie : elle punaise les demandes des habitants et les tamponne ;
  - à Quatre-Chemins, **Josette**, l'aubergiste : le tableau est cloué près de la porte du relais, et elle y épingle ce que demandent les voyageurs.
- **Ligne d'examen, Clairval :**
  - fr : « Le tableau de la mairie, sous son petit auvent. Edmée y punaise chaque matin les demandes du village, tamponnées et datées ; les vieilles jaunissent en dessous. »
  - en : "The town hall's board, under its little roof. Every morning Edmée pins up the village's requests, stamped and dated; the old ones yellow underneath."
- **Ligne d'examen, Quatre-Chemins :**
  - fr : « Un tableau cloué près de la porte du relais. Josette y épingle ce que demandent les voyageurs, et ses propres demandes, écrites plus gros. »
  - en : "A board nailed up by the inn door. Josette pins up what travellers ask for, and her own requests too, in bigger letters."

## 2. Les six modèles d'avis

| Type | Clé | fr | en |
|---|---|---|---|
| hunt | `contract.hunt.title` | Chasse aux {target} | Hunt: {target} |
| hunt | `contract.hunt.text` | Les {target} s'enhardissent et rôdent près des chemins. Prime à qui en vaincra {count}. Secteur : {zone}. | The {target} are growing bold and prowling near the paths. A bounty to whoever sees off {count} of them. Area: {zone}. |
| gather | `contract.gather.title` | Commande : {count} {target} | Wanted: {count} {target} |
| gather | `contract.gather.text` | Les réserves baissent et les routes sont vides. Cherche {count} {target}, au plus vite. Payé comptant, sans marchander. | Stores are running low and the roads are empty. Wanted: {count} {target}, soon as can be. Paid on the nail, no haggling. |
| craft | `contract.craft.title` | Sur commande : {target} | Made to order: {target} |
| craft | `contract.craft.text` | Cherche une main habile pour fabriquer ceci : {target}. Travail vérifié avant paiement, comme il se doit. | Wanted: a skilled pair of hands to make this: {target}. Work checked before payment, as is only right. |
| dungeon | `contract.dungeon.title` | Reconnaissance : {target} | Scouting: {target} |
| dungeon | `contract.dungeon.text` | Ce qu'il y a après l'entrée, plus personne n'ose le dire. Franchir la deuxième salle, puis revenir raconter. Lieu : {target}. | Nobody dares say any more what lies past the entrance. Get through the second room, then come back and tell. Place: {target}. |
| horde | `contract.horde.title` | Horde en vue : {target} | Horde sighted: {target} |
| horde | `contract.horde.text` | Les {target} font bande, six ou plus à la fois. Seul, c'est folie : on y va à plusieurs, ou on n'y va pas. | The {target} are banding together, six or more at a time. Alone, it's madness: go in numbers, or don't go at all. |
| boss | `contract.boss.title` | Prime au chef : {target} | Bounty on the leader: {target} |
| boss | `contract.boss.text` | Tout ce qui sort de là obéit à quelque chose, tout au fond. Il faut l'abattre, et pas seul. Lieu : {target}. | Whatever comes out of there answers to something deep inside. It must be brought down, and not alone. Place: {target}. |

Longueurs des modèles bruts : titres de 14 à 28 caractères, textes de 99 à 126 caractères. Le plafond de 140 peut être dépassé une fois `{target}` rempli par un long nom de donjon (« Grotte du Roi grenouille ») : à vérifier par un test sur les noms réels.

## 3. Les libellés d'interface

| Clé | fr | en |
|---|---|---|
| `contracts.window` | Tableau d'avis | Notice Board |
| `contracts.tab` | Contrats | Contracts |
| `contracts.handIn` | Rendre | Hand in |
| `contracts.swapFree` | Changer cet avis (gratuit) | Swap this notice (free) |
| `contracts.swapPaid` | Changer cet avis ({price} Éclats) | Swap this notice ({price} Éclats) |
| `contracts.group` | Groupe | Group |
| `contracts.pending` | {n} avis en attente | Notices waiting: {n} |
| `contracts.empty` | Aucun avis pour l'instant : revenez demain. | No notices for now: come back tomorrow. |
| `contracts.done` | Contrat rempli : +{xp} XP, +{shards} Éclats. | Contract fulfilled: +{xp} XP, +{shards} Éclats. |
| `contracts.renewal` | Le tableau se renouvelle chaque jour à l'aube. | New notices go up every day at dawn. |

## 4. Signalements

1. **`contracts.pending` en anglais** : « {n} notices waiting » donnerait « 1 notices ». Je propose « Notices waiting: {n} », juste pour tout nombre (« avis » est invariable en français).
2. **La casse de `{target}`** : les avis mettent `{target}` en milieu de phrase (« Les {target} »). Si le jeu fournit « Sangliers » avec une majuscule, il faut le nom en minuscules pour hunt, horde et gather.
3. **Les noms de donjon et de zone** sont toujours placés après « Lieu : », « Secteur : » ou un titre à deux-points : ainsi « Bauge du Roi sanglier » ou « Bois de Clairval » n'ont pas besoin d'article.
4. **horde** n'utilise pas `{zone}` (la consigne ne la donne pas pour ce type) ; si le jeu la fournit, on peut ajouter « Secteur : {zone}. » (17 caractères).
5. **« À l'aube »** pour un renouvellement à 4 h (heure du serveur) : c'est une image, pas une heure exacte.
6. **Edmée et Josette comme afficheuses [P]** : aucune réplique n'est ajoutée à leurs dialogues ; une ligne sur le tableau pourra venir plus tard, sans être nécessaire.
7. **« Les routes sont vides »** (gather) reprend une anomalie déjà connue au niveau 1 (les routes fermées de Quatre-Chemins), sans rien expliquer.
