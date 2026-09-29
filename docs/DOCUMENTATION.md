# Documentation — SmartMeeting CHU

Document destiné à comprendre et défendre le projet.

---

## 1. Vue d'ensemble

SmartMeeting est une application web de gestion des réunions hospitalières. Un médecin décrit
sa réunion (par formulaire ou en langage naturel), le backend vérifie les disponibilités réelles,
et la secrétaire valide ou refuse la demande. Les participants sont prévenus par email.

**Flux classique**

```
Utilisateur → Angular → API REST (.NET) → Service métier → EF Core → PostgreSQL
```

**Flux IA**

```
Utilisateur → Angular → API .NET → LLM (Structured Outputs)
                               ↓
                        Tool Calling
                               ↓
                     Services métier .NET → EF Core → PostgreSQL
```

Le LLM ne touche jamais la base : il propose, le backend vérifie et décide.

---

## 2. Les briques techniques

### Angular
Framework frontend. Il affiche les pages et appelle l'API. Aucune règle métier ne s'y trouve :
un utilisateur pourrait modifier le code du navigateur, donc les vérifications sont côté serveur.

Fichiers clés : `Frontend/src/app/core/api.service.ts` (appels HTTP),
`auth.interceptor.ts` (ajout du token), `auth.guard.ts` (protection des routes).

### API REST
Une API REST expose des adresses (`/api/meetings`) et des verbes HTTP :
GET pour lire, POST pour créer, PUT pour modifier, DELETE pour supprimer.
Elle renvoie du JSON, ce qui la rend utilisable par n'importe quel client.

### Controller
Porte d'entrée HTTP. Il lit la requête, appelle le service, renvoie le code HTTP adapté
(200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 409 Conflict).
Il ne contient aucune règle métier.

### Service
Contient la logique métier : création d'une réunion, détection des conflits, validation.
Exemple : `AvailabilityService` est le cœur du projet.

### DTO (Data Transfer Object)
Objet d'échange entre l'API et le client. Il protège les données sensibles :
`User` contient `PasswordHash`, mais `UserDto` ne le contient pas.

### Entity et EF Core
Une entité est une classe C# qui correspond à une table (`Meeting` ↔ table `Meetings`).
Entity Framework Core est un ORM : il traduit le C# en SQL, ce qui évite d'écrire les requêtes à la main.

### DbContext
Classe centrale d'EF Core (`AppDbContext`). Elle déclare les tables (`DbSet`) et les relations
(clés étrangères, clé composite de `MeetingParticipant`, cascades).

### PostgreSQL
Base de données relationnelle, choisie pour sa robustesse, sa gratuité et son support
des transactions, nécessaires pour empêcher les doubles réservations.

### JWT (JSON Web Token)
Jeton signé renvoyé à la connexion. Il contient l'identifiant et le rôle de l'utilisateur.
Le client le renvoie à chaque requête dans l'en-tête `Authorization: Bearer <token>`.
L'API vérifie la signature : aucune session n'est stockée côté serveur.

### Docker et Docker Compose
Docker met chaque composant dans un conteneur avec ses dépendances.
Docker Compose décrit les quatre conteneurs (base, API, frontend, MailHog) et les démarre ensemble
avec `docker compose up --build`.

### LLM, Structured Outputs, Tool Calling
- **LLM** : modèle de langage capable de comprendre une phrase libre.
- **Structured Outputs** : on impose au LLM un schéma JSON strict, donc la réponse est exploitable par le code.
- **Tool Calling** : le LLM ne fait rien seul ; il demande l'exécution d'outils définis par le backend
  (`check_room_availability`, `suggest_alternative_slots`, `create_meeting`), et c'est le backend qui exécute.

---

## 3. Les règles métier

### Détection des conflits (`AvailabilityService`)
Deux créneaux se chevauchent si `débutA < finB ET débutB < finA`. Trois vérifications :

