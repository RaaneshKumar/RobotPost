﻿using System.Reflection;

public static class Extensions {
   public static void CheckAndWritePrevPos (this Position pos, StreamWriter sw, ref int lineNum) {
      if (pos == null || pos.IsSkipped) return;
      if (pos.PrevPos != null && pos.PrevPos.IsWritten) {
         sw.WriteLine ($"  {lineNum++}: {pos.Motion} P[{pos.PCount}:{pos.Name}] {(pos.Motion == 'J' ? "R[15:SPD_J]% CNT10    ;" : "R[16:SPD_L]mm/sec FINE    ;")}");
         pos.PrevPos.IsWritten = true;
      } else pos.PrevPos!.CheckAndWritePrevPos (sw, ref lineNum);
   }
} 