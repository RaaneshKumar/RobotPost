﻿using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

#region class RobotPost ------------------------------------------------------------------------------
public partial class RobotPost {
   #region Constructor --------------------------------------------
   public RobotPost (string path) {
      mFilePath = path;
      mFileName = Path.GetFileNameWithoutExtension (path);
      mPositions = [];
      mBends = [];
      mRbcSr = new (mFilePath);
   }
   #endregion

   #region Properties ---------------------------------------------
   public List<Bend> Bends => mBends;
   #endregion

   #region Methods ------------------------------------------------
   void CollectPositions () {
      int pCount = 0, bCount = 0; // Point count and bend count.
      string[] jointTags = ["J1", "J2", "J3", "J4", "J5", "J6"];
      Regex pattern = MyRegex ();
      string label = "";
      bool isRamPt = false, isBend = false;
      Bend bend = new (0, mGripperType); // Only for initializing.

      for (int i = 0; ; i++) {
         string? rbcLine = mRbcSr.ReadLine ();
         if (rbcLine == null) break;
         if (rbcLine == "" || i < 8) { bCount = 0; continue; }
         if (rbcLine == "Bend 0") { isRamPt = true; bCount++; continue; }
         if (rbcLine.StartsWith ('(')) {
            if (mRbcSr.Peek () != '(') label = ++pCount == 1 ? "Init" : rbcLine[1..^1];
            if (rbcLine.StartsWith ("(-") && rbcLine.Contains ("Bend ")) { // Starts collecting points in Bend object.
               bend = new (++bCount, mGripperType);
               isBend = true;
               mBends.Add (bend);
            }
            if (rbcLine.Contains ("Post-bend Safe")) isBend = false; // Back to RPP
            if (rbcLine.Contains ("Regrip")) bend.HasRegrip = true;
            if (rbcLine.Contains (":JawGripper:"))
               // Gets the gripper type used
               if (int.TryParse (rbcLine.Substring (14, 1), out int gripperType)) mGripperType = gripperType == 0 ? EGripper.Vacuum : EGripper.Pinch;
         } else if (rbcLine.StartsWith ("G01") && pattern.Match (rbcLine) is { Success: true } match) {
            string[] points = jointTags.Select (j => double.Parse (match.Groups[j].Value).ToString ("F2")).ToArray ();
            var motion = rbcLine.Contains ("Forward") ? 'J' : 'L';
            if (isRamPt) mBends[bCount - 1].BendSubPts.Add ((points, motion)); // Collects ram points for each bend.
            //else (isBend ? bend.Positions : mPositions).Add (new (pCount, points, label, motion)); 
            else {
               Position newPos = new (pCount, points, label, motion);
               if (isBend) {
                  if (bend.Positions.Count != 0) newPos.PrevPos = bend.Positions[^1];
                  bend.Positions.Add (newPos);
               } else {
                  if (mPositions.Count != 0) newPos.PrevPos = mPositions[^1];
                  mPositions.Add (newPos);
               }
            }
         } else
            if (isRamPt) mBends[bCount - 1].RamPts.Add (double.Parse (rbcLine));
      }
   }

   public void GenOutputFiles (string dir = null) {
      CollectPositions ();
      GenMainLS (dir);
      for (int i = 0; i < Bends.Count; i++) {
         var bend = Bends[i];
         bend.GenBendLS (dir);
         bend.GenBendSub (dir);
         bend.GenRamPts ();
      }
   }

