# samples/ — démos Outlet

## SwapDemo — swap SMTP ↔ SendGrid en une ligne de DI

Une console minimale qui résout le port générique `IEmailSender` et envoie un message.
Le **seul** changement pour passer d'un provider à l'autre est la ligne
`AddSmtpEmail(...)` ↔ `AddSendGridEmail(...)` ; tout le reste du code est identique.

```bash
# SMTP (par défaut)
dotnet run --project samples/SwapDemo -- smtp

# SendGrid — la même appli, un autre adapter
dotnet run --project samples/SwapDemo -- sendgrid
```

Sortie type (sans serveur/clé réels, la livraison échoue proprement — le port et le
swap sont quand même démontrés) :

```
Provider     : smtp
Active adapter: SmtpEmailSender  (behind IEmailSender)
Not delivered (expected without a real server/credentials):
  Connection refused ...
```

### Livrer pour de vrai

- **SMTP** : pointez vers un serveur (ex. [smtp4dev](https://github.com/rnwood/smtp4dev) en local) :
  ```bash
  SMTP_HOST=localhost SMTP_PORT=2525 dotnet run --project samples/SwapDemo -- smtp
  ```
- **SendGrid** :
  ```bash
  SENDGRID_API_KEY=SG.xxxx dotnet run --project samples/SwapDemo -- sendgrid
  ```

### Le point à retenir

Le code applicatif ne dépend que de `IEmailSender` (le **port générique**). Les adapters
(`SmtpEmailSender`, `SendGridEmailSender`) sont interchangeables derrière ce port — c'est
ce que garantit le design d'Outlet. Dans un vrai projet, ces fichiers seraient **copiés
chez vous** par `outlet add email-smtp email-sendgrid` (vous les possédez et pouvez les
éditer) ; ici ils sont compilés depuis `registry/email/` pour garder une source unique.

## CacheSwapDemo — swap In-Memory ↔ Redis ↔ Memcached en une ligne de DI

Même histoire pour le cache : une console qui résout le port générique `ICacheStore`,
écrit une clé puis la relit. Le **seul** changement entre providers est la ligne
`AddInMemoryCache()` ↔ `AddRedisCache(...)` ↔ `AddMemcachedCache(...)`.

```bash
# In-memory (par défaut) — aucun serveur requis, tourne tel quel
dotnet run --project samples/CacheSwapDemo -- memory

# Redis / Memcached — la même appli, un autre adapter
dotnet run --project samples/CacheSwapDemo -- redis
dotnet run --project samples/CacheSwapDemo -- memcached
```

Sortie type (provider `memory`, hermétique) :

```
Provider      : memory
Active adapter: InMemoryCacheStore  (behind ICacheStore)
Cached + read back ✅  "Swapping cache providers is a one-line change."
```

Pour Redis/Memcached réels, surchargez l'endpoint (`REDIS_CONFIGURATION`,
`MEMCACHED_HOST`/`MEMCACHED_PORT`) ou démarrez un serveur local ; sans serveur, l'accès
échoue proprement et le message l'explique. Comme pour l'email, dans un vrai projet ces
fichiers seraient copiés chez vous par `outlet add cache-memory cache-redis cache-memcached`.
