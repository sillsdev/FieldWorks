// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IcuWrappersTests.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests ICU wrapper
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IcuWrappersTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsSymbol method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsSymbol()
		{
			Assert.IsFalse(Icu.IsSymbol('#'));
			Assert.IsFalse(Icu.IsSymbol('a'));
			Assert.IsTrue(Icu.IsSymbol('$'));
			Assert.IsTrue(Icu.IsSymbol('+'));
			Assert.IsTrue(Icu.IsSymbol('`'));
			Assert.IsTrue(Icu.IsSymbol(0x0385));
			Assert.IsTrue(Icu.IsSymbol(0x0B70));
		}

		/// <summary>
		/// Can't easily check the correctness, but make sure we can at least get this.
		/// </summary>
		[Test]
		public void CanGetUnicodeVersion()
		{
			var result = Icu.UnicodeVersion;
			Assert.That(result.Length >= 3);
			Assert.That(result.IndexOf("."), Is.GreaterThan(0));
			int major;
			Assert.True(int.TryParse(result.Substring(0, result.IndexOf(".")), out major));
			Assert.That(major >= 6);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFC, normalize to NFC
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFC2NFC()
		{
			var normalizedString = Icu.Normalize("tést", Icu.UNormalizationMode.UNORM_NFC);
			Assert.AreEqual("tést", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormC));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFC, normalize to NFD
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFC2NFD()
		{
			var normalizedString = Icu.Normalize("tést", Icu.UNormalizationMode.UNORM_NFD);
			var i=0;
			foreach (var c in normalizedString.ToCharArray())
				Console.WriteLine("pos {0}: {1} ({1:x})", i++, c);
			Assert.AreEqual(0x0301, normalizedString[2]);
			Assert.AreEqual("te\u0301st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormD));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFD, normalize to NFC
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFD2NFC()
		{
			var normalizedString = Icu.Normalize("te\u0301st", Icu.UNormalizationMode.UNORM_NFC);
			Assert.AreEqual("tést", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormC));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFD, normalize to NFD
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFD2NFD()
		{
			var normalizedString = Icu.Normalize("te\u0301st", Icu.UNormalizationMode.UNORM_NFD);
			Assert.AreEqual("te\u0301st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormD));
		}
	}
}