   void GenMainLS (string dir) {
      // Collect post bend safe of last bend after which deposit points starts.
      var depositIdx = mPositions.IndexOf (mPositions.Where (x => x.Name == "Post-bend Safe").LastOrDefault ());
      depositIdx = depositIdx == -1 ? 0 : depositIdx;
      StringBuilder positionsSB = new ();
      using StreamWriter mainLsSW = new ($"{mFileName}.LS");
      StreamReader headerSR;
      StreamReader mainLsSR;

      if (dir != null) {
         headerSR = new ($"{dir}/Header.txt");
         mainLsSR = new ($"{dir}/MainLS_HC({(mGripperType == EGripper.Vacuum ? "VG" : "PG")}).txt");
      } else {
         headerSR = new (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("RobotPost.HardCodes.Header.txt")!);
         mainLsSR = new (Assembly.GetExecutingAssembly ().GetManifestResourceStream ($"RobotPost.HardCodes.MainLS_HC({(mGripperType == EGripper.Vacuum ? "VG" : "PG")}).txt")!); 
      }

      mainLsSW.WriteLine ($"/PROG  {mFileName}\n");
      // Header part of the hard code
      for (string? header = headerSR.ReadLine (); header != null; header = headerSR.ReadLine ()) mainLsSW.WriteLine (header);

      for (int i = 1; ; i++) { // Remaining part of the hard code
         var hardCode = mainLsSR.ReadLine ();
         if (hardCode == null) break;
         if (hardCode.StartsWith ('[')) {
            //int pointIdx = int.Parse (hardCode.Split ('[', ']')[1]) - 1;
            //var point = mPositions[pointIdx];
            var pointName = hardCode.Split ('[', ']')[1];
            var point = mPositions.Where (x => x.Name == pointName).First ();

            if (point.PCount != 1) {
               if (!point.PrevPos.IsWritten) point.CheckAndWritePrevPos (mainLsSW, i);
            }

            mainLsSW.WriteLine ($"  {i}: {point.Motion} P[{point.PCount}:{point.Name}] {(point.Motion == 'J' ? "R[15:SPD_J]% CNT10    ;" : "R[16:SPD_L]mm/sec FINE    ;")}");
            point.IsWritten = true;
         } else if (hardCode.StartsWith ('*')) // Sub Bend Program Calls
            for (int j = 1; j <= Bends.Count; j++)
               mainLsSW.WriteLine ($"  {(j == 1 ? i++ : i)}:{(j == 1 ? $"  SELECT R[17]={j},CALL BEND{j}Positioning_sub ;" : $"       ={j},CALL BEND{j}Positioning_sub ;")}");
         else if (hardCode.StartsWith ('(')) { // Points after deposit
            //int hcIdx = int.Parse (hardCode.Split ('(', ')')[1]) - 1;
            //var point = mPositions[depositIdx + hcIdx];
            var pointName = hardCode.Split ('(', ')')[1];
            var point = mPositions.Where (x => x.Name == pointName).First ();

            if (point.PCount != 1) {
               if (!point.PrevPos.IsWritten) point.CheckAndWritePrevPos (mainLsSW, i);
            }

            mainLsSW.WriteLine ($"  {i}: {point.Motion} P[{point.PCount}:{point.Name}] {(point.Motion == 'J' ? "R[15:SPD_J]% CNT10    ;" : "R[16:SPD_L]mm/sec FINE    ;")}");
            point.IsWritten = true;
            //point.Che
         } else mainLsSW.WriteLine ($"  {i}: {hardCode}");
      }

      for (int i = 0; i < mPositions.Count; i++) {
         var position = mPositions[i];
         WriteToSB (positionsSB, position.Pos, position.PCount, position.Name);
      }
      mainLsSW.WriteLine ("/POS\n" + positionsSB + "\n/END");
   }

   // Writes the positions to the required string builder. 
   void WriteToSB (StringBuilder sb, string[] jPositions, int pCount, string? label = null) {
      sb.Append ($"P[{pCount}:{(label != null ? $"\"{label}\"" : "\"\"")}]{{\nGP1:\n" +
                 $"UF : {(pCount < 9 ? 1 : pCount < 20 ? 2 : 3)}, UT : 2,\n" +
                 $"J1 = {jPositions[0]} deg, J2 = {jPositions[1]} deg, J3 = {jPositions[2]} deg,\n" +
                 $"J4 = {jPositions[3]} deg, J5 = {jPositions[4]} deg, J6 = {jPositions[5]} deg\n}};\n");
   }
   #endregion

   #region Attributes ---------------------------------------------
   [GeneratedRegex (@"G01 J1\s(?<J1>[-+]?\d*\.?\d+) J2\s(?<J2>[-+]?\d*\.?\d+) J3\s(?<J3>[-+]?\d*\.?\d+) J4\s(?<J4>[-+]?\d*\.?\d+) J5\s(?<J5>[-+]?\d*\.?\d+) J6\s(?<J6>[-+]?\d*\.?\d+).*", RegexOptions.Compiled)]
   private static partial Regex MyRegex ();
   #endregion

   #region Private fields -----------------------------------------
   string mFileName, mFilePath;
   List<Position> mPositions; // Has Pickup and Deposit positions
   List<Bend> mBends;
   StreamReader mRbcSr;
   EGripper mGripperType;
   #endregion
}
#endregion



