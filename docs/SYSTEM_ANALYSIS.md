# Trivia Game - System Analysis

## Existing Problem
Manual trivia games are hard to manage for multiple players, score tracking is inconsistent, and there is no central statistics history.

## Proposed System
A client-server trivia platform with:
- Web client for full gameplay and account management.
- MAUI mobile client for phone access + native backend health check.
- ASP.NET Core backend with API, SignalR, and MySQL data layer.

## Goals
1. Real-time room-based gameplay.
2. Authenticated users with role-based permissions (`User`, `Manager`, `Admin`).
3. Persistent game results and statistics.
4. Password reset by secure token and email.

## Main Constraints
- Requires MySQL server and SMTP configuration.
- Requires network access from mobile device to backend host.
