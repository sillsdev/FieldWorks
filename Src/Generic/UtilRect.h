/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilRect.h
Responsibility: Shon Katzenberger
Last reviewed:

	Point and Rect classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UtilRect_H
#define UtilRect_H 1

// This namespace allows us to disambiguate between FW's Rect and Point classes and Graphite's
// classes with the same name.
namespace fwutil
{

/*----------------------------------------------------------------------------------------------
	Point class. This derives from POINT so it can be used in Win32 calls.
----------------------------------------------------------------------------------------------*/
class Point : public POINT
{
public:
	/*------------------------------------------------------------------------------------------
		Constructors.
	------------------------------------------------------------------------------------------*/
	Point(void)
	{
		AssertPtr(this);
	}
	Point(int xp, int yp)
	{
		AssertPtr(this);

		x = xp;
		y = yp;
	}
	Point(const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		x = pt.x;
		y = pt.y;
	}

	/*------------------------------------------------------------------------------------------
		Operators.
	------------------------------------------------------------------------------------------*/
	Point & operator = (const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		x = pt.x;
		y = pt.y;
		return *this;
	}

	Point & operator += (const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		x += pt.x;
		y += pt.y;
		return * this;
	}

	Point & operator -= (const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		x -= pt.x;
		y -= pt.y;
		return * this;
	}

	Point operator + (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return Point(x + pt.x, y + pt.y);
	}

	Point operator - (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return Point(x - pt.x, y - pt.y);
	}

	bool operator == (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return x == pt.x && y == pt.y;
	}

	bool operator != (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return x != pt.x || y != pt.y;
	}

	/*------------------------------------------------------------------------------------------
		Other methods.
	------------------------------------------------------------------------------------------*/
	void Clear(void)
	{
		AssertPtr(this);

		x = y = 0;
	}

	void Set(int xp, int yp)
	{
		AssertPtr(this);

		x = xp;
		y = yp;
	}

	void Offset(long dx, long dy)
	{
		AssertPtr(this);

		x += dx;
		y += dy;
	}

	// Map the point from rcSrc to rcDst coordinates.
	Point & Map(const RECT & rcSrc, const RECT & rcDst)
	{
		AssertPtr(this);
		AssertPtr(&rcSrc);
		AssertPtr(&rcDst);

		int dzsSrc, dzsDst;

		dzsSrc = rcSrc.right - rcSrc.left;
		Assert(dzsSrc > 0);
		dzsDst = rcDst.right - rcDst.left;
		if (dzsSrc == dzsDst)
			x += rcDst.left - rcSrc.left;
		else
			x = rcDst.left + MulDiv(x - rcSrc.left, dzsDst, dzsSrc);

		dzsSrc = rcSrc.bottom - rcSrc.top;
		Assert(dzsSrc > 0);
		dzsDst = rcDst.bottom - rcDst.top;
		if (dzsSrc == dzsDst)
			y += rcDst.top - rcSrc.top;
		else
			y = rcDst.top + MulDiv(y - rcSrc.top, dzsDst, dzsSrc);

		return *this;
	}

	void Scale(int nNum, int nDen = 1)
	{
		AssertPtr(this);
		Assert(nDen);

		x = MulDiv(x, nNum, nDen);
		y = MulDiv(y, nNum, nDen);
	}
#ifdef WIN32
	void ClientToScreen(HWND hwnd)
	{
		::ClientToScreen(hwnd, this);
	}
	void ScreenToClient(HWND hwnd)
	{
		::ScreenToClient(hwnd, this);
	}
#endif // WIN32
};

#define MakePoint(lp) Point((short)LOWORD(lp), (short)HIWORD(lp))


/*----------------------------------------------------------------------------------------------
	Rectangle class. This derives from RECT so it can be used in Win32 calls.
----------------------------------------------------------------------------------------------*/
class Rect : public RECT
{
public:
	/*------------------------------------------------------------------------------------------
		Constructors.
	------------------------------------------------------------------------------------------*/
	Rect(void)
	{
		AssertPtr(this);
		Clear();
	}
	Rect(int xLeft, int yTop = 0, int xRight = 0, int yBottom = 0)
	{
		AssertPtr(this);

		left = xLeft;
		top = yTop;
		right = xRight;
		bottom = yBottom;
	}
	Rect(const RECT & rc)
	{
		AssertPtr(this);
		AssertPtr(&rc);

		left = rc.left;
		top = rc.top;
		right = rc.right;
		bottom = rc.bottom;
	}

