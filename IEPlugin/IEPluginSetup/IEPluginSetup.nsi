!define VERSION "0.6.24"
!define PROGRAM_NAME "iRachunki.pl"

; DO NOT CHANGE THIS !!!
!define _NET_VERSION "2.0"
!define _NET_2_0_URL "http://www.microsoft.com/downloads/details.aspx?FamilyID=0856EACB-4362-4B0D-8EDD-AAB15C5E04F5&displaylang=pl"
!define REG_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\iRachunki.pl"

!include MUI2.nsh
!include WordFunc.nsh
!include LogicLib.nsh
!insertmacro VersionCompare

;--------------------------------
; Prepare installation
;--------------------------------

; The name of the installer
Name "${PROGRAM_NAME} wersja ${VERSION}"
BrandingText "${PROGRAM_NAME} wersja ${VERSION}"
ShowInstDetails nevershow

; The file to write
OutFile "${PROGRAM_NAME}-IE-${VERSION}.exe"

; The default installation directory
InstallDir "$PROGRAMFILES\Billbank\Plugin ${PROGRAM_NAME}"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------
; Pages
;--------------------------------

!define MUI_ICON ..\Resources\Plugin.ico
!define MUI_UNICON ..\Resources\Plugin-uninst.ico

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Polish"

;--------------------------------
; The stuff to install
;--------------------------------

Section "iRachunki.pl" ;No components page, name is not important

  SectionIn RO

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File ..\bin\Release\IEPlugin.dll
  File ..\lib\Interop.SHDocVw.dll
  File ..\lib\Microsoft.mshtml.dll
  File ..\AddinExpress\AddinExpress.IE.dll
  File ..\AddinExpress\adxloader.dll
  File ..\AddinExpress\adxregext.exe
  File ..\Resources\Plugin.ico
  
  ExecWait '$INSTDIR\adxregext.exe /install="$INSTDIR\IEPlugin.dll" /privileges=admin'
  
  WriteRegDWORD HKCU "Software\Microsoft\Internet Explorer\CommandBar" "ToolBandWidth" 800
  
  ; Write the installation path into the registry
  WriteRegStr HKLM SOFTWARE\Billbank "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM ${REG_KEY} "DisplayName" "${PROGRAM_NAME} - dodatek dla przegl¹darki Internet Explorer"
  WriteRegStr HKLM ${REG_KEY} "DisplayIcon" '"$INSTDIR\Plugin.ico"'
  WriteRegStr HKLM ${REG_KEY} "DisplayVersion" '${VERSION}'
  WriteRegStr HKLM ${REG_KEY} "Publisher" "Billbank Sp. z o.o."
  WriteRegStr HKLM ${REG_KEY} "Contact" "serwis@irachunki.pl"
  WriteRegStr HKLM ${REG_KEY} "URLInfoAbout" "http://irachunki.pl/"
  
  WriteRegStr HKLM ${REG_KEY} "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM ${REG_KEY} "NoModify" 1
  WriteRegDWORD HKLM ${REG_KEY} "NoRepair" 1
  WriteUninstaller "uninstall.exe"
    
SectionEnd ; end the section

;--------------------------------
; Uninstaller
;--------------------------------

Section "Uninstall"

  ExecWait '$INSTDIR\adxregext.exe /uninstall="$INSTDIR\IEPlugin.dll" /privileges=admin'
  
  ; Remove registry keys
  DeleteRegKey HKLM ${REG_KEY}
  DeleteRegKey HKLM SOFTWARE\Billbank

  ; Remove files and uninstaller
  Delete $INSTDIR\Microsoft.mshtml.dll
  Delete $INSTDIR\AddinExpress.IE.dll
  Delete $INSTDIR\adxloader.dll
  Delete $INSTDIR\adxregext.exe
  Delete $INSTDIR\Plugin.ico
  Delete $INSTDIR\IEPlugin.dll
  Delete $INSTDIR\Interop.SHDocVw.dll
  Delete $INSTDIR\uninstall.exe

  ; Remove directories used
  RMDir /r "$INSTDIR"

SectionEnd

;--------------------------------
; Initialize installer
;--------------------------------

