// wwwroot/js/futuredate.js
// ---------------------------------------------------------------------------
// Client-side adapter for the server-side [FutureDate] validation attribute.
//
// Per Phase 3 Technical Requirement #3, this JavaScript lives in
// wwwroot/js/ and is referenced from the specific Razor view(s) that need
// date validation (Add.cshtml and Edit.cshtml under Areas/Airline/Views/Home).
//
// The server attribute emits three data attributes on the <input>:
//   data-val                = "true"
//   data-val-futuredate     = "<error message>"
//   data-val-futuredate-maxyears = "3"
//
// jQuery-validation-unobtrusive reads these and calls into the adapter
// registered below. The adapter parses the bounds, then the validator
// method runs the same "today < date <= today + N years" check the
// server performs in FutureDateAttribute.IsValid.
// ---------------------------------------------------------------------------
(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) {
        // Guard: if jquery.validate.unobtrusive isn't loaded yet, the
        // _ValidationScriptsPartial include order in the view is wrong.
        return;
    }

    // 1. Register the adapter so unobtrusive knows how to read our data-* attrs.
    $.validator.unobtrusive.adapters.add(
        "futuredate",          // rule name (must match data-val-<name>)
        ["maxyears"],          // additional params pulled from data-val-<name>-<param>
        function (options) {
            options.rules["futuredate"] = {
                maxyears: parseInt(options.params.maxyears, 10) || 3
            };
            options.messages["futuredate"] = options.message;
        }
    );

    // 2. Register the actual rule that does the comparison on the client.
    $.validator.addMethod("futuredate", function (value, element, params) {
        // Empty is handled by [Required]; don't double-report here.
        if (!value) {
            return true;
        }

        // Parse "YYYY-MM-DD" into a local-midnight Date. Using new Date(value)
        // directly would interpret the string as UTC, which shifts the day
        // across timezones. We want the same calendar day the user picked.
        var parts = String(value).split("-");
        if (parts.length < 3) {
            // Fallback for other formats (e.g. "YYYY/MM/DD") — let Date parse.
            var fallback = new Date(value);
            if (isNaN(fallback.getTime())) { return false; }
            parts = [fallback.getFullYear(),
                     fallback.getMonth() + 1,
                     fallback.getDate()];
        }
        var entered = new Date(parseInt(parts[0], 10),
                               parseInt(parts[1], 10) - 1,
                               parseInt(parts[2], 10));
        if (isNaN(entered.getTime())) {
            return false; // Not a parseable date.
        }

        // Normalize both sides to midnight so we compare calendar days only.
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        var maxYears = (params && params.maxyears) ? params.maxyears : 3;
        var maxDate = new Date(today);
        maxDate.setFullYear(maxDate.getFullYear() + maxYears);

        // Must be strictly after today AND no later than today + maxYears.
        return entered > today && entered <= maxDate;
    });
})(jQuery);
