using System.Reflection;
using PurgeTemp.Utils;

namespace PurgeTempTest
{
	public class AssemblyUtilsTest
	{
		private static readonly Assembly testAssembly = typeof(AssemblyUtilsTest).Assembly;

		// ========== GetAssemblyName tests ==========

		[Theory(DisplayName = "Test that GetAssemblyName returns the correct name for the given assembly")]
		[InlineData(false, "PurgeTemp")]
		[InlineData(true, "PurgeTempTest")]
		public void GetAssemblyNameReturnsExpectedNameTest(bool useTestAssembly, string expected)
		{
			Assembly? assembly = useTestAssembly ? testAssembly : null;
			Assert.Equal(expected, AssemblyUtils.GetAssemblyName(assembly));
		}

		// ========== GetVersion tests ==========

		[Fact(DisplayName = "Test that GetVersion returns '1.0.0.0' for the default (PurgeTemp) assembly")]
		public void GetVersionDefaultReturnsExpectedVersionTest()
		{
			Assert.Equal("1.0.0.0", AssemblyUtils.GetVersion());
		}

		[Fact(DisplayName = "Test that GetVersion returns a non-empty version string for a given assembly")]
		public void GetVersionWithAssemblyReturnsNonEmptyStringTest()
		{
			string result = AssemblyUtils.GetVersion(testAssembly);
			Assert.False(string.IsNullOrEmpty(result));
			Assert.NotEqual("0.0.0", result);
		}

		// ========== GetCopyright tests ==========

		[Theory(DisplayName = "Test that GetCopyright returns the copyright string or the fallback for the given assembly")]
		[InlineData(false, "Copyright (c) 2026 Raphael Stoeckli")]
		[InlineData(true, "See copyright information at project website")]
		public void GetCopyrightReturnsExpectedValueTest(bool useTestAssembly, string expected)
		{
			Assembly? assembly = useTestAssembly ? testAssembly : null;
			Assert.Equal(expected, AssemblyUtils.GetCopyright(assembly));
		}

		// ========== GetDescription tests ==========

		[Fact(DisplayName = "Test that GetDescription returns a non-empty description for the default (PurgeTemp) assembly")]
		public void GetDescriptionDefaultReturnsNonEmptyTest()
		{
			string result = AssemblyUtils.GetDescription();
			Assert.False(string.IsNullOrEmpty(result));
			Assert.StartsWith("Purge-Temp is an application", result);
		}

		[Fact(DisplayName = "Test that GetDescription returns the fallback text when the assembly has no description attribute")]
		public void GetDescriptionWithTestAssemblyReturnsFallbackTest()
		{
			string result = AssemblyUtils.GetDescription(testAssembly);
			Assert.Equal("See project website for a exact description of the application", result);
		}

		// ========== GetRepositoryURL tests ==========

		[Theory(DisplayName = "Test that GetRepositoryURL returns the repository URL or the fallback for the given assembly")]
		[InlineData(false, "https://rabanti.ch")]
		[InlineData(true, "<could not determine the Repository URL>")]
		public void GetRepositoryURLReturnsExpectedValueTest(bool useTestAssembly, string expected)
		{
			Assembly? assembly = useTestAssembly ? testAssembly : null;
			Assert.Equal(expected, AssemblyUtils.GetRepositoryURL(assembly));
		}
	}
}
