using System;
using System.Collections.Generic;

namespace DirtyOlives.Core.Models
{
    /// <summary>
    /// The vodkas offered when rating a dirty martini.
    /// <see cref="MartiniRating.Vodka"/> stays a free string, so anything not
    /// listed here can still be typed in via the "Other" option.
    /// </summary>
    public static class VodkaTypes
    {
        /// <summary>Sentinel shown in the dropdown to reveal a free text box.</summary>
        public const string Other = "Other";

        public static IReadOnlyList<string> All { get; } = new List<string>
        {
            "Ketel One",
            "Belvedere",
            "Grey Goose",
            "Tito's"
        };

        /// <summary>True when the value matches one of the listed vodkas.</summary>
        public static bool IsKnown(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var vodka in All)
            {
                if (string.Equals(vodka, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
