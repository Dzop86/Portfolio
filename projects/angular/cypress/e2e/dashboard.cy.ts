// The Angular dashboard (D44) as published under /angular/, in a real browser.
const visit = (path: string, theme: 'dark' | 'light' = 'dark') =>
  cy.visit(`/angular/${path}`, { onBeforeLoad: (win) => win.localStorage.setItem('theme', theme) });

describe('every view, language, theme and width', () => {
  for (const [width, height] of [[1280, 800], [375, 812]] as const) {
    for (const theme of ['dark', 'light'] as const) {
      for (const lang of ['fr', 'en'] as const) {
        for (const view of ['projects', 'results'] as const) {
          it(`${lang} #/${view} (${theme}, ${width} px): loads its data, is accessible, no horizontal scroll`, () => {
            cy.viewport(width, height);
            visit(`?lang=${lang}#/${view}`, theme);
            cy.get('html').should('have.attr', 'lang', lang);
            cy.get(view === 'projects' ? '[data-project]' : '[data-bar]').should('have.length.greaterThan', 0);
            cy.get('[role="alert"]').should('not.exist');
            cy.window().then((win) => expect(win.document.documentElement.scrollWidth - win.innerWidth).to.be.at.most(1));
            cy.checkA11y();
          });
        }
      }
    }
  }
});

describe('the dashboard', () => {
  it('keeps the view and the language in the address, and switches language on the same view', () => {
    visit('?lang=fr#/projects');
    cy.contains('nav a', 'Résultats').click();
    cy.location('hash').should('eq', '#/results');
    cy.location('search').should('eq', '?lang=fr');
    cy.get('nav a[aria-current="page"]').should('have.text', 'Résultats');
    cy.get('a[aria-label="Read the dashboard in English"]').click();
    cy.location('search').should('eq', '?lang=en');
    cy.contains('h2', 'Results').should('be.visible');
  });

  it('filters the projects, and a card leads to its page on the site', () => {
    visit('?lang=en#/projects');
    cy.get('#f-tech').select('Ada');
    cy.get('[data-project="ada"]').should('exist');
    cy.get('[data-project="react"]').should('not.exist');
    cy.get('#f-tech').select('All');
    cy.get('[data-project="parallele"]').contains('a', 'Project page').click();
    cy.location('pathname').should('eq', '/en/project-parallele.html');
  });

  it('draws the torus first, its family shown in the menu, and redraws for another family', () => {
    visit('?lang=en#/results');
    cy.get('[data-bar]').should('have.length', 5);
    cy.get('#f-family').should('have.value', 'torus').find('option:selected').should('have.text', 'torus');
    cy.get('[data-table="meshio"] tbody tr').first().invoke('text').then((torus) => {
      cy.get('#f-family').select('sphere');
      cy.get('[data-table="meshio"] tbody tr').first().invoke('text').should('not.eq', torus);
    });
  });

  it('remembers the theme across the site', () => {
    visit('?lang=en#/projects');
    cy.contains('button', 'Light theme').click();
    cy.window().its('localStorage').invoke('getItem', 'theme').should('eq', 'light');
    cy.reload();
    cy.get('html').should('have.attr', 'data-theme', 'light');
    cy.contains('button', 'Dark theme').should('exist');
  });
});
