#!/usr/bin/dotnet run
#:package Spectre.Console@0.49.1
#:package BCrypt.Net-Next@4.0.3

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

var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter the [green]password[/]:")
        .Secret()
        .PromptStyle("green")
        .Validate(value => string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Error("Password cannot be empty.")
            : ValidationResult.Success()));

var saltedPassword = string.Concat(password, salt);
var hash = BCrypt.Net.BCrypt.HashPassword(saltedPassword);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold]BCrypt hash (password + salt):[/]");
AnsiConsole.WriteLine(hash);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("Done. Keep this hash safe and use it with your app configuration.");
