﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Puerts
{
    public static class HotfixInject
    {
        private static Dictionary<string, Dictionary<string, List<string>>> allInjectMethods;

        #region StartInject
        /// <summary> 开始注入 </summary>
        public static void StartInject(string assmeblyPath, List<string> injectList, string codeDir)
        {
            allInjectMethods = new Dictionary<string, Dictionary<string, List<string>>>();
            assmeblyPath = Path.GetFullPath(assmeblyPath);
            AssemblyDefinition assembly = null;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(assmeblyPath,
                    new ReaderParameters { ReadSymbols = true, ReadWrite = true, });

                CreateTempFile(assmeblyPath);

                if (IsDirty(assembly))
                {
                    UnityEngine.Debug.LogError("assembly dirty");
                    return;
                }

                SetDirty(assembly);

                foreach (var type in assembly.MainModule.Types)
                {
                    var methodStrs = InjectType(assembly.MainModule, type, injectList); // 注入
                    if (!string.IsNullOrEmpty(methodStrs))
                    {
                        UnityEngine.Debug.Log(methodStrs);
                    }
                }

                GenerateHotfixCode(codeDir);

                assembly.Write(new WriterParameters { WriteSymbols = true });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("inject exception:\r\n" + e);
                return;
            }
            finally
            {
                //如果不清理，在window下会锁定文件
                if (assembly != null && assembly.MainModule.SymbolReader != null)
                {
                    assembly.MainModule.SymbolReader.Dispose();
                }
                if (assembly != null)
                {
                    assembly.Dispose();
                }
            }
            UnityEngine.Debug.Log(Path.GetFileName(assmeblyPath) + " inject success");
            UnityEditor.AssetDatabase.Refresh();
        }
        /// <summary> 注入Type </summary>
        private static string InjectType(ModuleDefinition module, TypeDefinition type, List<string> injectList)
        {
            var methodStrs = "";
            foreach (var nestedTypes in type.NestedTypes)
            {
                methodStrs += InjectType(module, nestedTypes, injectList);
            }
            foreach (var method in type.Methods)
            {
                if (!IsHotfix(method, injectList)) continue;
                var result = DoInjectMethod(module, method);
                if (!string.IsNullOrEmpty(result))
                {
                    methodStrs += result + "\n";
                }
            }
            return string.IsNullOrEmpty(methodStrs) ? "" : string.Format("class : {0}\nmethons : \n{1}\n", type.FullName, methodStrs);
        }
        #endregion

        #region Tools

        #region IsHotfix
        private static bool IsHotfix(MethodDefinition method, List<string> injectList)
        {
            var methodString = GetMethodString(method);
            foreach (var item in injectList)
            {
                var itemMethodString = item;
                if (itemMethodString == methodString)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Method String
        private static string GetMethodString(MethodDefinition method)
        {
            // Type MethodName(Type parme1Name,Type parme2Name);
            return string.Format("{0} {1}.{2}({3});",
                method.ReturnType.FullName,
                method.DeclaringType.FullName,
                method.Name,
                GetMethodParamsString(method));
        }
        private static string GetMethodParamsString(MethodDefinition method)
        {
            // Type parme1Name,Type parme2Name
            var result = "";
            if (method.HasParameters)
            {
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    var p = method.Parameters[i];
                    result += string.Format("{0} {1}", p.ParameterType.FullName, p.Name);
                    if (i != method.Parameters.Count - 1)
                    {
                        result += ", ";
                    }
                }
            }
            return result;
        }
        private static int GetMethodId(string className, string methodName, string paramString)
        {
            if (!allInjectMethods.ContainsKey(className))
            {
                allInjectMethods.Add(className, new Dictionary<string, List<string>>());
            }
            var methods = allInjectMethods[className];
            if (!methods.ContainsKey(methodName))
            {
                methods.Add(methodName, new List<string>());
            }
            var methodIds = methods[methodName];
            methodIds.Add(paramString);
            return methodIds.Count - 1;
        }
        #endregion

        #endregion

        #region Sign Dirty
        private const string TypeNameForInjectFlag = "__PUERTS_INJECT_FLAG";
        public static bool IsDirty(AssemblyDefinition a)
        {
            return a.MainModule.Types.Any(t => t.Name == TypeNameForInjectFlag);
        }
        public static void SetDirty(AssemblyDefinition a)
        {
            a.MainModule.Types.Add(
                new TypeDefinition("__PUERTS_GEN", TypeNameForInjectFlag,
                Mono.Cecil.TypeAttributes.Class, a.MainModule.TypeSystem.Object));
        }
        #endregion

        #region Create Temp
        /// <summary> 创建dll缓存 </summary>
        private static void CreateTempFile(string assmeblyPath)
        {
            string tmpPath = Path.Combine("./Temp/", "assmebly_backups", Path.GetFileName(assmeblyPath));
            var temDir = Path.GetDirectoryName(tmpPath);
            if (!Directory.Exists(temDir))
                Directory.CreateDirectory(temDir);
            try { File.Copy(assmeblyPath, tmpPath, true); }
            catch { }
        }
        #endregion

        #region Generate
        private static void GenerateHotfixCode(string codeDir)
        {
            var path = Path.Combine(codeDir, "hotfix_map.js.txt");
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, GenerateHotfixCode(), System.Text.Encoding.UTF8);
        }
        public static string GenerateHotfixCode()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("(function () {");
            sb.AppendLine("\"use strict\";");
            sb.AppendLine("var hotfix_map = {");

            foreach (var classItem in allInjectMethods)
            {
                sb.AppendLine(string.Format("    \"{0}\": ", classItem.Key) + "{");
                foreach (var methodItem in classItem.Value)
                {
                    sb.AppendLine(string.Format("        \"{0}\": [", methodItem.Key));
                    for (int i = 0; i < methodItem.Value.Count; i++)
                    {
                        var id = methodItem.Value[i];
                        sb.AppendLine(string.Format("            \"{0}\", // {1}", id, i));
                    }
                    sb.AppendLine("        ],");
                }
                sb.AppendLine("    },");
            }
            sb.AppendLine("}");
            sb.AppendLine("return hotfix_map;");
            sb.AppendLine("}());");
            return sb.ToString();
        }
        #endregion

        #region Inject Method

        #region IL **
        /*  // 注入的IL的伪代码
            public class FooBar
            {
                public void Foo(string params1, int params2, Action params3)
                {
                    if(LuaPatch.HasPatch(className, methodName, methodId))
                    {
                        LuaPatch.CallPatch(className, methodName, methodId, this, params1, params2, params3);
                        return;
                    }
                    // the old code here
                    Debug.Log("这里是原来的逻辑代码, 无返回值");
                }
                public Vector2 Bar(string params1, int params2, Action params3)
                {
                    if (LuaPatch.HasPatch(className, methodName, methodId))
                    {
                        return (Vector2)LuaPatch.CallPatch(className, methodName, methodId, this, params1, params2, params3);
                    }
                    // the old code here
                    Debug.Log("这里是原来的逻辑代码, 有返回值");
                    return Vector2.one;
                }
            }
        */
        #endregion

        /// <summary> 开始注入方法 </summary>
        private static string DoInjectMethod(ModuleDefinition module, MethodDefinition method)
        {
            if (method.Name.Equals(".ctor") || 
                method.Name == ".cctor" || 
                method.IsAbstract || method.IsPInvokeImpl || 
                method.IsGetter || method.IsSetter ||
                method.Name.Contains("<") || !method.HasBody) return "";
            if (IsGeneric(method))
            {
                UnityEngine.Debug.LogWarningFormat("jump Generic Method : {0}.{1}", method.DeclaringType.FullName, method.FullName);
                return ""; 
            }
            InjectMethod(module, method);
            return GetMethodString(method);
        }
        private static void InjectMethod(ModuleDefinition module, MethodDefinition method)
        {
            var type = method.DeclaringType;

            var hotfixType = module.Types.Single(t => t.FullName == "Puerts.Hotfix");
            var hasPatchRef = module.ImportReference(hotfixType.Methods.Single(m=>m.Name == "HasPatch"));
            var callPatchRef = module.ImportReference(hotfixType.Methods.Single(m => m.Name == "CallPatch"));

            var className = type.FullName;
            var methodName = method.Name;
            var methodId = GetMethodId(className, methodName, GetMethodParamsString(method));

            // 使用下面方法进行importReference会导致Unity代码无法重新编译
            // var hasPatchRef = module.ImportReference(typeof(Hotfix).GetMethod("HasPatch"));
            // var callPatchMethod = module.ImportReference(typeof(Hotfix).GetMethod("CallPatch"));

            var firstIns = method.Body.Instructions.First();
            var worker = method.Body.GetILProcessor();

            // bool result = Hotfix.HasPatch(className, methodName, methodId);
            var current = InsertBefore(worker, firstIns, worker.Create(OpCodes.Ldstr, className));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldstr, methodName));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldc_I4, methodId));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Call, hasPatchRef));

            // if(result == false) jump to the under code
            current = InsertAfter(worker, current, worker.Create(OpCodes.Brfalse, firstIns));

            // else Hotfix.CallPatch(className, methodName, methodId, this or null, args)
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldstr, className));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldstr, methodName));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldc_I4, methodId));
            // 如果不是static方法，则把this传进参数
            if (method.IsStatic)
                current = InsertAfter(worker, current, worker.Create(OpCodes.Ldnull));
            else
                current = InsertAfter(worker, current, worker.Create(OpCodes.Ldarg_0));

            var paramsCount = method.Parameters.Count;
            // 创建 args参数 object[] 集合
            current = InsertAfter(worker, current, worker.Create(OpCodes.Ldc_I4, paramsCount));
            current = InsertAfter(worker, current, worker.Create(OpCodes.Newarr, module.ImportReference(typeof(object))));
            for (int index = 0; index < paramsCount; index++)
            {
                var argIndex = method.IsStatic ? index : index + 1;
                // 压入参数
                current = InsertAfter(worker, current, worker.Create(OpCodes.Dup));
                current = InsertAfter(worker, current, worker.Create(OpCodes.Ldc_I4, index));
                var paramType = method.Parameters[index].ParameterType;
                // 获取参数类型定义, 用来区分是否枚举类 [若你所使用的类型不在本assembly, 则此处需要遍历其他assembly以取得TypeDefinition]
                var paramTypeDef = module.GetType(paramType.FullName);
                // 这里很重要, 需要判断出 值类型数据(不包括枚举) 是不需要拆箱的
                if (paramType.IsValueType && (paramTypeDef == null || !paramTypeDef.IsEnum))
                {
                    current = InsertAfter(worker, current, worker.Create(OpCodes.Ldarg, argIndex));
                }
                else
                {
                    current = InsertAfter(worker, current, worker.Create(OpCodes.Ldarg, argIndex));
                    current = InsertAfter(worker, current, worker.Create(OpCodes.Box, paramType));
                }
                current = InsertAfter(worker, current, worker.Create(OpCodes.Stelem_Ref));
            }
            current = InsertAfter(worker, current, worker.Create(OpCodes.Call, callPatchRef));
            var methodReturnVoid = method.ReturnType.FullName.Equals("System.Void");
            var patchCallReturnVoid = callPatchRef.ReturnType.FullName.Equals("System.Void");
            // Hotfix.CallPatch()有返回值时
            if (!patchCallReturnVoid)
            {
                // 方法无返回值, 则需先Pop出栈区中CallPatch()返回的结果
                if (methodReturnVoid) current = InsertAfter(worker, current, worker.Create(OpCodes.Pop));
                // 方法有返回值时, 返回值进行拆箱
                else current = InsertAfter(worker, current, worker.Create(OpCodes.Unbox_Any, method.ReturnType));
            }
            // return
            InsertAfter(worker, current, worker.Create(OpCodes.Ret));

            // 重新计算语句位置偏移值
            ComputeOffsets(method.Body);
        }
        /// <summary> 语句前插入Instruction, 并返回当前语句 </summary>
        private static Instruction InsertBefore(ILProcessor worker, Instruction target, Instruction instruction)
        {
            worker.InsertBefore(target, instruction);
            return instruction;
        }
        /// <summary> 语句后插入Instruction, 并返回当前语句 </summary>
        private static Instruction InsertAfter(ILProcessor worker, Instruction target, Instruction instruction)
        {
            worker.InsertAfter(target, instruction);
            return instruction;
        }
        private static void ComputeOffsets(Mono.Cecil.Cil.MethodBody body)
        {
            var offset = 0;
            foreach (var instruction in body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }
        #region generic
        private static bool IsGeneric(MethodDefinition method)
        {
            return method.HasGenericParameters || genericInOut(method);
        }
        private static bool genericInOut(MethodDefinition method)
        {
            if (hasGenericParameter(method.ReturnType) || isNoPublic(method.ReturnType))
            {
                return true;
            }
            var parameters = method.Parameters;

            if (!method.IsStatic
                && (hasGenericParameter(method.DeclaringType) || (method.DeclaringType.IsValueType && isNoPublic(method.DeclaringType))))
            {
                return true;
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                if (hasGenericParameter(parameters[i].ParameterType) ||
                    ((parameters[i].ParameterType.IsValueType ||
                    parameters[i].ParameterType.IsByReference ||
                    parameters[i].CustomAttributes.Any
                    (ca => ca.AttributeType.FullName == "System.ParamArrayAttribute")) && isNoPublic(parameters[i].ParameterType)))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool hasGenericParameter(TypeReference type)
        {
            if (type.HasGenericParameters)
            {
                return true;
            }
            if (type.IsByReference)
            {
                return hasGenericParameter(((ByReferenceType)type).ElementType);
            }
            if (type.IsArray)
            {
                return hasGenericParameter(((ArrayType)type).ElementType);
            }
            if (type.IsGenericInstance)
            {
                foreach (var typeArg in ((GenericInstanceType)type).GenericArguments)
                {
                    if (hasGenericParameter(typeArg))
                    {
                        return true;
                    }
                }
                return false;
            }
            return type.IsGenericParameter;
        }
        static bool isNoPublic(TypeReference type)
        {
            if (type.IsByReference)
            {
                return isNoPublic(((ByReferenceType)type).ElementType);
            }
            if (type.IsArray)
            {
                return isNoPublic(((ArrayType)type).ElementType);
            }
            else
            {
                if (type.IsGenericInstance)
                {
                    foreach (var typeArg in ((GenericInstanceType)type).GenericArguments)
                    {
                        if (isNoPublic(typeArg))
                        {
                            return true;
                        }
                    }
                }

                var resolveType = type.Resolve();
                if ((!type.IsNested && !resolveType.IsPublic) || (type.IsNested && !resolveType.IsNestedPublic))
                {
                    return true;
                }
                if (type.IsNested)
                {
                    var parent = type.DeclaringType;
                    while (parent != null)
                    {
                        var resolveParent = parent.Resolve();
                        if ((!parent.IsNested && !resolveParent.IsPublic) || (parent.IsNested && !resolveParent.IsNestedPublic))
                        {
                            return true;
                        }
                        if (parent.IsNested)
                        {
                            parent = parent.DeclaringType;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return false;
            }

        } 
        #endregion
        #endregion
    }
}