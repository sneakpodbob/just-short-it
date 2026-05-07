#!/usr/bin/dotnet run
#:package Spectre.Console@0.55.2
#:package BCrypt.Net-Next@4.1.0

using Spectre.Console;

AnsiConsole.Write(new Rule("BCrypt Hash Generator").RuleStyle("grey").LeftJustified());
AnsiConsole.MarkupLine("This tool creates a BCrypt hash locally.");
AnsiConsole.MarkupLine("No values are stored and nothing is sent anywhere.[grey] (Input stays in your local console process)[/]");
AnsiConsole.WriteLine();

var salt = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter an additional [green]salt[/]:")
        .Secret()
        .PromptStyle("green")
        .Validate(value => string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Error("Salt cannot be empty.")
            : ValidationResult.Success()));

var bcryptSalt = BCrypt.Net.BCrypt.GenerateSalt(workFactor: 12);

var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter the [green]password[/]:")
        .Secret()
        .PromptStyle("green")
        .Validate(value => string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Error("Password cannot be empty.")
            : ValidationResult.Success()));

var saltedPassword = string.Concat(password, salt);

var hash = BCrypt.Net.BCrypt.HashPassword(saltedPassword, bcryptSalt);

var interogationResult = BCrypt.Net.BCrypt.InterrogateHash(hash);

AnsiConsole.WriteLine($"Version: {interogationResult.Version}");
AnsiConsole.WriteLine($"Work Factor: {interogationResult.WorkFactor}");
AnsiConsole.WriteLine($"Settings: {interogationResult.Settings ?? "null"}");

var verify = BCrypt.Net.BCrypt.Verify(saltedPassword, hash);
AnsiConsole.WriteLine($"Verification of password + salt against hash: {(verify ? "[green]Success[/]" : "[red]Failure[/]")}");

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold]BCrypt hash (password + salt):[/]");
AnsiConsole.WriteLine(hash);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold]BCrypt salt:[/]");
AnsiConsole.WriteLine(salt);
AnsiConsole.MarkupLine("Done. Keep this hash safe and use it with your app configuration.");
