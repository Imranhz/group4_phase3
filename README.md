# Group 4 Flight — Phase 3 (Validation)

This is the Phase 3 deliverable for the Group 4 flight project. Building on the
Phase 2 airline-agent flight management feature, this phase layers in user-input
validation: standard regex validation, a reusable custom attribute, and a
remote uniqueness check — all working on both the client and the server.

## What was implemented

The validation rules from the Phase 3 specification are all wired onto the
`Flight` model class (not as an `IValidatableObject` — see the "Why not
self-validation?" note below).

| Field            | Rule                                                                             | Mechanism                                    |
| ---------------- | -------------------------------------------------------------------------------- | -------------------------------------------- |
| `Airline`        | drop-down, no validation (only `[Required]` + `[Range(1, ...)]` so a selection is made) | `[Required]`, `[Range]`                 |
| `FlightCode`     | Alphanumeric, **2 letters followed by 1–4 digits** (e.g. `AC101`)                | `[RegularExpression(@"^[A-Za-z]{2}[0-9]{1,4}$")]` |
| `From`           | Letters only, max 50                                                             | `[RegularExpression]` + `[StringLength(50)]`  |
| `To`             | Letters only, max 50                                                             | `[RegularExpression]` + `[StringLength(50)]`  |
| `Date` (rule 1)  | **Must be after today, and no more than 3 years ahead** — *custom validator*     | `[FutureDate(MaxYearsAhead = 3)]`             |
| `Date` (rule 2)  | **`Date` + `FlightCode` pair must not already exist** — *remote validator*       | `[Remote("VerifyFlightDate", ...)]`           |
| `DepartureTime`  | 24 hours / 60 minutes (`HH:mm`)                                                  | `[RegularExpression(@"^([01]\d\|2[0-3]):[0-5]\d$")]` |
| `ArrivalTime`    | 24 hours / 60 minutes (`HH:mm`)                                                  | `[RegularExpression(@"^([01]\d\|2[0-3]):[0-5]\d$")]` |
| `CabinType`      | drop-down, no validation (only `[Required]` so a selection is made)              | `[Required]`                                  |
| `Emission`       | 0 – 5000 kg CO₂e                                                                 | `[Range(0, 5000)]`                            |
| `AircraftType`   | drop-down, no validation (only `[Required]` so a selection is made)              | `[Required]`                                  |
| `Price`          | ≥ 0 and ≤ $50,000                                                                | `[Range(typeof(decimal), "0", "50000")]`      |

All validation errors render **at property level** (next to the field) via
`<span asp-validation-for="..."></span>`.

### Custom validator — `FutureDateAttribute`

Lives at `Models/Validation/FutureDateAttribute.cs`. It implements:

- `ValidationAttribute.IsValid(...)` for **server-side** validation.
- `IClientModelValidator.AddValidation(...)` for **client-side** validation —
  it emits `data-val`, `data-val-futuredate`, and `data-val-futuredate-maxyears`
  attributes on the rendered `<input>`.

Its partner adapter is in `wwwroot/js/futuredate.js`, which registers the rule
with `jquery.validate.unobtrusive`. Per Technical Requirement #3, the JavaScript
function lives in `wwwroot/js/` and is referenced from the Razor view rather
than inlined in it.

Because the attribute is a separate reusable class (not an
`IValidatableObject` implementation on `Flight`), it can be applied to other
date fields in the future — e.g. a user's date of birth — which is exactly
what Technical Requirement #4 asks for.

### Remote validation — `Date + FlightCode` uniqueness

- The `[Remote]` attribute on `Flight.Date` points at
  `Airline/Home/VerifyFlightDate` and uses `AdditionalFields` (per slide #52)
  to ship `FlightCode` and `FlightId` alongside the `Date` being validated.
- `VerifyFlightDate` queries the database and returns `Json(true)` when the
  pair is unique, or a `Json("...")` error string when it is not.

**Avoiding duplicate validation effort (Technical Requirement #1):** The
remote endpoint writes two tokens into `TempData`:

```csharp
TempData["RemoteDateCheck"]         = "{FLIGHTCODE}|{yyyy-MM-dd}";
TempData["RemoteDateCheck:Unique"]  = true/false;
```

When the subsequent `Add`/`Edit` `POST` fires, the controller's
`IsDateFlightCodePairUnique(...)` helper first **peeks** at TempData; if the
submitted pair matches what was just verified, it trusts the cached answer
and skips the database round-trip. If TempData is empty (for example,
client-side JS disabled, or the remote call never fired), the helper falls
back to a fresh database query — so server-side validation is **never**
skipped.

### View changes

- `Areas/Airline/Views/Home/Add.cshtml` and `Edit.cshtml` now use tag helpers
  (`asp-for`, `asp-validation-for`, `asp-validation-summary`) so the `data-val-*`
  attributes are emitted automatically.
- Both views include `<partial name="_ValidationScriptsPartial" />`
  (jquery.validate + jquery.validate.unobtrusive from CDN) and
  `<script src="~/js/futuredate.js"></script>` so client-side checking is live.

## Assumptions made

1. **Client-side jQuery stack loaded from CDN.** Phase 2's layout already pulls
   jQuery 3.7.1 from a CDN, so the added `_ValidationScriptsPartial.cshtml`
   follows the same pattern for `jquery.validate` and
   `jquery.validate.unobtrusive`. This keeps the project's dependency surface
   unchanged (no new NuGet / npm packages).
2. **`From`/`To` allow spaces** (e.g. "New York") alongside letters. The regex
   is `^[A-Za-z\s]{1,50}$`. The spec said "letters only, 50 letters max" which
   reasonably covers multi-word city names; pure-alpha would reject "New York".
3. **Time inputs use `HH:mm`.** HTML `<input type="time">` produces this
   format. The regex `^([01]\d|2[0-3]):[0-5]\d$` matches it exactly.
4. **Case-insensitive FlightCode uniqueness.** Two flights with codes `ac101`
   and `AC101` on the same date are treated as duplicates.
5. **The dropdowns (`Airline`, `CabinType`, `AircraftType`) still need a
   selection.** The requirement says "no validation" but empty submissions
   should still be rejected; we enforce that minimally with `[Required]`.
6. **Phase 2 behaviour is preserved.** No Phase 2 logic was deleted; the
   controller's CRUD actions, PRG redirects, and TempData "flash" messages
   all still work.

## How to run / test

1. Open the solution in Visual Studio or run from the CLI:
   ```
   dotnet restore
   dotnet run
   ```
2. Browse to `https://localhost:<port>/Airline/Home/Add`.

### Verifying client-side validation (JavaScript ON)

- Leave a field blank or enter a bad value and **Tab out** of it — you should
  see the error message appear immediately, **before** you click *Add Flight*.
- Try each rule:
  - `FlightCode` = `A1` → "must start with 2 letters and be followed by 1-4 digits"
  - `From` = `Toronto1` → "letters only, max 50"
  - `Date` = yesterday → "Date must be after today and within 3 years..."
  - `Date` = today's date + existing `FlightCode` (e.g. `AC101` with an existing seeded date) → remote validation fires via AJAX and flags the duplicate.
  - `Emission` = `6000` → "between 0 and 5000"
  - `Price` = `-5` or `60000` → "between $0 and $50,000"

### Verifying server-side validation (JavaScript OFF)

1. In Chrome DevTools → ⋮ → Settings → Preferences → Debugger → **Disable
   JavaScript** (or use `Ctrl+Shift+P` → "Disable JavaScript").
2. Reload the `Add` page and repeat the same bad inputs.
3. Click **Add Flight** — the exact same error messages should render (now
   coming from the server round-trip). This proves the server-side validation
   is present even when client-side is compromised.

## File layout

```
output/
├── Program.cs
├── Group4Flight.csproj
├── Group4Flight.sln
├── appsettings.json
├── flights.db
├── Areas/
│   ├── Admin/ ...................................... (unchanged)
│   └── Airline/
│       ├── Controllers/
│       │   └── HomeController.cs ................... + VerifyFlightDate remote endpoint
│       └── Views/
│           ├── Home/
│           │   ├── Add.cshtml ...................... tag-helper form + validation spans
│           │   ├── Edit.cshtml ..................... tag-helper form + validation spans
│           │   └── Index.cshtml .................... (unchanged)
│           └── Shared/_AirlineLayout.cshtml ......... (unchanged)
├── Controllers/HomeController.cs ................... (unchanged)
├── Models/
│   ├── Flight.cs ................................... decorated with validation attrs
│   ├── FlightContext.cs ............................ (unchanged)
│   ├── FlightViewModel.cs .......................... (unchanged)
│   ├── Airline.cs / User.cs / etc. ................. (unchanged)
│   └── Validation/
│       └── FutureDateAttribute.cs .................. NEW — custom validator
├── Views/
│   ├── Home/ ....................................... (unchanged)
│   └── Shared/
│       ├── _Layout.cshtml .......................... (unchanged)
│       └── _ValidationScriptsPartial.cshtml ........ NEW — jQuery validate scripts
└── wwwroot/
    ├── css/site.css ................................ (unchanged)
    ├── images/ ..................................... (unchanged)
    └── js/
        ├── site.js ................................. (unchanged)
        └── futuredate.js ........................... NEW — client adapter for FutureDate
```

## Why not self-validation?

Technical Requirement #4 explicitly warns against converting the `Flight`
model into an `IValidatableObject`. The reason: custom validators should be
reusable. Our `FutureDateAttribute` lives in its own file and can be dropped
onto any `DateTime` property in any model — e.g. a user DOB check, a booking
date, etc. If we had stuffed that logic into `Flight.Validate(...)`, we'd
have to copy/paste it every time we needed the same rule elsewhere.
