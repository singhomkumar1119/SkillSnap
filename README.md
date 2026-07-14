# SkillSnap — Full-Stack Portfolio App

A portfolio builder where a developer can showcase their profile, projects, and
skills to the public, while managing that content through an authenticated
admin area. Built with **Blazor WebAssembly** (client) and **ASP.NET Core Web
API** (server), sharing DTOs through a **SkillSnap.Shared** class library.

## Project structure

```
SkillSnap/
├── SkillSnap.sln
├── SkillSnap.Shared/        # DTOs shared by client and server
│   └── Models/Dtos.cs
├── SkillSnap.Server/        # ASP.NET Core Web API
│   ├── Controllers/          # Auth, Profile, Projects, Skills
│   ├── Data/                    # EF Core DbContext + seed data
│   ├── Models/                # EF entities + ApplicationUser
│   ├── Services/              # TokenService (JWT)
│   └── Program.cs
└── SkillSnap.Client/         # Blazor WebAssembly
    ├── Pages/                  # Home, Login, Register, ManageProjects, ManageSkills
    ├── Layout/                 # MainLayout, NavMenu
    ├── Services/               # AuthStateService, ApiClientService
    └── Program.cs
```

## Running it locally

> This was written and reviewed without a local .NET runtime available, so
> please do a fresh `dotnet restore` / `dotnet build` after downloading to
> confirm everything compiles cleanly, and let me know if anything needs a fix.

1. **Restore and run the server:**
   ```bash
   cd SkillSnap.Server
   dotnet restore
   dotnet run
   ```
   This applies EF Core migrations automatically (via `DbSeeder.SeedAsync`),
   creates `skillsnap.db` (SQLite), and seeds a demo account:
   `demo@skillsnap.dev` / `Demo@Pass123`. Swagger opens at `/swagger` for
   quick manual testing of each endpoint.

2. **Run the client** (in a second terminal):
   ```bash
   cd SkillSnap.Client
   dotnet restore
   dotnet run
   ```
   Visit `https://localhost:5173`.

3. **First-time setup note:** if you haven't created EF Core migrations yet, run:
   ```bash
   cd SkillSnap.Server
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   ```
   before the first `dotnet run`.

---

## Written Reflection

### 1. Describe your SkillSnap application

SkillSnap is a portfolio site where visitors can view a developer's profile,
project list, and skill set, while the owner can log in to add, edit, or
remove that content without touching any code.

**Three key features:**
- **Public portfolio view** — profile, projects, and skills load together on
  the home page with no login required, so it works as a shareable link.
- **Authenticated content management** — dedicated "Manage Projects" and
  "Manage Skills" pages let the logged-in owner add, edit, and delete their
  own entries through simple forms with inline validation.
- **Secure, token-based accounts** — registration and login are backed by
  ASP.NET Identity and issue a JWT that the Blazor client stores and attaches
  to every request, so write access is properly gated behind a real account.

### 2. Main challenges and how they were solved

- **Cross-origin requests between client and server.** Since the Blazor WASM
  client and the API run on different ports/origins in development, the
  browser blocks requests by default. This was solved with an explicit CORS
  policy on the server (`AllowClient`) that only allows the client's known
  origin, rather than opening CORS to everything.
- **Keeping the client aware of login state everywhere at once.** Blazor WASM
  has no built-in session/cookie mechanism the way server-rendered apps do.
  This was solved by writing `AuthStateService` as both the app's
  `AuthenticationStateProvider` and its single source of truth for the JWT —
  logging in or out calls `NotifyAuthenticationStateChanged`, which
  automatically updates the nav menu, route guards, and any `AuthorizeView`
  blocks without extra wiring.
- **Preventing one user from editing another user's data.** Just checking
  "is this request authenticated" isn't enough — an authenticated user could
  still try to edit someone else's project by guessing an ID. This was solved
  with an explicit ownership check (`project.OwnerId != CurrentUserId`) in
  every write endpoint, returning `403 Forbidden` rather than trusting the
  token alone.

### 3. How the app's core features are structured

