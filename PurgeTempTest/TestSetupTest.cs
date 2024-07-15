using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	/// <summary>
	/// Test class to check the utility functions of the test project
	/// </summary>
	public class TestSetupTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;

		public TestSetupTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		[Fact(DisplayName = "Test of the working test setup on the creation of a temp folder")]
		public void CreateFileInTempFolderTest()
		{
			string filePath = Path.Combine(fixture.CreateUniqueTempFolder(), "testFile.txt");
			File.WriteAllText(filePath, "Hello, World!");
			Assert.True(File.Exists(filePath));
			Assert.Equal("Hello, World!", File.ReadAllText(filePath));
		}

		[Fact(DisplayName = "Test of the working test setup on the deletion of a previously created temp folder")]
		public void DeleteFileInTempFolderTest()
		{
			string filePath = Path.Combine(fixture.CreateUniqueTempFolder(), "testFile.txt");
			File.WriteAllText(filePath, "Hello, World!");
			File.Delete(filePath);
			Assert.False(File.Exists(filePath));
		}
	}
}