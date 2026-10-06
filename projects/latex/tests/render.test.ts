// Unit tests of the HTML rendering: structure, numbering and references, KaTeX, lists, tables,
// footnotes, escaping and link safety, diagnostics.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { renderLatex } from '../src/index.ts';

const html = (src: string, lang: 'fr' | 'en' = 'en') => renderLatex(src, { lang, today: '2026-10-06' }).html;
const messages = (src: string) => renderLatex(src).diagnostics.map((d) => `${d.severity}: ${d.message}`);

test('paragraphs, emphasis, typography', () => {
  assert.equal(html("Hello \\emph{world}.\n\nSecond ``para'' --- yes."),
    '<p>Hello <em>world</em>.</p><p>Second “para” — yes.</p>');
  assert.equal(html('a~b, \\textbf{c} \\texttt{d}'), '<p>a\u00a0b, <strong>c</strong> <code>d</code></p>');
});

test('sections are numbered, get ids, and fill the outline and the table of contents', () => {
  const r = renderLatex('\\tableofcontents\n\\section{Intro}\\subsection{Goal}\\section*{Aside}\\section{Mesh}');
  assert.match(r.html, /<h2 id="tex-sec-1"><span class="tex-num">1<\/span> Intro<\/h2>/);
  assert.match(r.html, /<h3 id="tex-sec-1.1"><span class="tex-num">1.1<\/span> Goal<\/h3>/);
  assert.match(r.html, /<h2>Aside<\/h2>/);
  assert.match(r.html, /<h2 id="tex-sec-2"><span class="tex-num">2<\/span> Mesh<\/h2>/);
  assert.deepEqual(r.outline.map((e) => `${e.number} ${e.title} @${e.line}`), ['1 Intro @2', '1.1 Goal @2', '2 Mesh @2']);
  assert.match(r.html, /<nav class="tex-toc"><p class="tex-toc-title">Contents<\/p><ol><li class="tex-toc-1"><a href="#tex-sec-1">/);
});

test('references resolve forward and backward, like a second LaTeX run', () => {
  const out = html('See \\ref{s:b} and \\eqref{e:m}.\\section{A}\\section{B}\\label{s:b}\n\\begin{equation}E=mc^2\\label{e:m}\\end{equation}');
  assert.match(out, /See <a href="#tex-sec-2">2<\/a> and <a href="#tex-eq-1">\(1\)<\/a>\./);
  assert.match(out, /<div class="tex-display" id="tex-eq-1">/);
  assert.doesNotMatch(out, /\\label/);
});

test('mathematics goes through KaTeX; numbered environments carry their tag', () => {
  assert.match(html('$x^2$'), /class="katex"/);
  assert.match(html('$$\\sum_i a_i$$'), /class="katex-display"/);
  const eq = html('\\begin{equation}a\\end{equation}\\begin{equation*}b\\end{equation*}\\begin{align}x&=1\\\\y&=2\\end{align}');
  assert.equal((eq.match(/class="katex-tag"/g) ?? []).length, 2, 'equation and align numbered, equation* not');
  assert.match(eq, /\(2\)/);
});

test('a KaTeX error is a located diagnostic, and the formula is shown as source', () => {
  const r = renderLatex('ok\n$\\frac{a$');
  assert.match(r.html, /<code class="tex-error" title="[^"]+">\\frac\{a<\/code>/);
  assert.equal(r.diagnostics.length, 1);
  assert.deepEqual([r.diagnostics[0]?.severity, r.diagnostics[0]?.pos.line, r.diagnostics[0]?.pos.column], ['error', 2, 1]);
  assert.match(r.diagnostics[0]?.message ?? '', /^math: /);
});

test('lists, description, quote, verbatim', () => {
  assert.equal(html('\\begin{itemize}\\item a \\item b\\end{itemize}'), '<ul><li><p>a</p></li><li><p>b</p></li></ul>');
  assert.equal(html('\\begin{enumerate}\\item x\\end{enumerate}'), '<ol><li><p>x</p></li></ol>');
  assert.equal(html('\\begin{description}\\item[Term] def\\end{description}'), '<dl><dt>Term</dt><dd><p>def</p></dd></dl>');
  assert.equal(html('\\begin{verbatim}<b>&\\x\\end{verbatim}'), '<pre>&lt;b&gt;&amp;\\x</pre>');
  assert.equal(html('\\begin{quote}q\\end{quote}'), '<blockquote><p>q</p></blockquote>');
});

test('tables: columns aligned from the spec, rules, empty last row dropped', () => {
  const t = html('\\begin{tabular}{|l|r|}\\hline a & 1 \\\\ \\hline b & 22 \\\\ \\end{tabular}');
  assert.equal(t, '<div class="tex-table"><table><tr class="tex-rule"><td style="text-align:left">a</td><td style="text-align:right">1</td></tr>'
    + '<tr class="tex-rule"><td style="text-align:left">b</td><td style="text-align:right">22</td></tr></table></div>');
});

