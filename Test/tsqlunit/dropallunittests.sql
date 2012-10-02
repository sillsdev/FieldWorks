BEGIN
	DECLARE @sp_name nvarchar(250), @dynSQL nvarchar(250)

	DECLARE utp_cursor CURSOR DYNAMIC
	FOR
	SELECT [name]
	FROM [sysobjects]
	WHERE [name] like 'ut_%'
	AND [xtype] = 'P'
	AND [type] = 'P'

	OPEN utp_cursor

	FETCH NEXT FROm utp_cursor
	INTO @sp_name

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @dynSQL = 'DROP PROC dbo.' + @sp_name
		EXEC sp_executesql @dynSQL

		FETCH NEXT FROm utp_cursor
		INTO @sp_name
	END

	CLOSE utp_cursor
	DEALLOCATE utp_cursor
END