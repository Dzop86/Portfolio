// Every visible text in French and English; a key missing in one language does not compile.
import type { Lang } from './api';

const fr = {
  title: 'Dashboard du portfolio',
  lead: 'Les données du site, lues dans son API JSON statique : avancement des projets, sprints et résultats mesurés.',
  'nav.label': 'Vues du dashboard',
  'nav.projects': 'Projets',
  'nav.sprints': 'Sprints',
  'nav.results': 'Résultats',
  'lang.switch': 'English',
  'lang.switchLabel': 'Read the dashboard in English',
  'theme.toLight': 'Thème clair',
  'theme.toDark': 'Thème sombre',
  back: 'Retour au site',
  loading: 'Chargement des données…',
  'error.title': 'Les données n’ont pas pu être chargées',
  'error.text': 'Le fichier {url} n’a pas répondu ({status}). Les mêmes informations sont sur les pages du site.',
  'error.network': 'réseau injoignable',
  'error.retry': 'Réessayer',

  'projects.summary': '{done} projets terminés sur {total}, {inProgress} en cours ; {pointsDone} points livrés sur {points}.',
  'projects.filters': 'Filtrer les projets',
  'projects.status': 'État',
  'projects.tech': 'Technologie',
  'projects.all': 'Tous',
  'projects.allTechs': 'Toutes',
  'projects.count': '{n} projet(s) affiché(s)',
  'projects.none': 'Aucun projet ne correspond à ces filtres.',
  'projects.sprint': 'Sprint {sprint}',
  'projects.points': '{n} points',
  'projects.page': 'Fiche du projet',
  'projects.code': 'Code',
  'status.done': 'Terminé',
  'status.in-progress': 'En cours',
  'status.planned': 'Prévu',

  'sprints.summary': '{done} sprints terminés sur {count} ; vélocité moyenne de {mean} points par sprint terminé.',
  'sprints.velocity': 'Vélocité : points engagés et livrés par sprint',
  'sprints.committed': 'Engagés',
  'sprints.delivered': 'Livrés',
  'sprints.sprint': 'Sprint',
  'sprints.burndown': 'Burndown : points des projets restant à livrer',
  'sprints.points': 'Points',
  'sprints.open': 'en cours',
  'sprints.remaining': 'Restant',
  'sprints.after': 'Après le sprint {n}',
  'sprints.goal': 'Objectif du sprint {n}',
  'sprints.latest': 'Derniers sprints',

  'results.parallel': 'Calcul parallèle : courbure de Gauss sur {size} triangles',
  'results.parallelLead': 'Temps médian de {runs} essais par version (plus court est meilleur), mesuré sur {cpu} et {gpu}. Accélération par rapport au C++ séquentiel.',
  'results.bar': '{ms} ms, ×{speedup}',
  'results.version': 'Version',
  'results.time': 'Temps',
  'results.speedup': 'Accélération',
  'results.error': 'Écart max. sur un défaut',
  'results.meshIo': 'Lecture de maillages : temps selon la taille',
  'results.meshIoLead': 'Médiane de {runs} lectures par point, mesurée en WebAssembly ({runtime}) sur {cpu}, rangée dans la base SQL du projet sql.',
  'results.library': 'Bibliothèque',
  'results.family': 'Famille',
  'results.format': 'Format',
  'results.triangles': 'Triangles',
  'results.ms': 'ms',
  'results.ml': 'Classification de formes 3D',
  'results.mlText': 'Le modèle {model} classe des nuages de {points} points en {classes} formes ({list}) ; précision sur le jeu de test :',
  'results.mlOnnx': 'Écart maximal entre PyTorch et l’export ONNX : {gap}.',
  'backend.sequential': 'C++ séquentiel',
  'backend.openmp': 'OpenMP, {threads} threads',
  'backend.opencl': 'OpenCL (processeur)',
  'backend.cuda-double': 'CUDA, double précision',
  'backend.cuda-float': 'CUDA, simple précision',
  'family.torus': 'tore',
  'family.sphere': 'sphère',
  'family.cylinder': 'cylindre',
  'shape.sphere': 'sphère',
  'shape.torus': 'tore',
  'shape.box': 'pavé',
  'shape.cylinder': 'cylindre',
  'shape.cone': 'cône',
  'shape.capsule': 'capsule',
};

export type Key = keyof typeof fr;

