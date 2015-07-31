// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Responsibility: Bob Eaton

using System;
using EncCnvtrs;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.InteropServices;   // for COMException
using System.Diagnostics;

namespace MTestEC
{
	/// <summary>
	/// Managed test client for EncConverters
	/// </summary>
	class TestEC
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			TestEC TEC = new TestEC();
			TEC.DoNameTest();
			TEC.DoTest();
			TEC.DoDirectXMLAccessTest();
		}

		private EncConverters   m_EC;
		EncConverters GetECs
		{
			// with the 'if' condition commented out, this will get a new ECs for *every*
			//  request. Do this during testing just to see if we can make it crash
			get { /* if( m_EC == null ) */ m_EC = new EncConverters(); return m_EC; }
		}

		// it is not anticipated that clients will need to access the XML file directly,
		//  but if they want to, they can use the mappingRegistry classes defined in the
		//  EncCnvtrs.dll assembly
		public void DoDirectXMLAccessTest()
		{
			mappingRegistry file = EncConverters.GetRepositoryFile();
			foreach(mappingRegistry.encodingRow aEncodingRow in file.encoding)
			{
				MessageBox.Show(String.Format("encoding/@name='{0}' @isUnicode={1}",
					aEncodingRow.name,
					((aEncodingRow.IsisUnicodeNull())? false : aEncodingRow.isUnicode)));
				// etc...
			}
			foreach(mappingRegistry.mappingRow aMappingRow in file.mapping)
			{
				MessageBox.Show(String.Format("mapping/@name='{0}' @leftType={1} @rightType={2}",
					aMappingRow.name,
					aMappingRow.leftType,
					aMappingRow.rightType));
				foreach(mappingRegistry.specRow aSpecRow in aMappingRow.GetspecsRows()[0].GetspecRows())
				{
					MessageBox.Show(String.Format("spec/@type='{0}' @path={1} @direction={2}",
						aSpecRow.type,
						aSpecRow.path,
						aSpecRow.direction));
				}
				// etc...
			}
			foreach(mappingRegistry.fontRow aFontRow in file.font)
			{
				MessageBox.Show(String.Format("font/@name='{0}' @cp={1}",
					aFontRow.name,
					aFontRow.cp));
				// etc...
			}
			foreach(mappingRegistry.implementRow aImplementRow in file.implement)
			{
				MessageBox.Show(String.Format("implement/@type='{0}' @use='{1}' @priority={2}",
					aImplementRow.type,
					aImplementRow.use,
					aImplementRow.priority));
				// etc...
			}
		}

		public void DoTest()
		{
			EncConverters.WriteStorePath(@"C:\Program Files\Common Files\SIL\Converters\Repository" + EncConverters.strDefXmlFilename);    // start from this each time

			GetECs.AddConversionMap("UEC",null,ConvType.Unicode_to_from_Unicode,"SIL.asdgtecForm",
				null,null,ProcessTypeFlags.DontKnow);

			string strMapPath = @"C:\Code\EC\Setup\MapsTables\";

			GetECs.Add("Greek<>Unicode",strMapPath + "SILGreek.tec",ConvType.Legacy_to_from_Unicode,
				null,"Unicode Greek",ProcessTypeFlags.UnicodeEncodingConversion);

			string mapName = "Annapurna", fontName = "Annapurna",
				encodingName = "SIL-ANNAPURNA_05-2002", becomes = "Unicode Devanagari",
				aliasName = "SIL-ANNAPURNA_05";

			// first add TECkit spec
			GetECs.AddConversionMap(mapName, strMapPath + "ann2Unicode.cct",
				ConvType.Legacy_to_Unicode, EncConverters.strTypeSILcc, encodingName, becomes,
				ProcessTypeFlags.UnicodeEncodingConversion);
			Debug.Assert(GetECs[mapName].LeftEncodingID == encodingName);
			Debug.Assert(GetECs[mapName].RightEncodingID == becomes);

			// GetECs.Remove(mapName);

			GetECs.AddAlias(encodingName,aliasName);
			GetECs.AddAlias(aliasName,"asgd");
			try
			{
				// can't add a second alias with the same name
				GetECs.AddAlias(becomes,"asgd");
			}
			catch(COMException e)
			{
				Debug.Assert(e.ErrorCode == (int)ErrStatus.InvalidAliasName);
			}

			// then add a CC spec
			GetECs.AddConversionMap(mapName, strMapPath + "Annapurna.tec",
				ConvType.Legacy_to_from_Unicode, EncConverters.strTypeSILtec, aliasName, becomes,
				ProcessTypeFlags.UnicodeEncodingConversion);
			Debug.Assert(GetECs[mapName].LeftEncodingID == encodingName);
			Debug.Assert(GetECs[mapName].RightEncodingID == becomes);

			// then overwrite the TECkit spec (fixing the unirange
			GetECs.AddConversionMap("Annapurna<>Unicode", strMapPath + "Annapurna.tec",
				ConvType.Legacy_to_from_Unicode, EncConverters.strTypeSILtec, aliasName, becomes,
				ProcessTypeFlags.UnicodeEncodingConversion);

			GetECs.AddFont(fontName, 1252, encodingName);

			ECAttributes aAttributes = GetECs.Attributes(encodingName, AttributeType.EncodingID);

			aAttributes.Add("FontName", fontName);

			aAttributes = GetECs.Attributes(fontName, AttributeType.FontName);
			aAttributes.Add("Keyman Keyboard", "DevRom");

			GetECs.AddImplementation("Encode::Registry","SIL.utr22c","Encode::UTR22",null,-3);
			GetECs.AddImplementation("Encode::Registry",EncConverters.strTypeSILtec,"Encode::TECkit",null,3);
			GetECs.AddImplementation("Encode::Registry","cp","Encode::WinCP",null,5);
			GetECs.AddImplementation("Encode::Registry","Private.dictconv","SIL::DictConv",null,0);

			/*
			GetECs.Clear();

			string strFontName = "Annapurna";
			string strMapName = "Annapurna";
			encodingName = "SIL-ANNAPURNA_05-2002";
			GetECs.AddConversionMap(strMapName, strMapPath + "Annapurna.tec",
				ConvType.Legacy_to_from_Unicode, EncConverters.strTypeSILtec,encodingName,null,
				ProcessTypeFlags.UnicodeEncodingConversion);
			GetECs.AddFont(strFontName, 1252, encodingName);
			GetECs.AddAlias("SIL-ANNAPURNA_05-2002","Annapurna");
			GetECs.AddAlias("SIL-ANNAPURNA_05-2002","Annapurna");  // make sure it doesn't happen twice
			GetECs.AddAlias("SIL-ANNAPURNA_05-2002","SIL-ANNAPURNA_05");
			GetECs.Remove("Annapurna2");
			GetECs.Clear();
			foreach(IEncConverter aEC in GetECs.Values)
			{
				MessageBox.Show(aEC.Name);
			}
			foreach(IEncConverter aEC in GetECs.Values)
			{
				MessageBox.Show(aEC.Name);
			}
			*/

			string strMapName = "IPA93";
			GetECs.AddConversionMap(strMapName,strMapPath + "SIL-IPA93.tec",
				ConvType.Legacy_to_from_Unicode,EncConverters.strTypeSILtec,"SIL-IPA93-2002","Unicode IPA",
				ProcessTypeFlags.UnicodeEncodingConversion);

			string strFontName = "SILDoulous IPA93";
			encodingName = "SIL-IPA93-2002";
			GetECs.AddFont(strFontName, 42, encodingName);

			strFontName = "SILManuscript IPA93";
			GetECs.AddFont(strFontName, 42, encodingName);

			strFontName = "SILSophia IPA93";
			GetECs.AddFont(strFontName, 42, encodingName);

			strMapName = "UniDevanagri<>UniIPA";
			GetECs.AddConversionMap(strMapName,strMapPath + "UDev2UIpa.tec",
				ConvType.Unicode_to_from_Unicode, EncConverters.strTypeSILtec,becomes,"Unicode IPA",
				ProcessTypeFlags.Transliteration);

			strFontName = "Doulous SIL";
			GetECs.AddUnicodeFontEncoding(strFontName, "Unicode IPA");
			GetECs.AddUnicodeFontEncoding(strFontName, "Unicode Greek");

			GetECs.AddFontMapping("IPA93","SILDoulous IPA93","Doulous SIL");

			GetECs.AddConversionMap("Annapurna<>IPA93",null,ConvType.Legacy_to_from_Legacy,
				EncConverters.strTypeSILcomp,null,null,ProcessTypeFlags.Transliteration);

			GetECs.AddCompoundConverterStep("Annapurna<>IPA93","Annapurna",true,NormalizeFlags.None);
			GetECs.AddCompoundConverterStep("Annapurna<>IPA93",strMapName,true,NormalizeFlags.FullyComposed);
			GetECs.AddCompoundConverterStep("Annapurna<>IPA93","IPA93",false,NormalizeFlags.FullyDecomposed);

			strMapName = "Devanagri<>Latin(ICU)";
			GetECs.AddConversionMap(strMapName,"Devanagari-Latin", ConvType.Unicode_to_from_Unicode,
				"ICU.trans","Unicode Devanagari",null,ProcessTypeFlags.ICUTransliteration);

			strMapName = "UTF-8<>UTF-16";
			GetECs.Add(strMapName,"UTF-8",ConvType.Unicode_to_from_Unicode,
				null,null,ProcessTypeFlags.ICUConverter);

			GetECs.Add(strMapName,"65001",ConvType.Unicode_to_from_Unicode,
				null,null,ProcessTypeFlags.CodePageConversion);

			GetECs.AddConversionMap(strMapName,"EncodingFormConversionRequest",
				ConvType.Unicode_to_from_Unicode,EncConverters.strTypeSILtecForm,null,null,
				ProcessTypeFlags.DontKnow);

/*
			foreach(string aConverterName in GetECs.EnumByProcessType(ProcessTypeFlags.ICUConverter) )
			{
				MessageBox.Show(aConverterName);
			}

			EncConverters aNewECs = GetECs.ByEncodingID("SIL-ANNAPURNA_05-2002",ProcessTypeFlags.UnicodeEncodingConversion);
			foreach(IEncConverter aEC in aNewECs.Values)
			{
				MessageBox.Show(aEC.Name);
			}
			aNewECs = GetECs.ByEncodingID("SIL-ANNAPURNA_05-2002",ProcessTypeFlags.Transliteration);
			foreach(IEncConverter aEC in aNewECs.Values)
			{
				MessageBox.Show(aEC.Name);
			}

			aNewECs = GetECs.ByFontName("SILDoulous IPA93", ProcessTypeFlags.DontKnow);
			foreach(IEncConverter aEC in aNewECs.Values)
			{
				MessageBox.Show(aEC.Name);
			}
*/
			try
			{
				GetECs.RemoveAlias("SIL-ANNAPURNA_05");
			}
			catch(COMException e)
			{
				MessageBox.Show(e.Message);
			}
			try
			{
				// this *should* fail because we've already removed it by the alias name
				GetECs.Remove("Annapurna");
			}
			// what isn't this working???
			catch(COMException e)
			{
				MessageBox.Show(e.Message);
				// System.Diagnostics.Debug.Assert(e.);
				// Assert(e == ErrStatus.NoAliasName);
			}
			/*
			ECAttributes aAttrs = GetECs.Attributes(strMapName,AttributeType.Converter);
			aAttrs.Add("test attribute","test value");
			aAttrs.Remove("Copyright");
			aAttrs.AddNonPersist("non-persist add", "test");
			aAttrs.RemoveNonPersist("TECkitCompilerVersion");
			foreach(DictionaryEntry aAttr in aAttrs)
			{
				MessageBox.Show("Property: " + aAttr.Key + " = " + aAttr.Value);
			}

			GetECs.Remove(strMapName);
*/
		}
		// this method will do a series of tests for the 'name' of a collection item. Basically
		//  as long as there is only a single 'spec' for a 'mapping', then the 'mapping' is
		//  the item's name (e.g. "utf-8"). If there are multiple specs that do the same
		//  mapping, then there'll be multiple collection items--one for each spec. And the
		//  name of each item will be the 'mapping' plus the implementation type identifier
		//  (e.g. "utf-8 (SIL.cp)")
		public void DoNameTest()
		{
			EncConverters.WriteStorePath(@"C:\Program Files\Common Files\SIL\Converters\Repository\ECRepository.xml");    // start from this each time

			string mappingName = "schmaboogle";
			string strConverterCp = GetECs.BuildConverterSpecName(mappingName,EncConverters.strTypeSILcp);
			string strConverterIcu = GetECs.BuildConverterSpecName(mappingName,"ICU.conv");
			string strConverterTec = GetECs.BuildConverterSpecName(mappingName,EncConverters.strTypeSILtecForm);

			GetECs.Clear();

			AddCp(mappingName);
			Debug.Assert(GetECs.Count == 1);
			Debug.Assert(GetECs[0].Name == mappingName);

			try
			{
				// this should fail because it is doesn't work to use the combined name unless
				//  there are two or more specs present
				GetECs.Remove(strConverterCp);
				Debug.Assert(false);    // it should throw
			}
			catch(COMException e)
			{
				Debug.Assert(e.ErrorCode == (int)ErrStatus.NoConverter);
			}

			GetECs.Remove(GetECs[0].Name);
			Debug.Assert(GetECs.Count == 0);

			AddCp(mappingName);
			AddIcu(mappingName);
			Debug.Assert(GetECs.Count == 2);
			Debug.Assert(   (GetECs[0].Name == strConverterCp)
				||  (GetECs[0].Name == strConverterIcu) );
			Debug.Assert(   (GetECs[1].Name == strConverterCp)
				||  (GetECs[1].Name == strConverterIcu) );

			GetECs.Remove(mappingName);   // removes *all* specs
			Debug.Assert(GetECs.Count == 0);

			AddCp(mappingName);
			AddIcu(mappingName);
			// verify we can get the highest priority one (cp) by the mapping name
			Debug.Assert(   (GetECs[mappingName] != null)
				&&  (GetECs[mappingName].Name == strConverterCp) );
			// and also by their individual names
			Debug.Assert(   (GetECs[strConverterIcu] != null)
				&&  (GetECs[strConverterIcu].Name == strConverterIcu) );
			Debug.Assert(   (GetECs[strConverterCp] != null)
				&&  (GetECs[strConverterCp].Name == strConverterCp) );

			GetECs.Remove(strConverterCp);      // should only remove one.
			Debug.Assert(GetECs.Count == 1);
			Debug.Assert(GetECs[0].Name == mappingName);   // the remaining one should have reverted
			Debug.Assert(GetECs[0].ImplementType == "ICU.conv");    // at this type
			GetECs.Remove(mappingName);
			Debug.Assert(GetECs.Count == 0);

			// repeat, removing the other one by name
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterIcu);      // should only remove one.
			Debug.Assert(GetECs.Count == 1);
			Debug.Assert(GetECs[0].Name == mappingName);
			Debug.Assert(GetECs[0].ImplementType == EncConverters.strTypeSILcp);    // at this type
			GetECs.Remove(mappingName);
			Debug.Assert(GetECs.Count == 0);

			// now do three
			//  first in order of priority
			AddIcu(mappingName);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			AddTec(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterTec);
			AddCp(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(mappingName);

			// now, mixed up (adding a lower priority shouldn't change the mappingName'd index)
			AddTec(mappingName);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			AddIcu(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterTec);
			AddCp(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(mappingName);

			// now, mixed up (but in reverse priority order)
			AddTec(mappingName);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			AddCp(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			AddIcu(mappingName);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(mappingName);

			// now, make sure we can get them by their combined names as well.
			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			Debug.Assert(GetECs[strConverterCp].Name == strConverterCp);
			Debug.Assert(GetECs[strConverterTec].Name == strConverterTec);
			Debug.Assert(GetECs[strConverterIcu].Name == strConverterIcu);
			GetECs.Remove(mappingName);

			// add three and try to remove them one at a time, in different orders
			// remove in reverse priority (so the new highest keeps adjusting)
			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterCp);
			Debug.Assert(GetECs[mappingName].Name == strConverterTec);
			GetECs.Remove(strConverterTec);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == "ICU.conv");
			GetECs.Remove(mappingName);

			// remove in increasing priority (so the new highest doesn't change)
			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterCp);
			Debug.Assert(GetECs[mappingName].Name == strConverterTec);
			GetECs.Remove(strConverterIcu);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == EncConverters.strTypeSILtecForm);
			GetECs.Remove(mappingName);

			// remove in increasing priority (so the new highest doesn't change)
			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterIcu);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(strConverterTec);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == EncConverters.strTypeSILcp);
			GetECs.Remove(mappingName);

			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterIcu);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(strConverterCp);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == EncConverters.strTypeSILtecForm);
			GetECs.Remove(mappingName);

			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterTec);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(strConverterIcu);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == EncConverters.strTypeSILcp);
			GetECs.Remove(mappingName);

			// final permutation
			AddTec(mappingName);
			AddCp(mappingName);
			AddIcu(mappingName);
			GetECs.Remove(strConverterTec);
			Debug.Assert(GetECs[mappingName].Name == strConverterCp);
			GetECs.Remove(strConverterCp);
			Debug.Assert(GetECs[mappingName].Name == mappingName);
			Debug.Assert(GetECs[mappingName].ImplementType == "ICU.conv");
			GetECs.Remove(mappingName);
		}
		// add a code page converter for testing
		void AddCp(string mappingName)
		{
			GetECs.Add(mappingName,"65001",ConvType.Unicode_to_from_Unicode,null,null,ProcessTypeFlags.CodePageConversion);
		}

		// add a TECkit converter (same mapping) for testing
		void AddTec(string mappingName)
		{
			GetECs.AddConversionMap(mappingName,null,ConvType.Unicode_to_from_Unicode,EncConverters.strTypeSILtecForm,null,null,ProcessTypeFlags.DontKnow);
		}

		// add an ICU converter (same mapping) for testing
		void AddIcu(string mappingName)
		{
			GetECs.Add(mappingName,"utf-8",ConvType.Unicode_to_from_Unicode,null,null,ProcessTypeFlags.ICUConverter);
		}
	}
}
