# GymPortal

A full-featured gymnasium management and booking web application built with ASP.NET Core MVC and Clean Architecture on .NET 10.

---

## Features

- User registration, login, and profile management
- Google OAuth authentication
- Membership plans (Monthly / Yearly) with lifecycle tracking
- Training program browsing by category and difficulty
- Class session booking with capacity enforcement
- Admin dashboard for managing users and classes
- E-commerce store for gym products
- Customer service / contact form
- Role-based access control (Admin / Member)
- Email-ready password reset and account confirmation flows

---

## Tech Stack

| Layer          | Technology                              |
|----------------|-----------------------------------------|
| Runtime        | .NET 10                                 |
| Web Framework  | ASP.NET Core MVC                        |
| ORM            | Entity Framework Core 10                |
| Database       | SQL Server (In-Memory for dev/test)     |
| Authentication | ASP.NET Identity + Google OAuth         |
| Frontend       | Razor Views, Bootstrap, jQuery          |
| Testing        | xUnit, Moq, FluentAssertions            |

---

## Architecture

The project follows **Clean Architecture** with four layers:

```
GymPortal.Domain/          → Entities, enums, domain exceptions, Result<T>
GymPortal.Application/     → Services, DTOs, repository interfaces
GymPortal.Infrastructure/  → EF Core, repositories, seed data, Identity setup
GymPortal.Web/             → Controllers, ViewModels, Razor Views, static assets
GymPortal.Tests/           → Unit tests, integration tests, test infrastructure
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or use the built-in in-memory mode for development)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/mriadalrashed/aspnet-riad-alrashed.git
   cd aspnet-riad-alrashed
   ```

2. **Configure secrets**

   Update `GymPortal.Web/appsettings.json` with your Google OAuth credentials:

   ```json
   "Authentication": {
     "Google": {
       "ClientId": "<your-client-id>",
       "ClientSecret": "<your-client-secret>"
     }
   }
   ```

3. **Run the application**

   ```bash
   dotnet run --project GymPortal.Web
   ```

   The database is created and seeded automatically on first launch.

### Default Credentials

| Role   | Email             | Password   |
|--------|-------------------|------------|
| Admin  | admin@gym.com     | Admin@123  |
| Member | member@gym.com    | Member@123 |

---

## Running Tests

```bash
dotnet test
```

The test suite includes unit tests for all services and controllers, plus integration tests using an in-memory database and a fake authentication handler.

---

## Database

The application uses **SQL Server** by default. Switch to the in-memory provider for local development by updating `ServiceCollectionExtensions` in `GymPortal.Infrastructure`.

Entity relationships:

- `ApplicationUser` → one `Membership`, many `Bookings`
- `ClassSession` → many `Bookings`, belongs to a `TrainingProgram`
- Unique constraint: one active membership per user; one confirmed booking per user per session

---

## Project Structure

```
GymPortal.Web/
├── Controllers/        # 8 controllers (Home, Account, Booking, Classes, ...)
├── ViewModels/         # 13 view models
├── Views/              # Razor views per feature area
└── wwwroot/            # Static assets (CSS, JS, images)

GymPortal.Application/
├── Services/           # IUserService, IClassService, IBookingService, ...
├── DTOs/               # 9 data transfer objects
└── Interfaces/         # IBaseRepository, IUnitOfWork

GymPortal.Infrastructure/
├── Data/               # AppDbContext, SeedData, migrations factory
└── Repositories/       # BaseRepository<T>, UnitOfWork

GymPortal.Domain/
├── Entities/           # ApplicationUser, Membership, Booking, ClassSession, ...
├── Enums/              # BookingStatus, MembershipType, DifficultyLevel, ...
└── Common/             # BaseEntity, Result<T>, domain exceptions
```

---

## 🤖 AI Usage Disclosure
AI tools were used as a productivity aid during development, particularly for refining code comments, improving documentation clarity, assisting in structuring unit and integration tests, and supporting frontend development, including translating Figma designs into HTML and CSS.

---
## License

This project is for educational purposes.
