@echo off
start java -cp "repeater/.;*;%~dp0/*" -jar repeater/repeater.jar %1
sleep 2
bin\Debug\csharp-cgdk.exe %2 > res\%2.log

