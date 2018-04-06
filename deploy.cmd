@echo off
if not exist .git\ goto :wrongfolder 
if not exist src\ goto :wrongfolder 
md Deploy 1>nul 2>nul
del Deploy\*.* /q
copy src\DeckRatings\bin\release\DeckRatings.exe Deploy\.
copy src\DeckRatings\bin\release\CsvHelper.dll Deploy\.
copy src\DeckRatings\bin\release\CommandLine.dll Deploy\.
copy src\RatingPurge\bin\Release\RatingPurge.exe Deploy\.
copy src\RatingPurge\bin\Release\Newtonsoft.Json.dll Deploy\.
copy src\RatingPurge\bin\Release\WikiaClientLibrary.dll Deploy\.
goto :eof

:wrongfolder
echo Please run from the root folder.
