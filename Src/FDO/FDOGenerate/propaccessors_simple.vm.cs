## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#if( $prop.Cardinality.ToString() == "Atomic")
	#set( $suffix = "Hvo")
#else
	#set( $suffix = "")
#end

#if( $prop.CSharpType == "???" )
		// Type not implemented in FDO
#elseif( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get
			{
				return new ${prop.CSharpType}(m_cache, m_hvo,
					${prop.FlidLine}, ${prop.ViewName});
			}
		}
#elseif( $prop.Signature == "String")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return new ${prop.CSharpType}(m_cache, m_hvo, ${prop.FlidLine});}
		}
#elseif( $prop.Signature == "TextPropBinary")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name} $suffix
		/// </summary>
		/// <remarks>TsTextProps handled as Unknown props</remarks>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$suffix$generated
#else
		public $prop.CSharpType $prop.NiuginianPropName$suffix
#end
		{
			get { return (${prop.CSharpType})m_cache.Get${prop.GetSetMethod}(m_hvo, ${prop.FlidLine});}
			set { m_cache.Set${prop.GetSetMethod}(m_hvo, ${prop.FlidLine}, value); }
		}
#else ## All other properties
## For CmObjects the return value here will just be an int, not an interface.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name} $suffix
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$suffix$generated
#else
		public $prop.CSharpType $prop.NiuginianPropName$suffix
#end
		{
#if( $prop.OverridenType == "")
			get { return m_cache.Get${prop.GetSetMethod}(m_hvo, ${prop.FlidLine});}
			set { m_cache.Set${prop.GetSetMethod}(m_hvo, ${prop.FlidLine}, value); }
#else
			get { return ($prop.OverridenType)m_cache.Get${prop.GetSetMethod}(m_hvo, ${prop.FlidLine});}
			set { m_cache.Set${prop.GetSetMethod}(m_hvo, ${prop.FlidLine}, (int)value); }
#end
		}
#end
