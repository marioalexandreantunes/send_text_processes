#NoTrayIcon
#Region ;**** Directives created by AutoIt3Wrapper_GUI ****
#AutoIt3Wrapper_Icon=image.ico
#EndRegion ;**** Directives created by AutoIt3Wrapper_GUI ****
#include <WindowsConstants.au3>

Global $hParent, $hChild, $received
Global $hParent = HWnd($CmdLine[1]) ; get parent handle from commandline

; child gui
$hChild = GUICreate("Child", 300, 150, 440, Default, 0x00800000)
$input_send = GUICtrlCreateInput("", 10, 10, 280, 20)
$btn = GUICtrlCreateButton("SEND TO PARENT", 80, 35, 140, 20, 0x0001)
GUICtrlCreateLabel("RECEIVED FROM PARENT", 10, 75, 280, 20, 0x01)
$input_received = GUICtrlCreateInput("", 10, 90, 280, 20, 0x0800)
GUISetState(@SW_SHOW)

GUIRegisterMsg($WM_COPYDATA, "WM_COPYDATA_ReceiveData") ; register WM_COPYDATA
WM_COPYDATA_SendData($hParent, $hChild) ; return child handle to parent

; main loop
While 1
    $msg = GUIGetMsg()
    If $msg = $btn Then WM_COPYDATA_SendData($hParent, GUICtrlRead($input_send)) ; send to parent
    If $received Then
        GUICtrlSetData($input_received, $received) ; receive from parent
        $received = ""
    EndIf
WEnd
Exit

;===================================================================================================================================
Func WM_COPYDATA_ReceiveData($hWnd, $MsgID, $wParam, $lParam) ;
    Local $tCOPYDATA = DllStructCreate("dword;dword;ptr", $lParam)
    Local $tMsg = DllStructCreate("char[" & DllStructGetData($tCOPYDATA, 2) & "]", DllStructGetData($tCOPYDATA, 3))
    $received = DllStructGetData($tMsg, 1)
EndFunc

Func WM_COPYDATA_SendData($hWnd, $sData)
    Local $tCOPYDATA = DllStructCreate("dword;dword;ptr")
    Local $tMsg = DllStructCreate("char[" & StringLen($sData) + 1 & "]")
    DllStructSetData($tMsg, 1, $sData)
    DllStructSetData($tCOPYDATA, 2, StringLen($sData) + 1)
    DllStructSetData($tCOPYDATA, 3, DllStructGetPtr($tMsg))
    $Ret = DllCall("user32.dll", "lparam", "SendMessage", "hwnd", $hWnd, "int", $WM_COPYDATA, "wparam", 0, "lparam", DllStructGetPtr($tCOPYDATA))
EndFunc