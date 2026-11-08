# Le Voleur, première moitié : la discrétion *(8 novembre 2026, sprint 204, à valider par Charles)*

Ce document prépare la story du sprint 204 : « En tant que Voleur, mon kit se cache et frappe de dos, et je deviens Assassin ou Malandrin, avec Cabriole et Feinte ; xUnit. PloufVoleur créé. » Les poisons qui se cumulent et l'Ombremage viennent au sprint 205. Les finales de l'Assassin et du Malandrin sont écrites dès ce sprint : une lignée sans finales n'est pas une donnée valide (`GameData.CheckSpecs`), et en écrire de provisoires au niveau 100 serait jeté au sprint suivant. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §3 la discrétion, §4.3 la Cabriole et la Feinte) et de `classes-liste.md` §5.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Deuxième classe neuve** après le Prêtre : jouable en entrant dans `classes.json` sous l'id `rogue`, celui que la page de création lui donne depuis le sprint 195 (« Rogue » en anglais) [V, `Lobby.Roster`].
- **Sa fiche** (`classes-conception.md` §1.2) : 58 PV + 3 par niveau, 8 PA, **5 PM**, **initiative 30**, **budget de dégâts 1,15** (1,05 dans la conception, relevé à la mesure, §6), portée 1 à 3, des sauts. La question de son tour : « Comment je l'isole, et par où je ressors ? »
- **Deux briques neuves** :
  1. **les règles d'une classe** : une liste `rules` dans `classes.json`, lue comme les règles d'une voie (un talent de plus au combat). Le Voleur y porte son moteur ;
  2. **la Discrétion** (`Hidden`) : un ennemi ne peut viser le porteur que de 2 cases ou moins ; elle tombe quand le porteur frappe.
- **Son moteur, dès le niveau 1** : **+15 % de dégâts sur une cible qui n'a aucun allié à 2 cases** (règle `isolated`).
- **Ses 24 sorts** : 16 d'élément, presque tous à une cible et au contact, et 8 neutres (sauts, Discrétion, marque, échange). Les poisons se renouvellent comme ceux de l'Archer ; ils se cumuleront au sprint 205.
- **« Frapper de dos »** : le combat ne connaît pas d'orientation [V] ; la conception l'a remplacé par la cible isolée (§3, ce que j'exclus), c'est le moteur.
- **Deux branches sur trois** : l'Assassin (isoler, empoisonner, achever) et le Malandrin (sauter, feinter, piéger), avec leurs 4 lignées et leurs 8 finales ; **la Cabriole et la Feinte** reviennent au Malandrin. 6 sorts de voie neufs, aucune brique de plus.

## 1. Les règles d'une classe

Une classe peut porter des règles, comme une voie (`TalentRule`, sprint 186). Au combat, elles s'ajoutent à celles de la voie du héros, dans le même talent « voie » [V, `Fight.TalentsOf`]. Aucune autre classe n'en a : leurs combats ne changent pas, ni ceux qu'on rejoue.

## 2. Le moteur : la cible isolée

`{"kind": "isolated", "cells": 2, "percent": 15}` : un coup du porteur fait 15 % de dégâts de plus si **aucun combattant du camp de la cible** (elle exceptée) ne se tient à 2 cases ou moins d'elle. Les invocations comptent : une image ou un faucon protège son maître. Le bonus s'ajoute à ceux des talents, sous le même plafond. L'IA le lit comme les autres règles vivantes, au moment de choisir sa cible [V, `Ai` passe par `Fight.Damage` et `ContextOf`].

C'est le « dos et flanc » de la conception sans orientation des combattants (§3, ce que j'exclus) : le Voleur cherche le monstre qui s'est écarté, ou l'en écarte.

## 3. La Discrétion

| | |
|---|---|
| Données | `status` avec `"stat": "Hidden"`, sur soi |
| Visée | un ennemi ne peut lancer un sort **sur la case** du porteur qu'à 2 cases ou moins (`ActionError.Hidden`). Une zone lancée à côté le touche encore |
| Fin | ses tours écoulés, ou dès que le porteur lance un sort qui blesse un ennemi (après le coup, qui en profite) |
| Purge | c'est un bonus : l'Inquisiteur l'arrache |
| IA | la règle est dans `CheckCast`, que l'IA et le client consultent déjà [V] ; l'IA du Voleur la pèse comme Incorporel, 1 par tour |
| Client | le porteur est dessiné à demi transparent ; l'infobulle dit « invisible au-delà de 2 cases » |

**Pourquoi 2 cases et pas l'invisibilité :** un combat au tour par tour où un monstre ne voit plus personne s'arrête ; ici, les monstres de contact frappent encore le Voleur, ceux de loin doivent s'approcher. C'est sa porte de sortie.

**Le relevé des combats.** `Hidden` n'existe que dans les sorts du Voleur : un combat enregistré avant ne le rencontre pas et se rejoue à l'identique ; `FightRecord` ne change pas de version.

