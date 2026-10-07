using System.Text.Json.Serialization;
using Insequens.Contracts.V2;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Insequens.Api.Versioning;

/// <summary>
/// Binds a query or route value to a v2 enum (one marked with <see cref="CamelCaseStringEnumConverter{TEnum}"/>)
/// by name only, ignoring case, as its JSON does. MVC's default binder also takes numbers (<c>priority=3</c>) and
/// undefined values, which the v2 contract does not allow.
/// </summary>
public sealed class StringEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        return IsV2Enum(enumType) ? new StringEnumModelBinder(enumType) : null;
    }

    private static bool IsV2Enum(Type type) =>
        type.IsEnum
        && type.GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
            .OfType<JsonConverterAttribute>()
            .Any(attribute => attribute.ConverterType is { IsGenericType: true } converter
                && converter.GetGenericTypeDefinition() == typeof(CamelCaseStringEnumConverter<>));

    private sealed class StringEnumModelBinder(Type enumType) : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            var name = Enum.GetNames(enumType).FirstOrDefault(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"'{value}' is not one of: {string.Join(", ", Enum.GetNames(enumType).Select(JsonNamingCamelCase))}.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(Enum.Parse(enumType, name));
            return Task.CompletedTask;
        }

        private static string JsonNamingCamelCase(string name) => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(name);
    }
}
