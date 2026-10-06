# Décisions, aventure

## V1. Réécrire plutôt que reprendre
**Choix :** une nouvelle histoire et un nouveau code ; le jeu d'origine (« Robert Bizarre Adventure ») n'est pas repris.
**Pourquoi :** il a été écrit à deux ; le publier demanderait l'accord du co-auteur. Le jeu garde l'esprit (lieux, objets, combat, commandes) dans un décor de laboratoire, en lien avec le portfolio.

## V2. Un moteur sans entrée ni sortie, compilable en JavaScript
**Choix :** `Game.respond(ligne)` renvoie le texte à afficher ; aucune dépendance, pas de `String.format`, de `ResourceBundle` ni d'expressions régulières ; TeaVM compile le moteur en module JavaScript (profil Maven `web`).
**Pourquoi :** le même code sert au terminal, au navigateur et aux tests ; TeaVM ne couvre qu'une partie de la bibliothèque Java, d'où ces choix.
**Alternatives :** CheerpJ (exécute le .jar tel quel, mais charge une machine virtuelle Java complète depuis un service tiers), une réécriture en JavaScript (deux moteurs à maintenir).

## V3. Un combat décidé par le parapluie
**Choix :** le robot a 18 points et frappe toujours 4 ; le joueur frappe de 1 à 3, plus 3 avec le parapluie.
**Pourquoi :** l'indice du gardien doit compter : sans parapluie, la défaite est certaine (le robot inflige 20 points avant le sixième coup nécessaire), avec, la victoire aussi (cinq coups suffisent, le robot n'inflige que 16). Le hasard ne change que les chiffres affichés.
