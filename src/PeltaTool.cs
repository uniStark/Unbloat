// PeltaTool - a single lightweight Windows tray app that replaces Armoury Crate /
// Logitech G HUB / Creative App / MSI Center for the things people actually use:
//   * auto-switch Equalizer APO profiles by foreground game (footstep EQ vs music EQ)
//   * per-device EQ (e.g. ROG Pelta headset + Creative T60 speakers)
//   * live default-device volume readout (Core Audio)
//   * Logitech wireless mouse battery via HID++ (no G HUB needed)
//
// EQ profiles live next to the exe under .\eq\ ; the active config is written to
// EqualizerAPO's config.txt (which it auto-reloads). See README for setup.

using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;

class PeltaTool {
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);

  // EQ profile templates + game list live next to the exe.
  static readonly string EqDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "eq");
  // Standard EqualizerAPO install location (adjust if you installed it elsewhere).
  const string ApoDir = @"C:\Program Files\EqualizerAPO\config";
  static string ConfigTxt = ApoDir + @"\config.txt";
  static string GamesF = Path.Combine(EqDir, "games.txt");

  static NotifyIcon tray;
  static System.Windows.Forms.Timer timer;
  static string mode = "", ovr = "Auto";
  static HashSet<string> games = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  static ToolStripMenuItem miStatus, miVol, miBat, miAuto, miFps, miDef;
  static volatile string batText = "Mouse: ...";

  [STAThread]
  static void Main() {
    Application.EnableVisualStyles();
    LoadGames();
    Vol.Init();
    tray = new NotifyIcon(); tray.Icon = SystemIcons.Application; tray.Visible = true;
    ContextMenuStrip m = new ContextMenuStrip();
    miStatus = new ToolStripMenuItem("Headset EQ: ..."); miStatus.Enabled = false;
    miVol = new ToolStripMenuItem("Volume: ..."); miVol.Enabled = false;
    miBat = new ToolStripMenuItem("Mouse: ..."); miBat.Enabled = false;
    miAuto = new ToolStripMenuItem("Auto (switch by game)"); miAuto.Click += delegate { ovr="Auto"; Apply(true); };
    miFps = new ToolStripMenuItem("Force FPS (footsteps)"); miFps.Click += delegate { ovr="FPS"; Apply(true); };
    miDef = new ToolStripMenuItem("Force Default (music/movie)"); miDef.Click += delegate { ovr="Default"; Apply(true); };
    ToolStripMenuItem miEdit = new ToolStripMenuItem("Edit game list..."); miEdit.Click += delegate { try{Process.Start("notepad.exe",GamesF);}catch{} };
    ToolStripMenuItem miReload = new ToolStripMenuItem("Reload game list"); miReload.Click += delegate { LoadGames(); };
    ToolStripMenuItem miExit = new ToolStripMenuItem("Exit"); miExit.Click += delegate { tray.Visible=false; Application.Exit(); };
    m.Items.Add(miStatus); m.Items.Add(miVol); m.Items.Add(miBat);
    m.Items.Add(new ToolStripSeparator());
    m.Items.Add(miAuto); m.Items.Add(miFps); m.Items.Add(miDef);
    m.Items.Add(new ToolStripSeparator());
    m.Items.Add(miEdit); m.Items.Add(miReload);
    m.Items.Add(new ToolStripSeparator()); m.Items.Add(miExit);
    tray.ContextMenuStrip = m;

    Thread bt = new Thread(BatLoop); bt.IsBackground = true; bt.Start();
    timer = new System.Windows.Forms.Timer(); timer.Interval = 1500; timer.Tick += delegate { Apply(false); }; timer.Start();
    Apply(true);
    Application.Run();
  }

  static void BatLoop() {
    while (true) {
      try { batText = Bat.Read(); } catch { batText = "Mouse: ?"; }
      Thread.Sleep(30000);
    }
  }

  static void LoadGames() {
    games.Clear();
    try {
      if (!File.Exists(GamesF))
        File.WriteAllText(GamesF, "# one game exe per line; foreground = FPS profile\r\ncs2.exe\r\ncsgo.exe\r\nr5apex.exe\r\nTslGame.exe\r\nvalorant.exe\r\n");
      foreach (string line in File.ReadAllLines(GamesF)) { string t=line.Trim(); if(t.Length==0||t.StartsWith("#"))continue; games.Add(t); }
    } catch {}
  }
  static string FgExe() {
    try { IntPtr h=GetForegroundWindow(); uint pid; GetWindowThreadProcessId(h,out pid); if(pid==0)return ""; return Process.GetProcessById((int)pid).ProcessName+".exe"; } catch { return ""; }
  }
  static void Apply(bool force) {
    string want = ovr=="FPS"?"FPS":(ovr=="Default"?"Default":(games.Contains(FgExe())?"FPS":"Default"));
    if (want!=mode || force) {
      mode=want;
      string inc = want=="FPS"?"pelta-fps.txt":"pelta-default.txt";
      try { File.WriteAllText(ConfigTxt, "Device: PELTA\r\nInclude: "+inc+"\r\nDevice: T60\r\nInclude: t60.txt\r\n"); } catch {}
    }
    miAuto.Checked=ovr=="Auto"; miFps.Checked=ovr=="FPS"; miDef.Checked=ovr=="Default";
    miStatus.Text="Headset EQ: "+mode+(ovr=="Auto"?" (auto)":" (forced)");
    string vs=Vol.Get(); miVol.Text = vs==null?"Volume: (n/a)":vs;
    miBat.Text = batText;
    if(tray!=null) tray.Text = "Pelta: "+mode;
  }
}