	/*------------------------------------------------------------------------------------------
		Operators.
	------------------------------------------------------------------------------------------*/
	Rect & operator = (const RECT & rc)
	{
		AssertPtr(this);
		AssertPtr(&rc);

		left = rc.left;
		top = rc.top;
		right = rc.right;
		bottom = rc.bottom;
		return *this;
	}

	Rect & operator += (const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		left += pt.x;
		right += pt.x;
		top += pt.y;
		bottom += pt.y;
		return * this;
	}

	Rect & operator -= (const POINT & pt)
	{
		AssertPtr(this);
		AssertPtr(&pt);

		left -= pt.x;
		right -= pt.x;
		top -= pt.y;
		bottom -= pt.y;
		return * this;
	}

	Rect operator + (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return Rect(left + pt.x, top + pt.y, right + pt.x, bottom + pt.y);
	}

	Rect operator - (const POINT & pt) const
	{
		AssertPtr(this);
		AssertPtr(&pt);

		return Rect(left - pt.x, top - pt.y, right - pt.x, bottom - pt.y);
	}

	bool operator == (const RECT & rc) const
	{
		AssertPtr(this);
		AssertPtr(&rc);

		return left == rc.left && top == rc.top && right == rc.right && bottom == rc.bottom;
	}

	bool operator != (const RECT & rc) const
	{
		AssertPtr(this);
		AssertPtr(&rc);

		return left != rc.left || top != rc.top || right != rc.right || bottom != rc.bottom;
	}

	/*------------------------------------------------------------------------------------------
		Other methods.
	------------------------------------------------------------------------------------------*/
	void Clear(void)
	{
		AssertPtr(this);

		left = top = right = bottom = 0;
	}

	bool IsClear(void)
	{
		return !left && !top && !right && !bottom;
	}

	void Set(int xLeft, int yTop, int xRight, int yBottom)
	{
		AssertPtr(this);

		left = xLeft;
		top = yTop;
		right = xRight;
		bottom = yBottom;
	}

	void Offset(long dx, long dy)
	{
		AssertPtr(this);

		left += dx;
		right += dx;
		top += dy;
		bottom += dy;
	}

	Point TopLeft(void) const
	{
		AssertPtr(this);

		return Point(left, top);
	}

	Point BottomRight(void) const
	{
		AssertPtr(this);

		return Point(right, bottom);
	}

	Point Size(void) const
	{
		AssertPtr(this);

		return Point(right - left, bottom - top);
	}

	int Width(void) const
	{
		AssertPtr(this);

		return right - left;
	}

	int Height(void) const
	{
		AssertPtr(this);

		return bottom - top;
	}

	bool IsEmpty(void) const
	{
		AssertPtr(this);

		return bottom <= top || right <= left;
	}

	void Map(const RECT & rcSrc, const RECT & rcDst)
	{
		AssertPtr(this);
		AssertPtr(&rcSrc);
		AssertPtr(&rcDst);

		int dzsSrc, dzsDst;

		dzsSrc = rcSrc.right - rcSrc.left;
		Assert(dzsSrc > 0);
		dzsDst = rcDst.right - rcDst.left;
		if (dzsSrc == dzsDst)
		{
			// Change right first in case "this" is one of the mapping rectangles.
			right += rcDst.left - rcSrc.left;
			left += rcDst.left - rcSrc.left;
		}
		else
		{
			// Change right first in case "this" is one of the mapping rectangles.
			right = rcDst.left + MulDiv(right - rcSrc.left, dzsDst, dzsSrc);
			left = rcDst.left + MulDiv(left - rcSrc.left, dzsDst, dzsSrc);
		}

		dzsSrc = rcSrc.bottom - rcSrc.top;
		Assert(dzsSrc > 0);
		dzsDst = rcDst.bottom - rcDst.top;
		if (dzsSrc == dzsDst)
		{
			// Change bottom first in case "this" is one of the mapping rectangles.
			bottom += rcDst.top - rcSrc.top;
			top += rcDst.top - rcSrc.top;
		}
		else
		{
			// Change bottom first in case "this" is one of the mapping rectangles.
			bottom = rcDst.top + MulDiv(bottom - rcSrc.top, dzsDst, dzsSrc);
			top = rcDst.top + MulDiv(top - rcSrc.top, dzsDst, dzsSrc);
		}
	}

	int MapXTo(int x, const RECT & rcDst) const
	{
		AssertPtr(this);
		AssertPtr(&rcDst);
		Assert(Width() > 0);

		int dxs = Width();
		int dxd = rcDst.right - rcDst.left;

		if (dxs == dxd)
			return x + rcDst.left - left;

		return rcDst.left + MulDiv(x - left, dxd, dxs);
	}

