﻿using static System.Console;

if (args.Length == 0) { WriteLine ("No input!"); ReadKey (); return; }
var rpp = new RobotPost (args[0]);
rpp.GenOutputFiles ();
WriteLine ("File generated successfully.");
ReadKey ();