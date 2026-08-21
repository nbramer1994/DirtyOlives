using System;
using System.Collections.Generic;
using System.Linq;
using DirtyOlives.Core.Models;

namespace DirtyOlives.Core.Services
{
    /// <summary>
    /// Turns the individual category ratings into an overall olive score and a
    /// "side piece recommendation" - what to order alongside (or instead of) this martini.
    /// </summary>
    public static class SidePieceRecommendationEngine
    {
        // Relative weight of each 1-5 category in the final score.
        private const double GlassWeight = 0.15;
        private const double OlivesWeight = 0.30;
        private const double MixtureWeight = 0.30;
        private const double VodkaWeight = 0.25;

        // Ice crispys are a bonus on top of the weighted score.
        private const double IceCrispysBonus = 0.5;

        /// <summary>
        /// Final score out of 10 olives, rounded to the nearest half olive.
        /// </summary>
        public static double CalculateFinalRating(MartiniRating rating)
        {
            ArgumentNullException.ThrowIfNull(rating);

            var weighted =
                rating.GlassRating * GlassWeight +
                rating.OlivesRating * OlivesWeight +
                rating.MixtureRating * MixtureWeight +
                rating.VodkaRating * VodkaWeight;

            // Weighted average is on a 0-5 scale, so double it for a 0-10 scale.
            var score = weighted * 2d;

            if (rating.HasIceCrispys)
            {
                score += IceCrispysBonus;
            }

            score = Math.Clamp(score, 0d, 10d);

            // Round to the nearest half olive.
            return Math.Round(score * 2d, MidpointRounding.AwayFromZero) / 2d;
        }

        /// <summary>
        /// Suggests a side piece based on which parts of the martini were strong or weak.
        /// </summary>
        public static SidePieceRecommendation Recommend(MartiniRating rating)
        {
            ArgumentNullException.ThrowIfNull(rating);

            var score = CalculateFinalRating(rating);
            var weakest = WeakestCategory(rating);

            var (name, reason) = weakest switch
            {
                _ when score >= 9 => ("Another one of these", "Near perfect pour - do not change a thing."),
                "Olives" => ("Blue cheese stuffed olive skewer", "The olives underdelivered, so bring your own backup."),
                "Mixture" => ("Extra olive brine on the side", "The mixture was not dirty enough to carry the drink."),
                "Vodka" => ("A top shelf vodka upgrade", "The well pour dragged the whole glass down."),
                "Glass" => ("A chilled coupe swap", "The glassware was the weak link - ask for a proper chilled glass."),
                _ => ("Salted nuts and a water back", "Everything was solid; just keep the palate honest.")
            };

            if (!rating.HasIceCrispys && score < 9)
            {
                reason += " Ask them to shake it harder next time for ice crispys.";
            }

            return new SidePieceRecommendation
            {
                FinalRating = score,
                WeakestCategory = weakest,
                Name = name,
                Reason = reason
            };
        }

        private static string WeakestCategory(MartiniRating rating)
        {
            var categories = new Dictionary<string, int>
            {
                ["Glass"] = rating.GlassRating,
                ["Olives"] = rating.OlivesRating,
                ["Mixture"] = rating.MixtureRating,
                ["Vodka"] = rating.VodkaRating
            };

            return categories.OrderBy(c => c.Value).First().Key;
        }
    }

    public class SidePieceRecommendation
    {
        public double FinalRating { get; set; }
        public string WeakestCategory { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
