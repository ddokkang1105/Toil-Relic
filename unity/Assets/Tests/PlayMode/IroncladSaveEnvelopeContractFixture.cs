using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    internal static class IroncladSaveEnvelopeContractFixture
    {
        public static IroncladSaveEnvelopeFixture Load()
        {
            var path = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "IroncladSaveEnvelopeContracts.json");
            return Parse(File.ReadAllText(path));
        }

        public static IroncladSaveEnvelopeFixture Parse(string json)
        {
            RejectUnexpectedDiagnosticFields(json);

            var fixture = JsonUtility.FromJson<IroncladSaveEnvelopeFixture>(json);
            if (fixture == null)
            {
                throw new InvalidDataException("Ironclad save-envelope fixture is empty.");
            }

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
            var diagnosticKeyCount = Regex.Matches(json, "\"expectedDiagnostic\"\\s*:").Count;
            var diagnosticObjects = Regex.Matches(
                json,
                "\"expectedDiagnostic\"\\s*:\\s*\\{(?<body>(?:\"(?:\\\\.|[^\"\\\\])*\"|[^{}])*)\\}",
                RegexOptions.Singleline);
            var nullDiagnosticCount = Regex.Matches(
                json,
                "\"expectedDiagnostic\"\\s*:\\s*null").Count;
            if (diagnosticObjects.Count + nullDiagnosticCount != diagnosticKeyCount)
            {
                throw new InvalidDataException("expectedDiagnostic must be a flat JSON object.");
            }

            foreach (Match diagnosticObject in diagnosticObjects)
            {
                var properties = Regex.Matches(
                    diagnosticObject.Groups["body"].Value,
                    "\"(?<name>(?:\\\\.|[^\"\\\\])*)\"\\s*:");
                if (properties.Cast<Match>().Any(property =>
                        !allowed.Contains(property.Groups["name"].Value)))
                {
                    throw new InvalidDataException("expectedDiagnostic contains a non-allowlisted field.");
                }
            }
        }
    }

    [Serializable]
    internal sealed class IroncladSaveEnvelopeFixture
    {
        private static readonly string[] KnownArtifactRoles =
        {
            "Live",
            "LastKnownGood",
            "Stage",
            "Quarantine",
            "RecoveryMarker"
        };

        private static readonly string[] RequiredDiagnosticFields =
        {
            "OperationRole",
            "ArtifactRole",
            "ExceptionType",
            "RelativeFilename"
        };

        private static readonly string[] RequiredForbiddenDiagnosticFields =
        {
            "RawBytes",
            "PlayerFields",
            "AbsoluteRoot",
            "ExceptionMessage"
        };

        private static readonly string[] RequiredCheckpoints =
        {
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
        };

        private static readonly string[] RequiredCaseIds =
        {
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
        };

        private static readonly string[] RequiredOperations =
        {
            "Save", "Save", "Save", "Save", "Save",
            "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load", "Load",
            "Save", "Save",
            "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame",
            "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame", "NewGame",
            "NewGame", "NewGame"
        };

        public int schemaVersion;
        public string[] artifactRoles;
        public string[] payloadLabels;
        public DiagnosticContractFixture diagnosticContract;
        public SaveEnvelopeCaseFixture[] cases;

        public IReadOnlyList<string> OrderedCaseIds =>
            (cases ?? Array.Empty<SaveEnvelopeCaseFixture>()).Select(testCase => testCase.id).ToArray();

        public void Validate()
        {
            Require(schemaVersion == 1, "schemaVersion must be 1.");
            Require((artifactRoles ?? Array.Empty<string>()).SequenceEqual(KnownArtifactRoles),
                "artifactRoles must contain the canonical ordered role set.");
            Require(payloadLabels != null && payloadLabels.Length > 0 && payloadLabels.All(IsPresent),
                "payloadLabels must contain non-empty symbolic labels.");
            Require(payloadLabels.Distinct(StringComparer.Ordinal).Count() == payloadLabels.Length,
                "payloadLabels must be unique.");
            Require(diagnosticContract != null, "diagnosticContract is required.");
            Require((diagnosticContract.allowedFields ?? Array.Empty<string>()).SequenceEqual(RequiredDiagnosticFields),
                "diagnosticContract.allowedFields must match the allowlist.");
            Require((diagnosticContract.forbiddenFields ?? Array.Empty<string>()).SequenceEqual(RequiredForbiddenDiagnosticFields),
                "diagnosticContract.forbiddenFields must match the denylist.");
            Require(OrderedCaseIds.SequenceEqual(RequiredCaseIds),
                "cases must match the canonical ordered 33-case manifest.");
            Require(cases.Select(testCase => testCase.operation).SequenceEqual(RequiredOperations),
                "case operations must match the canonical Save/Load/NewGame partition.");
            Require(RequiredCheckpoints.All(required => cases.Any(testCase => testCase.checkpoint == required)),
                "cases must cover every required save-envelope checkpoint.");

            foreach (var testCase in cases)
            {
                Require(testCase != null, "cases must not contain null entries.");
                Require(IsPresent(testCase.operation), $"{testCase.id}: operation is required.");
                Require(IsPresent(testCase.checkpoint), $"{testCase.id}: checkpoint is required.");
                Require(testCase.mutationSide == "Before" || testCase.mutationSide == "After",
                    $"{testCase.id}: mutationSide must be Before or After.");
                Require(IsPresent(testCase.expectedImmediateStatus),
                    $"{testCase.id}: expectedImmediateStatus is required.");
                Require(IsPresent(testCase.expectedNextLoadStatus),
                    $"{testCase.id}: expectedNextLoadStatus is required.");
                Require(testCase.expectedAuthoritativeLabel == "None"
                    || payloadLabels.Contains(testCase.expectedAuthoritativeLabel, StringComparer.Ordinal),
                    $"{testCase.id}: expectedAuthoritativeLabel is unknown.");
                Require(testCase.initialArtifacts != null && testCase.initialArtifacts.Length > 0,
                    $"{testCase.id}: initialArtifacts must not be empty.");

                ValidateArtifacts(testCase.id, testCase.initialArtifacts);
                ValidateArtifacts(testCase.id, testCase.expectedSurvivingArtifacts);

                if (testCase.expectedDiagnostic != null && testCase.expectedDiagnostic.HasAnyField)
                {
                    Require(IsPresent(testCase.expectedDiagnostic.operationRole),
                        $"{testCase.id}: diagnostic operationRole is required.");
                    Require(KnownArtifactRoles.Contains(testCase.expectedDiagnostic.artifactRole, StringComparer.Ordinal),
                        $"{testCase.id}: diagnostic artifactRole is unknown.");
                    Require(IsPresent(testCase.expectedDiagnostic.exceptionType),
                        $"{testCase.id}: diagnostic exceptionType is required.");
                    Require(IsPresent(testCase.expectedDiagnostic.relativeFilename)
                        && !Path.IsPathRooted(testCase.expectedDiagnostic.relativeFilename)
                        && !testCase.expectedDiagnostic.relativeFilename.Contains(".."),
                        $"{testCase.id}: diagnostic relativeFilename must be a redacted relative filename.");
                }
            }
        }

        private void ValidateArtifacts(string caseId, IEnumerable<ArtifactPayloadFixture> artifacts)
        {
            foreach (var artifact in artifacts ?? Array.Empty<ArtifactPayloadFixture>())
            {
                Require(artifact != null, $"{caseId}: artifacts must not contain null entries.");
                Require(KnownArtifactRoles.Contains(artifact.role, StringComparer.Ordinal),
                    $"{caseId}: unknown artifact role {artifact.role}.");
                Require(payloadLabels.Contains(artifact.payloadLabel, StringComparer.Ordinal),
                    $"{caseId}: unknown payload label {artifact.payloadLabel}.");
            }
        }

        private static bool IsPresent(string value) => !string.IsNullOrWhiteSpace(value);

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException(message);
            }
        }
    }

    [Serializable]
    internal sealed class DiagnosticContractFixture
    {
        public string[] allowedFields;
        public string[] forbiddenFields;
    }

    [Serializable]
    internal sealed class SaveEnvelopeCaseFixture
    {
        public string id;
        public ArtifactPayloadFixture[] initialArtifacts;
        public string operation;
        public string checkpoint;
        public string mutationSide;
        public string expectedImmediateStatus;
        public string expectedNextLoadStatus;
        public string expectedAuthoritativeLabel;
        public ArtifactPayloadFixture[] expectedSurvivingArtifacts;
        public bool expectedImmediateRecoveryNoticePending;
        public bool expectedNextRecoveryNoticePending;
        public ExpectedDiagnosticFixture expectedDiagnostic;
    }

    [Serializable]
    internal sealed class ArtifactPayloadFixture
    {
        public string role;
        public string payloadLabel;
    }

    [Serializable]
    internal sealed class ExpectedDiagnosticFixture
    {
        public string operationRole;
        public string artifactRole;
        public string exceptionType;
        public string relativeFilename;

        public bool HasAnyField =>
            !string.IsNullOrEmpty(operationRole)
            || !string.IsNullOrEmpty(artifactRole)
            || !string.IsNullOrEmpty(exceptionType)
            || !string.IsNullOrEmpty(relativeFilename);
    }

    public sealed class IroncladSaveEnvelopeContractTests
    {
        [Test]
        [Category("SaveEnvelopeContracts")]
        public void SharedFixture_ProvidesValidatedOrderedCases()
        {
            var fixture = IroncladSaveEnvelopeContractFixture.Load();

            Assert.That(fixture.schemaVersion, Is.EqualTo(1));
            Assert.That(fixture.OrderedCaseIds, Is.Not.Empty);
            Assert.That(
                fixture.OrderedCaseIds,
                Is.EqualTo(fixture.OrderedCaseIds.OrderBy(id => id, StringComparer.Ordinal)));
        }

        [Test]
        [Category("SaveEnvelopeContracts")]
        public void SharedFixture_RejectsMissingRequiredFieldsAndUnknownArtifactRoles()
        {
            var path = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "IroncladSaveEnvelopeContracts.json");
            var json = File.ReadAllText(path);
            var missingField = json.Replace(
                "\"checkpoint\": \"StageWrite\"",
                "\"ignoredCheckpoint\": \"StageWrite\"");
            var unknownRole = json.Replace(
                "\"role\": \"Live\"",
                "\"role\": \"CloudBackup\"");

            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(missingField));
            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(unknownRole));
            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(json.Replace(
                    "\"operationRole\": \"StageWrite\"",
                    "\"operationRole\": \"StageWrite\", \"rawBytes\": \"secret\"")));
            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(json.Replace(
                    "\"operationRole\": \"StageWrite\"",
                    "\"operationRole\": \"StageWrite\", \"diagnosticCode\": \"E_WRITE\"")));

            var normalizedJson = json.Replace("\r\n", "\n");
            var firstCaseStart = normalizedJson.IndexOf(
                "    {\n      \"id\": \"01-save-stage-write-before\"",
                StringComparison.Ordinal);
            var secondCaseStart = normalizedJson.IndexOf(
                "    {\n      \"id\": \"02-save-stage-validation-before\"",
                StringComparison.Ordinal);
            Assert.That(firstCaseStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(secondCaseStart, Is.GreaterThan(firstCaseStart));
            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(
                    normalizedJson.Remove(firstCaseStart, secondCaseStart - firstCaseStart)));
            Assert.Throws<InvalidDataException>(() =>
                IroncladSaveEnvelopeContractFixture.Parse(json.Replace(
                    "\"operation\": \"Save\"",
                    "\"operation\": \"Load\"")));
        }

        [Test]
        [Category("SaveEnvelopeContracts")]
        public void SaveLoadResults_EnforcePlayerAndNoticeInvariants()
        {
            var playerType = RequireType("ToilRelic.Unity.Core.PlayerState");
            var resultType = RequireType("ToilRelic.Unity.Save.SaveLoadResult");
            var player = Activator.CreateInstance(playerType);
            playerType.GetMethod("InitDefaults", BindingFlags.Instance | BindingFlags.Public)!.Invoke(player, null);

            var loaded = InvokeFactory(resultType, "Loaded", player, false);
            var restartedRecovery = InvokeFactory(resultType, "Loaded", player, true);
            var recovered = InvokeFactory(resultType, "Recovered", player, null);
            var missing = InvokeFactory(resultType, "Missing", "Load/Live/IOException/savegame.json");
            var unreadable = InvokeFactory(
                resultType,
                "Unreadable",
                "Load/LastKnownGood/InvalidDataException/savegame.json.lkg");

            Assert.That(Read(resultType, loaded, "Player"), Is.SameAs(player));
            Assert.That(Read(resultType, loaded, "RecoveryNoticePending"), Is.False);
            Assert.That(Read(resultType, restartedRecovery, "Status").ToString(), Is.EqualTo("Loaded"));
            Assert.That(Read(resultType, restartedRecovery, "RecoveryNoticePending"), Is.True);
            Assert.That(Read(resultType, recovered, "Status").ToString(), Is.EqualTo("Recovered"));
            Assert.That(Read(resultType, recovered, "Player"), Is.SameAs(player));
            Assert.That(Read(resultType, recovered, "RecoveryNoticePending"), Is.True);
            Assert.That(Read(resultType, missing, "Player"), Is.Null);
            Assert.That(Read(resultType, missing, "RecoveryNoticePending"), Is.False);
            Assert.That(Read(resultType, missing, "Diagnostic"), Is.Not.Null);
            Assert.That(Read(resultType, unreadable, "Player"), Is.Null);
            Assert.That(Read(resultType, unreadable, "RecoveryNoticePending"), Is.False);
            Assert.That(Read(resultType, unreadable, "Diagnostic"), Is.Not.Null);

            var loadedNull = Assert.Throws<TargetInvocationException>(() =>
                InvokeFactory(resultType, "Loaded", null, false));
            var recoveredNull = Assert.Throws<TargetInvocationException>(() =>
                InvokeFactory(resultType, "Recovered", null, null));
            Assert.That(loadedNull.InnerException, Is.TypeOf<ArgumentNullException>());
            Assert.That(recoveredNull.InnerException, Is.TypeOf<ArgumentNullException>());
        }

        private static Type RequireType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null)
            ?? throw new AssertionException($"Required runtime type was not found: {fullName}");

        private static object InvokeFactory(Type resultType, string name, params object[] arguments)
        {
            var method = resultType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(null, arguments);
        }

        private static object Read(Type resultType, object result, string propertyName) =>
            resultType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(result);
    }
}
