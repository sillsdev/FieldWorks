// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords
{
	public class ParserTraceUITransform
	{
		private readonly XslCompiledTransform m_transform;

		public ParserTraceUITransform(string xslName)
		{
			m_transform = M3ToXAmpleTransformer.CreateTransform(xslName, "PresentationTransforms");
		}

		public string Transform(IPropertyTable propertyTable, XDocument doc, string baseName)
		{
			return Transform(propertyTable, doc, baseName, new XsltArgumentList());
		}

		public string Transform(IPropertyTable propertyTable, XDocument doc, string baseName, XsltArgumentList args)
		{
			var cache = propertyTable.GetValue<LcmCache>(FwUtils.cache);
			SetWritingSystemBasedArguments(cache, propertyTable, args);
			args.AddParam("prmIconPath", "", IconPath);
			var filePath = Path.Combine(Path.GetTempPath(), cache.ProjectId.Name + baseName + ".htm");
			using (var writer = new StreamWriter(filePath))
			{
				m_transform.Transform(doc.CreateNavigator(), args, writer);
			}
			return filePath;
		}

		private static void SetWritingSystemBasedArguments(LcmCache cache, IPropertyTable propertyTable, XsltArgumentList argumentList)
		{
			var wsf = cache.WritingSystemFactory;
			var wsContainer = cache.ServiceLocator.WritingSystems;
			var defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defAnalWs.Handle, wsf, propertyTable))
			{
				argumentList.AddParam("prmAnalysisFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmAnalysisFontSize", "", myFont.Size + "pt");
			}
			var defVernWs = wsContainer.DefaultVernacularWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defVernWs.Handle, wsf, propertyTable))
			{
				argumentList.AddParam("prmVernacularFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmVernacularFontSize", "", myFont.Size + "pt");
			}
			argumentList.AddParam("prmVernacularRTL", "", defVernWs.RightToLeftScript ? "Y" : "N");
		}

		private static string TransformPath => FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse");

		private static string IconPath
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append("file:///");
				sb.Append(TransformPath.Replace(@"\", "/"));
				sb.Append("/");
				return sb.ToString();
			}
		}
	}
}