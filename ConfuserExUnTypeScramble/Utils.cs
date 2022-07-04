using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace ConfuserExUnTypeScramble
{
    public static class Utils
    {
        /// <summary>
        /// Get the name of the typeSig type
        /// </summary>
        /// <param name="typeSig"></param>
        /// <returns>
        /// the type name of the typeSig
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetTypeName(this TypeSig typeSig)
        {
            if (typeSig == null) throw new ArgumentNullException();
            if (typeSig.Next != null) return typeSig.Next.GetTypeName();
            return typeSig.FullName;
        }

        public static void IncreaseUnscrambleTime()
        {
            Program.unscrambledTimes++;
        }
    }
}
