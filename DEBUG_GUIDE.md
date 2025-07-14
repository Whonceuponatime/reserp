# SEACURE(CARE) Debugging Guide

## 🐛 When App Fails on Other Computers

If SEACURE(CARE) works on your development computer but fails on other computers, use these debugging methods to identify the issue.

---

## 🎯 Quick Debug Method (Recommended)

### **1. Build Debug Installer**
```batch
.\build_debug_installer.bat
```

### **2. Install on Target Computer**
- Copy `Installer\SEACURE_CARE_Setup_v1.0.0_DEBUG.exe` to the target computer
- Run the installer as administrator (if needed)
- The installer will create debug shortcuts automatically

### **3. Run with Debug Console**
- Use the **"SEACURE(CARE) Debug"** desktop shortcut
- A **black console window** will appear showing detailed startup information
- **Keep this console window open** while the app starts

### **4. Analyze Console Output**
The debug console will show:
```
=================================================
SEACURE(CARE) Maritime ERP - Debug Mode
=================================================
Application started at: 1/14/2025 2:30:45 PM
Command line args: --debug
Working directory: C:\Program Files\SEACURE(CARE) Debug
Application directory: C:\Program Files\SEACURE(CARE) Debug\
User data directory: C:\Users\Username\AppData\Roaming\SEACURE(CARE)
=================================================

[STARTUP] Starting application host...
[STARTUP] Host started successfully
[STARTUP] Initializing database...
[DATABASE] Initializing database...
[DATABASE] Database connection string: Data Source=C:\Users\Username\AppData\Roaming\SEACURE(CARE)\Database\maritime_erp.db
[DATABASE] Setting command timeout to 30 seconds...
[DATABASE] Ensuring database is created...
[DATABASE] Database creation completed successfully
[DATABASE] Database initialization completed successfully
[STARTUP] Database initialized successfully
[STARTUP] Showing login window...
[STARTUP] Application startup completed successfully
=================================================
```

---

## 🔧 Manual Debug Methods

### **Method 1: Command Line Arguments**
On any installed version, run from Command Prompt:
```cmd
cd "C:\Program Files\SEACURE(CARE)"
MaritimeERP.Desktop.exe --debug
```

### **Method 2: Run with -d Flag**
```cmd
MaritimeERP.Desktop.exe -d
```

---

## 🚨 Common Error Scenarios

### **Database Issues**
**Console shows:**
```
[DATABASE ERROR] Database initialization failed!
[DATABASE ERROR] Exception: SqliteException
[DATABASE ERROR] Message: SQLite Error 14: 'unable to open database file'
```
**Solution:** Check user permissions on `%APPDATA%\SEACURE(CARE)\Database\` folder

### **Missing .NET Runtime**
**Console shows:**
```
[ERROR] Application startup failed!
[ERROR] Exception: DllNotFoundException
[ERROR] Message: Unable to load DLL 'hostfxr'
```
**Solution:** This shouldn't happen with single-file deployment, but check if Windows is up to date

### **File Access Issues**
**Console shows:**
```
[DATABASE ERROR] Message: Access to the path 'C:\Users\...\maritime_erp.db' is denied
```
**Solution:** Run as administrator or check antivirus software

### **Configuration Issues**
**Console shows:**
```
[STARTUP] Starting application host...
[ERROR] Exception: FileNotFoundException
[ERROR] Message: Could not load file or assembly...
```
**Solution:** Reinstall the application

---

## 📋 Debugging Checklist

When app fails on target computer:

1. **✅ Build debug installer:** `.\build_debug_installer.bat`
2. **✅ Install on target computer**
3. **✅ Run "SEACURE(CARE) Debug" shortcut**
4. **✅ Screenshot/copy console output**
5. **✅ Note exactly where it fails:**
   - During host startup?
   - During database initialization?
   - During login window creation?
6. **✅ Check target computer details:**
   - Windows version
   - Available disk space
   - User permissions
   - Antivirus software

---

## 📤 Sending Debug Information

**Include this information:**
1. **Console output** (copy/paste text or screenshot)
2. **Target computer specs:**
   - Windows version (run `winver`)
   - Available space on C: drive
   - User account type (admin/standard)
3. **Error behavior:**
   - Does it crash immediately?
   - Does it hang at a specific step?
   - Any error dialog boxes?

---

## 🏗️ Available Installers

| Installer Type | Use Case | Debug Console |
|----------------|----------|---------------|
| `SEACURE_CARE_Setup_v1.0.0_SingleFile.exe` | **Production** | No |
| `SEACURE_CARE_Setup_v1.0.0_DEBUG.exe` | **Debugging** | Yes (automatic) |
| `SEACURE_CARE_Setup_v1.0.0.exe` | Legacy (messy files) | No |

**For troubleshooting: Always use the DEBUG installer!** 🎯

---

## 💡 Debug Console Commands

While debug console is open:
- **View output:** Scroll up to see startup sequence
- **Keep alive:** Don't close console window until app fully loads
- **Copy text:** Right-click → Select All → Copy
- **Take screenshot:** If text copying doesn't work

The console will automatically close when the application exits. 