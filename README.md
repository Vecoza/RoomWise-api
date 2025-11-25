# RoomWise API

Booking API with reservations, payments (Stripe), availability, promotions, wishlist, loyalty, and admin stats.

## Quick start (Docker)

1. Create/verify `RoomWise.Api/.env` with required variables (see below).
2. From repo root: `docker compose up -d --build`
3. API docs (Scalar): http://localhost:5184/scalar

## Environment variables (`RoomWise.Api/.env`)

Required:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:5184`
- `ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=RoomWise;Username=postgres;Password=YOUR_PASS` (inside Docker)  
  For local `dotnet run`: use `Host=localhost;Port=5433;...`.
- `Jwt:Key=YourSuperSecretKeyForJWTTokenGeneration-MustBeAtLeast32CharactersLong!`
- `Jwt:Issuer=RoomWiseApi`
- `Jwt:Audience=RoomWiseMobile`
- `STRIPE__SECRETKEY=sk_test_xxx`
- `STRIPE__WEBHOOKSECRET=whsec_xxx`
- `AdminUser:Email=admin@example.com`
- `AdminUser:Password=ChangeMe123!`

## Demo credentials

- Guest (seeded): `vecaTest@gmail.com` / `VecaTest123!`
- Admin: set via `AdminUser:*` in `.env` (created on startup)

## Local dev (without Docker)

1. Ensure Postgres is running on `localhost:5433` with DB `RoomWise`, user/pass matching your `.env`.
2. `dotnet restore`
3. Run: `dotnet watch run --project RoomWise.Api`  
   Docs at http://localhost:5184/scalar

## Useful endpoints

- Reservations: `/api/reservations/...` (create with payment intent, my list, cancel)
- Payments: `/api/payments/intent`, `/api/payments/webhook` (Stripe)
- Loyalty: `/api/loyalty/balance`, `/api/loyalty/history`
- Wishlist: `/api/wishlist` (POST/DELETE/GET)
- Stats (admin): `/api/admin/stats/overview`, `/revenue-by-month`, `/top-hotels`, `/top-users`

## Notes

- Builds output to `bin/`/`obj/` are ignored by Git.
- Stripe keys must be present or the API will fail on startup.
- Scalar UI requires the API to be running and DB reachable.\*\*\*
