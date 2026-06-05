# samples/ — démos

Démo cible v1 : une app exemple où le swap SMTP ↔ SendGrid se résume à changer
**une ligne de DI** (`AddSmtpEmail()` ↔ `AddSendGridEmail()`) derrière le même
`IEmailSender`.

Vide tant que le contenu email du registre n'existe pas (Linear « Outlet — MVP »).