## 4. Les 24 sorts du Voleur

Ordre de D62 : un sort de chaque élément et un neutre au niveau 1, puis un sort tous les 5 niveaux. Dégâts au barème × 1,15 (§6). Noms de travail, au scénariste.

| Niv. | Sort (id) | Élément | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| 1 | Coup de pommeau `pommel-strike` | Terre | 3 | 1 | | |
| 1 | Dague ardente `ember-dagger` | Feu | 4 | 1 | | |
| 1 | Entaille `gash` | Eau | 3 | 1-2 | poison Eau 3 × 2 tours | |
| 1 | Couteau lancé `thrown-knife` | Air | 3 | 1-3 | | |
| 1 | Esquive `dodge` | — | 2 | 1-3 | **bond** | 1 |
| 5 | Fondu `fade` | — | 2 | 0 | **Discrétion, 2 tours** | 3 |
| 10 | Croc-en-jambe `trip` | Terre | 3 | 1 | −2 PM, 1 tour | 1 |
| 15 | Fiole ardente `fire-vial` | Feu | 3 | 1-3 | poison Feu 4 × 3 tours | |
| 20 | Saignée `bleed` | Eau | 4 | 1 | poison Eau 5 × 3 tours | |
| 25 | Bond fendant `pounce` | Air | 4 | 2-3 en ligne, croix 1 | **bond**, frappe autour de la case d'arrivée (comme l'Assaut) | 1 |
| 30 | Fumigène `smoke-bomb` | — | 3 | 0, cercle 1 | ennemis : −2 de portée ; soi : **Discrétion, 2 tours** | 3 |
| 35 | Assommoir `cosh` | Terre | 5 | 1 | −1 PA, 1 tour | |
| 40 | Lame rouge `red-blade` | Feu | 5 | 1 | | 1 |
| 45 | Venin lent `slow-venom` | Eau | 4 | 1-2 | poison Eau 6 × 3 tours, −1 PM | |
| 50 | Volte `volte` | Air | 5 | 1-2 | soi : +2 PM, 1 tour | |
| 55 | Contrat `contract` | — | 2 | 1-4 | ennemi : −15 % de résistance, 2 tours | 3 |
| 60 | Brise-genou `kneecap` | Terre | 6 | 1 | −2 PM, 1 tour | 2 |
| 65 | Feu grégeois `greek-fire` | Feu | 6 | 1-2 | poison Feu 8 × 3 tours | |
| 70 | Noyade `drown` | Eau | 6 | 1 | | 1 |
| 75 | Tempête de lames `blade-storm` | Air | 6 | 1-3 | | 2 |
| 80 | Ombre portée `shadow-swap` | — | 2 | 1-4 | **échange** de place avec un ennemi | 2 |
| 85 | Antidote `antidote` | — | 3 | 0 | **purge** de ses malus, soin 12-16 | 4 |
| 90 | Œil du tueur `killers-eye` | — | 2 | 0 | +30 % de dégâts, 2 tours | 3 |
| 95 | Pas de velours `velvet-step` | — | 1 | 0 | +2 PM, 1 tour | 2 |

Une seule zone d'élément, la croix du Bond fendant (un bond se pose sur une case libre, son coup ne peut frapper qu'autour) : le Voleur frappe **une cible** (§1.4 de la conception, ce qui le sépare du Mage). Ses 16 sorts d'élément ont leurs jumeaux composés, comme ceux des autres classes.

## 5. L'arbre : Assassin et Malandrin

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Règle et sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Assassin** (`assassin`) | +10 % sur une cible sous un état ; **Coup bas**, **Lame enduite** | Lame noire (`black-blade`) | **Exécution** | Ombre mortelle (Kill : « Prime touchée ») ; Faucheur silencieux (Faster : Exécution en 1 tour) |
| | | Empoisonneur (`poisoner`) | poisons +1 tour ; **Fiole de peste** | Maître des toxines (TargetState : +20 % sous deux états) ; Alchimiste mortel (Extends : 4 cases, cercle 1) |
| **Malandrin** (`scoundrel`) | +3 % par case marchée depuis le dernier sort, 15 % au plus ; **Cabriole**, **Coup de jarret** | Bretteur (`swashbuckler`) | +1 PM à chaque tour ; **Feinte** | Duelliste (`rogue-duelist`, StayOrMove : 5 % par case, 25 % en tout) ; Lame dansante (Faster : Cabriole sans relance) |
| | | Saboteur (`saboteur`) | **Chausse-trappes** | Démolisseur (TargetState : +25 % sur une cible sans PM au prochain tour) ; Maître des pièges (Second : deux fois par tour, deux à la fois) |

