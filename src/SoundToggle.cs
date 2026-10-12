using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SoundToggleWin11 {

    public enum EDataFlow { Render = 0, Capture = 1, All = 2 }
    public enum ERole { Console = 0, Multimedia = 1, Communications = 2 }
    public enum EDeviceState { Active = 1, All = 15 }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyKey {
        public Guid fmtid;
        public uint pid;
        public PropertyKey(Guid f, uint p) { fmtid = f; pid = p; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr value;

        public string AsString() {
            if (vt == 31 && value != IntPtr.Zero) return Marshal.PtrToStringUni(value);
            return null;
        }

        public uint AsUInt() {
            if (vt == 21 || vt == 22 || vt == 19 || vt == 20 || vt == 17) return (uint)value.ToInt64();
            if (vt == 37 && value != IntPtr.Zero) return (uint)Marshal.ReadInt32(value);
            return 0;
        }
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore {
        int GetCount(out int cProps);
        int GetAt(int iProp, out PropertyKey key);
        int GetValue(ref PropertyKey key, out PropVariant v);
        int SetValue(ref PropertyKey key, ref PropVariant v);
        int Commit();
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice {
        int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int GetState(out int state);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceCollection {
        int GetCount(out int count);
        int Item(int index, out IMMDevice device);
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator {
        int EnumAudioEndpoints(EDataFlow flow, EDeviceState mask, out IMMDeviceCollection devices);
        int GetDefaultAudioEndpoint(EDataFlow flow, ERole role, out IMMDevice device);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        int RegisterEndpointNotificationCallback(IntPtr client);
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumeratorObject { }

    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPolicyConfig {
        int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr format);
        int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, int bDefault, IntPtr format);
        int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id);
        int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr format, IntPtr mixFormat);
        int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, int bDefault, IntPtr dflt, IntPtr min);
        int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr period);
        int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, ref PropertyKey key, out PropVariant v);
        int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, ref PropertyKey key, ref PropVariant v);
        int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, ERole role);
        int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string id, int visible);
    }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    public class PolicyConfigObject { }

    public static class Audio {
        static readonly Guid PKEY_Device = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0");
        const uint PID_FriendlyName = 14;

        [DllImport("ole32.dll")]
        static extern int PropVariantClear(ref PropVariant pv);

        public static List<string[]> Devices(EDataFlow flow) {
            var en = (IMMDeviceEnumerator)(new MMDeviceEnumeratorObject());
            IMMDeviceCollection col;
            Marshal.ThrowExceptionForHR(en.EnumAudioEndpoints(flow, EDeviceState.Active, out col));

            int count;
            Marshal.ThrowExceptionForHR(col.GetCount(out count));

            var result = new List<string[]>(count);
            for (int i = 0; i < count; i++) {
                IMMDevice dev;
                Marshal.ThrowExceptionForHR(col.Item(i, out dev));

                string id = null;
                Marshal.ThrowExceptionForHR(dev.GetId(out id));

                string name = id;
                IPropertyStore store;
                if (dev.OpenPropertyStore(0, out store) == 0) {
                    PropVariant pv;
                    PropertyKey k1 = new PropertyKey(PKEY_Device, PID_FriendlyName);
                    if (store.GetValue(ref k1, out pv) == 0) {
                        try { var s = pv.AsString(); if (!string.IsNullOrWhiteSpace(s)) name = s; }
                        finally { PropVariantClear(ref pv); }
                    }
                }
                result.Add(new string[] { id, name, "0" });
            }
            return result;
        }

        public static string DefaultId(EDataFlow flow) { return DefaultIdForRole(flow, ERole.Console); }
        public static string DefaultIdForRole(EDataFlow flow, ERole role) {
            var en = (IMMDeviceEnumerator)(new MMDeviceEnumeratorObject());
            IMMDevice dev;
            if (en.GetDefaultAudioEndpoint(flow, role, out dev) != 0) return null;
            string id;
            if (dev.GetId(out id) != 0) return null;
            return id;
        }

        public static int SetDefault(string id, ERole role) {
            var policy = (IPolicyConfig)(new PolicyConfigObject());
            return policy.SetDefaultEndpoint(id, role);
        }

        public static void SetDefaultAll(string id) {
            var policy = (IPolicyConfig)(new PolicyConfigObject());
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(id, ERole.Console));
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(id, ERole.Multimedia));
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(id, ERole.Communications));
        }
    }
}
namespace SoundToggleWin11 {
 public static class CMediaRouting {
  public delegate bool WindowCallback(System.IntPtr h, System.IntPtr data);
  [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool EnumWindows(WindowCallback callback,System.IntPtr data);
  [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(System.IntPtr h,out uint pid);
  [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern System.IntPtr GetDlgItem(System.IntPtr parent,int id);
  [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowEnabled(System.IntPtr h);
  [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern System.IntPtr SendMessage(System.IntPtr h,uint msg,System.IntPtr w,System.IntPtr l);
  [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,EntryPoint="SendMessageW")] static extern System.IntPtr ReadText(System.IntPtr h,uint msg,System.IntPtr w,System.Text.StringBuilder text);
  [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ShowWindow(System.IntPtr h,int cmd);
  public static System.IntPtr Find(uint pid) {
   System.IntPtr found=System.IntPtr.Zero;
   EnumWindows(delegate(System.IntPtr h,System.IntPtr data) { uint owner;GetWindowThreadProcessId(h,out owner);if(owner==pid && GetDlgItem(h,1000)!=System.IntPtr.Zero) {found=h;return false;}return true;},System.IntPtr.Zero);
   return found;
  }
  public static string Option(System.IntPtr window,int index) { var text=new System.Text.StringBuilder(1024);ReadText(GetDlgItem(window,1000),0x148,new System.IntPtr(index),text);return text.ToString(); }
  public static int Selected(System.IntPtr window,int id) { return SendMessage(GetDlgItem(window,id),0x147,System.IntPtr.Zero,System.IntPtr.Zero).ToInt32(); }
  public static void SetPlayback(System.IntPtr window,int index) {
   var combo=GetDlgItem(window,1000);
   if(combo==System.IntPtr.Zero || !IsWindowEnabled(window) || !IsWindowEnabled(combo)) throw new System.InvalidOperationException("C-Media's Playback control is disabled. For connected 3.5 mm earphones, unplug the audio plug for five seconds and reinsert it fully, then retry. This is a one-time jack-detection recovery, not required for normal toggling. If the control remains disabled, check the ASUS C-Media driver/control panel. No playback switch was performed.");
   if(Option(window,0)!="Speaker / Headset / Headphone (External)" || Option(window,1)!="Speaker (Built-in)") throw new System.InvalidOperationException("Unexpected C-Media playback controls; refusing to change unrelated settings.");
   if(index!=0 && index!=1) throw new System.ArgumentOutOfRangeException("index");
   if(SendMessage(combo,0x14E,new System.IntPtr(index),System.IntPtr.Zero).ToInt32()!=index) throw new System.InvalidOperationException("C-Media playback selection failed.");
   SendMessage(window,0x111,new System.IntPtr((1<<16)|1000),combo);
   if(Selected(window,1000)!=index) throw new System.InvalidOperationException("C-Media did not retain playback routing.");
  }
 }
}
namespace SoundToggleWin11 {
 public class Settings {
  public string Mode {get;set;} public string HeadphonesId {get;set;} public string HeadphonesName {get;set;} public string SpeakersId {get;set;} public string SpeakersName {get;set;}
 }
 public sealed class Output { public string Id;public string Name;public override string ToString(){return Name;} }
 public static class Program {
  static string home; static bool noUI; static System.Collections.Generic.List<string> messages=new System.Collections.Generic.List<string>();
  static readonly System.Web.Script.Serialization.JavaScriptSerializer json=new System.Web.Script.Serialization.JavaScriptSerializer();
  static void Log(string text){messages.Add(text);System.Console.WriteLine(text);}
  static bool Has(string[] args,string flag){foreach(string a in args)if(a.Equals(flag,System.StringComparison.OrdinalIgnoreCase))return true;return false;}
  static string Value(string[] args,string prefix){foreach(string a in args)if(a.StartsWith(prefix,System.StringComparison.OrdinalIgnoreCase))return a.Substring(prefix.Length);return null;}
  static System.Collections.Generic.List<Output> Outputs(){var list=new System.Collections.Generic.List<Output>();foreach(var d in Audio.Devices(EDataFlow.Render))list.Add(new Output{Id=d[0],Name=d[1]});return list;}
  static Output Find(System.Collections.Generic.List<Output> list,string id){foreach(var d in list)if(d.Id==id)return d;return null;}
  static bool Physical(Output d){return !System.Text.RegularExpressions.Regex.IsMatch(d.Name,"FxSound|ASUS Utility|Noise-cancelling|Voicemeeter|VB-Audio",System.Text.RegularExpressions.RegexOptions.IgnoreCase);}
  static System.IntPtr Panel(){
   foreach(var p in System.Diagnostics.Process.GetProcessesByName("CMediaAudioControlPanel")){using(p){var h=CMediaRouting.Find((uint)p.Id);if(h!=System.IntPtr.Zero)return h;}}
   var start=new System.Diagnostics.ProcessStartInfo("explorer.exe","shell:AppsFolder\\E2B691C9.C-Media6549AudioControlPanel_y9kx1840xc4zw!CMEDIAAUDIOCONTROLPANEL");start.WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden;start.UseShellExecute=true;System.Diagnostics.Process.Start(start);
   for(int i=0;i<40;i++){System.Threading.Thread.Sleep(250);foreach(var p in System.Diagnostics.Process.GetProcessesByName("CMediaAudioControlPanel")){using(p){var h=CMediaRouting.Find((uint)p.Id);if(h!=System.IntPtr.Zero){CMediaRouting.ShowWindow(h,0);return h;}}}}
   throw new System.InvalidOperationException("The installed ASUS C-Media Audio Control Panel is required for physical 3.5 mm jack/speaker routing. Other audio chipsets need their own supported driver controls.");
  }
  static void Route(int index){var h=Panel();int mic=CMediaRouting.Selected(h,1001);CMediaRouting.SetPlayback(h,index);System.Threading.Thread.Sleep(350);if(CMediaRouting.Selected(h,1000)!=index)throw new System.InvalidOperationException("Physical playback routing verification failed.");if(CMediaRouting.Selected(h,1001)!=mic)throw new System.InvalidOperationException("Recording routing changed unexpectedly.");}
  internal static bool EffectiveMatch(string actualId,Output target,System.Collections.Generic.List<Output> devices,bool allowFx,bool fxRunning,string fxOutputName){
   if(string.Equals(actualId,target.Id,System.StringComparison.OrdinalIgnoreCase))return true;
   if(!allowFx||!fxRunning||string.IsNullOrEmpty(fxOutputName))return false;
   var actual=Find(devices,actualId);if(actual==null||actual.Name.IndexOf("FxSound",System.StringComparison.OrdinalIgnoreCase)<0)return false;
   int matches=0;Output matched=null;foreach(var device in devices)if(string.Equals(device.Name,fxOutputName,System.StringComparison.OrdinalIgnoreCase)){matches++;matched=device;}
   return matches==1&&matched!=null&&string.Equals(matched.Id,target.Id,System.StringComparison.OrdinalIgnoreCase);
  }
  static string FxOutputName(){
   try{
    string file=System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),"FxSound","FxSound.settings");
    var settings=new System.Xml.XmlReaderSettings();settings.DtdProcessing=System.Xml.DtdProcessing.Prohibit;settings.XmlResolver=null;
    using(var reader=System.Xml.XmlReader.Create(file,settings)){var document=new System.Xml.XmlDocument();document.XmlResolver=null;document.Load(reader);var node=document.SelectSingleNode("/PROPERTIES/VALUE[@name='output_device_name']");return node==null?null:node.Attributes["val"].Value;}
   }catch{return null;}
  }
  static bool FxRunning(){var processes=System.Diagnostics.Process.GetProcessesByName("FxSound");bool running=processes.Length>0;foreach(var process in processes)process.Dispose();return running;}
  static void VerifyPlayback(Output target,bool physicalJack){
   int consecutive=0;string last="";bool throughFx=false;var timer=System.Diagnostics.Stopwatch.StartNew();
   while(timer.ElapsedMilliseconds<2500){
    System.Threading.Thread.Sleep(200);var devices=Outputs();string fxName=FxOutputName();bool fxRunning=FxRunning();bool valid=true;bool sampleFx=false;
    foreach(ERole role in System.Enum.GetValues(typeof(ERole))){string actual=Audio.DefaultIdForRole(EDataFlow.Render,role);if(!EffectiveMatch(actual,target,devices,physicalJack,fxRunning,fxName)){valid=false;last=role+": expected "+target.Id+", actual "+(actual??"<unavailable>");}else if(actual!=target.Id){sampleFx=true;}}
    consecutive=valid?consecutive+1:0;throughFx=sampleFx;
    if(consecutive>=3){if(throughFx)Log("Verified physical playback through FxSound -> "+target.Name+"; enhancer remains running.");else Log("Verified all playback roles on "+target.Name);return;}
   }
   throw new System.InvalidOperationException("Playback did not reach the selected output. "+last+". Check the output selected in your audio software.");
  }
  static void Save(Settings s){System.IO.File.WriteAllText(System.IO.Path.Combine(home,"devices.json"),json.Serialize(s),new System.Text.UTF8Encoding(false));}
  static Settings Load(System.Collections.Generic.List<Output> devices){
   string file=System.IO.Path.Combine(home,"devices.json");if(!System.IO.File.Exists(file))return null;var s=json.Deserialize<Settings>(System.IO.File.ReadAllText(file));
   if(s==null||string.IsNullOrEmpty(s.HeadphonesId)||string.IsNullOrEmpty(s.SpeakersId)||(s.Mode!="CmediaJack"&&s.HeadphonesId==s.SpeakersId))throw new System.InvalidOperationException("Invalid configuration. Run SoundToggle.exe -Setup.");return s;
  }
  static Settings Auto(System.Collections.Generic.List<Output> devices){
   var onboard=new System.Collections.Generic.List<Output>();foreach(var d in devices)if(d.Name.IndexOf("C-Media",System.StringComparison.OrdinalIgnoreCase)>=0)onboard.Add(d);
   using(var hardware=Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USB\\VID_0B05&PID_6210&MI_00")){if(hardware==null||onboard.Count!=1)return null;}
   var h=Panel();if(CMediaRouting.Option(h,0)!="Speaker / Headset / Headphone (External)"||CMediaRouting.Option(h,1)!="Speaker (Built-in)")throw new System.InvalidOperationException("This C-Media driver has different controls; automatic physical routing is not supported.");
   var s=new Settings{Mode="CmediaJack",HeadphonesId=onboard[0].Id,SpeakersId=onboard[0].Id,HeadphonesName="3.5 mm earphones",SpeakersName="Built-in speakers"};Save(s);Log("Detected this PC's onboard C-Media routing; saved local configuration.");return s;
  }
  static Settings Setup(System.Collections.Generic.List<Output> devices){
   if(noUI)throw new System.InvalidOperationException("This PC needs device selection. Run SoundToggle.exe -Setup without -NoUI.");var physical=new System.Collections.Generic.List<Output>();foreach(var d in devices)if(Physical(d))physical.Add(d);if(physical.Count<2)throw new System.InvalidOperationException("Two separate playback outputs are required for USB/Bluetooth switching. Shared 3.5 mm jack routing is supported automatically only on the detected ASUS C-Media hardware, not universally on every PC.");
   using(var form=new System.Windows.Forms.Form()){form.Text="Sound Toggle — choose this PC's devices";form.ClientSize=new System.Drawing.Size(600,235);form.StartPosition=System.Windows.Forms.FormStartPosition.CenterScreen;form.FormBorderStyle=System.Windows.Forms.FormBorderStyle.FixedDialog;form.MaximizeBox=false;form.MinimizeBox=false;
    var note=new System.Windows.Forms.Label{Text="Select actual earphones and speakers. USB/USB-C headsets must be connected. Microphones are never switched.",Left=15,Top=10,Width=570,Height=40};form.Controls.Add(note);
    form.Controls.Add(new System.Windows.Forms.Label{Text="Earphones / headphones",Left=15,Top=55,Width=560});var hp=new System.Windows.Forms.ComboBox{Left=15,Top=80,Width=570,DropDownStyle=System.Windows.Forms.ComboBoxStyle.DropDownList};form.Controls.Add(hp);
    form.Controls.Add(new System.Windows.Forms.Label{Text="Internal speakers",Left=15,Top=115,Width=560});var sp=new System.Windows.Forms.ComboBox{Left=15,Top=140,Width=570,DropDownStyle=System.Windows.Forms.ComboBoxStyle.DropDownList};form.Controls.Add(sp);foreach(var d in physical){hp.Items.Add(d);sp.Items.Add(d);}
    var save=new System.Windows.Forms.Button{Text="Save",Left=375,Top=190,Width=100};save.Click+=delegate{if(hp.SelectedIndex<0||sp.SelectedIndex<0||hp.SelectedIndex==sp.SelectedIndex){System.Windows.Forms.MessageBox.Show("Choose two different outputs.","Sound Toggle");return;}form.DialogResult=System.Windows.Forms.DialogResult.OK;};form.Controls.Add(save);
    var cancel=new System.Windows.Forms.Button{Text="Cancel",Left=485,Top=190,Width=100,DialogResult=System.Windows.Forms.DialogResult.Cancel};form.Controls.Add(cancel);form.CancelButton=cancel;if(form.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return null;
    var a=(Output)hp.SelectedItem;var b=(Output)sp.SelectedItem;var s=new Settings{Mode="Endpoints",HeadphonesId=a.Id,HeadphonesName=a.Name,SpeakersId=b.Id,SpeakersName=b.Name};Save(s);return s;
   }
  }
  static void Shortcut(string path,string target,string arguments){dynamic shell=System.Activator.CreateInstance(System.Type.GetTypeFromProgID("WScript.Shell"));dynamic link=shell.CreateShortcut(path);link.TargetPath=target;link.Arguments=arguments;link.WorkingDirectory=System.IO.Path.GetDirectoryName(target);link.IconLocation=System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.System),"mmsys.cpl")+",0";link.Description="Toggle earphones and built-in speakers";link.Save();System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
  static void Install(){
   string folder=System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),"Programs","SoundToggle");System.IO.Directory.CreateDirectory(folder);string target=System.IO.Path.Combine(folder,"SoundToggle.exe");string current=System.Reflection.Assembly.GetExecutingAssembly().Location;if(!current.Equals(target,System.StringComparison.OrdinalIgnoreCase))System.IO.File.Copy(current,target,true);
   Shortcut(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),"Toggle Sound.lnk"),target,"");string menu=System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Programs),"Sound Toggle");System.IO.Directory.CreateDirectory(menu);Shortcut(System.IO.Path.Combine(menu,"Toggle Sound.lnk"),target,"");Shortcut(System.IO.Path.Combine(menu,"Configure Sound Toggle.lnk"),target,"-Setup");Log("Installed: "+target);if(!noUI)System.Windows.Forms.MessageBox.Show("Installed for your Windows account. Desktop and Start menu shortcuts are ready.\n\nRun Toggle Sound to use it. Device settings are detected or selected on this PC; no settings from another PC are copied.","Sound Toggle");
  }
  [System.STAThread] public static int Main(string[] args){
   noUI=Has(args,"-NoUI");home=Value(args,"--config-dir=")??System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),"SoundToggle");home=System.IO.Path.GetFullPath(home);bool locked=false;System.Threading.Mutex mutex=null;
   try{System.IO.Directory.CreateDirectory(home);System.Windows.Forms.Application.EnableVisualStyles();if(Has(args,"--install")){Install();return 0;}
    foreach(string arg in args)if(!arg.StartsWith("--config-dir=",System.StringComparison.OrdinalIgnoreCase)&&!Has(new[]{"-NoUI","-Setup","-List","-Speakers","-Earphones"},arg))throw new System.ArgumentException("Unknown argument: "+arg);
    mutex=new System.Threading.Mutex(false,"Local\\SoundTogglePlayback");try{locked=mutex.WaitOne(15000);}catch(System.Threading.AbandonedMutexException){locked=true;}if(!locked)throw new System.InvalidOperationException("Another switch is still running.");
    var devices=Outputs();if(Has(args,"-List")){foreach(var d in devices)Log(d.Name+" | "+d.Id);return 0;}bool speakers=Has(args,"-Speakers"),earphones=Has(args,"-Earphones");if(speakers&&earphones)throw new System.ArgumentException("Use either -Speakers or -Earphones.");
    Settings pair=Load(devices);if(Has(args,"-Setup")){Setup(devices);return 0;}if(pair==null)pair=Auto(devices)??Setup(devices);if(pair==null)return 0;
    string current=Audio.DefaultId(EDataFlow.Render);Output target;string label;
    if(pair.Mode=="CmediaJack"){target=Find(devices,pair.SpeakersId);if(target==null)throw new System.InvalidOperationException("The saved onboard output is unavailable. Configure this PC again.");int route=speakers?1:earphones?0:(CMediaRouting.Selected(Panel(),1000)==1?0:1);Route(route);label=route==1?"Built-in speakers":"3.5 mm earphones";}
    else {string id=speakers?pair.SpeakersId:earphones?pair.HeadphonesId:(current==pair.HeadphonesId?pair.SpeakersId:pair.HeadphonesId);target=Find(devices,id);if(target==null)throw new System.InvalidOperationException("The selected output is disconnected or disabled. No unrelated output was selected.");if(id==pair.SpeakersId&&target.Name.IndexOf("C-Media",System.StringComparison.OrdinalIgnoreCase)>=0){using(var hardware=Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USB\\VID_0B05&PID_6210&MI_00")){if(hardware!=null)Route(1);}}label=target.Name;}
    Audio.SetDefaultAll(target.Id);VerifyPlayback(target,pair.Mode=="CmediaJack");
    if(pair.Mode=="CmediaJack"){int expected=label=="Built-in speakers"?1:0;if(CMediaRouting.Selected(Panel(),1000)!=expected)throw new System.InvalidOperationException("Physical playback route changed during verification.");}
    Log("Switched to: "+label);return 0;
   }catch(System.Exception e){Log("ERROR: "+e.Message);if(!noUI)System.Windows.Forms.MessageBox.Show(e.Message,"Sound Toggle",System.Windows.Forms.MessageBoxButtons.OK,System.Windows.Forms.MessageBoxIcon.Warning);return 1;}
   finally{if(locked)mutex.ReleaseMutex();if(mutex!=null)mutex.Dispose();try{System.IO.File.WriteAllLines(System.IO.Path.Combine(home,"last-run.log"),messages,new System.Text.UTF8Encoding(false));}catch{}}
  }
 }
}
