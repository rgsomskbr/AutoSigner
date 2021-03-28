using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoSigner
{
	internal static class Program
	{
		private const string ValidationErrorTemplate = "Ошибка валидации конфигурации: {Error}";
		private const string ConfigName = "config.json";
		private const string LogFileName = "autosigner-.log";

		private static ConfigOptions GetValidOptions()
		{
			var configText = File.ReadAllText(ConfigName);
			var config = System.Text.Json.JsonSerializer.Deserialize<ConfigOptions>(configText);
			if (string.IsNullOrWhiteSpace(config.SourceDirectory))
			{
				Log.Error(ValidationErrorTemplate, "Не указана исходная папка");
				return null;
			}

			if (string.IsNullOrWhiteSpace(config.DestinationDirectory))
			{
				Log.Error(ValidationErrorTemplate, "Не указана папка назначения");
				return null;
			}

			if (string.IsNullOrWhiteSpace(config.Signer))
			{
				Log.Error(ValidationErrorTemplate, "Не указан скрипт подписания");
				return null;
			}

			if (config.FolderKeyMap?.Any() != true)
			{
				Log.Error(ValidationErrorTemplate, "Не указана таблица соответствий подпапок ключам поиска");
				return null;
			}

			return config;
		}

		private static int Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console()
				.CreateLogger()
			;

			try
			{
				Log.Information("Инициализация");

				var configOptions = GetValidOptions();
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
						.WriteTo.File(Environment.ExpandEnvironmentVariables(Path.Combine(configOptions.LogFile, LogFileName)), rollingInterval: RollingInterval.Day)
						.CreateLogger();

					var processor = new Processor(configOptions);
					processor.Process();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Необработанное исключение");
				return 2;
			}
			finally
			{
				Log.CloseAndFlush();
			}

			return 0;
		}
	}
}
