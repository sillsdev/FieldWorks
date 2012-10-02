// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SampleApp.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace XCore
{
	/// <summary>
	/// Summary description for SampleApp.
	/// </summary>
	public class SampleApp
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SampleApp"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SampleApp()
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public Form NewMainWindow()
		{
			XWindow form = new XWindow();
			ImageHolder holder = new ImageHolder ();

			//noticed that we only have one size of image, so we are sticking it in both the small and the large lists
			form.AddLargeImageList(holder.miscImages, new string[]{"chat", "wheelchair", "food", "basinet", "submit"});
			form.AddLargeImageList(holder.airportImages, new string[]{"defaultLocation","USA", "Italy"});
			form.AddSmallImageList(holder.miscImages, new string[]{"chat", "wheelchair", "food", "basinet", "submit"});
			form.AddSmallImageList(holder.airportImages, new string[]{"defaultLocation","USA", "Italy"});
			form.LoadUI(ConfigurationPath);//Argument("x"));
			form.Show();
			return form;
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			//			if(rgArgs.Length == 0)
			//				rgArgs = new string[]{"-t", "TEST", "-x",
			//										 ConfigurationPath};
			//			return SampleApp.Go(rgArgs);
			SampleApp app = new SampleApp();
			Application.Run(app.NewMainWindow());

			return 0;
		}


		protected static string ConfigurationPath
		{
			get
			{
				string asmPathname = Assembly.GetExecutingAssembly().CodeBase;
				asmPathname = SIL.Utils.FileUtils.StripFilePrefix(asmPathname);
				string asmPath = asmPathname.Substring(0, asmPathname.LastIndexOf("/"));
				return System.IO.Path.Combine(asmPath, "itinerary.xml");
			}
		}
		#region Construction and Initializing

		#endregion // Construction and Initializing
	}
}
