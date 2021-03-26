using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal class Processor
	{
		private readonly ConfigOptions _configOptions;
		private readonly Regex _regexPattern;
		private readonly ILogger<Processor> _logger;

		private string ExecuteProcessor(string commandLine, IDictionary<string, string> tokens)
		{
			var arguments = ArgumentExpander.ExpandArguments(commandLine, tokens);
			var command = ArgumentExpander.SplitCommandAndArguments(arguments);
			var startInfo = new ProcessStartInfo
			{
				FileName = command.Key,
				Arguments = command.Value,
				WorkingDirectory = Path.GetDirectoryName(command.Key),
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.GetEncoding(_configOptions.ConsoleCodePage),
				StandardErrorEncoding = Encoding.GetEncoding(_configOptions.ConsoleCodePage),
			};

			using var process = System.Diagnostics.Process.Start(startInfo);
			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				var error = process.StandardError.ReadToEnd();
				throw new SystemException($"Ошибка обработки:{Environment.NewLine}{error}");
			}

			var lastLine = string.Empty;
			while (!process.StandardOutput.EndOfStream)
			{
				lastLine = process.StandardOutput.ReadLine();
				_logger.LogDebug(lastLine);
			}

			return lastLine;
		}

		private IEnumerable<string> GetFiles(string path)
		{
			var files = Directory.EnumerateFiles(path, string.Empty, SearchOption.AllDirectories);

			_logger.LogInformation("Всего найдено файлов: {Files}", files.Count());

			if (_configOptions.SubfoldersMode == SubfoldersMode.Skip)
			{
				files = Directory.EnumerateFiles(path, string.Empty, SearchOption.TopDirectoryOnly);
			}

			_logger.LogInformation("Отобрано согласно {Mode}: {Files}", nameof(SubfoldersMode), files.Count());

			if (_regexPattern != null)
			{
				files = files
					.Where(f => _regexPattern.IsMatch(Path.GetFileName(f)));

				_logger.LogInformation("Отобрано согласно {Pattern}: {Files}", nameof(ConfigOptions.SearchPattern), files.Count());
			}

			return files;
		}

		private string PackFiles(string archiveName, params string[] files)
		{
			var destination = Path.Combine(_configOptions.DestinationDirectory, Path.ChangeExtension(archiveName, ".zip"));

			_logger.LogInformation("Архивирование: {Destination}", destination);

			using var stream = File.Create(destination);
			using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

			foreach (var file in files)
			{
				archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
			}

			return destination;
		}

		private string SignFile(IDictionary<string, string> tokens)
		{
			_logger.LogInformation("Подписание");

			var result = ExecuteProcessor(_configOptions.Signer, tokens);
			if (string.IsNullOrWhiteSpace(result))
			{
				throw new SystemException("Скрипт подписания не вернул путь к файлу подписи");
			}

			if (File.Exists(result))
			{
				_logger.LogInformation("Файл подписи: {SignedFile}", result);
				return result;
			}

			throw new SystemException("Файл подписи не найден");
		}

		private void ProcessFile(string file, string subfolder, string key)
		{
			_logger.LogInformation("Обработка файла: {File}", file);

			var tokens = new Dictionary<string, string>
			{
				{ ArgumentExpander.SourceFileToken, file },
				{ ArgumentExpander.SubfolderToken, subfolder },
				{ ArgumentExpander.KeyToken, key },
			};

			var signedFile = SignFile(tokens);
			var destination = string.Empty;

			_logger.LogInformation("Перенос результатов в конечную папку");
			switch (_configOptions.Results)
			{
				case Results.Pack:
					destination = PackFiles(Path.GetFileName(file), file, signedFile);
					break;

				case Results.Move:
					destination = Path.Combine(_configOptions.DestinationDirectory, Path.GetFileName(signedFile));
					File.Move(signedFile, destination, true);

					break;

				case Results.MovePack:
					destination = PackFiles(Path.GetFileName(file), signedFile);
					break;
			}

			if (!string.IsNullOrWhiteSpace(_configOptions.PostProcessor))
			{
				_logger.LogInformation("Вызов постпроцессора");
				tokens[ArgumentExpander.DestinationFileToken] = destination;
				ExecuteProcessor(_configOptions.PostProcessor, tokens);
			}

			_logger.LogInformation("Удаление исходного файла");
			File.Delete(file);

			if (File.Exists(signedFile))
			{
				_logger.LogInformation("Удаление подписи");
				File.Delete(signedFile);
			}
		}

		public Processor(
			IOptions<ConfigOptions> options,
			ILogger<Processor> logger)
		{
			_configOptions = options.Value;
			_logger = logger;

			if (!string.IsNullOrWhiteSpace(_configOptions.SearchPattern))
			{
				_regexPattern = new Regex(_configOptions.SearchPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);
			}
		}

		public void Process()
		{
			foreach (var subfolder in _configOptions.FolderKeyMap)
			{
				_logger.LogInformation("Обработка подпапки: {Subfolder}", subfolder.Key);

				var subfolderPath = Path.Combine(_configOptions.SourceDirectory, subfolder.Key);
				var files = GetFiles(subfolderPath);
				if (files.Any())
				{
					foreach (var file in files)
					{
						ProcessFile(file, subfolder.Key, subfolder.Value);
					}
				}
			}
		}
	}
}
