using System.Reflection;
using System.Runtime.CompilerServices;
using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;

namespace App.Application.Tests.Erp;

public sealed class ErpDataProviderContractTests
{
    private static readonly string[] RequiredMethodNames =
    [
        "GetOrderAsync",
        "GetOrderItemsAsync",
        "GetOrderSummariesAsync",
        "GetProductAsync",
        "GetProductBomAsync",
        "GetStockLevelsAsync",
        "GetOpenPurchaseOrdersAsync",
        "GetWorkOrdersAsync",
        "GetCapacityAndCalendarAsync",
        "GetShippingDurationAsync"
    ];

    private static readonly string[] ForbiddenOperationPrefixes =
    [
        "Create", "Update", "Delete", "Save", "Write", "Post", "Put", "Patch", "Sync"
    ];

    private static readonly Type[] DtoTypes =
    [
        typeof(OrderReadDto),
        typeof(OrderItemReadDto),
        typeof(OrderSummaryReadDto),
        typeof(ProductReadDto),
        typeof(BomItemReadDto),
        typeof(StockLevelReadDto),
        typeof(OpenPurchaseOrderReadDto),
        typeof(WorkOrderReadDto),
        typeof(RoutingReadDto),
        typeof(OperationReadDto),
        typeof(CapacityAndCalendarReadDto),
        typeof(WorkCenterReadDto),
        typeof(WorkingShiftReadDto),
        typeof(HolidayReadDto),
        typeof(PlannedDowntimeReadDto),
        typeof(ShippingDurationReadDto)
    ];

    [Fact]
    public void Interface_IsPublicAndOwnedByApplicationAssembly()
    {
        var contractType = typeof(IErpDataProvider);

        Assert.True(contractType.IsPublic);
        Assert.True(contractType.IsInterface);
        Assert.Equal(typeof(OrderReadDto).Assembly, contractType.Assembly);
        Assert.Equal("App.Application", contractType.Assembly.GetName().Name);
    }

