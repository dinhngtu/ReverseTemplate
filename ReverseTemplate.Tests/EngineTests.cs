using ReverseTemplate.Engine;
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
            var templateLine = AssertParses(@"a={{%d=a}}");
            var engine = new Template(new[] { templateLine });
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal("1", output["a"]);
        }

        [Fact]
        public async Task SingleGroupTestAsync() {
            using var input = new StringReader("a=1");
            var templateLine = AssertParses(@"a={{%d=a}}");
            var engine = new Template(new[] { templateLine });
            var output = await engine.ProcessRecordsAsync(input, false).SingleAsync();
            Assert.Equal(1, output.Count);
        }

        [Fact]
        public async Task MultipleTestAsync() {
            using var input = new StringReader("a=1\na=2\na=3");
            var templateLine = AssertParses(@"a={{%d=a}}");
            var engine = new Template(new[] { templateLine });
            await foreach (var output in engine.ProcessRecordsAsync(input, true)) {
                Assert.NotNull(output["a"]);
            }
        }

        [Fact]
        public async Task TrailingTestCountAsync() {
            using var input = new StringReader("\na=1\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            Assert.Equal(3, await engine.ProcessRecordsAsync(input, true).CountAsync());
        }

        [Fact]
        public async Task TrailingTestContentAsync() {
            using var input = new StringReader("\na=1\na=2\n\na=3\n\n\n");
            var engine = MakeTemplate("a={{%d=a}}\n\n");
            var count = 0;
            await foreach (var output in engine.ProcessRecordsAsync(input, true)) {
                Assert.Equal((count + 1).ToString(), output["a"]);
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
            Assert.Equal("a", output["key"]);
            Assert.Equal("xxx", output["value"]);
        }
    }
}
