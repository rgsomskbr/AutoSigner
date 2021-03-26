using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal static class ArgumentExpander
	{
		public const string SourceFileToken = "%SRC%";
		public const string DestinationFileToken = "%DST%";
		public const string SubfolderToken = "%FLD%";
		public const string KeyToken = "%KEY%";

		private static string ReplaceTokens(string source, string token, string value) =>
			string.Join(value, source.Split(token));

		public static string ExpandArguments(string arguments, IDictionary<string, string> tokens)
		{
			foreach (var token in tokens)
			{
				arguments = ReplaceTokens(arguments, token.Key, token.Value);
			}

			return arguments;
		}

		public static KeyValuePair<string, string> SplitCommandAndArguments(string commandLine)
		{
			var commandLength = commandLine[0] == '"'
				? commandLine.IndexOf("\" ") + 1
				: commandLine.IndexOf(" ");

			return new KeyValuePair<string, string>(
				commandLine.Substring(0, commandLength).Trim(),
				commandLine.Substring(commandLength).Trim());
		}
	}
}
