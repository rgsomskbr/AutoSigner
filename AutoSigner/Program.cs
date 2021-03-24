﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.IO;

namespace AutoSigner
{
	internal static class Program
	{
		private const string ConfigName = "config.json";
		private const string LogFileName = "autosigner-.log";

		private static ConfigOptions GetValidOptions(IServiceProvider serviceProvider)
		{
			try
			{
				return serviceProvider.GetRequiredService<IOptions<ConfigOptions>>().Value;
			}
			catch (OptionsValidationException ex)
			{
				Log.Error("Ошибка валидации параметров:");
				foreach (var failure in ex.Failures)
				{
					Log.Error("{Failure}", failure);
				}

				return null;
			}
		}

		private static int Main()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console()
				.CreateLogger()
			;

			try
			{
				Log.Information("Инициализация");

				var configuration = new ConfigurationBuilder()
					.AddJsonFile(ConfigName)
					.Build()
				;

				var services = new ServiceCollection()
					.AddSingleton(configuration)
				;

				services
					.AddOptions<ConfigOptions>()
					.Bind(configuration)
#if DEBUG
					.Configure(cfg => cfg.LogFile = "logs")
#endif
					.ValidateDataAnnotations()
				;

				using var serviceProvider = services.BuildServiceProvider(true);
				var configOptions = GetValidOptions(serviceProvider);
				if (configOptions == null)
				{
					return 1;
				}

				if (!string.IsNullOrWhiteSpace(configOptions.LogFile))
				{
					Log.CloseAndFlush();

					Log.Logger = new LoggerConfiguration()
						.MinimumLevel.Debug()
						.WriteTo.Console()
						.WriteTo.File(Path.Combine(configOptions.LogFile, LogFileName), rollingInterval: RollingInterval.Day)
						.CreateLogger();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Необработанное исключение");
			}
			finally
			{
				Log.CloseAndFlush();
			}

			return 0;
		}
	}
}