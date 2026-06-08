import { defineConfig } from 'vitepress'

// GitHub Pages project site is served under /<repo>/. Override with DOCS_BASE
// (e.g. '/' for a user/org site, or after the repo rename — cf. Linear HIJ-486).
const base = process.env.DOCS_BASE ?? '/Outlet-CLI/'

const github = 'https://github.com/Leroy-Florian/Outlet-CLI'

export default defineConfig({
  base,
  title: 'Outlet',
  description:
    'A copy-paste registry of backend infrastructure for .NET — one generic port, swappable adapters.',
  cleanUrls: true,
  lastUpdated: true,

  // Internal repo notes that are not part of the published site.
  srcExclude: ['README.md'],

  // English is the default locale (served at the root); French lives under /fr/.
  locales: {
    root: {
      label: 'English',
      lang: 'en-US',
      themeConfig: {
        nav: [{ text: 'Guide', link: '/guide/introduction' }],
        sidebar: {
          '/guide/': [
            {
              text: 'Introduction',
              items: [
                { text: 'What is Outlet?', link: '/guide/introduction' },
                { text: 'Getting started', link: '/guide/getting-started' },
              ],
            },
            {
              text: 'Contributing',
              items: [
                { text: 'Testing strategy', link: '/testing' },
                { text: 'Production readiness', link: '/production-readiness' },
              ],
            },
          ],
        },
      },
    },
    fr: {
      label: 'Français',
      lang: 'fr-FR',
      link: '/fr/',
      themeConfig: {
        nav: [{ text: 'Guide', link: '/fr/guide/introduction' }],
        sidebar: {
          '/fr/guide/': [
            {
              text: 'Introduction',
              items: [
                { text: "Qu'est-ce qu'Outlet ?", link: '/fr/guide/introduction' },
                { text: 'Démarrage', link: '/fr/guide/getting-started' },
              ],
            },
          ],
        },
      },
    },
  },

  themeConfig: {
    socialLinks: [{ icon: 'github', link: github }],
    search: { provider: 'local' },
  },
})
