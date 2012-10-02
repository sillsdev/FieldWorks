/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OrientationManager.h
Responsibility: John Thomson
Last reviewed: never

Description:
	Orientation manager contains methods implementing parts of SimpleRootSite that need to be different
	when a view is oriented vertically. Subclasses handle non-standard orientations, while the default methods
	in OrientationManager itself handle normal horizontal orientation.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef OrientationManager_INCLUDED
#define OrientationManager_INCLUDED 1

// forward declarations.
class AfVwScrollWndBase;

// Hungarian: omgr
class OrientationManager
{
protected:
	AfVwScrollWndBase * m_pavswb;
public:

		OrientationManager(AfVwScrollWndBase * pavswb);
		virtual ~OrientationManager() {}
		virtual int LayoutWidth();
		// TODO
		//virtual void DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
		//	SIL.FieldWorks.Common.Utils.Rect drawRect, uint backColor, bool drawSel);

		/// <summary>
		/// Simply tells whether orientation is a vertical one. The default is not.
		/// </summary>
		virtual bool IsVertical()
		{
			return false;
		}

		virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot);

		/// <summary>
		/// The specified rectangle is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same was that the drawing code rotates the actual pixels drawn.
		/// </summary>
		/// <param name="rect"></param>
		virtual RECT RotateRectDstToPaint(RECT rect)
		{
			return rect;
		}
		/// <summary>
		/// The specified point is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same way that the drawing code rotates the actual pixels drawn.
		/// </summary>
		/// <param name="pt"></param>
		virtual POINT RotatePointDstToPaint(POINT pt)
		{
			return pt;
		}

		/// <summary>
		/// The specified point is in 'paint' coordinates. In a vertical view it requires rotation
		/// reversing way that the drawing code rotates the actual pixels drawn to get 'destination'
		/// coordinates that the root box will interpret correctly.
		/// </summary>
		/// <param name="pt"></param>
		virtual POINT RotatePointPaintToDst(POINT pt)
		{
			return pt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Usually Cursors.IBeam; overridden in vertical windows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		// TODO: implement if sufficiently important.
		//virtual Cursor IBeamCursor
		//{
		//	get { return Cursors.IBeam; }
		//}


		/// <summary>
		/// Allow the orientation manager to convert arrow key codes. The default changes nothing.
		/// </summary>
		/// <param name="keyValue"></param>
		/// <returns></returns>
		// TODO: implement if sufficiently important.
		//virtual int ConvertKeyValue(int keyValue)
		//{
		//	return keyValue;
		//}
};

/// <summary>
/// A base class for orientation managers that do vertical alignment.
/// </summary>
class VerticalOrientationManager : public OrientationManager
{
public:
	/// <summary>
	/// make one.
	/// </summary>
	/// <param name="site"></param>
	VerticalOrientationManager(AfVwScrollWndBase * pavswb)
		: OrientationManager(pavswb)
	{
	}

	/// <summary>
	/// Simply tells whether orientation is a vertical one. All vertical ones are.
	/// </summary>
	virtual bool IsVertical()
	{
		return true;
	}

	virtual int LayoutWidth();
	// TODO
	//virtual void DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
	//	SIL.FieldWorks.Common.Utils.Rect drawRect, uint backColor, bool drawSel);

	int ClientWidth();
	virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot);
	virtual RECT RotateRectDstToPaint(RECT rect);
	virtual POINT RotatePointDstToPaint(POINT pt);
	virtual POINT RotatePointPaintToDst(POINT pt);

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Usually Cursors.IBeam; overridden in vertical windows.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	// TODO if wanted
	//override Cursor IBeamCursor
	//{
	//	get { return SIL.FieldWorks.Resources.ResourceHelper.HorizontalIBeamCursor; }
	//}

	/// <summary>
	/// Convert arrow key codes so as to handle rotation (and line inversion).
	/// Enhance JohnT: possibly up/down inversion should be handled by the VwVerticalRootBox
	/// class, in which case, Up and Down results should be swapped here?
	/// </summary>
	/// <param name="keyValue"></param>
	/// <returns></returns>
	// Todo if wanted.
	//override int ConvertKeyValue(int keyValue);

};
#endif