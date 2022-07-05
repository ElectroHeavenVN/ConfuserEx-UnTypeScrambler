using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace ConfuserExUnTypeScrambler
{
    /// <summary>
    /// Contains algorithm for fixing ConfuserEx type scramble.
    /// </summary>
    public class UnTypeScrambler
    {
        /// <summary>
        /// Collection of type scrambled methods.
        /// </summary>
        public static Dictionary<MethodDef, IList<TypeSig>> scrambledMethods = new Dictionary<MethodDef, IList<TypeSig>>();

        /// <summary>
        /// Collection of excluded methods.
        /// </summary>
        public static List<MethodDef> excludedMethods = new List<MethodDef>();

        /// <summary>
        /// Fix calls to type scrambled methods.
        /// </summary>
        /// <param name="types">
        /// The collection of <see cref="TypeDef"/>.
        /// </param>
        public static void FixTypeScrambleCalls(IList<TypeDef> types)
        {
            foreach (TypeDef type in types)
            {
                if (type.HasNestedTypes) FixTypeScrambleCalls(type.NestedTypes);
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        if ((instruction.OpCode.OperandType == OperandType.InlineMethod || instruction.OpCode.OperandType == OperandType.InlineTok) && instruction.Operand is MethodSpec methodSpec)
                        {
                            MethodDef methodDef = methodSpec.ResolveMethodDef();
                            if (methodDef == null) continue;
                            if (scrambledMethods.ContainsKey(methodDef) && !excludedMethods.Contains(methodDef))
                            {
                                methodSpec.GenericInstMethodSig.GenericArguments.Clear();
                                methodSpec.GenericInstMethodSig.Generic = false;
                            }

                        }
                    }
                }
            }
        }
        /// <summary>
        /// Remove method generic parameters if possible.
        /// </summary>
        /// <param name="types">
        /// The collection of <see cref="TypeDef"/>.
        /// </param>
        public static void RemoveGenericParameters(IList<TypeDef> types)
        {
            foreach (TypeDef type in types)
            {
                if (type.HasNestedTypes) RemoveGenericParameters(type.NestedTypes);
                foreach (MethodDef method in type.Methods)
                {
                    if (method.HasGenericParameters && !excludedMethods.Contains(method) && scrambledMethods.ContainsKey(method))
                    {
                        if (!Utils.isFoundGenericParameters(method))
                        {
                            method.GenericParameters.Clear();
                            MethodSig signature = (MethodSig)method.Signature;
                            signature.GenParamCount = 0;
                            signature.Generic = false;
                        }
                        else Console.WriteLine("Unable to remove generic parameters of method: " + method.FullName + " [" + method.MDToken + "]!");
                    }
                }
            }
        }

        /// <summary>
        /// Main algorithm.
        /// </summary>
        /// <param name="types">
        /// The collection of <see cref="TypeDef"/>.
        /// </param>
        public static void UnTypeScramble(IList<TypeDef> types)
        {
            foreach (TypeDef type in types)
            {
                if (type.HasNestedTypes) UnTypeScramble(type.NestedTypes);
                foreach (MethodDef method in type.Methods)
                {
                    if (scrambledMethods.ContainsKey(method) && !excludedMethods.Contains(method))
                    {
                        for (int i = 0; i < method.GenericParameters.Count; i++)
                        {
                            GenericParam genericParam = method.GenericParameters[i];
                            if (genericParam.FullName == method.ReturnType.GetTypeName() && !scrambledMethods[method][i].IsGenericParameter)
                            {
                                method.ReturnType = FixTypeSig(method.ReturnType, method, i);
                            }
                        }
                        for (int i = 0; i < method.Body.Variables.Count; i++)
                        {
                            Local local = method.Body.Variables[i];
                            for (int j = 0; j < method.GenericParameters.Count; j++)
                            {
                                local.Type = FixTypeSig(local.Type, method, j);
                            }
                        }
                        for (int i = 0; i < method.Parameters.Count; i++)
                        {
                            Parameter param = method.Parameters[i];
                            for (int j = 0; j < method.GenericParameters.Count; j++)
                            {
                                if (param.Type.GetTypeName() == method.GenericParameters[j].FullName && !scrambledMethods[method][j].IsGenericParameter)
                                {
                                    param.Type = FixTypeSig(param.Type, method, j);
                                }
                            }
                        }
                        foreach (Instruction instruction in method.Body.Instructions)
                        {
                            if ((instruction.OpCode.OperandType == OperandType.InlineType || instruction.OpCode.OperandType == OperandType.InlineTok) && instruction.Operand is TypeSpec typeSpec)
                            {
                                for (int j = 0; j < method.GenericParameters.Count; j++)
                                {
                                    if ((typeSpec.FullName == method.GenericParameters[j].FullName) && !scrambledMethods[method][j].IsGenericParameter)
                                    {
                                        instruction.Operand = scrambledMethods[method][j].ToTypeDefOrRef();
                                        Program.unscrambledTimes++;
                                    }
                                }
                            }
                            if ((instruction.OpCode.OperandType == OperandType.InlineMethod || instruction.OpCode.OperandType == OperandType.InlineTok) && instruction.Operand is MethodSpec methodSpec)
                            {
                                IList<TypeSig> typeSigs = methodSpec.GenericInstMethodSig.GenericArguments;
                                for (int i = 0; i < typeSigs.Count; i++)
                                {
                                    for (int j = 0; j < method.GenericParameters.Count; j++)
                                    {
                                        if (!scrambledMethods[method][j].IsGenericParameter)
                                        {
                                            if (typeSigs[i].FullName == method.GenericParameters[j].FullName)
                                            {
                                                methodSpec.GenericInstMethodSig.GenericArguments[i] = scrambledMethods[method][j];
                                                Program.unscrambledTimes++;
                                            }
                                            else if (typeSigs[i].IsGenericInstanceType)
                                            {
                                                UnTypeScrambleGenericInstType(method, j, typeSigs[i]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static TypeSig FixTypeSig(TypeSig typeSig, MethodDef method, int j)
        {
            TypeSig typeSig1 = scrambledMethods[method][j];
            if (typeSig.Next == null)
            {
                bool isfnptr = typeSig.IsFunctionPointer;
                if (isfnptr)
                {
                    FnPtrSig fnPtr = (FnPtrSig)typeSig1;
                    FnPtrSig fnPtrSig = new FnPtrSig(fnPtr.Signature);
                    typeSig = typeSig1 = fnPtrSig;
                    Program.unscrambledTimes++;
                }
            }
            if (typeSig.Next != null && !typeSig1.IsGenericParameter)
            {
                typeSig1 = FixTypeSig(typeSig.Next, method, j);
            }
            if ((typeSig.GetTypeName() == method.GenericParameters[j].FullName || (typeSig.Next != null && typeSig.Next.GetTypeName() == method.GenericParameters[j].FullName)) && !scrambledMethods[method][j].IsGenericParameter)
            {
                bool ispointer = typeSig.IsPointer;
                bool isbyref = typeSig.IsByRef;
                bool isszarray = typeSig.IsSZArray;
                bool ismodreqd = typeSig.IsRequiredModifier;
                bool ismodopt = typeSig.IsOptionalModifier;
                bool ispinned = typeSig.IsPinned;
                bool isarray = typeSig.IsArray;
                if (ispointer || isbyref || isszarray || isarray || ismodreqd || ismodopt || ispinned)
                {
                    if (ispointer)
                    {
                        PtrSig ptrSig = new PtrSig(typeSig1);
                        typeSig = typeSig1 = ptrSig;
                    }
                    if (isbyref)
                    {
                        ByRefSig byRefSig = new ByRefSig(typeSig1);
                        typeSig = typeSig1 = byRefSig;
                    }
                    if (isszarray)
                    {
                        SZArraySig sZArraySig = new SZArraySig(typeSig1);
                        typeSig = typeSig1 = sZArraySig;
                    }
                    else if (isarray)
                    {
                        ArraySig sig = (ArraySig)typeSig;
                        ArraySig arraySig = new ArraySig(typeSig1, sig.Rank, sig.Sizes, sig.LowerBounds);
                        typeSig = typeSig1 = arraySig;
                    }
                    if (ismodreqd)
                    {
                        CModReqdSig cModReqd = (CModReqdSig)typeSig;
                        CModReqdSig cModReqdSig = new CModReqdSig(cModReqd.Modifier, typeSig1);
                        typeSig = typeSig1 = cModReqdSig;
                    }
                    if (ismodopt)
                    {
                        CModOptSig cModOpt = (CModOptSig)typeSig;
                        CModOptSig cModOptSig = new CModOptSig(cModOpt.Modifier, typeSig1);
                        typeSig = typeSig1 = cModOptSig;
                    }
                    if (ispinned)
                    {
                        PinnedSig pinnedSig = new PinnedSig(typeSig1);
                        typeSig = pinnedSig;
                    }
                }
                else typeSig = typeSig1;
                Program.unscrambledTimes++;
            }
            return typeSig;
        }

        private static void UnTypeScrambleGenericInstType(MethodDef method, int j, TypeSig typeSig)
        {
            if (scrambledMethods[method][j].IsGenericInstanceType)
            {
                for (int k = 0; k < typeSig.ToGenericInstSig().GenericArguments.Count; k++)
                {
                    if (typeSig.ToGenericInstSig().GenericArguments[k].FullName == method.GenericParameters[j].FullName)
                    {
                        typeSig.ToGenericInstSig().GenericArguments[k] = scrambledMethods[method][j].ToGenericInstSig().GenericArguments[k];
                        Program.unscrambledTimes++;
                    }
                    else if (typeSig.ToGenericInstSig().GenericArguments[k].IsGenericInstanceType)
                    {
                        UnTypeScrambleGenericInstType(method, j, typeSig.ToGenericInstSig().GenericArguments[k]);
                    }
                }
            }
            else
            {
                for (int k = 0; k < typeSig.ToGenericInstSig().GenericArguments.Count; k++)
                {
                    if (typeSig.ToGenericInstSig().GenericArguments[k].FullName == method.GenericParameters[j].FullName)
                    {
                        typeSig.ToGenericInstSig().GenericArguments[k] = scrambledMethods[method][j];
                        Program.unscrambledTimes++;
                    }
                    else if (typeSig.ToGenericInstSig().GenericArguments[k].IsGenericInstanceType)
                    {
                        UnTypeScrambleGenericInstType(method, j, typeSig.ToGenericInstSig().GenericArguments[k]);
                    }
                }
            }
        }

        /// <summary>
        /// Get all scrambled methods in <paramref name="types" />.
        /// </summary>
        /// <param name="types"> 
        /// The collection of <see cref="TypeDef"/>.
        /// </param>
        public static void GetScrambledMethods(IList<TypeDef> types)
        {
            foreach (TypeDef type in types)
            {
                if (type.HasNestedTypes) GetScrambledMethods(type.NestedTypes);
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode.OperandType == OperandType.InlineMethod || instruction.OpCode.OperandType == OperandType.InlineTok)
                        {
                            if (instruction.Operand is MethodSpec)
                            {
                                MethodSpec methodSpec = (MethodSpec)instruction.Operand;
                                MethodDef methodDef = methodSpec.Method.ResolveMethodDef();
                                if (methodDef == null) continue;
                                if (methodSpec.Method.IsMethod && methodDef.HasGenericParameters)
                                {
                                    GenericInstMethodSig genericInstMethodSig = (GenericInstMethodSig)methodSpec.Instantiation;
                                    try
                                    {
                                        if (!excludedMethods.Contains(methodDef))
                                        {
                                            scrambledMethods.Add(methodDef, genericInstMethodSig.GenericArguments);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        for (int i = 0; i < genericInstMethodSig.GenericArguments.Count; i++)
                                        {
                                            if (scrambledMethods[methodDef][i].IsGenericParameter && !genericInstMethodSig.GenericArguments[i].IsGenericParameter)
                                            {
                                                scrambledMethods[methodDef][i] = genericInstMethodSig.GenericArguments[i];
                                            }
                                            if (!scrambledMethods[methodDef][i].IsGenericParameter && !genericInstMethodSig.GenericArguments[i].IsGenericParameter && scrambledMethods[methodDef][i].FullName != genericInstMethodSig.GenericArguments[i].FullName)
                                            {
                                                if (!excludedMethods.Contains(methodDef)) excludedMethods.Add(methodDef);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Fix <see cref="Activator.CreateInstance(Type)"/> by adding an explicit cast.
        /// </summary>
        /// <param name="types">
        /// The collection of <see cref="TypeDef"/>.
        /// </param>
        public static void FixActivatorCreateInstance(IList<TypeDef> types)
        {
            foreach (TypeDef type in types)
            {
                if (type.HasNestedTypes) FixActivatorCreateInstance(type.NestedTypes);
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody || excludedMethods.Contains(method)) continue;
                    method.Body.SimplifyBranches();
                    method.Body.SimplifyMacros(method.Parameters);
                    for (int i = method.Body.Instructions.Count - 1; i >= 2; i--)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Call && method.Body.Instructions[i - 1].OpCode == OpCodes.Call && method.Body.Instructions[i - 2].OpCode == OpCodes.Ldtoken)
                        {
                            MemberRef memberRef = (MemberRef)method.Body.Instructions[i].Operand;
                            if (memberRef.FullName.Contains("System.Activator::CreateInstance(")) method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Castclass, method.Body.Instructions[i - 2].Operand));
                        }
                    }
                    method.Body.OptimizeBranches();
                    method.Body.OptimizeMacros();
                }
            }
        }
    }
}
