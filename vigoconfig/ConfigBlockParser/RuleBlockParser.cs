﻿using System.Text.RegularExpressions;
using Serilog;
using vigobase;

namespace vigoconfig;

internal class RuleBlockParser(PartialFolderConfigRule partialRule, SourceBlock codeBlock, CommonDefinitions commonDefinitions)
{
    #region local private class ConfigPhrase

    private class ConfigPhrase(bool required, Func<Tokenizer, PartialFolderConfigRule, ConfigPhrase, bool> parseFunc, string name)
    {
        public string Name { get; } = name;
        private bool Required { get; } = required;
        public bool ParsedSuccessfully { get; private set; }
        public bool PhraseFound { get; private set; }
        private int PhraseOccurenceCount { get; set; }
        private int NumberOfSuccessfulMatches { get; set; }
        public Func<Tokenizer, PartialFolderConfigRule, ConfigPhrase, bool> ParseFunc { get; set; } = parseFunc;
        public string? ErrorMessage { get; private set; }
        public SourceLine? SourceLine { get; set; }

        public bool HasErrors()
        {
            if (NumberOfSuccessfulMatches == 1 && PhraseOccurenceCount == 1)
                return false;
            if (Required)
                return true;
            return 0 < PhraseOccurenceCount;
        }

        public bool ReturnPhraseNotFound()
        {
            ParsedSuccessfully = false;
            PhraseFound = false;
            return PhraseFound;
        }

        public bool ReturnParseWithErrors(string errorMessage, SourceLine? errorSourceLine = null)
        {
            ParsedSuccessfully = false;
            PhraseFound = true;
            PhraseOccurenceCount++;
            ErrorMessage = errorMessage;
            if (errorSourceLine is not null)
                SourceLine = errorSourceLine;
            return PhraseFound;
        }

        public bool ReturnSuccessfullyParsed()
        {
            ParsedSuccessfully = true;
            PhraseFound = true;
            PhraseOccurenceCount++;
            NumberOfSuccessfulMatches++;
            return PhraseFound;
        }
    }

    #endregion
    
    public void Parse()
    {
        if (ParseValues()) 
            return;
        
        Log.Fatal("Failed to parse {TheBlockDescription}." +
                  " There is a syntax error or an unrecognized value in the line {TheLine}." +
                  " The error message was {TheErrorMessage}",
            codeBlock.Description,
            _lastErrorSourceLine,
            _lastErrorMessage);

        throw new VigoFatalException(AppEnv.Faults.Fatal(
            "FX154",
            null,
            "Failed to parse a folder configuration script. See log for details"));
    }

