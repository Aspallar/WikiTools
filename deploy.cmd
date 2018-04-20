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
copy src\GetRatings\bin\Release\GetRatings.exe Deploy\.
copy src\CardNames\bin\Release\CardNames.exe Deploy\.
copy src\CardNames\bin\Release\Hunspell*.dll Deploy\.
copy src\CardNames\bin\Release\NHunspell.dll Deploy\.
copy src\CardNames\bin\Release\en_US.* Deploy\.
copy src\CardNames\bin\Release\GathererShared.dll Deploy\.
copy src\CardNames\bin\Release\Cards.json Deploy\.
copy src\UploadFiles\bin\Release\UploadFiles.exe Deploy\.
copy src\UploadFiles\bin\Release\UploadFiles.exe.config Deploy\.
copy src\UploadFiles\bin\Release\WikiToolsShared.dll Deploy\.
goto :eof

:wrongfolder
echo Please run from the root folder.
