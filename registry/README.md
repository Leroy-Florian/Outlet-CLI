# registry/ — source de vérité du catalogue

Chaque item vit dans `registry/<concern>/<item>/` :

- du **vrai code compilable et testé** (le manifeste ne ment jamais) ;
- un manifeste explicite `<item>.registry.json` validé contre `registry/registry-item.schema.json`
  (`name`, `type` = `outlet:contract` | `outlet:adapter`, `concern`, `targetFrameworks`,
  `registryDependencies`, `nugetDependencies`, `files`).

Le manifeste agrégé publié est **généré en CI** vers `dist/registry/` — jamais édité à la main (HIJ-498).

## Convention de namespace

Le code des items utilise le namespace racine canonique `Outlet.Registry.<Concern>`
(ex. `Outlet.Registry.Email`). À l'installation, il est **réécrit** vers le namespace
du projet cible par Roslyn (HIJ-493) — jamais par find/replace.

## Comment les items sont compilés + testés

Les sources des items ne forment pas un projet livré : elles sont compilées **directement**
par un harnais de tests (`tests/Outlet.Registry.Email.Tests`) via `<Compile Include="…">`,
qui référence les packages provider et exécute des tests hermétiques (contrat + DI).
C'est ce qui garantit que le manifeste ne ment pas. La vérification **multi-TFM** des
`targetFrameworks` déclarés est portée par HIJ-509 ; les niveaux de tests adapters
(frontière HTTP / émulateur / live) par HIJ-512.

## Contenu actuel — concern `email`

| Item | Type | Provider | DI |
|---|---|---|---|
| `email-abstractions` | contract | — (zéro dépendance) | — |
| `email-smtp` | adapter | MailKit | `AddSmtpEmail(...)` |
| `email-sendgrid` | adapter | SendGrid | `AddSendGridEmail(...)` |

Le port générique `IEmailSender` est identique pour les deux adapters → swap en **une ligne**
de DI. `email-sendgrid` illustre le pattern « spécifique à côté du générique » :
`ISendGridEmailSender` (templates dynamiques) implémenté par la même instance, forwardée
vers les deux interfaces.

## Convention `nugetDependencies`

Un item déclare uniquement les packages **qu'il introduit** (la lib provider : MailKit,
SendGrid…). L'infrastructure DI/Options (`Microsoft.Extensions.DependencyInjection`,
`Microsoft.Extensions.Options`) est **fournie par l'hôte** : on appelle `AddXxx(...)` sur
*votre* `IServiceCollection`, vous l'avez donc déjà. Les pinner à un plancher provoquerait
un downgrade (NU1605) chez les hôtes qui en ont une version plus récente.