    [Fact]
    public void Interface_ContainsExactlyTheRequiredReadOperations()
    {
        var methods = typeof(IErpDataProvider).GetMethods();

        Assert.Equal(
            RequiredMethodNames.Order(),
            methods.Select(method => method.Name).Order());
        Assert.DoesNotContain(
            methods,
            method => ForbiddenOperationPrefixes.Any(prefix =>
                method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void EveryMethod_IsTaskReturningAndCancellationAware()
    {
        foreach (var method in typeof(IErpDataProvider).GetMethods())
        {
            Assert.True(method.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
            Assert.Equal(typeof(CancellationToken), method.GetParameters()[^1].ParameterType);
        }
    }

    [Fact]
    public void Contract_UsesOnlyBclAndApplicationOwnedTypes()
    {
        foreach (var method in typeof(IErpDataProvider).GetMethods())
        {
            AssertAllowedType(method.ReturnType);

            foreach (var parameter in method.GetParameters())
            {
                AssertAllowedType(parameter.ParameterType);
            }
        }
    }

    [Fact]
    public void CollectionOperations_ReturnReadOnlyLists()
    {
        var collectionMethods = new[]
        {
            "GetOrderItemsAsync",
            "GetOrderSummariesAsync",
            "GetProductBomAsync",
            "GetStockLevelsAsync",
            "GetOpenPurchaseOrdersAsync",
            "GetWorkOrdersAsync"
        };

        foreach (var methodName in collectionMethods)
        {
            var resultType = GetTaskResultType(methodName);

            Assert.True(resultType.IsGenericType);
            Assert.Equal(typeof(IReadOnlyList<>), resultType.GetGenericTypeDefinition());
        }
    }

    [Fact]
    public void SingleResourceLookups_HaveNullableReturnSemantics()
    {
        var nullability = new NullabilityInfoContext();

        foreach (var methodName in new[]
                 {
                     "GetOrderAsync", "GetProductAsync", "GetShippingDurationAsync"
                 })
        {
            var method = typeof(IErpDataProvider).GetMethod(methodName)!;
            var taskResult = nullability.Create(method.ReturnParameter).GenericTypeArguments[0];

            Assert.Equal(NullabilityState.Nullable, taskResult.ReadState);
        }
    }

    [Fact]
    public void Dtos_ArePublicSealedRecordsWithReadOnlyPublicProperties()
    {
        foreach (var dtoType in DtoTypes)
        {
            Assert.True(dtoType.IsPublic);
            Assert.True(dtoType.IsSealed);
            Assert.NotNull(dtoType.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public));
            Assert.All(
                dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public),
                property =>
                {
                    Assert.NotNull(property.SetMethod);
                    Assert.Contains(
                        typeof(IsExternalInit),
                        property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
                });
        }
    }

    [Fact]
    public void Dtos_ContainNoForbiddenTypesOrAttributes()
    {
        var forbiddenFragments = new[]
        {
            "App.Integration",
            "MockErp.Api",
            "App.Persistence",
            "App.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "System.Net.Http",
            "System.Text.Json"
        };

        foreach (var dtoType in DtoTypes)
        {
            Assert.DoesNotContain(
                dtoType.GetCustomAttributesData(),
                attribute => IsForbidden(attribute.AttributeType, forbiddenFragments));

            foreach (var property in dtoType.GetProperties())
            {
                Assert.False(IsForbidden(property.PropertyType, forbiddenFragments));
                Assert.DoesNotContain(
                    property.GetCustomAttributesData(),
                    attribute => IsForbidden(attribute.AttributeType, forbiddenFragments));
            }
        }
    }

    [Fact]
    public void DurationProperties_UseExplicitMinutesSuffixAndIntegerTypes()
    {
        var durationProperties = DtoTypes
            .SelectMany(type => type.GetProperties())
            .Where(property =>
                property.Name.Contains("Duration", StringComparison.Ordinal) ||
                property.Name.Contains("LeadTime", StringComparison.Ordinal) ||
                property.Name.Contains("Capacity", StringComparison.Ordinal) ||
                property.Name.Contains("Load", StringComparison.Ordinal) ||
                property.Name.Contains("Downtime", StringComparison.Ordinal))
            .Where(property => property.PropertyType == typeof(long) ||
                               property.PropertyType == typeof(long?))
            .ToArray();

        Assert.NotEmpty(durationProperties);
        Assert.All(
            durationProperties,
            property => Assert.EndsWith("Minutes", property.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void QuantityProperties_AreDecimal()
    {
        var quantityProperties = DtoTypes
            .SelectMany(type => type.GetProperties())
            .Where(property => property.Name.Contains("Quantity", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(quantityProperties);
        Assert.All(quantityProperties, property => Assert.Equal(typeof(decimal), property.PropertyType));
    }

    [Fact]
    public void ExternalReferenceProperties_AreStringsOrReadOnlyStringLists()
    {
        var referenceProperties = DtoTypes
            .SelectMany(type => type.GetProperties())
            .Where(property => property.Name.EndsWith("Reference", StringComparison.Ordinal) ||
                               property.Name.EndsWith("References", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(referenceProperties);
        Assert.All(
            referenceProperties,
            property => Assert.True(
                property.PropertyType == typeof(string) ||
                property.PropertyType == typeof(IReadOnlyList<string>),
                $"{property.DeclaringType?.Name}.{property.Name} must use a string reference."));
    }

    private static Type GetTaskResultType(string methodName)
    {
        return typeof(IErpDataProvider)
            .GetMethod(methodName)!
            .ReturnType
            .GetGenericArguments()[0];
    }

    private static void AssertAllowedType(Type type)
    {
        if (type.IsGenericType)
        {
            Assert.Contains(
                type.GetGenericTypeDefinition(),
                new[] { typeof(Task<>), typeof(IReadOnlyList<>) });

            foreach (var argument in type.GetGenericArguments())
            {
                AssertAllowedType(argument);
            }

            return;
        }

        Assert.True(
            type.Assembly == typeof(IErpDataProvider).Assembly ||
            (type.Namespace?.StartsWith("System", StringComparison.Ordinal) ?? false),
            $"Type {type.FullName} is not a BCL or Application-owned type.");
    }

    private static bool IsForbidden(Type type, IEnumerable<string> forbiddenFragments)
    {
        if (forbiddenFragments.Any(fragment =>
                (type.FullName ?? string.Empty).Contains(fragment, StringComparison.Ordinal)))
        {
            return true;
        }

        return type.IsGenericType &&
               type.GetGenericArguments().Any(argument => IsForbidden(argument, forbiddenFragments));
    }
}
