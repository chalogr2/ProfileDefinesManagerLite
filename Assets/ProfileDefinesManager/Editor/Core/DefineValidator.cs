using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProfileDefinesManager
{
    /// <summary>
    /// Categories of issue that can be found in a scripting define symbol.
    /// </summary>
    public enum ValidationIssueType
    {
        /// <summary>The symbol is null, empty, or contains only whitespace.</summary>
        Empty,

        /// <summary>The symbol has leading or trailing whitespace characters.</summary>
        LeadingTrailingWhitespace,

        /// <summary>The symbol starts with a digit (invalid for a C# preprocessor identifier).</summary>
        StartsWithDigit,

        /// <summary>The symbol contains characters other than letters, digits, or underscores.</summary>
        InvalidCharacters,

        /// <summary>The symbol appears more than once in the same collection (exact, case-sensitive match).</summary>
        Duplicate,

        /// <summary>
        /// Another symbol in the same collection spells the same name but with different casing,
        /// which can cause build inconsistencies.
        /// </summary>
        CasingMismatch,
    }

    /// <summary>
    /// A single validation issue discovered for a scripting define symbol.
    /// </summary>
    public readonly struct ValidationIssue
    {
        /// <summary>Gets the category of this issue.</summary>
        public ValidationIssueType Type { get; }

        /// <summary>Gets the symbol that has this issue (may contain the offending characters).</summary>
        public string Symbol { get; }

        /// <summary>Gets a human-readable description of this issue.</summary>
        public string Description { get; }

        /// <summary>Initializes a new <see cref="ValidationIssue"/>.</summary>
        /// <param name="type">The category of the issue.</param>
        /// <param name="symbol">The offending symbol string.</param>
        /// <param name="description">A human-readable description.</param>
        public ValidationIssue(ValidationIssueType type, string symbol, string description)
        {
            Type = type;
            Symbol = symbol;
            Description = description;
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{Type}] \"{Symbol}\": {Description}";
    }

    /// <summary>
    /// Validates scripting define symbols for correctness and cross-symbol consistency.
    /// </summary>
    public static class DefineValidator
    {
        // A valid C# preprocessor identifier: letter or underscore, followed by letters/digits/underscores.
        private static readonly Regex _validSymbol =
            new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        // ─── Single-symbol validation ─────────────────────────────────────────────

        /// <summary>
        /// Validates a single define symbol for syntactic correctness.
        /// </summary>
        /// <param name="symbol">The symbol string to validate.</param>
        /// <returns>
        /// A list of <see cref="ValidationIssue"/> items describing every problem found.
        /// An empty list means the symbol is valid.
        /// </returns>
        public static List<ValidationIssue> ValidateSymbol(string symbol)
        {
            var issues = new List<ValidationIssue>();

            if (string.IsNullOrWhiteSpace(symbol))
            {
                issues.Add(new ValidationIssue(
                    ValidationIssueType.Empty,
                    symbol ?? string.Empty,
                    "Symbol is empty or contains only whitespace."));
                return issues; // no further checks make sense
            }

            if (symbol != symbol.Trim())
            {
                issues.Add(new ValidationIssue(
                    ValidationIssueType.LeadingTrailingWhitespace,
                    symbol,
                    "Symbol has leading or trailing whitespace."));
            }

            var trimmed = symbol.Trim();

            if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                issues.Add(new ValidationIssue(
                    ValidationIssueType.StartsWithDigit,
                    symbol,
                    "Symbol must not start with a digit."));
            }

            if (!_validSymbol.IsMatch(trimmed))
            {
                issues.Add(new ValidationIssue(
                    ValidationIssueType.InvalidCharacters,
                    symbol,
                    "Symbol contains invalid characters. Only letters (A-Z, a-z), digits (0-9), and underscores (_) are allowed."));
            }

            return issues;
        }

        /// <summary>
        /// Returns <c>true</c> if the symbol is a syntactically valid C# preprocessor identifier
        /// with no leading or trailing whitespace.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        public static bool IsValid(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            return _validSymbol.IsMatch(symbol);
        }

        // ─── Collection validation ────────────────────────────────────────────────

        /// <summary>
        /// Validates a collection of define symbols, checking both per-symbol correctness
        /// and cross-symbol issues such as duplicates and casing mismatches.
        /// </summary>
        /// <param name="defines">The symbols to validate.</param>
        /// <returns>All <see cref="ValidationIssue"/> items found across the entire collection.</returns>
        public static List<ValidationIssue> ValidateSet(IEnumerable<string> defines)
        {
            var issues = new List<ValidationIssue>();
            var list = defines?.ToList() ?? new List<string>();

            // Per-symbol syntax checks
            foreach (var symbol in list)
                issues.AddRange(ValidateSymbol(symbol));

            // Exact-match duplicate detection (case-sensitive)
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in list)
            {
                if (!seen.Add(symbol) && !string.IsNullOrWhiteSpace(symbol))
                {
                    issues.Add(new ValidationIssue(
                        ValidationIssueType.Duplicate,
                        symbol,
                        $"Symbol \"{symbol}\" appears more than once."));
                }
            }

            // Casing-mismatch detection: same name, different capitalisation
            // Build a map from upper-cased key → first occurrence
            var canonicalMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in list)
            {
                var trimmed = symbol?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (canonicalMap.TryGetValue(trimmed, out var existing))
                {
                    // Different spelling → casing mismatch
                    if (!string.Equals(existing, trimmed, StringComparison.Ordinal))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationIssueType.CasingMismatch,
                            trimmed,
                            $"Symbol \"{trimmed}\" conflicts with \"{existing}\" (same name, different casing)."));
                    }
                }
                else
                {
                    canonicalMap[trimmed] = trimmed;
                }
            }

            return issues;
        }

        // ─── Normalization ────────────────────────────────────────────────────────

        /// <summary>
        /// Normalizes a collection of define symbols by trimming whitespace, removing empty entries,
        /// deduplicating (case-sensitive), and sorting alphabetically (case-insensitive).
        /// </summary>
        /// <param name="defines">The input symbols.</param>
        /// <returns>A new, normalized list of define symbols.</returns>
        public static List<string> Normalize(IEnumerable<string> defines)
        {
            if (defines == null)
                return new List<string>();

            return defines
                .Select(d => d?.Trim() ?? string.Empty)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
