/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PurgeTemp.Controller;
using PurgeTemp.Interface;
using PurgeTemp.Logger;
using PurgeTemp.Utils;

/// <summary>
/// Main class
/// </summary>
public class Program
{
	static int Main(string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureServices((context, services) =>
			{
				services.AddSingleton<IConfiguration>(context.Configuration);
				services.AddSingleton<ILoggerFactory, LoggerFactory>();
				services.AddTransient<ISettings, Settings>(provider =>
				{
					IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
					Settings settings = new Settings(configuration, Settings.APP_SETTINGS_FILE);
					if (args.Length != 0)
					{
						ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
						PathUtils pathUtils = new PathUtils(settings, loggerFactory);
						CLI cli = new CLI(pathUtils);
						settings = cli.ParseSetting(settings, configuration, args);
					}

					return settings;
				});
				services.AddTransient<IPurgeLogger, PurgeLogger>();
				services.AddTransient<IAppLogger, AppLogger>();
				services.AddTransient<IDesktopNotification, DesktopNotification>();
				services.AddTransient<PathUtils>();
				services.AddTransient<PurgeController>();
			})
			.Build();

		using (IServiceScope scope = host.Services.CreateScope())
		{
			IServiceProvider services = scope.ServiceProvider;
			PurgeController purgeController = services.GetRequiredService<PurgeController>();
			int statusCode = purgeController.ExecutePurge();
			return statusCode;
		}
	}

	/// <summary>
	/// Returns an application context for test purposes
	/// </summary>
	/// <param name="testSettings">Settings that overrides the default settings for the test environment</param>
	/// <returns>PurgeController instance, based on the test settings</returns>
	public static PurgeController GetTestEnvironment(Settings testSettings)
	{
		string[] args = Array.Empty<string>();
		testSettings.TestEnvironmentMessage = "TEST-ENVIRONMENT : ";
		return GetTestEnvironment(testSettings, args);
	}

	/// <summary>
	/// Returns an application context for test purposes
	/// </summary>
	/// <param name="testSettings">Settings that overrides the default settings for the test environment</param>
	/// <param name="args">Program arguments to be parsed (optional)</param>
	/// <returns>PurgeController instance, based on the test settings</returns>
	public static PurgeController GetTestEnvironment(Settings testSettings, string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureServices((context, services) =>
			{
				services.AddSingleton<IConfiguration>(context.Configuration);
				services.AddSingleton<ILoggerFactory, LoggerFactory>();
				services.AddTransient<ISettings, Settings>(provider =>
				{
					IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
					Settings settings;
					if (testSettings == null)
					{
						settings = new Settings(configuration, Settings.APP_SETTINGS_FILE);
					}
					else
					{
						settings = testSettings;
					}
					if (args.Length != 0)
					{
						ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
						PathUtils pathUtils = new PathUtils(settings, loggerFactory);
						CLI cli = new CLI(pathUtils);
						settings = cli.ParseSetting(settings, configuration, args);
					}

					return settings;
				});
				services.AddTransient<IPurgeLogger, PurgeLogger>();
				services.AddTransient<IAppLogger, AppLogger>();
				services.AddTransient<IDesktopNotification, DesktopNotification>();
				services.AddTransient<PathUtils>();
				services.AddTransient<PurgeController>();
			})
			.Build();

		IServiceScope scope = host.Services.CreateScope();
		IServiceProvider services = scope.ServiceProvider;
		return services.GetRequiredService<PurgeController>();
	}

}
