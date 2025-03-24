using static System.Console;

if (args.Length == 0) { WriteLine ("no input!"); ReadKey (); return; }
var rpp = new RobotPost (args[0]); // Gets the rbc file from Flux as input.

Write ("Enter the hard code files directory: ");
string? hcDir = ReadLine ();
bool isValidDir = Directory.Exists (hcDir);
bool allFilesExist = File.Exists (hcDir + "/BendLS_NoRegripHC.txt")
                  && File.Exists (hcDir + "/BendLS_RegripHC.txt")
                  && File.Exists (hcDir + "/CenteringTableLS_HC.txt")
                  && File.Exists (hcDir + "/BendSub_HC.txt")
                  && File.Exists (hcDir + "/Header.txt")
                  && File.Exists (hcDir + "/MainLS_HC.txt")
                  && File.Exists (hcDir + "/DepositLS_HC.txt")
                  && File.Exists (hcDir + "/PickUpLS_HC.txt");

// Invalid hard code files.
if (!isValidDir) {
   WriteLine ("Invalid directory");
   CheckToContinue ();
   return;
}
if (isValidDir && !allFilesExist) {
   WriteLine ("One or many required hard code files does not exist.");
   CheckToContinue () ;
   return;
}

// Valid hard code files.
if (hcDir != null) {
   rpp.GenOutputFiles (hcDir);
   PrintToConsole ("\nFiles generated successfully.");
   return;
}

#region Methods ------------------------------------------------
void PrintToConsole (string msg) {
   WriteLine (msg);
   ReadKey ();
}

// Asks the user whether to continue with the default hard code files and executes them.
void CheckToContinue () {
   Write ("Do you want to continue with default files (Y/N)?  ");
   if (ReadKey ().Key == ConsoleKey.Y) {
      rpp.GenOutputFiles ();
      PrintToConsole ("\nFiles generated successfully.");
   } else PrintToConsole ("\nNo files generated."); 
}
#endregion