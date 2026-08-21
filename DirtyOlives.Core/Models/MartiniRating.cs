using System;
using System.Collections.Generic;
using System.Text;
using DirtyOlives.Core.Services;

namespace DirtyOlives.Core.Models
{
    public enum GlassStyle
    {
        Classic,
        Coupe,
        NickAndNora,
        Stemless,
        Rocks,
        Novelty
    }

    public class MartiniRating
    {
        public const int DefaultUserId = 1;

        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Owner of this rating. Authentication is not wired up yet, so this defaults to user 1.
        /// </summary>
        public int UserId { get; set; } = DefaultUserId;

        // Glass
        public GlassStyle GlassStyle { get; set; } = GlassStyle.Classic;
        public int GlassRating { get; set; }

        // Olives
        public int OliveCount { get; set; }
        public string OliveType { get; set; } = string.Empty;
        public int OlivesRating { get; set; }

        // Mixture
        public int MixtureRating { get; set; }

        // Vodka
        public string Vodka { get; set; } = string.Empty;
        public int VodkaRating { get; set; }

        // Ice crispys are a simple yes/no bonus
        public bool HasIceCrispys { get; set; }

        // Metadata
        public string Location { get; set; } = string.Empty;
        public DateTime DateRated { get; set; } = DateTime.Today;

        /// <summary>
        /// Overall score out of 10 olives, rounded to the nearest half olive.
        /// </summary>
        public double FinalRating => SidePieceRecommendationEngine.CalculateFinalRating(this);

        public string GlassStyleDisplay => GlassStyle switch
        {
            GlassStyle.NickAndNora => "Nick & Nora",
            _ => GlassStyle.ToString()
        };

        // Optional convenience property for display
        public string Summary =>
            $"{DateRated:d} • {Location} • {OliveCount} olives";
    }

}
