-- Final cleanup which is independent of the specific version.

-- Handle debris left over from deleting classes/fields in the conceptual model
DECLARE @sQry NVARCHAR(4000)
DECLARE @hvo INT

-- First, remove any user view field objects which shouldn't exist
DECLARE uvfCursor CURSOR local static forward_only read_only FOR
	SELECT uvf.id
	FROM UserViewField uvf
	WHERE NOT EXISTS (SELECT Id FROM Field$ f WHERE f.Id = uvf.Flid)
		AND uvf.Flid != 0
OPEN uvfCursor
FETCH NEXT FROM uvfCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sQry = 'EXEC DeleteObjects ''' + CONVERT(NVARCHAR(11), @hvo) + '''';
	EXEC (@sQry)
	FETCH NEXT FROM uvfCursor INTO @hvo
END
CLOSE uvfCursor
DEALLOCATE uvfCursor

-- Now, remove any CmObjects which are now bogus due to invalid OwnFlid$ values.
DECLARE cmoCursor CURSOR local static forward_only read_only FOR
	SELECT o.Id
	FROM CmObject o
	WHERE NOT EXISTS (SELECT Id FROM Field$ f WHERE f.Id = o.OwnFlid$)
		AND o.OwnFlid$ IS NOT NULL
OPEN cmoCursor
FETCH NEXT FROM cmoCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sQry = 'EXEC DeleteObjects ''' + CONVERT(NVARCHAR(11), @hvo) + '''';
	EXEC (@sQry)
	FETCH NEXT FROM cmoCursor INTO @hvo
END
CLOSE cmoCursor
DEALLOCATE cmoCursor

-- Finally, remove any CmObjects which are now bogus due to invalid Class$ values.
DECLARE cmoCursor CURSOR local static forward_only read_only FOR
	SELECT o.Id
	FROM CmObject o
	WHERE NOT EXISTS (SELECT Id FROM Class$ c WHERE c.Id = o.Class$)
		AND o.Class$ IS NOT NULL
OPEN cmoCursor
FETCH NEXT FROM cmoCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sQry = 'EXEC DeleteObjects ''' + CONVERT(NVARCHAR(11), @hvo) + '''';
	EXEC (@sQry)
	FETCH NEXT FROM cmoCursor INTO @hvo
END
CLOSE cmoCursor
DEALLOCATE cmoCursor
