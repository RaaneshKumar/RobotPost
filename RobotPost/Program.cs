using static System.Console;

if (args.Length == 0) { WriteLine ("No input!"); ReadKey (); return; }
var rpp = new RobotPost (args[0]);

Console.Write ("Enter the hard code files directory: ");
string hcDir = Console.ReadLine ();
if (!Directory.Exists (hcDir)) { Console.WriteLine ("Invalid directory"); return; }
if (!File.Exists (hcDir + "/BendLS_NoRegripHC.txt")
     || !File.Exists (hcDir + "/BendLS_RegripHC.txt")
     || !File.Exists (hcDir + "/BendSub_HC(PG).txt")
     || !File.Exists (hcDir + "/BendSub_HC(VG).txt")
     || !File.Exists (hcDir + "/Header.txt")
     || !File.Exists (hcDir + "/MainLS_HC(PG).txt")
     || !File.Exists (hcDir + "/MainLS_HC(VG).txt")) {
   Console.WriteLine ("One or many required hard code files does not exist.");
   return;
}

rpp.GenOutputFiles ();
WriteLine ("File generated successfully.");
ReadKey ();