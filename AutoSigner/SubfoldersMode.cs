﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoSigner
{
	internal enum SubfoldersMode
	{
		[JsonPropertyName("parse")]
		Parse,

		[JsonPropertyName("skip")]
		Skip,
	}
}
