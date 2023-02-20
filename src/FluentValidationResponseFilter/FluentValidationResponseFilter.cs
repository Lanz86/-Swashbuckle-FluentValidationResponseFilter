using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace LnzSoftware.Swashbuckle.FluentValidationResponseFilter
{
    public class FluentValidationResponseFilter : IOperationFilter
    {
        private readonly Type _validationResponseType;
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationResponseFilter(IServiceProvider serviceProvider)
        {
            _validationResponseType = typeof(ValidationException);
            _serviceProvider = serviceProvider;
        }

        public FluentValidationResponseFilter(Type validationResponseType, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _validationResponseType = validationResponseType;
        }

        private static List<string> contentTypes = new List<string>
    {
        "application/problem+json"
    };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            List<ValidationFailure> failures = new List<ValidationFailure>();
            foreach (var param in context.ApiDescription.ParameterDescriptions)
            {
                if (param.Type == null) continue;
                Type validatorType = typeof(IValidator<>).MakeGenericType(param.Type);
                var validator = _serviceProvider.CreateScope().ServiceProvider.GetService(validatorType) as IValidator;
                if (validator != null)
                {
                    var validationRoules = validator.CreateDescriptor();
                    foreach (var rule in validationRoules.Rules)
                    {
                        List<string> errorMessages = new List<string>();
                        var expression = rule.Expression != null ? string.Join(".", rule.Expression?.ToString()?.Split('.')?.Skip(1)?.Take(1)) : null;
                        foreach (var component in rule.Components)
                        {
                            failures.Add(new ValidationFailure { ErrorMessage = component.GetUnformattedErrorMessage(), PropertyName = expression, ErrorCode = component.ErrorCode });
                        }
                    }
                }
            }

            if (!operation.Responses.Any(x => x.Key == StatusCodes.Status400BadRequest.ToString()))
            {
                operation.Responses.Add(StatusCodes.Status400BadRequest.ToString(), new OpenApiResponse());
            }

            foreach (var response in operation.Responses.Where(x => x.Key == StatusCodes.Status400BadRequest.ToString()))
            {
                operation.Responses[StatusCodes.Status400BadRequest.ToString()].Content.Clear();
                var validationResponse = Activator.CreateInstance(_validationResponseType);
                var method = _validationResponseType.GetMethod("CreateObject");

                method.Invoke(validationResponse, new object[] { failures });

                var schema = context.SchemaGenerator.GenerateSchema(_validationResponseType, context.SchemaRepository);
                var example = new OpenApiString(JsonSerializer.Serialize(validationResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                operation.Responses[StatusCodes.Status400BadRequest.ToString()].Content.Add("application/json", new OpenApiMediaType { Example = example, Schema = schema });
            }
        }
    }
}