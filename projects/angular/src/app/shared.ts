// What the Angular dashboard shares with the React one: the API's types, the views' computations, the
// chart scales and the texts are imported from projects/react as they are (D44); only the views differ.
export type { Lang, MeshIoFile, MlFile, ParallelBenchFile, Project, ProjectsFile, Status } from '../../../react/src/api';
export { distinct, filterProjects, meshIoSeries, parallelAtLargest, projectSummary, type StatusFilter } from '../../../react/src/model';
export { linearScale, logScale, type Scale } from '../../../react/src/charts/scale';
export { formatMs, formatNumber, formatPercent, makeT, pickLang, type Key, type T } from '../../../react/src/i18n';
