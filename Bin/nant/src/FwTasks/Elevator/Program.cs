using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Elevator
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1)
				return;

			Process process = new Process();
			process.StartInfo.FileName = args[0];
			process.StartInfo.CreateNoWindow = true;
			StringBuilder bldr = new StringBuilder();
			for (int i = 1; i < args.Length; i++)
			{
				bldr.Append(args[i]);
				bldr.Append(" ");
			}
			process.StartInfo.Arguments = bldr.ToString();
			process.StartInfo.Verb = "runas";
			process.Start();
			process.WaitForExit();
		}
	}
}
