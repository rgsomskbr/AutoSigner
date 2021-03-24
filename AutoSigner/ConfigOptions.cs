using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal class ConfigOptions
	{
		[Required(ErrorMessage = "Не указана исходная папка")]
		public string SourceDirectory { get; set; }

		[Required(ErrorMessage = "Не указана папка назначения")]
		public string DestinationDirectory { get; set; }

		public string SearchPattern { get; set; } = "*";

		[Required(ErrorMessage = "Не указан метод обработки файлов")]
		public FilesMode FilesMode { get; set; }

		[Required(ErrorMessage = "Не указан метод обработки подпапок")]
		public SubfoldersMode SubfoldersMode { get; set; }

		[Required(ErrorMessage = "Не указан скрипт подписания")]
		public string Signer { get; set; }

		public string PreProcessor { get; set; }
		public string PostProcessor { get; set; }
		public string ExternalArchiver { get; set; }

		[Required(ErrorMessage = "Не указана таблица соответствий подпапок ключам поиска")]
		public Dictionary<string, string> FolderKeyMap { get; set; }

		public string LogFile { get; set; }
	}
}
