// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class Md5Checksum : Task
	{
		private static readonly HashAlgorithm Hasher = HashAlgorithm.Create("MD5");

		[Required]
		public string SourceFile { get; set; }

		public override bool Execute()
		{
			try
			{
				var outputFile = SourceFile + ".MD5";
				using (var writer = new StreamWriter(outputFile))
				{
					writer.Write(Compute(SourceFile));
				}
				return true;
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
				return false;
			}
		}

		public static string Compute(string filename)
		{
			byte[] checksumBytes;
			using (var file = File.OpenRead(filename))
			{
				checksumBytes = Hasher.ComputeHash(file);
			}
			var bldr = new StringBuilder();
			foreach (var b in checksumBytes)
			{
				bldr.AppendFormat("{0:x2}", b);
			}

			return bldr.ToString();
		}
	}
}
