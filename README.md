# RoomWise API

Booking API with reservations, payments (Stripe), availability, promotions, wishlist, loyalty, recommendations, and admin stats.

## Prerequisites

- Docker Desktop (recommended for easy setup)
- .NET SDK 9 (only needed for local run without Docker)
- Stripe CLI (optional, only for local webhook testing)

## Quick start (Docker)

1. Create/verify `RoomWise.Api/.env` (see required variables below).
2. From the repo root, start the stack:
   ```sh
   docker compose up -d --build
   ```
3. Open the API docs (Scalar): http://localhost:5184/scalar

### Services + URLs

- API: http://localhost:5184
- Scalar docs: http://localhost:5184/scalar
- PostgreSQL (host access): `localhost:5433`
- pgAdmin: http://localhost:5050/browser  
  Servers -> Register -> Name(example. roomwise) -> Host name: db -> username: postgres -> password: Vedran55
- RabbitMQ Management UI: http://localhost:15672  
  Login: `guest` / `guest`

## Environment variables (`RoomWise.Api/.env`)

Required:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:5184`
- `ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=RoomWise;Username=postgres;Password=YOUR_PASS`
  - Inside Docker use `Host=db`.
  - For local `dotnet run` use `Host=localhost;Port=5433`.
- `Jwt:Key=YourSuperSecretKeyForJWTTokenGeneration-MustBeAtLeast32CharactersLong!`
- `Jwt:Issuer=RoomWiseApi`
- `Jwt:Audience=RoomWiseMobile`
- `STRIPE__SECRETKEY=sk_test_xxx`
- `STRIPE__WEBHOOKSECRET=whsec_xxx`
- `AdminUser:Email=admin@example.com`
- `AdminUser:Password=ChangeMe123!`

## Demo credentials

- Guest (seeded): `vecaTest@gmail.com` / `VecaTest123!`
- Admin: (seeded) `admin1@roomwise.com` / `HotelAdmin123!`
  The hotel with id 1 is assigned admin1 and so on until the hotel with id 14. For each created hotel with its id, admin[hotel id]@gmail.com is assigned.

## Stripe webhooks (local dev)

To test card payments and update payment status via webhooks:

```sh
stripe listen --forward-to http://localhost:5184/api/payments/webhook
```

Use the `whsec_...` from the Stripe CLI output in `STRIPE__WEBHOOKSECRET`.

## Useful endpoints

### Auth & onboarding

- `POST /api/auth/register` — start registration, sends verification code
- `POST /api/auth/verify-email` — verify code and create account
- `POST /api/auth/request-email-verification` — resend verification code
- `POST /api/auth/login` — login (returns JWT + refresh token)
- `POST /api/auth/refresh` — refresh access token

### Profile & account

- `GET /api/me/profile` — get my profile (auto-creates if missing)
- `PUT /api/me/profile` — update profile
- `POST /api/me/profile/change-password` — change password
- `POST /api/me/profile/avatar` — upload avatar (multipart/form-data)
- `GET /api/me/notifications` — list notifications
- `POST /api/me/notifications/{id}/read` — mark notification as read
- `GET /api/me/payment-methods` — list payment methods
- `POST /api/me/payment-methods` — add payment method
- `DELETE /api/me/payment-methods/{id}` — remove payment method

### Hotels & search

- `GET /api/hotels` — list hotels (admin/general)
- `GET /api/hotels/search` — hotel cards for listings
- `GET /api/hotels/{id}/details` — hotel details + room types
- `GET /api/hotels/hot-deals` — discounted hotels
- `GET /api/search/hotels` — availability search by dates/guests
- `GET /api/hotels/{id}/reviews` — hotel reviews

### Reservations & payments

- `POST /api/reservations` — create reservation (guest)
- `POST /api/reservations/with-payment-intent` — create reservation + Stripe intent
- `GET /api/reservations/my?status=current|past|cancelled` — guest reservations
- `POST /api/reservations/{id}/cancel` — cancel reservation
- `GET /api/reservations/{publicId}` — fetch by publicId
- `POST /api/payments/intent` — create payment intent
- `POST /api/payments/webhook` — Stripe webhook receiver

### Loyalty, wishlist, recommendations

- `GET /api/loyalty/balance` — current loyalty balance
- `GET /api/loyalty/history?page=&pageSize=` — loyalty ledger
- `GET /api/wishlist` — wishlist items
- `POST /api/wishlist/{hotelId}` / `DELETE /api/wishlist/{hotelId}` — manage wishlist
- `GET /api/recommendations?top=10` — personalized recommendations

### Admin reporting & operations

- `GET /api/admin/stats/overview` — totals (users/reservations/revenue)
- `GET /api/admin/stats/revenue-by-month?year=YYYY` — revenue chart
- `GET /api/admin/stats/top-hotels?limit=` — top hotels
- `GET /api/admin/stats/top-users?limit=` — top guests
- `GET /api/reports/reservations-summary?from=&to=&status=` — summary report
- `GET /api/reservations/arrivals?date=YYYY-MM-DD` — today arrivals list
- `GET /api/roomtypes/availability?date=YYYY-MM-DD` — live availability per room type

### Content management (admin)

- Hotels: `/api/hotels` (CRUD, admin scoped)
- Room types: `/api/roomtypes` (CRUD), `/api/roomrates` (CRUD)
- Availability: `/api/roomavailabilities` (CRUD), `/api/roomavailabilities/batch-upsert`
- Media: `/api/hotelimages` (CRUD + `/reorder` + `/upload`), `/api/roomtypeimages` (CRUD + `/reorder` + `/upload`)
- Add-ons: `/api/addons` (GET public, write admin)
- Promotions: `/api/promotions` (CRUD), `/api/promotions/preview`
- Tags: `/api/tags` (CRUD), `/api/tags/hotel/{hotelId}` (set tags)
- Facilities/Cities/Countries: `/api/facilities`, `/api/cities`, `/api/countries` (CRUD)

## Notes

- Build output `bin/` and `obj/` are ignored by Git.
- Stripe keys must be present or the API fails on startup.
- Scalar UI requires API + DB to be running.