| Sort | Voie | PA | Portée | Zone | Effets | Relance |
|---|---|---|---|---|---|---|
| Coup bas `path-backstab` | Assassin | 3 | 1 | | dégâts | 1 |
| Lame enduite `path-envenom` | Assassin | 2 | 1-2 | | poison 7 × 3 tours | 1 |
| Exécution `path-execute` | Lame noire | 5 | 1 | | dégâts | 2 |
| Fiole de peste `path-plague` | Empoisonneur | 3 | 1-3 | | poison 12 × 3 tours | 2 |
| Cabriole `path-vault` | Malandrin | 2 | 2-4 | | bond | 1 |
| Coup de jarret `path-hamstring` | Malandrin | 3 | 1 | | dégâts, −1 PM | |
| Feinte `path-feint` | Bretteur | 2 | 0 | | le prochain qui frappe au contact perd 2 PM | 3 |
| Chausse-trappes `path-caltrops` | Saboteur | 3 | 0-1 | croix 1 | glyphe 2 tours : 10-13, −2 PM | 2 |

Les identifiants qui existent déjà prennent `rogue-` devant (Duelliste, une finale du Guerrier). Bretteur est aussi le titre d'une lignée du Guerrier (`jailer`) : même nom, deux identifiants, comme la conception l'a prévu (§1.4). Les icônes de l'arbre sont des silhouettes de game-icons.net (CC BY 3.0), dans la palette des voies.

## 6. Mesures

**Le Disjoint seul au niveau 30, kit des artisans** (exigence de la CI : 30 à 100 sur 100). Au premier jet (58 + 3 PV, budget 1,05), le Voleur ne le bat jamais : il lui laisse 21 PV sur 449 en moyenne, en 6 tours de corps-à-corps, en ne jouant que la Dague ardente. Les leviers essayés :

| Essai | Victoires | Ce que ça dit |
|---|---|---|
| 58 + 3, budget 1,05 | 0 sur 100 | premier jet |
| 58 + 4 | 3 sur 20 | |
| 58 + 5 | 19 sur 20 | aussi solide que le Guerrier : contraire à sa fiche, **rejeté** |
| budget 1,1 | 17 sur 100 | sous le plancher |
| budget 1,12 et 1,13 | 25 sur 100 | mêmes dés (13-17) une fois arrondis |
| **budget 1,15** | 20 sur 20 | **retenu** : fragile et qui frappe fort, « une cible, vite » |

**Les autres exigences de la CI.** La grande salle de la Bauge, seul au niveau de la quête : tenue (70 au moins). Les hordes de la Forêt au niveau 45 avec les pièces d'artisan de 40 et 45 : le Voleur y gagne 2,4 combats sur 10 de plus (plafond par classe : 3) ; la moyenne des cinq classes passe à 1,6, son plafond de 1,5 à 1,7. **Les quatre autres classes jouent exactement les mêmes combats qu'avant** (empreinte en solo inchangée) : ses briques ne touchent qu'elles-mêmes.

**Les groupes de référence** (hordes de synthèse au niveau 45, 40 combats, nus) : Guerrier + Voleur 60 %, Archer + Voleur 60 %, Mage + Voleur 57 %, Guerrier + Archer + Voleur 100 %, Guerrier + Prêtre + Voleur 97 %, Archer + Mage + Prêtre + Voleur 100 %. Le Voleur vaut le Prêtre en second héros.

**La fumée de la CI** : le Coup bas n'est joué par l'IA dans aucune voie, même passé de 4 à 3 PA (les sorts du kit, avec leurs rangs, frappent plus fort par PA) ; la Cabriole reste un bond que l'IA ne joue pas (T152). Quatre finales ne changent aucun des 5 combats d'un héros de 100 : Lame dansante (la Cabriole), Maître des pièges (la seconde Chausse-trappe), Démolisseur (une cible sans PM, rare seul), Faucheur silencieux.

Un effet de seuil : contre le Disjoint seul, un point de dés décide du combat. **Le Voleur garde 58 + 3 PV**, les plus bas après l'Invocateur.

## 7. À trancher par Charles

- **Q1. Les noms** des 24 sorts : noms de travail, au scénariste.
- **Q2. La Discrétion à 2 cases** : a) **2 cases (Reco)** ; b) 1 case, plus forte ; c) invisible tant qu'il ne frappe pas, au risque d'un monstre qui n'a plus rien à faire.
- **Q3. Le moteur sur les invocations** : a) **une invocation protège son maître de l'isolement (Reco)**, plus simple et lisible ; b) seuls les vrais combattants comptent.
- **Q4. Les finales écrites dès ce sprint** pour l'Assassin et le Malandrin : a) **oui (Reco)**, une lignée sans finales n'est pas valide ; b) un palier 100 provisoire, jeté au sprint 205.
- **Q5. Le Coup bas** que l'IA ne joue pas : a) **reprendre l'IA au jalon 2, avec la Cabriole (Reco)** ; b) le monter au-dessus du kit, au risque d'un Assassin trop fort à 30.
