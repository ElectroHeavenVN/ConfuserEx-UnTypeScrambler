﻿using System;
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

        public static bool isContainsGenericParameters(MethodDef method)
        {
            if (!UnTypeScrambler.scrambledMethods.ContainsKey(method)) throw new Exception("scrambledMethods does not contains method: " + method.FullName + " [" + method.MDToken + "]!");
            foreach (TypeSig typeSig in UnTypeScrambler.scrambledMethods[method])
            {
                if (typeSig.IsGenericParameter) return true;
            }
            return false;
        }
    }
}
