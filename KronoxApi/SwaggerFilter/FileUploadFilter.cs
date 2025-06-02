using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KronoxApi.SwaggerFilter;

/// <summary>
/// Swagger-filter som anpassar OpenAPI-dokumentationen för endpoints som tar emot filuppladdning via [FromForm] och IFormFile.
/// Gör så att Swagger UI visar korrekt multipart/form-data-schema.
/// </summary>

public class FileUploadFilter : IOperationFilter
{
    // Modifierar OpenAPI-operationen för att korrekt beskriva filuppladdning via multipart/form-data.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Hämta alla parametrar med [FromForm] som är av typen IFormFile
        var fileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
            .ToList();

        // Om inga IFormFile-parametrar finns, finns inget att göra
        if (!fileParams.Any())
        {
            return;
        }

        // Ta bort de genererade parametrarna för IFormFile från operation.Parameters
        foreach (var fileParam in fileParams)
        {
            var parameterToRemove = operation.Parameters.FirstOrDefault(p => p.Name == fileParam.Name);
            if (parameterToRemove != null)
            {
                operation.Parameters.Remove(parameterToRemove);
            }
        }

        // Skapa ett schema för multipart/form-data
        var schema = new OpenApiSchema
        {
            Type = "object"
        };

        // Lägg till en egenskap i schemat för varje IFormFile-parameter
        foreach (var fileParam in fileParams)
        {
            schema.Properties.Add(fileParam.Name, new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
            schema.Required.Add(fileParam.Name);
        }

        // Om det finns andra [FromForm]-parametrar (exempelvis strängar) så lägg till dem också.
        var formParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Form &&
                        p.ModelMetadata?.ModelType != typeof(IFormFile))
            .ToList();

        foreach (var formParam in formParams)
        {
            if (!schema.Properties.ContainsKey(formParam.Name))
            {
                schema.Properties.Add(formParam.Name, new OpenApiSchema
                {
                    Type = "string"
                });
                schema.Required.Add(formParam.Name);
            }
        }

        // Ange requestBody med multipart/form-data och det nya schemat
        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
        };
    }
}