test('citations and the bibliography are numbered in order of the items', () => {
  const out = html('As shown \\cite{tierny,mari}, see \\cite[p.~3]{mari}.\n\\begin{thebibliography}{9}\\bibitem{mari} J.-L. Mari.\\bibitem{tierny} J. Tierny.\\end{thebibliography}');
  assert.match(out, /As shown \[<a href="#tex-bib-2">2<\/a>, <a href="#tex-bib-1">1<\/a>\], see \[<a href="#tex-bib-1">1<\/a>, p.\u00a03\]\./);
  assert.match(out, /<section class="tex-bib"><h2>References<\/h2><ol><li id="tex-bib-1"><p>J.-L. Mari.<\/p><\/li><li id="tex-bib-2">/);
});

test('footnotes are numbered and collected at the end', () => {
  const out = html('A\\footnote{First.} B\\footnote{Second.}');
  assert.match(out, /A<sup class="tex-fnref" id="tex-fnref-1"><a href="#tex-fn-1">1<\/a><\/sup> B<sup/);
  assert.match(out, /<section class="tex-notes"><p class="tex-notes-title">Notes<\/p><ol><li id="tex-fn-1">First\. <a href="#tex-fnref-1"/);
});

test('title block, abstract and words follow the language', () => {
  const doc = '\\title{Maillages}\\author{Charles Lepaire}\\date{\\today}\\begin{document}\\maketitle\\begin{abstract}R\\end{abstract}\\tableofcontents\\end{document}';
  const fr = html(doc, 'fr');
  assert.match(fr, /<header class="tex-title"><h1>Maillages<\/h1><p class="tex-author">Charles Lepaire<\/p><p class="tex-date">2026-10-06<\/p><\/header>/);
  assert.match(fr, /Résumé/);
  assert.match(fr, /Table des matières/);
  assert.match(html(doc, 'en'), /Abstract/);
});

test('all source text is escaped; only http(s) and mailto links survive', () => {
  assert.equal(html('<script>alert(1)</script> & "x"'), '<p>&lt;script&gt;alert(1)&lt;/script&gt; &amp; &quot;x&quot;</p>');
  assert.equal(html('\\href{javascript:alert(1)}{click}'), '<p>click</p>');
  assert.equal(html('\\url{https://dzop86.github.io/Portfolio/}'),
    '<p><a href="https://dzop86.github.io/Portfolio/" rel="noopener">https://dzop86.github.io/Portfolio/</a></p>');
  assert.match(html('\\href{https://a.org/?q="><img>}{x}'), /href="https:\/\/a.org\/\?q=&quot;&gt;&lt;img&gt;"/);
  // In math, KaTeX (trust: false) shows \href as refused source text; the URL only survives as text.
  assert.doesNotMatch(html('$\\href{javascript:alert(1)}{x}$'), /href="javascript:/i);
});

test('diagnostics: undefined references and citations, unknown environments and commands, duplicates', () => {
  assert.deepEqual(messages('\\ref{nope} \\cite{who}'), ['warning: undefined reference "nope"', 'warning: undefined citation "who"']);
  assert.deepEqual(messages('\\begin{figure}x\\end{figure}'), ['warning: unknown environment "figure", shown as plain content']);
  assert.deepEqual(messages('\\section{A}\\label{a}\\section{B}\\label{a}'), ['warning: label "a" defined twice']);
  assert.deepEqual(messages('\\label{x}'), ['warning: \\label{x} has nothing numbered to point at']);
  assert.match(html('\\foo{bar}'), /<span class="tex-unknown">\\foo<\/span>bar/);
  assert.deepEqual(messages('a & b'), ['warning: & outside a table: write \\& for the character']);
  assert.deepEqual(messages('\\href{ftp://x}{y}'), ['warning: link "ftp://x" ignored: only http, https and mailto links are kept']);
});

test('headings start at the requested level, so the document fits inside a page', () => {
  const out = renderLatex('\\title{T}\\maketitle\\section{S}\\subsection{U}\\begin{thebibliography}{9}\\end{thebibliography}', { headingLevel: 3 }).html;
  assert.match(out, /<h3>T<\/h3>/);
  assert.match(out, /<h4 id="tex-sec-1">/);
  assert.match(out, /<h5 id="tex-sec-1.1">/);
  assert.match(out, /<h4>References<\/h4>/);
  assert.doesNotMatch(out, /<h[12][ >]/);
});

test('diagnostics come sorted by position', () => {
  const lines = renderLatex('\\ref{b}\n$\\frac$\n\\ref{a}').diagnostics.map((d) => d.pos.line);
  assert.deepEqual(lines, [1, 2, 3]);
});
