using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Db4objects;
using Db4objects.Db4o;
using Db4objects.Db4o.Config.Encoding;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.FDO.DomainServices;
using Db4objects.Db4o.Query;
using Db4objects.Db4o.Typehandlers;
using Db4objects.Db4o.Reflect;
using Db4objects.Db4o.Internal.Handlers;
using Db4objects.Db4o.Marshall;
using Db4objects.Db4o.Internal.Marshall;
using Db4objects.Db4o.Internal;
using Db4objects.Db4o.Linq;

namespace FDOBrowser
{


	internal class Db4oToXmlConverter
	{

		internal static CmObjectSurrogateTypeHandler CMOSTHandler = new CmObjectSurrogateTypeHandler();

	// extracted from the CellarPropertyType enum
		protected static string GetFlidTypeAsString(CellarPropertyType flidType)
		{
			string retval;
			switch (flidType)
			{
				default:
					throw new ArgumentException("Property element name not recognized.");
				case CellarPropertyType.Boolean:
					retval = "Boolean";
					break;
				case CellarPropertyType.Integer:
					retval = "Integer";
					break;
				case CellarPropertyType.Time:
					retval = "Time";
					break;
				case CellarPropertyType.String:
					retval = "String";
					break;
				case CellarPropertyType.MultiString:
					retval = "MultiString";
					break;
				case CellarPropertyType.MultiBigString:
					retval = "MultiBigString";
					break;
				case CellarPropertyType.Unicode:
					retval = "Unicode";
					break;
				case CellarPropertyType.MultiUnicode:
					retval = "MultiUnicode";
					break;
				case CellarPropertyType.Guid:
					retval = "Guid";
					break;
				case CellarPropertyType.Image:
					retval = "Image";
					break;
				case CellarPropertyType.GenDate:
					retval = "GenDate";
					break;
				case CellarPropertyType.Binary:
					retval = "Binary";
					break;
				case CellarPropertyType.Numeric:
					retval = "Numeric";
					break;
				case CellarPropertyType.Float:
					retval = "Float";
					break;
				case CellarPropertyType.OwningAtomic:
					retval = "OA";
					break;
				case CellarPropertyType.OwningCollection:
					retval = "OC";
					break;
				case CellarPropertyType.OwningSequence:
					retval = "OS";
					break;
				case CellarPropertyType.ReferenceAtomic:
					retval = "RA";
					break;
				case CellarPropertyType.ReferenceCollection:
					retval = "RC";
					break;
				case CellarPropertyType.ReferenceSequence:
					retval = "RS";
					break;
			}
			return retval;
		}

	// Just copied from the db4o settings used in Flex
		internal static void doConfig(Db4objects.Db4o.Config.IEmbeddedConfiguration config)
		{
			config.Common.StringEncoding = StringEncodings.Utf8();
			config.Common.RegisterTypeHandler(
				new CustomFieldInfoTypeHandlerPredicate(),
				new CustomFieldInfoTypeHandler());
			config.Common.RegisterTypeHandler(
				new ModelVersionNumberTypeHandlerPredicate(),
				new ModelVersionNumberTypeHandler());
			config.Common.RegisterTypeHandler(
							new CmObjectSurrogateTypeHandlerPredicate(),
							 CMOSTHandler);
			config.Common.Callbacks = false;

			config.Common.WeakReferences = false;
			config.Common.CallConstructors = false;

			config.Common.ActivationDepth = 2;
			config.Common.UpdateDepth = 2;
			config.Common.DetectSchemaChanges = true;
			config.Common.TestConstructors = false;
			config.File.BlockSize = 8;
			config.Common.BTreeNodeSize = 50;config.Common.StringEncoding = StringEncodings.Utf8();

			config.Common.Queries.EvaluationMode(Db4objects.Db4o.Config.QueryEvaluationMode.Snapshot);

			var type = typeof(CmObjectSurrogate);
			config.Common.ObjectClass(type).CascadeOnDelete(true);
			config.Common.ObjectClass(type).UpdateDepth(2);
			config.Common.ObjectClass(type).MinimumActivationDepth(2);
			config.Common.ObjectClass(type).MaximumActivationDepth(2);


			type = typeof(CustomFieldInfo);
			config.Common.ObjectClass(type).CascadeOnDelete(true);
			config.Common.ObjectClass(type).UpdateDepth(2);
			config.Common.ObjectClass(type).MinimumActivationDepth(2);
			config.Common.ObjectClass(type).MaximumActivationDepth(2);

			type = typeof(ModelVersionNumber);
			config.Common.ObjectClass(type).CascadeOnDelete(true);
			config.Common.ObjectClass(type).UpdateDepth(2);
			config.Common.ObjectClass(type).MinimumActivationDepth(2);
			config.Common.ObjectClass(type).MaximumActivationDepth(2);



		}

	// Just gets the version from the opened db40 file
		internal static int GetModelVersionNumber(IEmbeddedObjectContainer db)
		{
		   var result =  db.Query<ModelVersionNumber>();
		   if (result.Count > 0)
			  return result[0].m_modelVersionNumber;
		   else
			   return 0;
		}


