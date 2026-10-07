// cy.checkA11y(): axe-core run in the page against WCAG 2 A and AA, colour contrast included, as the
// site's Playwright tests do. Written here because cypress-axe does not accept Cypress 16 yet.
import type { AxeResults } from 'axe-core';

declare global {
  namespace Cypress {
    interface Chainable {
      checkA11y(): Chainable<void>;
    }
  }
}

Cypress.Commands.add('checkA11y', () => {
  cy.readFile('node_modules/axe-core/axe.min.js').then((source: string) => {
    cy.window()
      .then((win) => {
        (win as unknown as { eval: (code: string) => void }).eval(source);
        const axe = (win as unknown as { axe: { run: (ctx: Document, opts: object) => Promise<AxeResults> } }).axe;
        return axe.run(win.document, { runOnly: ['wcag2a', 'wcag2aa'] });
      })
      .then((results) => {
        expect(results.violations.map((v) => `${v.id}: ${v.nodes.length}`)).to.deep.equal([]);
      });
  });
});

export {};
