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
#set( $fullSig = $propTypeClass.GetRelativeQualifiedSignature($prop.Parent.Parent) )

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private I$propTypeClass $prop.NiuginianPropName$generated
#else
		public I$propTypeClass $prop.NiuginianPropName
#end
		{
			get
			{
#if( $prop.IsHandGenerated)
				int hvo = ${prop.NiuginianPropName}Hvo$generated;
#else
				int hvo = ${prop.NiuginianPropName}Hvo;
#end
				if (hvo > 0)
					//this factory method will make a subclass if that's what it is
					return CmObject.CreateFromDBObject(m_cache, hvo) as I$propTypeClass;
				else
					return null;
			}
			set
			{
#if ( $prop.IsOwning )
				SetOwningProperty(${prop.FlidLine}, value);
#else
				SetReferenceProperty(${prop.FlidLine}, value);
#end
			}
		}
