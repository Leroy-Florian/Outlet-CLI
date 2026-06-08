# Qu'est-ce qu'Outlet ?

Outlet est un **registre de code « copier-coller » pour l'infrastructure backend en
.NET** — la même idée que [shadcn/ui](https://ui.shadcn.com/), mais pour des
préoccupations backend comme l'envoi d'email, le cache, la résilience ou le storage.

Idée centrale : pour chaque **préoccupation**, on expose un **port générique** (une
interface métier minimale) et plusieurs **adapters interchangeables** (un par
provider / lib). Vous **copiez le code dans votre repo et le possédez** : vous pouvez
changer de provider derrière le même port et éditer le code librement.

> **Ce n'est PAS une librairie consommée en dépendance.** C'est du code qu'on
> s'approprie. Métaphore du nom : une *prise* (le port) dans laquelle on *branche* un
> provider (l'adapter).

## Principes de design

- **Ownership / copier-coller** — aucune dépendance runtime à Outlet. Désinstaller Outlet
  ne casse rien chez vous.
- **Séparation contrat / adapter** — le contrat (port + DTOs) a zéro dépendance externe et
  vit côté application/domaine ; l'adapter dépend du contrat plus de la lib provider.
  L'app ne référence jamais l'adapter concret.
- **Port générique minimal et identique** entre adapters — c'est ce qui garantit la
  swappabilité. Aucune spécificité provider ne fuite dans le port générique.
- **Le spécifique à côté du générique** — une feature spécifique à un provider passe par
  une 2ᵉ interface dédiée (même classe adapter) ou par l'édition de votre copie — jamais
  dans le port commun.
- **DI explicite** — chaque adapter fournit une extension `AddXxx()`. Pas de scan
  d'assembly.
- **Adapters minces** — la résilience (retry / circuit breaker) est une préoccupation
  séparée composée *par-dessus* le port, jamais embarquée dans l'adapter.

## Périmètre du v1

La première tranche cible **l'email uniquement** : un port (`IEmailSender`) et deux
adapters (par exemple SMTP et SendGrid). L'objectif : que changer de provider se résume à
modifier une seule ligne `AddXxx()`.

## Distribution

- Un **registre distant** (un manifeste plus des fichiers servis en HTTP), conçu
  multi-sources dès le départ pour permettre des registres privés d'entreprise plus tard.
- Une **CLI** (`outlet`, un `dotnet tool` global) au-dessus d'un core engine réutilisable.
- Un **manifeste JSON explicite par item** (`*.registry.json`), validé et généré en CI :
  le manifeste ne ment jamais sur le contenu d'un item.

Continuez avec [Démarrage](/fr/guide/getting-started).
