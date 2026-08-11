using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    public sealed class IroncladSaveEnvelopePlayModeTests
    {
        private const string CategoryName = "SaveEnvelopeContracts";
        private static readonly string[] ArtifactRoles =
        {
            "Live",
            "LastKnownGood",
            "Stage",
            "Quarantine",
            "RecoveryMarker"
        };

        private static object deliberatelyLeakedSentinel;
        private Type saveServiceType;
        private Type operationsType;
        private FieldInfo savePathOverrideField;
        private FieldInfo operationsField;
        private object previousSavePathOverride;
        private object previousOperations;
        private string directoryPath;
        private string livePath;

        [SetUp]
        public void SetUp()
        {
            try
            {
                saveServiceType = RequireType("ToilRelic.Unity.Save.SaveService");
                operationsType = RequireType("ToilRelic.Unity.Save.SaveEnvelopeFileOperations");
                savePathOverrideField = RequirePrivateStaticField(saveServiceType, "savePathOverride");
                operationsField = RequirePrivateStaticField(saveServiceType, "fileOperations");
                previousSavePathOverride = savePathOverrideField.GetValue(null);
                previousOperations = operationsField.GetValue(null);
                directoryPath = Path.Combine(
                    Path.GetTempPath(),
                    $"toil-relic-unity-envelope-{Guid.NewGuid():N}");
                Directory.CreateDirectory(directoryPath);
                livePath = Path.Combine(directoryPath, "savegame.json");
                savePathOverrideField.SetValue(null, livePath);
                operationsField.SetValue(null, NewOperations(null));
            }
            catch
            {
                RestoreStaticState();
                DeleteFixtureDirectory();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                RestoreStaticState();
            }
            finally
            {
                DeleteFixtureDirectory();
            }
        }

        [Test]
        [Category(CategoryName)]
        public void SaveService_ExposesOnlyThePrivateResettableEnvelopeOperationsSeam()
        {
            Assert.That(operationsField, Is.Not.Null);
            Assert.That(operationsField.FieldType, Is.EqualTo(operationsType));
            Assert.That(operationsField.IsPrivate, Is.True);
            Assert.That(operationsField.IsStatic, Is.True);
        }

        public static IEnumerable SharedSaveAndRecoveryCases()
        {
            return IroncladSaveEnvelopeContractFixture.Load().cases
                .Where(testCase => testCase.operation == "Save" || testCase.operation == "Load")
                .Select(testCase => new TestCaseData(testCase.id).SetName($"SharedCase_{testCase.id}"));
        }

        public static IEnumerable SharedNewGameCases()
        {
            return IroncladSaveEnvelopeContractFixture.Load().cases
                .Where(testCase => testCase.operation == "NewGame")
                .Select(testCase => new TestCaseData(testCase.id).SetName($"SharedNewGameCase_{testCase.id}"));
        }

        [TestCaseSource(nameof(SharedSaveAndRecoveryCases))]
        [Category(CategoryName)]
        public void SharedCase_ExecutesWithExactArtifactsAndNextLoad(string caseId)
        {
            var testCase = IroncladSaveEnvelopeContractFixture.Load().cases
                .Single(item => item.id == caseId);
            Seed(testCase.initialArtifacts);
            var operations = NewOperations((checkpoint, mutationSide) =>
            {
                if (checkpoint != testCase.checkpoint || mutationSide != testCase.mutationSide) return;

                if (testCase.id == "01-save-stage-write-before")
                {
                    File.WriteAllBytes(StagePath, PayloadBytes("partial-candidate"));
                }
                else if (testCase.id == "02-save-stage-validation-before")
                {
                    File.WriteAllBytes(StagePath, PayloadBytes("invalid-candidate"));
                    return;
                }

                if (testCase.expectedImmediateStatus == "Interrupted")
                {
                    throw CreateInterruption(testCase.checkpoint);
                }

                throw new IOException("secret player and absolute root: " + directoryPath);
            });
            operationsField.SetValue(null, operations);

            object saveResult = null;
            object loadResult = null;
            Exception interruption = null;
            try
            {
                if (testCase.operation == "Save")
                {
                    saveResult = InvokeStatic("Save", Player("current-b"));
                }
                else
                {
                    loadResult = InvokeStatic("Load");
                }
            }
            catch (TargetInvocationException exception)
            {
                interruption = exception.InnerException;
            }

            string diagnostic;
            switch (testCase.expectedImmediateStatus)
            {
                case "Failed":
                    Assert.That(saveResult, Is.Not.Null, testCase.id);
                    Assert.That(Read(saveResult, "Succeeded"), Is.False, testCase.id);
                    diagnostic = Read(saveResult, "Diagnostic") as string;
                    break;
                case "Unreadable":
                    Assert.That(loadResult, Is.Not.Null, testCase.id);
                    Assert.That(Read(loadResult, "Status").ToString(), Is.EqualTo("Unreadable"), testCase.id);
                    diagnostic = Read(loadResult, "Diagnostic") as string;
                    break;
                case "Interrupted":
                    Assert.That(interruption, Is.Not.Null, testCase.id);
                    Assert.That(interruption.GetType().Name, Is.EqualTo("SaveEnvelopeInterruptionException"), testCase.id);
                    diagnostic = null;
                    break;
                default:
                    throw new InvalidDataException(
                        $"Unexpected immediate status {testCase.expectedImmediateStatus}.");
            }

            AssertDiagnostic(testCase, diagnostic);
            Assert.That(
                DetermineImmediateNoticeState(testCase, loadResult),
                Is.EqualTo(testCase.expectedImmediateRecoveryNoticePending),
                testCase.id);
            AssertExactArtifacts(testCase.expectedSurvivingArtifacts);

            operationsField.SetValue(null, NewOperations(null));
            var nextLoad = InvokeStatic("Load");
            Assert.That(
                Read(nextLoad, "Status").ToString(),
                Is.EqualTo(testCase.expectedNextLoadStatus),
                testCase.id);
            Assert.That(
                Read(nextLoad, "RecoveryNoticePending"),
                Is.EqualTo(testCase.expectedNextRecoveryNoticePending),
                testCase.id);
            Assert.That(File.ReadAllBytes(livePath),
                Is.EqualTo(PayloadBytes(testCase.expectedAuthoritativeLabel)),
                testCase.id);
            Assert.That(PlayerTreasure(Read(nextLoad, "Player")),
                Is.EqualTo(PayloadTreasure(testCase.expectedAuthoritativeLabel)),
                testCase.id);
        }

        [TestCaseSource(nameof(SharedNewGameCases))]
        [Category(CategoryName)]
        public void SharedNewGameCase_PreservesDataEdgeAndRetriesIdempotently(string caseId)
        {
            var testCase = IroncladSaveEnvelopeContractFixture.Load().cases
                .Single(item => item.id == caseId);
            Seed(testCase.initialArtifacts);
            operationsField.SetValue(null, NewOperations((checkpoint, mutationSide) =>
            {
                if (checkpoint == testCase.checkpoint && mutationSide == testCase.mutationSide)
                {
                    throw new IOException("secret player and absolute root: " + directoryPath);
                }
            }));

            var result = InvokeStatic("Delete");

            Assert.That(Read(result, "Succeeded"), Is.False, testCase.id);
            AssertDiagnostic(testCase, Read(result, "Diagnostic") as string);
            Assert.That(
                testCase.initialArtifacts.Any(artifact => artifact.role == "RecoveryMarker"),
                Is.EqualTo(testCase.expectedImmediateRecoveryNoticePending),
                testCase.id);
            AssertExactArtifacts(testCase.expectedSurvivingArtifacts);

            operationsField.SetValue(null, NewOperations(null));
            var nextLoad = InvokeStatic("Load");
            Assert.That(Read(nextLoad, "Status").ToString(),
                Is.EqualTo(testCase.expectedNextLoadStatus), testCase.id);
            Assert.That(Read(nextLoad, "RecoveryNoticePending"),
                Is.EqualTo(testCase.expectedNextRecoveryNoticePending), testCase.id);
            if (testCase.expectedAuthoritativeLabel == "None")
            {
                Assert.That(Read(nextLoad, "Player"), Is.Null, testCase.id);
                Assert.That(File.Exists(livePath), Is.False, testCase.id);
            }
            else
            {
                Assert.That(File.ReadAllBytes(livePath),
                    Is.EqualTo(PayloadBytes(testCase.expectedAuthoritativeLabel)), testCase.id);
                Assert.That(PlayerTreasure(Read(nextLoad, "Player")),
                    Is.EqualTo(PayloadTreasure(testCase.expectedAuthoritativeLabel)), testCase.id);
            }

            var retry = InvokeStatic("Delete");
            Assert.That(Read(retry, "Succeeded"), Is.True, Read(retry, "Diagnostic") as string);
            AssertExactArtifacts(Array.Empty<ArtifactPayloadFixture>());
            Assert.That(Read(InvokeStatic("Load"), "Status").ToString(), Is.EqualTo("Missing"));
        }

        [Test]
        [Category(CategoryName)]
        public void Delete_WithEveryArtifact_RemovesCompleteEnvelopeAndRestartsMissing()
        {
            Seed(new[]
            {
                new ArtifactPayloadFixture { role = "Live", payloadLabel = "current-a" },
                new ArtifactPayloadFixture { role = "LastKnownGood", payloadLabel = "prior-lkg" },
                new ArtifactPayloadFixture { role = "Stage", payloadLabel = "partial-candidate" },
                new ArtifactPayloadFixture { role = "Quarantine", payloadLabel = "prior-quarantine" },
                new ArtifactPayloadFixture { role = "RecoveryMarker", payloadLabel = "marker" }
            });

            var result = InvokeStatic("Delete");

            Assert.That(Read(result, "Succeeded"), Is.True, Read(result, "Diagnostic") as string);
            AssertExactArtifacts(Array.Empty<ArtifactPayloadFixture>());
            Assert.That(Read(InvokeStatic("Load"), "Status").ToString(), Is.EqualTo("Missing"));
        }

        [TestCase("Live")]
        [TestCase("LastKnownGood")]
        [Category(CategoryName)]
        public void Delete_WhenAuthorityReadFails_LeavesEveryArtifactByteForByteUnchanged(string lockedRole)
        {
            var initial = new[]
            {
                new ArtifactPayloadFixture { role = "Live", payloadLabel = "current-a" },
                new ArtifactPayloadFixture { role = "LastKnownGood", payloadLabel = "prior-lkg" },
                new ArtifactPayloadFixture { role = "Stage", payloadLabel = "partial-candidate" },
                new ArtifactPayloadFixture { role = "Quarantine", payloadLabel = "prior-quarantine" },
                new ArtifactPayloadFixture { role = "RecoveryMarker", payloadLabel = "marker" }
            };
            Seed(initial);
            object result;
            using (var liveLock = new FileStream(
                ArtifactPath(lockedRole),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                result = InvokeStatic("Delete");
            }

            Assert.That(Read(result, "Succeeded"), Is.False);
            Assert.That(Read(result, "Diagnostic"), Is.EqualTo(
                $"operation=ClassifyAuthority; artifact={lockedRole}; exception=IOException; " +
                $"file={Path.GetFileName(ArtifactPath(lockedRole))}"));
            AssertExactArtifacts(initial);
        }

        [Test]
        [Category(CategoryName)]
        public void Save_WithValidLive_RotatesExactLiveBytesAndPromotesCandidate()
        {
            File.WriteAllBytes(livePath, PayloadBytes("current-a"));
            var oldLive = File.ReadAllBytes(livePath);

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.True, Read(result, "Diagnostic") as string);
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(oldLive));
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(PayloadBytes("current-b")));
            Assert.That(File.Exists(StagePath), Is.False);
        }

        [Test]
        [Category(CategoryName)]
        public void Save_WithInvalidLive_RejectsWithoutChangingAuthority()
        {
            var damagedLive = PayloadBytes("corrupt-live");
            var priorLastKnownGood = PayloadBytes("prior-lkg");
            File.WriteAllBytes(livePath, damagedLive);
            File.WriteAllBytes(LastKnownGoodPath, priorLastKnownGood);

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.False);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(damagedLive));
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(priorLastKnownGood));
            Assert.That(File.Exists(StagePath), Is.True);
        }

        [Test]
        [Category(CategoryName)]
        public void Save_ReplacesInvalidPriorLastKnownGoodWithExactDisplacedLive()
        {
            File.WriteAllBytes(livePath, PayloadBytes("current-a"));
            File.WriteAllBytes(LastKnownGoodPath, PayloadBytes("corrupt-live"));

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.True, Read(result, "Diagnostic") as string);
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(PayloadBytes("current-a")));
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(PayloadBytes("current-b")));
        }

        [Test]
        [Category(CategoryName)]
        public void Save_CreatesMissingParentDirectoryBeforeStaging()
        {
            livePath = Path.Combine(directoryPath, "new", "nested", "savegame.json");
            savePathOverrideField.SetValue(null, livePath);

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.True, Read(result, "Diagnostic") as string);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(PayloadBytes("current-b")));
        }

        [Test]
        [Category(CategoryName)]
        public void Load_WithMissingLiveAndInvalidLastKnownGood_IsUnreadableAndPreservesBytes()
        {
            var invalid = PayloadBytes("corrupt-live");
            File.WriteAllBytes(LastKnownGoodPath, invalid);

            var result = InvokeStatic("Load");

            Assert.That(Read(result, "Status").ToString(), Is.EqualTo("Unreadable"));
            Assert.That(Read(result, "RecoveryNoticePending"), Is.False);
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(invalid));
        }

        [TestCase("Stage")]
        [TestCase("Quarantine")]
        [TestCase("RecoveryMarker")]
        [Category(CategoryName)]
        public void Load_WithOnlyNonAuthorityArtifact_ReturnsMissing(string artifactRole)
        {
            File.WriteAllBytes(
                ArtifactPath(artifactRole),
                PayloadBytes(artifactRole == "RecoveryMarker" ? "marker" : "current-a"));

            var result = InvokeStatic("Load");

            Assert.That(Read(result, "Status").ToString(), Is.EqualTo("Missing"));
            Assert.That(Read(result, "RecoveryNoticePending"), Is.False);
        }

        [Test]
        [Category(CategoryName)]
        public void Load_WithMarkerDirectory_FailsBeforeQuarantineOrPromotion()
        {
            var damaged = PayloadBytes("corrupt-live");
            File.WriteAllBytes(livePath, damaged);
            File.WriteAllBytes(LastKnownGoodPath, PayloadBytes("valid-lkg"));
            Directory.CreateDirectory(RecoveryMarkerPath);

            var result = InvokeStatic("Load");

            Assert.That(Read(result, "Status").ToString(), Is.EqualTo("Unreadable"));
            Assert.That(Read(result, "Diagnostic") as string, Does.StartWith(
                "operation=CreateRecoveryMarker; artifact=RecoveryMarker; exception=IOException;"));
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(damaged));
            Assert.That(File.Exists(QuarantinePath), Is.False);
            Assert.That(Read(result, "RecoveryNoticePending"), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category(CategoryName)]
        public void Load_WithValidLastKnownGood_RecoversWhenMarkerIsOptional(bool markerAlreadyExists)
        {
            var lastKnownGood = PayloadBytes("valid-lkg");
            File.WriteAllBytes(LastKnownGoodPath, lastKnownGood);
            if (markerAlreadyExists) File.WriteAllBytes(RecoveryMarkerPath, Array.Empty<byte>());

            var result = InvokeStatic("Load");

            Assert.That(Read(result, "Status").ToString(), Is.EqualTo("Recovered"));
            Assert.That(Read(result, "RecoveryNoticePending"), Is.True);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(lastKnownGood));
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(lastKnownGood));
            Assert.That(File.Exists(QuarantinePath), Is.False);
            Assert.That(new FileInfo(RecoveryMarkerPath).Length, Is.Zero);
        }

        [Test]
        [Category(CategoryName)]
        public void Save_WhenProgressWriteFails_RetainsRecoveryNoticeAndRecoveredLive()
        {
            var recoveredLive = PayloadBytes("recovered-lkg");
            File.WriteAllBytes(livePath, recoveredLive);
            File.WriteAllBytes(RecoveryMarkerPath, Array.Empty<byte>());
            operationsField.SetValue(null, NewOperations((checkpoint, side) =>
            {
                if (checkpoint == "StageWrite" && side == "Before")
                {
                    throw new IOException("progress write failed");
                }
            }));

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.False);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(recoveredLive));
            Assert.That(File.Exists(RecoveryMarkerPath), Is.True);
            operationsField.SetValue(null, NewOperations(null));
            var nextLoad = InvokeStatic("Load");
            Assert.That(Read(nextLoad, "Status").ToString(), Is.EqualTo("Loaded"));
            Assert.That(Read(nextLoad, "RecoveryNoticePending"), Is.True);
        }

        [Test]
        [Category(CategoryName)]
        public void Save_RetryAfterPartialStage_TruncatesAndPromotesCompleteCandidate()
        {
            File.WriteAllBytes(StagePath, PayloadBytes("partial-candidate"));

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.True, Read(result, "Diagnostic") as string);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(PayloadBytes("current-b")));
            Assert.That(File.Exists(StagePath), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [Category(CategoryName)]
        public void Load_AllSupportedVersionsRemainRecoverableWithoutMigration(int version)
        {
            var candidate = VersionedCandidateBytes(version);
            File.WriteAllBytes(LastKnownGoodPath, candidate);

            var result = InvokeStatic("Load");

            Assert.That(Read(result, "Status").ToString(), Is.EqualTo("Recovered"));
            Assert.That(Read(result, "Player"), Is.Not.Null);
            Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(candidate));
            Assert.That(File.ReadAllBytes(LastKnownGoodPath), Is.EqualTo(candidate));
        }

        [TestCase("PromoteLive")]
        [TestCase("PromoteRecovery")]
        [Category(CategoryName)]
        public void MutationThenException_IsSettledByFreshValidatingLoad(string checkpointName)
        {
            if (checkpointName == "PromoteLive")
            {
                File.WriteAllBytes(livePath, PayloadBytes("current-a"));
            }
            else
            {
                File.WriteAllBytes(livePath, PayloadBytes("corrupt-live"));
                File.WriteAllBytes(LastKnownGoodPath, PayloadBytes("valid-lkg"));
            }

            operationsField.SetValue(null, NewOperations((checkpoint, side) =>
            {
                if (checkpoint == checkpointName && side == "After")
                {
                    throw new IOException("unknown commit state");
                }
            }));

            if (checkpointName == "PromoteLive")
            {
                Assert.That(Read(InvokeStatic("Save", Player("current-b")), "Succeeded"), Is.False);
            }
            else
            {
                Assert.That(Read(InvokeStatic("Load"), "Status").ToString(), Is.EqualTo("Unreadable"));
            }

            operationsField.SetValue(null, NewOperations(null));
            var nextLoad = InvokeStatic("Load");
            Assert.That(Read(nextLoad, "Status").ToString(), Is.EqualTo("Loaded"));
            Assert.That(
                PlayerTreasure(Read(nextLoad, "Player")),
                Is.EqualTo(PayloadTreasure(
                    checkpointName == "PromoteLive" ? "current-b" : "valid-lkg")));
            Assert.That(
                Read(nextLoad, "RecoveryNoticePending"),
                Is.EqualTo(checkpointName == "PromoteRecovery"));
        }

        [TestCase("Before")]
        [TestCase("After")]
        [Category(CategoryName)]
        public void Save_MissingLivePromotionFailure_IsSettledByFreshValidatingLoad(string mutationSide)
        {
            operationsField.SetValue(null, NewOperations((checkpoint, side) =>
            {
                if (checkpoint == "PromoteLive" && side == mutationSide)
                {
                    throw new IOException("missing-live promotion fault");
                }
            }));

            var result = InvokeStatic("Save", Player("current-b"));

            Assert.That(Read(result, "Succeeded"), Is.False);
            Assert.That(File.Exists(LastKnownGoodPath), Is.False);
            operationsField.SetValue(null, NewOperations(null));
            if (mutationSide == "Before")
            {
                Assert.That(File.Exists(livePath), Is.False);
                Assert.That(File.Exists(StagePath), Is.True);
                Assert.That(Read(InvokeStatic("Load"), "Status").ToString(), Is.EqualTo("Missing"));
            }
            else
            {
                Assert.That(File.Exists(StagePath), Is.False);
                var nextLoad = InvokeStatic("Load");
                Assert.That(Read(nextLoad, "Status").ToString(), Is.EqualTo("Loaded"));
                Assert.That(PlayerTreasure(Read(nextLoad, "Player")), Is.EqualTo(PayloadTreasure("current-b")));
            }
        }

        [Test]
        [Category(CategoryName)]
        public void Delete_WhenArtifactMetadataProbeFails_DoesNotClaimSuccess()
        {
            File.WriteAllBytes(StagePath, PayloadBytes("partial-candidate"));
            operationsField.SetValue(null, NewOperations((checkpoint, side) =>
            {
                if (checkpoint == "InspectArtifactForDeletion" && side == "Before")
                {
                    throw new IOException("metadata probe fault");
                }
            }));

            var result = InvokeStatic("Delete");

            Assert.That(Read(result, "Succeeded"), Is.False);
            Assert.That(Read(result, "Diagnostic") as string, Does.Contain("operation=DeleteStage"));
            Assert.That(File.Exists(StagePath), Is.True);
        }

        [Test]
        [Order(-100)]
        [Category(CategoryName)]
        public void StaticSeam_TearDownRestoresAnIntentionallyInstalledSentinel()
        {
            deliberatelyLeakedSentinel = NewOperations((_, __) =>
                throw new AssertionException("Sentinel operations leaked."));
            operationsField.SetValue(null, deliberatelyLeakedSentinel);
            Assert.That(operationsField.GetValue(null), Is.SameAs(deliberatelyLeakedSentinel));
        }

        [Test]
        [Order(-99)]
        [Category(CategoryName)]
        public void StaticSeam_NextSetupStartsWithFreshProductionOperations()
        {
            Assert.That(deliberatelyLeakedSentinel, Is.Not.Null);
            Assert.That(operationsField.GetValue(null), Is.Not.SameAs(deliberatelyLeakedSentinel));
            Assert.That(Read(InvokeStatic("Save", Player("current-a")), "Succeeded"), Is.True);
        }

        [Test]
        [Category(CategoryName)]
        public void StaticSeam_SetupFailurePathRestoresBothStaticFields()
        {
            var pathBeforeScope = savePathOverrideField.GetValue(null);
            var operationsBeforeScope = operationsField.GetValue(null);
            try
            {
                savePathOverrideField.SetValue(null, Path.Combine(directoryPath, "setup-failure.json"));
                operationsField.SetValue(null, NewOperations((_, __) =>
                    throw new AssertionException("Setup sentinel must be restored.")));
                throw new InvalidOperationException("simulated fixture setup failure");
            }
            catch (InvalidOperationException)
            {
                // The fixture setup contract restores state before propagating its own failure.
            }
            finally
            {
                savePathOverrideField.SetValue(null, pathBeforeScope);
                operationsField.SetValue(null, operationsBeforeScope);
            }

            Assert.That(savePathOverrideField.GetValue(null), Is.EqualTo(pathBeforeScope));
            Assert.That(operationsField.GetValue(null), Is.SameAs(operationsBeforeScope));
            Assert.That(Read(InvokeStatic("Save", Player("current-a")), "Succeeded"), Is.True);
        }

        [Test]
        [Category(CategoryName)]
        public void Diagnostic_ContainsOnlyAllowlistedRedactedFields()
        {
            File.WriteAllBytes(livePath, PayloadBytes("current-a"));
            operationsField.SetValue(null, NewOperations((checkpoint, side) =>
            {
                if (checkpoint == "StageWrite" && side == "Before")
                {
                    throw new IOException(
                        "secret player current-b raw payload at " + directoryPath);
                }
            }));

            var result = InvokeStatic("Save", Player("current-b"));
            var diagnostic = Read(result, "Diagnostic") as string;

            Assert.That(diagnostic, Is.EqualTo(
                "operation=StageWrite; artifact=Stage; exception=IOException; file=savegame.json.stage"));
            Assert.That(diagnostic, Does.Not.Contain(directoryPath));
            Assert.That(diagnostic, Does.Not.Contain("secret player"));
            Assert.That(diagnostic, Does.Not.Contain("current-b"));
            Assert.That(diagnostic, Does.Not.Contain("raw payload"));
        }

        private object NewOperations(Action<string, string> checkpoint)
        {
            var constructor = operationsType.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.GetParameters().Length == 1);
            return constructor.Invoke(new object[] { checkpoint });
        }

        private Exception CreateInterruption(string checkpoint)
        {
            var type = RequireType("ToilRelic.Unity.Save.SaveEnvelopeInterruptionException");
            return (Exception)Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { checkpoint },
                null);
        }

        private object InvokeStatic(string methodName, params object[] arguments)
        {
            var method = saveServiceType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(null, arguments);
        }

        private object Player(string label)
        {
            var playerType = RequireType("ToilRelic.Unity.Core.PlayerState");
            var player = Activator.CreateInstance(playerType);
            playerType.GetMethod("InitDefaults", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(player, null);
            var amount = PayloadTreasure(label);
            if (amount > 0)
            {
                var itemType = RequireType("ToilRelic.Unity.Core.ItemType");
                playerType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(player, new[] { Enum.Parse(itemType, "Treasure"), (object)amount });
            }

            return player;
        }

        private byte[] PayloadBytes(string label)
        {
            switch (label)
            {
                case "current-a":
                case "current-b":
                case "prior-lkg":
                case "valid-lkg":
                case "recovered-lkg":
                    var playerJson = JsonUtility.ToJson(Player(label), prettyPrint: false);
                    return Encoding.UTF8.GetBytes(
                        $"{{\"version\":3,\"player\":{playerJson}}}");
                case "corrupt-live":
                    return Encoding.UTF8.GetBytes("{ corrupt-live");
                case "partial-candidate":
                    return Encoding.UTF8.GetBytes("{ partial-candidate");
                case "invalid-candidate":
                    return Encoding.UTF8.GetBytes("{}");
                case "prior-quarantine":
                    return Encoding.UTF8.GetBytes("prior quarantine bytes");
                case "marker":
                    return Array.Empty<byte>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(label), label, null);
            }
        }

        private static int PayloadTreasure(string label)
        {
            switch (label)
            {
                case "current-a": return 1;
                case "current-b": return 2;
                case "prior-lkg": return 3;
                case "valid-lkg": return 4;
                case "recovered-lkg": return 5;
                default: throw new ArgumentOutOfRangeException(nameof(label), label, null);
            }
        }

        private static int PlayerTreasure(object player) =>
            (int)player.GetType().GetProperty("TreasureCount", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(player);

        private byte[] VersionedCandidateBytes(int version)
        {
            if (version == 3) return PayloadBytes("current-a");

            var inventory =
                "[{\"type\":0,\"amount\":0},{\"type\":1,\"amount\":0}," +
                "{\"type\":2,\"amount\":0},{\"type\":3,\"amount\":0}]";
            var player =
                $"{{\"maxHp\":30,\"hp\":30,\"level\":1,\"experience\":0," +
                $"\"treasureCount\":0,\"inventory\":{inventory}}}";
            var json = version == 0
                ? $"{{\"player\":{player}}}"
                : $"{{\"version\":{version},\"player\":{player}}}";
            return Encoding.UTF8.GetBytes(json);
        }

        private string ArtifactPath(string role)
        {
            switch (role)
            {
                case "Live": return livePath;
                case "LastKnownGood": return LastKnownGoodPath;
                case "Stage": return StagePath;
                case "Quarantine": return QuarantinePath;
                case "RecoveryMarker": return RecoveryMarkerPath;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        private string StagePath => livePath + ".stage";
        private string LastKnownGoodPath => livePath + ".lkg";
        private string QuarantinePath => livePath + ".quarantine";
        private string RecoveryMarkerPath => livePath + ".recovery-pending";

        private void Seed(IEnumerable<ArtifactPayloadFixture> artifacts)
        {
            foreach (var artifact in artifacts)
            {
                File.WriteAllBytes(ArtifactPath(artifact.role), PayloadBytes(artifact.payloadLabel));
            }
        }

        private void AssertExactArtifacts(IEnumerable<ArtifactPayloadFixture> expectedArtifacts)
        {
            var expected = expectedArtifacts.ToDictionary(
                artifact => artifact.role,
                artifact => artifact.payloadLabel,
                StringComparer.Ordinal);
            foreach (var role in ArtifactRoles)
            {
                var path = ArtifactPath(role);
                if (expected.TryGetValue(role, out var label))
                {
                    Assert.That(File.Exists(path), Is.True, $"Expected {role} to exist.");
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(PayloadBytes(label)), role);
                }
                else
                {
                    Assert.That(File.Exists(path), Is.False, $"Expected {role} to be absent.");
                }
            }
        }

        private bool DetermineImmediateNoticeState(SaveEnvelopeCaseFixture testCase, object loadResult)
        {
            if (loadResult != null) return (bool)Read(loadResult, "RecoveryNoticePending");
            if (testCase.operation == "Save" &&
                testCase.initialArtifacts.Any(artifact => artifact.role == "RecoveryMarker"))
            {
                return true;
            }

            return File.Exists(RecoveryMarkerPath) && LiveContainsValidPayload();
        }

        private bool LiveContainsValidPayload()
        {
            if (!File.Exists(livePath)) return false;
            var bytes = File.ReadAllBytes(livePath);
            return new[] { "current-a", "current-b", "prior-lkg", "valid-lkg", "recovered-lkg" }
                .Any(label => bytes.SequenceEqual(PayloadBytes(label)));
        }

        private void AssertDiagnostic(SaveEnvelopeCaseFixture testCase, string actual)
        {
            if (testCase.expectedDiagnostic == null || !testCase.expectedDiagnostic.HasAnyField)
            {
                Assert.That(actual, Is.Null, testCase.id);
                return;
            }

            Assert.That(actual, Is.EqualTo(
                $"operation={testCase.expectedDiagnostic.operationRole}; " +
                $"artifact={testCase.expectedDiagnostic.artifactRole}; " +
                $"exception={testCase.expectedDiagnostic.exceptionType}; " +
                $"file={testCase.expectedDiagnostic.relativeFilename}"), testCase.id);
            Assert.That(actual, Does.Not.Contain(directoryPath), testCase.id);
            Assert.That(actual, Does.Not.Contain("secret player").IgnoreCase, testCase.id);
        }

        private void RestoreStaticState()
        {
            if (operationsField != null) operationsField.SetValue(null, previousOperations);
            if (savePathOverrideField != null) savePathOverrideField.SetValue(null, previousSavePathOverride);
        }

        private void DeleteFixtureDirectory()
        {
            if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        private static FieldInfo RequirePrivateStaticField(Type type, string name) =>
            type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new AssertionException($"Required private static field was not found: {type.FullName}.{name}");

        private static object Read(object instance, string propertyName) =>
            instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                .GetValue(instance);

        private static Type RequireType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null)
            ?? throw new AssertionException($"Required runtime type was not found: {fullName}");
    }
}
