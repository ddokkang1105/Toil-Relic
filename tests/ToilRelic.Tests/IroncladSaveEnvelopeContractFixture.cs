using System.Text.Json;
using System.Text.Json.Nodes;
using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

internal static class IroncladSaveEnvelopeContractFixture
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IroncladSaveEnvelopeFixture Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "IroncladSaveEnvelopeContracts.json");
        return Parse(File.ReadAllText(path));
    }

    public static IroncladSaveEnvelopeFixture Parse(string json)
    {
        RejectUnexpectedDiagnosticFields(json);
        var fixture = JsonSerializer.Deserialize<IroncladSaveEnvelopeFixture>(json, Options)
            ?? throw new InvalidDataException("Ironclad save-envelope fixture is empty.");
        fixture.Validate();
        return fixture;
    }

    private static void RejectUnexpectedDiagnosticFields(string json)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "operationRole",
            "artifactRole",
            "exceptionType",
            "relativeFilename"
        };
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Ironclad save-envelope fixture is empty.");
        foreach (var testCase in root["cases"]?.AsArray() ?? [])
        {
            if (testCase?["expectedDiagnostic"] is not JsonObject diagnostic)
            {
                continue;
            }

            if (diagnostic.Any(property => !allowed.Contains(property.Key)))
            {
                throw new InvalidDataException("expectedDiagnostic contains a non-allowlisted field.");
            }
        }
    }
}

internal sealed class IroncladSaveEnvelopeFixture
{
    private static readonly HashSet<string> KnownArtifactRoles =
    [
        "Live",
        "LastKnownGood",
        "Stage",
        "Quarantine",
        "RecoveryMarker"
    ];

    private static readonly string[] RequiredDiagnosticFields =
    [
        "OperationRole",
        "ArtifactRole",
        "ExceptionType",
        "RelativeFilename"
    ];

    private static readonly string[] RequiredForbiddenDiagnosticFields =
    [
        "RawBytes",
        "PlayerFields",
        "AbsoluteRoot",
        "ExceptionMessage"
    ];

    private static readonly string[] RequiredCheckpoints =
    [
        "StageWrite",
        "StageValidation",
        "PreserveLastKnownGood",
        "PromoteLive",
        "AfterLivePromotion",
        "PromoteRecovery",
        "AfterRecoveryPromotion",
        "CreateRecoveryMarker",
        "FlushRecoveryMarker",
        "DeleteRecoveryMarker",
        "DeletePriorQuarantine",
        "MoveDamagedLive",
        "ClassifyLiveAuthority",
        "ClassifyLastKnownGoodAuthority",
        "DeleteStage",
        "DeleteQuarantine",
        "DeleteLastKnownGood",
        "DeleteLive"
    ];

    private static readonly string[] RequiredCaseIds =
    [
        "01-save-stage-write-before",
        "02-save-stage-validation-before",
        "03-save-lkg-preservation-before",
        "04-save-live-promotion-before",
        "05-save-live-promotion-after",
        "06-recovery-promotion-before",
        "07-recovery-promotion-after",
        "08-recovery-marker-create-before",
        "09-recovery-marker-create-after",
        "10-recovery-marker-flush-before",
        "11-recovery-marker-flush-after",
        "12-recovery-prior-quarantine-delete-before",
        "13-recovery-prior-quarantine-delete-after",
        "14-recovery-damaged-live-move-before",
        "15-recovery-damaged-live-move-after",
        "16-progress-marker-delete-before",
        "17-progress-marker-delete-after",
        "18-newgame-classify-live-before",
        "19-newgame-classify-lkg-before",
        "20-newgame-delete-stage-before",
        "21-newgame-delete-stage-after",
        "22-newgame-delete-quarantine-before",
        "23-newgame-delete-quarantine-after",
        "24-newgame-delete-nonauthoritative-lkg-before",
        "25-newgame-delete-nonauthoritative-lkg-after",
        "26-newgame-delete-invalid-live-before",
        "27-newgame-delete-invalid-live-after",
        "28-newgame-delete-authoritative-live-before",
        "29-newgame-delete-authoritative-live-after",
        "30-newgame-delete-authoritative-lkg-before",
        "31-newgame-delete-authoritative-lkg-after",
        "32-newgame-delete-marker-before",
        "33-newgame-delete-marker-after"
    ];

    private static readonly string[] RequiredOperations =
    [
        "Save", "Save", "Save", "Save", "Save",
        "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load",
        "Save", "Save",
        "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame",
        "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame",
        "NewGame", "NewGame"
    ];

    public int SchemaVersion { get; set; }
    public string[] ArtifactRoles { get; set; } = [];
    public string[] PayloadLabels { get; set; } = [];
    public DiagnosticContractFixture DiagnosticContract { get; set; } = new();
    public SaveEnvelopeCaseFixture[] Cases { get; set; } = [];

