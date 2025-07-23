#Region ;**** Directives created by AutoIt3Wrapper_GUI ****
#AutoIt3Wrapper_Icon=image.ico
#AutoIt3Wrapper_Change2CUI=y
#EndRegion ;**** Directives created by AutoIt3Wrapper_GUI ****
#include <WindowsConstants.au3>
#include <Process.au3> ; for _ProcessGetName
#include <GUIConstantsEx.au3>
#include <Array.au3>

Global $hParent, $hChild, $received, $iChildPID
Global $Csharp = False

; Process command line arguments
ConsoleWrite("args: " & _ArrayToString($CmdLine) & @CRLF)

If $CmdLine[0] > 0 Then
    For $i = 1 To $CmdLine[0]
        Switch StringLower($CmdLine[$i])
            Case "csharp", "c#", "-csharp", "/csharp", "--csharp"
                $Csharp = True
                ConsoleWrite("C# mode activated" & @CRLF)
            Case "autoit", "au3", "-autoit", "/autoit", "--autoit"
                $Csharp = False
                ConsoleWrite("AutoIt mode activated" & @CRLF)
            Case "help", "-help", "/help", "--help", "/?"
                ConsoleWrite("Usage: " & @ScriptName & " [options]" & @CRLF)
                ConsoleWrite("Options:" & @CRLF)
                ConsoleWrite("  csharp, c#     - Use child in C#" & @CRLF)
                ConsoleWrite("  autoit, au3    - Use child in AutoIt (default)" & @CRLF)
                ConsoleWrite("  help           - Show this help" & @CRLF)
                Exit
        EndSwitch
    Next
EndIf

; parent gui
$hParent = GUICreate("Parent", 300, 120, 100)
$input_send = GUICtrlCreateInput("", 10, 10, 280, 20)
$btn = GUICtrlCreateButton("SEND TO CHILD", 80, 35, 140, 20, 0x0001)
GUICtrlCreateLabel("RECEIVED FROM CHILD", 10, 75, 280, 20, 0x01)
$input_received = GUICtrlCreateInput("", 10, 90, 280, 20, 0x0800)
GUISetState(@SW_SHOW)

; launch child - trade handles
GUIRegisterMsg($WM_COPYDATA, "WM_COPYDATA_ReceiveData") ; register WM_COPYDATA

If $Csharp Then
    $iChildPID = Run('"' & @ScriptDir & '\Child.exe" ' & $hParent)
Else
    If @Compiled Then
        $iChildPID = ShellExecute("Child_autoit.exe", $hParent) ; ShellExecute returns the PID
    Else
        $iChildPID = Run('"' & @AutoItExe & '" "' & @ScriptDir & '\Child_autoit.au3" ' & $hParent)
    EndIf
EndIf

; Wait for child handle
While Not $received
    Sleep(50)
WEnd
$hChild = HWnd($received)
$received = ""

; main loop
While 1
    $msg = GUIGetMsg()
    If $msg = $GUI_EVENT_CLOSE Then ExitLoop
    If $msg = $btn Then WM_COPYDATA_SendData($hChild, GUICtrlRead($input_send))
    If $received Then
        GUICtrlSetData($input_received, $received)
        $received = ""
    EndIf
WEnd

; Kill child process correctly
If ProcessExists($iChildPID) Then
    ProcessClose($iChildPID)
EndIf

Exit

;===========================================================================
Func WM_COPYDATA_ReceiveData($hWnd, $MsgID, $wParam, $lParam)
    Local $tCOPYDATA = DllStructCreate("dword;dword;ptr", $lParam)
    Local $tMsg = DllStructCreate("char[" & DllStructGetData($tCOPYDATA, 2) & "]", DllStructGetData($tCOPYDATA, 3))
    $received = DllStructGetData($tMsg, 1)
EndFunc   ;==>WM_COPYDATA_ReceiveData

Func WM_COPYDATA_SendData($hWnd, $sData)
    Local $tCOPYDATA = DllStructCreate("dword;dword;ptr")
    Local $tMsg = DllStructCreate("char[" & StringLen($sData) + 1 & "]")
    DllStructSetData($tMsg, 1, $sData)
    DllStructSetData($tCOPYDATA, 2, StringLen($sData) + 1)
    DllStructSetData($tCOPYDATA, 3, DllStructGetPtr($tMsg))
    DllCall("user32.dll", "lparam", "SendMessage", "hwnd", $hWnd, "int", $WM_COPYDATA, "wparam", 0, "lparam", DllStructGetPtr($tCOPYDATA))
EndFunc   ;==>WM_COPYDATA_SendData