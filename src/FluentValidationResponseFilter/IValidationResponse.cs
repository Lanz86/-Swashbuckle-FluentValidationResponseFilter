using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace LnzSoftware.Swashbuckle.FluentValidationResponseFilter
{
    public interface IValidationResponse
    {
        public void CreateObject(IEnumerable<ValidationFailure> validationFailures);
    }
}