const en: Record<Key, string> = {
  title: 'Portfolio dashboard',
  lead: 'The site’s data, read from its static JSON API: project progress, sprints and measured results.',
  'nav.label': 'Dashboard views',
  'nav.projects': 'Projects',
  'nav.sprints': 'Sprints',
  'nav.results': 'Results',
  'lang.switch': 'Français',
  'lang.switchLabel': 'Lire le dashboard en français',
  'theme.toLight': 'Light theme',
  'theme.toDark': 'Dark theme',
  back: 'Back to the site',
  loading: 'Loading the data…',
  'error.title': 'The data could not be loaded',
  'error.text': 'The file {url} did not answer ({status}). The same information is on the site’s pages.',
  'error.network': 'network unreachable',
  'error.retry': 'Try again',

  'projects.summary': '{done} of {total} projects done, {inProgress} in progress; {pointsDone} of {points} points delivered.',
  'projects.filters': 'Filter the projects',
  'projects.status': 'Status',
  'projects.tech': 'Technology',
  'projects.all': 'All',
  'projects.allTechs': 'All',
  'projects.count': '{n} project(s) shown',
  'projects.none': 'No project matches these filters.',
  'projects.sprint': 'Sprint {sprint}',
  'projects.points': '{n} points',
  'projects.page': 'Project page',
  'projects.code': 'Code',
  'status.done': 'Done',
  'status.in-progress': 'In progress',
  'status.planned': 'Planned',

  'sprints.summary': '{done} of {count} sprints done; mean velocity of {mean} points per finished sprint.',
  'sprints.velocity': 'Velocity: points committed and delivered per sprint',
  'sprints.committed': 'Committed',
  'sprints.delivered': 'Delivered',
  'sprints.sprint': 'Sprint',
  'sprints.burndown': 'Burndown: project points left to deliver',
  'sprints.points': 'Points',
  'sprints.open': 'in progress',
  'sprints.remaining': 'Remaining',
  'sprints.after': 'After sprint {n}',
  'sprints.goal': 'Goal of sprint {n}',
  'sprints.latest': 'Latest sprints',

  'results.parallel': 'Parallel computing: Gaussian curvature on {size} triangles',
  'results.parallelLead': 'Median time of {runs} runs per version (shorter is better), measured on {cpu} and {gpu}. Speed-up over sequential C++.',
  'results.bar': '{ms} ms, ×{speedup}',
  'results.version': 'Version',
  'results.time': 'Time',
  'results.speedup': 'Speed-up',
  'results.error': 'Largest error on a defect',
  'results.meshIo': 'Reading meshes: time by size',
  'results.meshIoLead': 'Median of {runs} reads per point, measured in WebAssembly ({runtime}) on {cpu}, stored in the SQL database of the sql project.',
  'results.library': 'Library',
  'results.family': 'Family',
  'results.format': 'Format',
  'results.triangles': 'Triangles',
  'results.ms': 'ms',
  'results.ml': '3D shape classification',
  'results.mlText': 'The {model} model sorts clouds of {points} points into {classes} shapes ({list}); accuracy on the test set:',
  'results.mlOnnx': 'Largest gap between PyTorch and the ONNX export: {gap}.',
  'backend.sequential': 'Sequential C++',
  'backend.openmp': 'OpenMP, {threads} threads',
  'backend.opencl': 'OpenCL (processor)',
  'backend.cuda-double': 'CUDA, double precision',
  'backend.cuda-float': 'CUDA, single precision',
  'family.torus': 'torus',
  'family.sphere': 'sphere',
  'family.cylinder': 'cylinder',
  'shape.sphere': 'sphere',
  'shape.torus': 'torus',
  'shape.box': 'box',
  'shape.cylinder': 'cylinder',
  'shape.cone': 'cone',
  'shape.capsule': 'capsule',
};

export const DICTIONARY: Record<Lang, Record<Key, string>> = { fr, en };

export type T = (key: Key, values?: Record<string, string | number>) => string;

/** The translation function of a language: {name} placeholders filled, an unknown one left visible. */
export function makeT(lang: Lang): T {
  return (key, values = {}) =>
    DICTIONARY[lang][key].replace(/\{(\w+)\}/g, (whole, name: string) => (name in values ? String(values[name]) : whole));
}

/** ?lang=fr|en first, then the browser's language, French by default (as the site's root page). */
export function pickLang(search: string, browser: string | undefined): Lang {
  const asked = new URLSearchParams(search).get('lang');
  if (asked === 'fr' || asked === 'en') return asked;
  return browser?.toLowerCase().startsWith('en') ? 'en' : 'fr';
}

/** Numbers in the language's style: 1 234,5 in French, 1,234.5 in English. */
export function formatNumber(value: number, lang: Lang, digits = 0): string {
  return value.toLocaleString(lang === 'fr' ? 'fr-FR' : 'en-GB', { minimumFractionDigits: digits, maximumFractionDigits: digits });
}

/** A duration in ms with as many decimals as it deserves: 2.35, 21.6, 332. */
export function formatMs(ms: number, lang: Lang): string {
  return formatNumber(ms, lang, ms < 10 ? 2 : ms < 100 ? 1 : 0);
}

/** A share as a percentage in the language's style: 95,2 % in French, 95.2% in English. */
export function formatPercent(share: number, lang: Lang, digits = 1): string {
  return share.toLocaleString(lang === 'fr' ? 'fr-FR' : 'en-GB', { style: 'percent', minimumFractionDigits: digits, maximumFractionDigits: digits });
}
