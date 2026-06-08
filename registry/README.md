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

## Contenu actuel — concern `storage`

Stockage d'objets/blobs derrière un port générique unique `IObjectStorage`
(`PutAsync` / `GetAsync` / `ExistsAsync` / `DeleteAsync` / `ListAsync`).

| Item | Type | Provider | DI |
|---|---|---|---|
| `storage-abstractions` | contract | — (zéro dépendance) | — |
| `storage-in-memory` | adapter | — (zéro dépendance) | `AddInMemoryObjectStorage(...)` |
| `storage-filesystem` | adapter | — (système de fichiers, zéro dépendance) | `AddFileSystemObjectStorage(...)` |
| `storage-s3` | adapter | AWSSDK.S3 | `AddS3ObjectStorage(...)` |
| `storage-azure-blob` | adapter | Azure.Storage.Blobs | `AddAzureBlobStorage(...)` |

Le port générique est identique pour les quatre adapters → swap en **une ligne** de DI.
`storage-s3` et `storage-azure-blob` illustrent le pattern « spécifique à côté du générique » :
`IS3ObjectStorage` (URL présignée) et `IAzureBlobStorage` (SAS URI), chacun implémenté par la
même instance, forwardée vers les deux interfaces.

Conventions du port (identiques entre adapters) : une **clé** est un identifiant opaque,
sensible à la casse, de type chemin (`invoices/2026/03.pdf`) ; l'**absence n'est pas une erreur**
(`GetAsync` → `null`, `DeleteAsync` → `false`) ; les vraies fautes d'I/O remontent en exception.

Tests : `storage-in-memory` et `storage-filesystem` rejouent toute la suite de conformité de port
de façon **hermétique** (lane PR) ; `storage-s3` et `storage-azure-blob` la rejouent contre un vrai
endpoint (MinIO / Azurite) dans la **lane Live nightly** (skippée si l'endpoint n'est pas configuré).

## Convention `nugetDependencies`

Un item déclare uniquement les packages **qu'il introduit** (la lib provider : MailKit,
SendGrid…). L'infrastructure DI/Options (`Microsoft.Extensions.DependencyInjection`,
`Microsoft.Extensions.Options`) est **fournie par l'hôte** : on appelle `AddXxx(...)` sur
*votre* `IServiceCollection`, vous l'avez donc déjà. Les pinner à un plancher provoquerait
un downgrade (NU1605) chez les hôtes qui en ont une version plus récente.
