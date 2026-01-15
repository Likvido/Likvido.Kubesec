using Spectre.Console;

namespace Likvido.Kubesec;

public static class UpdateValueCommand
{
    public static int Run(
        string oldValue,
        string newValue,
        string? context = null,
        string? @namespace = null,
        string? namespaceIncludes = null,
        string? namespaceRegex = null,
        bool skipPrompts = false,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (oldValue == newValue)
        {
            AnsiConsole.MarkupLine("[red]Error: Old and new values are identical[/]");
            return 1;
        }

        // Step 1: Create backup folder
        var backupFolder = $"kubesec-rollback-{DateTime.Now:yyyyMMdd-HHmmss}";

        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.MarkupLine("[bold]Replace Values[/]");
        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"Old value: [yellow]{Markup.Escape(TruncateForDisplay(oldValue, 60))}[/]");
        AnsiConsole.MarkupLine($"New value: [yellow]{Markup.Escape(TruncateForDisplay(newValue, 60))}[/]");
        AnsiConsole.WriteLine();

        var kubeCtl = new KubeCtl(context);
        Dictionary<(string Namespace, string Name), IReadOnlyList<Secret>> allSecrets;

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[dim]Dry run mode - no changes will be made[/]");
            AnsiConsole.WriteLine();
            allSecrets = kubeCtl.GetNamespacesWithSecrets(@namespace, namespaceIncludes, namespaceRegex, cancellationToken: cancellationToken);
        }
        else
        {
            AnsiConsole.MarkupLine($"Creating backup to [cyan]{backupFolder}/[/]");
            AnsiConsole.WriteLine();
            allSecrets = BackupCommand.CreateBackup(kubeCtl, backupFolder, context, @namespace, namespaceIncludes, namespaceRegex, cancellationToken: cancellationToken);
        }

        // Step 2: Find matches in the backed up secrets
        var matchingSecrets = new List<SecretUpdateInfo>();

        AnsiConsole.MarkupLine("Finding secrets containing the old value...");
        AnsiConsole.WriteLine();

        foreach (var secretEntry in allSecrets)
        {
            var (ns, secretName) = secretEntry.Key;
            var secrets = secretEntry.Value;

            // Check for matches in each secret key
            foreach (var secret in secrets)
            {
                if (secret.Value.Contains(oldValue, StringComparison.Ordinal))
                {
                    var occurrenceCount = CountOccurrences(secret.Value, oldValue);
                    matchingSecrets.Add(new SecretUpdateInfo(
                        ns,
                        secretName,
                        secret.Name,
                        secret.Value,
                        secret.Value.Replace(oldValue, newValue),
                        occurrenceCount,
                        secrets.ToList()));
                }
            }
        }

        if (matchingSecrets.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No secrets found containing the specified value.[/]");

            // Clean up empty backup folder
            if (!dryRun && Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
                AnsiConsole.MarkupLine("[dim]Removed empty backup folder.[/]");
            }

            return 0;
        }

        // Group by namespace/secret for counting unique secrets
        var uniqueSecrets = matchingSecrets
            .GroupBy(m => (m.Namespace, m.SecretName))
            .ToList();

        AnsiConsole.MarkupLine($"Found [green]{uniqueSecrets.Count}[/] secret(s) containing the value");
        AnsiConsole.WriteLine();

        // Step 3: Display preview with diff-style output
        foreach (var match in matchingSecrets)
        {
            DisplayDiff(match, oldValue, newValue);
        }

        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.MarkupLine($"[bold]Total: {uniqueSecrets.Count} secret(s) will be updated[/]");
        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.WriteLine();

        // Step 4: Handle dry run or confirm with user
        if (dryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run complete - no changes were made.[/]");

            if (!string.IsNullOrEmpty(backupFolder) && Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
            }

            return 0;
        }

        if (!skipPrompts)
        {
            if (!PromptContinue("Apply changes?"))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                AnsiConsole.MarkupLine($"[dim]Backup preserved at:[/] {backupFolder}/");
                return 0;
            }
        }

        // Step 5: Apply changes
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Updating secrets...");

        var successCount = 0;
        var failedSecrets = new List<string>();

        foreach (var group in uniqueSecrets)
        {
            var (ns, secretName) = group.Key;
            var firstMatch = group.First();

            // Create updated secrets list
            var updatedSecrets = firstMatch.AllSecrets.Select(s =>
            {
                var matchForKey = group.FirstOrDefault(m => m.KeyName == s.Name);
                if (matchForKey != null)
                {
                    return new Secret(s.Name, matchForKey.NewValue);
                }

                return s;
            }).ToList();

            try
            {
                ApplySecretUpdate(kubeCtl, secretName, ns, updatedSecrets);
                AnsiConsole.MarkupLine($"  [green]OK[/] {Markup.Escape(ns)}/{Markup.Escape(secretName)}");
                successCount++;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]FAILED[/] {Markup.Escape(ns)}/{Markup.Escape(secretName)}: {Markup.Escape(ex.Message)}");
                failedSecrets.Add($"{ns}/{secretName}");
            }
        }

        // Step 6: Report results
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine(new string('=', 45));
        AnsiConsole.MarkupLine($"[bold]Done! Updated {successCount} secret(s).[/]");
        AnsiConsole.WriteLine(new string('=', 45));

        if (failedSecrets.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]Failed: {failedSecrets.Count} secret(s)[/]");
            foreach (var failed in failedSecrets)
            {
                AnsiConsole.MarkupLine($"  - {Markup.Escape(failed)}");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Rollback:[/] kubesec restore -c {context ?? "<context>"} {backupFolder}/");

        return failedSecrets.Count > 0 ? 1 : 0;
    }

    private static void DisplayDiff(SecretUpdateInfo match, string oldValue, string newValue)
    {
        AnsiConsole.MarkupLine($"[green]>[/] [bold]{Markup.Escape(match.Namespace)}/{Markup.Escape(match.SecretName)}[/]");
        AnsiConsole.MarkupLine($"  {match.OccurrenceCount} occurrence(s) will be replaced");

        // Find the line containing the match and show context
        var lines = match.OldValue.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(oldValue, StringComparison.Ordinal))
            {
                var oldLine = TruncateForDisplay(lines[i].Trim(), 80);
                var newLine = TruncateForDisplay(lines[i].Replace(oldValue, newValue).Trim(), 80);

                AnsiConsole.MarkupLine($"  [dim]Line {i + 1}:[/]");
                AnsiConsole.MarkupLine($"    [red]- {Markup.Escape(oldLine)}[/]");
                AnsiConsole.MarkupLine($"    [green]+ {Markup.Escape(newLine)}[/]");

                // Only show first few matches per secret to avoid overwhelming output
                if (i > 5)
                {
                    AnsiConsole.MarkupLine($"    [dim]... and more[/]");
                    break;
                }
            }
        }

        AnsiConsole.WriteLine();
    }

    private static string TruncateForDisplay(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var halfLength = (maxLength - 3) / 2;
        return $"{value[..halfLength]}...{value[^halfLength..]}";
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static void ApplySecretUpdate(
        KubeCtl kubeCtl,
        string secretName,
        string @namespace,
        List<Secret> secrets)
    {
        var filePath = PushCommand.CreateSecretsTempFile(secretName, @namespace, secrets);

        try
        {
            kubeCtl.ApplyFile(filePath);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static bool PromptContinue(string question)
    {
        ConsoleKey response;
        do
        {
            Console.Write($"{question} [y/N] ");
            response = Console.ReadKey(false).Key;
            if (response != ConsoleKey.Enter)
            {
                Console.WriteLine();
            }
        } while (response != ConsoleKey.Y && response != ConsoleKey.N && response != ConsoleKey.Enter);

        return response == ConsoleKey.Y;
    }

    private record SecretUpdateInfo(
        string Namespace,
        string SecretName,
        string KeyName,
        string OldValue,
        string NewValue,
        int OccurrenceCount,
        List<Secret> AllSecrets);
}
