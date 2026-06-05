# registry/ — source de vérité du catalogue

Chaque item vit dans `registry/<concern>/<item>/` :

- du **vrai code compilable et testé** (le manifeste ne ment jamais) ;
- un manifeste explicite `<item>.registry.json` (`name`, `type` = `outlet:contract` | `outlet:adapter`,
  `targetFrameworks`/`minTfm`, `registryDependencies`, `nugetDependencies`, `files`).

Le manifeste agrégé publié est **généré en CI** vers `dist/registry/` — jamais édité à la main.

**Vide pour l'instant** : la v1 cible la préoccupation `email` uniquement
(`email-abstractions`, `email-smtp`, `email-sendgrid`) — voir Linear, projet « Outlet — MVP ».