    public IReadOnlyList<string> OrderedCaseIds => Cases.Select(testCase => testCase.Id).ToArray();

    public void Validate()
    {
        Require(SchemaVersion == 1, "schemaVersion must be 1.");
        Require(ArtifactRoles.SequenceEqual(KnownArtifactRoles),
            "artifactRoles must contain the canonical ordered role set.");
        Require(PayloadLabels.Length > 0 && PayloadLabels.All(IsPresent),
            "payloadLabels must contain non-empty symbolic labels.");
        Require(PayloadLabels.Distinct(StringComparer.Ordinal).Count() == PayloadLabels.Length,
            "payloadLabels must be unique.");
        Require(DiagnosticContract.AllowedFields.SequenceEqual(RequiredDiagnosticFields),
            "diagnosticContract.allowedFields must match the allowlist.");
        Require(DiagnosticContract.ForbiddenFields.SequenceEqual(RequiredForbiddenDiagnosticFields),
            "diagnosticContract.forbiddenFields must match the denylist.");
        Require(OrderedCaseIds.SequenceEqual(RequiredCaseIds),
            "cases must match the canonical ordered 33-case manifest.");
        Require(Cases.Select(testCase => testCase.Operation).SequenceEqual(RequiredOperations),
            "case operations must match the canonical Save/Load/NewGame partition.");
        Require(RequiredCheckpoints.All(required => Cases.Any(testCase => testCase.Checkpoint == required)),
            "cases must cover every required save-envelope checkpoint.");

        foreach (var testCase in Cases)
        {
            Require(IsPresent(testCase.Operation), $"{testCase.Id}: operation is required.");
            Require(IsPresent(testCase.Checkpoint), $"{testCase.Id}: checkpoint is required.");
            Require(testCase.MutationSide is "Before" or "After",
                $"{testCase.Id}: mutationSide must be Before or After.");
            Require(IsPresent(testCase.ExpectedImmediateStatus),
                $"{testCase.Id}: expectedImmediateStatus is required.");
            Require(IsPresent(testCase.ExpectedNextLoadStatus),
                $"{testCase.Id}: expectedNextLoadStatus is required.");
            Require(testCase.ExpectedAuthoritativeLabel == "None"
                || PayloadLabels.Contains(testCase.ExpectedAuthoritativeLabel, StringComparer.Ordinal),
                $"{testCase.Id}: expectedAuthoritativeLabel is unknown.");
            Require(testCase.InitialArtifacts.Length > 0,
                $"{testCase.Id}: initialArtifacts must not be empty.");

            ValidateArtifacts(testCase.Id, testCase.InitialArtifacts);
            ValidateArtifacts(testCase.Id, testCase.ExpectedSurvivingArtifacts);

            if (testCase.ExpectedDiagnostic != null)
            {
                Require(IsPresent(testCase.ExpectedDiagnostic.OperationRole),
                    $"{testCase.Id}: diagnostic operationRole is required.");
                Require(KnownArtifactRoles.Contains(testCase.ExpectedDiagnostic.ArtifactRole),
                    $"{testCase.Id}: diagnostic artifactRole is unknown.");
                Require(IsPresent(testCase.ExpectedDiagnostic.ExceptionType),
                    $"{testCase.Id}: diagnostic exceptionType is required.");
                Require(IsPresent(testCase.ExpectedDiagnostic.RelativeFilename)
                    && !Path.IsPathRooted(testCase.ExpectedDiagnostic.RelativeFilename)
                    && !testCase.ExpectedDiagnostic.RelativeFilename.Contains("..", StringComparison.Ordinal),
                    $"{testCase.Id}: diagnostic relativeFilename must be a redacted relative filename.");
            }
        }
    }

    private void ValidateArtifacts(string caseId, IEnumerable<ArtifactPayloadFixture> artifacts)
    {
        foreach (var artifact in artifacts)
        {
            Require(KnownArtifactRoles.Contains(artifact.Role), $"{caseId}: unknown artifact role {artifact.Role}.");
            Require(PayloadLabels.Contains(artifact.PayloadLabel, StringComparer.Ordinal),
                $"{caseId}: unknown payload label {artifact.PayloadLabel}.");
        }
    }

    private static bool IsPresent(string? value) => !string.IsNullOrWhiteSpace(value);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}

internal sealed class DiagnosticContractFixture
{
    public string[] AllowedFields { get; set; } = [];
    public string[] ForbiddenFields { get; set; } = [];
}