1. **Conflit de salle** : la salle est déjà réservée sur un créneau qui se chevauche.
2. **Conflit de participant** : un participant est déjà dans une autre réunion.
3. **Indisponibilité** : garde, bloc opératoire, consultation ou congé déclaré.

S'y ajoute un contrôle de capacité de la salle.

### Double réservation
La création d'une réunion se fait dans une **transaction** : la vérification et l'insertion
forment un bloc unique. Une seconde demande simultanée échoue.

### Réunion urgente
Une réunion URGENT en conflit avec une réunion NORMAL peut la déplacer, mais uniquement
après validation de la secrétaire (`POST /api/meetings/{urgentId}/preempt/{normalId}`).
La réunion normale est déplacée vers le premier créneau libre, ou annulée s'il n'y en a aucun.

### Créneaux alternatifs
Le backend balaie la journée puis les 4 jours suivants par tranches de 30 minutes, et renvoie
les 3 premiers créneaux où tous les participants et une salle sont libres.

---

## 4. Rôles et autorisations

| Rôle | Droits |
|------|--------|
| ADMIN | Utilisateurs, salles, statistiques, tout ce que fait la secrétaire |
| SECRETAIRE | Consulter, accepter, refuser, modifier les demandes, arbitrer les urgences |
| PARTICIPANT | Créer une réunion, répondre aux invitations, déclarer ses indisponibilités |

Techniquement : `[Authorize(Roles = "Admin,Secretaire")]` sur les controllers,
et `roleGuard(...)` sur les routes Angular. La protection serveur est la seule qui compte.

---

## 5. Les emails

`SmtpEmailService` (MailKit) envoie cinq types d'emails : invitation, confirmation, refus,
modification, rappel. `NotificationService` enregistre d'abord la notification en base, puis envoie.
Si l'envoi échoue, l'opération métier réussit quand même et `IsSent` reste à `false`.

En développement, MailHog remplace un vrai serveur SMTP : les emails s'affichent sur
`http://localhost:8025` sans jamais partir sur internet.

Les rappels sont envoyés par `ReminderBackgroundService`, qui vérifie toutes les 5 minutes
les réunions approuvées commençant dans l'heure.

---

## 6. Lancer le projet

```bash
cp .env.example .env     # renseigner JWT_SECRET, puis LLM_API_KEY pour activer l'IA
docker compose up --build
```

| Service | URL |
|---------|-----|
| Application | http://localhost:4200 |
| API Swagger | http://localhost:8080/swagger |
| Emails (MailHog) | http://localhost:8025 |

Comptes de démonstration, mot de passe `Password123!` :
`admin@chu-demo.ma`, `secretaire@chu-demo.ma`, `ahmed.benali@chu-demo.ma`, `sara.amrani@chu-demo.ma`.

Sans `LLM_API_KEY`, toute l'application fonctionne : seul l'assistant IA affiche un message
indiquant qu'il n'est pas configuré.

---

## 7. Tests

```bash
dotnet test Tests/SmartMeeting.Tests.csproj
```

Les tests couvrent l'authentification, le CRUD des salles, le chevauchement des créneaux,
les trois types de conflits et la recherche de créneaux alternatifs. Ils utilisent une base
en mémoire, donc ils ne nécessitent pas PostgreSQL.

---

## 8. Scénario de démonstration (5 minutes)

1. Connexion avec `ahmed.benali@chu-demo.ma`.
2. Déclarer une indisponibilité (garde de 14h à 18h demain).
3. Assistant IA : « Je veux une réunion demain à 14h avec Sara pendant une heure ».
4. L'IA extrait les informations, le backend détecte le conflit de garde et propose un autre créneau.
5. Choisir le créneau alternatif et envoyer la demande.
6. Se connecter avec `secretaire@chu-demo.ma` : la demande apparaît, la valider.
7. Ouvrir MailHog : les emails d'invitation et de confirmation sont visibles.
8. Ouvrir la page Statistiques.
