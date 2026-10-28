# S160 : la fenêtre de guilde (maquette)

Sources : `src/assets/tokens.css`, `src/Rpg.Client/UiStyle.cs`, `godot/OsmoseTheme.cs`, `docs/graphisme/s157-maquettes.md` (recette des icônes §1, cadre en relief §4.1, composants §4.3), `s158-succes.md` (§1 cadre commun, §6 variations), `godot/GuildPanel.cs`, `src/Rpg.Client/GuildPage.cs`, `src/Rpg.Core/Guilds.cs`, `GuildHall.cs`, `GuildEndpoints.cs`. D84, D54 : la capture Wakfu sert de référence d'ergonomie ; ni mise en page, ni asset, ni pictogramme repris (ici : pas de lieu de guilde, le niveau est un hexagone doré dessiné par nous, les actions d'un membre passent par un menu « ⋯ », le recrutement est un onglet).
Remplace `GuildPanel.cs` (840×580, cartes par membre, quatre icônes d'action par ligne).

**Point focal** : l'en-tête (blason, nom, hexagone doré du niveau). Dans l'onglet Membres, le second regard va aux points verts.

**Pistache en texte ?** La charte n'interdit le pistache en texte que sur fond clair ; mais la règle transverse S157 §4.3 de l'interface d'Osmose va plus loin : le pistache n'est jamais du texte (il marque bords, coches, remplissages et sélection). Les noms sont donc en `#ffffff` / `text-strong` ; l'état « connecté » est porté par le point vert `--osmose-air`, pas par la couleur du nom.

---

## 1. Cadre et en-tête (commun aux onglets, membre d'une guilde)

```
x: 0 16    120 132                         528 544                                 1024 1040
   +--------------------------------------------------------------------------------+  y 0
   | [guild24] Guilde (G)                                                     [ X ] |  TitleBar h 44
   +================================================================================+  y 44
   | +--------+  Les Veilleurs                       | +----------------------------+ |  y 56
   | |        |                                      | | MESSAGE DU MENEUR    [crayon]| |  y 56-80 nom 20 px
   | | BLASON |  /‾\                                  | | « Contrats de groupe ce    | |
   | |  80×80 | | 7 | [====== 620 / 1 050 ======   ] | |  soir à 21 h, venez       | |  y 84-128 hexagone 40×44,
   | |        |  \_/                                  | |  nombreux ! »              | |  barre h 16 à y 98-114
   | +--------+  Semaine [=====   ] 80 / 120  12 / 50 membres · niv. moyen 34       | |  y 136-156
   |  104×104                                         | +----------------------------+ |  y 160
   |                                                                                |  12
   | [Membres] [Coffre] [Journal] [Recrutement (3)]          [loupe] Rechercher... [x]|  y 172-216
   |--------------------------------------------------------------------------------|
   |  CONTENU DE L'ONGLET  1008 × 352  (y 216-568)                                  |
   |                                                                                |
   |--------------------------------------------------------------------------------|  y 572
   | [v] Afficher les membres déconnectés  [ ] Signaler leurs connexions  [Quitter…] |  y 580-624 pied (Membres)
   +--------------------------------------------------------------------------------+  y 640
```

- Fenêtre : `WindowPanel` 1040×640 centrée (à 1280×720 : marges 120 / 40), `WindowDrag.Make(panel, "guild")` gardé. `TitleBar` : icône `guild` (lorc/flying-flag, S157) 24 px, « Guilde » 18 px gras #ffffff, « (G) » 14 px `text-dim` ; `CloseButton` 44×44. Le nom de la guilde n'est plus dans le titre : il est dans l'en-tête.
- **Écu** (x 16-120, y 56-160) : nouvelle variation `CrestFrame` (PanelContainer, 9-patch §4.1 : corps `raised-top` #3a3a3d → `raised-bottom` #262628, contour 2 px `--osmose-ui-frame` #7a5434, reflet, ombre (0, 2) 0,45, rayon 8, marges 12) ; dedans `CrestView` 80×80 (inchangé). Infobulle : `guild.crest`.
- **Nom** (x 132-528, y 56-80) : 20 px gras #ffffff, une ligne, `TrimEllipsis` (20 caractères tiennent : ≈ 240 px).
- **Niveau** (y 84-128) : hexagone `LevelHex` 40×44 à x 132 (§5.3), chiffre 16 px gras `--osmose-ui-gold-text` #3a2410 centré ; écart 8 ; `ProgressBar` h 16, x 180-528 (348), y 98-114, texte dans la barre « 620 / 1 050 » (14 px gras blanc, contour 3 px noir) ; niveau 10 : variation `GoldBar` pleine, texte `guild.level.top`. Infobulle de la ligne entière : `guild.points.how` (existant) précédé de `guild.points` (« 620 / 1 050 points vers le niveau 8 »).
- **Ligne de la semaine** (y 136-156) : « Semaine » 14 px `text-dim`, écart 8, `ProgressBarCompact` 120×8 (remplissage progrès ; objectif atteint → remplissage doré), écart 8, « 80 / 120 » 14 px gras `--osmose-ui-text`. À droite (aligné x 528) : `guild.roster` « 12 / 50 membres · niveau moyen 34 » 14 px `--osmose-ui-text`, chiffres en #ffffff. Le niveau moyen compte tous les membres (déconnectés compris), arrondi à l'entier.
- **Message** (x 544-1024, y 56-160, 480×104) : `DetailPanel` (S158 §3.3) avec marges 12. `SectionLabel` « MESSAGE DU MENEUR » à (12, 12) ; texte 14 px `--osmose-ui-text` de y 40 à 94 (3 lignes de 18, retour à la ligne, `TrimEllipsis` à la 3e, texte entier en infobulle ; 200 caractères ≈ 3 lignes de 65). Sans message : `guild.message.none` 14 px `text-dim`.
  - **Crayon** (meneur seul) : variation `TreeToggle` réutilisée (44×44 transparent, survol `Hover` rayon 6), icône delapouite/pencil 20 px `text-dim` → `text-strong` au survol, à (424, 4) dans le panneau (coin haut droit). Infobulle `guild.edit` (existant). Ouvre l'éditeur (§1.1). Absent pour officiers et membres (pas grisé : rien à proposer).
- **Onglets** (y 172-216) : `TabBar` du thème S157 (h 44, côtés 16, écart 2, actif : haut 3 px pistache, texte #ffffff gras). Libellés : `guild.tab.members`, `guild.tab.chest`, `guild.tab.log`, `guild.tab.recruit`. « Recrutement » n'existe que pour le meneur et les officiers ; quand des candidatures attendent, badge S157 §3.3 (20×20, fond `--pistache`, chiffre 14 px gras `--chocolat`, rayon 10, bord 2 px #1f1f1f) collé à droite du libellé, écart 6. L'onglet ouvert est mémorisé par session ; à la première ouverture : Membres.
- **Recherche** (x 764-1024, y 172-216) : `SearchField` (S158) 260×44, indice `guild.search`, visible dans Membres seulement. Sans accents ni casse, sur le nom et la note, dès 1 caractère.
- Message d'erreur du serveur (`Tell`) : centré dans le pied, 14 px `--osmose-ui-close-hover-text` #f08070, effacé après 4 s (comme S158).

### 1.1 Éditeur du meneur (fenêtre modale 560×416, centrée sur la fenêtre de guilde)
```
   +----------------------------------------------------+  WindowPanel + TitleBar h 44 « Message, objectif et blason »
   | MESSAGE DU MENEUR                         142 / 200 |  SectionLabel + compteur 14 px text-dim (rouge #f08070 à 200)
   | +------------------------------------------------+ |
   | | TextEdit 528×88, retour à la ligne              | |  fond well, bord comme LineEdit
   | +------------------------------------------------+ |
   | OBJECTIF DE LA SEMAINE   [  120  ][-][+] points     |  SpinBox 136×44, pas 10, 1-5 000
   | BLASON                                              |
   | +------+  [Forme v] [Pictogramme v]                 |  aperçu CrestFrame 88×88 (CrestView 64)
   | |aperçu|  [Fond  v] [Motif       v]                 |  OptionButton 2×2, 208×44 chacun, écart 8
   | +------+                                            |
   |                       [ Annuler ]  [ Enregistrer ]  |  ButtonSecondary 120×44, ButtonPrimary 152×44
   +----------------------------------------------------+
```
- Un seul « Enregistrer » envoie le message et l'objectif puis le blason (deux requêtes existantes, `NoticePressed` puis `CrestPressed` si changé) ; fond = motif : « Enregistrer » désactivé, infobulle `guild.crest.same` (la règle `GuildCrests.WhyNot`). Échap / Annuler ferment sans rien envoyer. Voile #000000 à 50 % sous la modale.

---

## 2. Onglet Membres

```
x 16                                                                                1012|piste 12|1024
   | ● |cls| Nom ▲            | Note                    | Rang         |Niv.| Guilde | Succès |   |  y 216-260 en-têtes h 44
   |---+---+------------------+-------------------------+--------------+----+--------+--------+---|
   | ● |[G]| Ondine (vous)    | Toujours partante       | [couronne] Meneur | 100 |  2 340 |  1 240 | ⋯ |  y 260, lignes h 44
   | ● |[M]| Plop             | Dispo le soir           | [galon] Officier |  34 |    610 |    415 | ⋯ |
   | ○ |[S]| Plouf            | —                       | [soldat] Membre  |  12 |     40 |     90 | ⋯ |  déconnecté : estompé
   |  ...  7 lignes visibles (260-568), défilement vertical                                         |
```

### 2.1 Colonnes (largeur utile 996 = 1008 − piste 12 ; marges de ligne 8 à gauche et à droite)

| # | Colonne | Largeur | Contenu | Alignement | Tri (clic sur l'en-tête) |
|---|---|---|---|---|---|
| 1 | État | 32 | point 10×10 (§2.2) | centré | connectés d'abord |
| 2 | Classe | 44 | icône de classe 28×28 (§5.2) | centré | par nom de classe |
| 3 | Nom | flexible (216 à 1040 px de fenêtre, min 136) | 14 px gras ; « (vous) » 14 px `text-dim` | gauche | alphabétique |
| 4 | Note | 220 (min 140) | 14 px, une ligne, `TrimEllipsis` ; vide « — » `text-off` | gauche | alphabétique, vides en dernier |
| 5 | Rang | 136 | icône 20 + écart 6 + nom 14 px | gauche | meneur, officiers, membres |
| 6 | Niveau | 64 | 14 px gras | droite | numérique |
| 7 | Guilde (points apportés) | 104 | 14 px, milliers groupés (espace fine) | droite | numérique |
| 8 | Succès (points) | 96 | 14 px, milliers groupés | droite | numérique |
| 9 | Actions | 44 | bouton « ⋯ » (§2.4) | centré | — |

Somme : 8 + 32 + 44 + 216 + 220 + 136 + 64 + 104 + 96 + 44 + 8 = 972 + gouttières 8 × 3 entre Note/Rang, Rang/Niveau, Niveau/Guilde = 996.

- **En-têtes** : nouvelle variation `ColumnHeader` (Button plat, h 44, fond `--osmose-ui-tab` #252526 pour toute la rangée, filet bas 1 px `--osmose-ui-frame-dark`), libellé 14 px gras `text-dim` ; survol `text-strong` ; colonne triée : libellé #ffffff + flèche 8×5 (▲ croissant / ▼ décroissant, triangle SVG, `--chocolat-clair`) après le libellé, écart 4. Premier clic : croissant (décroissant pour Niveau, Guilde, Succès), second : inverse. Départage toujours par rang puis nom. Tri par défaut : `GuildRules.Ordered` (rang, puis connectés, puis nom) — aucune flèche affichée. En-têtes 1 et 9 sans libellé (État : infobulle `guild.col.status`). Libellés : `guild.col.class`, `.name`, `.note`, `.rank`, `.level`, `.contribution`, `.achievements` ; infobulles des deux dernières : `guild.col.contribution.tip`, `guild.col.achievements.tip`.
- **Lignes** : `ListRow` (S157 : alternance #ffffff08, séparateur 1 px #ffffff0f, survol #2d2d2d, sélection `selected` + bord gauche 3 px pistache). Clic : sélectionne (rien d'autre) ; clic droit ou « ⋯ » : menu d'actions. Infobulle de la ligne : `guild.seen.*` ou zone (existant `Where`), la note entière.

### 2.2 États d'une ligne

| État | Point | Nom | Autres textes | Icône de classe |
|---|---|---|---|---|
| connecté | plein 10×10 `--osmose-air` #52c45a, anneau 2 px `--osmose-ui-edge` | #ffffff | `--osmose-ui-text` #cccccc | opaque |
| déconnecté | anneau seul 2 px `text-off` #6a6a6a, centre transparent (forme ≠ couleur : lisible sans couleur) | `text-dim` #9d9d9d | `text-dim` | `modulate` alpha 0,5 |
| soi-même | (selon l'état) | « (vous) » ajouté | — | — ; liseré gauche 3 px `--chocolat-clair` (le pistache reste à la sélection) |
| meneur | — | inchangé | rang « Meneur » en `--osmose-ui-gold` #ffd166 (11,8:1), couronne dorée | — |
| officier | — | — | rang `--osmose-ui-text`, galon chocolat clair | — |
| membre | — | — | rang `text-dim`, soldat gris | — |

Contrastes sur #1f1f1f : #ffffff 16:1, #cccccc 11,6:1, #9d9d9d 6,9:1, #6a6a6a (anneau, graphique) 3,4:1 ≥ 3:1.

### 2.3 Pied de l'onglet Membres (y 580-624)
- `CheckBox` `guild.show-offline` (cochée par défaut) à x 16 ; `CheckBox` `guild.notify-online` à x 360 ; les deux mémorisées dans les réglages du client. Décochée « afficher » : les lignes déconnectées disparaissent, l'en-tête garde « 12 / 50 membres » (pas de filtre sur le compte).
- À droite : `ButtonSecondary` `guild.leave` 176×44, variante `ButtonDanger` (§5.1), puis confirmation (`ConfirmationDialog` thémée) `guild.leave.confirm` ; pour le meneur, `guild.leave.confirm-leader` (le titre passe à l'officier le plus ancien).
- Signal de connexion : ligne de discussion système + toast 3 s en haut à droite « {0} vient de se connecter. » (`guild.online-now`), seulement si la case est cochée, jamais pour soi.

### 2.4 Menu d'actions « ⋯ »
- Bouton : `TreeToggle` 44×44, pictogramme trois points (3 disques 4×4, écart 4, SVG simple, `text-dim` → `text-strong`). Absent (place gardée) si aucune action n'est permise (`GuildPage.Actions` toutes fausses et pas sa propre note).
- `PopupMenu` thémé (fond `Tooltip` #181818f7, bord 1 px `--chocolat-clair`, rayon 6, items h 44, icône 20, texte 14 px) : `guild.note.edit` (soi-même ; le meneur et les officiers pour tous, si la règle est retenue §7) ; séparateur ; `guild.promote`, `guild.demote`, `guild.hand-over` (existants, icônes delapouite/sergeant, delapouite/private, lorc/crown) ; séparateur ; `guild.kick` (lorc/cross-mark) en `--osmose-ui-close-hover-text` #f08070. Transmettre et exclure demandent confirmation.
- Note : petite modale 480×192 : `LineEdit` 448×44 (`MaxLength` 40), compteur, Annuler / Enregistrer.

### 2.5 Vides
- Recherche sans résultat : centré dans la liste, `guild.search.empty` 16 px `--osmose-ui-text` + `ButtonSecondary` `achievements.empty.clear` (réutilisé : « Effacer la recherche »).
- Tous déconnectés et case décochée : `guild.all-offline` 14 px `text-dim`.

---

## 3. Onglet Coffre (consultable partout, modifiable chez Edmée)

Le `GET /guild/chest` ne vérifie pas le lieu : la fenêtre montre le contenu et le journal partout ; déposer, retirer et ouvrir un onglet restent chez Edmée (`AtTheChest`). Pas de trésor de guilde.

```
x 16          216 232                                  676 692                          1024
   +------------+  +-------------------------------------+  MOUVEMENTS                       y 228
   |[coffre] 1  |  | [] [] [] [] [] [] [] []  8 × 5      |  ----------------------------------
   | ouvert     |  | [] [] [] [] [] [] [] []  cases 52   |  il y a 2 h  Plop  +3 [ic] Chêne   lignes h 36
   |------------|  | [] [] [] [] [] [] [] []  écart 4    |  il y a 5 h  Ondine −1 [ic] Fer    (lecture seule)
   |[coffre] 2  |  | [] [] [] [] [] [] [] []             |  ...  défilement, 100 au plus
   | réservé    |  | [] [] [] [] [] [] [] []             |
   |------------|  +-------------------------------------+  y 504
   |[cadenas] 3 |  « 23 / 40 emplacements »   text-dim       y 512-532
   | niveau 6   |
   |[cadenas] 4 |  [i] Déposer et retirer : chez Edmée, sur la place de Clairval.     y 540-568
   | niveau 9   |      (chez Edmée : [ Ouvrir le coffre ] ButtonPrimary 200×44 à la place)
   +------------+
```
- Onglets du coffre (x 16-216) : `TreeRow` (S158) 200×64 (deux lignes : « Onglet {0} » 14 px gras ; dessous l'état 14 px `text-dim` : `guild.chest.open` « ouvert aux membres » / `guild.chest.reserved` « meneur et officiers » / `guild.chest.locked` « au niveau {0} »). Icône 24 : lorc/locked-chest (onglet disponible), lorc/padlock en `text-off` (pas encore débloqué : `GuildPage.TabLevel`, ligne désactivée, alpha 0,5).
- Grille : 40 emplacements `IconFrameLocked` (S158) 52×52, écart 4 (8 × 52 + 7 × 4 = 444 ; 5 × 52 + 4 × 4 = 276), y 228-504. Objet : icône 40 + quantité 14 px gras #ffffff contour 3 px noir en bas à droite ; infobulle de l'objet (existante). Cases vides : creux seul.
- Journal (x 692-1024, 332) : `SectionLabel` `guild.chest.moves`, puis lignes en lecture seule h 36 (pas des cibles) : quand 14 px `text-dim` (64), qui 14 px #ffffff (96, trim), quantité 14 px gras (+ `--osmose-air`, − `--osmose-ui-close-hover-text`, avec le signe : pas la couleur seule), icône 20 + nom de l'objet trim ; infobulle : onglet et date complète. Filtre implicite : l'onglet choisi à gauche (`guild.chest.all-tabs` en tête de liste pour tout voir).
- Pied (y 540-568) : pictogramme info 20 `--chocolat-clair` + `guild.chest.where` 14 px `--osmose-ui-text`. Chez Edmée : `ButtonPrimary` `guild.chest.open-at` qui ouvre la fenêtre du coffre existante (S156) ; le pied général de la fenêtre est vide dans cet onglet.

---

## 4. Onglet Journal

```
x 16                                                                          1024
   [Tout v]  (OptionButton 200×44 : Tout, Arrivées et départs, Rangs, Niveau et blason)   y 228-272
   ---------------------------------------------------------------------------------
   28 oct. 21:04 | [porte]    Plouf a rejoint la guilde (invité·e par Plop).           lignes h 36
   28 oct. 18:30 | [galon]    Ondine a nommé Plop officier.
   27 oct. 09:12 | [hexagone] La guilde atteint le niveau 7.                           niveau : texte doré
   ...  défilement, 100 derniers événements, le plus récent en haut                     y 284-568
```
- Colonnes : date 120 (14 px `text-dim`, format court localisé), icône 20 (écart 8), phrase 14 px `--osmose-ui-text`, noms en #ffffff. Montée de niveau : phrase en `--osmose-ui-gold`. Lignes en lecture seule h 36, alternance #ffffff08.
- Icônes (silhouettes déjà au dépôt, 20 px, couleur unie `text-dim`, pas de relief à cette taille) : arrivée delapouite/team-upgrade (`invite`), départ / exclusion lorc/cross-mark, rang delapouite/sergeant, meneur lorc/crown, niveau = `LevelHex` réduit 20×22 sans chiffre, message / blason delapouite/pencil.
- Vide : `guild.log.empty` 14 px `text-dim` centré.

---

## 5. Onglet Recrutement (meneur et officiers)

```
x 16                                                                           1024
   INVITER UN JOUEUR                                                                  y 228 SectionLabel
   [ Nom d'un personnage en jeu      320×44 ]  [ Inviter ]  L'invitation vaut une minute.   y 252-296
   CANDIDATURES (3)                                                                   y 312
   +-----------------------------------------------------------------------------+
   |[M] Plouf   niv. 34   il y a 2 h                    [ Accepter ] [ Refuser ] |  ligne h 64
   |    « J'aime les donjons du soir et je joue surtout la Mage. »               |
   +-----------------------------------------------------------------------------+
   |  ... défilement (y 336-568, 3,6 lignes visibles)                           |
```
- Invitation : `LineEdit` 320×44 (`MaxLength` 20, Entrée = inviter), `ButtonPrimary` `guild.invite` (existant) 136×44, note `guild.invite.lasts` 14 px `text-dim`. Guilde complète : champ et bouton désactivés, infobulle `guild.full`.
- Candidature : nouvelle variation `ApplicationRow` = `ListRow` h 64 (sans sélection) : icône de classe 28 à (12, 10) ; ligne 1 (y 10-30) nom 16 px gras #ffffff, « niv. 34 » 14 px `text-dim`, « il y a 2 h » 14 px `text-dim` ; ligne 2 (y 34-54) le mot entre guillemets 14 px `--osmose-ui-text`, `TrimEllipsis`, entier en infobulle. À droite, centrés verticalement : `ButtonPrimary` `guild.apply.accept` 120×44, écart 8, `ButtonSecondary` `guild.apply.refuse` 112×44.
- Officier : les boutons sont désactivés, infobulle `guild.apply.leader-only` (si la règle du brief est gardée ; voir §7). Guilde complète : « Accepter » désactivé, infobulle `guild.full`.
- Vide : `guild.apply.none` 14 px `text-dim` centré dans la liste.

---

## 6. Vue d'un personnage sans guilde (même fenêtre 1040×640, sans en-tête ni onglets)

```
x 16                                   512 528                                   1024
   +--------------------------------------+ +--------------------------------------+   y 56
   | FONDER UNE GUILDE                    | | POSTULER À UNE GUILDE                |   DetailPanel 496×272
   | +------+ Edmée, la greffière, sur la | | Nom de la guilde                     |
   | |écu par| place de Clairval, inscrit  | | [ Les Veilleurs           464×44  ] |
   | |défaut | une guilde.                 | | Un mot pour le meneur      42 / 140 |
   | +------+  [éclat] 1 000 Éclats        | | [ J'aime les donjons...   464×44  ] |
   |                                      | |                                      |
   | [ Nom de la guilde (3 à 20)  464×44 ]| |                     [ Postuler ]     |   ButtonPrimary 160×44
   |                [ Fonder · 1 000 ]   | |                                      |   y 328
   +--------------------------------------+ +--------------------------------------+
   VOS CANDIDATURES (1 / 3)                                                             y 344 SectionLabel
   +-------------------------------------------------------------------------------+
   | [écu40] Les Veilleurs   envoyée il y a 2 h   « J'aime les... »  En attente  [ Retirer ] |  h 56
   +-------------------------------------------------------------------------------+
   ... (y 368-568)
   Un meneur ou un officier peut aussi vous inviter.                                    y 580-624 pied, text-dim
```
- **Fonder** : `CrestFrame` 88×88 avec `GuildCrests.Default` ; texte `guild.found.where` 14 px `--osmose-ui-text` ; prix : pictogramme Éclat 20 + « 1 000 Éclats » 14 px gras `--osmose-cost` #f2d36b. Hors de chez Edmée : champ et bouton masqués, à leur place `guild.found.talk` 14 px `text-dim`. Après la réponse d'Edmée (`OfferFounding`) : `LineEdit` 464×44 (`guild.found.name`, existant) et `ButtonPrimary` `guild.found.pay` 200×44, aligné à droite ; Éclats insuffisants : désactivé, infobulle `guild.found.poor` ; nom invalide : bord du champ `--osmose-ui-close-hover-text` + `Tell`.
- **Postuler** : deux `LineEdit` 464×44 (nom : `MaxLength` 20 ; mot : `MaxLength` 140, compteur 14 px `text-dim` à droite de l'étiquette), étiquettes 14 px `--osmose-ui-text` ; `ButtonPrimary` `guild.apply` 160×44 désactivé tant que le nom a moins de 3 caractères ou que 3 candidatures attendent (infobulle `guild.apply.max`).
- **Vos candidatures** : `ListRow` h 56 (sans sélection) : `CrestView` 40 (si la guilde existe encore), nom 16 px gras #ffffff, « envoyée il y a… » 14 px `text-dim`, le mot trim 14 px `--osmose-ui-text`, statut (pastille h 24, rayon 12, côtés 8, texte 14 px) : en attente fond `--osmose-ui-tab` texte `text-strong` ; refusée fond `--osmose-ui-close-hover` #3a2424 texte #f08070 ; acceptée : la vue bascule sur la guilde. `ButtonSecondary` `guild.apply.withdraw` 112×44 (en attente seulement). Vide : `guild.apply.mine.none` 14 px `text-dim`.
- Une invitation reçue reste le bandeau existant (`guild.invited`, Rejoindre / Refuser).

---

## 7. Composants

**Réutilisés** : `WindowPanel`, `TitleBar`, `CloseButton`, `TabBar` (S157), `ListRow`, `ProgressBar` / `GoldBar`, `ProgressBarCompact`, `SearchField`, `SectionLabel`, `DetailPanel`, `TreeRow`, `TreeToggle`, `IconFrameLocked`, `CheckBox`, `LineEdit`, `ButtonPrimary`, `ButtonSecondary`, infobulle, barre de défilement, badge S157.

**À ajouter à `OsmoseTheme`** (une constante chacune, test xUnit : couleurs = `UiStyle`, cibles ≥ 44) :

| Variation | Base | Valeurs |
|---|---|---|
| `CrestFrame` | PanelContainer | 9-patch §4.1 `raised-top` → `raised-bottom`, contour 2 px `Frame` #7a5434, rayon 8, marges 12 ; nouveau 9-patch `frame-crest` dans `ui_art.py` |
| `LevelHex` | TextureRect + Label | PNG 80×88 (2×) affiché 40×44 (§8) ; label 16 px gras `GoldText` centré |
| `ColumnHeader` | Button | plat, h 44, côtés 8 ; texte 14 gras `TextDim`, survol `TextStrong`, trié #ffffff ; fond de rangée `Tab` + filet bas 1 px `FrameDark` |
| `ButtonDanger` | ButtonSecondary | comme `ButtonSecondary` ; survol : texte `CloseHoverText` #f08070, bord 1 px `CloseHoverText` ; pressé : fond `CloseHover` #3a2424 |
| `ApplicationRow` | PanelContainer | `ListRow` h 64, sans état sélectionné |
| `StatusPill` | Label | h 24, rayon 12, côtés 8, 14 px ; variantes `Pending` (`Tab` / `TextStrong`) et `Refused` (`CloseHover` / `CloseHoverText`) |
| `PopupMenu` (thème par défaut) | PopupMenu | fond `Tooltip`, bord 1 px `ChocolateLight`, rayon 6, items h 44, survol `Hover`, icône 20, 14 px `Text` |

**Jetons** : aucun nouveau. `--osmose-air` #52c45a (point connecté, « + » du journal) et `--osmose-distance` / `--osmose-melee` / `--osmose-tempo` (classes) existent déjà ; à exposer dans `UiStyle` (`Online = "#52c45a"` et les trois teintes) avec leur test « présent dans tokens.css ». Disparaissent de `GuildPanel` : `7fd06b`, `e2b23a`, `ffb4a2`, `ff8a75`, `2a1d06` et toutes les `Card(...)` posées à la main.

---

## 8. Icônes (recette S157 §1, PNG 2×, `tools/ui_art.py`, crédits CC BY 3.0 dans `credits.json`)

| Usage | Silhouette game-icons.net (vérifiée le 28 oct. 2026) | main | light | dark | outline | Rendu |
|---|---|---|---|---|---|---|
| Rang meneur | lorc/crown (déjà au dépôt) | #ffd166 | #ffe6ab | #8c7338 | #332a14 | 40 → 20 |
| Rang officier | delapouite/sergeant (au dépôt) | #c9a27f | #e1ccb9 | #6f5946 | #282019 | 40 → 20 |
| Rang membre | delapouite/private (au dépôt) | #cfcfcf | #e5e5e5 | #727272 | #292929 | 40 → 20 |
| Classe Sentinelle | **lorc/high-shot** (nouvelle) | #6bb8f2 `--osmose-distance` | #b0d9f8 | #3b6585 | #152530 | 56 → 28 |
| Classe Garde | **lorc/checked-shield** (nouvelle) | #e8a33d `--osmose-melee` | #f2cc94 | #805a22 | #2e210c | 56 → 28 |
| Classe Mage | **lorc/pointy-hat** (nouvelle) | #b48cff `--osmose-tempo` | #d6c0ff | #634d8c | #241c33 | 56 → 28 |
| Crayon | **delapouite/pencil** (nouvelle) | — unie `TextDim`, teinte par `icon_*_color` | | | | 40 → 20 |
| Onglet du coffre | **lorc/locked-chest** (nouvelle) | #d09a5c (palette `inventory`) | #e5c7a5 | #725533 | #2a1f12 | 48 → 24 |
| Onglet verrouillé | **lorc/padlock** (nouvelle) | unie `TextOff` | | | | 48 → 24 |
| Classe : variantes grises | les trois ci-dessus, palette `-locked` S158 §3.2 | #8a8a8a | #bfbfbf | #4c4c4c | #1c1c1c | pour les déconnectés : non, on garde `modulate` 0,5 ; grises réservées si `modulate` sort trop terne à la relecture |

Teintes light/dark/outline : formule S157 §1.3 (`mix` 0,45 et × 0,20), entrées ajoutées à `palette.json` (`rank-leader`, `rank-officer`, `rank-member`, `class-sentinel`, `class-guard`, `class-mage`, `chest`). Les icônes de classe sont aussi utilisables ailleurs (groupe, contacts, candidature) : dossier `assets/icons/classes/<id>.png`.

**Hexagone de niveau** (SVG fait main, pas game-icons ; `ui_art.py`, PNG 80×88) :
- Hexagone pointe en haut : `points="40,4 76,24 76,64 40,84 4,64 4,24"`, `stroke-linejoin="round"`.
- Couches : ombre (même polygone `translate(0 3)`, #000, opacité 0,45) ; corps dégradé vertical `--osmose-ui-gold-top` #ffe6ab (0) → `--osmose-ui-gold` #ffd166 (0,5) → `--osmose-ui-gold-bottom` #c9962e (1) ; reflet : polygone `40,8 72,26 72,44 8,44 8,26` en #fff 0,35 → 0 ; contour 4 unités (2 px affichés) `--chocolat` #5a3a22. Même famille que `GoldBand` : le doré dit « niveau de guilde, récompense collective ».
- Chiffre (1 à 10) : 16 px gras `GoldText` #3a2410 (10:1), centré, décalé de +1 px en bas.

---

## 9. Tailles et fenêtre réduite
- 1280×720 : 1040×640 centrée. Viewport plus petit : fenêtre = viewport − 32, min 960×560. Largeur 960 : colonne Nom 216 → 176, Note 220 → 180 ; message de l'en-tête 480 → 400 (2 lignes + ellipse) ; coffre : journal 332 → 252 ; vue sans guilde : cartes 456. Hauteur 560 : 5 lignes de membres visibles au lieu de 7, grille du coffre défile.
- Toutes les cibles ≥ 44 (en-têtes de colonne, lignes, « ⋯ », crayon, onglets, cases, boutons) ; les lignes de journal (36) sont en lecture seule ; texte ≥ 14 px partout.
- Grille de 4 px.

## 10. Données et règles à signaler (hors graphisme, à trancher avant le code)
- `GuildMember` n'a ni **note**, ni **points apportés**, ni **points de succès** : trois champs à ajouter (le serveur cumule déjà les points de guilde par source ; il faut les attribuer au membre). Proposition pour la note : 40 caractères, écrite par le membre lui-même, effaçable par le meneur et les officiers.
- **Journal de guilde** : n'existe pas (seul le coffre a un journal) ; table d'événements (arrivée, départ, exclusion, rang, titre transmis, niveau, message, blason), 100 derniers.
- **Candidatures** : table (personnage, guilde, mot ≤ 140, date, état) ; proposition : 3 en attente au plus par personnage, expirent après 7 jours, retirées à l'entrée dans une guilde. Le brief réserve accepter / refuser au meneur ; les officiers invitant déjà, on peut aussi leur ouvrir (alors `guild.apply.leader-only` disparaît).
- **Signal de connexion** : nouvelle nouvelle `GuildNewsKind.Online` (id, nom) poussée aux membres connectés.
- Pas de « lieu » de guilde (Osmose n'a pas de maison de guilde) : la ligne de la semaine en tient lieu.

## 11. Textes FR / EN (`Texts.cs`, vouvoiement)
Gardées : `guild.title`, `guild.rank.*`, `guild.col.name/rank/level/class`, `guild.seen.*`, `guild.name`, `guild.invite`, `guild.leave`, `guild.promote/demote/kick/hand-over`, `guild.invited/join/decline`, `guild.found.name`, `guild.found`, `guild.level`, `guild.points`, `guild.points.how`, `guild.message.none`, `guild.edit`, `guild.goal`, `guild.save`, `guild.cancel`, `guild.crest*`. Retirées : `guild.col.where`, `guild.room`, `guild.online`, `guild.offline` (remplacées par le tableau et `guild.roster`), `guild.none` (remplacée par la vue §6).

| Clé | FR | EN |
|---|---|---|
| `guild.shortcut` | ({0}) | ({0}) |
| `guild.level.top` | Niveau maximal | Top level |
| `guild.week.short` | Semaine | Week |
| `guild.roster` | {0} / {1} membres · niveau moyen {2} | {0} / {1} members · average level {2} |
| `guild.message.title` | Message du meneur | Leader's message |
| `guild.tab.members` | Membres | Members |
| `guild.tab.chest` | Coffre | Chest |
| `guild.tab.log` | Journal | Log |
| `guild.tab.recruit` | Recrutement | Recruitment |
| `guild.search` | Rechercher un membre | Search members |
| `guild.col.status` | Connecté ou non | Online or not |
| `guild.col.note` | Note | Note |
| `guild.col.contribution` | Guilde | Guild |
| `guild.col.contribution.tip` | Points apportés à la guilde | Points brought to the guild |
| `guild.col.achievements` | Succès | Achievements |
| `guild.col.achievements.tip` | Points de succès | Achievement points |
| `guild.you` | (vous) | (you) |
| `guild.show-offline` | Afficher les membres déconnectés | Show offline members |
| `guild.notify-online` | Signaler la connexion d'un membre | Tell me when a member logs in |
| `guild.online-now` | {0} vient de se connecter. | {0} has just logged in. |
| `guild.leave.confirm` | Quitter la guilde {0} ? | Leave the guild {0}? |
| `guild.leave.confirm-leader` | Quitter la guilde {0} ? Votre titre de meneur passera à l'officier le plus ancien. | Leave the guild {0}? Your leader's title will pass to the longest-serving officer. |
| `guild.actions` | Actions | Actions |
| `guild.note.edit` | Modifier la note | Edit the note |
| `guild.note.hint` | Une note de {0} caractères au plus | A note of {0} characters at most |
| `guild.search.empty` | Aucun membre ne correspond à « {0} ». | No member matches “{0}”. |
| `guild.all-offline` | Aucun membre n'est connecté. | No member is online. |
| `guild.crest.same` | Le fond et le motif doivent être de deux couleurs. | The field and the charge need two colours. |
| `guild.chest.tab` | Onglet {0} | Tab {0} |
| `guild.chest.open` | ouvert aux membres | open to members |
| `guild.chest.reserved` | meneur et officiers | leader and officers |
| `guild.chest.locked` | au niveau {0} | at level {0} |
| `guild.chest.slots` | {0} / {1} emplacements | {0} / {1} slots |
| `guild.chest.moves` | Mouvements | Movements |
| `guild.chest.all-tabs` | Tous les onglets | All tabs |
| `guild.chest.where` | Déposer et retirer : chez Edmée, sur la place de Clairval. | Deposit and withdraw at Edmée's, on Clairval's square. |
| `guild.chest.open-at` | Ouvrir le coffre | Open the chest |
| `guild.log.filter.all` / `.members` / `.ranks` / `.guild` | Tout / Arrivées et départs / Rangs / Niveau et blason | All / Arrivals and departures / Ranks / Level and crest |
| `guild.log.joined` | {0} a rejoint la guilde. | {0} joined the guild. |
| `guild.log.invited-by` | {0} a rejoint la guilde (invitation de {1}). | {0} joined the guild (invited by {1}). |
| `guild.log.left` | {0} a quitté la guilde. | {0} left the guild. |
| `guild.log.kicked` | {1} a exclu {0}. | {1} sent {0} out. |
| `guild.log.rank` | {1} a nommé {0} {2}. | {1} made {0} {2}. |
| `guild.log.leader` | {0} mène désormais la guilde. | {0} now leads the guild. |
| `guild.log.level` | La guilde atteint le niveau {0}. | The guild reaches level {0}. |
| `guild.log.message` | {0} a changé le message. | {0} changed the message. |
| `guild.log.crest` | {0} a changé le blason. | {0} changed the crest. |
| `guild.log.empty` | Rien à signaler pour l'instant. | Nothing to report yet. |
| `guild.invite.title` | Inviter un joueur | Invite a player |
| `guild.invite.lasts` | L'invitation vaut une minute. | The invitation lasts one minute. |
| `guild.full` | La guilde est complète ({0} membres). | The guild is full ({0} members). |
| `guild.apply.title` | Candidatures ({0}) | Applications ({0}) |
| `guild.apply.accept` | Accepter | Accept |
| `guild.apply.refuse` | Refuser | Refuse |
| `guild.apply.leader-only` | Seul le meneur répond aux candidatures. | Only the leader answers applications. |
| `guild.apply.none` | Aucune candidature en attente. | No application waiting. |
| `guild.ago` | il y a {0} | {0} ago |
| `guild.found.section` | Fonder une guilde | Found a guild |
| `guild.found.where` | Edmée, la greffière, sur la place de Clairval, inscrit les nouvelles guildes. | Edmée, the registrar on Clairval's square, records new guilds. |
| `guild.found.price` | {0} Éclats | {0} Shards |
| `guild.found.talk` | Parlez à Edmée pour fonder votre guilde. | Talk to Edmée to found your guild. |
| `guild.found.pay` | Fonder · {0} Éclats | Found · {0} Shards |
| `guild.found.poor` | Il vous faut {0} Éclats. | You need {0} Shards. |
| `guild.apply.section` | Postuler à une guilde | Apply to a guild |
| `guild.apply.guild` | Nom de la guilde | Guild's name |
| `guild.apply.word` | Un mot pour le meneur | A word for the leader |
| `guild.apply` | Postuler | Apply |
| `guild.apply.max` | Vous avez déjà {0} candidatures en attente. | You already have {0} applications waiting. |
| `guild.apply.mine` | Vos candidatures ({0} / {1}) | Your applications ({0} / {1}) |
| `guild.apply.sent` | envoyée il y a {0} | sent {0} ago |
| `guild.apply.pending` | En attente | Waiting |
| `guild.apply.refused` | Refusée | Refused |
| `guild.apply.withdraw` | Retirer | Withdraw |
| `guild.apply.mine.none` | Aucune candidature en cours. | No application in progress. |
| `guild.apply.or-invite` | Un meneur ou un officier peut aussi vous inviter. | A leader or an officer can also invite you. |

À faire relire au scénariste (ton) : `guild.found.where`, `guild.found.talk`, `guild.log.empty`, `guild.invite.lasts`, `guild.apply.word` ; `guild.log.rank` (« a nommé Plop Officier » : rang en minuscule dans la phrase, accord au féminin à vérifier).
