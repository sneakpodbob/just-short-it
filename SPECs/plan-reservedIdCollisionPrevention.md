# Plan: Reserved ID Collision Prevention

## Collision Analysis

The ID alphabet is `[A-Za-z0-9]` — no `.` or `/` characters. This has two important consequences:

**What CAN collide — Razor Pages routes:**

| Reserved path | Length | Effect |
| -- | - | - |
| `Urls` | 4 | Razor Page route wins; redirect unreachable |
| `Login` | 5 | Razor Page route wins; redirect unreachable |
| `Error` | 5 | Razor Page route wins; redirect unreachable |
| `Logout` | 6 | Razor Page route wins; redirect unreachable |
| `Inspect` | 7 | Razor Page route wins; redirect unreachable |

Because ASP.NET Core routing is **case-insensitive**, case variants like `login`, `LOGIN`, `lOgIn` are also permanently unreachable — but the current ID generator can produce them without any guard.

**What does NOT collide — static files:**

- The alphabet has no `.` or `/`, so it can't produce paths like `css/site.css`
- `UseStaticFiles` doesn't serve directories — `/css` falls through to `/{Id?}` and works fine
- Only extension-less files at the wwwroot root could shadow an ID, but none exist in this project

**Future risk**: Any new Razor Page or minimal API endpoint with a literal first-path-segment creates a new blind spot with no current guard.

---

## Proposed Solution: Automatic Endpoint-Aware `ReservedIdProvider`

### Phase 1 — New `Service/ReservedIdProvider.cs`

- Define `IReservedIdProvider` interface with `IReadOnlySet<string> ReservedIds`
- Implement `ReservedIdProvider`, constructor-injected with `IEnumerable<EndpointDataSource>`
- Use `Lazy<IReadOnlySet<string>>` (thread-safe) for first-access initialization:
  - Enumerate all `RouteEndpoint` instances across all data sources
  - For each, inspect `RoutePattern.PathSegments[0].Parts[0]` — if it is a `RoutePatternLiteralPart`, add `.Content` to the set
  - Use `HashSet<string>(StringComparer.OrdinalIgnoreCase)` for case-insensitive blocking
- This automatically picks up `Urls`, `Login`, `Logout`, `Inspect`, `Error`, and any future pages or API endpoints
- Register in `just-short-it/Program.cs` as `builder.Services.AddSingleton<IReservedIdProvider, ReservedIdProvider>()`

### Phase 2 — Integrate into `Service/SqliteUrlStore.cs`

- Add `IReservedIdProvider` constructor parameter
- **`CreateAsync`**: add a reserved-ID check before the DB write — `_reservedIdProvider.ReservedIds.Contains(id)` → log warning + `return false`
- **`GenerateNewId`**: inside the length loop, after building `existingIds`, union in all reserved IDs of matching length: `existingIds.UnionWith(_reservedIdProvider.ReservedIds.Where(r => r.Length == currentLength))`. This keeps the saturation check correct and prevents reserved IDs from ever being returned as candidates.

### Phase 3 — Tests

- `CreateAsync` rejects exact-case reserved ID (`Urls`, `Login`, etc.)
- `CreateAsync` rejects case-variant reserved ID (`login`, `LOGIN`)
- `GenerateNewId` never returns a reserved ID (mock provider with a known set, generate many IDs)
- `ReservedIdProvider.ReservedIds` returns expected values when built with a fake `EndpointDataSource`

---

**Relevant files**

- `just-short-it/Service/ReservedIdProvider.cs` — new file
- `just-short-it/Service/SqliteUrlStore.cs` — inject + use in `CreateAsync` and `GenerateNewId`
- `just-short-it/Program.cs` — register singleton
- `just-short-it.Tests/` — new or extended test coverage

**Decisions & scope**

- No DB migration needed — reserved IDs are a runtime in-memory concern
- Lazy initialization is safe: `EndpointDataSource` entries are fully populated by `MapRazorPages()` before the first HTTP request, which is when `ReservedIds` is first accessed
- The lazy scan approach means this is **zero-maintenance**: add a new page or endpoint and it's automatically protected
- Static files are explicitly out of scope (no real collision possible given alphabet constraints)
- The lazy scan approach means this is **zero-maintenance**: add a new page or endpoint and it's automatically protected
