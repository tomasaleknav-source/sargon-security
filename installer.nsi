Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "FileFunc.nsh"
!ifndef APP_VERSION
!define APP_VERSION "0.49"
!endif
Name "SARGON Security ${APP_VERSION}"
OutFile "SARGON_Security_PC_Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\TelefonoSargasPC"
RequestExecutionLevel user
SetCompressor /SOLID lzma
Icon "SargasPC.Shield.ico"
UninstallIcon "SargasPC.Shield.ico"
VIProductVersion "${APP_VERSION}.0.0"
VIAddVersionKey "ProductName" "SARGON Security"
VIAddVersionKey "FileDescription" "Bandomosios Windows programos diegiklis"
VIAddVersionKey "FileVersion" "${APP_VERSION}.0.0"
VIAddVersionKey "LegalCopyright" "Telefono sargas contributors 2026"
!define MUI_ABORTWARNING
Var SARGONUpdateInstall
!define MUI_FINISHPAGE_RUN "$INSTDIR\TelefonoSargasPC.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENCIJA_IR_RIBOS.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Lithuanian"
Function .onInit
 StrCpy $SARGONUpdateInstall "0"
 ${GetParameters} $R0
 ClearErrors
 ${GetOptions} $R0 "/SARGON-UPDATE" $R1
 IfErrors +2
 StrCpy $SARGONUpdateInstall "1"
 ${IfNot} ${AtLeastWin10}
  MessageBox MB_OK|MB_ICONSTOP "Reikia Windows 10 / 11 ir .NET Framework 4.8."
  Abort
 ${EndIf}
 SetRegView 64
 ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
 SetRegView 32
 ${If} $0 < 528040
  MessageBox MB_OK|MB_ICONSTOP "Reikia .NET Framework 4.8. Naudokite oficialius Windows atnaujinimus arba Microsoft diegiklį. Automatinis atsisiuntimas nevykdomas."
  Abort
 ${EndIf}
 System::Call 'kernel32::OpenMutex(i 0x100000, i 0, t "Local\SargasPC_v1") p.r0'
 ${If} $0 != 0
  ${If} $SARGONUpdateInstall == "1"
   StrCpy $R2 0
  sargon_update_wait:
   System::Call 'kernel32::OpenMutex(i 0x100000, i 0, t "Local\SargasPC_v1") p.r0'
   ${If} $0 == 0
    Goto sargon_update_ready
   ${EndIf}
   System::Call 'kernel32::CloseHandle(p r0)'
   ${If} $R2 >= 60
    Goto sargon_update_busy
   ${EndIf}
   Sleep 500
   IntOp $R2 $R2 + 1
   Goto sargon_update_wait
  sargon_update_busy:
   MessageBox MB_OK|MB_ICONSTOP "SARGON dar neužsidarė. Atnaujinimas sustabdytas; paleiskite diegiklį vėliau."
   Abort
  sargon_update_ready:
  ${Else}
  System::Call 'kernel32::CloseHandle(p r0)'
  MessageBox MB_OK|MB_ICONSTOP "Pirmiausia uždarykite veikiančią SARGON Security programą."
  Abort
  ${EndIf}
 ${EndIf}
FunctionEnd
Function .onInstSuccess
 ${If} $SARGONUpdateInstall == "1"
  ExecShell "open" "$INSTDIR\TelefonoSargasPC.exe"
 ${EndIf}
FunctionEnd
Section "SARGON Security" Main
 SectionIn RO
 SetShellVarContext current
 SetOutPath "$INSTDIR"
 Delete "$DESKTOP\Telefono sargas PC.lnk"
 Delete "$SMPROGRAMS\Telefono sargas PC\Telefono sargas PC.lnk"
 Delete "$SMPROGRAMS\Telefono sargas PC\Pašalinti.lnk"
 RMDir "$SMPROGRAMS\Telefono sargas PC"
 File "build\TelefonoSargasPC.exe"
 File "build\TelefonoSargasPC.exe.config"
 File "initial.hdb"
 File /oname=shield.ico "SargasPC.Shield.ico"
 !if /FileExists "src/publisher.der"
 File "src/publisher.der"
 !endif
 !if /FileExists "update-publisher.der"
 File "update-publisher.der"
 !endif
 File "SARGON_Defender_Valdymas.ps1"
 File "Paleisti_SARGON_Defender.cmd"
 File "README_LT.md"
 File "LICENCIJA_IR_RIBOS.txt"
 WriteUninstaller "$INSTDIR\Pasalinti.exe"
 CreateDirectory "$SMPROGRAMS\SARGON Security"
 CreateShortcut "$SMPROGRAMS\SARGON Security\SARGON Security.lnk" "$INSTDIR\TelefonoSargasPC.exe" "" "$INSTDIR\shield.ico" 0
 CreateShortcut "$SMPROGRAMS\SARGON Security\Pašalinti.lnk" "$INSTDIR\Pasalinti.exe"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "DisplayName" "SARGON Security (bandomoji)"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "DisplayVersion" "0.49"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "DisplayIcon" "$INSTDIR\shield.ico"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "UninstallString" '$\"$INSTDIR\Pasalinti.exe$\"'
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "InstallLocation" "$INSTDIR"
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "NoModify" 1
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "NoRepair" 1
SectionEnd
Section "Paleisti prisijungus prie Windows (foninis režimas)"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "SARGON Security" '$\"$INSTDIR\TelefonoSargasPC.exe$\" --startup'
SectionEnd
Section /o "Paslauga ir bendras ClamAV (bandomoji, reikia administratoriaus)" Service
 SetOutPath "$INSTDIR\ServicePackage"
 File /r "build\service-package\*"
 System::Call 'ole32::CoCreateGuid(g .r3) i.r2'
 ${If} $2 != 0
  MessageBox MB_OK|MB_ICONSTOP "Diegimo užklausos nepavyko paruošti. Paslauga neįdiegta."
 ${Else}
  ClearErrors
  DetailPrint "Atvertas atskiras SARGON paruošimo langas. Jam baigus spauskite jame Baigti ir grįžti į diegiklį."
  ExecShellWait "runas" "$INSTDIR\ServicePackage\SARGON.Setup.exe" '--package "$INSTDIR\ServicePackage" --allow-unsigned-beta --request $3' SW_SHOWNORMAL
  SetRegView 64
  ReadRegStr $4 HKLM "Software\SARGONService" "LastSetupId"
  SetRegView 32
  ${If} $4 == $3
   WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "ServiceInstalled" 1
  ${Else}
   MessageBox MB_OK|MB_ICONEXCLAMATION "Paslaugos diegimas nepatvirtintas arba atšauktas. Sąsaja įdiegta, bet tai nėra veikiančios paslaugos patvirtinimas."
  ${EndIf}
 ${EndIf}
