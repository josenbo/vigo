﻿namespace vigoconfig_tests;

public class TokenizerTests
{
    [Fact]
    public void Alternatives()
    {
        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        var expectedTokens = new List<string>() { "DO", "DEPLOY", "TEXT", "FILE", "IF", "NAME", "EQUALS", "." };
        var tokenizer = PrepareTokenizer("DO DEPLOY  TEXT    FILE IF NAME EQUALS                    .");

        var isMatch = tokenizer.TryReadTokens(["DO"], ["IGNORE", "DEPLOY", "CHECK"], ["TEXT", "", "BINARY"], ["FILE"], ["IF"], ["NAME"], ["EQUALS", "MATCHES"], ["*"]);
        
        Assert.True(isMatch);
        Assert.Equal(expectedTokens, tokenizer.MatchedTokens);
    }
    
    [Fact]
    public void OptionalAlternative()
    {
        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        var expectedTokens = new List<string>() { "DO", "DEPLOY", "", "FILE", "IF", "NAME", "MATCHES", "a" };
        var tokenizer = PrepareTokenizer("DO DEPLOY             FILE IF NAME MATCHES a"
        );

        var isMatch = tokenizer.TryReadTokens(["DO"], ["IGNORE", "DEPLOY", "CHECK"], ["TEXT", "", "BINARY"], ["FILE"], ["IF"], ["NAME"], ["EQUALS", "MATCHES"], ["*"]);
        
        Assert.True(isMatch);
        Assert.Equal(expectedTokens, tokenizer.MatchedTokens);
    }

    [Fact]
    public void MatchWildcard()
    {
        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        var expectedTokens = new List<string>() { "DO", "DEPLOY", "", "FILE", "IF", "NAME", "MATCHES", "weirdo[0-9]{1,3}.zippo-dong"};
        var tokenizer = PrepareTokenizer("DO DEPLOY             FILE IF NAME MATCHES weirdo[0-9]{1,3}.zippo-dong"
        );

        var isMatch = tokenizer.TryReadTokens( ["DO"], ["IGNORE", "DEPLOY", "CHECK"], ["TEXT", "", "BINARY"], ["FILE"], ["IF"], ["NAME"], ["EQUALS", "MATCHES"], ["*"]);

        Assert.True(isMatch);
        Assert.Equal(expectedTokens, tokenizer.MatchedTokens);
        
    }
    
    #region Helpers

    private static Tokenizer PrepareTokenizer(string codeToTest)
    {
        var lineNumber = 700;
        var sourceLines = new List<SourceLine>();
        
        foreach (var line in codeToTest.Split(LineSeparators))
        {
            sourceLines.Add(new SourceLine(lineNumber, line));
            lineNumber += 2;
        }

        var sourceBlock = new SourceBlockRule(sourceLines, codeToTest, "Unit test script block", 1, 1);

        var tokenizer = new Tokenizer(sourceBlock);

        return tokenizer;
    }
    
    public TokenizerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _logLevelSwitch = new LoggingLevelSwitch
        {
            MinimumLevel = LogEventLevel.Debug
        };

        Log.Logger = new LoggerConfiguration()
            // add the xunit test output sink to the serilog logger
            // https://github.com/trbenning/serilog-sinks-xunit#serilog-sinks-xunit
            .WriteTo.TestOutput(testOutputHelper)
            .MinimumLevel.ControlledBy(_logLevelSwitch)
            .CreateLogger();
    }
    
    // ReSharper disable once NotAccessedField.Local
    private readonly ITestOutputHelper _testOutputHelper;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly LoggingLevelSwitch _logLevelSwitch;
    private static readonly char[] LineSeparators = ['\r', '\n'];

    #endregion
}