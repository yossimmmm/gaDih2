# Use Cases

## UC1 - Login
- Actor: Registered user
- Flow: enter email/password -> backend validates -> session cookie created -> navigate to menu.

## UC2 - Role-Based Navigation
- Actor: Authenticated user
- Rules:
  - `User`: gameplay/stat pages.
  - `Manager`: user permissions + room creation.
  - `Admin`: all permissions + user role management.

## UC3 - Forgot Password
- Actor: User who forgot password
- Flow: submit email -> token generated -> email sent -> open reset page -> set new password.

## UC4 - Admin Role Management
- Actor: Admin
- Flow: open admin users page -> select new role -> save role update.

## UC5 - Mobile Health Check
- Actor: Mobile user
- Flow: set backend URL -> click "Check Backend Health" -> native app calls `/api/health` and shows status.
