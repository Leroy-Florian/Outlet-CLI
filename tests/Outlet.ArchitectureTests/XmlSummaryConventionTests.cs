using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Locks the documentation philosophy from CLAUDE.md: an XML doc comment is OPTIONAL — never
/// required on a type, method or property — and when it exists it must earn its place by
/// explaining the WHY (intent, constraint, trade-off) the identifier itself cannot carry.
///
/// A <c>&lt;summary&gt;</c> that merely paraphrases the name it documents adds nothing, so this
/// test fails any summary whose meaningful words are ALL already contained in that identifier.
/// Absence of a summary is always fine: this gate never asks for documentation, it only judges
/// the summaries that happen to exist.
///
/// Like the other source-scanning conventions (DateTime, primary constructors), XML doc comments
/// are not in IL, so this works on the source text. It is deliberately conservative — only
/// near-pure paraphrases are flagged — to keep false positives out of the architecture gate.
/// </summary>
public sealed class XmlSummaryConventionTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    private static readonly string[] ScopedRoots =
    [
        "src",
    ];

    private static readonly string[] ExcludedPathFragments =
    [
        "/bin/",
        "/obj/",
    ];

    // Pulls the inner text of the first <summary>…</summary> block out of a joined doc comment.
    private static readonly Regex SummaryPattern = new(
        @"<summary>(?<body>.*?)</summary>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    // Type declarations (class / interface / struct / enum / record [class|struct]).
    private static readonly Regex TypeDeclPattern = new(
        @"\b(?<kind>class|interface|struct|enum|record)\b(?:\s+(?:class|struct))?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    // A method / constructor: the identifier (with optional generic args) immediately before '('.
    private static readonly Regex MethodDeclPattern = new(
        @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>()]*>)?\s*\(",
        RegexOptions.Compiled);

    // A property / indexer: the identifier immediately before '{' or '=>'.
    private static readonly Regex PropertyDeclPattern = new(
        @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|=>)",
        RegexOptions.Compiled);

    // An enum member or field: a leading identifier (terminated by ',', '=', '{' or end of line).
    private static readonly Regex MemberDeclPattern = new(
        @"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:[,={]|$)",
        RegexOptions.Compiled);

    // Function words plus generic "doc verbs" that carry no intent on their own. A summary made
    // only of these (plus words already in the identifier) explains nothing the name didn't.
    // Note: domain nouns (value, type, item, user…) are intentionally NOT here — when they belong
    // to the name they are caught as paraphrase; when they don't, they are real information.
    private static readonly HashSet<string> FillerWords = new(StringComparer.Ordinal)
    {
        "the", "a", "an", "of", "for", "to", "that", "this", "these", "those", "and", "or", "nor",
        "but", "with", "without", "when", "whenever", "while", "into", "onto", "from", "on", "in",
        "by", "as", "at", "its", "it", "is", "are", "am", "be", "been", "being", "was", "were",
        "will", "would", "can", "could", "should", "may", "might", "must", "has", "have", "had",
        "do", "does", "did", "all", "any", "no", "not", "none", "one", "ones", "each", "every",
        "per", "via", "only", "just", "already", "yet", "here", "there", "then", "than", "so",
        "such", "up", "down", "over", "under", "out", "off", "also", "both", "either", "neither",
        "whether", "if", "else", "otherwise", "about", "against", "between", "among", "within",
        "across", "through", "after", "before", "during", "until", "upon", "given", "which", "who",
        "whose", "what", "where", "why", "how", "more", "most", "less", "least", "some", "other",
        "others", "we", "our", "they", "their", "them", "you", "your",
        // generic doc verbs (their inflections)
        "get", "gets", "getting", "got", "gotten", "set", "sets", "setting", "return", "returns",
        "returning", "returned", "represent", "represents", "representing", "represented",
        "provide", "provides", "providing", "provided", "contain", "contains", "containing",
        "contained", "hold", "holds", "holding", "held", "store", "stores", "storing", "stored",
        "define", "defines", "defining", "defined", "specify", "specifies", "specifying",
        "specified", "indicate", "indicates", "indicating", "indicated", "denote", "denotes",
        "describe", "describes", "describing", "described", "wrap", "wraps", "wrapping", "wrapped",
        "expose", "exposes", "exposing", "exposed", "encapsulate", "encapsulates", "use", "uses",
        "using", "used", "allow", "allows", "allowing", "allowed", "create", "creates", "creating",
        "created", "build", "builds", "building", "built", "make", "makes", "making", "made", "add",
        "adds", "adding", "added", "remove", "removes", "removing", "removed", "give", "gives",
    };

    [Fact]
    public void Xml_summaries_should_explain_intent_not_paraphrase_the_name()
    {
        var violations = new List<string>();

        foreach (var root in ScopedRoots)
        {
            var fullRoot = Path.Combine(RepoRoot, root);
            if (!Directory.Exists(fullRoot)) continue;

            foreach (var file in Directory.EnumerateFiles(fullRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcluded(file)) continue;

                foreach (var (line, name, summary) in FindParaphrasingSummaries(file))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(RepoRoot, file)}:{line} → '{name}': \"{Truncate(summary)}\"");
                }
            }
        }

        violations.Should().BeEmpty(
            "An XML <summary> exists to explain the WHY a name cannot carry; one that only " +
            "paraphrases the identifier should be deleted, not written (see CLAUDE.md → documentation). " +
            $"Found {violations.Count} paraphrasing summary(ies):{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    private static IEnumerable<(int Line, string Name, string Summary)> FindParaphrasingSummaries(string file)
    {
        var lines = File.ReadAllLines(file);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!IsDocLine(lines[i])) continue;

            // Collect the maximal run of consecutive /// lines.
            var blockStart = i;
            while (i < lines.Length && IsDocLine(lines[i])) i++;
            var blockEnd = i; // exclusive

            var summary = ExtractSummary(lines, blockStart, blockEnd);
            if (summary is null) continue;

            if (!TryFindDeclaration(lines, blockEnd, out var declLine, out var name)) continue;

            var summaryWords = SummaryContentWords(summary);
            if (summaryWords.Count == 0) continue; // empty / filler-only summaries are not our concern

            var nameWords = IdentifierWords(name);
            if (nameWords.Count == 0) continue;

            if (summaryWords.All(w => IsDerivedFrom(w, nameWords)))
                yield return (declLine + 1, name, summary.Trim());

            // step back one: the outer loop's i++ would skip the line after a non-doc line.
            i = blockEnd - 1;
        }
    }

    private static bool IsDocLine(string line) => line.TrimStart().StartsWith("///", StringComparison.Ordinal);

    private static string? ExtractSummary(string[] lines, int start, int end)
    {
        var joined = string.Join(
            ' ',
            lines[start..end].Select(l => l.TrimStart().TrimStart('/').Trim()));

        var match = SummaryPattern.Match(joined);
        return match.Success ? match.Groups["body"].Value : null;
    }

    private static bool TryFindDeclaration(string[] lines, int from, out int declLine, out string name)
    {
        declLine = -1;
        name = string.Empty;

        var attributeDepth = 0;
        for (var j = from; j < lines.Length; j++)
        {
            var trimmed = lines[j].Trim();

            // Skip attributes, which can span several lines via the bracket balance.
            if (attributeDepth > 0)
            {
                attributeDepth += Count(trimmed, '[') - Count(trimmed, ']');
                continue;
            }
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith('['))
            {
                attributeDepth += Count(trimmed, '[') - Count(trimmed, ']');
                continue;
            }

            // First real line below the doc block is the declaration it documents.
            if (TryExtractName(trimmed, out name))
            {
                declLine = j;
                return true;
            }
            return false;
        }

        return false;
    }

    private static bool TryExtractName(string declaration, out string name)
    {
        name = string.Empty;

        var type = TypeDeclPattern.Match(declaration);
        if (type.Success)
        {
            name = type.Groups["name"].Value;
            return true;
        }

        var method = MethodDeclPattern.Match(declaration);
        if (method.Success && method.Groups["name"].Value is not ("if" or "while" or "for" or "foreach" or "switch" or "catch" or "lock" or "using"))
        {
            name = method.Groups["name"].Value;
            return true;
        }

        var property = PropertyDeclPattern.Match(declaration);
        if (property.Success)
        {
            name = property.Groups["name"].Value;
            return true;
        }

        var member = MemberDeclPattern.Match(declaration);
        if (member.Success)
        {
            name = member.Groups["name"].Value;
            return true;
        }

        return false;
    }

    private static List<string> SummaryContentWords(string summary)
    {
        // Drop XML tags (and their cref/name attributes) entirely, then keep prose words only.
        var prose = Regex.Replace(summary, @"<[^>]+>", " ");

        return [.. SplitWords(prose).Where(w => w.Length >= 3 && !FillerWords.Contains(w))];
    }

    private static HashSet<string> IdentifierWords(string identifier)
    {
        // Interfaces: drop the leading 'I' so IRegistryClient → registry, client.
        if (identifier.Length >= 2 && identifier[0] == 'I' && char.IsUpper(identifier[1]))
            identifier = identifier[1..];

        return new HashSet<string>(
            SplitWords(identifier).Where(w => w != "async"),
            StringComparer.Ordinal);
    }

    private static IEnumerable<string> SplitWords(string text)
    {
        // camelCase / PascalCase boundaries → spaces, then split on any non-letter.
        var spaced = Regex.Replace(text, @"(?<=[a-z0-9])(?=[A-Z])", " ");
        spaced = Regex.Replace(spaced, @"(?<=[A-Z])(?=[A-Z][a-z])", " ");

        return Regex
            .Matches(spaced, "[A-Za-z]+")
            .Select(m => m.Value.ToLowerInvariant());
    }

    private static bool IsDerivedFrom(string summaryWord, HashSet<string> identifierWords)
    {
        foreach (var idWord in identifierWords)
        {
            if (summaryWord == idWord) return true;

            // A shared 4+ char prefix absorbs plural/verb forms (serialize/serializer, item/items).
            var shared = Math.Min(summaryWord.Length, idWord.Length);
            if (shared >= 4 && (summaryWord.StartsWith(idWord, StringComparison.Ordinal)
                                || idWord.StartsWith(summaryWord, StringComparison.Ordinal)))
                return true;

            var stem = Stem(summaryWord);
            if (stem.Length >= 4 && stem == Stem(idWord)) return true;
        }

        return false;
    }

    private static readonly string[] StemSuffixes =
    [
        "izations", "ization", "izes", "ized", "izers", "izer", "izing", "ize",
        "ements", "ement", "ments", "ment", "tions", "tion", "sions", "sion",
        "ings", "ing", "ers", "er", "ors", "or", "ies", "ied", "es", "ed", "s",
    ];

    private static string Stem(string word)
    {
        foreach (var suffix in StemSuffixes)
        {
            if (word.Length - suffix.Length >= 4 && word.EndsWith(suffix, StringComparison.Ordinal))
                return word[..^suffix.Length];
        }
        return word;
    }

    private static int Count(string text, char c)
    {
        var n = 0;
        foreach (var ch in text)
            if (ch == c) n++;
        return n;
    }

    private static string Truncate(string s) =>
        s.Length <= 120 ? s : s[..117] + "...";

    private static bool IsExcluded(string path)
    {
        var normalized = path.Replace('\\', '/');
        return ExcludedPathFragments.Any(frag => normalized.Contains(frag, StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateRepoRoot([CallerFilePath] string callerPath = "")
    {
        var dir = Path.GetDirectoryName(callerPath)!;
        for (var i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) && File.Exists(Path.Combine(dir, "Outlet.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
            if (string.IsNullOrEmpty(dir)) break;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from {callerPath}. Expected a directory with 'src/' and 'Outlet.slnx'.");
    }
}
