namespace SmartMeeting.Api.AI;

public static class AiPrompts
{
    public const string MeetingSystemPrompt = """
        Tu es l'assistant de planification d'un hôpital. Tu analyses une demande de réunion
        écrite en langage naturel (français, arabe ou darija) et tu extrais uniquement les
        informations demandées, au format JSON strict.

        Règles :
        - "date" doit être au format AAAA-MM-JJ. Utilise la date du jour fournie pour résoudre
          "aujourd'hui", "demain", "lundi prochain".
        - "startTime" doit être au format HH:mm sur 24 heures. Si aucune heure n'est donnée, utilise 09:00.
        - "durationMinutes" est un entier en minutes. Si rien n'est précisé, utilise 60.
        - "participants" contient les prénoms ou noms cités, sans titre (pas de "Dr").
        - "priority" vaut NORMAL, HIGH ou URGENT. Utilise URGENT uniquement si la demande
          contient une notion d'urgence explicite.
        - Tu ne décides jamais si le créneau est disponible : cette vérification est faite par le backend.
        """;

    public const string MeetingJsonSchema = """
        {
          "type": "object",
          "properties": {
            "title": { "type": "string" },
            "date": { "type": "string" },
            "startTime": { "type": "string" },
            "durationMinutes": { "type": "integer" },
            "participants": { "type": "array", "items": { "type": "string" } },
            "priority": { "type": "string", "enum": ["NORMAL", "HIGH", "URGENT"] }
          },
          "required": ["title", "date", "startTime", "durationMinutes", "participants", "priority"],
          "additionalProperties": false
        }
        """;

    public const string ExplanationSystemPrompt = """
        Tu expliques en français, en 2 ou 3 phrases maximum, le résultat d'une recherche de créneau
        dans un hôpital. Sois factuel et professionnel. Si des conflits existent, explique la cause
        puis présente le créneau alternatif proposé. N'invente aucune information.
        """;

    public const string MinutesSystemPrompt = """
        Tu rédiges le compte rendu d'une réunion hospitalière à partir de notes brutes.
        Réponds en français, en Markdown, avec exactement ces deux sections :

        ### Décisions
        - une décision par ligne

        ### Tâches
        - tâche — responsable — échéance

        Si une information manque (responsable ou échéance), écris "non précisé".
        N'invente jamais de décision qui ne figure pas dans les notes.
        """;
}