    private bool ParseValues()
    {
        var phrases = new List<ConfigPhrase>()
        {
            new ConfigPhrase(false, ParseRenameTo, "Rename To"),
            new ConfigPhrase(false, ParseNameReplacePattern, "Name Replace Pattern"),
            new ConfigPhrase(false, ParseFileMode, "File Mode"),
            new ConfigPhrase(false, ParseSourceEncoding, "Source Encoding"),
            new ConfigPhrase(false, ParseTargetEncoding, "Target Encoding"),
            new ConfigPhrase(false, ParseNewlineStyle, "Newline Style"),
            new ConfigPhrase(false, ParseAddTrailingNewline, "Add Trailing Newline"),
            new ConfigPhrase(false, ParseValidCharacters, "Valid Characters"),
            new ConfigPhrase(false, ParseBuildTargets, "Build Targets")
        };

        // if (codeBlock.Content.Contains("DO CHECK TEXT FILE IF FILE NAME IN check_ascii_german_list"))
        //     Log.Debug("Arrived at rule {TheRule}", codeBlock);
        
        if (!ParseRuleHeader(_tokenizer, partialRule, commonDefinitions))
        {
            _lastErrorSourceLine = _tokenizer.GetCurrentSourceLine();
            _lastErrorMessage = "A file rule section must start with 'DO [IGNORE|DEPLOY|CHECK] [TEXT|BINARY| ] FILE' optionally followed by 'IF NAME EQUALS <name>', 'IF NAME MATCHES <regex>' or 'IF NAME IN = <listname>'";
            return false;
        }

        while (!_tokenizer.AtEnd)
        {
            if (_tokenizer.TryReadTokens(["DONE"]))
            {
                var retval = true;
                var isFirstError = true;
                
                foreach (var phrase in phrases.Where(p => p.HasErrors()))
                {
                    if (isFirstError)
                    {
                        Log.Debug("Showing errors for {TheBlockDescription}", codeBlock.Description);
                        retval = false;
                        isFirstError = false;
                    }

                    _lastErrorSourceLine = phrase.SourceLine;
                    _lastErrorMessage = phrase.ErrorMessage;
                    
                    Log.Debug("{TheErrorMessage} on line {TheLine}", phrase.ErrorMessage, phrase.SourceLine);
                }
                
                return retval;
            }
            
            // Match the first phrase that has not already been matched
            
            var initialMatch = phrases
                .Where(e => !e.PhraseFound)
                .FirstOrDefault(p => p.ParseFunc(_tokenizer, partialRule, p));

            if (initialMatch is not null)
            {
                if (!initialMatch.HasErrors() && initialMatch.ParsedSuccessfully)
                    continue;
                
                _lastErrorSourceLine = initialMatch.SourceLine;
                _lastErrorMessage = initialMatch.ErrorMessage;
                    
                Log.Debug("Parsing failed with the message {TheErrorMessage} on line {TheLine}", initialMatch.ErrorMessage, initialMatch.SourceLine);
                
                return false;
            }
            
            // We are definitely in an error state. To give more information to the user
            // we check, if an already parsed line appears a second time

            _lastErrorSourceLine = _tokenizer.GetCurrentSourceLine();
            
            var duplicateMatch = phrases
                .Where(e => e.PhraseFound)
                .FirstOrDefault(p => p.ParseFunc(_tokenizer, partialRule, p));

            if (duplicateMatch is not null)
            {
                _lastErrorMessage = $"Parsing failed because of a duplicate entry for {duplicateMatch.Name} on line {_lastErrorSourceLine.LineNumber}";
                    
                Log.Debug("Parsing failed because of a duplicate entry for {ThePhrase} on line {TheLine}", duplicateMatch.Name, _lastErrorSourceLine);
                
                return false;
            }

            _lastErrorMessage = $"There is unrecognized content on line {_lastErrorSourceLine.LineNumber}";
            
            Log.Debug("There is unrecognized content on line {TheLine}", _lastErrorSourceLine);
            
            return false;
        }

        _lastErrorSourceLine = codeBlock.Lines[^1];
        _lastErrorMessage = "The folder defaults and settings section must be closet with DONE";
        return false;
    }

