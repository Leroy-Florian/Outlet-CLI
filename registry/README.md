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

## Contenu actuel — concern `resilience`

| Item | Type | Provider | DI |
|---|---|---|---|
| `resilience-abstractions` | contract | — (zéro dépendance) | — |
| `resilience-polly` | adapter | Polly v8 | `AddPollyResilience(...)` |
| `resilience-microsoft` | adapter | Microsoft.Extensions.Resilience | `AddMicrosoftResilience(...)` |
| `resilience-builtin` | adapter | — (zéro dépendance, fait main) | `AddBuiltInResilience(...)` |

Le port générique `IResilienceExecutor` enveloppe une opération arbitraire avec les stratégies
configurées (retry / timeout / circuit breaker) — il est **composé par-dessus** un autre port
(ex. `IEmailSender`), jamais embarqué dans son adapter (principe #9). Les trois adapters lient
les **mêmes** `ResilienceOptions` et mappent l'ouverture du circuit vers le **même** type
`ResilienceRejectedException` → swap en une ligne, swappabilité **testée** par la suite de
conformance de port. `resilience-polly` illustre le « spécifique à côté du générique » :
`IPollyResilienceExecutor` (accès au `ResiliencePipeline` brut) implémenté par la même instance.
`resilience-builtin` incarne l'ownership : retry + timeout + circuit breaker écrits main, **zéro
package**.
## Contenu actuel — concern `cache`

| Item | Type | Provider | DI |
|---|---|---|---|
| `cache-abstractions` | contract | — (zéro dépendance) | — |
| `cache-memory` | adapter | Microsoft.Extensions.Caching.Memory | `AddInMemoryCache(...)` |
| `cache-redis` | adapter | StackExchange.Redis | `AddRedisCache(...)` |
| `cache-memcached` | adapter | EnyimMemcachedCore | `AddMemcachedCache(...)` |

Le port générique `ICacheStore` (octets opaques + `CacheEntryOptions` à TTL absolu — la seule
politique d'expiration que **tous** les backends honorent à l'identique) est commun aux trois
adapters → swap en **une ligne** de DI. La (dé)sérialisation reste une préoccupation séparée,
composée par-dessus le port, jamais embarquée dedans (comme la résilience). `cache-redis`
illustre le pattern « spécifique à côté du générique » : `IRedisCacheStore` (compteurs atomiques
`INCR`) implémenté par la même instance, forwardée vers les deux interfaces.

`cache-memory` est la **base hermétique** (aucun serveur) qui rejoue la suite de conformité de
port dans la lane PR ; les comportements réels de `cache-redis`/`cache-memcached` sont vérifiés
contre un vrai serveur dans la lane planifiée (`Category=Live`, endpoints surchargeables via
`OUTLET_REDIS` / `OUTLET_MEMCACHED`).
## Contenu actuel — concern `storage`

Stockage d'objets/blobs derrière un port générique unique `IBlobStorage`
(`PutAsync` / `GetAsync` / `ExistsAsync` / `DeleteAsync` / `ListAsync`).

| Item | Type | Provider | DI |
|---|---|---|---|
| `storage-abstractions` | contract | — (zéro dépendance) | — |
| `storage-in-memory` | adapter | — (zéro dépendance) | `AddInMemoryBlobStorage(...)` |
| `storage-filesystem` | adapter | — (système de fichiers, zéro dépendance) | `AddFileSystemBlobStorage(...)` |
| `storage-s3` | adapter | AWSSDK.S3 | `AddS3BlobStorage(...)` |
| `storage-azure-blob` | adapter | Azure.Storage.Blobs | `AddAzureBlobStorage(...)` |

Le port générique est identique pour les quatre adapters → swap en **une ligne** de DI.
`storage-s3` et `storage-azure-blob` illustrent le pattern « spécifique à côté du générique » :
`IS3BlobStorage` (URL présignée) et `IAzureBlobStorage` (SAS URI), chacun implémenté par la
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
