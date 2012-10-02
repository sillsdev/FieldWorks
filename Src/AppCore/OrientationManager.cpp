/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OrientationManager.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	Orientation manager contains methods implementing parts of AfVwWnd that need to be different
	when a view is oriented vertically. Subclasses handle non-standard orientations, while the default methods
	in OrientationManager itself handle normal horizontal orientation.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
/// <summary>
/// Make one.
/// </summary>
/// <param name="site"></param>
OrientationManager::OrientationManager(AfVwScrollWndBase * pavswb)
{
	m_pavswb = pavswb;
}

/// <summary>
/// The width available for laying out a line of text, before subtracting margins. Normally this is the width of the pane,
/// but for vertical alignment it is the height.
/// </summary>
/// <returns></returns>
int OrientationManager::LayoutWidth()
{
	RECT rcClient;
	::GetClientRect(m_pavswb->m_pwndSubclass->Hwnd(), &rcClient);
	return rcClient.right - rcClient.left;
}

/// <summary>
/// The core of the Draw() method, where the rectangle actually gets painted.
/// Vertical views use a rotated drawing routine.
/// </summary>
/// <param name="vdrb"></param>
/// <param name="rootb"></param>
/// <param name="hdc"></param>
/// <param name="drawRect"></param>
/// <param name="backColor"></param>
/// <param name="drawSel"></param>
// TODO
// void OrientationManager::DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
//	SIL.FieldWorks.Common.Utils.Rect drawRect, uint backColor, bool drawSel)
//{
//	vdrb.DrawTheRoot(rootb, hdc, drawRect, backColor,
//		drawSel, m_site);
//}

/// -----------------------------------------------------------------------------------
/// <summary>
/// Construct coord transformation rectangles. Height and width are dots per inch.
/// src origin is 0, dest origin is controlled by scrolling.
/// </summary>
/// <param name="rcSrcRoot"></param>
/// <param name="rcDstRoot"></param>
/// -----------------------------------------------------------------------------------
 void OrientationManager::GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	prcSrcRoot->left = prcSrcRoot->top = 0;
	int dxInch;
	int dyInch;
	pvg->get_XUnitsPerInch(&dxInch);
	pvg->get_YUnitsPerInch(&dyInch);
	prcSrcRoot->right = dxInch;
	prcSrcRoot->bottom = dyInch;
	int dxdScrollOffset, dydScrollOffset;
	m_pavswb->GetScrollOffsets(&dxdScrollOffset, &dydScrollOffset);
	prcDstRoot->left = (-dxdScrollOffset) + m_pavswb->GetHorizMargin();
	prcDstRoot->top = (-dydScrollOffset);
	prcDstRoot->right = prcDstRoot->left + prcSrcRoot->right;
	prcDstRoot->bottom = prcDstRoot->top + prcSrcRoot->bottom;
}

/// ------------------------------------------------------------------------------------
/// <summary>
/// Usually Cursors.IBeam; overridden in vertical windows.
/// </summary>
/// ------------------------------------------------------------------------------------
// TODO: implement if sufficiently important.
//internal  OrientationManager::Cursor IBeamCursor
//{
//	get { return Cursors.IBeam; }
//}


/// <summary>
/// Allow the orientation manager to convert arrow key codes. The default changes nothing.
/// </summary>
/// <param name="keyValue"></param>
/// <returns></returns>
// TODO: implement if sufficiently important.
//int OrientationManager::ConvertKeyValue(int keyValue)
//{
//	return keyValue;
//}

/// <summary>
/// The width available for laying out a line of text, before subtracting margins. Normally this is the width of the pane,
/// but for vertical alignment it is the height.
/// </summary>
/// <returns></returns>
 int VerticalOrientationManager::LayoutWidth()
{
		RECT rcClient;
		::GetClientRect(m_pavswb->m_pwndSubclass->Hwnd(), &rcClient);
		return rcClient.bottom - rcClient.top;
}
/// <summary>
/// The core of the Draw() method, where the rectangle actually gets painted.
/// Vertical views use a rotated drawing routine.
/// </summary>
/// <param name="vdrb"></param>
/// <param name="rootb"></param>
/// <param name="hdc"></param>
/// <param name="drawRect"></param>
/// <param name="backColor"></param>
/// <param name="drawSel"></param>
// TODO
// void VerticalOrientationManager::DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
//	SIL.FieldWorks.Common.Utils.Rect drawRect, uint backColor, bool drawSel)
//{
//	vdrb.DrawTheRootRotated(rootb, hdc, drawRect, backColor,
//		drawSel, m_site, 1);
//}

