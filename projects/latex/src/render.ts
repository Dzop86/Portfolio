// Renders a parsed document to HTML: numbered sections and table of contents, KaTeX for mathematics,
// numbered equations, \ref and \cite resolved like a second LaTeX run, lists, tables, footnotes. Every
// piece of source text is escaped; only http(s) and mailto links are kept.
import katex from 'katex';
import type { Diagnostic, Document, Node, Pos } from './ast.ts';

export interface OutlineEntry {
  level: 1 | 2 | 3;
  number: string;
  title: string;
  id: string;
  line: number;
}

export interface Rendered {
  html: string;
  outline: OutlineEntry[];
  diagnostics: Diagnostic[];
}

export interface Options {
  lang?: 'fr' | 'en';
  /** Text of \today; a fixed value keeps the output reproducible. */
  today?: string;
  /** Prefix of every id, so two rendered documents can share a page. */
  idPrefix?: string;
  /** Heading level of the document title (1 to 3): sections are one level below, and so on. */
  headingLevel?: 1 | 2 | 3;
}

const WORDS = {
  fr: { contents: 'Table des matières', references: 'Références', abstract: 'Résumé', notes: 'Notes' },
  en: { contents: 'Contents', references: 'References', abstract: 'Abstract', notes: 'Notes' },
};

const LEVELS: Record<string, 1 | 2 | 3> = { section: 1, subsection: 2, subsubsection: 3 };
/** Math environments, and whether they are numbered. */
const MATH_ENVS: Record<string, boolean> = {
  equation: true, 'equation*': false, align: true, 'align*': false,
  gather: true, 'gather*': false, multline: true, 'multline*': false,
};
/** Environments KaTeX needs inside display math to lay out several lines. */
const MATH_INNER: Record<string, string> = { align: 'aligned', gather: 'gathered', multline: 'gathered' };

export function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c] as string);
}

/** TeX typography in plain text: quotes, dashes, non-breaking spaces, single newlines as spaces. */
function typography(s: string): string {
  return s
    .replace(/``/g, '“').replace(/''/g, '”')
    .replace(/---/g, '—').replace(/--/g, '–')
    .replace(/~/g, ' ')
    .replace(/[ \t]*\n[ \t]*/g, ' ');
}

function safeUrl(url: string): string | null {
  const u = url.trim();
  return /^(https?:\/\/|mailto:)/i.test(u) ? u : null;
}

/** Plain text of nodes (for titles in the outline and alt texts). */
function plain(nodes: Node[]): string {
  return nodes.map((n) => {
    if (n.type === 'text') return typography(n.value);
    if (n.type === 'group') return plain(n.children);
    if (n.type === 'command') return n.args.map(plain).join(' ');
    if (n.type === 'math') return n.tex;
    return '';
  }).join('').replace(/\s+/g, ' ').trim();
}

const isCommand = (n: Node | undefined, name: string): n is Extract<Node, { type: 'command' }> =>
  n?.type === 'command' && n.name === name;

/** Splits nodes on \item: [{ label, nodes }], with what comes before the first \item dropped. */
function items(nodes: Node[]) {
  const out: { label: Node[] | null; nodes: Node[]; pos: Pos }[] = [];
  for (const n of nodes) {
    if (isCommand(n, 'item')) out.push({ label: n.optional, nodes: [], pos: n.pos });
    else out.at(-1)?.nodes.push(n);
  }
  return out;
}