internal sealed class SaveEnvelopeCaseFixture
{
    public string Id { get; set; } = string.Empty;
    public ArtifactPayloadFixture[] InitialArtifacts { get; set; } = [];
    public string Operation { get; set; } = string.Empty;
    public string Checkpoint { get; set; } = string.Empty;
    public string MutationSide { get; set; } = string.Empty;
    public string ExpectedImmediateStatus { get; set; } = string.Empty;
    public string ExpectedNextLoadStatus { get; set; } = string.Empty;
    public string ExpectedAuthoritativeLabel { get; set; } = string.Empty;
    public ArtifactPayloadFixture[] ExpectedSurvivingArtifacts { get; set; } = [];
    public bool ExpectedImmediateRecoveryNoticePending { get; set; }
    public bool ExpectedNextRecoveryNoticePending { get; set; }
    public ExpectedDiagnosticFixture? ExpectedDiagnostic { get; set; }
}

internal sealed class ArtifactPayloadFixture
{
    public string Role { get; set; } = string.Empty;
    public string PayloadLabel { get; set; } = string.Empty;
}

internal sealed class ExpectedDiagnosticFixture
{
    public string OperationRole { get; set; } = string.Empty;
    public string ArtifactRole { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string RelativeFilename { get; set; } = string.Empty;
}

[Collection(ConsoleCollection.Name)]
public sealed class IroncladSaveEnvelopeContractTests
{
    [Fact]
    public void SharedFixture_ProvidesValidatedOrderedCases()
    {
        var fixture = IroncladSaveEnvelopeContractFixture.Load();

        Assert.Equal(1, fixture.SchemaVersion);
        Assert.NotEmpty(fixture.OrderedCaseIds);
        Assert.Equal(fixture.OrderedCaseIds.OrderBy(id => id, StringComparer.Ordinal), fixture.OrderedCaseIds);
    }

    [Fact]
    public void SharedFixture_RejectsMissingRequiredFieldsAndUnknownArtifactRoles()
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "IroncladSaveEnvelopeContracts.json")))!.AsObject();
        var cases = root["cases"]!.AsArray();

        var missingField = root.DeepClone().AsObject();
        missingField["cases"]![0]!.AsObject().Remove("checkpoint");
        Assert.Throws<InvalidDataException>(() =>
            IroncladSaveEnvelopeContractFixture.Parse(missingField.ToJsonString()));

        var unknownRole = root.DeepClone().AsObject();
        unknownRole["cases"]![0]!["initialArtifacts"]![0]!["role"] = "CloudBackup";
        Assert.Throws<InvalidDataException>(() =>
            IroncladSaveEnvelopeContractFixture.Parse(unknownRole.ToJsonString()));

        var forbiddenDiagnostic = root.DeepClone().AsObject();
        forbiddenDiagnostic["cases"]![0]!["expectedDiagnostic"]!["rawBytes"] = "secret";
        Assert.Throws<InvalidDataException>(() =>
            IroncladSaveEnvelopeContractFixture.Parse(forbiddenDiagnostic.ToJsonString()));

        var missingCase = root.DeepClone().AsObject();
        missingCase["cases"]!.AsArray().RemoveAt(0);
        Assert.Throws<InvalidDataException>(() =>
            IroncladSaveEnvelopeContractFixture.Parse(missingCase.ToJsonString()));

        var wrongOperation = root.DeepClone().AsObject();
        wrongOperation["cases"]![0]!["operation"] = "Load";
        Assert.Throws<InvalidDataException>(() =>
            IroncladSaveEnvelopeContractFixture.Parse(wrongOperation.ToJsonString()));
    }

    [Fact]
    public void LoadResults_EnforcePlayerAndNoticeInvariants()
    {
        var player = new Player("Contract Hero");

        var loaded = LoadResult.Loaded(player);
        var restartedRecovery = LoadResult.Loaded(player, recoveryNoticePending: true);
        var recovered = LoadResult.Recovered(player);
        var missing = LoadResult.Missing("Load/Live/IOException/savegame.json");
        var unreadable = LoadResult.Unreadable("Load/LastKnownGood/InvalidDataException/savegame.json.lkg");

        Assert.Same(player, loaded.Player);
        Assert.False(loaded.RecoveryNoticePending);
        Assert.Equal(LoadStatus.Loaded, restartedRecovery.Status);
        Assert.True(restartedRecovery.RecoveryNoticePending);
        Assert.Equal(LoadStatus.Recovered, recovered.Status);
        Assert.Same(player, recovered.Player);
        Assert.True(recovered.RecoveryNoticePending);
        Assert.Null(missing.Player);
        Assert.False(missing.RecoveryNoticePending);
        Assert.NotNull(missing.Diagnostic);
        Assert.Null(unreadable.Player);
        Assert.False(unreadable.RecoveryNoticePending);
        Assert.NotNull(unreadable.Diagnostic);
        Assert.Throws<ArgumentNullException>(() => LoadResult.Loaded(null!));
        Assert.Throws<ArgumentNullException>(() => LoadResult.Recovered(null!));
    }
}
