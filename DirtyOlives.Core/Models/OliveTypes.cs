using System.Collections.Generic;

namespace DirtyOlives.Core.Models
{
    /// <summary>A named olive variety with a short tasting note.</summary>
    public record OliveTypeOption(string Name, string Description);

    /// <summary>
    /// The common olive varieties offered when rating a dirty martini.
    /// <see cref="MartiniRating.OliveType"/> stays a free string, so anything
    /// not listed here can still be typed in via the "Other" option.
    /// </summary>
    public static class OliveTypes
    {
        /// <summary>Sentinel shown in the dropdown to reveal a free text box.</summary>
        public const string Other = "Other";

        public static IReadOnlyList<OliveTypeOption> All { get; } = new List<OliveTypeOption>
        {
            new("Castelvetrano",
                "Bright green Sicilian olives with a buttery, mild, and slightly sweet taste. They pair exceptionally well with gin."),
            new("Manzanilla",
                "Classic Spanish green olives that are typically stuffed with pimento. They provide a sharp, salty, and direct briny punch."),
            new("Spanish Queen / Gordal",
                "Extra-large, meaty green olives with a clean, hearty bite and robust juice that makes fantastic brine."),
            new("Cerignola",
                "Giant southern Italian olives boasting a firm, meaty texture and a mild, buttery flavor."),
            new("Blue Cheese Stuffed",
                "Queen olives stuffed with pungent blue cheese for extra savory depth in vodka-heavy pours."),
            new("Garlic Stuffed",
                "Queen olives stuffed with savory garlic for extra savory depth in vodka-heavy pours."),
            new("Jalapeño Stuffed",
                "Queen olives stuffed with spicy jalapeño for extra heat in vodka-heavy pours.")
        };

        /// <summary>True when the value matches one of the listed varieties.</summary>
        public static bool IsKnown(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var option in All)
            {
                if (string.Equals(option.Name, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string? DescriptionFor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            foreach (var option in All)
            {
                if (string.Equals(option.Name, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return option.Description;
                }
            }

            return null;
        }
    }
}