SectionEnd
Section "Darbalaukio nuoroda"
 SetShellVarContext current
 CreateShortcut "$DESKTOP\SARGON Security.lnk" "$INSTDIR\TelefonoSargasPC.exe" "" "$INSTDIR\shield.ico" 0
SectionEnd
Function un.onInit
 System::Call 'kernel32::OpenMutex(i 0x100000, i 0, t "Local\SargasPC_v1") p.r0'
 ${If} $0 != 0
  System::Call 'kernel32::CloseHandle(p r0)'
  MessageBox MB_OK|MB_ICONSTOP "Pirmiausia uždarykite SARGON Security."
  Abort
 ${EndIf}
FunctionEnd
Section "Uninstall"
 ReadRegDWORD $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC" "ServiceInstalled"
 ${If} $0 == 1
  SetRegView 64
  ReadRegStr $1 HKLM "Software\SARGONService" "CurrentDirectory"
  SetRegView 32
  IfFileExists "$1\SARGON.Setup.exe" 0 service_missing
  System::Call 'ole32::CoCreateGuid(g .r3) i.r2'
  ${If} $2 != 0
   Abort "Paslaugos pašalinimo užklausos paruošti nepavyko."
  ${EndIf}
  ExecShellWait "runas" "$1\SARGON.Setup.exe" '--uninstall --request $3' SW_SHOWNORMAL
  SetRegView 64
  ReadRegStr $4 HKLM "Software\SARGONService" "LastRemovedId"
  SetRegView 32
  ${If} $4 != $3
   Abort "Paslaugos pašalinimas nepatvirtintas. Uždarymas atšauktas; duomenys išsaugoti."
  ${EndIf}
  Goto service_removed
  service_missing:
   Abort "Paslaugos pašalinimo įrankis nerastas. Pirmiausia atkurkite diegimą."
  service_removed:
 ${EndIf}
 Delete "$INSTDIR\ServicePackage\*"
 RMDir "$INSTDIR\ServicePackage"

 DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "SARGON Security"
 SetShellVarContext current
 Delete "$DESKTOP\SARGON Security.lnk"
 Delete "$SMPROGRAMS\SARGON Security\SARGON Security.lnk"
 Delete "$SMPROGRAMS\SARGON Security\Pašalinti.lnk"
 RMDir "$SMPROGRAMS\SARGON Security"
 Delete "$INSTDIR\SARGON_Defender_Valdymas.ps1"
 Delete "$INSTDIR\Paleisti_SARGON_Defender.cmd"
 Delete "$INSTDIR\TelefonoSargasPC.exe"
 Delete "$INSTDIR\TelefonoSargasPC.exe.config"
 Delete "$INSTDIR\shield.ico"
 Delete "$INSTDIR\initial.hdb"
 Delete "$INSTDIR\update-publisher.der"
 Delete "$INSTDIR\publisher.der"
 Delete "$INSTDIR\README_LT.md"
 Delete "$INSTDIR\LICENCIJA_IR_RIBOS.txt"
 Delete "$INSTDIR\Pasalinti.exe"
 RMDir "$INSTDIR"
 DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TelefonoSargasPC"
 MessageBox MB_OK "Programa pašalinta. Jūsų duomenys ir šifruotos kopijos liko: $LOCALAPPDATA\TelefonoSargasPC. Nenaikinkite jų, kol neatkūrėte reikalingų kopijų."
SectionEnd

Function .onInstSuccess
 System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'
FunctionEnd
