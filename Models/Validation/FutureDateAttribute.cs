using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Group4Flight.Models.Validation
{
    /// <summary>
    /// Custom validation attribute that ensures a date is strictly greater than today
    /// and no more than <see cref="MaxYearsAhead"/> years into the future.
    ///
    /// Implements <see cref="IClientModelValidator"/> so jQuery-unobtrusive can
    /// pick up the data-val-* attributes and run the same rule on the client side.
    /// The matching JavaScript adapter lives in wwwroot/js/futuredate.js.
    ///
    /// Designed to be reusable (e.g. flight date, user DOB range, etc.) — per
    /// Phase 3 technical requirement #4, we do NOT move validation onto the
    /// model class as IValidatableObject because that would not be shareable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FutureDateAttribute : ValidationAttribute, IClientModelValidator
    {
        public int MaxYearsAhead { get; set; } = 3;

        public FutureDateAttribute()
        {
            ErrorMessage = "Date must be after today and within {0} years from today.";
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessageString, MaxYearsAhead);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            if (value is DateTime date)
            {
                var today = DateTime.Today;
                var maxDate = today.AddYears(MaxYearsAhead);

                if (date.Date <= today || date.Date > maxDate)
                {
                    return new ValidationResult(
                        FormatErrorMessage(validationContext.DisplayName),
                        new[] { validationContext.MemberName! });
                }
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Emits the data-val-* attributes consumed by the client-side adapter
        /// in wwwroot/js/futuredate.js.
        /// </summary>
        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-futuredate",
                FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
            MergeAttribute(context.Attributes, "data-val-futuredate-maxyears",
                MaxYearsAhead.ToString());
        }

        private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes.Add(key, value);
            }
        }
    }
}
