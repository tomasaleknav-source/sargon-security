using System;using System.IO;using System.Text;using System.Diagnostics;using System.Reflection;using System.Threading;using SargasPC;
class CommandTests {
 static int cases;
 static ProcessStartInfo Child(string mode){string exe=Assembly.GetExecutingAssembly().Location;bool mono=Type.GetType("Mono.Runtime")!=null;return new ProcessStartInfo(mono?(Environment.GetEnvironmentVariable("SARGAS_TEST_MONO")??"mono"):exe,(mono?"\""+exe+"\" ":"")+mode){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};}
 static void Check(bool value){cases++;if(!value)throw new Exception("Command case "+cases);}
 static void Reject(string mode,int timeout,Func<bool> cancel){var clock=Stopwatch.StartNew();bool rejected=false;try{BoundedCommand.Run(Child(mode),timeout,cancel);}catch(IOException){rejected=true;}Check(rejected);Check(clock.ElapsedMilliseconds<timeout+1000);}
 static void Main(string[] args){if(args.Length>0){switch(args[0]){case "ok":Console.Write("žinutė");return;case "limit":Console.Write(new string('x',16384));return;case "flood":Console.Write(new string('x',1000000));return;case "errorflood":Console.Error.Write(new string('x',1000000));return;case "both":Console.Error.Write(new string('e',16384));Console.Write(new string('x',16384));return;case "exit":Console.Error.Write("private fixture detail");Environment.Exit(3);return;case "sleep":Thread.Sleep(2500);return;case "inherited":var child=Child("sleep");child.RedirectStandardOutput=false;child.RedirectStandardError=false;using(var p=Process.Start(child)){}return;}}
 Check(BoundedCommand.Run(Child("ok"),3000,()=>false)=="žinutė");Check(BoundedCommand.Run(Child("limit"),3000,()=>false).Length==16384);Check(BoundedCommand.Run(Child("both"),3000,()=>false).Length==16384);
 Reject("flood",3000,()=>false);Reject("errorflood",3000,()=>false);Reject("exit",3000,()=>false);Reject("sleep",150,()=>false);Reject("inherited",200,()=>false);var timer=Stopwatch.StartNew();Reject("sleep",3000,()=>timer.ElapsedMilliseconds>150);Reject("ok",3000,()=>true);
 bool leaked=false;try{BoundedCommand.Run(Child("exit"),3000,()=>false);}catch(IOException e){leaked=e.Message.Contains("private fixture");}Check(!leaked);Console.WriteLine("PC CommandTests: "+cases+" checks passed (bounded stdout/stderr, deadline through pipe EOF, cancellation, error privacy; no Defender invocation).");
 }
}