Function .onInit

  # call userInfo plugin to get user info.  The plugin puts the result in the stack
  UserInfo::GetAccountType

  # pop the result from the stack into $0
  pop $0

  # compare the result with the string "Admin" to see if the user is admin.
  # If match, jump 3 lines down.
  strCmp $0 "Admin" UserOK

  # if there is not a match, print message and return
  MessageBox MB_OK|MB_ICONSTOP "Dodatek ${PROGRAM_NAME} mo¿e byæ zainstalowany tylko przez u¿ytkownika z uprawnieniami Administratora.\
  $\nUruchom instalator ponownie jako Administrator."
  Abort

  UserOK:

  ; ---
  ; Check .NET version (if any is installed)
  Call GetDotNETVersion
  Pop $0
  ${If} $0 == "not found"
    MessageBox MB_OK|MB_ICONSTOP "Biblioteka uruchomieniowa .NET (.NET runtime library) nie jest zainstalowana \
    (wymagana jest wersja ${_NET_VERSION} lub nowsza).\
    $\n$\nZainstaluj wymagane oprogramowanie ze strony Microsoft i uruchom ten instalator ponownie."
    
    ExecShell "open" ${_NET_2_0_URL}
    Abort
  ${EndIf}
 
  StrCpy $0 $0 "" 1 # skip "v"
 
  ${VersionCompare} $0 "2.0" $1
  ${If} $1 == 2
    MessageBox MB_OK|MB_ICONSTOP "Wymagana jest biblioteka uruchomieniowa .NET (.NET runtime library) \
    w wersji ${_NET_VERSION} lub nowszej.\
    $\nZainstalowana wersja to $0.\
    $\n$\nZainstaluj wymagane oprogramowanie ze strony Microsoft i uruchom ten instalator ponownie."
    
    ExecShell "open" ${_NET_2_0_URL}
    Abort
  ${EndIf}
  
  ; ---
  ; Check if plugin is already installed
  ReadRegStr $R0 HKLM ${REG_KEY} "UninstallString"
  StrCmp $R0 "" done
 
  MessageBox MB_TOPMOST|MB_OKCANCEL|MB_ICONEXCLAMATION \
  "Inna wersja dodatku ${PROGRAM_NAME} jest ju¿ zainstalowana na tym komputerze. \
  $\n$\nKliknij $\"OK$\", aby usun¹æ poprzedni¹ wersjê i zainstalowaæ now¹, albo \
  $\nkliknij $\"Anuluj$\", jeœli chcesz zrezygnowaæ z instalacji." IDOK uninst
  Abort
  
;Run the uninstaller
  uninst:
  ClearErrors
  ExecWait '$R0 /S _?=$INSTDIR' ;Do not copy the uninstaller to a temp file
 
  IfErrors no_remove_uninstaller done
    ;You can either use Delete /REBOOTOK in the uninstaller or add some code
    ;here to remove the uninstaller. Use a registry key to check
    ;whether the user has chosen to uninstall. If you are using an uninstaller
    ;components page, make sure all sections are uninstalled.
  no_remove_uninstaller:
  Abort ; do not continue when uninstallation is cancelled
  
  done:
 
FunctionEnd

;--------------------------------
; Initialize uninstaller
;--------------------------------

Function un.onInit

  repeat:
  FindWindow $0 "IEFrame"
  StrCmp $0 0 continue
    MessageBox MB_ICONSTOP|MB_RETRYCANCEL "Aby odinstalowaæ dodatek ${PROGRAM_NAME} \
    zakmnij wszystkie okna przegl¹darki Internet Explorer i kliknij $\"Ponów próbê$\" \
    $\nalbo kliknij $\"Anuluj$\", jeœli chcesz zrezygnowaæ z instalacji" IDRETRY repeat
    Abort
  continue:
  
FunctionEnd

;--------------------------------
; After installation (success)
;--------------------------------

Function .onInstSuccess

  FindWindow $0 "IEFrame"
  StrCmp $0 0 continue
  
    MessageBox MB_OK|MB_ICONINFORMATION \
    "Aby dokoñczyæ instalacjê dodatku ${PROGRAM_NAME} nale¿y zamkn¹æ przegl¹darkê Internet Explorer i uruchomiæ j¹ ponownie."
  
  continue:
FunctionEnd

;--------------------------------
; Gets current version of .NET framework installed on client's machine
;--------------------------------

Function GetDotNETVersion
  Push $0
  Push $1
 
  System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i) i .r1 ?u"
  StrCmp $1 "error" 0 +2
    StrCpy $0 "not found"
 
  Pop $1
  Exch $0
FunctionEnd