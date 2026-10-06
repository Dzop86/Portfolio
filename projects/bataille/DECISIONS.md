# Décisions, bataille

## B1. Un mélange écrit ici
**Choix :** xorshift64 et Fisher-Yates dans `War.Shuffled`, plutôt que `Ada.Numerics.Discrete_Random`.
**Pourquoi :** le générateur de la bibliothèque standard dépend de l'implémentation ; une graine doit donner la même partie sur tous les systèmes, pour les tests comme pour les statistiques commitées.

## B2. Un ordre de ramassage fixe, et une limite de plis
**Choix :** le gagnant d'un pli ramasse les cartes dans l'ordre où elles ont été posées ; une partie s'arrête à 10 000 plis et est déclarée sans fin.
**Pourquoi :** c'est la règle la plus simple à énoncer et à tester ; la limite rend chaque partie finie. Les statistiques montrent la conséquence (42 % de parties sans fin), présentée comme un résultat plutôt que cachée.
**Alternative :** ramasser dans un ordre tiré au hasard (comme le font de vrais joueurs), qui ferait finir presque toutes les parties ; une option possible plus tard.

## B3. Sortie UTF-8 écrite telle quelle
**Choix :** les noms de carte sont des `UTF_8_String` écrites par un flux (`String'Write`), pas par `Put_Line`.
**Pourquoi :** le compilateur lit les sources en UTF-8 (`-gnatW8`), si bien que `Text_IO` encoderait une seconde fois les octets des symboles (« Ã¢ » au lieu de « ♠ ») : vu sur la sortie, corrigé.
