## --------------------------------------------------------------------------------------------
## Copyright (C) 2007-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		protected override void ToXMLStringInternal(XmlWriter writer)
		{
#if ($className != "LgWritingSystm")
			base.ToXMLStringInternal(writer);
#end
#if ($class.Properties.Count > 0)
#foreach($prop in $class.BasicProperties)
#if ($prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode")
			ReadWriteServices.WriteMultiFoo(writer, "$prop.NiuginianPropName", (MultiAccessor)m_$prop.NiuginianPropName);
#elseif ($prop.Signature == "String")
			ReadWriteServices.WriteITsString(writer, Cache, "$prop.NiuginianPropName", m_$prop.NiuginianPropName);
#elseif ($prop.Signature == "Unicode")
			ReadWriteServices.WriteUnicodeString(writer, "$prop.NiuginianPropName", m_$prop.NiuginianPropName);
#elseif( $prop.Signature == "TextPropBinary")
			ReadWriteServices.WriteTextPropBinary(writer, Cache.WritingSystemFactory, "$prop.NiuginianPropName", m_$prop.NiuginianPropName);
#elseif( $prop.Signature == "Time")
			ReadWriteServices.WriteDateTime(writer, "$prop.NiuginianPropName", m_${prop.NiuginianPropName});
#elseif( $prop.Signature == "Binary")
			ReadWriteServices.WriteByteArray(writer, "$prop.NiuginianPropName", m_$prop.NiuginianPropName);
#elseif( $prop.Signature == "GenDate")
			ReadWriteServices.WriteGenDate(writer, "$prop.NiuginianPropName", m_${prop.NiuginianPropName});
#elseif( $prop.Signature == "Guid")
			ReadWriteServices.WriteGuid(writer, "$prop.NiuginianPropName", m_${prop.NiuginianPropName});
#else
			ReadWriteServices.WriteOtherValueTypeData(writer, "$prop.NiuginianPropName", m_${prop.NiuginianPropName}.ToString());
#end
#end
#foreach( $prop in $class.AtomicRefProperties)
			ReadWriteServices.WriteAtomicObjectProperty(writer, "$prop.Name", ObjectPropertyType.Reference, $prop.NiuginianPropName);
#end
#foreach( $prop in $class.AtomicOwnProperties)
#if( $prop.IsHandGenerated)
			// For persistence purposes, write what is actually there, don't create-on-demand during the write process.
			ReadWriteServices.WriteAtomicObjectProperty(writer, "$prop.Name", ObjectPropertyType.Owning, $prop.NiuginianPropName$generated);
#else
			ReadWriteServices.WriteAtomicObjectProperty(writer, "$prop.Name", ObjectPropertyType.Owning, $prop.NiuginianPropName);
#end
#end
#foreach( $prop in $class.VectorProperties)
			ReadWriteServices.WriteVectorProperty(writer, "$prop.Name", m_$prop.NiuginianPropName);
#end
#end
		}

		/// <summary>Reconstruct an instance from data in some data store.</summary>
		/// <remarks>Only to be used by backend provider system.</remarks>
		protected override void LoadFromDataStoreInternal(XElement root, LoadingServices loadingServices)
		{
			var idFactory = loadingServices.m_objIdFactory;
			var wsf = loadingServices.m_wsf;
			var tsf = loadingServices.m_tsf;
			var uowService = loadingServices.m_uowService;
			XElement currentProperty;

## Process each prop type if class has such, and if there is such an property element.
#foreach( $prop in $class.BasicProperties )
#if ($prop.Signature == "MultiString")
			// Deal with MultiString data type.
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				ReadWriteServices.LoadMultiStringAccessor(this, $prop.Number, currentProperty, ref m_${prop.NiuginianPropName}, wsf, tsf);
				currentProperty.Remove();
			}

#elseif ($prop.Signature == "MultiUnicode")
			// Deal with MultiUnicode data type.
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				ReadWriteServices.LoadMultiUnicodeAccessor(this, $prop.Number, currentProperty, ref m_${prop.NiuginianPropName}, wsf, tsf);
				currentProperty.Remove();
			}

