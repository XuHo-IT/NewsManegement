using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FUNewsManagementSystem.Infrastructure;

/// <summary>
/// Operation filter to handle file uploads in Swagger/OpenAPI documentation
/// Properly configures multipart/form-data requests with DTOs and IFormFile
/// </summary>
public class FileUploadOperation : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this is a file upload operation
        var hasConsumesFormData = context.MethodInfo
            .GetCustomAttributes(typeof(ConsumesAttribute), false)
            .OfType<ConsumesAttribute>()
            .FirstOrDefault()?
            .ContentTypes
            .Any(ct => ct.Contains("multipart/form-data")) ?? false;

        if (!hasConsumesFormData)
            return;

        const string fileUploadMimeType = "multipart/form-data";

        if (context.ApiDescription.ActionDescriptor?.Parameters == null)
            return;

        var parameters = context.ApiDescription.ActionDescriptor.Parameters;
        
        // Get DTO parameter
        var dtoParam = parameters.FirstOrDefault(p => p.ParameterType.Name.EndsWith("Dto"));
        var fileParam = parameters.FirstOrDefault(p => p.ParameterType == typeof(IFormFile));

        if (dtoParam == null && fileParam == null)
            return;

        var formProperties = new Dictionary<string, OpenApiSchema>();

        // Add file parameter as binary
        if (fileParam != null)
        {
            formProperties[fileParam.Name] = new OpenApiSchema()
            {
                Type = "string",
                Format = "binary",
                Description = "Image file to upload"
            };
        }

        // Add DTO properties as form fields
        if (dtoParam != null)
        {
            var dtoType = dtoParam.ParameterType;
            var properties = dtoType.GetProperties();

            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                var isNullable = propType.IsGenericType && 
                                 propType.GetGenericTypeDefinition() == typeof(Nullable<>);
                
                string schemaType = "string";

                if (propType == typeof(int) || propType == typeof(int?) ||
                    propType == typeof(short) || propType == typeof(short?))
                {
                    schemaType = "integer";
                }
                else if (propType == typeof(bool) || propType == typeof(bool?))
                {
                    schemaType = "boolean";
                }
                else if (propType == typeof(DateTime) || propType == typeof(DateTime?))
                {
                    schemaType = "string";
                }
                else if (propType.IsGenericType && 
                         propType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // For List types, we'll skip them or handle as comma-separated
                    formProperties[prop.Name] = new OpenApiSchema()
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "integer" },
                        Description = "Comma-separated list of IDs"
                    };
                    continue;
                }

                formProperties[prop.Name] = new OpenApiSchema()
                {
                    Type = schemaType,
                    Description = $"{prop.Name} field"
                };
            }
        }

        if (formProperties.Count == 0)
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    fileUploadMimeType, new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = formProperties
                        }
                    }
                }
            }
        };

        // Remove any conflicting request bodies
        if (operation.RequestBody?.Content != null)
        {
            var keysToRemove = operation.RequestBody.Content
                .Keys
                .Where(k => k != fileUploadMimeType)
                .ToList();

            foreach (var key in keysToRemove)
            {
                operation.RequestBody.Content.Remove(key);
            }
        }
    }
}