    private static bool ParseRuleHeader(Tokenizer tokenizer, PartialFolderConfigRule rule, CommonDefinitions commonDefinitions)
    {
        try
        {
            string fileType;
            
            // todo: remove CHECK after all configuration have been switched from CHECK to PREVIEW
            if (tokenizer.TryReadTokens(["DO"], ["IGNORE", "DEPLOY", "CHECK", "PREVIEW"], ["ALL"], ["TEXT", "", "BINARY"], ["FILES"]))
            {
                fileType = tokenizer.MatchedTokens[3];
            }
            // todo: remove CHECK after all configuration have been switched from CHECK to PREVIEW
            else if (tokenizer.TryReadTokens(["DO"], ["IGNORE", "DEPLOY", "CHECK", "PREVIEW"], ["TEXT", "", "BINARY"], ["FILE"], ["IF"], ["NAME"], ["EQUALS", "MATCHES", "IN"], ["*"]))
            {
                fileType = tokenizer.MatchedTokens[2];
            }
            else return false;
        
            rule.Action = tokenizer.MatchedTokens[1] switch
            {
                "IGNORE" => FileRuleActionEnum.IgnoreFile,
                "DEPLOY" => FileRuleActionEnum.DeployFile,
                "PREVIEW" => FileRuleActionEnum.PreviewFile,
                // todo: remove CHECK after all configuration have been switched from CHECK to PREVIEW
                "CHECK" => FileRuleActionEnum.PreviewFile,
                _ => throw new VigoFatalException(AppEnv.Faults.Fatal(
                    "FX189",
                    $"Expected the rule action to be in [IGNORE, DEPLOY, PREVIEW], but found {tokenizer.MatchedTokens[1]}",
                    "Failed to parse a folder configuration script. See log for details"))
            };

            if (!string.IsNullOrWhiteSpace(fileType))
            {
                rule.Handling ??= new PartialFolderConfigHandling();
                
                rule.Handling.FileType = fileType switch
                {
                    "TEXT" => FileTypeEnum.TextFile,
                    "BINARY" => FileTypeEnum.BinaryFile,
                    _ => throw new VigoFatalException(AppEnv.Faults.Fatal(
                        "FX196",
                        $"Expected the file type of the rule to be in [TEST, BINARY] but found the value {fileType}",
                        "Failed to parse a folder configuration script. See log for details"))
                };
            }

            switch (tokenizer.MatchedTokens.Count)
            {
                case 8:
                
                    rule.Condition = tokenizer.MatchedTokens[^2] switch
                    {
                        "EQUALS" => FileRuleConditionEnum.MatchName,
                        "MATCHES" => FileRuleConditionEnum.MatchPattern,
                        "IN" => FileRuleConditionEnum.MatchHandler,
                        _ => throw new VigoFatalException(AppEnv.Faults.Fatal(
                            "FX203",
                            $"Expected the condition clause of the rule to be either 'IF NAME EQUALS <name>' or 'IF NAME MATCHES <pattern>', but found '{string.Join(", ", tokenizer.MatchedTokens.Skip(4))}'",
                            "Failed to parse a folder configuration script. See log for details"))
                    };
                    
                    var conditionArg = tokenizer.MatchedTokens[^1];

                    if (rule.Condition is FileRuleConditionEnum.MatchName or FileRuleConditionEnum.MatchPattern)
                    {
                        rule.CompareWith = conditionArg;
                        break;
                    }

                    if (!commonDefinitions.NamedFileListDict.TryGetValue(conditionArg, out var namedFileList))
                        throw new VigoFatalException(AppEnv.Faults.Fatal(
                            "FX609",
                            null,
                            $"The file list {conditionArg} was not found"));
                    
                    rule.NameTestAndReplaceHandler = namedFileList;
                    rule.CompareWith = string.Empty;
                    
                    break;
                
                case 5:
                    
                    rule.Condition = FileRuleConditionEnum.Unconditional;
                    break;
            }

            return true;
        }
        catch (VigoRecoverableException e)
        {
            Log.Error(e, "Parsing the file rule header failed");
            return false;
        }
    }

