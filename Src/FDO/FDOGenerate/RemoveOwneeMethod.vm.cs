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
		/// <summary>Remove an object from its old owning flid.</summary>
		/// <param name='owningFlid'>The previous owning flid.</param>
		/// <param name='removee'>The object that was owned in 'owningFlid'.</param>
		internal override void RemoveOwnee(int owningFlid, ICmObject removee)
		{
			switch (owningFlid)
			{
				default:
					base.RemoveOwnee(owningFlid, removee);
					break;
#foreach( $prop in $class.AtomicOwnProperties )
				case $prop.Number:
					m_$prop.NiuginianPropName = null;
					((ICmObjectInternal)this).RemoveObjectSideEffects(new RemoveObjectEventArgs(removee, $prop.Number, -2, true));
					((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(
						this, $prop.Number,
						removee.Guid,
						Guid.Empty);
					break;
#end
#foreach( $prop in $class.CollectionOwnProperties )
				case $prop.Number:
					var originalValue$prop.Number = m_${prop.NiuginianPropName}.ToGuidArray();
					((IFdoOwningCollectionInternal<I$fdogenerate.GetClass($prop.Signature)>)m_${prop.NiuginianPropName}).RemoveOwnee((I$fdogenerate.GetClass($prop.Signature))removee);
					((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(
						this,
						$prop.Number,
						originalValue$prop.Number,
						m_${prop.NiuginianPropName}.ToGuidArray());
					break;
#end
#foreach( $prop in $class.SequenceOwnProperties )
				case $prop.Number:
					var originalValue$prop.Number = m_${prop.NiuginianPropName}.ToGuidArray();
					((IFdoOwningSequenceInternal<I$fdogenerate.GetClass($prop.Signature)>)m_${prop.NiuginianPropName}).RemoveOwnee((I$fdogenerate.GetClass($prop.Signature))removee);
					((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(
						this,
						$prop.Number,
						originalValue$prop.Number,
						m_${prop.NiuginianPropName}.ToGuidArray());
					break;
#end
			}
		}