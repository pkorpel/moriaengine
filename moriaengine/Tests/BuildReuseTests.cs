using System;
using System.IO;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #1358 - a boot reuses the last build of a script project when nothing it depends on changed: generated sources are
	/// written only when their text changes, a fingerprint covers the project's inputs, a stamp ties it to the output file.
	/// </summary>
	public sealed class BuildReuseTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "moria-buildreuse-" + Guid.NewGuid().ToString("N"));

		public BuildReuseTests() {
			Directory.CreateDirectory(this.dir);
		}

		public void Dispose() {
			Directory.Delete(this.dir, true);
		}

		private string Write(string relativePath, string text) {
			string path = Path.Combine(this.dir, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text);
			return path;
		}

		[Fact]
		public void AGeneratedSourceIsWrittenOnlyWhenItsTextChanges() {
			string path = Path.Combine(this.dir, "A.Generated.cs");
			Assert.True(MsBuildLauncher.WriteSourceIfChanged(path, "class A {}\r\n"));
			DateTime old = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(path, old);

			Assert.False(MsBuildLauncher.WriteSourceIfChanged(path, "class A {}\r\n"));
			Assert.Equal(old, File.GetLastWriteTimeUtc(path));

			Assert.True(MsBuildLauncher.WriteSourceIfChanged(path, "class A { int x; }\r\n"));
			Assert.NotEqual(old, File.GetLastWriteTimeUtc(path));
			Assert.Equal("class A { int x; }\r\n", File.ReadAllText(path));
		}

		[Fact]
		public void AStampIsReusedOnlyForTheSameInputsAndTheSameOutputFile() {
			string output = this.Write("bin/out.dll", "assembly");
			string stamp = Path.Combine(this.dir, "obj", "Debug", "stamp.txt");

			Assert.False(MsBuildLauncher.TryReuse(stamp, "abc", out _));

			MsBuildLauncher.WriteStamp(stamp, "abc", output);
			Assert.True(MsBuildLauncher.TryReuse(stamp, "abc", out string reused));
			Assert.Equal(Path.GetFullPath(output), reused);
			Assert.True(MsBuildLauncher.LastCompileReused);

			Assert.False(MsBuildLauncher.TryReuse(stamp, "abd", out _));
			Assert.False(MsBuildLauncher.LastCompileReused);

			// rebuilt by something else (an IDE, a plain dotnet build): not the stamped file any more
			File.SetLastWriteTimeUtc(output, File.GetLastWriteTimeUtc(output).AddSeconds(5));
			Assert.False(MsBuildLauncher.TryReuse(stamp, "abc", out _));

			MsBuildLauncher.WriteStamp(stamp, "abc", output);
			File.Delete(output);
			Assert.False(MsBuildLauncher.TryReuse(stamp, "abc", out _));

			// a tree copied with its obj/: the stamp points at the other tree's output
			string elsewhere = Path.Combine(Path.GetTempPath(), "moria-buildreuse-other-" + Guid.NewGuid().ToString("N") + ".dll");
			File.WriteAllText(elsewhere, "assembly");
			try {
				MsBuildLauncher.WriteStamp(stamp, "abc", elsewhere);
				Assert.False(MsBuildLauncher.TryReuse(stamp, "abc", out _));
			} finally {
				File.Delete(elsewhere);
			}
		}

		[Fact]
		public void TheFingerprintFollowsTheProjectsInputsOnly() {
			string source = this.Write("A.cs", "class A {}");
			this.Write("Sub/B.cs", "class B {}");
			this.Write("P.csproj", "<Project />");
			string binFile = this.Write("bin/X.cs", "x");
			string objFile = this.Write("obj/Y.cs", "y");
			string hidden = this.Write(".git/Z.cs", "z");
			string notInput = this.Write("readme.txt", "r");
			string first = BuildFingerprint.Of(this.dir, "Debug", "1");

			Assert.Equal(first, BuildFingerprint.Of(this.dir, "Debug", "1"));
			Assert.NotEqual(first, BuildFingerprint.Of(this.dir, "Release", "1"));
			Assert.NotEqual(first, BuildFingerprint.Of(this.dir, "Debug", "2"));

			File.WriteAllText(binFile, "x2");
			File.WriteAllText(objFile, "y2");
			File.WriteAllText(hidden, "z2");
			File.WriteAllText(notInput, "r2");
			File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddHours(1)); // a write time alone isn't a change
			Assert.Equal(first, BuildFingerprint.Of(this.dir, "Debug", "1"));

			File.WriteAllText(source, "class A { }");
			string edited = BuildFingerprint.Of(this.dir, "Debug", "1");
			Assert.NotEqual(first, edited);

			// bin/obj are skipped only at the project's top, as the SDK's globs do
			this.Write("Sub/bin/W.cs", "w");
			string nested = BuildFingerprint.Of(this.dir, "Debug", "1");
			Assert.NotEqual(edited, nested);

			File.Move(Path.Combine(this.dir, "Sub", "B.cs"), Path.Combine(this.dir, "Sub", "C.cs"));
			string renamed = BuildFingerprint.Of(this.dir, "Debug", "1");
			Assert.NotEqual(nested, renamed);

			this.Write("Strings.resx", "<root />");
			Assert.NotEqual(renamed, BuildFingerprint.Of(this.dir, "Debug", "1"));
		}
	}
}
