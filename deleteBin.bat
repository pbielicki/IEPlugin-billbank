cd IEPlugin
rd /S /Q bin
rd /S /Q obj

cd IEPluginSetup
rd /S /Q Debug
rd /S /Q Release
del *.exe

cd ..\..\
cd IEPluginTests
rd /S /Q bin
rd /S /Q obj

cd ..