/*==================================================================
** ipymssqlTest.sql
**
** Description:
**	A companion file to ipymssqlTest.py
**
**    License:
**
**        Copyright (C) 2007  SIL International
**
**        This library is free software; you can redistribute it and/or
**        modify it under the terms of the GNU Lesser General Public
**        License as published by the Free Software Foundation; either
**        version 2.1 of the License, or (at your option) any later version.
**
**        This library is distributed in the hope that it will be useful,
**        but WITHOUT ANY WARRANTY; without even the implied warranty of
**        MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
**        Lesser General Public License for more details.
**
**        You should have received a copy of the GNU Lesser General Public
**        License along with this library; if not, write to the Free Software
**        Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
==================================================================*/

--( Using callproc() on this stored procedure from adodbapi returns an
--( unchanged output parameter.

CREATE PROCEDURE [Test]
	@n1 INT,
	@n2 NVARCHAR(4000),
	@n3 INT,
	@n4 INT OUTPUT
AS
BEGIN
	SET @n4 = @n4 + 1

	CREATE TABLE TestCallProc (col NVARCHAR(100))
	INSERT into TestCallProc (col) VALUES ('Test1')
	INSERT into TestCallProc (col) VALUES ('Test2')
	INSERT into TestCallProc (col) VALUES ('Test3')
	INSERT into TestCallProc (col) VALUES ('Test4')
	INSERT into TestCallProc (col) VALUES ('Test5')
	SELECT * FROM TestCallProc
	DROP TABLE TestCallProc

	RETURN 999
END
