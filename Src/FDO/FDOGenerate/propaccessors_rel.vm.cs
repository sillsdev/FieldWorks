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
#set( $propTypeClass = $fdogenerate.GetClass($prop.Signature) )
#set( $propTypeInterfaceBase = $$propTypeClass )

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
## Interfaces are not needed here (for now anyway),
## since it will return some kind of FDO collection class.
## Once generic classes are added for those, then we do need to add the interface to the generic collection.
## It should then be something like: public ${prop.CSharpType}<IFoo> $prop.NiuginianPropName
#if( $prop.IsHandGenerated)
		private ${prop.CSharpType}<I$propTypeInterfaceBase> $prop.NiuginianPropName$generated
#else
		public ${prop.CSharpType}<I$propTypeInterfaceBase> $prop.NiuginianPropName
#end
		{
			get
			{
				if (m_$prop.NiuginianPropName == null)
					m_$prop.NiuginianPropName = new ${prop.CSharpType}<I$propTypeInterfaceBase>(m_cache, m_hvo, ${prop.FlidLine});
				return m_$prop.NiuginianPropName;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the vector for use in subsequent calls to the ${prop.Name} property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ${prop.CSharpType}<I$propTypeInterfaceBase> m_$prop.NiuginianPropName;