# docs/

This folder is the **Outlet documentation site**, built with
[VitePress](https://vitepress.dev/) and deployed to GitHub Pages.

## Layout

```
docs/
  .vitepress/config.ts     ← site config (i18n: EN default, FR under /fr/)
  index.md                 ← EN home (hero)
  guide/                   ← EN guide pages
  fr/                      ← FR mirror (home + guide)
  testing.md               ← contributor doc (linked in the EN sidebar)
  production-readiness.md  ← contributor doc (linked in the EN sidebar)
  README.md                ← this file (excluded from the built site)
```

English is the default locale (served at `/`); French is served under `/fr/`.

## Commands (run from the repo root)

```bash
npm run docs:dev       # local dev server with hot reload
npm run docs:build     # static build into docs/.vitepress/dist
npm run docs:preview   # preview the built site
```

The site is built on every pull request and deployed to GitHub Pages on push to
`master` (see `.github/workflows/docs.yml`). The base path defaults to `/Outlet-CLI/`
and can be overridden with the `DOCS_BASE` environment variable.

> The interactive playground (post-v1) is a separate concern — it lives in
> `playground/web` and will consume `@outlet/hateoas` and `@outlet/effect-react`.
> Project conventions live in `CLAUDE.md` (root) and `.specify/memory/constitution.md`.
