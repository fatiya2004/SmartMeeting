# Questions d'entretien — réponses courtes à mémoriser

## Choix techniques

**Pourquoi Angular ?**
Framework complet et structuré (routing, formulaires, HTTP inclus), en TypeScript, très utilisé
en entreprise, et qui s'accorde bien avec un backend .NET.

**Pourquoi .NET 9 ?**
Performant, typé, avec Entity Framework Core et une sécurité JWT intégrée. C'est aussi une
technologie très demandée dans les structures publiques.

**Pourquoi PostgreSQL ?**
Base relationnelle gratuite et robuste, avec de vraies transactions — indispensables pour
empêcher deux réservations simultanées de la même salle.

**Pourquoi REST ?**
Simple, standard, testable avec Swagger, et utilisable par n'importe quel client.

**Pourquoi pas de microservices ?**
Le projet a un seul domaine métier et un seul développeur. Les microservices ajouteraient de la
complexité (réseau, déploiement, cohérence) sans bénéfice ici. Un monolithe bien découpé suffit.

## Sécurité

**C'est quoi JWT ?**
Un jeton signé contenant l'identifiant et le rôle de l'utilisateur. Le serveur ne stocke pas de
session : il vérifie la signature à chaque requête.

**Authentification vs autorisation ?**
L'authentification répond à « qui es-tu ? » (login). L'autorisation répond à « as-tu le droit ? » (rôle).

**Comment gérer les rôles ?**
`[Authorize(Roles = "Admin,Secretaire")]` côté API, et `roleGuard` côté Angular. Le guard Angular
n'est qu'un confort d'interface : la vraie protection est côté serveur.

**Pourquoi hacher le mot de passe ?**
Avec BCrypt, même en cas de fuite de la base, les mots de passe ne sont pas lisibles. Le hachage
est à sens unique, et BCrypt ajoute un sel contre les attaques par dictionnaire.

## Données

**C'est quoi EF Core ?**
Un ORM : il traduit les objets C# en SQL. `context.Meetings.Where(...)` devient une requête SELECT.

**C'est quoi un DTO ?**
Un objet d'échange. Il évite d'exposer les entités : `UserDto` ne contient pas le mot de passe.

**Pourquoi une table `MeetingParticipant` ?**
La relation entre réunion et utilisateur est « plusieurs à plusieurs » et porte une information
supplémentaire : le statut de réponse. Il faut donc une table intermédiaire.

## Logique métier

**Comment détecter un conflit ?**
Deux créneaux se chevauchent si `début1 < fin2 ET début2 < fin1`. On applique ce test à la salle,
aux participants et aux indisponibilités déclarées.

**Comment empêcher une double réservation ?**
La vérification et l'insertion sont dans une même transaction. La deuxième demande simultanée
échoue au moment de la validation.

**Comment gérer une réunion urgente ?**
Le système identifie le conflit et signale que la réunion normale peut être déplacée.
La secrétaire valide, le backend déplace la réunion vers le premier créneau libre et notifie tout le monde.
Rien n'est jamais déplacé automatiquement.

**Comment trouver un créneau alternatif ?**
Le backend teste des créneaux de 30 en 30 minutes, sur la journée puis les 4 jours suivants,
et retient les trois premiers où participants et salle sont libres.

## Intelligence artificielle

**C'est quoi un LLM ?**
Un modèle de langage entraîné à comprendre et produire du texte. Ici, il transforme une phrase
libre en données structurées.

**Pourquoi Structured Outputs ?**
Sans contrainte, le LLM répond en texte libre, impossible à exploiter de façon fiable.
Avec un schéma JSON strict, la réponse est toujours au même format.

**C'est quoi Tool Calling ?**
Le LLM n'exécute rien lui-même : il demande l'exécution d'outils définis par le backend
(vérifier une salle, chercher un créneau). Le backend contrôle chaque appel.

**Pourquoi le LLM ne doit-il pas accéder à la base ?**
Trois raisons : la sécurité (un prompt malveillant pourrait détruire des données), la fiabilité
(un LLM peut inventer), et la responsabilité (les règles métier doivent être vérifiables et testables).

**Et si le LLM se trompe ?**
Ce n'est pas grave : il ne fait que proposer. Le backend revérifie tout, et la secrétaire décide.
La qualité de l'analyse est mesurée sur un jeu de demandes de test.

**Que se passe-t-il sans clé API ?**
L'application fonctionne entièrement, seul l'assistant IA est désactivé et l'indique clairement.

## Infrastructure

**Pourquoi Docker ?**
Pour que l'application s'exécute de la même façon partout. Plus de « ça marche sur ma machine ».

**C'est quoi Docker Compose ?**
Un fichier qui décrit plusieurs conteneurs et leurs liens, démarrés en une commande :
`docker compose up --build`.

**À quoi sert MailHog ?**
C'est un faux serveur SMTP pour le développement : on teste l'envoi d'emails sans jamais
envoyer de vrai message.

**Qu'est-ce que fait la CI ?**
À chaque push, GitHub Actions compile le backend, exécute les tests, compile le frontend
et construit les images Docker. Une erreur est détectée avant la mise en production.

## Projet

**Comment testez-vous l'application ?**
Tests unitaires xUnit sur les règles métier (conflits, authentification, salles), tests manuels
de l'API via Swagger, et vérification des emails dans MailHog.

**Quelle est la partie la plus difficile ?**
La détection des conflits : il faut couvrir trois dimensions (salle, participants, indisponibilités)
et garantir qu'aucune double réservation ne passe, même en cas de demandes simultanées.

**Qu'amélioreriez-vous avec plus de temps ?**
La délégation du rôle de secrétaire pendant les congés, un LLM hébergé localement pour la
confidentialité des données de santé, et le monitoring avec Prometheus et Grafana.