#region class Bend -----------------------------------------------------------------------------
public class Bend {

   #region Constructor --------------------------------------------
   public Bend (int rank, EGripper gripperType) {
      mRank = rank;
      mPositions = [];
      mBendSubPts = [];
      mRamPts = [];
      mGripperType = gripperType;
   }
   #endregion

   #region Properties ---------------------------------------------
   public int Rank => mRank;

   public bool HasRegrip {
      get => mHasRegrip;
      set => mHasRegrip = value;
   }

   public List<Position> Positions {
      get => mPositions;
      set => mPositions = value;
   }

   public List<(string[] pos, char motion)> BendSubPts {
      get => mBendSubPts;
      set => mBendSubPts = value;
   }

   public List<double> RamPts {
      get => mRamPts;
      set => mRamPts = value;
   }
   #endregion

   #region Public methods -----------------------------------------
   public void GenBendLS (string dir) {
      StringBuilder positionsSB = new ();
      using StreamWriter bendLsSW = new ($"Bend{Rank}Positioning_Sub.LS");
      bendLsSW.WriteLine ($"/PROG  Bend{Rank}Positioning_sub\n");
      StreamReader headerSR;
      StreamReader bendLsSR;

      if (dir != null) {
         headerSR = new ($"{dir}/Header.txt");
         bendLsSR = new ($"{dir}/{(HasRegrip ? "BendLS_RegripHC.txt" : "BendLS_NoRegripHC.txt")}");
      } 
      else {
         headerSR = new (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("RobotPost.HardCodes.Header.txt")!);
         bendLsSR = new (Assembly.GetExecutingAssembly ().GetManifestResourceStream ($"RobotPost.HardCodes.{(HasRegrip ? "BendLS_RegripHC.txt" : "BendLS_NoRegripHC.txt")}")!);
      }
      

      var firstPos = mPositions[0];
      for (string? header = headerSR.ReadLine (); header != null; header = headerSR.ReadLine ())// Header part of the hard code
         bendLsSW.WriteLine (header);

      for (int i = 1; ; i++) { // Remaining part of the hard code
         var hardCode = bendLsSR.ReadLine ();
         if (hardCode == null) break;
         if (hardCode.StartsWith ('[')) {
            //int pointIdx = int.Parse (hardCode.Split ('[', ']')[1]) - 1;
            var pointName = hardCode.Split ('[', ']')[1];
            if (pointName == "") continue;
            if (Rank == 1 && (pointName is "Pre-bend Safe" or "Station Front")) continue;
            var point = mPositions.Where (x=> x.Name == pointName).First ();
            //var point = Positions[pointIdx];

            if (point.PCount != firstPos.PCount) {
               if (!point.PrevPos.IsWritten) point.CheckAndWritePrevPos (bendLsSW, i);
            }

            bendLsSW.WriteLine ($"  {i}: {point.Motion} P[{point.PCount}:{point.Name}] {(point.Motion == 'J' ? "R[15:SPD_J]% CNT10    ;" : "R[16:SPD_L]mm/sec FINE    ;")}");
            point.IsWritten = true;
         } else if (hardCode.StartsWith ('*')) { // Bend sub Calls
            bendLsSW.WriteLine ($"  {i}:  CALL BEND{Rank}SUB    ;");
         } else if (hardCode.StartsWith ('^')) {
            bendLsSW.WriteLine ($"  {i}:   R[17]={Rank + 1} ;"); // R[17] = 2 for first bend, 3 for second bend...
         } else {
            bendLsSW.WriteLine ($"  {i}: {hardCode}");
         }
      }

      for (int i = 0; i < Positions.Count; i++) {
         var position = Positions[i];
         WriteToSB (positionsSB, position.Pos, position.PCount, position.Name);
      }
      bendLsSW.WriteLine ("/POS\n" + positionsSB + "\n/END");
   }

