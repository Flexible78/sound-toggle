using System;using System.Collections.Generic;using SoundToggleWin11;
public static class VerificationTests {
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr CreateWindowEx(int ex,string cls,string title,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
 [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr h);
 [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool EnableWindow(IntPtr h,bool enabled);
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,EntryPoint="SendMessageW")] static extern IntPtr AddOption(IntPtr h,uint msg,IntPtr w,string text);
 static void DisabledPlaybackTest(){
  IntPtr window=CreateWindowEx(0,"STATIC","Routing regression fixture",0,0,0,100,100,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
  Check(window!=IntPtr.Zero,"native fixture window created");
  try{
   IntPtr combo=CreateWindowEx(0,"COMBOBOX","",0x40000003,0,0,100,100,window,new IntPtr(1000),IntPtr.Zero,IntPtr.Zero);
   Check(combo!=IntPtr.Zero,"native playback fixture created");
   AddOption(combo,0x143,IntPtr.Zero,"Speaker / Headset / Headphone (External)");AddOption(combo,0x143,IntPtr.Zero,"Speaker (Built-in)");
   SendMessage(combo,0x14E,new IntPtr(1),IntPtr.Zero);EnableWindow(combo,false);
   foreach(int target in new[]{0,1}){
    bool refused=false;try{CMediaRouting.SetPlayback(window,target);}catch(InvalidOperationException error){refused=error.Message.Contains("Playback control is disabled");}
    Check(refused,"disabled control rejects route "+target+", including unchanged selection");
    Check(SendMessage(combo,0x147,IntPtr.Zero,IntPtr.Zero).ToInt32()==1,"disabled control selection was not cosmetically changed");
   }
  }finally{DestroyWindow(window);}
 }
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 public static int Main(){
  DisabledPlaybackTest();
  var target=new Output{Id="physical",Name="Laptop speakers"};var fx=new Output{Id="fx",Name="FxSound Speakers"};var other=new Output{Id="other",Name="USB headset"};var list=new List<Output>{target,fx,other};
  Check(Program.EffectiveMatch("physical",target,list,false,false,null),"direct endpoint verification");
  Check(Program.EffectiveMatch("fx",target,list,true,true,"Laptop speakers"),"verified enhancer route in shared-jack mode");
  Check(!Program.EffectiveMatch("fx",target,list,false,true,"Laptop speakers"),"USB/Bluetooth mode cannot accept an enhancer redirect");
  Check(!Program.EffectiveMatch("fx",target,list,true,false,"Laptop speakers"),"stopped enhancer is rejected");
  Check(!Program.EffectiveMatch("fx",target,list,true,true,"USB headset"),"wrong enhancer output is rejected");
  Check(!Program.EffectiveMatch("fx",target,list,true,true,null),"unknown enhancer output is rejected");
  Check(!Program.EffectiveMatch("other",target,list,true,true,"Laptop speakers"),"unrelated default output is rejected");
  list.Add(new Output{Id="duplicate",Name="Laptop speakers"});Check(!Program.EffectiveMatch("fx",target,list,true,true,"Laptop speakers"),"ambiguous device names are rejected");return 0;
 }
}
