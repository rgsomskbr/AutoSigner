using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal static class Extensions
	{
		public static IDisposable BeginPropScope(this ILogger logger, params (string key, object value)[] properties) =>
			logger.BeginScope(properties.ToDictionary(s => s.key, s => s.value));
	}
}
