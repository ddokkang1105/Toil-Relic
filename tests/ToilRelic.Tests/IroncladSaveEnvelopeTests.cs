using System.Text;
using System.Text.Json;
using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class IroncladSaveEnvelopeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Fact]
    public void Save_WithValidLive_RotatesExactLiveBytesAndPromotesCandidate()
    {
        using var fixture = new EnvelopeFixture();
        var first = fixture.Player("Current A");
        Assert.True(fixture.System.Save(first).Succeeded);
        var oldLive = File.ReadAllBytes(fixture.LivePath);

        var result = fixture.System.Save(fixture.Player("Current B"));

        Assert.True(result.Succeeded, result.Diagnostic);
        Assert.Equal(oldLive, File.ReadAllBytes(fixture.LastKnownGoodPath));
        Assert.Equal("Current B", fixture.FreshSystem().Load().Player!.Name);
        Assert.False(File.Exists(fixture.StagePath));
    }

    [Fact]
    public void Load_WithInvalidLiveAndValidLastKnownGood_QuarantinesAndRecoversExactBytes()
    {
        using var fixture = new EnvelopeFixture();
        Assert.True(fixture.System.Save(fixture.Player("Recover Me")).Succeeded);
        var recoveryBytes = File.ReadAllBytes(fixture.LivePath);
        File.Move(fixture.LivePath, fixture.LastKnownGoodPath);
        var damagedBytes = Encoding.UTF8.GetBytes("{ damaged-live");
        File.WriteAllBytes(fixture.LivePath, damagedBytes);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Recovered, result.Status);
        Assert.True(result.RecoveryNoticePending);
        Assert.Equal("Recover Me", result.Player!.Name);
        Assert.Equal(recoveryBytes, File.ReadAllBytes(fixture.LivePath));
        Assert.Equal(recoveryBytes, File.ReadAllBytes(fixture.LastKnownGoodPath));
        Assert.Equal(damagedBytes, File.ReadAllBytes(fixture.QuarantinePath));
        Assert.True(File.Exists(fixture.RecoveryMarkerPath));
    }

    [Fact]
    public void Save_WhenStageWriteFails_PreservesPriorAuthorityAndReturnsRedactedDiagnostic()
    {
        using var fixture = new EnvelopeFixture();
        Assert.True(fixture.System.Save(fixture.Player("Secret Player")).Succeeded);
        var priorLive = File.ReadAllBytes(fixture.LivePath);
        var operations = new SaveEnvelopeFileOperations((checkpoint, side) =>
        {
            if (checkpoint == SaveEnvelopeCheckpoint.StageWrite && side == SaveEnvelopeMutationSide.Before)
            {
                File.WriteAllText(fixture.StagePath, "partial Secret Player");
                throw new IOException("absolute root: " + fixture.DirectoryPath);
            }
        });
        var faulted = new SaveSystem(fixture.LivePath, operations);

        var result = faulted.Save(fixture.Player("Never Promote"));

        Assert.False(result.Succeeded);
        Assert.Equal(priorLive, File.ReadAllBytes(fixture.LivePath));
        Assert.Contains("operation=StageWrite", result.Diagnostic);
        Assert.Contains("artifact=Stage", result.Diagnostic);
        Assert.Contains("exception=IOException", result.Diagnostic);
        Assert.Contains("file=savegame.json.stage", result.Diagnostic);
        Assert.DoesNotContain(fixture.DirectoryPath, result.Diagnostic);
        Assert.DoesNotContain("Secret Player", result.Diagnostic);
        Assert.Equal(LoadStatus.Loaded, fixture.FreshSystem().Load().Status);
    }

    public static IEnumerable<object[]> SharedSaveAndRecoveryCases() =>
        IroncladSaveEnvelopeContractFixture.Load().Cases
            .Take(17)
            .Select(testCase => new object[] { testCase.Id });

    public static IEnumerable<object[]> SharedNewGameCases() =>
        IroncladSaveEnvelopeContractFixture.Load().Cases
            .Skip(17)
            .Select(testCase => new object[] { testCase.Id });

    [Theory]
    [MemberData(nameof(SharedSaveAndRecoveryCases))]
    public void SharedCase_ExecutesWithExactArtifactsAndNextLoad(string caseId)
    {
        var testCase = IroncladSaveEnvelopeContractFixture.Load().Cases.Single(item => item.Id == caseId);
        using var fixture = new EnvelopeFixture();
        fixture.Seed(testCase.InitialArtifacts);
        var checkpoint = Enum.Parse<SaveEnvelopeCheckpoint>(testCase.Checkpoint);
        var mutationSide = Enum.Parse<SaveEnvelopeMutationSide>(testCase.MutationSide);
        var operations = new SaveEnvelopeFileOperations((actualCheckpoint, actualSide) =>
        {
            if (actualCheckpoint != checkpoint || actualSide != mutationSide) return;

            if (testCase.Id == "01-save-stage-write-before")
            {
                File.WriteAllBytes(fixture.StagePath, fixture.PayloadBytes("partial-candidate"));
            }
            else if (testCase.Id == "02-save-stage-validation-before")
            {
                File.WriteAllBytes(fixture.StagePath, fixture.PayloadBytes("invalid-candidate"));
                return;
            }

            if (testCase.ExpectedImmediateStatus == "Interrupted")
            {
                throw new SaveEnvelopeInterruptionException(testCase.Checkpoint);
            }

            throw new IOException("secret player and absolute root: " + fixture.DirectoryPath);
        });
        var faultedSystem = new SaveSystem(fixture.LivePath, operations);

        PersistenceResult? saveResult = null;
        LoadResult? loadResult = null;
        Exception? interruption = null;
        try
        {
            if (testCase.Operation == "Save")
            {
                saveResult = faultedSystem.Save(fixture.Player("Current B"));
            }
            else
            {
                loadResult = faultedSystem.Load();
            }
        }
        catch (SaveEnvelopeInterruptionException ex)
        {
            interruption = ex;
        }

        string? diagnostic;
        switch (testCase.ExpectedImmediateStatus)
        {
            case "Failed":
                Assert.NotNull(saveResult);
                Assert.False(saveResult.Succeeded);
                diagnostic = saveResult.Diagnostic;
                break;
            case "Unreadable":
                Assert.NotNull(loadResult);
                Assert.Equal(LoadStatus.Unreadable, loadResult.Status);
                diagnostic = loadResult.Diagnostic;
                break;
            case "Interrupted":
                Assert.IsType<SaveEnvelopeInterruptionException>(interruption);
                diagnostic = null;
                break;
            default:
                throw new InvalidDataException($"Unexpected status {testCase.ExpectedImmediateStatus}.");
        }

        AssertDiagnostic(testCase, diagnostic, fixture.DirectoryPath);
        Assert.Equal(
            testCase.ExpectedImmediateRecoveryNoticePending,
            DetermineImmediateNoticeState(testCase, loadResult, fixture));
        fixture.AssertExactArtifacts(testCase.ExpectedSurvivingArtifacts);

        var nextLoad = fixture.FreshSystem().Load();
        Assert.Equal(Enum.Parse<LoadStatus>(testCase.ExpectedNextLoadStatus), nextLoad.Status);
        Assert.Equal(testCase.ExpectedNextRecoveryNoticePending, nextLoad.RecoveryNoticePending);
        Assert.Equal(
            fixture.PayloadBytes(testCase.ExpectedAuthoritativeLabel),
            File.ReadAllBytes(fixture.LivePath));
        Assert.Equal(
            fixture.PayloadPlayerName(testCase.ExpectedAuthoritativeLabel),
            nextLoad.Player!.Name);
    }

    [Theory]
    [MemberData(nameof(SharedNewGameCases))]
    public void SharedNewGameCase_PreservesDataEdgeAndRetriesIdempotently(string caseId)
    {
        var testCase = IroncladSaveEnvelopeContractFixture.Load().Cases.Single(item => item.Id == caseId);
        using var fixture = new EnvelopeFixture();
        fixture.Seed(testCase.InitialArtifacts);
        var checkpoint = Enum.Parse<SaveEnvelopeCheckpoint>(testCase.Checkpoint);
        var mutationSide = Enum.Parse<SaveEnvelopeMutationSide>(testCase.MutationSide);
        var operations = new SaveEnvelopeFileOperations((actualCheckpoint, actualSide) =>
        {
            if (actualCheckpoint == checkpoint && actualSide == mutationSide)
            {
                throw new IOException("secret player and absolute root: " + fixture.DirectoryPath);
            }
        });
        var faultedSystem = new SaveSystem(fixture.LivePath, operations);

        var result = faultedSystem.Delete();

        Assert.False(result.Succeeded);
        AssertDiagnostic(testCase, result.Diagnostic, fixture.DirectoryPath);
        Assert.Equal(
            testCase.ExpectedImmediateRecoveryNoticePending,
            testCase.InitialArtifacts.Any(artifact => artifact.Role == "RecoveryMarker"));
        fixture.AssertExactArtifacts(testCase.ExpectedSurvivingArtifacts);

        var nextLoad = fixture.FreshSystem().Load();
        Assert.Equal(Enum.Parse<LoadStatus>(testCase.ExpectedNextLoadStatus), nextLoad.Status);
        Assert.Equal(testCase.ExpectedNextRecoveryNoticePending, nextLoad.RecoveryNoticePending);
        if (testCase.ExpectedAuthoritativeLabel == "None")
        {
            Assert.Null(nextLoad.Player);
            Assert.False(File.Exists(fixture.LivePath));
        }
        else
        {
            Assert.Equal(
                fixture.PayloadBytes(testCase.ExpectedAuthoritativeLabel),
                File.ReadAllBytes(fixture.LivePath));
            Assert.Equal(
                fixture.PayloadPlayerName(testCase.ExpectedAuthoritativeLabel),
                nextLoad.Player!.Name);
        }

        var retry = fixture.FreshSystem().Delete();
        Assert.True(retry.Succeeded, retry.Diagnostic);
        fixture.AssertExactArtifacts([]);
        Assert.Equal(LoadStatus.Missing, fixture.FreshSystem().Load().Status);
    }

    [Fact]
    public void Delete_WithEveryArtifact_RemovesCompleteEnvelopeAndRestartsMissing()
    {
        using var fixture = new EnvelopeFixture();
        fixture.Seed(
        [
            new ArtifactPayloadFixture { Role = "Live", PayloadLabel = "current-a" },
            new ArtifactPayloadFixture { Role = "LastKnownGood", PayloadLabel = "prior-lkg" },
            new ArtifactPayloadFixture { Role = "Stage", PayloadLabel = "partial-candidate" },
            new ArtifactPayloadFixture { Role = "Quarantine", PayloadLabel = "prior-quarantine" },
            new ArtifactPayloadFixture { Role = "RecoveryMarker", PayloadLabel = "marker" }
        ]);

        var result = fixture.System.Delete();

        Assert.True(result.Succeeded, result.Diagnostic);
        fixture.AssertExactArtifacts([]);
        Assert.Equal(LoadStatus.Missing, fixture.FreshSystem().Load().Status);
    }

    [Theory]
    [InlineData("Live")]
    [InlineData("LastKnownGood")]
    public void Delete_WhenAuthorityReadFails_LeavesEveryArtifactByteForByteUnchanged(string lockedRole)
    {
        using var fixture = new EnvelopeFixture();
        var initial = new[]
        {
            new ArtifactPayloadFixture { Role = "Live", PayloadLabel = "current-a" },
            new ArtifactPayloadFixture { Role = "LastKnownGood", PayloadLabel = "prior-lkg" },
            new ArtifactPayloadFixture { Role = "Stage", PayloadLabel = "partial-candidate" },
            new ArtifactPayloadFixture { Role = "Quarantine", PayloadLabel = "prior-quarantine" },
            new ArtifactPayloadFixture { Role = "RecoveryMarker", PayloadLabel = "marker" }
        };
        fixture.Seed(initial);
        PersistenceResult result;
        using (var liveLock = new FileStream(
            fixture.ArtifactPath(lockedRole),
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None))
        {
            result = fixture.System.Delete();
        }

        Assert.False(result.Succeeded);
        Assert.Equal(
            $"operation=ClassifyAuthority; artifact={lockedRole}; exception=IOException; " +
            $"file={Path.GetFileName(fixture.ArtifactPath(lockedRole))}",
            result.Diagnostic);
        fixture.AssertExactArtifacts(initial);
    }

    [Fact]
    public void Save_WithInvalidLive_RejectsCandidateWithoutChangingLiveOrLastKnownGood()
    {
        using var fixture = new EnvelopeFixture();
        var damagedLive = fixture.PayloadBytes("corrupt-live");
        var priorLkg = fixture.PayloadBytes("prior-lkg");
        File.WriteAllBytes(fixture.LivePath, damagedLive);
        File.WriteAllBytes(fixture.LastKnownGoodPath, priorLkg);

        var result = fixture.System.Save(fixture.Player("Current B"));

        Assert.False(result.Succeeded);
        Assert.Equal(damagedLive, File.ReadAllBytes(fixture.LivePath));
        Assert.Equal(priorLkg, File.ReadAllBytes(fixture.LastKnownGoodPath));
        Assert.True(File.Exists(fixture.StagePath));
    }

    [Fact]
    public void Save_WithInvalidPriorLastKnownGood_ReplacesItWithExactDisplacedLive()
    {
        using var fixture = new EnvelopeFixture();
        File.WriteAllBytes(fixture.LivePath, fixture.PayloadBytes("current-a"));
        File.WriteAllBytes(fixture.LastKnownGoodPath, fixture.PayloadBytes("corrupt-live"));

        var result = fixture.System.Save(fixture.Player("Current B"));

        Assert.True(result.Succeeded, result.Diagnostic);
        Assert.Equal(fixture.PayloadBytes("current-a"), File.ReadAllBytes(fixture.LastKnownGoodPath));
        Assert.Equal(fixture.PayloadBytes("current-b"), File.ReadAllBytes(fixture.LivePath));
    }

    [Fact]
    public void Load_WithMissingLiveAndInvalidLastKnownGood_IsUnreadableAndPreservesBytes()
    {
        using var fixture = new EnvelopeFixture();
        var invalid = fixture.PayloadBytes("corrupt-live");
        File.WriteAllBytes(fixture.LastKnownGoodPath, invalid);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.False(result.RecoveryNoticePending);
        Assert.Equal(invalid, File.ReadAllBytes(fixture.LastKnownGoodPath));
    }

    [Theory]
    [InlineData("Stage")]
    [InlineData("Quarantine")]
    [InlineData("RecoveryMarker")]
    public void Load_WithOnlyNonAuthorityArtifact_ReturnsMissing(string artifactRole)
    {
        using var fixture = new EnvelopeFixture();
        File.WriteAllBytes(fixture.ArtifactPath(artifactRole), fixture.PayloadBytes(
            artifactRole == "RecoveryMarker" ? "marker" : "current-a"));

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Missing, result.Status);
        Assert.False(result.RecoveryNoticePending);
    }

    [Fact]
    public void Load_WithMarkerDirectory_FailsRecoveryBeforeQuarantineOrPromotion()
    {
        using var fixture = new EnvelopeFixture();
        var damaged = fixture.PayloadBytes("corrupt-live");
        File.WriteAllBytes(fixture.LivePath, damaged);
        File.WriteAllBytes(fixture.LastKnownGoodPath, fixture.PayloadBytes("valid-lkg"));
        Directory.CreateDirectory(fixture.RecoveryMarkerPath);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Contains("operation=CreateRecoveryMarker", result.Diagnostic);
        Assert.Equal(damaged, File.ReadAllBytes(fixture.LivePath));
        Assert.False(File.Exists(fixture.QuarantinePath));
        Assert.False(result.RecoveryNoticePending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Load_WithValidLastKnownGood_RecoversWhenLiveMissingAndMarkerIsOptional(bool markerAlreadyExists)
    {
        using var fixture = new EnvelopeFixture();
        var lastKnownGood = fixture.PayloadBytes("valid-lkg");
        File.WriteAllBytes(fixture.LastKnownGoodPath, lastKnownGood);
        if (markerAlreadyExists)
        {
            File.WriteAllBytes(fixture.RecoveryMarkerPath, []);
        }

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Recovered, result.Status);
        Assert.True(result.RecoveryNoticePending);
        Assert.Equal(lastKnownGood, File.ReadAllBytes(fixture.LivePath));
        Assert.Equal(lastKnownGood, File.ReadAllBytes(fixture.LastKnownGoodPath));
        Assert.False(File.Exists(fixture.QuarantinePath));
        Assert.True(File.Exists(fixture.RecoveryMarkerPath));
    }

    [Fact]
    public void Save_WhenProgressWriteFails_RetainsExistingRecoveryNoticeAndRecoveredLive()
    {
        using var fixture = new EnvelopeFixture();
        var recoveredLive = fixture.PayloadBytes("recovered-lkg");
        File.WriteAllBytes(fixture.LivePath, recoveredLive);
        File.WriteAllBytes(fixture.RecoveryMarkerPath, []);
        var operations = new SaveEnvelopeFileOperations((checkpoint, side) =>
        {
            if (checkpoint == SaveEnvelopeCheckpoint.StageWrite && side == SaveEnvelopeMutationSide.Before)
            {
                throw new IOException("progress write failed");
            }
        });

        var result = new SaveSystem(fixture.LivePath, operations).Save(fixture.Player("Current B"));

        Assert.False(result.Succeeded);
        Assert.Equal(recoveredLive, File.ReadAllBytes(fixture.LivePath));
        Assert.True(File.Exists(fixture.RecoveryMarkerPath));
        var nextLoad = fixture.FreshSystem().Load();
        Assert.Equal(LoadStatus.Loaded, nextLoad.Status);
        Assert.True(nextLoad.RecoveryNoticePending);
    }

    [Fact]
    public void Save_RetryAfterPartialStage_TruncatesAndPromotesCompleteCandidate()
    {
        using var fixture = new EnvelopeFixture();
        File.WriteAllBytes(fixture.StagePath, fixture.PayloadBytes("partial-candidate"));

        var result = fixture.System.Save(fixture.Player("Current B"));

        Assert.True(result.Succeeded, result.Diagnostic);
        Assert.Equal(fixture.PayloadBytes("current-b"), File.ReadAllBytes(fixture.LivePath));
        Assert.False(File.Exists(fixture.StagePath));
    }

    [Fact]
    public void Load_LegacyLastKnownGood_RecoversExactBytesWithoutUpgradingFormat()
    {
        using var fixture = new EnvelopeFixture();
        var legacy = fixture.PayloadBytes("legacy-lkg");
        File.WriteAllBytes(fixture.LastKnownGoodPath, legacy);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Recovered, result.Status);
        Assert.Equal("Legacy LKG", result.Player!.Name);
        Assert.Equal(legacy, File.ReadAllBytes(fixture.LivePath));
        Assert.Equal(legacy, File.ReadAllBytes(fixture.LastKnownGoodPath));
    }

    [Theory]
    [InlineData("PromoteLive")]
    [InlineData("PromoteRecovery")]
    public void MutationThenException_IsSettledByFreshValidatingLoad(string checkpointName)
    {
        var checkpoint = Enum.Parse<SaveEnvelopeCheckpoint>(checkpointName);
        using var fixture = new EnvelopeFixture();
        if (checkpoint == SaveEnvelopeCheckpoint.PromoteLive)
        {
            File.WriteAllBytes(fixture.LivePath, fixture.PayloadBytes("current-a"));
        }
        else
        {
            File.WriteAllBytes(fixture.LivePath, fixture.PayloadBytes("corrupt-live"));
            File.WriteAllBytes(fixture.LastKnownGoodPath, fixture.PayloadBytes("valid-lkg"));
        }

        var operations = new SaveEnvelopeFileOperations((actual, side) =>
        {
            if (actual == checkpoint && side == SaveEnvelopeMutationSide.After)
            {
                throw new IOException("unknown commit state");
            }
        });
        var faulted = new SaveSystem(fixture.LivePath, operations);

        if (checkpoint == SaveEnvelopeCheckpoint.PromoteLive)
        {
            Assert.False(faulted.Save(fixture.Player("Current B")).Succeeded);
        }
        else
        {
            Assert.Equal(LoadStatus.Unreadable, faulted.Load().Status);
        }

        var nextLoad = fixture.FreshSystem().Load();
        Assert.Equal(LoadStatus.Loaded, nextLoad.Status);
        Assert.Equal(
            checkpoint == SaveEnvelopeCheckpoint.PromoteLive ? "Current B" : "Valid LKG",
            nextLoad.Player!.Name);
        Assert.Equal(
            checkpoint == SaveEnvelopeCheckpoint.PromoteRecovery,
            nextLoad.RecoveryNoticePending);
    }

    private static bool DetermineImmediateNoticeState(
        SaveEnvelopeCaseFixture testCase,
        LoadResult? loadResult,
        EnvelopeFixture fixture)
    {
        if (loadResult is not null) return loadResult.RecoveryNoticePending;
        if (testCase.Operation == "Save" && testCase.InitialArtifacts.Any(a => a.Role == "RecoveryMarker"))
        {
            return true;
        }

        return File.Exists(fixture.RecoveryMarkerPath) && fixture.LiveContainsValidPayload();
    }

    private static void AssertDiagnostic(
        SaveEnvelopeCaseFixture testCase,
        string? actual,
        string absoluteRoot)
    {
        if (testCase.ExpectedDiagnostic is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.Equal(
            $"operation={testCase.ExpectedDiagnostic.OperationRole}; " +
            $"artifact={testCase.ExpectedDiagnostic.ArtifactRole}; " +
            $"exception={testCase.ExpectedDiagnostic.ExceptionType}; " +
            $"file={testCase.ExpectedDiagnostic.RelativeFilename}",
            actual);
        Assert.DoesNotContain(absoluteRoot, actual);
        Assert.DoesNotContain("secret player", actual, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class EnvelopeFixture : IDisposable
    {
        public EnvelopeFixture()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"toil-relic-envelope-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            LivePath = Path.Combine(DirectoryPath, "savegame.json");
            System = new SaveSystem(LivePath);
        }

        public string DirectoryPath { get; }
        public string LivePath { get; }
        public string StagePath => LivePath + ".stage";
        public string LastKnownGoodPath => LivePath + ".lkg";
        public string QuarantinePath => LivePath + ".quarantine";
        public string RecoveryMarkerPath => LivePath + ".recovery-pending";
        public SaveSystem System { get; }

        public Player Player(string name) => new(name);
        public SaveSystem FreshSystem() => new(LivePath);

        public string ArtifactPath(string role) => role switch
        {
            "Live" => LivePath,
            "LastKnownGood" => LastKnownGoodPath,
            "Stage" => StagePath,
            "Quarantine" => QuarantinePath,
            "RecoveryMarker" => RecoveryMarkerPath,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        public void Seed(IEnumerable<ArtifactPayloadFixture> artifacts)
        {
            foreach (var artifact in artifacts)
            {
                File.WriteAllBytes(ArtifactPath(artifact.Role), PayloadBytes(artifact.PayloadLabel));
            }
        }

        public byte[] PayloadBytes(string label) => label switch
        {
            "current-a" => Serialize(Player("Current A")),
            "current-b" => Serialize(Player("Current B")),
            "prior-lkg" => Serialize(Player("Prior LKG")),
            "valid-lkg" => Serialize(Player("Valid LKG")),
            "recovered-lkg" => Serialize(Player("Recovered LKG")),
            "corrupt-live" => Encoding.UTF8.GetBytes("{ corrupt-live"),
            "partial-candidate" => Encoding.UTF8.GetBytes("{ partial-candidate"),
            "invalid-candidate" => Encoding.UTF8.GetBytes("{}"),
            "prior-quarantine" => Encoding.UTF8.GetBytes("prior quarantine bytes"),
            "marker" => [],
            "legacy-lkg" => Encoding.UTF8.GetBytes(
                "{\"Name\":\"Legacy LKG\",\"MaxHp\":30,\"Hp\":18,\"Level\":2," +
                "\"Experience\":3,\"TreasureCount\":0,\"Inventory\":{}}"),
            _ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
        };

        public string PayloadPlayerName(string label) => label switch
        {
            "current-a" => "Current A",
            "current-b" => "Current B",
            "prior-lkg" => "Prior LKG",
            "valid-lkg" => "Valid LKG",
            "recovered-lkg" => "Recovered LKG",
            "legacy-lkg" => "Legacy LKG",
            _ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
        };

        public void AssertExactArtifacts(IEnumerable<ArtifactPayloadFixture> expectedArtifacts)
        {
            var expected = expectedArtifacts.ToDictionary(a => a.Role, a => a.PayloadLabel, StringComparer.Ordinal);
            foreach (var role in new[] { "Live", "LastKnownGood", "Stage", "Quarantine", "RecoveryMarker" })
            {
                var path = ArtifactPath(role);
                if (expected.TryGetValue(role, out var label))
                {
                    Assert.True(File.Exists(path), $"Expected {role} to exist.");
                    Assert.Equal(PayloadBytes(label), File.ReadAllBytes(path));
                }
                else
                {
                    Assert.False(File.Exists(path), $"Expected {role} to be absent.");
                }
            }
        }

        public bool LiveContainsValidPayload()
        {
            if (!File.Exists(LivePath)) return false;
            var bytes = File.ReadAllBytes(LivePath);
            return new[] { "current-a", "current-b", "prior-lkg", "valid-lkg", "recovered-lkg", "legacy-lkg" }
                .Any(label => bytes.AsSpan().SequenceEqual(PayloadBytes(label)));
        }

        private static byte[] Serialize(Player player) =>
            JsonSerializer.SerializeToUtf8Bytes(player.ToSaveData(), JsonOptions);

        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }
}
