using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace SIL.Utils
{
	public class XslCompiledTransformUtil
	{
		// Singleton
		private static XslCompiledTransformUtil s_instance;
		private XslCompiledTransformUtil() {}
		public static XslCompiledTransformUtil Instance
		{
			get { return s_instance ?? (s_instance = new XslCompiledTransformUtil()); }
		}

		private readonly Dictionary<string, XslCompiledTransform> m_transformCache = new Dictionary<string, XslCompiledTransform>();

		public void TransformXDocumentToFile(string xslPath, XDocument inputDoc, string outputPath, XsltArgumentList argumentList)
		{
			XslCompiledTransform transform = GetTransform(xslPath);
			using (var fileStream = new FileStream(outputPath, FileMode.Create))
			{
				transform.Transform(inputDoc.CreateReader(), argumentList, fileStream);
				fileStream.Close();
			}
		}

		public void TransformFileToFile(string xslPath, string inputPath, string outputPath, XsltArgumentList argumentList)
		{
			XslCompiledTransform transform = GetTransform(xslPath);
			using (var fileStream = new FileStream(outputPath, FileMode.Create))
			{
				transform.Transform(inputPath, argumentList, fileStream);
				fileStream.Close();
			}
		}

		private XslCompiledTransform GetTransform(string xslPath)
		{
			lock(m_transformCache)
			{
				XslCompiledTransform compiledXsl;
				if (m_transformCache.TryGetValue(xslPath, out compiledXsl))
					return compiledXsl;

				var transform = new XslCompiledTransform();
				transform.Load(xslPath, new XsltSettings(true, false), new XmlUrlResolver());
				m_transformCache.Add(xslPath, transform);
				return transform;
			}
		}
	}
}
