using Spectre.Console;

namespace Likvido.Kubesec;

public static class FindCommand
{
    public static int Run(
        string searchTerm,
        string? context = null,
        string? @namespace = null,
        string? namespaceIncludes = null,
        string? namespaceRegex = null,
        CancellationToken cancellationToken = default)
    {
        var kubeCtl = new KubeCtl(context);
        var allSecrets = kubeCtl.GetNamespacesWithSecrets(@namespace, namespaceIncludes, namespaceRegex, cancellationToken: cancellationToken);

        var results = SearchSecrets(allSecrets, searchTerm);

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No matches found for:[/] {Markup.Escape(searchTerm)}");
            return 0;
        }

        DisplayResults(searchTerm, results);
        return 0;
    }

    private static List<SearchResult> SearchSecrets(
        Dictionary<(string Namespace, string Name), IReadOnlyList<Secret>> allSecrets,
        string searchTerm)
    {
        var results = new List<SearchResult>();

        foreach (var secretEntry in allSecrets)
        {
            var (@namespace, secretName) = secretEntry.Key;

            foreach (var secret in secretEntry.Value)
            {
                var matches = FindMatchesWithContext(secret.Value, searchTerm, contextLines: 2);

                if (matches.Count > 0)
                {
                    results.Add(new SearchResult(
                        @namespace,
                        secretName,
                        secret.Name,
                        matches));
                }
            }
        }

        return results;
    }

    private static List<MatchContext> FindMatchesWithContext(
        string content,
        string searchTerm,
        int contextLines)
    {
        var matches = new List<MatchContext>();
        var lines = content.Split('\n');
        var matchedLineIndices = new HashSet<int>();

        // First pass: find all lines that contain the search term
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                matchedLineIndices.Add(i);
            }
        }

        // Second pass: create context blocks, merging overlapping ranges
        var processedLines = new HashSet<int>();

        foreach (var matchIndex in matchedLineIndices.OrderBy(x => x))
        {
            if (processedLines.Contains(matchIndex))
            {
                continue;
            }

            var startLine = Math.Max(0, matchIndex - contextLines);
            var endLine = Math.Min(lines.Length - 1, matchIndex + contextLines);

            // Extend range to include any other matches that fall within or adjacent
            var extendedEnd = endLine;
            foreach (var otherMatch in matchedLineIndices.Where(m => m > matchIndex && m <= endLine + contextLines))
            {
                extendedEnd = Math.Min(lines.Length - 1, otherMatch + contextLines);
                processedLines.Add(otherMatch);
            }

            endLine = extendedEnd;

            var contextText = string.Join('\n',
                lines.Skip(startLine).Take(endLine - startLine + 1));

            // Find which lines within this context block are matches
            var matchLinesInContext = matchedLineIndices
                .Where(m => m >= startLine && m <= endLine)
                .Select(m => m - startLine)
                .ToList();

            matches.Add(new MatchContext(
                LineNumber: matchIndex + 1, // 1-based line numbers
                ContextText: contextText,
                MatchLineIndices: matchLinesInContext));

            processedLines.Add(matchIndex);
        }

        return matches;
    }

    private static void DisplayResults(string searchTerm, List<SearchResult> results)
    {
        AnsiConsole.MarkupLine($"[bold]Finding:[/] {Markup.Escape(searchTerm)}");
        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.WriteLine();

        foreach (var result in results)
        {
            AnsiConsole.MarkupLine($"[green]>[/] [bold]{Markup.Escape(result.Namespace)}/{Markup.Escape(result.SecretName)}[/]");

            foreach (var match in result.Matches)
            {
                AnsiConsole.MarkupLine($"  [dim]Key:[/] {Markup.Escape(result.KeyName)}");
                AnsiConsole.MarkupLine($"  [dim]Line {match.LineNumber}:[/]");

                // Display context with highlighting
                var contextLines = match.ContextText.Split('\n');
                for (int i = 0; i < contextLines.Length; i++)
                {
                    var isMatchLine = match.MatchLineIndices.Contains(i);
                    var prefix = isMatchLine ? "[yellow]>[/]" : " ";
                    var line = Markup.Escape(contextLines[i]);

                    // Highlight the search term in matching lines
                    if (isMatchLine)
                    {
                        line = HighlightSearchTerm(contextLines[i], searchTerm);
                    }

                    AnsiConsole.MarkupLine($"    {prefix} {line}");
                }

                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.MarkupLine($"[bold]Found {results.Count} secret(s) containing the search term[/]");
    }

    private static string HighlightSearchTerm(string line, string searchTerm)
    {
        // Find all occurrences and highlight them
        var escapedLine = Markup.Escape(line);
        var escapedTerm = Markup.Escape(searchTerm);

        // Case-insensitive replacement for highlighting
        var index = 0;
        var result = escapedLine;

        while (true)
        {
            var foundIndex = result.IndexOf(escapedTerm, index, StringComparison.OrdinalIgnoreCase);
            if (foundIndex < 0)
            {
                break;
            }

            var actualMatch = result.Substring(foundIndex, escapedTerm.Length);
            var highlighted = $"[bold yellow]{actualMatch}[/]";
            result = result[..foundIndex] + highlighted + result[(foundIndex + escapedTerm.Length)..];
            index = foundIndex + highlighted.Length;
        }

        return result;
    }

    private record SearchResult(
        string Namespace,
        string SecretName,
        string KeyName,
        List<MatchContext> Matches);

    private record MatchContext(
        int LineNumber,
        string ContextText,
        List<int> MatchLineIndices);
}