   public void GenBendSub (string dir) {
      StringBuilder ramPtsSB = new ();
      using StreamWriter bendSubSW = new ($"BEND{Rank}SUB.LS");
      bendSubSW.WriteLine ($"/PROG BEND{Rank}SUB\n");
      StreamReader bendSubHcSR;

      if (dir != null) {
         bendSubHcSR = new ($"{dir}/BendSub_HC({(mGripperType == EGripper.Vacuum ? "VG" : "PG")}).txt");
      } else {
         bendSubHcSR = new (Assembly.GetExecutingAssembly ().GetManifestResourceStream ($"RobotPost.HardCodes.BendSub_HC({(mGripperType == EGripper.Vacuum ? "VG" : "PG")}).txt")!);
      }

      for (string? hardCode = bendSubHcSR.ReadLine (); hardCode != null; hardCode = bendSubHcSR.ReadLine ()) // Header part of the hard code
         bendSubSW.WriteLine (hardCode);
      for (int i = 0; i < BendSubPts.Count; i++) {
         var ramPt = BendSubPts[i];
         WriteToSB (ramPtsSB, ramPt.pos, i + 1);
      }
      bendSubSW.WriteLine (ramPtsSB + "\n/END");
   }

   public void GenRamPts () {
      using StreamWriter ramPtsSW = new ($"BEND{Rank}_RamPts.txt");
      for (int i = 0; i < RamPts.Count; i++)
         ramPtsSW.WriteLine (RamPts[i]);
   }
   #endregion

   #region Implementation -----------------------------------------
   // Writes the positions to the required string builder. 
   void WriteToSB (StringBuilder sb, string[] jPositions, int pCount, string? label = null) {
      sb.Append ($"P[{pCount}:{(label != null ? $"\"{label}\"" : "\"\"")}]{{\nGP1:\n" +
                 $"UF : {(pCount < 9 ? 1 : pCount < 20 ? 2 : 3)}, UT : 2,\n" +
                 $"J1 = {jPositions[0]} deg, J2 = {jPositions[1]} deg, J3 = {jPositions[2]} deg,\n" +
                 $"J4 = {jPositions[3]} deg, J5 = {jPositions[4]} deg, J6 = {jPositions[5]} deg\n}};\n");
   }
   #endregion

   #region Private fields -----------------------------------------
   List<Position> mPositions;
   List<(string[] pos, char motion)> mBendSubPts;
   List<double> mRamPts;
   int mRank;
   bool mHasRegrip;
   EGripper mGripperType;
   #endregion
}
#endregion

#region class Position -------------------------------------------------------------------------
public class Position {
   public Position (int pCount, string[] pos, string name, char motion) {
      mIsWritten = false;
      mMotion = motion;
      mPCount = pCount;
      mPos = pos;
      //mPrevPos = new ();
      mName = name;
   }

   public Position () {
      mIsWritten= false;
      mPos = new string[6];
      //mPrevPos = new ();
      mName = "";
   }

   #region Properties ---------------------------------------------
   public bool IsWritten {
      get => mIsWritten;
      set => mIsWritten = value;
   }
   
   public char Motion => mMotion;

   public int PCount => mPCount;

   public string[] Pos => mPos;

   public string Name => mName;

   public Position PrevPos {
      get => mPrevPos;
      set => mPrevPos = value;
   }
   #endregion

   #region Private fields -----------------------------------------
   bool mIsWritten;
   char mMotion;
   int mPCount;
   string[] mPos;
   Position mPrevPos;
   string mName;
   #endregion
}
#endregion

#region Enums ----------------------------------------------------------------------------------
public enum EGripper { Vacuum, Pinch }
#endregion