// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using System.Linq;
using Xunit;

using static ReverseTemplate.Tests.TestUtils;

namespace ReverseTemplate.Tests {
    public class EngineTests {
        [Fact]
        public void SingleTest() {
            using var input = new StringReader("a=1");
            var engine = MakeTemplate("a={{%d=a}}");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal("1", output["a"].Values.Single());
        }

        [Fact]
        public void SingleGroupTest() {
            using var input = new StringReader("a=1");
            var engine = MakeTemplate("a={{%d=a}}");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Single(output);
        }

        [Fact]
        public void MultipleTest() {
            using var input = new StringReader("a=1\na=2\na=3");
            var engine = MakeTemplate("a={{%d=a}}");
            foreach (var output in engine.ProcessRecords(input, true)) {
                Assert.NotNull(output["a"]);
            }
        }

        [Fact]
        public void TrailingTestCount() {
            using var input = new StringReader("\na=1\n\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            Assert.Equal(3, engine.ProcessRecords(input, true).Count());
        }

        [Fact]
        public void TrailingTestContent() {
            using var input = new StringReader("\na=1\n\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            var count = 0;
            foreach (var output in engine.ProcessRecords(input, true)) {
                Assert.Equal((count + 1).ToString(), output["a"].Values.Single());
                count++;
            }
        }

        [Fact]
        public void MultilineTemplateTest() {
            using var input = new StringReader("a=1\nb=2\na=3\nb=4\n");
            var engine = MakeTemplate("a={{%d=a}}\nb={{%d=b}}\n");
            foreach (var output in engine.ProcessRecords(input, true)) {
                Assert.NotNull(output["a"]);
                Assert.NotNull(output["b"]);
            }
        }

        [Fact]
        public void RegexTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("{{/[a-z]/=key}}={{/[a-z]*/=value}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal("a", output["key"].Values.Single());
            Assert.Equal("xxx", output["value"].Values.Single());
        }

        [Fact]
        public void FilterTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#/a/b\nb={{/[a-z]*/=value}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal("xxx", output["value"].Values.Single());
        }

        [Fact]
        public void DoubleFilterTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#/a/b\n#/b/c\nc={{/[a-z]*/=value}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal("xxx", output["value"].Values.Single());
        }

        [Fact]
        public void FileNameTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#x/{{%d=x}}\na={{/[a-z]*/=value}}\n");
            var output = engine.ProcessRecords(input, false, relativeFilePath: "x/1").Single();
            Assert.Equal("1", output["x"].Values.Single());
            Assert.Equal("xxx", output["value"].Values.Single());
        }

        [Fact]
        public void FileNameAndFilterTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#x/{{%d=x}}\n#/a/b\nb={{/[a-z]*/=value}}\n");
            var output = engine.ProcessRecords(input, false, relativeFilePath: "x/1").Single();
            Assert.Equal("1", output["x"].Values.Single());
            Assert.Equal("xxx", output["value"].Values.Single());
        }

        [Fact]
        public void CaptureNameTransformTest() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("a={{/[a-z]*/=a.b.c.d[]}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal("xxx", output["a__b__c__d"].Values.Single());
        }

        [Fact]
        public void DumbTest() {
            using var input = new StringReader("a\n1=b\na\n2=b\n");
            var engine = MakeTemplate("a\n{{%d=b}}=b\n");
            Assert.Equal(2, engine.ProcessRecords(input, true).Count());
        }

        [Fact]
        public void ArrayTest() {
            using var input = new StringReader("a=1\na=2\na=3\na=4\n");
            var engine = MakeTemplate("a={{%d=a[]|}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal(4, output["a"].Values.Count);
            Assert.Equal("1", output["a"].Values[0]);
            Assert.Equal("2", output["a"].Values[1]);
            Assert.Equal("3", output["a"].Values[2]);
            Assert.Equal("4", output["a"].Values[3]);
        }

        [Fact]
        public void BeginArrayTest() {
            using var input = new StringReader("a=1\na=2\na=3\na=4\nb=0\n");
            var engine = MakeTemplate("a={{%d=a[]|}}\nb={{%d=b}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Equal(4, output["a"].Values.Count);
            Assert.Equal("1", output["a"].Values[0]);
            Assert.Equal("2", output["a"].Values[1]);
            Assert.Equal("3", output["a"].Values[2]);
            Assert.Equal("4", output["a"].Values[3]);
            Assert.Single(output["b"].Values, "0");
        }

        [Fact]
        public void EndArrayTest() {
            using var input = new StringReader("b=0\na=1\na=2\na=3\na=4\n");
            var engine = MakeTemplate("b={{%d=b}}\na={{%d=a[]|}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Single(output["b"].Values, "0");
            Assert.Equal(4, output["a"].Values.Count);
            Assert.Equal("1", output["a"].Values[0]);
            Assert.Equal("2", output["a"].Values[1]);
            Assert.Equal("3", output["a"].Values[2]);
            Assert.Equal("4", output["a"].Values[3]);
        }

        [Fact]
        public void WrappedArrayTest() {
            using var input = new StringReader("b=0\na=1\na=2\na=3\na=4\nc=5\n");
            var engine = MakeTemplate("b={{%d=b}}\na={{%d=a[]|}}\nc={{%d=c}}\n");
            var output = engine.ProcessRecords(input, false).Single();
            Assert.Single(output["b"].Values, "0");
            Assert.Equal(4, output["a"].Values.Count);
            Assert.Equal("1", output["a"].Values[0]);
            Assert.Equal("2", output["a"].Values[1]);
            Assert.Equal("3", output["a"].Values[2]);
            Assert.Equal("4", output["a"].Values[3]);
            Assert.Single(output["c"].Values, "5");
        }
    }
}
