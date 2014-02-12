using System.Collections.Generic;
using System.IO;
using System.Xml;
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
		protected XmlDocument m_parseResult;

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

		protected string TransformToHtml(string sInputFile, string sTempFileBase, string sTransformFile, List<XmlUtils.XSLParameter> args)
		{
			string sOutput = CreateTempFile(sTempFileBase, "htm");
			string sTransform = Path.Combine(TransformPath, sTransformFile);
			SetWritingSystemBasedArguments(args);
			AddParserSpecificArguments(args);
			XmlUtils.TransformFileToFile(sTransform, args.ToArray(), sInputFile, sOutput);
			return sOutput;
		}

		private void SetWritingSystemBasedArguments(List<XmlUtils.XSLParameter> args)
		{
			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			IWritingSystem defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defAnalWs.Handle, m_mediator, wsf))
			{
				args.Add(new XmlUtils.XSLParameter("prmAnalysisFont", myFont.FontFamily.Name));
				args.Add(new XmlUtils.XSLParameter("prmAnalysisFontSize", myFont.Size + "pt"));
			}

			IWritingSystem defVernWs = wsContainer.DefaultVernacularWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defVernWs.Handle, m_mediator, wsf))
			{
				args.Add(new XmlUtils.XSLParameter("prmVernacularFont", myFont.FontFamily.Name));
				args.Add(new XmlUtils.XSLParameter("prmVernacularFontSize", myFont.Size + "pt"));
			}

			string sRtl = defVernWs.RightToLeftScript ? "Y" : "N";
			args.Add(new XmlUtils.XSLParameter("prmVernacularRTL", sRtl));
		}

		protected virtual void AddParserSpecificArguments(List<XmlUtils.XSLParameter> args)
		{
			// default is to do nothing
		}
	}
}