// ---- Logitech wireless mouse battery via HID++ 2.0 (BatteryVoltage feature 0x1001) ----
static class Bat {
  [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
  [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle h, ref HIDD_ATTRIBUTES a);
  [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
  [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
  [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, ref HIDP_CAPS c);
  [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr w, uint f);
  [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, uint i, ref SP_DID data);
  [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DID d, IntPtr det, uint dsz, ref uint req, IntPtr di);
  [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
  [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)] static extern SafeFileHandle CreateFile(string n, uint a, uint s, IntPtr sec, uint d, uint f, IntPtr t);
  [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteFile(SafeFileHandle h, byte[] b, uint n, out uint w, ref NativeOverlapped o);
  [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadFile(SafeFileHandle h, byte[] b, uint n, out uint r, ref NativeOverlapped o);
  [DllImport("kernel32.dll", SetLastError=true)] static extern bool GetOverlappedResult(SafeFileHandle h, ref NativeOverlapped o, out uint n, bool w);
  [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr CreateEvent(IntPtr a, bool m, bool i, IntPtr n);
  [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint ms);
  [DllImport("kernel32.dll")] static extern bool CancelIoEx(SafeFileHandle h, IntPtr o);
  [DllImport("kernel32.dll")] static extern bool ResetEvent(IntPtr h);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

  [StructLayout(LayoutKind.Sequential)] struct SP_DID { public int cbSize; public Guid g; public int flags; public IntPtr res; }
  [StructLayout(LayoutKind.Sequential)] struct HIDD_ATTRIBUTES { public int Size; public ushort VendorID, ProductID, VersionNumber; }
  [StructLayout(LayoutKind.Sequential)] struct HIDP_CAPS { public ushort Usage, UsagePage, InLen, OutLen, FeatLen;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=17)] public ushort[] Res;
    public ushort Nlc, Nibc, Nivc, Nidi, Nobc, Novc, Nodi, Nfbc, Nfvc, Nfdi; }

  // Logitech Lightspeed receiver (G Pro Wireless and many others). Change if needed.
  const ushort VID = 0x046D, PID = 0xC539;
  static string path = null;

  static string Find() {
    Guid g; HidD_GetHidGuid(out g);
    IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12 /*PRESENT|DEVICEINTERFACE*/);
    if (set == (IntPtr)(-1)) return null;
    try {
      SP_DID did = new SP_DID(); did.cbSize = Marshal.SizeOf(did);
      for (uint i=0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref did); i++) {
        uint sz=0; SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, ref sz, IntPtr.Zero);
        if (sz==0) continue;
        IntPtr det = Marshal.AllocHGlobal((int)sz);
        try {
          Marshal.WriteInt32(det, IntPtr.Size==8 ? 8 : 6);
          if (!SetupDiGetDeviceInterfaceDetail(set, ref did, det, sz, ref sz, IntPtr.Zero)) continue;
          string p = Marshal.PtrToStringUni(IntPtr.Add(det, 4));
          SafeFileHandle hh = CreateFile(p, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
          if (hh.IsInvalid) continue;
          HIDD_ATTRIBUTES at = new HIDD_ATTRIBUTES(); at.Size = Marshal.SizeOf(at);
          bool ga = HidD_GetAttributes(hh, ref at);
          int ol=0, up=0; IntPtr pp;
          if (HidD_GetPreparsedData(hh, out pp)) { HIDP_CAPS c=new HIDP_CAPS(); HidP_GetCaps(pp, ref c); HidD_FreePreparsedData(pp); ol=c.OutLen; up=c.UsagePage; }
          hh.Close();
          // HID++ long interface: vendor usage page 0xFF00, 20-byte reports.
          if (ga && at.VendorID==VID && at.ProductID==PID && up==0xFF00 && ol==20) return p;
        } finally { Marshal.FreeHGlobal(det); }
      }
    } finally { SetupDiDestroyDeviceInfoList(set); }
    return null;
  }

  // Approximate single-cell Li-ion voltage (mV) -> percentage.
  static int Pct(int mv) {
    int[] vv = {4200,4100,4000,3950,3900,3850,3800,3750,3700,3650,3600,3500,3400};
    int[] pp = {100,  85,  70,  62,  55,  50,  45,  37,  28,  18,  10,  3,   0};
    if (mv>=vv[0]) return 100;
    for (int i=0;i<vv.Length-1;i++){ if (mv<=vv[i] && mv>=vv[i+1]){ double f=(double)(mv-vv[i+1])/(vv[i]-vv[i+1]); return (int)Math.Round(pp[i+1]+f*(pp[i]-pp[i+1])); } }
    return 0;
  }

  public static string Read() {
    if (path==null) path = Find();
    if (path==null) return "Mouse: (off?)";
    SafeFileHandle h = CreateFile(path, 0x80000000|0x40000000, 3, IntPtr.Zero, 3, 0x40000000 /*OVERLAPPED*/, IntPtr.Zero);
    if (h.IsInvalid) { path=null; return "Mouse: (busy)"; }
    IntPtr rE = CreateEvent(IntPtr.Zero,false,false,IntPtr.Zero), wE = CreateEvent(IntPtr.Zero,false,false,IntPtr.Zero);
    try {
      byte[] f = Q(h,rE,wE,(byte)0x00,(byte)0x10,(byte)0x01); // IRoot.getFeature(0x1001 BatteryVoltage)
      if (f==null || f[4]==0) { path=null; return "Mouse: ?"; }
      byte[] b = Q(h,rE,wE,f[4],(byte)0,(byte)0);             // BatteryVoltage.getBatteryVoltage
      if (b==null) return "Mouse: ?";
      int mv = (b[4]<<8)|b[5];
      return "Mouse: " + Pct(mv) + "% (" + mv + "mV)";
    } finally { h.Close(); CloseHandle(rE); CloseHandle(wE); }
  }

  // Send a HID++ 2.0 long request (device index 1, software id 0x0A) and read the matching reply.
  static byte[] Q(SafeFileHandle h, IntPtr rE, IntPtr wE, byte fi, byte p4, byte p5) {
    byte[] req = new byte[20]; req[0]=0x11; req[1]=0x01; req[2]=fi; req[3]=0x0A; req[4]=p4; req[5]=p5;
    var wo = new NativeOverlapped(); wo.EventHandle=wE; ResetEvent(wE); uint w;
    if (!WriteFile(h, req, 20, out w, ref wo)) { if (Marshal.GetLastWin32Error()==997){ WaitForSingleObject(wE,1000);} else return null; }
    int dl = Environment.TickCount + 1500;
    while (Environment.TickCount < dl) {
      byte[] buf = new byte[20]; var ro = new NativeOverlapped(); ro.EventHandle=rE; ResetEvent(rE); uint r;
      bool ok = ReadFile(h, buf, 20, out r, ref ro);
      if (!ok) { int e=Marshal.GetLastWin32Error();
        if (e==997){ uint wr=WaitForSingleObject(rE,(uint)Math.Max(1,dl-Environment.TickCount)); if(wr!=0){CancelIoEx(h,IntPtr.Zero);return null;} if(!GetOverlappedResult(h,ref ro,out r,false))return null; }
        else return null; }
      if (buf[0]==req[0]&&buf[1]==req[1]&&buf[2]==req[2]&&buf[3]==req[3]) return buf;
    }
    return null;
  }
}

// ---- Default playback device name + volume via Windows Core Audio ----
static class Vol {
  [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid c, IntPtr o, uint x, ref Guid i, out IntPtr p);
  static Guid CLSID = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
  static IMMDeviceEnumerator en; static Guid pkFmt = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
  public static void Init() { try { Guid iid=typeof(IMMDeviceEnumerator).GUID; IntPtr p; if(CoCreateInstance(ref CLSID,IntPtr.Zero,1,ref iid,out p)==0) en=(IMMDeviceEnumerator)Marshal.GetObjectForIUnknown(p); } catch {} }
  public static string Get() {
    if (en==null) return null;
    IMMDevice dev=null; object vo=null; IPropertyStore st=null;
    try {
      if (en.GetDefaultAudioEndpoint(0,0,out dev)!=0||dev==null) return null;
      Guid iidVol=typeof(IAudioEndpointVolume).GUID; dev.Activate(ref iidVol,1,IntPtr.Zero,out vo);
      IAudioEndpointVolume aev=(IAudioEndpointVolume)vo; float v; aev.GetMasterVolumeLevelScalar(out v);
      dev.OpenPropertyStore(0,out st); PROPERTYKEY k=new PROPERTYKEY(); k.fmtid=pkFmt; k.pid=14;
      PROPVARIANT pv; st.GetValue(ref k,out pv); string name=pv.vt==31?Marshal.PtrToStringUni(pv.p):"Device";
      return name+": "+(int)Math.Round(v*100)+"%";
    } catch { return null; }
    finally { if(st!=null)Marshal.ReleaseComObject(st); if(vo!=null)Marshal.ReleaseComObject(vo); if(dev!=null)Marshal.ReleaseComObject(dev); }
  }
  [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDeviceEnumerator {
    [PreserveSig] int EnumAudioEndpoints(int f,int m,out IntPtr d); [PreserveSig] int GetDefaultAudioEndpoint(int f,int r,out IMMDevice d); }
  [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDevice {
    [PreserveSig] int Activate(ref Guid iid,uint ctx,IntPtr p,[MarshalAs(UnmanagedType.IUnknown)] out object o); [PreserveSig] int OpenPropertyStore(uint a,out IPropertyStore s); [PreserveSig] int GetId(out IntPtr id); [PreserveSig] int GetState(out uint st); }
  [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IAudioEndpointVolume {
    [PreserveSig] int RegisterControlChangeNotify(IntPtr p); [PreserveSig] int UnregisterControlChangeNotify(IntPtr p); [PreserveSig] int GetChannelCount(out uint c);
    [PreserveSig] int SetMasterVolumeLevel(float l,ref Guid g); [PreserveSig] int SetMasterVolumeLevelScalar(float l,ref Guid g); [PreserveSig] int GetMasterVolumeLevel(out float l); [PreserveSig] int GetMasterVolumeLevelScalar(out float l); }
  [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPropertyStore {
    [PreserveSig] int GetCount(out uint c); [PreserveSig] int GetAt(uint i,out PROPERTYKEY k); [PreserveSig] int GetValue(ref PROPERTYKEY k,out PROPVARIANT v); [PreserveSig] int SetValue(ref PROPERTYKEY k,ref PROPVARIANT v); [PreserveSig] int Commit(); }
  [StructLayout(LayoutKind.Sequential)] struct PROPERTYKEY { public Guid fmtid; public uint pid; }
  [StructLayout(LayoutKind.Explicit)] struct PROPVARIANT { [FieldOffset(0)] public ushort vt; [FieldOffset(8)] public IntPtr p; }
}
