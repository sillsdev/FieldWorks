using System.IO;
using System.Xml.Linq;
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
	public abstract class ParserTraceBase
	{
		/// <summary>
		/// xCore Mediator.
		/// </summary>
		protected Mediator m_mediator;

		protected FdoCache m_cache;
		protected string m_sDataBaseName = "";
		/// <summary>
		/// The parse result xml document
		/// </summary>
		protected XDocument m_parseResult;

		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected ParserTraceBase(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_sDataBaseName = m_cache.ProjectId.Name;
		}

		protected string CreateTempFile(string sPrefix, string sExtension)
		{
			string sTempFileName = Path.Combine(Path.GetTempPath(), m_sDataBaseName + sPrefix) + "." + sExtension;
			using (StreamWriter sw = File.CreateText(sTempFileName))
				sw.Close();
			return sTempFileName;
		}

		/// <summary>
		/// Path to transforms
		/// </summary>
		protected string TransformPath
		{
			get { return FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse"); }
		}

		protected string TransformToHtml(string inputPath, string tempFileBase, string transformFile, XsltArgumentList argumentList)
		{
			string outputPath = CreateTempFile(tempFileBase, "htm");
			string transformPath = Path.Combine(TransformPath, transformFile);
			SetWritingSystemBasedArguments(argumentList);
			AddParserSpecificArguments(argumentList);
			XslCompiledTransformUtil.Instance.TransformFileToFile(transformPath, inputPath, outputPath, argumentList);
			return outputPath;
		}

		protected string TransformToHtml(XDocument inputDoc, string tempFileBase, string transformFile, XsltArgumentList argumentList)
		{
			string outputPath = CreateTempFile(tempFileBase, "htm");
			string transformPath = Path.Combine(TransformPath, transformFile);
			SetWritingSystemBasedArguments(argumentList);
			AddParserSpecificArguments(argumentList);
			XslCompiledTransformUtil.Instance.TransformXDocumentToFile(transformPath, inputDoc, outputPath, argumentList);
			return outputPath;
		}

		private void SetWritingSystemBasedArguments(XsltArgumentList argumentList)
		{
			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			IWritingSystem defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defAnalWs.Handle, m_mediator, wsf))
			{
				argumentList.AddParam("prmAnalysisFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmAnalysisFontSize", "", myFont.Size + "pt");
			}

			IWritingSystem defVernWs = wsContainer.DefaultVernacularWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defVernWs.Handle, m_mediator, wsf))
			{
				argumentList.AddParam("prmVernacularFont", "", myFont.FontFamily.Name);
				argumentList.AddParam("prmVernacularFontSize", "", myFont.Size + "pt");
			}

			string sRtl = defVernWs.RightToLeftScript ? "Y" : "N";
			argumentList.AddParam("prmVernacularRTL", "", sRtl);
		}

		protected virtual void AddParserSpecificArguments(XsltArgumentList argumentList)
		{
			// default is to do nothing
		}
	}
}
