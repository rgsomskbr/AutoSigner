using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal record ConfigOptions
	{
		[Required(ErrorMessage = "Не указана исходная папка")]
		public string SourceDirectory { get; init; }

		[Required(ErrorMessage = "Не указана папка назначения")]
		public string DestinationDirectory { get; init; }

		public string SearchPattern { get; init; }

		public SubfoldersMode SubfoldersMode { get; init; } = SubfoldersMode.Parse;

		[Required(ErrorMessage = "Не указан скрипт подписания")]
		public string Signer { get; init; }

		public Results Results { get; init; } = Results.Pack;

		public string PostProcessor { get; init; }

		[Required(ErrorMessage = "Не указана таблица соответствий подпапок ключам поиска")]
		public Dictionary<string, string> FolderKeyMap { get; init; }

		public string ConsoleCodePage { get; init; } = "cp866";
		public string LogFile { get; init; }
	}
}
