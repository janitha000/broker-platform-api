using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Broker.Hosting.OpenApi;

/// <summary>
/// Swashbuckle NRT support omits <c>required</c> on positional records.
/// Non-nullable CLR properties become required OpenAPI fields (camelCase).
/// </summary>
public sealed class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
            return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsNullable(property))
                continue;

            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!schema.Properties.ContainsKey(name))
                continue;

            schema.Required.Add(name);
        }
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;

        var info = Nullability.Create(property);
        return info.ReadState == NullabilityState.Nullable;
    }
}