	int MapYTo(int y, const RECT & rcDst) const
	{
		AssertPtr(this);
		AssertPtr(&rcDst);
		Assert(Height() > 0);

		int dys = Height();
		int dyd = rcDst.bottom - rcDst.top;

		if (dys == dyd)
			return y + rcDst.top - top;

		return rcDst.top + MulDiv(y - top, dyd, dys);
	}

	void Scale(int nNum, int nDen = 1)
	{
		AssertPtr(this);
		Assert(nDen);

		left = MulDiv(left, nNum, nDen);
		right = MulDiv(left, nNum, nDen);
		top = MulDiv(top, nNum, nDen);
		bottom = MulDiv(bottom, nNum, nDen);
	}

	void ScaleSize(int nNum, int nDen = 1)
	{
		AssertPtr(this);
		Assert(nDen);

		right = left + MulDiv(right - left, nNum, nDen);
		bottom = top + MulDiv(bottom - top, nNum, nDen);
	}

	void Inflate(int dx, int dy)
	{
		left -= dx;
		right += dx;
		top -= dy;
		bottom += dy;
	}

	void Center(const RECT & rc)
	{
		AssertPtr(this);
		AssertPtr(&rc);

		int dx = Width();
		int dy = Height();

		left = (rc.left + rc.right - dx) / 2;
		right = left + dx;
		top = (rc.top + rc.bottom - dy) / 2;
		bottom = top + dy;
	}

	void Center(int x, int y)
	{
		AssertPtr(this);

		int dx = Width();
		int dy = Height();

		left = x - dx / 2;
		right = left + dx;
		top = y - dy / 2;
		bottom = top + dy;
	}

	bool Inside(const RECT & rc)
	{
		return left >= rc.left && top >= rc.top && right <= rc.right && bottom <= rc.bottom ||
			IsEmpty();
	}

	bool Contains(const POINT & pt)
	{
		return pt.x >= left && pt.y >= top && pt.x < right  && pt.y < bottom;
	}

	bool Intersect(const RECT & rc1, const RECT & rc2)
	{
		left = Max(rc1.left, rc2.left);
		right = Min(rc1.right, rc2.right);
		top = Max(rc1.top, rc2.top);
		bottom = Min(rc1.bottom, rc2.bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

	bool Intersect(const RECT & rc)
	{
		left = Max(rc.left, left);
		right = Min(rc.right, right);
		top = Max(rc.top, top);
		bottom = Min(rc.bottom, bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

	bool Union(const RECT & rc1, const RECT & rc2)
	{
		left = Min(rc1.left, rc2.left);
		right = Max(rc1.right, rc2.right);
		top = Min(rc1.top, rc2.top);
		bottom = Max(rc1.bottom, rc2.bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

	bool Union(const RECT & rc)
	{
		left = Min(rc.left, left);
		right = Max(rc.right, right);
		top = Min(rc.top, top);
		bottom = Max(rc.bottom, bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

	bool Sum(const RECT & rc1, const RECT & rc2)
	{
		left = Min(rc1.left, rc2.left);
		right = Max(rc1.right, rc2.right);
		top = Min(rc1.top, rc2.top);
		bottom = Max(rc1.bottom, rc2.bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

	bool Sum(const RECT & rc)
	{
		left = Min(rc.left, left);
		right = Max(rc.right, right);
		top = Min(rc.top, top);
		bottom = Max(rc.bottom, bottom);

		if (IsEmpty())
		{
			Clear();
			return false;
		}
		return true;
	}

#ifdef WIN32
	void ClientToScreen(HWND hwnd)
	{
		POINT topLeft = {left, top};
		POINT bottomRight = {right, bottom};
		::ClientToScreen(hwnd, &topLeft);
		::ClientToScreen(hwnd, &bottomRight);
		left = topLeft.x;
		top = topLeft.y;
		right = bottomRight.x;
		bottom = bottomRight.y;
	}
	void ScreenToClient(HWND hwnd)
	{
		POINT topLeft = {left, top};
		POINT bottomRight = {right, bottom};
		::ScreenToClient(hwnd, &topLeft);
		::ScreenToClient(hwnd, &bottomRight);
		left = topLeft.x;
		top = topLeft.y;
		right = bottomRight.x;
		bottom = bottomRight.y;
	}
#endif // WIN32
};

} // namespace fwutil

#endif // !UtilRect_H
