using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Common;

/// <summary>
/// Shared helper for building a human-readable variant description string.
/// Extracted from duplicated logic in CartService, OrderService, and PaymentService.
/// </summary>
public static class VariantDescriptionBuilder
{
    public static string Build(ProductVariant? variant)
    {
        if (variant is null) return string.Empty;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(variant.Size)) parts.Add($"Size: {variant.Size}");
        if (!string.IsNullOrEmpty(variant.Color)) parts.Add($"Color: {variant.Color}");
        return string.Join(", ", parts);
    }
}
