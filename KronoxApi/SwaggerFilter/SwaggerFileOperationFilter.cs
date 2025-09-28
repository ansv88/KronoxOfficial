using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KronoxApi.SwaggerFilter;

// Swagger-filter som korrigerar representationen av filuppladdningar i Swagger UI.
public class SwaggerFileOperationFilter : IOperationFilter
{
    // Tillämpas på en Swagger-operation för att förbättra hanteringen av filuppladdningar.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
            .ToList();
        if (!fileParams.Any()) return;

        operation.Parameters = operation.Parameters
            .Where(p => !fileParams.Any(fp => string.Equals(fp.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        // Lägg till IFormFile-fält
        foreach (var fileParam in fileParams)
        {
            var fileSchema = new OpenApiSchema { Type = "string", Format = "binary" };
            if (!string.IsNullOrEmpty(fileParam.ModelMetadata?.Description))
            {
                fileSchema.Description = fileParam.ModelMetadata.Description;
            }

            if (!schema.Properties.ContainsKey(fileParam.Name))
                schema.Properties.Add(fileParam.Name, fileSchema);

            if (fileParam.IsRequired)
                schema.Required.Add(fileParam.Name);
        }

        // Lägg till andra [FromForm]-fält
        var formParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source == BindingSource.Form && p.ModelMetadata?.ModelType != typeof(IFormFile));

        foreach (var formParam in formParams)
        {
            var formParamSchema = InferSchemaForType(formParam.ModelMetadata?.ModelType);
            if (!string.IsNullOrEmpty(formParam.ModelMetadata?.Description))
            {
                formParamSchema.Description = formParam.ModelMetadata.Description;
            }

            if (!schema.Properties.ContainsKey(formParam.Name))
                schema.Properties.Add(formParam.Name, formParamSchema);

            if (formParam.IsRequired)
                schema.Required.Add(formParam.Name);
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType { Schema = schema }
            },
            Required = fileParams.Any(p => p.IsRequired) || formParams.Any(p => p.IsRequired)
        };
    }

    // Försöker härleda ett lämpligt OpenApiSchema för en given typ.
    private OpenApiSchema InferSchemaForType(Type? type)
    {
        if (type == null) return new OpenApiSchema { Type = "string" };

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort))
            return new OpenApiSchema { Type = "integer" };

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new OpenApiSchema { Type = "number" };

        if (type == typeof(bool))
            return new OpenApiSchema { Type = "boolean" };

        if (type == typeof(DateTime))
            return new OpenApiSchema { Type = "string", Format = "date-time" };

        if (type.IsEnum)
        {
            return new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames(type)
                    .Select(name => new Microsoft.OpenApi.Any.OpenApiString(name))
                    .Cast<Microsoft.OpenApi.Any.IOpenApiAny>()
                    .ToList()
            };
        }

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
        {
            var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            return new OpenApiSchema { Type = "array", Items = InferSchemaForType(elementType) };
        }

        return new OpenApiSchema { Type = "string" };
    }
}