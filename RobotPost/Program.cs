using static System.Console;

if (args.Length == 0) { WriteLine ("No input!"); ReadKey (); return; }
var rpp = new RobotPost (args[0]);

Console.Write ("Enter the hard code files directory: ");
string hcPath = Console.ReadLine ();
if (!Directory.Exists (hcPath)) { Console.WriteLine ("Invalid directory"); return; }
if (!File.Exists (hcPath + "/BendLS_NoRegripHC.txt")
     || !File.Exists (hcPath + "/BendLS_RegripHC.txt")
     || !File.Exists (hcPath + "/BendSub_HC(PG).txt")
     || !File.Exists (hcPath + "/BendSub_HC(VG).txt")
     || !File.Exists (hcPath + "/Header.txt")
     || !File.Exists (hcPath + "/MainLS_HC(PG).txt")
     || !File.Exists (hcPath + "/MainLS_HC(VG).txt")) {
   Console.WriteLine ("One or many required hard code files does not exist.");
   return;
}

rpp.GenOutputFiles ();
WriteLine ("File generated successfully.");
ReadKey ();