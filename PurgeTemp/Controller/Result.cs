/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

namespace PurgeTemp.Controller
{
	/// <summary>
	/// Class to handle any operation or check with a result of the type T and an error code
	/// </summary>
	/// <typeparam name="T">Type of the result payload (can be null)</typeparam>
	public class Result<T> : Result
	{
		private T? value;

		/// <summary>
		/// Optional payload of the type T
		/// </summary>
		public T? Value
		{
			get { return this.value; }
		}

		public static Result<T> Success(T value)
		{
			Result<T> result = new Result<T>();
			result.value = value;
			result.ErrorCode = ErrorCodes.Success;
			result.IsValid = true;
			return result;
		}

		public static Result<T> Fail(T value, int errorCode = ErrorCodes.UnknownError)
		{
			Result<T> result = new Result<T>();
			result.value = value;
			result.ErrorCode = errorCode;
			result.IsValid = false;
			return result;
		}

		public static Result<T> Fail(int errorCode = ErrorCodes.UnknownError)
		{
			Result<T> result = new Result<T>();
			result.ErrorCode = errorCode;
			result.IsValid = false;
			return result;
		}
	}

	/// <summary>
	/// Class to handle any operation or check that should just return a status (validity and error code)
	/// </summary>
	public class Result
	{
		private int errorCode;
		private bool isValid;

		/// <summary>
		/// If true, the result is valid
		/// </summary>
		public bool IsValid
		{
			get { return isValid; }
			internal set { isValid = value; }
		}

		/// <summary>
		/// If true, the result is invalid (Inversion of IsValid property)
		/// </summary>
		public bool IsNotValid
		{
			get { return !isValid; }
		}

		/// <summary>
		/// Error code, referring to <see cref="ErrorCodes"/> 
		/// </summary>
		public int ErrorCode
		{
			get { return errorCode; }
			internal set { errorCode = value; }
		}

		public static Result Success()
		{
			Result result = new Result();
			result.ErrorCode = ErrorCodes.Success;
			result.IsValid = true;
			return result;
		}

		public static Result Fail(int errorCode = ErrorCodes.UnknownError)
		{
			Result result = new Result();
			result.ErrorCode = errorCode;
			result.IsValid = false;
			return result;
		}
	}
}
