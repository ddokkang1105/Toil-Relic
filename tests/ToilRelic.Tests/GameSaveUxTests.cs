using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class GameSaveUxTests
{
    [Fact]
    public void Run_MissingSave_ShowsDiagnosisAndSavesBeforeQuit()
    {
        using var fixture = new GameFixture();

        var capture = CaptureConsole(new StringReader("1\n6\n"), () => new Game(fixture.System).Run());

        Assert.Contains("Start a new game to begin.", capture.Output);
        Assert.DoesNotContain("1. Continue", capture.Output);
        Assert.Contains("1. New Game", capture.Output);
        Assert.True(File.Exists(fixture.SavePath));
        Assert.True(capture.Output.IndexOf("Save: Saved just now", StringComparison.Ordinal) <
                    capture.Output.IndexOf("Game over", StringComparison.Ordinal));
        Assert.Equal(string.Empty, capture.Error);
    }

    [Fact]
    public void Run_ValidSave_OffersContinueAndUsesLoadedPlayer()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(new Player("Archivist")).Succeeded);

        var capture = CaptureConsole(new StringReader("1\n6\n"), () => new Game(fixture.System).Run());

        Assert.Contains("Save found. Continue or start a new game.", capture.Output);
        Assert.Contains("1. Continue", capture.Output);
        Assert.Contains("2. New Game", capture.Output);
        Assert.Contains("Archivist | HP", capture.Output);
        Assert.Equal(string.Empty, capture.Error);
    }

    [Fact]
    public void Run_UnreadableSave_DiagnosesWithoutMutationBeforeSelection()
    {
        using var fixture = new GameFixture();
        var original = "{ unreadable save";
        File.WriteAllText(fixture.SavePath, original);
        var input = new ScriptedTextReader();

        Assert.Throws<EndOfInputException>(() =>
            CaptureConsole(input, () => new Game(fixture.System).Run()));

        Assert.Contains("Save could not be read. Start New Game to replace it.", input.CapturedOutput);
        Assert.DoesNotContain("Continue", input.CapturedOutput);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Fact]
    public void Run_EmptyJsonObject_DiagnosesWithoutOfferingContinue()
    {
        using var fixture = new GameFixture();
        var original = "{}";
        File.WriteAllText(fixture.SavePath, original);
        var input = new ScriptedTextReader();

        Assert.Throws<EndOfInputException>(() =>
            CaptureConsole(input, () => new Game(fixture.System).Run()));

        Assert.Contains("Save could not be read. Start New Game to replace it.", input.CapturedOutput);
        Assert.DoesNotContain("Continue", input.CapturedOutput);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Fact]
    public void Run_ImpossibleCoreValues_DiagnosesWithoutOfferingContinueOrChangingBytes()
    {
        using var fixture = new GameFixture();
        var original = """
            {
              "Name": "Impossible Wanderer",
              "MaxHp": 0,
              "Hp": 0,
              "Level": 0,
              "Experience": -1,
              "TreasureCount": -1,
              "Inventory": { "999": -1 }
            }
            """;
        File.WriteAllText(fixture.SavePath, original);
        var input = new ScriptedTextReader();

        Assert.Throws<EndOfInputException>(() =>
            CaptureConsole(input, () => new Game(fixture.System).Run()));

        Assert.Contains("Save could not be read. Start New Game to replace it.", input.CapturedOutput);
        Assert.DoesNotContain("Continue", input.CapturedOutput);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Fact]
    public void Run_ReplacementFailure_RemainsOnTitleWithSafeCopy()
    {
        using var fixture = new GameFixture();
        var original = "locked unreadable bytes";
        File.WriteAllText(fixture.SavePath, original);
        using var lockStream = new FileStream(fixture.SavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var input = new ScriptedTextReader(new InputStep("1"));

        var exception = Assert.Throws<EndOfInputException>(() =>
            CaptureConsole(input, () => new Game(fixture.System).Run()));

        Assert.NotNull(exception);
        Assert.Equal(2, CountOccurrences(input.CapturedOutput, "Save could not be read. Start New Game to replace it."));
        Assert.DoesNotContain(fixture.SavePath, input.CapturedOutput);
        Assert.DoesNotContain("IOException", input.CapturedOutput);
        lockStream.Position = 0;
        using var reader = new StreamReader(lockStream, leaveOpen: true);
        Assert.Equal(original, reader.ReadToEnd());
    }

    [Fact]
    public void Run_SaveFailureThenRecovery_PrintsFinalOutcomesAfterActions()
    {
        using var fixture = new GameFixture(useBlockedParent: true);
        var input = new ScriptedTextReader(
            new InputStep("1"),
            new InputStep("4"),
            new InputStep(string.Empty, fixture.UnblockSaveDirectory),
            new InputStep("4"),
            new InputStep(string.Empty),
            new InputStep("6"));

        var capture = CaptureConsole(input, () => new Game(fixture.System).Run());
        var firstRest = capture.Output.IndexOf("[Rest]", StringComparison.Ordinal);
        var failed = capture.Output.IndexOf("Save: Failed", firstRest, StringComparison.Ordinal);
        var warning = capture.Output.IndexOf("Save failed. Progress may not be saved.", failed, StringComparison.Ordinal);
        var secondRest = capture.Output.IndexOf("[Rest]", warning, StringComparison.Ordinal);
        var recovered = capture.Output.IndexOf("Save: Saved just now", secondRest, StringComparison.Ordinal);

        Assert.True(firstRest >= 0);
        Assert.True(firstRest < failed);
        Assert.True(failed < warning);
        Assert.True(warning < secondRest);
        Assert.True(secondRest < recovered);
        Assert.True(File.Exists(fixture.SavePath));
        Assert.DoesNotContain(fixture.SavePath, capture.Output);
        Assert.False(string.IsNullOrWhiteSpace(capture.Error));
    }

    private static ConsoleCapture CaptureConsole(TextReader input, Action action)
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        if (input is ScriptedTextReader scripted)
        {
            scripted.Output = output;
        }

        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            Console.SetError(error);
            action();
            return new ConsoleCapture(output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += search.Length;
        }

        return count;
    }

    private sealed record ConsoleCapture(string Output, string Error);
    private sealed record InputStep(string Value, Action? BeforeRead = null);

    private sealed class ScriptedTextReader : TextReader
    {
        private readonly Queue<InputStep> _steps;

        public ScriptedTextReader(params InputStep[] steps)
        {
            _steps = new Queue<InputStep>(steps);
        }

        public StringWriter? Output { private get; set; }
        public string CapturedOutput => Output?.ToString() ?? string.Empty;

        public override string? ReadLine()
        {
            if (_steps.Count == 0)
            {
                throw new EndOfInputException();
            }

            var step = _steps.Dequeue();
            step.BeforeRead?.Invoke();
            return step.Value;
        }
    }

    private sealed class EndOfInputException : Exception
    {
    }

    private sealed class GameFixture : IDisposable
    {
        private readonly string? _blockedParent;

        public GameFixture(bool useBlockedParent = false)
        {
            DirectoryPath = Directory.CreateTempSubdirectory("toil-relic-game-tests-").FullName;
            if (useBlockedParent)
            {
                _blockedParent = Path.Combine(DirectoryPath, "save-parent");
                File.WriteAllText(_blockedParent, "not a directory");
                SavePath = Path.Combine(_blockedParent, "savegame.json");
            }
            else
            {
                SavePath = Path.Combine(DirectoryPath, "savegame.json");
            }

            System = new SaveSystem(SavePath);
        }

        public string DirectoryPath { get; }
        public string SavePath { get; }
        public SaveSystem System { get; }

        public void UnblockSaveDirectory()
        {
            Assert.NotNull(_blockedParent);
            File.Delete(_blockedParent);
            Directory.CreateDirectory(_blockedParent);
        }

        public void Dispose()
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
