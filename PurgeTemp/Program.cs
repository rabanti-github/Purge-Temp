/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
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
	static void Main(string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureServices((context, services) =>
			{
				services.AddSingleton<IConfiguration>(context.Configuration);
				services.AddTransient<CLI>();
				services.AddSingleton<ISettings, Settings>(provider =>
				{
					IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
					Settings settings = new Settings(configuration, Settings.APP_SETTINGS_FILE);
					if (args.Length != 0)
					{
						CLI cli = provider.GetRequiredService<CLI>();
						settings = cli.ParseSetting(settings, configuration, args);
					}

					return settings;
				});
				services.AddTransient<IPurgeLogger, PurgeLogger>();
				services.AddTransient<IAppLogger, AppLogger>();
				services.AddTransient<IDesktopNotification, DesktopNotification>();
				services.AddTransient<FileUtils>();
				services.AddTransient<PathUtils>();
				services.AddTransient<PurgeController>();
			})
			.Build();

		using (IServiceScope scope = host.Services.CreateScope())
		{
			IServiceProvider services = scope.ServiceProvider;
			PurgeController purgeController = services.GetRequiredService<PurgeController>();
			purgeController.ExecutePurge();
		}
	}



	//	public static IHostBuilder CreateHostBuilder(string[] args) =>
	//	Host.CreateDefaultBuilder(args)
	//		.ConfigureAppConfiguration((context, config) =>
	//		{
	//			config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
	//			if (args.Length > 0)
	//			{
	//				config.AddJsonFile(args[0], optional: true, reloadOnChange: true);
	//			}
	//		})
	//		.ConfigureServices((context, services) =>
	//		{
	//			services.AddSingleton<IConfiguration>(context.Configuration);
	//			services.AddSingleton<ISettings, Settings>();
	//			services.AddTransient<IAppLogger, AppLogger>();
	//			services.AddTransient<PurgeController>();
	//		});

	/// <summary>
	/// Returns an application context for test purposes
	/// </summary>
	/// <param name="testSettings">Settings that overrides the default settings for the test environment</param>
	/// <returns>PurgeController instance, based on the test settings</returns>
	public static PurgeController GetTestEnvironment(Settings testSetting)
	{
		string[] args = Array.Empty<string>();
		return GetTestEnvironment(testSetting, args);
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
				services.AddSingleton<CLI>();
				services.AddSingleton<ISettings, Settings>(provider =>
				{
					IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
					Settings settings = new Settings(configuration, Settings.APP_SETTINGS_FILE);
					if (args.Length != 0)
					{
						CLI cli = provider.GetRequiredService<CLI>();
						settings = cli.ParseSetting(settings, configuration, args);
					}

					return settings;
				});
				services.AddSingleton<IPurgeLogger, PurgeLogger>();
				services.AddSingleton<IAppLogger, AppLogger>();
				services.AddSingleton<IDesktopNotification, DesktopNotification>();
				services.AddSingleton<FileUtils>();
				services.AddSingleton<PathUtils>();
				services.AddTransient<PurgeController>();
			})
			.Build();

		IServiceScope scope = host.Services.CreateScope();
		IServiceProvider services = scope.ServiceProvider;
		return services.GetRequiredService<PurgeController>();
	}

}
