# Parent ⇄ Child WM_COPYDATA Demo

This repository shows how two Windows GUI applications can exchange messages using the native **WM_COPYDATA** mechanism.  
The **parent** application is written in **AutoIt** while the **child** window can be started either as a **C# WinForms** program or as another AutoIt script, proving that the technique works across programming languages.

---

## 1. Why this project exists

Most examples of WM_COPYDATA show two programs written in the *same* language.  
Here you will find a minimal, self-contained reference that answers:

* How do I pass a window handle on the command line?
* How do I correctly marshal / unmarshal the `COPYDATASTRUCT` in C# and in AutoIt?
* How do I keep the UI responsive while receiving messages?
* What changes (if any) are needed for 32-bit vs 64-bit builds?

Feel free to copy the pieces that you need – everything is published under the MIT license.

---

## 2. Repository layout

| File | Language | Purpose |
|------|----------|---------|
| `Parent.au3` | AutoIt | Creates the parent GUI, launches the child process, sends and receives strings. |
| `Child.cs` | C# (.NET 4.8 WinForms) | Shows how to receive the parent handle, send it back and exchange messages. Compiled binary is **Child.exe**. |
| `Child_autoit.au3` | AutoIt | Pure-AutoIt version of the child window. Compiled binary is **Child_autoit.exe**. |
| `*.exe` | – | Pre-built binaries so you can test without building anything. |
| `image.ico` | – | Application icon referenced by the AutoIt compiler directives. |

---

## 3. Building the sources

### 3.1 C# child

1. Open `Child.cs` in Visual Studio (or run `csc` from the command line).  
2. Compile as **Win32** *or* **Any CPU** – the code automatically detects 32-/64-bit parent handles.

### 3.2 AutoIt scripts

The AutoIt files can be executed directly with the interpreter:

```bash
"C:\Program Files\AutoIt3\AutoIt3.exe" Parent.au3
```

To create stand-alone executables run *Aut2Exe* (comes with AutoIt) or use the `#AutoIt3Wrapper_*` directives already present in the scripts.

---

## 4. Running the demo

```bash
Parent.exe [csharp|autoit]
```

*Without an argument* the parent starts the AutoIt child (`Child_autoit.exe`).  
With the `csharp` switch the C# child (`Child.exe`) is launched instead.

1. Type text in the **SEND TO CHILD** field of the parent and press the button – the text appears in the child window.
2. Type a reply in the child’s **SEND TO PARENT** field – the message is echoed back in the parent.
3. Close the parent window – the child process is terminated automatically.

---

## 5. How it works – step by step

1. **Parent** creates its GUI and registers `WM_COPYDATA` via `GUIRegisterMsg`.
2. It launches the child executable and *passes its own window handle* (`$hParent`) as the first command-line argument.
3. **Child** converts this argument back into an `IntPtr` / `HWND` and stores it.
4. Immediately after showing its window the child sends **its own handle** to the parent using `SendMessage(WM_COPYDATA)` – this completes the handshake.
5. From now on both windows hold a valid handle to the other side and can send arbitrary zero-terminated byte sequences using the same message.
6. The receiver unpacks the `COPYDATASTRUCT`, converts the byte array to a string and updates its textbox (all on the UI thread to keep things simple).

---

## 6. Code highlights

### AutoIt (Parent & Child)
* `DllStructCreate("dword;dword;ptr")`  
  Builds the `COPYDATASTRUCT` used by the Windows API.
* `DllCall("user32.dll", "lparam", "SendMessage", …)`  
  Sends the structure to the other window.

### C# (Child)
* `[StructLayout(LayoutKind.Sequential)] struct COPYDATASTRUCT`  
  Mirrors the unmanaged structure.
* `Marshal.AllocHGlobal` + `Marshal.Copy`  
  Allocates and fills unmanaged memory before sending.
* `protected override void WndProc(ref Message m)`  
  Intercepts `WM_COPYDATA`, extracts the payload and invokes a UI update.

---

## 7. 32-bit vs 64-bit considerations

AutoIt is typically 32-bit, but Windows handles are 64-bit on x64 systems.  
The C# child therefore tries multiple parsing strategies (`int`, `long`, hex string) to remain compatible with both architectures.

---

## 8. Using

![Example of using the apps](https://i.imgur.com/OCNiiUG.png)

---

## 9. License

Released under the **MIT License** – see `LICENSE` for details.  
Icons are property of their respective owners.

---

Enjoy hacking!
