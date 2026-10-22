# Osmose : playtest à plusieurs (sprint 125, D79)

> 45 minutes, guidées. Charles et un à trois joueurs ; ou Charles seul avec plusieurs jeux ouverts (question 71 du game designer, valeur par défaut). Les retours deviennent la décision suivante (D80).

## Avant
- Le jeu Windows de `Téléchargements\Osmose`, un exemplaire par joueur (le même dossier peut être lancé plusieurs fois).
- Le serveur local : `docker compose up -d --build rpg-api` (port 8002).
- Un personnage par joueur, de niveau 20 au moins (ou les personnages de Plouf).

## Le parcours (environ 45 minutes)
1. **Se grouper (5 min).** Clic droit sur un autre joueur → Inviter. Vérifier le bandeau à droite : le chef, les niveaux, un joueur hors ligne pâli. Le chef essaie la croix « Exclure », puis réinvite.
2. **Une horde à deux (10 min).** Dans le Bois moisi ou au Lac, un groupe de 7 ou 8 monstres. L'un attaque ; l'autre ferme la question exprès et rejoint par la ligne du bandeau (« X attaque : rejoindre (12 s) ») ou en voyant les épées au-dessus du groupe. Pendant le combat : regarder l'anneau et les secondes dans la frise, laisser passer deux tours exprès pour voir « absent » et les tours de 10 s.
3. **Un chef à trois (10 min).** La reine des abeilles (`fields-0-4`, niveau 30) ou un autre chef. Noter si le combat paraît juste.
4. **Un donjon (10 min).** Chacun entre avec sa clé ; les trois salles ensemble.
5. **Un échange (5 min).** Un objet et quelques Éclats ; puis un échange ouvert et un jeu fermé : l'échange doit s'annuler chez l'autre.
6. **Le redémarrage (5 min).** Pendant un combat à plusieurs : `docker compose restart rpg-api`. Le combat doit reprendre là où il en était (rouvrir le jeu ou attendre la reconnexion) ; refaire le groupe (les groupes ne survivent pas au redémarrage).

## À noter
- Ce qui a été lent, confus ou injuste, avec l'heure.
- Les combats perdus qui semblaient gagnables (et l'inverse).
- Tout message d'erreur affiché.