- **Business logic** lives in the controllers, kept intentionally thin:
  validate the incoming DTO, check ownership where relevant, delegate the
  actual read/write to EF Core, and invalidate any related cache entry.
- **Data persistence** uses EF Core with SQLite through a single
  `ApplicationDbContext` that also hosts ASP.NET Identity's tables, so user
  accounts and portfolio data live in one file with one connection string.
  `DbSeeder` runs on startup to apply migrations and seed a demo account plus
  sample projects/skills, so the app isn't empty on first run.
- **Blazor components** are split by responsibility: `Home.razor` is a
  read-only view, `ManageProjects.razor`/`ManageSkills.razor` are CRUD forms
  behind `[Authorize]`, and `Layout/NavMenu.razor` reacts to auth state rather
  than each page tracking it independently.
- **The API** was built and shaped iteratively — the shared `SkillSnap.Shared`
  project defines each DTO once, and both client and server reference it, so a
  changed field shows up as a compile error on both sides instead of a silent
  mismatch at runtime.

### 4. Authentication and security implementation

- **ASP.NET Identity** (`IdentityCore<ApplicationUser>`) handles password
  hashing, account lockout after 5 failed attempts, and a password policy
  (minimum 8 characters, requires uppercase and a non-alphanumeric character)
  enforced server-side, not just in the UI.
- **Token management:** `TokenService` issues a signed JWT (HMAC-SHA256) with
  a 2-hour expiry on successful login/registration. The client stores it in
  local storage via `Blazored.LocalStorage` and attaches it as a
  `Bearer` header on every request; the server validates issuer, audience,
  signature, and expiry on each call via `AddJwtBearer`.
- **Role/ownership-based authorization:** rather than a single global admin
  role, each `Project`/`Skill`/`Profile` record carries an `OwnerId`. Every
  write endpoint is `[Authorize]`-protected *and* separately checks that the
  authenticated user's ID matches the record's owner before allowing an
  edit or delete, returning `403 Forbidden` otherwise.
- **Input validation** happens through DataAnnotations on the shared DTOs
  (`[Required]`, `[EmailAddress]`, `[StringLength]`, `[Range]`, `[Url]`), and
  `[ApiController]`'s automatic model validation rejects malformed requests
  with a `400` before they ever reach a database call.

### 5. Performance optimization strategies

- **In-memory response caching** on the read-heavy `GET /api/projects` and
  `GET /api/skills` endpoints (`IMemoryCache`, 60-second TTL), since portfolio
  pages are viewed far more often than they're edited. Any create/update/
  delete explicitly evicts the relevant cache key so visitors never see stale
  data for more than a few seconds after an edit.
- **Parallel data loading on the client:** `Home.razor` fires the profile,
  projects, and skills requests together with `Task.WhenAll` instead of
  awaiting them one at a time, so the page's total load time is bound by the
  slowest single call rather than the sum of all three.
- **Response compression** (`AddResponseCompression`) shrinks the JSON
  payloads sent to the client, which matters more than it sounds like for a
  WASM app since every byte competes with the (already sizable) Blazor
  runtime download on first load.
- **Database indexes** on `OwnerId` for `Project`, `Skill`, and `Profile`
  speed up the "this user's data" queries that run on nearly every request.
- Copilot-assisted pass: reviewing each controller with Copilot Chat surfaced
  the missing cache-invalidation calls on the `PUT`/`DELETE` endpoints early
  (an easy bug to miss — the *reads* were cached but a stale entry would have
  lingered for up to 60 seconds after an edit), which is now handled by
  calling `_cache.Remove(...)` in every mutating action.

---

## Known limitations / what I'd add with more time
- No automated tests (unit or integration) yet — the project would benefit
  from `xUnit` + `WebApplicationFactory` tests against the controllers.
- The demo password policy is intentionally simple; a production app would
  want refresh tokens instead of a single 2-hour JWT with no renewal path.
- No pagination on the projects/skills lists — fine at this scale, but would
  need to be added if a portfolio grew to dozens of entries.
