// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Program.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Tools.FileCacheMgr
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class Program
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static void Main(string[] args)
		{
			using (FileCacheManager mgr = new FileCacheManager())
			{
				if (args.Length > 0)
				{
					for (int i = 0; args.Length > i; i++)
					{
						if (args[i] == "-s" || args[i] == "/s")
						{
							mgr.DisplayStatistic();
							return;
						}
						else if (args[i] == "-r" || args[i] == "/r")
						{
							mgr.ResetStatistics();
							return;
						}
						else if (args[i] == "-p" || args[i] == "/p" ||
								args[i] == "-rp" || args[i] == "/rp" )
						{
							bool fRemoteCache = (args[i].Substring(1) == "rp");

							i++;
							int nMonths = 0;
							int nDays = 0;
							if (args.Length > i && args[i].Length > 0)
							{
								try
								{
									string arg = args[i];
									int numberLen = char.IsDigit(arg[arg.Length - 1]) ? arg.Length : arg.Length - 1;
									int number = Convert.ToInt32(arg.Substring(0, numberLen));
									switch (arg[arg.Length - 1])
									{
										case 'd':
											nDays = number;
											break;
										case 'w':
											nDays = number * 7;
											break;
										case 'm':
										default:
											nMonths = number;
											break;
									}
								}
								catch (FormatException)
								{
									Console.WriteLine("Illegal number specified as parameter for {0}", fRemoteCache ? "-rp" : "-p");
									return;
								}
							}
							else
								nMonths = 2;
							mgr.PurgeCache(nMonths, nDays, fRemoteCache);
							return;
						}
						else if (args[i] == "-d" || args[i] == "/d")
						{
							mgr.DebugInfo();
							return;
						}
					}
				}

				Console.WriteLine("Usage: ");
				Console.WriteLine("-s\t\tDisplay statistics");
				Console.WriteLine("-r\t\tReset statistics");
				Console.WriteLine("-p [time]\tPurge cached files that haven't " +
					"been accessed for specified time\t\tDefault is 2 months.");
				Console.WriteLine("-rp [time]\tPurge cached files on remote cache.");
				Console.WriteLine("-d\t\tPrint out debug information.");
				Console.WriteLine();
				Console.WriteLine("Time specifier: number[m|w|d]");
				Console.WriteLine("\tExample:\t1w	1 week");
				Console.WriteLine("\t\t\t3d	3 days");
			}
		}
	}
}
