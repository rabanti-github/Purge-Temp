using PurgeTemp;
using PurgeTemp.Controller;

namespace PurgeTempTest
{
	public class ResultTest
	{
		// ========== Non-generic Result tests ==========

		[Fact(DisplayName = "Test that Result.Success() returns a valid result with success error code")]
		public void ResultSuccessTest()
		{
			Result result = Result.Success();
			Assert.True(result.IsValid);
			Assert.False(result.IsNotValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that Result.Fail() returns an invalid result with default UnknownError code")]
		public void ResultFailDefaultTest()
		{
			Result result = Result.Fail();
			Assert.False(result.IsValid);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.UnknownError, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that Result.Fail(errorCode) returns an invalid result with the specified error code")]
		[InlineData(ErrorCodes.InvalidArguments)]
		[InlineData(ErrorCodes.EmptyFolderName)]
		[InlineData(ErrorCodes.CouldNotDeleteLastFolder)]
		[InlineData(ErrorCodes.ExecutionTooFrequent)]
		public void ResultFailWithCodeTest(int givenErrorCode)
		{
			Result result = Result.Fail(givenErrorCode);
			Assert.False(result.IsValid);
			Assert.True(result.IsNotValid);
			Assert.Equal(givenErrorCode, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that IsNotValid is the inverse of IsValid on a success result")]
		public void ResultIsNotValidInverseOnSuccessTest()
		{
			Result result = Result.Success();
			Assert.NotEqual(result.IsValid, result.IsNotValid);
		}

		[Fact(DisplayName = "Test that IsNotValid is the inverse of IsValid on a fail result")]
		public void ResultIsNotValidInverseOnFailTest()
		{
			Result result = Result.Fail();
			Assert.NotEqual(result.IsValid, result.IsNotValid);
		}

		// ========== Generic Result<T> tests with string ==========

		[Theory(DisplayName = "Test that Result<string>.Success(value) returns a valid result with the given payload")]
		[InlineData("hello")]
		[InlineData("")]
		[InlineData("special chars !@#$%")]
		public void ResultStringSuccessTest(string givenValue)
		{
			Result<string> result = Result<string>.Success(givenValue);
			Assert.True(result.IsValid);
			Assert.False(result.IsNotValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Theory(DisplayName = "Test that Result<string>.Fail(value) returns an invalid result with the given payload and default error code")]
		[InlineData("error data")]
		[InlineData("")]
		public void ResultStringFailWithValueDefaultCodeTest(string givenValue)
		{
			Result<string> result = Result<string>.Fail(givenValue);
			Assert.False(result.IsValid);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.UnknownError, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Theory(DisplayName = "Test that Result<string>.Fail(value, errorCode) returns an invalid result with the given payload and error code")]
		[InlineData("error data", ErrorCodes.InvalidArguments)]
		[InlineData("path issue", ErrorCodes.EmptyFolderName)]
		[InlineData("", ErrorCodes.CouldNotDeleteLastFolder)]
		public void ResultStringFailWithValueAndCodeTest(string givenValue, int givenErrorCode)
		{
			Result<string> result = Result<string>.Fail(givenValue, givenErrorCode);
			Assert.False(result.IsValid);
			Assert.True(result.IsNotValid);
			Assert.Equal(givenErrorCode, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Theory(DisplayName = "Test that Result<string>.Fail(errorCode) returns an invalid result with no payload and the specified error code")]
		[InlineData(ErrorCodes.InvalidArguments)]
		[InlineData(ErrorCodes.UnknownError)]
		[InlineData(ErrorCodes.CouldNotCreateNewStageFolder)]
		public void ResultStringFailWithCodeOnlyTest(int givenErrorCode)
		{
			Result<string> result = Result<string>.Fail(givenErrorCode);
			Assert.False(result.IsValid);
			Assert.True(result.IsNotValid);
			Assert.Equal(givenErrorCode, result.ErrorCode);
			Assert.Null(result.Value);
		}

		// ========== Generic Result<T> tests with int ==========

		[Theory(DisplayName = "Test that Result<int>.Success(value) returns a valid result with the given int payload")]
		[InlineData(0)]
		[InlineData(42)]
		[InlineData(-1)]
		public void ResultIntSuccessTest(int givenValue)
		{
			Result<int> result = Result<int>.Success(givenValue);
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Theory(DisplayName = "Test that Result<int>.Fail(value, errorCode) returns an invalid result with the given int payload")]
		[InlineData(0, ErrorCodes.InvalidArguments)]
		[InlineData(99, ErrorCodes.CouldNotRenameStageFolder)]
		public void ResultIntFailWithValueAndCodeTest(int givenValue, int givenErrorCode)
		{
			Result<int> result = Result<int>.Fail(givenValue, givenErrorCode);
			Assert.False(result.IsValid);
			Assert.Equal(givenErrorCode, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Fact(DisplayName = "Test that Result<int>.Fail(errorCode) returns an invalid result with default int value")]
		public void ResultIntFailWithCodeOnlyTest()
		{
			Result<int> result = Result<int>.Fail(ErrorCodes.InvalidArguments);
			Assert.False(result.IsValid);
			Assert.Equal(ErrorCodes.InvalidArguments, result.ErrorCode);
			Assert.Equal(0, result.Value);
		}

		// ========== Generic Result<T> tests with bool ==========

		[Theory(DisplayName = "Test that Result<bool>.Success(value) returns a valid result with the given bool payload")]
		[InlineData(true)]
		[InlineData(false)]
		public void ResultBoolSuccessTest(bool givenValue)
		{
			Result<bool> result = Result<bool>.Success(givenValue);
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		[Theory(DisplayName = "Test that Result<bool>.Fail(value, errorCode) returns an invalid result with the given bool payload")]
		[InlineData(true, ErrorCodes.UnknownError)]
		[InlineData(false, ErrorCodes.InvalidArguments)]
		public void ResultBoolFailWithValueAndCodeTest(bool givenValue, int givenErrorCode)
		{
			Result<bool> result = Result<bool>.Fail(givenValue, givenErrorCode);
			Assert.False(result.IsValid);
			Assert.Equal(givenErrorCode, result.ErrorCode);
			Assert.Equal(givenValue, result.Value);
		}

		// ========== Generic Result<T> tests with nullable reference type ==========

		[Fact(DisplayName = "Test that Result<string>.Success(null) returns a valid result with null payload")]
		public void ResultStringSuccessNullValueTest()
		{
			Result<string> result = Result<string>.Success(null!);
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
			Assert.Null(result.Value);
		}

		[Fact(DisplayName = "Test that Result<string>.Fail(null) returns an invalid result with null payload")]
		public void ResultStringFailNullValueTest()
		{
			Result<string> result = Result<string>.Fail(null!);
			Assert.False(result.IsValid);
			Assert.Equal(ErrorCodes.UnknownError, result.ErrorCode);
			Assert.Null(result.Value);
		}

		// ========== Inheritance tests ==========

		[Fact(DisplayName = "Test that Result<T> inherits from Result")]
		public void ResultGenericInheritsFromResultTest()
		{
			Result<string> result = Result<string>.Success("test");
			Assert.IsAssignableFrom<Result>(result);
		}

		[Fact(DisplayName = "Test that a generic result can be accessed as non-generic Result")]
		public void ResultGenericAsNonGenericTest()
		{
			Result result = Result<string>.Success("test");
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}
	}
}
