using Serilog;
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
				Log.Debug(lastLine);
			}

			return lastLine;
		}

		private IEnumerable<string> GetFiles(string path)
		{
			var files = Directory.EnumerateFiles(path, string.Empty, SearchOption.AllDirectories);

			Log.Information("Всего найдено файлов: {Files}", files.Count());

			if (_configOptions.SubfoldersMode == SubfoldersMode.Skip)
			{
				files = Directory.EnumerateFiles(path, string.Empty, SearchOption.TopDirectoryOnly);
			}

			Log.Information("Отобрано согласно {Mode}: {Files}", nameof(SubfoldersMode), files.Count());

			if (_regexPattern != null)
			{
				files = files
					.Where(f => _regexPattern.IsMatch(Path.GetFileName(f)));

				Log.Information("Отобрано согласно {Pattern}: {Files}", nameof(ConfigOptions.SearchPattern), files.Count());
			}

			return files;
		}

		private string PackFiles(string archiveName, params string[] files)
		{
			var destination = Path.Combine(_configOptions.DestinationDirectory, Path.ChangeExtension(archiveName, ".zip"));

			Log.Information("Архивирование: {Destination}", destination);

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
			Log.Information("Подписание");

			var result = ExecuteProcessor(_configOptions.Signer, tokens);
			if (string.IsNullOrWhiteSpace(result))
			{
				throw new SystemException("Скрипт подписания не вернул путь к файлу подписи");
			}

			if (File.Exists(result))
			{
				Log.Information("Файл подписи: {SignedFile}", result);
				return result;
			}

			throw new SystemException("Файл подписи не найден");
		}

		private void ProcessFile(string file, string subfolder, string key)
		{
			Log.Information("Обработка файла: {File}", file);

			var tokens = new Dictionary<string, string>
			{
				{ ArgumentExpander.SourceFileToken, file },
				{ ArgumentExpander.SubfolderToken, subfolder },
				{ ArgumentExpander.KeyToken, key },
			};

			var signedFile = SignFile(tokens);
			var destination = string.Empty;

			Log.Information("Перенос результатов в конечную папку");
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
				Log.Information("Вызов постпроцессора");
				tokens[ArgumentExpander.DestinationFileToken] = destination;
				ExecuteProcessor(_configOptions.PostProcessor, tokens);
			}

			Log.Information("Удаление исходного файла");
			File.Delete(file);

			if (File.Exists(signedFile))
			{
				Log.Information("Удаление подписи");
				File.Delete(signedFile);
			}
		}

		public Processor(ConfigOptions options)
		{
			_configOptions = options;

			if (!string.IsNullOrWhiteSpace(_configOptions.SearchPattern))
			{
				_regexPattern = new Regex(_configOptions.SearchPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);
			}
		}

		public void Process()
		{
			foreach (var subfolder in _configOptions.FolderKeyMap)
			{
				Log.Information("Обработка подпапки: {Subfolder}", subfolder.Key);

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
