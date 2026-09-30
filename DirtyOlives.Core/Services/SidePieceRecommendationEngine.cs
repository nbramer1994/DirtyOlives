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
		private const double OlivesWeight = 0.15;
		private const double MixtureWeight = 0.35;
		private const double VodkaWeight = 0.25;

		// Ice crispys are a bonus on top of the weighted score.
		private const double IceCrispysBonus = 0.5;

		/// <summary>Category names, used for both scoring and messaging.</summary>
		public const string GlassCategory = "Glass";
		public const string OlivesCategory = "Olives";
		public const string MixtureCategory = "Mixture";
		public const string VodkaCategory = "Vodka";

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
		/// Suggests side pieces based on the score tier, which category let the
		/// drink down, and the specific olives, vodka, and glass that were used.
		/// Every matching suggestion is returned, most specific first.
		/// </summary>
		public static SidePieceRecommendation Recommend(MartiniRating rating)
		{
			ArgumentNullException.ThrowIfNull(rating);

			var score = CalculateFinalRating(rating);
			var tier = GetTier(score);
			var weakest = WeakestCategory(rating);
			var strongest = StrongestCategory(rating);

			var options = BuildCandidates(rating, tier, weakest, strongest);

			// Candidates are built most specific first, so the head of the list is
			// always the most tailored thing we can say about this glass.
			var headline = options[0];

			return new SidePieceRecommendation
			{
				FinalRating = score,
				Tier = tier,
				TierLabel = GetTierLabel(tier),
				WeakestCategory = weakest,
				StrongestCategory = strongest,
				Name = headline.Name,
				Reason = headline.Reason,
				Options = options,
				Tips = BuildTips(rating, tier, weakest)
			};
		}

		/// <summary>
		/// All plausible suggestions for this martini, most specific first.
		/// </summary>
		public static IReadOnlyList<SidePieceOption> GetAllOptions(MartiniRating rating)
		{
			ArgumentNullException.ThrowIfNull(rating);

			var score = CalculateFinalRating(rating);
			return BuildCandidates(rating, GetTier(score), WeakestCategory(rating), StrongestCategory(rating));
		}

		private static IReadOnlyList<SidePieceOption> BuildCandidates(
			MartiniRating rating,
			SidePieceTier tier,
			string weakest,
			string strongest)
		{
			var options = new List<SidePieceOption>();

			// 1. Ingredient specific calls always come first - they are the most
			//    tailored thing we can say about this particular glass.
			options.AddRange(IngredientOptions(rating, tier));

			// 2. Then what to do about the weakest category.
			options.AddRange(WeaknessOptions(weakest, tier));

			// 3. Finally the generic options for this score tier.
			options.AddRange(TierOptions(tier, strongest));

			// Different rules can arrive at the same suggestion; keep the first.
			return options
				.GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First())
				.ToList();
		}

		private static IEnumerable<SidePieceOption> IngredientOptions(MartiniRating rating, SidePieceTier tier)
		{
			var olive = rating.OliveType ?? string.Empty;
			var vodka = rating.Vodka ?? string.Empty;

			if (Contains(olive, "blue cheese"))
			{
				yield return new("A wedge salad with extra blue cheese crumbles",
					"You are already committed to the funk, so lean all the way in.");
				yield return new("Buffalo wings, drums only",
					"Blue cheese olives and buffalo sauce were made for each other.");
			}

			if (Contains(olive, "jalape") || Contains(olive, "spicy"))
			{
				yield return new("Jalapeno poppers and a cold beer back",
					"The heat in those olives deserves a partner and a fire extinguisher.");
				yield return new("Elote or street corn",
					"Spicy olives want something sweet and charred alongside.");
			}

			if (Contains(olive, "garlic"))
			{
				yield return new("Garlic butter escargot or garlic bread",
					"Garlic stuffed olives say you have already given up on the rest of your evening plans.");
			}

			if (Contains(olive, "castelvetrano"))
			{
				yield return new("A marcona almond and manchego plate",
					"Buttery Castelvetranos belong on a proper Mediterranean board.");
			}

			if (Contains(olive, "cerignola") || Contains(olive, "gordal") || Contains(olive, "queen"))
			{
				yield return new("Prosciutto and melon",
					"Big meaty olives pair with something salty and cured.");
			}

			if (Contains(olive, "manzanilla"))
			{
				yield return new("Patatas bravas with aioli",
					"Spanish olives, Spanish snacks. Keep the theme going.");
			}

			if (rating.OliveCount >= 5)
			{
				yield return new("Nothing. You ordered a salad.",
					$"{rating.OliveCount} olives is not a garnish, it is a course.");
			}
			else if (rating.OliveCount <= 1)
			{
				yield return new("An extra olive skewer",
					"One lonely olive is a rounding error. Ask for three.");
			}

			if (Contains(vodka, "titos"))
			{
				yield return new("Queso and chips",
					"Tito's is from Texas, so eat accordingly.");
			}

			if (Contains(vodka, "grey goose") || Contains(vodka, "belvedere") || Contains(vodka, "ketel"))
			{
				if (tier >= SidePieceTier.Good)
				{
					yield return new("Oysters on the half shell",
						$"A pour like that {vodka} deserves brine that did not come from a jar.");
				}
				else
				{
					yield return new("A word with the bartender",
						$"{vodka} is good vodka. Something else went wrong here.");
				}
			}

			if (rating.GlassStyle == GlassStyle.Rocks)
			{
				yield return new("A properly chilled stem glass",
					"A martini in a rocks glass is a cry for help.");
			}
			else if (rating.GlassStyle == GlassStyle.Novelty)
			{
				yield return new("A photo for the group chat",
					"Whatever this was served in, the story is worth more than the drink.");
			}
			else if (rating.GlassStyle == GlassStyle.NickAndNora && rating.GlassRating >= 4)
			{
				yield return new("A second round in the same glassware",
					"Nick and Nora glasses never miss. Stay the course.");
			}

			if (rating.HasIceCrispys && tier >= SidePieceTier.Good)
			{
				yield return new("Another one before the crispys melt",
					"Ice crispys are a perishable good. Act fast.");
			}
		}

		private static IEnumerable<SidePieceOption> WeaknessOptions(string weakest, SidePieceTier tier)
		{
			// A strong drink does not need its weakest link fixed.
			if (tier >= SidePieceTier.Excellent)
			{
				yield break;
			}

			switch (weakest)
			{
				case OlivesCategory:
					yield return new("Blue cheese stuffed olive skewer",
						"The olives underdelivered, so bring your own backup.");
					yield return new("A side of olives you actually like",
						"Whatever came in the glass was not pulling its weight.");
					yield return new("A castelvetrano upgrade",
						"Ask what else they have behind the bar. Anything beats a tired olive.");
					break;

				case MixtureCategory:
					yield return new("Extra olive brine on the side",
						"The mixture was not dirty enough to carry the drink.");
					yield return new("A dirty martini, filthy this time",
						"Say the word filthy out loud next time and watch what happens.");
					yield return new("A shot of brine as a chaser",
						"If they will not put it in the glass, drink it separately.");
					break;

				case VodkaCategory:
					yield return new("A top shelf vodka upgrade",
						"The well pour dragged the whole glass down.");
					yield return new("A gin martini instead",
						"If the vodka is this rough, at least gin brings something to the party.");
					yield return new("A beer and a nap",
						"That vodka is not getting better with the second round.");
					break;

				case GlassCategory:
					yield return new("A chilled coupe swap",
						"The glassware was the weak link - ask for a proper chilled glass.");
					yield return new("A glass straight out of the freezer",
						"Room temperature stemware ruins a martini in about ninety seconds.");
					break;
			}
		}

		private static IEnumerable<SidePieceOption> TierOptions(SidePieceTier tier, string strongest)
		{
			switch (tier)
			{
				case SidePieceTier.Perfect:
					yield return new("Another one of these",
						"Near perfect pour - do not change a thing.");
					yield return new("The bartender's name and a generous tip",
						"This is a relationship worth maintaining.");
					yield return new("A standing reservation",
						"You found it. Stop looking.");
					yield return new("Caviar service, obviously",
						"A ten olive martini has earned the good stuff.");
					break;

				case SidePieceTier.Excellent:
					yield return new("Oysters and a second round",
						$"The {strongest.ToLowerInvariant()} carried this one. Ride the wave.");
					yield return new("A shrimp cocktail",
						"Cold, snappy, and it will not get in the martini's way.");
					yield return new("Steak frites",
						"A martini this good deserves an actual dinner around it.");
					yield return new("A cheese board and no plans",
						"Settle in, this bar knows what it is doing.");
					break;

				case SidePieceTier.Good:
					yield return new("Salted nuts and a water back",
						"Everything was solid; just keep the palate honest.");
					yield return new("Truffle fries",
						"Good not great - let the food do some of the heavy lifting.");
					yield return new("Deviled eggs",
						"Briny, salty, and forgiving of a merely decent martini.");
					yield return new("A charcuterie plate",
						"Salt will flatter this one into a better drink than it is.");
					break;

				case SidePieceTier.Mediocre:
					yield return new("Bar snacks and lowered expectations",
						"This one is not the main event, so make the food the main event.");
					yield return new("A basket of fries",
						"Salt and starch will paper over most of what went wrong.");
					yield return new("A glass of water, then reassess",
						"Reset the palate before you commit to a second.");
					yield return new("Whatever the person next to you ordered",
						"They clearly know something you do not.");
					break;

				case SidePieceTier.Poor:
					yield return new("A different bar",
						"Cut your losses and walk somewhere with a cold shaker.");
					yield return new("A beer, honestly",
						"Hard to ruin a beer. Same cannot be said here.");
					yield return new("The check",
						"This martini has told you everything you need to know.");
					yield return new("A sincere apology from the kitchen",
						"Somebody back there owes you something.");
					break;
			}
		}

		private static IReadOnlyList<string> BuildTips(MartiniRating rating, SidePieceTier tier, string weakest)
		{
			var tips = new List<string>();

			if (!rating.HasIceCrispys && tier < SidePieceTier.Perfect)
			{
				tips.Add("Ask them to shake it harder next time for ice crispys.");
			}

			if (rating.OliveCount == 0)
			{
				tips.Add("Zero olives in a dirty martini is a bold and incorrect choice.");
			}
			else if (rating.OliveCount % 2 == 0)
			{
				tips.Add("Olives are traditionally served in odd numbers - ask for one more.");
			}

			if (rating.MixtureRating <= 2 && weakest == MixtureCategory)
			{
				tips.Add("Specify how many barspoons of brine you want; most bartenders under pour.");
			}

			if (rating.GlassRating <= 2)
			{
				tips.Add("A quick swirl of ice water in the glass before pouring costs nothing.");
			}

			if (rating.VodkaRating == 5 && rating.MixtureRating <= 3)
			{
				tips.Add("Great vodka and a weak mixture is a fixable problem - just ask for it dirtier.");
			}

			if (tier >= SidePieceTier.Excellent)
			{
				tips.Add("Write down the bartender's name in your notes while you still remember it.");
			}

			return tips;
		}

		private static SidePieceTier GetTier(double score) => score switch
		{
			>= 9d => SidePieceTier.Perfect,
			>= 7.5d => SidePieceTier.Excellent,
			>= 6d => SidePieceTier.Good,
			>= 4d => SidePieceTier.Mediocre,
			_ => SidePieceTier.Poor
		};

		private static string GetTierLabel(SidePieceTier tier) => tier switch
		{
			SidePieceTier.Perfect => "Top shelf",
			SidePieceTier.Excellent => "Excellent",
			SidePieceTier.Good => "Solid",
			SidePieceTier.Mediocre => "Drinkable",
			_ => "Rough"
		};

		/// <summary>
		/// Deterministic per-rating seed, kept for any caller that wants to vary
		/// presentation without reshuffling on every re-render.
		/// </summary>
		public static int GetSeed(MartiniRating rating)
		{
			ArgumentNullException.ThrowIfNull(rating);

			var hash = rating.Id == Guid.Empty
				? HashCode.Combine(
					rating.GlassRating,
					rating.OlivesRating,
					rating.MixtureRating,
					rating.VodkaRating,
					rating.OliveCount,
					rating.HasIceCrispys)
				: rating.Id.GetHashCode();

			return Math.Abs(hash == int.MinValue ? 0 : hash);
		}

		private static bool Contains(string value, string term) =>
			!string.IsNullOrWhiteSpace(value) &&
			value.Contains(term, StringComparison.OrdinalIgnoreCase);

		private static string WeakestCategory(MartiniRating rating) =>
			Categories(rating).OrderBy(c => c.Value).First().Key;

		private static string StrongestCategory(MartiniRating rating) =>
			Categories(rating).OrderByDescending(c => c.Value).First().Key;

		private static Dictionary<string, int> Categories(MartiniRating rating) => new()
		{
			[GlassCategory] = rating.GlassRating,
			[OlivesCategory] = rating.OlivesRating,
			[MixtureCategory] = rating.MixtureRating,
			[VodkaCategory] = rating.VodkaRating
		};
	}

	/// <summary>How good the martini was, used to pick the tone of the suggestion.</summary>
	public enum SidePieceTier
	{
		Poor = 0,
		Mediocre = 1,
		Good = 2,
		Excellent = 3,
		Perfect = 4
	}

	/// <summary>A single suggestion with the reasoning behind it.</summary>
	public record SidePieceOption(string Name, string Reason);

	public class SidePieceRecommendation
	{
		public double FinalRating { get; set; }
		public SidePieceTier Tier { get; set; }
		public string TierLabel { get; set; } = string.Empty;
		public string WeakestCategory { get; set; } = string.Empty;
		public string StrongestCategory { get; set; } = string.Empty;

		/// <summary>The most specific suggestion, also the first entry in <see cref="Options"/>.</summary>
		public string Name { get; set; } = string.Empty;
		public string Reason { get; set; } = string.Empty;

		/// <summary>Every suggestion that applies to this martini, most specific first.</summary>
		public IReadOnlyList<SidePieceOption> Options { get; set; } = new List<SidePieceOption>();

		/// <summary>Every coaching note that applies for next time. May be empty.</summary>
		public IReadOnlyList<string> Tips { get; set; } = new List<string>();
	}
}
