using static System.Console;

//if (args.Length == 0) { WriteLine ("No input!"); ReadKey (); return; }
var rpp = new RPP ("C:\\Users\\rajakumarra\\Downloads\\300x130vac_2bendtilt.rbc");
rpp.GenOutputFiles ();
WriteLine ("File generated successfully.");
ReadKey ();