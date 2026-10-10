# Décisions, rag

## G1. La documentation du portfolio comme corpus
**Choix :** le corpus est la documentation du dépôt : `README.md`, `PLAN.md`, `DECISIONS.md` à la racine, et le `README.md` et le `DECISIONS.md` de chaque projet (`docrag.chunking.DEFAULT_PATTERNS`) : 45 fichiers, 421 passages, en français avec des résumés en anglais.
**Pourquoi :** c'est une vraie documentation technique d'équipe (choix d'architecture, commandes, limites), écrite de zéro et publique : rien de confidentiel à indexer, et chaque réponse se vérifie en ouvrant le fichier cité.
**Limites :** la documentation de ce projet-ci est exclue (`DEFAULT_EXCLUDE`) : elle parle des questions de référence et y « répondrait » à la place des vrais passages. Un corpus de taille modeste ; les fichiers qui changent changent les mesures (la CI les refait à chaque modification de la documentation).

## G2. Des passages qui gardent leurs titres et leurs lignes
**Choix :** un découpage Markdown écrit ici (`chunking.py`) : des blocs (paragraphe, élément de liste, tableau, bloc de code) avec leur première et leur dernière ligne, regroupés sous les mêmes titres jusqu'à 1 200 caractères ; un bloc plus long reste seul, jamais coupé. Le texte cherché commence par le chemin des titres (« Journal des décisions > D8. Node 22 minimum »).
**Pourquoi :** une citation doit renvoyer à des lignes entières d'un fichier ; les titres disent de quoi parle un passage que son texte seul ne nomme pas. Mesuré sur les questions de référence avec le modèle retenu : 500 caractères, recherche hybride rappel@5 0,75 et MRR 0,53 ; 800 : 0,78 et 0,62 ; 1 200 : 0,81 et 0,62.
**Alternatives :** les découpeurs de LangChain ou LlamaIndex (taille fixe avec recouvrement) : ils perdent les numéros de ligne ou demandent de les reconstruire.

## G3. Une recherche hybride : BM25 et vecteurs, fusionnés par rang
**Choix :** BM25 (écrit ici, 40 lignes, sans accents ni mots courants) et la similarité cosinus des embeddings dans Qdrant, fusionnés par *reciprocal rank fusion* (1 / (60 + rang), sur les 20 premiers de chaque liste).
**Pourquoi :** les mots-clés trouvent les noms propres (`zone.js`, `MLflow`, D45) que les embeddings ratent, les embeddings trouvent les reformulations et passent d'une langue à l'autre. Mesuré (32 questions) : BM25 rappel@5 0,66, vecteurs 0,75, hybride 0,81 ; MRR 0,51, 0,49 et 0,62. RRF ne compare que des rangs : pas de normalisation de scores qui n'ont pas la même échelle.
**Limites :** 32 questions : une question vaut 3 points de rappel, les écarts de moins de 6 points ne disent pas grand-chose.

## G4. Un modèle d'embeddings multilingue local, choisi sur mesure
**Choix :** `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` par FastEmbed (ONNX, sans PyTorch, 220 Mo, 384 dimensions), en lots de 32.
**Pourquoi :** mesuré contre deux autres modèles multilingues de FastEmbed, recherche hybride : MiniLM rappel@5 0,81, MRR 0,62, 45 s pour indexer et évaluer ; `potion-multilingual-128M` (embeddings statiques) 0,63 et 0,54, 24 s ; `paraphrase-multilingual-mpnet-base-v2` 0,75 et 0,58, 300 s. Local : ni clé ni coût, et les mêmes vecteurs à chaque fois.
**Limites :** MiniLM tronque à 128 tokens : la fin d'un long passage ne compte pas dans son vecteur (BM25 la voit). Les lots de 256 de FastEmbed, par défaut, ont épuisé la mémoire avec mpnet (processus tué) : d'où les lots de 32.

## G5. Des tests sans modèle, une évaluation avec
**Choix :** les tests (Linux, Windows, macOS, Python 3.12 et 3.13) utilisent un faux embedder déterministe (sac de mots haché) et Qdrant en mémoire ; un job à part télécharge le vrai modèle (mis en cache), évalue les trois recherches et échoue si l'hybride trouve un bon passage dans les cinq premiers pour moins de 3 questions sur 4.
**Pourquoi :** les tests vérifient le code, vite et partout ; l'évaluation vérifie la qualité, là où elle a un sens.
