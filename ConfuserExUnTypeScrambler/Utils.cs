﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;

namespace ConfuserExUnTypeScrambler
{
    /// <summary>
    /// ConfuserEx UntypeScrambler utils
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Get the type name of this <see cref="TypeSig"/>.
        /// </summary>
        /// <param name="typeSig"></param>
        /// <returns>
        /// The type name of this <see cref="TypeSig"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeSig"/> is <see langword="null" />.
        /// </exception>
        public static string GetTypeName(this TypeSig typeSig)
        {
            if (typeSig == null) throw new ArgumentNullException();
            if (typeSig.Next != null) return typeSig.Next.GetTypeName();
            return typeSig.FullName;
        }

        /// <summary>
        ///Check if <see cref="UnTypeScrambler.scrambledMethods"/>[<paramref name="method"/>] contains any generic parameter. 
        /// </summary>
        /// <param name="method"></param>
        /// <returns>
        /// <see langword="true"/> if <see cref="UnTypeScrambler.scrambledMethods"/>[<paramref name="method"/>] contains any generic parameter, otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// <see cref="UnTypeScrambler.scrambledMethods"/> does not contains <paramref name="method"/>.
        /// </exception>
        public static bool isFoundGenericParameters(MethodDef method)
        {
            if (!UnTypeScrambler.scrambledMethods.ContainsKey(method)) throw new Exception("scrambledMethods does not contains method: " + method.FullName + " [0x" + method.MDToken + "]!");
            foreach (TypeSig typeSig in UnTypeScrambler.scrambledMethods[method])
            {
                if (typeSig.IsGenericParameter) return true;
            }
            return false;
        }

        internal static void LogException(Exception ex)
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ForegroundColor = consoleColor;
        }
    }
}
