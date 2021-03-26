using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal enum Results
	{
		[JsonPropertyName("pack")]
		Pack,

		[JsonPropertyName("move")]
		Move,

		[JsonPropertyName("move_pack")]
		MovePack,
	}
}
