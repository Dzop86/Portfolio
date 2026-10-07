import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// The site's colours, as they are (rule 6: tokens.css is the only source of colours).
import '../../../src/assets/tokens.css';
import './styles.css';
import { App } from './App';
import { pickLang } from './i18n';

const root = document.getElementById('root');
if (!root) throw new Error('no #root element');
createRoot(root).render(
  <StrictMode>
    <App lang={pickLang(window.location.search, navigator.language)} />
  </StrictMode>,
);
