# latex : lire et rendre du LaTeX en TypeScript

[![latex](https://github.com/Dzop86/Portfolio/actions/workflows/latex.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/latex.yml)

Bibliothèque TypeScript qui lit un sous-ensemble de LaTeX en un arbre situé, puis le rend en HTML, les formules par [KaTeX](https://katex.org/). Elle sert l'éditeur en direct de la fiche du projet (sprint 14), façon Overleaf, sur un article qui présente ce portfolio.

*TypeScript library: a LaTeX subset parsed into a located tree, then rendered to safe HTML with KaTeX; malformed input becomes located diagnostics. Strict types, tests on Linux, Windows and macOS.*

```ts
import { renderLatex } from './src/index.ts';
const { html, outline, diagnostics } = renderLatex('\\section{Topologie}\nUn tore a pour genre $g = 1$.', { lang: 'fr' });
```

## Dans le navigateur
La [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-latex.html) contient un éditeur en direct, façon Overleaf, sur l'article qui présente le portfolio (`article/portfolio.fr.tex` et `portfolio.en.tex`) : source et aperçu côte à côte (onglets sur mobile), aperçu redessiné pendant la frappe, diagnostics qui mènent à la ligne et à la colonne, plan qui fait défiler l'aperçu, téléchargement du `.tex`. Choix : D23 dans le [`DECISIONS.md` du site](../../DECISIONS.md).

## Ce qui est lu
- **Structure** : préambule (`\documentclass`, `\usepackage`, `\title`, `\author`, `\date`) et `\begin{document}`, ou un simple fragment ; `\maketitle`, `\tableofcontents`, `\section`, `\subsection`, `\subsubsection` (et leurs versions étoilées, non numérotées), `abstract`.
- **Texte** : `\emph`, `\textbf`, `\textit`, `\texttt`, `\textsc`, `\underline`, `\url`, `\href`, `\footnote`, `\\`, `~`, guillemets ``` `` '' ```, tirets `--` et `---`, caractères échappés (`\%`, `\&`, `\_`...), `\LaTeX`, `\today`.
- **Environnements** : `itemize`, `enumerate`, `description`, `quote`, `center`, `verbatim`, `tabular` (alignement des colonnes, `\hline`), `thebibliography` et `\bibitem`.
- **Mathématiques** : `$...$`, `\(...\)`, `$$...$$`, `\[...\]`, `equation`, `align`, `gather`, `multline` (et étoilés), par KaTeX ; une équation numérotée reçoit son numéro (`\tag`).
- **Renvois** : `\label`, `\ref`, `\eqref`, `\cite` (avec note `[p.~3]`), résolus vers l'avant comme après une seconde compilation.

## Diagnostics plutôt qu'échec
L'analyseur ne lève jamais d'exception (vérifié sur 2 000 entrées aléatoires). Chaque problème devient un diagnostic situé (ligne, colonne en caractères) et la suite du document est rendue :
- **erreurs** : accolade ou crochet non fermé, `}` orpheline, mathématiques non fermées, environnement jamais fermé ou fermé par le mauvais `\end`, argument manquant, formule refusée par KaTeX (montrée en source) ;
- **avertissements** : commande ou environnement inconnu (contenu affiché quand même), `_` `^` `#` `&` hors de leur place, renvoi ou citation indéfinis (`??` comme LaTeX), étiquette en double, lien autre que http(s) ou mailto (ignoré).

## Sûreté
Tout texte source est échappé. Les liens ne gardent que `http`, `https` et `mailto` ; KaTeX tourne avec `trust: false`, donc `\href` dans une formule reste du texte. La profondeur d'imbrication est bornée à 200.

## Lancer
```sh
npm run test:latex     # depuis la racine du dépôt : tsc strict, puis node:test sur les sources TypeScript
```
Node exécute directement le TypeScript (`--experimental-strip-types`) : les sources n'utilisent que de la syntaxe effaçable (`erasableSyntaxOnly`), sans étape de compilation pour les tests.

## Tests
- **Analyseur** (`tests/parse.test.ts`) : forme de l'arbre, arguments optionnels et obligatoires, commentaires et paragraphes, mathématiques, environnements, préambule, positions (UTF-8 et emoji compris), chaque erreur et son emplacement, profondeur bornée, 2 000 entrées aléatoires sans exception.
- **Rendu** (`tests/render.test.ts`) : paragraphes et typographie, sections et table des matières, renvois vers l'avant, KaTeX et numéros, erreur KaTeX située, listes, tableaux, citations, notes, titre et langue, échappement et liens, diagnostics triés. Vérifié en cassant le code : sans filtre des liens ou sans échappement, les tests échouent.
- **Article** (`tests/article.test.ts`) : les deux versions se rendent sans aucun diagnostic, avec leurs trois équations numérotées et le même plan.
- **Éditeur** (Playwright, 5 navigateurs, mobile compris) : article rendu, mise à jour pendant la frappe, diagnostic qui place le curseur, plan qui fait défiler l'aperçu, changement de langue, téléchargement, accessibilité (axe) dans les deux thèmes.
- **CI** (`.github/workflows/latex.yml`) : Linux, Windows et macOS, Node 22 et 24.

## Limites
- Pas de macros (`\newcommand`), de figures ni de flottants ; `align` reçoit un seul numéro pour tout l'environnement.
- Une note de bas de page dans une note est numérotée avant celle qui la contient.
- `\hline` après la dernière ligne d'un tableau n'est pas dessiné.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
