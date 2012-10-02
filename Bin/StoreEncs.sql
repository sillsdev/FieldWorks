-- This is a temporary hack to get some LgEncodings into the test databases
-- since the VB transfer program no longer transfers them from the kb2 files.

-- Run this script after transferring a kb2 file. Prior to running this script
-- change the second to last line to match the KB vernacular language.

declare @id int, @guid uniqueidentifier
-- Create an English analysis encoding
exec create_LgEncoding @id output, @guid output
update LgEncoding set encoding = 740664001, abbr = 'ENG' where @id = id
insert LangProject_CurrentAnalysisEncs (src, dst, ord) values (0, @id, 0)

-- Create a vernacular encoding.
-- Change the encoding and abbr values as follows:
--    French = 931905001    FRN
--    Ifugao = 1348164001   IFK
--    Greek  = 1095444271   GRKc
--    German = 1015011001   GER
set @id = NULL
set @guid = NULL
exec create_LgEncoding @id output, @guid output
update LgEncoding set encoding = 931905001, abbr = 'FRN' where @id = id
insert LangProject_CurrentVernacularEncs (src, dst, ord) values (0, @id, 0)
