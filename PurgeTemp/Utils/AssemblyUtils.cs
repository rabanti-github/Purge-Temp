/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using System.Reflection;

namespace PurgeTemp.Utils
{
	/// <summary>
	/// Utils class to get information about the current assembly
	/// </summary>
	public class AssemblyUtils
	{

		public static string GetAssemblyName()
		{
			string? assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			if (assemblyName != null)
			{
				return assemblyName.ToString();
			}
			else
			{
				return "Purge-Temp";
			}
		}

		public static string GetVersion()
		{
			Version? version = Assembly.GetExecutingAssembly().GetName().Version;
			if (version != null)
			{
				return version.ToString();
			}
			else
			{
				return "0.0.0";
			}
		}

		private static string GetAttributeValue<T>(Func<T, string> valueSelector, string defaultValue) where T : Attribute
		{
			T? attribute = (T?)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T), false);
			return attribute != null ? valueSelector(attribute) : defaultValue;
		}

		public static string GetCopyright()
		{
			return GetAttributeValue<AssemblyCopyrightAttribute>(attr => attr.Copyright, "See copyright information at project website");
		}

		public static string GetDescription()
		{
			return GetAttributeValue<AssemblyDescriptionAttribute>(attr => attr.Description, "See project website for a exact description of the application");
		}

		public static string GetRepositoryURL()
		{
			string? attribute = ((AssemblyMetadataAttribute[])Attribute.GetCustomAttributes(
					 Assembly.GetExecutingAssembly(), typeof(AssemblyMetadataAttribute)))
					 .FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;
			return attribute ?? "<could not determine the Repository URL>";
		}
		
	}
}
