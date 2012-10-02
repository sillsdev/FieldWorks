## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propComment = $prop.Comment )
#set( $propNotes = $prop.Notes )
#set( $propTypeClass = $fdogenerate.GetClass($prop.Signature) )
#if ( $prop.IsHandGenerated)
#set( $generated = "_Generated" )
#else
#set( $generated = "" )
#end

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
## Interfaces are not needed here (for now anyway),
## since it will return some kind of FDO collection class.
#if( $prop.IsOwning )
#if ( $prop.Cardinality.ToString() == "Collection" )
		[ModelProperty(CellarPropertyType.OwningCollection, $prop.Number, "$propTypeClass")]
		public IFdoOwningCollection<I$propTypeClass> $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.OwningSequence, $prop.Number, "$propTypeClass")]
		public IFdoOwningSequence<I$propTypeClass> $prop.NiuginianPropName$generated
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		[ModelProperty(CellarPropertyType.ReferenceCollection, $prop.Number, "$propTypeClass")]
		public IFdoReferenceCollection<I$propTypeClass> $prop.NiuginianPropName$generated
#else
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		[ModelProperty(CellarPropertyType.ReferenceSequence, $prop.Number, "IAnalyses")]
		public IFdoReferenceSequence<IAnalysis> $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.ReferenceSequence, $prop.Number, "$propTypeClass")]
		public IFdoReferenceSequence<I$propTypeClass> $prop.NiuginianPropName$generated
#end
#end
		{
			get
			{
				lock (SyncRoot)
				{
#if( $prop.IsOwning )
#if ( $prop.Cardinality.ToString() == "Collection" )
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new FdoOwningCollection<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#else
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new FdoOwningSequence<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new FdoReferenceCollection<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#else
					if (m_$prop.NiuginianPropName == null)
					{
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
						m_$prop.NiuginianPropName = new FdoReferenceSequence<IAnalysis>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<IAnalysisRepository>(),
							this, $prop.Number);
#else
						m_$prop.NiuginianPropName = new FdoReferenceSequence<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
#end
					}
#end
				}
				return m_$prop.NiuginianPropName;
			}
		}