    private static bool ParseRenameTo(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["RENAME"], ["TO"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (rule.Condition != FileRuleConditionEnum.MatchName)
        {
            Log.Error("The RENAME TO statement is only valid, when then rule's condition is {TheExpectedCondition}, not {TheObservedCondition}",
                FileRuleConditionEnum.MatchName,
                rule.Condition);
            return phrase.ReturnParseWithErrors("The RENAME TO statement is only valid, when then rule's condition is IF NAME EQUALS");    
        }
            
        rule.ReplaceWith = value;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseNameReplacePattern(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["NAME"], ["REPLACE"], ["PATTERN"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (rule.Condition != FileRuleConditionEnum.MatchPattern)
        {
            Log.Error("The NAME REPLACE PATTERN statement is only valid, when then rule's condition is {TheExpectedCondition}, not {TheObservedCondition}",
                FileRuleConditionEnum.MatchPattern,
                rule.Condition);
            return phrase.ReturnParseWithErrors("The NAME REPLACE PATTERN statement is only valid, when then rule's condition is IF NAME MATCHES");    
        }
            
        rule.ReplaceWith = value;

        return phrase.ReturnSuccessfullyParsed();
    }
    
    private static bool ParseFileMode(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["FILE"], ["MODE"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (!FilePermission.TryParse(value, out var permission))
        {
            Log.Debug("Could not derive a unix file mode from the value {TheFileModeValue}. Expecting three octal digits like 644 or a symbolic notation like ug+rw", value);
            return phrase.ReturnParseWithErrors($"Could not derive a unix file mode from the value {value}. Expecting three octal digits like 644 or a symbolic notation like ug+rw");
        }

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.Permissions = permission;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseSourceEncoding(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["SOURCE"], ["ENCODING"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (!FileEncodingEnumHelper.TryParse(value, out var encoding))
        {
            Log.Debug("Could not recognize a source encoding with the name {TheEncodingValue}. Valid names are: {ValidNames}", 
                value,
                FileEncodingEnumHelper.ValidNames);
            return phrase.ReturnParseWithErrors($"Could not recognize a source encoding with the name {value}. Valid names are: {string.Join(", ", FileEncodingEnumHelper.ValidNames)}");
        }

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.SourceFileEncoding = encoding;

        return phrase.ReturnSuccessfullyParsed();
    }
    
    private static bool ParseTargetEncoding(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["TARGET"], ["ENCODING"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (!FileEncodingEnumHelper.TryParse(value, out var encoding))
        {
            Log.Debug("Could not recognize a target encoding with the name {TheEncodingValue}. Valid names are: {ValidNames}", 
                value,
                FileEncodingEnumHelper.ValidNames);
            return phrase.ReturnParseWithErrors($"Could not recognize a target encoding with the name {value}. Valid names are: {string.Join(", ", FileEncodingEnumHelper.ValidNames)}");
        }

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.TargetFileEncoding = encoding;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseNewlineStyle(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["NEWLINE"], ["STYLE"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        if (!LineEndingEnumHelper.TryParse(value, out var lineEnding))
        {
            Log.Debug("Could not recognize the newline style with the name {TheEncodingValue}. Valid names are: {ValidNames}", 
                value,
                LineEndingEnumHelper.ValidNames);
            return phrase.ReturnParseWithErrors($"Could not recognize the newline style with the name {value}. Valid names are: {string.Join(", ", LineEndingEnumHelper.ValidNames)}");
        }

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.LineEnding = lineEnding;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseAddTrailingNewline(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["ADD"], ["TRAILING"], ["NEWLINE"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        bool addTrailingNewline;
            
        switch (value.ToLowerInvariant())
        {
            case "1":
            case "y":
            case "yes":
            case "true":
                addTrailingNewline = true;
                break;

            case "0":
            case "n":
            case "no":
            case "false":
                addTrailingNewline = false;
                break;

            default:
                Log.Debug("Could read the boolean value {TheFileModeValue} for the add trailing newline setting. Expecting one of: true, false, yes, no", value);
                return phrase.ReturnParseWithErrors($"Could read the boolean value {value} for the add trailing newline setting. Expecting one of: true, false, yes, no");
        }

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.FixTrailingNewline = addTrailingNewline;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseValidCharacters(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["VALID"], ["CHARACTERS"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        Regex? regexValidCharacters;
        
        try
        {
            regexValidCharacters = ValidCharactersHelper.ParseConfiguration(value);
        }
        catch (VigoException e)
        {
            Log.Debug(e, "Could not build a regular expression for the valid characters setting {TheFileModeValue}. Expecting All, Ascii or AsciiGerman, where Ascii and AsciiGerman may be followed by a plus sign and a sequence of additional characters", value);
            return phrase.ReturnParseWithErrors($"Could not build a regular expression for the valid characters setting {value}. Expecting All, Ascii or AsciiGerman, where Ascii and AsciiGerman may be followed by a plus sign and a sequence of additional characters");
        }
        
        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.IsDefinedValidCharsRegex = true;
        rule.Handling.ValidCharsRegex = regexValidCharacters;

        return phrase.ReturnSuccessfullyParsed();
    }

    private static bool ParseBuildTargets(Tokenizer tokenizer, PartialFolderConfigRule rule, ConfigPhrase phrase)
    {
        if (!tokenizer.TryReadTokens(["BUILD"], ["TARGETS"], ["*"]))
            return phrase.ReturnPhraseNotFound();

        phrase.SourceLine = tokenizer.GetCurrentSourceLine();

        var value = tokenizer.MatchedTokens[^1];

        var buildTargets = new List<string>();
        
        if (!value.Equals("NONE", StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                buildTargets.AddRange(DeploymentTargetHelper.ParseTargets(value));
            }
            catch (VigoException e)
            {
                Log.Debug(e,
                    "Failed to read the list of build targets {TheFileModeValue}. Expecting names separated by space, comma or semicolon, where the names themselves are composed of letters, digits, dash, underscore and period",
                    value);
                return phrase.ReturnParseWithErrors(
                    $"Failed to read the list of build targets {value}. Expecting names separated by space, comma or semicolon, where the names themselves are composed of letters, digits, dash, underscore and period");
            }
        }        

        rule.Handling ??= new PartialFolderConfigHandling();
        
        rule.Handling.Targets = buildTargets;

        return phrase.ReturnSuccessfullyParsed();
    }

    private readonly Tokenizer _tokenizer = new Tokenizer(codeBlock);
    private SourceLine? _lastErrorSourceLine;
    private string? _lastErrorMessage;
}