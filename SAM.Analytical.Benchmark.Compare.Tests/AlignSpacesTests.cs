// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class AlignSpacesTests
    {
        private const string GuidA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string GuidB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        private const string GuidC = "cccccccccccccccccccccccccccccccc";

        [TestMethod]
        public void MatchesByGuidEvenWhenNamesDiffer()
        {
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(GuidA, "Office", 40, 120) },
                new[] { Builders.Space(GuidA, "Office renamed by OpenStudio", 40, 120) });

            Assert.AreEqual(1, alignment.Pairs.Count);
            Assert.AreEqual(SpaceMatchKind.Guid, alignment.Pairs[0].MatchKind);
            Assert.AreEqual(1, alignment.Diagnostics.MatchedByGuid);
            Assert.AreEqual(0, alignment.Diagnostics.MatchedByName);
        }

        [TestMethod]
        public void FallsBackToNormalizedNameWhenNoGuid()
        {
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(null, "Office 1", 40, 120) },
                new[] { Builders.Space(null, "  office 1 ", 40, 120) });

            Assert.AreEqual(1, alignment.Pairs.Count);
            Assert.AreEqual(SpaceMatchKind.Name, alignment.Pairs[0].MatchKind);
            Assert.AreEqual(1, alignment.Diagnostics.MatchedByName);
        }

        [TestMethod]
        public void ReportsSpacesPresentOnOnlyOneSide()
        {
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(GuidA, "Shared", 40, 120), Builders.Space(GuidB, "TasOnly", 10, 30) },
                new[] { Builders.Space(GuidA, "Shared", 40, 120), Builders.Space(GuidC, "OsOnly", 12, 36) });

            CollectionAssert.AreEqual(new[] { GuidB }, alignment.Diagnostics.OnlyInTas.ToArray());
            CollectionAssert.AreEqual(new[] { GuidC }, alignment.Diagnostics.OnlyInOpenStudio.ToArray());
            Assert.AreEqual(1, alignment.Pairs.Count);
        }

        [TestMethod]
        public void DuplicateGuidExcludesEveryMemberFromMatching()
        {
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(GuidA, "First", 40, 120), Builders.Space(GuidA, "DuplicateGuid", 41, 121) },
                new[] { Builders.Space(GuidA, "Match", 40, 120) });

            CollectionAssert.AreEqual(new[] { GuidA }, alignment.Diagnostics.DuplicateTasGuids.ToArray());
            // A duplicated GUID is not uniquely identifiable, so NOTHING is matched — not even the OpenStudio
            // side that has a single GuidA — rather than arbitrarily aligning the first duplicate.
            Assert.AreEqual(0, alignment.Pairs.Count);
            Assert.AreEqual(2, alignment.OnlyInTas.Count);
            Assert.AreEqual(1, alignment.OnlyInOpenStudio.Count);
        }

        [TestMethod]
        public void DoesNotNameMatchWhenBothSidesHaveDifferentValidGuids()
        {
            // Same name but two different valid GUIDs are explicit, conflicting identities: they must not be
            // merged by name.
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(GuidB, "Office", 40, 120) },
                new[] { Builders.Space(GuidC, "Office", 40, 120) });

            Assert.AreEqual(0, alignment.Pairs.Count);
            CollectionAssert.Contains(alignment.Diagnostics.AmbiguousNameMatches.ToArray(), "OFFICE");
            Assert.AreEqual(1, alignment.OnlyInTas.Count);
            Assert.AreEqual(1, alignment.OnlyInOpenStudio.Count);
        }

        [TestMethod]
        public void NameMatchesWhenOneSideLacksAGuid()
        {
            // One side lacks a usable GUID, so name fallback is allowed and adopts the available GUID.
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(null, "Office", 40, 120) },
                new[] { Builders.Space(GuidC, "Office", 40, 120) });

            Assert.AreEqual(1, alignment.Pairs.Count);
            Assert.AreEqual(SpaceMatchKind.Name, alignment.Pairs[0].MatchKind);
            Assert.AreEqual(GuidC, alignment.Pairs[0].Guid);
        }

        [TestMethod]
        public void DiagnosesAmbiguousSplitNameInsteadOfGuessing()
        {
            SpaceAlignment alignment = Query.AlignSpaces(
                new[] { Builders.Space(null, "Room", 40, 120) },
                new[] { Builders.Space(null, "Room", 20, 60), Builders.Space(null, "room", 20, 60) });

            Assert.AreEqual(0, alignment.Pairs.Count);
            CollectionAssert.Contains(alignment.Diagnostics.AmbiguousNameMatches.ToArray(), "ROOM");
            CollectionAssert.Contains(alignment.Diagnostics.DuplicateOpenStudioNames.ToArray(), "ROOM");
            Assert.AreEqual(1, alignment.OnlyInTas.Count);
            Assert.AreEqual(2, alignment.OnlyInOpenStudio.Count);
        }

        [TestMethod]
        public void AlignmentIsDeterministicRegardlessOfInputOrder()
        {
            List<BenchmarkSpaceResult> tas = new()
            {
                Builders.Space(GuidB, "B", 10, 30),
                Builders.Space(GuidA, "A", 40, 120)
            };
            List<BenchmarkSpaceResult> openStudio = new()
            {
                Builders.Space(GuidA, "A", 40, 120),
                Builders.Space(GuidB, "B", 10, 30)
            };

            SpaceAlignment alignment = Query.AlignSpaces(tas, openStudio);

            // GUID matches are ordered by ordinal GUID, so A precedes B irrespective of the source order.
            Assert.AreEqual(GuidA, alignment.Pairs[0].Guid);
            Assert.AreEqual(GuidB, alignment.Pairs[1].Guid);
        }
    }
}
