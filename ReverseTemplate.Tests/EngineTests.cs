// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using static ReverseTemplate.Tests.TestUtils;

namespace ReverseTemplate.Tests {
    public class EngineTests {
        [Fact]
        public async Task SingleTestAsync() {
            using var input = new StringReader("a=1");
            var engine = MakeTemplate("a={{%d=a}}");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("1", output["a"]?.Value);
        }

        [Fact]
        public async Task SingleGroupTestAsync() {
            using var input = new StringReader("a=1");
            var engine = MakeTemplate("a={{%d=a}}");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal(1, output.Count);
        }

        [Fact]
        public async Task MultipleTestAsync() {
            using var input = new StringReader("a=1\na=2\na=3");
            var engine = MakeTemplate("a={{%d=a}}");
            await foreach (var output in engine.ProcessRecordsAsync(input, true)) {
                Assert.NotNull(output["a"]);
            }
        }

        [Fact]
        public async Task TrailingTestCountAsync() {
            using var input = new StringReader("\na=1\n\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            Assert.Equal(3, await engine.ProcessRecordsAsync(input, true).CountAsync());
        }

        [Fact]
        public async Task TrailingTestContentAsync() {
            using var input = new StringReader("\na=1\n\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            var count = 0;
            await foreach (var output in engine.ProcessRecordsAsync(input, true)) {
                Assert.Equal((count + 1).ToString(), output["a"]?.Value);
                count++;
            }
        }

        [Fact]
        public async Task MultilineTemplateTestAsync() {
            using var input = new StringReader("a=1\nb=2\na=3\nb=4\n");
            var engine = MakeTemplate("a={{%d=a}}\nb={{%d=b}}\n");
            await foreach (var output in engine.ProcessRecordsAsync(input, true)) {
                Assert.NotNull(output["a"]);
                Assert.NotNull(output["b"]);
            }
        }

        [Fact]
        public async Task RegexTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("{{/[a-z]/=key}}={{/[a-z]*/=value}}\n");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("a", output["key"]?.Value);
            Assert.Equal("xxx", output["value"]?.Value);
        }

        [Fact]
        public async Task FilterTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#/a/b\nb={{/[a-z]*/=value}}\n");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("xxx", output["value"]?.Value);
        }

        [Fact]
        public async Task DoubleFilterTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#/a/b\n#/b/c\nc={{/[a-z]*/=value}}\n");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("xxx", output["value"]?.Value);
        }

        [Fact]
        public async Task FileNameTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#x/{{%d=x}}\na={{/[a-z]*/=value}}\n");
            var output = await engine.ProcessRecordsAsync(input, false, relativeFilePath: "x/1").SingleAsync();
            Assert.Equal("1", output["x"]?.Value);
            Assert.Equal("xxx", output["value"]?.Value);
        }

        [Fact]
        public async Task FileNameAndFilterTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("#x/{{%d=x}}\n#/a/b\nb={{/[a-z]*/=value}}\n");
            var output = await engine.ProcessRecordsAsync(input, false, relativeFilePath: "x/1").SingleAsync();
            Assert.Equal("1", output["x"]?.Value);
            Assert.Equal("xxx", output["value"]?.Value);
        }

        [Fact]
        public async Task CaptureNameTransformTestAsync() {
            using var input = new StringReader("a=xxx\n");
            var engine = MakeTemplate("a={{/[a-z]*/=a.b.c.d[]}}\n");
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("xxx", output["a__b__c__d"]?.Value);
        }

        [Fact]
        public async Task DumbTestAsync() {
            using var input = new StringReader("a\n1=b\na\n2=b\n");
            var engine = MakeTemplate("a\n{{%d=b}}=b\n");
            Assert.Equal(2, await engine.ProcessRecordsAsync(input, true).CountAsync());
        }
    }
}
