# SmartMeeting CHU — Gestion intelligente des réunions

Application web de gestion des réunions et des salles pour un établissement hospitalier,
avec assistant IA (langage naturel), détection des conflits et notifications par email.

## Pile technique

| Couche        | Technologie                                |
|---------------|--------------------------------------------|
| Frontend      | Angular, TypeScript, Reactive Forms        |
| Backend       | .NET 9 Web API, C#, JWT                    |
| Accès données | Entity Framework Core                      |
| Base          | PostgreSQL 16                              |
| IA            | LLM + Structured Outputs + Tool Calling    |
| Infra         | Docker, Docker Compose, GitHub Actions     |

## Démarrage rapide

```bash
cp .env.example .env
# éditer .env (au minimum JWT_SECRET)
docker compose up --build
```

| Service          | URL                             |
|------------------|---------------------------------|
| Application      | http://localhost:4200           |
| API (Swagger)    | http://localhost:8080/swagger   |
| Boîte mail test  | http://localhost:8025           |
| PostgreSQL       | localhost:5432                  |

Pour activer l'assistant IA, renseignez `LLM_API_KEY` dans `.env`. Sans cette clé, toute
l'application fonctionne et seul l'assistant affiche un message d'indisponibilité.

## Comptes de démonstration

Mot de passe commun : `Password123!`

| Rôle        | Email                          |
|-------------|--------------------------------|
| Admin       | admin@chu-demo.ma              |
| Secrétaire  | secretaire@chu-demo.ma         |
| Participant | ahmed.benali@chu-demo.ma       |
| Participant | sara.amrani@chu-demo.ma        |

## Tester l'envoi d'emails

1. Se connecter via `POST /api/auth/login` avec le compte admin.
2. Copier le token et l'utiliser dans Swagger (bouton **Authorize**).
3. Appeler `POST /api/email-test?to=test@exemple.ma`.
4. Ouvrir http://localhost:8025 : l'email apparaît dans MailHog.

## Structure

```
SmartMeeting/
├── Backend/
│   ├── Controllers/     endpoints REST
│   ├── Services/        logique métier
│   ├── DTOs/            objets échangés avec le client
│   ├── Models/          entités de la base
│   ├── Data/            DbContext + données de démonstration
│   ├── Email/           service SMTP et modèles d'emails
│   └── Program.cs       configuration de l'application
│   └── AI/              client LLM, outils et assistant
├── Frontend/
│   └── src/app/
│       ├── core/        services, modèles, garde et intercepteur
│       ├── layout/      sidebar et navbar
│       └── pages/       login, dashboard, réunions, assistant IA, statistiques…
├── Tests/               tests xUnit (conflits, authentification, salles)
├── docs/                documentation et questions d'entretien
├── .github/workflows/   pipeline CI
├── docker-compose.yml
└── .env.example
```

## Fonctionnalités

- [x] Authentification JWT, rôles Admin / Secrétaire / Participant
- [x] CRUD salles et gestion des utilisateurs
- [x] Réunions : création, modification, annulation, invitations et réponses
- [x] Indisponibilités : garde, bloc opératoire, consultation, congé
- [x] Détection des conflits (salle, participant, indisponibilité, capacité)
- [x] Créneaux alternatifs expliqués
- [x] Réunions urgentes avec arbitrage de la secrétaire
- [x] Emails (invitation, confirmation, refus, modification) et rappels automatiques
- [x] Assistant IA : langage naturel, Structured Outputs, Tool Calling
- [x] Procès-verbaux générés par l'IA
- [x] Tableau de bord et statistiques
- [x] Docker Compose, tests xUnit, GitHub Actions

## Documentation

- `docs/DOCUMENTATION.md` — architecture, flux, règles métier, démonstration
- `docs/QUESTIONS-ENTRETIEN.md` — questions de jury avec réponses courtes
