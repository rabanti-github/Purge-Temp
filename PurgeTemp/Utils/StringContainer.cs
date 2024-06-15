/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

namespace PurgeTemp.Utils
{
	public struct StringContainer
	{
        public string Value { get; private set; }
        public bool Valid { get; private set; }
        public string ErrorMessage { get; private set; }

        public StringContainer(string value, bool valid, string errorMessage) {
            this.Value = value;
            this.Valid = valid;
            this.ErrorMessage = errorMessage;
        }

		public static StringContainer GetValidString(string value)
        {
            return new StringContainer(value, true, null);
        }

		public static StringContainer GetEmptyString()
		{
			return new StringContainer(null, true, null);
		}

		public static StringContainer GetInvalisString(string errorMessage)
		{
			return new StringContainer(null, false, errorMessage);
		}

        public override string ToString()
        {
            return Value;
        }

	}
}
