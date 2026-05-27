# Final 100 Checklist (Exam Submission)

Use this checklist before the exam so the project is graded as complete.

## A) Must Work Live

- [ ] Backend runs: `dotnet run --project TriviaGame\TriviaGame.csproj --launch-profile http`
- [ ] Web opens: `http://localhost:5038/login`
- [ ] DB connection works (login/register/rooms/stats load real data)
- [ ] Forgot password sends email and reset link works end-to-end
- [ ] Multiplayer flow works: create room -> join room -> play -> results
- [ ] Admin flow works: open `/admin-users`, update user, delete user, role change

## B) Required Academic Features

- [ ] Client-server architecture (ASP.NET Core + web client)
- [ ] Async server code (`async/await`) in data + APIs
- [ ] OOP model classes (`ModelsTrivia`)
- [ ] Role permissions (`User`, `Manager`, `Admin`) with role-based UI
- [ ] Stateless web service endpoints (HTTP APIs)
- [ ] Full CRUD demonstrated in services:
  - [ ] Create (register, room creation)
  - [ ] Read (users/rooms/stats)
  - [ ] Update (profile/role/admin user update)
  - [ ] Delete (admin delete user, room cleanup)
- [ ] SQL injection protection (parameterized queries)
- [ ] Delegate/event usage shown (`AuthAuditDispatcher.OnAuditAsync`)
- [ ] AI service integration (Gemini assistant)

## C) Multi-Platform Evidence

- [ ] Web client demoed in browser
- [ ] MAUI mobile client demoed (WebView + native health check)
- [ ] If possible: show emulator/device screenshot as proof

## D) Project Book Package

- [ ] `docs/PROJECT_BOOK_HE.docx` updated with final content
- [ ] Cover page complete (school, student, ID, mentor, date, track)
- [ ] Auto Table of Contents
- [ ] Header/Footer + page numbers
- [ ] Font matches instruction (Arial or David, size 12 body)
- [ ] Include all required chapters:
  - [ ] Introduction
  - [ ] System analysis
  - [ ] Database
  - [ ] Implementation (server/client)
  - [ ] User guide
  - [ ] Reflection
  - [ ] Bibliography (APA)
  - [ ] Appendices

## E) Required Screenshots (put inside the book)

- [ ] Login
- [ ] Register
- [ ] Forgot password
- [ ] Reset password
- [ ] Main menu (different role views if possible)
- [ ] Rooms list
- [ ] Create room
- [ ] Lobby
- [ ] Play screen
- [ ] Results
- [ ] Stats
- [ ] Top players
- [ ] Assistant
- [ ] Admin users page
- [ ] MAUI screen + health check

## F) Demo Day Quick Script

1. Start MySQL.
2. Run backend.
3. Open web login.
4. Show one user flow + one admin flow.
5. Show MAUI client opening backend.
6. Keep project book open and jump to diagrams + code appendix.

