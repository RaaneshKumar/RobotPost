using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public static class Extensions {
   public static void CheckAndWritePrevPos (this Position pos, StreamWriter sw, int lineNum) {
      if (pos.PrevPos.IsWritten) {
         sw.WriteLine ($"  {lineNum++}: {pos.Motion} P[{pos.PCount}:{pos.Name}] {(pos.Motion == 'J' ? "R[15:SPD_J]% CNT10    ;" : "R[16:SPD_L]mm/sec FINE    ;")}");
         pos.PrevPos.IsWritten = true;
      } else pos.PrevPos.CheckAndWritePrevPos (sw, lineNum);
   }
}