// Copyright (c) 2015 SIL International
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
		[Required]
		public string SourceFile { get; set; }

		public override bool Execute()
		{
			try
			{
				var hasher = HashAlgorithm.Create("MD5");
				byte[] checksum;
				using (var file = File.OpenRead(SourceFile))
				{
					checksum = hasher.ComputeHash(file);
				}
				var bldr = new StringBuilder();
				for ( int i=0; i < checksum.Length; i++ )
				{
					bldr.Append(String.Format("{0:x2}", checksum[i]));
				}
				var outputFile = SourceFile + ".MD5";
				using (var writer = new StreamWriter(outputFile))
				{
					writer.Write(bldr.ToString());
				}
				return true;
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
				return false;
			}
		}
	}
}
