using System.Net.WebSockets;
using static System.Console;

//if (args.Length == 0) { WriteLine ("no input!"); ReadKey (); return; }
//var rpp = new RobotPost (args[0]);
var rpp = new RobotPost ("C:\\Users\\nehrujiaj\\Downloads\\BOX8Bends.rbc");

Write ("Enter the hard code files directory: ");
string? hcDir = ReadLine ();
bool isValidDir = Directory.Exists (hcDir);
bool allFilesExist = File.Exists (hcDir + "/BendLS_NoRegripHC.txt")
     && File.Exists (hcDir + "/BendLS_RegripHC.txt")
     && File.Exists (hcDir + "/BendSub_HC(PG).txt")
     && File.Exists (hcDir + "/BendSub_HC(VG).txt")
     && File.Exists (hcDir + "/Header.txt")
     && File.Exists (hcDir + "/MainLS_HC(PG).txt")
     && File.Exists (hcDir + "/MainLS_HC(VG).txt");

if (!isValidDir) { WriteLine ("Invalid directory"); }
if (!allFilesExist) {
   WriteLine ("One or many required hard code files does not exist.");
}
if (!isValidDir || !allFilesExist) {
   Write ("Do you want to continue with default files (Y/N)?  ");
   var key = ReadKey ();
   if (key.Key == ConsoleKey.Y) { 
      rpp.GenOutputFiles ();
      WriteLine ("\nFile generated successfully.");
      ReadKey ();
      return; 
   }
   else {
      WriteLine ("\nNo files generated.");
      ReadKey ();
      return;
   }
}

if (hcDir != null) {
   rpp.GenOutputFiles (hcDir);
   WriteLine ("File generated successfully.");
   ReadKey ();
}