﻿using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public static class AnnotationsSwaggerGenOptionsExtensions
{
    /// <summary>
    /// Enables Swagger annotations (SwaggerOperationAttribute, SwaggerParameterAttribute etc.)
    /// </summary>
    /// <param name="options"></param>
    /// <param name="enableAnnotationsForInheritance">Enables SwaggerSubType attribute for inheritance</param>
    /// <param name="enableAnnotationsForPolymorphism">Enables SwaggerSubType and SwaggerDiscriminator attributes for polymorphism</param>
    public static void EnableAnnotations(
        this SwaggerGenOptions options,
        bool enableAnnotationsForInheritance,
        bool enableAnnotationsForPolymorphism)
    {
        options.SchemaFilter<AnnotationsSchemaFilter>();
        options.ParameterFilter<AnnotationsParameterFilter>();
        options.RequestBodyFilter<AnnotationsRequestBodyFilter>();
        options.OperationFilter<AnnotationsOperationFilter>();
        options.DocumentFilter<AnnotationsDocumentFilter>();

        if (enableAnnotationsForInheritance || enableAnnotationsForPolymorphism)
        {
            options.SelectSubTypesUsing(AnnotationsSubTypesSelector);
            options.SelectDiscriminatorNameUsing(AnnotationsDiscriminatorNameSelector);
            options.SelectDiscriminatorValueUsing(AnnotationsDiscriminatorValueSelector);

            if (enableAnnotationsForInheritance)
            {
                options.UseAllOfForInheritance();
            }

            if (enableAnnotationsForPolymorphism)
            {
                options.UseOneOfForPolymorphism();
            }
        }
    }

    /// <summary>
    /// Enables Swagger annotations (SwaggerOperationAttribute, SwaggerParameterAttribute etc.)
    /// </summary>
    /// <param name="options"></param>
    public static void EnableAnnotations(this SwaggerGenOptions options)
    {
        options.EnableAnnotations(
            enableAnnotationsForPolymorphism: false,
            enableAnnotationsForInheritance: false);
    }

    private static IEnumerable<Type> AnnotationsSubTypesSelector(Type type)
    {
        var subTypeAttributes = type.GetCustomAttributes(false)
            .OfType<SwaggerSubTypeAttribute>();

        if (subTypeAttributes.Any())
        {
            return subTypeAttributes.Select(attr => attr.SubType);
        }

        var jsonDerivedTypeAttributes = type.GetCustomAttributes(false)
            .OfType<JsonDerivedTypeAttribute>()
            .ToList();

        if (jsonDerivedTypeAttributes.Count > 0)
        {
            return jsonDerivedTypeAttributes.Select(attr => attr.DerivedType);
        }

        return [];
    }

    private static string AnnotationsDiscriminatorNameSelector(Type baseType)
    {
        var discriminatorAttribute = baseType.GetCustomAttributes(false)
            .OfType<SwaggerDiscriminatorAttribute>()
            .FirstOrDefault();

        if (discriminatorAttribute != null)
        {
            return discriminatorAttribute.PropertyName;
        }

        var jsonPolymorphicAttributes = baseType.GetCustomAttributes(false)
            .OfType<JsonPolymorphicAttribute>()
            .FirstOrDefault();

        if (jsonPolymorphicAttributes != null)
        {
            return jsonPolymorphicAttributes.TypeDiscriminatorPropertyName;
        }

        return null;
    }

    private static string AnnotationsDiscriminatorValueSelector(Type subType)
    {
        var baseType = subType.BaseType;
        while (baseType != null)
        {
            var subTypeAttribute = baseType.GetCustomAttributes(false)
                .OfType<SwaggerSubTypeAttribute>()
                .FirstOrDefault(attr => attr.SubType == subType);

            if (subTypeAttribute != null)
            {
                return subTypeAttribute.DiscriminatorValue;
            }

            var jsonDerivedTypeAttributes = baseType.GetCustomAttributes(false)
                .OfType<JsonDerivedTypeAttribute>()
                .FirstOrDefault(attr => attr.DerivedType == subType);

            if (jsonDerivedTypeAttributes is { TypeDiscriminator: string discriminator })
            {
                return discriminator;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }
}
