using static System.Console;

//if (args.Length == 0) { WriteLine ("No input!"); ReadKey (); return; }
var rpp = new RPP ("C:\\Users\\nehrujiaj\\Downloads\\350x400vac_3bend.rbc");
rpp.GenOutputFiles ();
WriteLine ("File generated successfully.");
ReadKey ();