export function render(doc: Document, options: Options = {}): Rendered {
  const lang = options.lang ?? 'en';
  const words = WORDS[lang];
  const prefix = options.idPrefix ?? 'tex-';
  const top = options.headingLevel ?? 1;
  const diagnostics: Diagnostic[] = [...doc.diagnostics];
  const warn = (message: string, pos: Pos) => diagnostics.push({ severity: 'warning', message, pos });

  // Pass 1: numbers of sections, equations and bibliography items, and the labels pointing at them.
  const labels = new Map<string, { number: string; id: string }>();
  const numbers = new Map<Node, { number: string; id: string }>();
  const citations = new Map<string, number>();
  const outline: OutlineEntry[] = [];
  const counters = [0, 0, 0];
  let equation = 0;
  let current: { number: string; id: string } | null = null;

  function label(key: string, pos: Pos) {
    if (labels.has(key)) warn(`label "${key}" defined twice`, pos);
    else if (current) labels.set(key, current);
    else warn(`\\label{${key}} has nothing numbered to point at`, pos);
  }

  function number(nodes: Node[]) {
    for (const n of nodes) {
      if (n.type === 'command' && LEVELS[n.name] && !n.star) {
        const level = LEVELS[n.name] as 1 | 2 | 3;
        counters[level - 1]! += 1;
        for (let k = level; k < 3; k++) counters[k] = 0;
        const num = counters.slice(0, level).join('.');
        current = { number: num, id: `${prefix}sec-${num}` };
        numbers.set(n, current);
        outline.push({ level, number: num, title: plain(n.args[0] ?? []), id: current.id, line: n.pos.line });
      } else if (n.type === 'command' && n.name === 'label') {
        label(plain(n.args[0] ?? []), n.pos);
      } else if (n.type === 'command' && n.name === 'bibitem') {
        const key = plain(n.args[0] ?? []);
        if (citations.has(key)) warn(`bibliography item "${key}" defined twice`, n.pos);
        else citations.set(key, citations.size + 1);
      } else if (n.type === 'env' && n.name in MATH_ENVS) {
        const keys = [...(n.raw ?? '').matchAll(/\\label\{([^}]*)\}/g)].map((m) => (m[1] ?? '').trim());
        if (MATH_ENVS[n.name]) {
          equation += 1;
          current = { number: String(equation), id: `${prefix}eq-${equation}` };
          numbers.set(n, current);
        }
        for (const key of keys) label(key, n.pos);
      } else {
        if (n.type === 'group') number(n.children);
        if (n.type === 'env') number(n.children);
        if (n.type === 'command') n.args.forEach(number);
      }
    }
  }
  number(doc.body);

  // Pass 2: HTML.
  const footnotes: string[] = [];

  function tex(source: string, display: boolean, pos: Pos, tag?: string): string {
    const body = source.replace(/\\label\{[^}]*\}/g, '');
    try {
      return katex.renderToString(tag ? `${body}\\tag{${tag}}` : body, {
        displayMode: display, throwOnError: true, strict: 'ignore', trust: false, output: 'htmlAndMathml',
      });
    } catch (e) {
      const message = e instanceof Error ? e.message.replace(/^KaTeX parse error: /, '') : String(e);
      diagnostics.push({ severity: 'error', message: `math: ${message}`, pos });
      return `<code class="tex-error" title="${escapeHtml(message)}">${escapeHtml(source)}</code>`;
    }
  }

  function inline(nodes: Node[]): string {
    return nodes.map(inlineNode).join('');
  }

  function inlineNode(n: Node): string {
    switch (n.type) {
      case 'text': return escapeHtml(typography(n.value));
      case 'group': return inline(n.children);
      case 'math': return tex(n.tex, n.display, n.pos);
      case 'align':
        warn('& outside a table: write \\& for the character', n.pos);
        return '&amp;';
      case 'parbreak': return ' ';
      case 'env': return block([n]);
      case 'command': return command(n);
    }
  }

  function command(n: Extract<Node, { type: 'command' }>): string {
    const arg = (k: number) => inline(n.args[k] ?? []);
    switch (n.name) {
      case 'emph': case 'textit': return `<em>${arg(0)}</em>`;
      case 'textbf': return `<strong>${arg(0)}</strong>`;
      case 'texttt': return `<code>${arg(0)}</code>`;
      case 'textsc': return `<span class="tex-sc">${arg(0)}</span>`;
      case 'underline': return `<u>${arg(0)}</u>`;
      case 'url': case 'href': {
        const raw = plain(n.args[0] ?? []).replace(/\s/g, '');
        const url = safeUrl(raw);
        const text = n.name === 'url' ? escapeHtml(raw) : arg(1);
        if (!url) {
          warn(`link "${raw}" ignored: only http, https and mailto links are kept`, n.pos);
          return text;
        }
        return `<a href="${escapeHtml(url)}" rel="noopener">${text}</a>`;
      }
      case 'ref': case 'eqref': {
        const key = plain(n.args[0] ?? []);
        const target = labels.get(key);
        if (!target) {
          warn(`undefined reference "${key}"`, n.pos);
          return '<span class="tex-undefined">??</span>';
        }
        const text = n.name === 'eqref' ? `(${target.number})` : target.number;
        return `<a href="#${escapeHtml(target.id)}">${text}</a>`;
      }
      case 'cite': {
        const keys = plain(n.args[0] ?? []).split(',').map((k) => k.trim()).filter(Boolean);
        const parts = keys.map((key) => {
          const k = citations.get(key);
          if (!k) {
            warn(`undefined citation "${key}"`, n.pos);
            return '?';
          }
          return `<a href="#${prefix}bib-${k}">${k}</a>`;
        });
        const note = n.optional ? `, ${inline(n.optional)}` : '';
        return `[${parts.join(', ')}${note}]`;
      }
      case 'footnote': {
        footnotes.push(arg(0));
        const k = footnotes.length;
        return `<sup class="tex-fnref" id="${prefix}fnref-${k}"><a href="#${prefix}fn-${k}">${k}</a></sup>`;
      }
      case 'LaTeX': return '<span class="tex-logo">L<span class="tex-a">a</span>T<span class="tex-e">e</span>X</span>';
      case 'TeX': return '<span class="tex-logo">T<span class="tex-e">e</span>X</span>';
      case 'today': return escapeHtml(options.today ?? new Date().toISOString().slice(0, 10));
      case 'ldots': case 'dots': return '…';
      case '\\': case 'newline': return '<br>';
      case ',': return ' ';
      case ' ': case ';': case ':': return ' ';
      case '!': case '-': case '/': case '@': case 'noindent': case 'centering': case 'label':
      case 'bibitem': case 'item': case 'hline': case 'documentclass': case 'usepackage': return '';
      case '%': case '&': case '$': case '#': case '_': case '{': case '}': case '~': case '^':
        return escapeHtml(n.name === '~' ? '~' : n.name);
      case 'par': case 'bigskip': case 'medskip': case 'smallskip': return ' ';
      case 'maketitle': case 'tableofcontents': return '';
      default:
        if (LEVELS[n.name] || n.name === 'paragraph') return `<strong>${arg(0)}</strong> `;
        return `<span class="tex-unknown">\\${escapeHtml(n.name)}</span>${n.args.map((a) => inline(a)).join('')}`;
    }
  }

  /** Inline runs between block nodes become paragraphs; blank lines end them. */
  function block(nodes: Node[]): string {
    let out = '';
    let para: Node[] = [];
    const flush = () => {
      const html = inline(para).trim();
      if (html) out += `<p>${html}</p>`;
      para = [];
    };
    for (const n of nodes) {
      const blockHtml = blockNode(n);
      if (blockHtml === null) {
        if (n.type === 'parbreak') flush();
        else para.push(n);
      } else {
        flush();
        out += blockHtml;
      }
    }
    flush();
    return out;
  }

  /** HTML of a block-level node, or null for inline content. */
  function blockNode(n: Node): string | null {
    if (n.type === 'math' && n.display) return `<div class="tex-display">${tex(n.tex, true, n.pos)}</div>`;
    if (n.type === 'command') {
      const level = LEVELS[n.name];
      if (level) {
        const num = numbers.get(n);
        const tag = `h${top + level}`;
        const id = num ? ` id="${escapeHtml(num.id)}"` : '';
        const numberHtml = num ? `<span class="tex-num">${num.number}</span> ` : '';
        return `<${tag}${id}>${numberHtml}${inline(n.args[0] ?? [])}</${tag}>`;
      }
      if (n.name === 'maketitle') {
        const part = (cls: string, nodes: Node[] | null) => (nodes ? `<p class="${cls}">${inline(nodes)}</p>` : '');
        return `<header class="tex-title">${doc.meta.title ? `<h${top}>${inline(doc.meta.title)}</h${top}>` : ''}${part('tex-author', doc.meta.author)}${part('tex-date', doc.meta.date)}</header>`;
      }
      if (n.name === 'tableofcontents') {
        const entries = outline.map((e) => `<li class="tex-toc-${e.level}"><a href="#${escapeHtml(e.id)}"><span class="tex-num">${e.number}</span> ${escapeHtml(e.title)}</a></li>`).join('');
        return `<nav class="tex-toc"><p class="tex-toc-title">${words.contents}</p><ol>${entries}</ol></nav>`;
      }
      return null;
    }
    if (n.type !== 'env') return null;
    if (n.name in MATH_ENVS) {
      const num = numbers.get(n);
      const inner = MATH_INNER[n.name.replace('*', '')];
      const source = inner ? `\\begin{${inner}}${n.raw}\\end{${inner}}` : (n.raw ?? '');
      const id = num ? ` id="${escapeHtml(num.id)}"` : '';
      return `<div class="tex-display"${id}>${tex(source, true, n.pos, num?.number)}</div>`;
    }
    switch (n.name) {
      case 'itemize': case 'enumerate': {
        const tag = n.name === 'itemize' ? 'ul' : 'ol';
        return `<${tag}>${items(n.children).map((it) => `<li>${block(it.nodes)}</li>`).join('')}</${tag}>`;
      }
      case 'description':
        return `<dl>${items(n.children).map((it) => `<dt>${inline(it.label ?? [])}</dt><dd>${block(it.nodes)}</dd>`).join('')}</dl>`;
      case 'quote': case 'quotation': return `<blockquote>${block(n.children)}</blockquote>`;
      case 'center': return `<div class="tex-center">${block(n.children)}</div>`;
      case 'abstract': return `<section class="tex-abstract"><p class="tex-abstract-title">${words.abstract}</p>${block(n.children)}</section>`;
      case 'verbatim': return `<pre>${escapeHtml(n.raw ?? '')}</pre>`;
      case 'tabular': return table(n);
      case 'thebibliography': {
        const entries = items(n.children.map((c) => (isCommand(c, 'bibitem') ? { ...c, name: 'item' } : c)));
        let k = 0;
        return `<section class="tex-bib"><h${top + 1}>${words.references}</h${top + 1}><ol>${entries.map((it) => `<li id="${prefix}bib-${++k}">${block(it.nodes)}</li>`).join('')}</ol></section>`;
      }
      case 'document': return block(n.children);
      default:
        warn(`unknown environment "${n.name}", shown as plain content`, n.pos);
        return `<div class="tex-env">${block(n.children)}</div>`;
    }
  }

  function table(n: Extract<Node, { type: 'env' }>): string {
    const spec = plain(n.args[0] ?? []).replace(/[|\s]/g, '');
    const aligns = [...spec].map((c) => ({ l: 'left', c: 'center', r: 'right' })[c] ?? 'left');
    const rows: { cells: Node[][]; rule: boolean }[] = [];
    let row: Node[][] = [[]];
    let ruleAbove = false;
    const endRow = () => {
      if (row.some((c) => c.some((x) => x.type !== 'text' || x.value.trim()))) rows.push({ cells: row, rule: ruleAbove });
      row = [[]];
      ruleAbove = false;
    };
    for (const c of n.children) {
      if (c.type === 'align') row.push([]);
      else if (isCommand(c, '\\')) endRow();
      else if (isCommand(c, 'hline')) ruleAbove = true;
      else row.at(-1)!.push(c);
    }
    endRow();
    const body = rows.map((r) => `<tr${r.rule ? ' class="tex-rule"' : ''}>${r.cells.map((cell, k) =>
      `<td style="text-align:${aligns[k] ?? 'left'}">${inline(cell).trim()}</td>`).join('')}</tr>`).join('');
    return `<div class="tex-table"><table>${body}</table></div>`;
  }

  let html = block(doc.body);
  if (footnotes.length) {
    html += `<section class="tex-notes"><p class="tex-notes-title">${words.notes}</p><ol>${footnotes.map((f, k) =>
      `<li id="${prefix}fn-${k + 1}">${f} <a href="#${prefix}fnref-${k + 1}" aria-label="↩">↩</a></li>`).join('')}</ol></section>`;
  }
  diagnostics.sort((a, b) => a.pos.offset - b.pos.offset);
  return { html, outline, diagnostics };
}