#elseif ($prop.Signature == "String")
			// Deal with String data type.
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = TsStringSerializer.DeserializeTsStringFromXml((XElement)currentProperty.FirstNode, wsf);
				currentProperty.Remove();
			}
#elseif ($prop.Signature == "Unicode")
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadUnicodeString(currentProperty);
				currentProperty.Remove();
			}
#elseif( $prop.Signature == "TextPropBinary")
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadTextPropBinary(currentProperty, wsf);
				currentProperty.Remove();
			}
#else
#if ( $prop.Signature == "Integer" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadInteger(currentProperty);
				currentProperty.Remove();
			}
#elseif ( $prop.Signature == "Binary" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadByteArray(currentProperty);
				currentProperty.Remove();
			}
#elseif ( $prop.Signature == "Boolean" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadBoolean(currentProperty);
				currentProperty.Remove();
			}
#elseif ( $prop.Signature == "Guid" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadGuid(currentProperty);
				currentProperty.Remove();
			}
#elseif ( $prop.Signature == "Time" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadDateTime(currentProperty);
				currentProperty.Remove();
			}
#elseif ( $prop.Signature == "GenDate" )
			currentProperty = root.Element("$prop.NiuginianPropName");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadGenDate(currentProperty);
				currentProperty.Remove();
			}
#end
#end
#end
#foreach( $prop in $class.AtomicProperties )
			currentProperty = root.Element("$prop.Name");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = ReadWriteServices.LoadAtomicObjectProperty(currentProperty.Element("objsur"), idFactory);
				currentProperty.Remove();
			}
#end
#foreach( $prop in $class.CollectionOwnProperties )
			currentProperty = root.Element("$prop.Name");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = new FdoOwningCollection<I$prop.Signature>(
					uowService,
					Cache.ServiceLocator.GetInstance<I${prop.Signature}Repository>(),
					this, $prop.Number);
				((IFdoVectorInternal)m_${prop.NiuginianPropName}).LoadFromDataStoreInternal(currentProperty, idFactory);
				currentProperty.Remove();
			}
#end
#foreach( $prop in $class.SequenceOwnProperties )
			currentProperty = root.Element("$prop.Name");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = new FdoOwningSequence<I$prop.Signature>(
					uowService,
					Cache.ServiceLocator.GetInstance<I${prop.Signature}Repository>(),
					this, $prop.Number);
				((IFdoVectorInternal)m_${prop.NiuginianPropName}).LoadFromDataStoreInternal(currentProperty, idFactory);
				currentProperty.Remove();
			}
#end
#foreach( $prop in $class.CollectionRefProperties )
			currentProperty = root.Element("$prop.Name");
			if (currentProperty != null)
			{
				m_$prop.NiuginianPropName = new FdoReferenceCollection<I$prop.Signature>(
					uowService,
					Cache.ServiceLocator.GetInstance<I${prop.Signature}Repository>(),
					this, $prop.Number);
				((IFdoVectorInternal)m_${prop.NiuginianPropName}).LoadFromDataStoreInternal(currentProperty, idFactory);
				currentProperty.Remove();
			}
#end
#foreach( $prop in $class.SequenceRefProperties )
			currentProperty = root.Element("$prop.Name");
			if (currentProperty != null)
			{
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
				m_$prop.NiuginianPropName = new FdoReferenceSequence<IAnalysis>(
					uowService,
					Cache.ServiceLocator.GetInstance<IAnalysisRepository>(),
					this, $prop.Number);
#else
				m_$prop.NiuginianPropName = new FdoReferenceSequence<I$prop.Signature>(
					uowService,
					Cache.ServiceLocator.GetInstance<I${prop.Signature}Repository>(),
					this, $prop.Number);
#end
				((IFdoVectorInternal)m_${prop.NiuginianPropName}).LoadFromDataStoreInternal(currentProperty, idFactory);
				currentProperty.Remove();
			}
#end

#if ($className != "LgWritingSystm")
			base.LoadFromDataStoreInternal(root, loadingServices);
#end
		}