int VerticalOrientationManager::ClientWidth()
{
	RECT rcClient;
	::GetClientRect(m_pavswb->m_pwndSubclass->Hwnd(), &rcClient);
	return rcClient.right - rcClient.left;
}
/// -----------------------------------------------------------------------------------
/// <summary>
/// Construct coord transformation rectangles. Height and width are dots per inch
/// (swapped for rotation);
/// src origin is 0, dest origin is controlled by scrolling.
///
/// A change in the y value of rcDstRoot origin will move the view left or right.
/// A zero position of the scroll bar puts the 'bottom' at the right of the window.
/// We want instead to put the 'top' at the left of the window for offset zero,
/// and move it to the left as the offset increases.
/// Passing an actual offset of 0 puts the bottom of the view at the right of the
/// window. Adding the rootbox height puts the top just out of sight beyond the right edge;
/// subtracting the client rectangle puts it in the proper zero-offset position with the
/// top just at the left of the window. Further subtracting the scroll offset moves it
/// further right, or 'up'.
/// </summary>
/// <param name="rcSrcRoot"></param>
/// <param name="rcDstRoot"></param>
/// -----------------------------------------------------------------------------------
void VerticalOrientationManager::GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	prcSrcRoot->left = prcSrcRoot->top = 0;
	int dxInch;
	int dyInch;
	pvg->get_XUnitsPerInch(&dxInch);
	pvg->get_YUnitsPerInch(&dyInch);
	prcSrcRoot->right = dyInch;
	prcSrcRoot->bottom = dxInch;

	int dxdScrollOffset, dydScrollOffset;
	m_pavswb->GetScrollOffsets(&dxdScrollOffset, &dydScrollOffset);

	int dypRootHeight;
	m_pavswb->m_qrootb->get_Height(&dypRootHeight);

	prcDstRoot->left = (-dydScrollOffset) + m_pavswb->GetHorizMargin();
	prcDstRoot->top = ClientWidth() - dypRootHeight + dxdScrollOffset;
	prcDstRoot->right = prcDstRoot->left + dyInch;
	prcDstRoot->bottom = prcDstRoot->top + dxInch;
}

/// <summary>
/// The specified rectangle is in 'destination' coordinates. In a vertical view it requires rotation
/// in the same way that the drawing code rotates the actual pixels drawn.
/// This basically means that the input is a rectangle whose top and bottom are distances from the
/// right of the window and whose left and right are measured from the top of the window.
/// We need to convert it to one measured from the top left.
/// </summary>
/// <param name="rect"></param>
RECT VerticalOrientationManager::RotateRectDstToPaint(RECT rect)
{
	int width = ClientWidth();
	RECT result;
	result.left = width - rect.bottom;
	result.top = rect.left;
	result.right = result.left + (rect.bottom - rect.top);
	result.bottom = result.top + (rect.right - rect.left);
	return result;
}

/// <summary>
/// The specified point is in 'destination' coordinates. In a vertical view it requires rotation
/// in the same way that the drawing code rotates the actual pixels drawn.
/// This basically means that pt.Y is measured from the right of the window and pt.X is measured
/// from the top. The result needs to be the same point in normal coordinates.
/// </summary>
/// <param name="pt"></param>
POINT VerticalOrientationManager::RotatePointDstToPaint(POINT pt)
{
	POINT result;
	result.x = ClientWidth() - pt.y;
	result.y = pt.x;
	return result;
}

/// <summary>
/// The specified point is in 'paint' coordinates. In a vertical view it requires rotation
/// reversing way that the drawing code rotates the actual pixels drawn to get 'destination'
/// coordinates that the root box will interpret correctly.
/// This basically converts a normal point to one where X is measured from the top of the client
/// rectangle and Y from the right.
/// </summary>
/// <param name="pt"></param>
POINT VerticalOrientationManager::RotatePointPaintToDst(POINT pt)
{
	POINT result;
	result.x = pt.y;
	result.y = ClientWidth() - pt.x;
	return result;
}

/// ------------------------------------------------------------------------------------
/// <summary>
/// Usually Cursors.IBeam; overridden in vertical windows.
/// </summary>
/// ------------------------------------------------------------------------------------
// TODO if wanted
//override VerticalOrientationManager::Cursor IBeamCursor
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
//override int VerticalOrientationManager::ConvertKeyValue(int keyValue)
//{
//	switch(keyValue)
//	{
//		case (int)Keys.Left:
//			return (int)Keys.Down;
//		case (int) Keys.Right:
//			return (int)Keys.Up;
//		case (int) Keys.Up:
//			return (int)Keys.Left;
//		case (int) Keys.Down:
//			return (int)Keys.Right;
//	}
//	return base.ConvertKeyValue(keyValue);
//}
