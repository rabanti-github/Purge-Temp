/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
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

		public static string GetAssemblyName(Assembly? assembly = null)
		{
			assembly ??= Assembly.GetExecutingAssembly();
			return assembly.GetName().Name ?? "Purge-Temp";
		}

		public static string GetVersion(Assembly? assembly = null)
		{
			assembly ??= Assembly.GetExecutingAssembly();
			return assembly.GetName().Version?.ToString() ?? "0.0.0";
		}

		private static string GetAttributeValue<T>(Func<T, string> valueSelector, string defaultValue, Assembly? assembly = null) where T : Attribute
		{
			assembly ??= Assembly.GetExecutingAssembly();
			T? attribute = (T?)Attribute.GetCustomAttribute(assembly, typeof(T), false);
			return attribute != null ? valueSelector(attribute) : defaultValue;
		}

		public static string GetCopyright(Assembly? assembly = null)
		{
			return GetAttributeValue<AssemblyCopyrightAttribute>(attr => attr.Copyright, "See copyright information at project website", assembly);
		}

		public static string GetDescription(Assembly? assembly = null)
		{
			return GetAttributeValue<AssemblyDescriptionAttribute>(attr => attr.Description, "See project website for a exact description of the application", assembly);
		}

		public static string GetRepositoryURL(Assembly? assembly = null)
		{
			assembly ??= Assembly.GetExecutingAssembly();
			string? attribute = ((AssemblyMetadataAttribute[])Attribute.GetCustomAttributes(
					 assembly, typeof(AssemblyMetadataAttribute)))
					 .FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;
			return attribute ?? "<could not determine the Repository URL>";
		}

	}
}
