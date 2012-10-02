using System;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for InterlinTests.
	/// </summary>
	public class InterlinTests : XWorksTests
	{
		public InterlinTests() : base (InterlinTests.ConfigurationFilePath)
		{
			//
			// TODO: Add constructor logic here
			//
		}

		protected static string ConfigurationFilePath
		{
			get
			{
				return @"g:\ww\distfiles\interlinEd\interlined.xml";
			}
		}
	}
}
