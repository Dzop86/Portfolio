import { useId, useState } from 'react';
import type { Lang, ProjectsFile, Status } from '../api';
import type { T } from '../i18n';
import { filterProjects, projectSummary, type StatusFilter } from '../model';

const STATUSES: Status[] = ['done', 'in-progress', 'planned'];

export function ProjectsView({ data, lang, t, siteRoot }: { data: ProjectsFile; lang: Lang; t: T; siteRoot: string }) {
  const [status, setStatus] = useState<StatusFilter>('all');
  const [tech, setTech] = useState('');
  const statusId = useId();
  const techId = useId();
  const sum = projectSummary(data.projects);
  const shown = filterProjects(data.projects, status, tech);

  return (
    <section aria-labelledby="h-projects">
      <h2 id="h-projects">{t('nav.projects')}</h2>
      <p>{t('projects.summary', sum)}</p>
      <progress className="meter" max={sum.points} value={sum.pointsDone} aria-label={t('projects.summary', sum)} />
      <form className="filters" aria-label={t('projects.filters')} onSubmit={(e) => e.preventDefault()}>
        <label htmlFor={statusId}>{t('projects.status')}</label>
        <select id={statusId} value={status} onChange={(e) => setStatus(e.target.value as StatusFilter)}>
          <option value="all">{t('projects.all')}</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {t(`status.${s}`)}
            </option>
          ))}
        </select>
        <label htmlFor={techId}>{t('projects.tech')}</label>
        <select id={techId} value={tech} onChange={(e) => setTech(e.target.value)}>
          <option value="">{t('projects.allTechs')}</option>
          {data.techs.map((x) => (
            <option key={x} value={x}>
              {x}
            </option>
          ))}
        </select>
      </form>
      <p className="muted" role="status">
        {shown.length ? t('projects.count', { n: shown.length }) : t('projects.none')}
      </p>
      <ul className="cards">
        {shown.map((p) => (
          <li key={p.id} className="card" data-project={p.id}>
            <h3>{p.name[lang]}</h3>
            <p className="badges">
              <span className={`badge status-${p.status}`}>{t(`status.${p.status}`)}</span>
              <span className="muted">{t('projects.sprint', { sprint: p.sprint })}</span>
              <span className="muted">{t('projects.points', { n: p.points })}</span>
            </p>
            <p>{p.pitch[lang]}</p>
            <p className="tags">
              {p.stack.map((s) => (
                <span key={s} className="tag">
                  {s}
                </span>
              ))}
            </p>
            <p className="links">
              <a href={`${siteRoot}${p.page[lang]}`}>{t('projects.page')}</a>
              {p.code && <a href={p.code}>{t('projects.code')}</a>}
            </p>
          </li>
        ))}
      </ul>
    </section>
  );
}
