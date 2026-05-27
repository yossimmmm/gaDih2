# ERD (Entity Relationship Diagram)

```mermaid
erDiagram
    users ||--o{ user_sessions : has
    users ||--o{ password_reset_tokens : has
    users ||--o{ room_players : joins
    users ||--o{ game_results : owns
    rooms ||--o{ room_players : contains
    rooms ||--o{ room_questions : contains
    rooms ||--o{ game_results : has
    questions ||--o{ question_options : has
    questions ||--o{ room_questions : used_in
    question_types ||--o{ questions : categorizes
    room_questions ||--o{ player_answers : answered_in
    room_players ||--o{ player_answers : answers
```

## Notes
- `users.role` stores authorization level: `User`, `Manager`, `Admin`.
- `room_players` is a linking table between users and rooms.
- `room_questions` links a game room to selected questions.
