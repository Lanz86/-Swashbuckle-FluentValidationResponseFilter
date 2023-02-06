using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LnzSoftware.Swashbuckle.FluentValidationResponseFilter
{
    public class ValidationException : IValidationResponse
    {
        public string Type
        {
            get
            {
                return "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            }
        }

        public string Title
        {
            get
            {
                return "One or more validation errors occurred.";
            }
        }

        public string Status
        {
            get
            {
                return "400";
            }
        }

        public IDictionary<string, string[]> Errors
        {
            get
            {
                return ValidationFailures
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
            }
        }

        private IEnumerable<ValidationFailure> ValidationFailures { get; set; } = new List<ValidationFailure>();

        public void CreateObject(IEnumerable<ValidationFailure> validationFailures)
        {
            ValidationFailures = validationFailures;
        }
    }
}