		internal static void GetAndWriteCustomFieldInfo(IEmbeddedObjectContainer db, XmlWriter writer)
		{
			var customfields = new List<CustomFieldInfo>();
			foreach (CustomFieldInfo cfi in db.Query<CustomFieldInfo>().ToArray())
			{
				customfields.Add(cfi);

			}
			if (customfields.Count > 0)
			{
				writer.WriteStartElement("AdditionalFields");
				foreach (CustomFieldInfo customFieldInfo in customfields)
				{
					writer.WriteStartElement("CustomField");
					writer.WriteAttributeString("name", customFieldInfo.m_fieldname);
					writer.WriteAttributeString("class", customFieldInfo.m_classname);
					if (customFieldInfo.m_destinationClass != 0)
						writer.WriteAttributeString("destclass", customFieldInfo.m_destinationClass.ToString());
					writer.WriteAttributeString("type", GetFlidTypeAsString(customFieldInfo.m_fieldType));
					if (customFieldInfo.m_fieldWs != 0)
						writer.WriteAttributeString("wsSelector", customFieldInfo.m_fieldWs.ToString());
					if (!String.IsNullOrEmpty(customFieldInfo.m_fieldHelp))
						writer.WriteAttributeString("helpString", customFieldInfo.m_fieldHelp);
					if (customFieldInfo.m_fieldListRoot != Guid.Empty)
						writer.WriteAttributeString("listRoot", customFieldInfo.m_fieldListRoot.ToString());
					if (customFieldInfo.Label != customFieldInfo.m_fieldname)
						writer.WriteAttributeString("label", customFieldInfo.Label);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
		}
		internal static void GetAndWriteSurrogates(IEmbeddedObjectContainer db, XmlWriter outfile)
		{

		// These two lines code uses the CachedProjectInformation to accumulate the raw data from Db4o to a manageable form
			var results = db.Query<CmObjectSurrogate>();
			var asBytes = results.ToArray();

		// And this line causes all the data to be available.  Not putting this statement in causes the follow reads to hit
		// a premature end of the data
			CMOSTHandler.CachedProjectInformation.Compressor.Dispose();

			byte[] compressedBytes = CMOSTHandler.CachedProjectInformation.CompressedMemoryStream.ToArray();
			var stream = new GZipStream(new MemoryStream(compressedBytes), CompressionMode.Decompress);

			var temp = new byte[16];
			var unicoding = new UnicodeEncoding();

			var xml = ReadNextSurrogateXml(stream, unicoding);
			while (xml != null)
			{
		// just copy from in to out
				outfile.WriteRaw(xml);

				xml = ReadNextSurrogateXml(stream, unicoding);
			}

		}

		// Churn through the byte stream just to extract the XML
		internal static string ReadNextSurrogateXml(GZipStream stream, UnicodeEncoding encoder)
		{
			var temp = new byte[16];
			if (stream.Read(temp, 0, 16) <= 0)
				return null;
			var guid = new Guid(temp);
			string className = encoder.GetString(stream.ReadBytes(stream.ReadInt()));
			byte[] xmldata = stream.ReadBytes(stream.ReadInt());
			long id = stream.ReadLong();
			return  UnicodeEncoding.UTF8.GetString(xmldata);

		}


	// The main routine
		/*
		public void db4o2xml(string db4ofile, string xmlfile)
		{

			var config = Db4oEmbedded.NewConfiguration();
			doConfig(config);
			CMOSTHandler.CachedProjectInformation = new CachedProjectInformation();


			var db = Db4oEmbedded.OpenFile(config, db4ofile);

		// from FDOXmlServices.WriterSettings
			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = false,
				CheckCharacters = true,
				ConformanceLevel = ConformanceLevel.Document,
				Encoding = new UTF8Encoding(false),
				Indent = true,
				IndentChars = (""),
				NewLineOnAttributes = false
			};

			var outfile = XmlTextWriter.Create(xmlfile, settings);


			outfile.WriteStartElement("languageproject");

			var modelversion = GetModelVersionNumber(db);
			outfile.WriteAttributeString("version", modelversion.ToString());

			GetAndWriteCustomFieldInfo(db,outfile);

			GetAndWriteSurrogates(db, outfile);

			outfile.WriteEndElement();

			db.Close();
			outfile.Close();
		}
		*/
		public void db4o2xml(string db4ofile, string xmlfile,bool compressed)
		{

			if ( File.Exists(xmlfile))
			{
				DialogResult d = MessageBox.Show("There is already an XML file by that name.  Do you wish to overwrite it?","caption",
					MessageBoxButtons.YesNo);
				if (d == DialogResult.No)
				{
					MessageBox.Show("Extract was not saved.");
					return;

				}

			}


			var config = Db4oEmbedded.NewConfiguration();
			doConfig(config);
			CMOSTHandler.CachedProjectInformation = new CachedProjectInformation();


			var db = Db4oEmbedded.OpenFile(config, db4ofile);

			// from FDOXmlServices.WriterSettings
			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = false,
				CheckCharacters = true,
				ConformanceLevel = ConformanceLevel.Document,
				Encoding = new UTF8Encoding(false),
				Indent = true,
				IndentChars = (""),
				NewLineOnAttributes = false
			};
			XmlWriter xmlstream;
			Stream outstream;
			Stream zipstream = null;

			if (compressed)
			{
				outstream = new FileStream(xmlfile + ".gz", FileMode.Create, FileAccess.Write);
				zipstream = new GZipStream(outstream, CompressionMode.Compress);
				xmlstream = XmlTextWriter.Create(zipstream, settings);

			}
			else
			{

				outstream = new FileStream(xmlfile, FileMode.Create, FileAccess.Write);
				xmlstream = XmlTextWriter.Create(outstream, settings);
			}


			xmlstream.WriteStartElement("languageproject");

			var modelversion = GetModelVersionNumber(db);
			xmlstream.WriteAttributeString("version", modelversion.ToString());

			GetAndWriteCustomFieldInfo(db, xmlstream);

			GetAndWriteSurrogates(db, xmlstream);

			xmlstream.WriteEndElement();

			db.Close();

			//xmlstream.Flush();
			xmlstream.Close();
			if (zipstream != null)
			{
				zipstream.Close();
			}
			outstream.Close();
		}
	}
}
