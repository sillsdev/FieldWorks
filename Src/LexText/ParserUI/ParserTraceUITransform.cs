using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserTraceUITransform
	{
		private readonly XslCompiledTransform m_transform;

		public ParserTraceUITransform(string xslName)
		{
			m_transform = XmlUtils.CreateTransform(xslName, "PresentationTransforms");
		}

		public string Transform(PropertyTable propertyTable, XDocument doc, string baseName)
		{
			return Transform(propertyTable, doc, baseName, new XsltArgumentList());
		}

		public string Transform(PropertyTable propertyTable, XDocument doc, string baseName, XsltArgumentList args)
		{
			var cache = propertyTable.GetValue<FdoCache>("cache");
			SetWritingSystemBasedArguments(cache, propertyTable, args);
			args.AddParam("prmIconPath", "", IconPath);
			string filePath = Path.Combine(Path.GetTempPath(), cache.ProjectId.Name + baseName + ".htm");
			using (var writer = new StreamWriter(filePath))
				m_transform.Transform(doc.CreateNavigator(), args, writer);
			return filePath;
		}

		private void SetWritingSystemBasedArguments(FdoCache cache, PropertyTable propertyTable, XsltArgumentList argumentList)
		{
			ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
			IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
			IWritingSystem defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defAnalWs.Handle, wsf, propertyTable))
			{
				argumentList.AddParam("prmAnalysisFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmAnalysisFontSize", "", myFont.Size + "pt");
			}

			IWritingSystem defVernWs = wsContainer.DefaultVernacularWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defVernWs.Handle, wsf, propertyTable))
			{
				argumentList.AddParam("prmVernacularFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmVernacularFontSize", "", myFont.Size + "pt");
			}

			string sRtl = defVernWs.RightToLeftScript ? "Y" : "N";
			argumentList.AddParam("prmVernacularRTL", "", sRtl);
		}

		private static string TransformPath
		{
			get { return FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse"); }
		}

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
