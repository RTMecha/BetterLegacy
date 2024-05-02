using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

namespace BetterLegacy.Patchers
{
    public delegate bool PrefixMethod();
    public delegate bool PrefixMethod<T>(T obj);
    public delegate bool PrefixMethod<T, T1>(T obj, T1 obj1);
    public delegate bool PrefixMethod<T, T1, T2>(T obj, T1 obj1, T2 obj2);
    public delegate bool PrefixMethod<T, T1, T2, T3>(T obj, T1 obj1, T2 obj2, T3 obj3);
    public delegate bool PrefixMethod<T, T1, T2, T3, T4>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4);
    public delegate bool PrefixMethod<T, T1, T2, T3, T4, T5>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5);
    public delegate bool PrefixMethod<T, T1, T2, T3, T4, T5, T6>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5, T6 obj6);

    public delegate Exception FinalizerMethod();
    public delegate Exception FinalizerMethod<T>(T obj);
    public delegate Exception FinalizerMethod<T, T1>(T obj, T1 obj1);
    public delegate Exception FinalizerMethod<T, T1, T2>(T obj, T1 obj1, T2 obj2);
    public delegate Exception FinalizerMethod<T, T1, T2, T3>(T obj, T1 obj1, T2 obj2, T3 obj3);
    public delegate Exception FinalizerMethod<T, T1, T2, T3, T4>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4);
    public delegate Exception FinalizerMethod<T, T1, T2, T3, T4, T5>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5);
    public delegate Exception FinalizerMethod<T, T1, T2, T3, T4, T5, T6>(T obj, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5, T6 obj6);

    public delegate IEnumerable<CodeInstruction> TranspilerMethod(IEnumerable<CodeInstruction> obj);

    public enum PatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer,
        ILManipulator
    }

    /// <summary>
    /// An attempt at a runtime patcher.
    /// </summary>
    public static class Patcher
    {
        public static void PatchPropertySetter(Type type, string property, BindingFlags bindingFlags, bool nonPublic,
            Type patchType, string patchMethod, Type[] patchParameters, PatchType fix = PatchType.Prefix)
        {
            var propertySetter = type.GetProperty(property, bindingFlags).GetSetMethod(nonPublic);

            var methodPrefix = AccessTools.Method(patchType, patchMethod, patchParameters);

            HarmonyMethod methodPatch = new HarmonyMethod(methodPrefix);

            switch (fix)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(propertySetter, prefix: methodPatch);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(propertySetter, postfix: methodPatch);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(propertySetter, transpiler: methodPatch);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(propertySetter, finalizer: methodPatch);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(propertySetter, ilmanipulator: methodPatch);
                        break;
                    }
            }
        }

        public static void PatchMethod(Type type, string method, Type[] parameters,
            Type patchType, string patchMethod, Type[] patchParameters, PatchType fix = PatchType.Prefix)
        {
            var methodToPatch = AccessTools.Method(type, method, parameters);

            var methodPrefix = AccessTools.Method(patchType, patchMethod, patchParameters);

            var methodPatch = new HarmonyMethod(methodPrefix);

            switch (fix)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(methodToPatch, prefix: methodPatch);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(methodToPatch, postfix: methodPatch);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(methodToPatch, transpiler: methodPatch);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(methodToPatch, finalizer: methodPatch);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(methodToPatch, ilmanipulator: methodPatch);
                        break;
                    }
            }
        }

        public static void TestPatch()
        {
            UnityEngine.Debug.Log($"Test patch invoked!");
            //CreatePatchMethod(typeof(Patcher), "", BindingFlags.Public | BindingFlags.Instance, PatchType.Prefix, (PrefixMethod<string>)delegate (string s)
            //{
            //    return false;
            //});
        }

        public static void PatchExamples()
        {
            // Initial Prefix patch test
            CreatePatchMethod(typeof(Patcher), "TestPatch", BindingFlags.Public | BindingFlags.Static, PatchType.Prefix, (PrefixMethod)delegate ()
            {
                UnityEngine.Debug.Log($"Ayy");
                return false;
            });

            // Transpiler patch test
            CreatePatchMethod(typeof(Patcher), "s", BindingFlags.Public | BindingFlags.Instance, PatchType.Transpiler, (TranspilerMethod)delegate (IEnumerable<CodeInstruction> codeInstructions)
            {
                var codeMatcher = new CodeMatcher(codeInstructions);

                codeMatcher.Start();
                for (int i = 0; i < codeMatcher.Length; i++)
                {
                    UnityEngine.Debug.Log($"Instruction: {codeMatcher.Instruction}");
                    codeMatcher.Advance(1);
                }

                return codeMatcher.InstructionEnumeration();
            });

            // Constructor patch test
            var method = AccessTools.Constructor(typeof(Patcher));

            CreatePatch(method, PatchType.Prefix, (Action)delegate () { });

            // Existing void as a method patcher test
            CreatePatch(method, PatchType.Prefix, (Action)TestPatch);

            // Finalizer example
            CreatePatch(method, PatchType.Finalizer, (FinalizerMethod)delegate ()
            {
                throw new Exception();
            });

            CreatePatchConstructor(typeof(DataManager.GameData.BeatmapObject), new Type[] { }, PatchType.Prefix, (PrefixMethod<DataManager.GameData.BeatmapObject>)delegate (DataManager.GameData.BeatmapObject __instance)
            {
                UnityEngine.Debug.Log($"{LegacyPlugin.className}{__instance}");
                return true;
            });

            CreatePatchConstructor(typeof(DataManager.GameData.BeatmapObject), new Type[] { typeof(float) }, PatchType.Prefix, (PrefixMethod<DataManager.GameData.BeatmapObject, float>)delegate (DataManager.GameData.BeatmapObject __instance, float __0)
            {
                UnityEngine.Debug.Log($"{LegacyPlugin.className}{__instance}\nTime: {__0}");
                return true;
            });

            CreatePatchConstructor(typeof(DataManager.GameData.BeatmapObject),
                new Type[] { typeof(bool), typeof(float), typeof(string), typeof(int), typeof(string), typeof(List<List<DataManager.GameData.EventKeyframe>>) },
                PatchType.Prefix,
                (PrefixMethod<DataManager.GameData.BeatmapObject, bool, float, string, int, string, List<List<DataManager.GameData.EventKeyframe>>>)delegate (DataManager.GameData.BeatmapObject __instance, bool __0, float __1, string __2, int __3, string __4, List<List<DataManager.GameData.EventKeyframe>> __5)
            {
                UnityEngine.Debug.Log($"{LegacyPlugin.className}{__instance}\nActive: {__0}\nTime: {__1}\nName: {__2}\nShape: {__3}\nText: {__4}\nEvents: {__5}");
                return true;
            });

            var list = new List<string>();

            var e = list.ToDictionary((string t) => int.Parse(t), (string t) => t);

            int num = DataManager.inst.AllThemes.Count;
            var dictionary = DataManager.inst.AllThemes.ToDictionary((DataManager.BeatmapTheme t) => DataManager.inst.AllThemes.FindAll(x => x.id == t.id).Count == 1 ? int.Parse(t.id) : num++, (DataManager.BeatmapTheme t) => t);

            // Unpatch example
            LegacyPlugin.harmony.Unpatch(method, HarmonyPatchType.All, LegacyPlugin.harmony.Id);

            CreatePatch(AccessTools.Method(typeof(EditorManager), nameof(EditorManager.inst.AddToPitch)), PatchType.Prefix, (PrefixMethod<float>)delegate (float t) { return false; });
        }

        public static void CreatePatchMethod(Type typeToPatch, string methodName, BindingFlags bindingFlags, PatchType patchType, Delegate action)
        {
            var method = typeToPatch.GetMethod(methodName, bindingFlags);

            var harmonyMethod = new HarmonyMethod(action.Method);

            switch (patchType)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(method, prefix: harmonyMethod);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(method, postfix: harmonyMethod);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(method, transpiler: harmonyMethod);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(method, finalizer: harmonyMethod);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(method, ilmanipulator: harmonyMethod);
                        break;
                    }
            }
        }
        
        public static void CreatePatchMethod(Type typeToPatch, string methodName, Type[] parameters, PatchType patchType, Delegate action)
        {
            var method = AccessTools.Method(typeToPatch, methodName, parameters);

            var harmonyMethod = new HarmonyMethod(action.Method);

            switch (patchType)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(method, prefix: harmonyMethod);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(method, postfix: harmonyMethod);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(method, transpiler: harmonyMethod);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(method, finalizer: harmonyMethod);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(method, ilmanipulator: harmonyMethod);
                        break;
                    }
            }
        }

        public static void CreatePatchConstructor(Type typeToPatch, Type[] parameters, PatchType patchType, Delegate action)
        {
            var method = AccessTools.Constructor(typeToPatch, parameters);

            var harmonyMethod = new HarmonyMethod(action.Method);

            switch (patchType)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(method, prefix: harmonyMethod);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(method, postfix: harmonyMethod);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(method, transpiler: harmonyMethod);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(method, finalizer: harmonyMethod);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(method, ilmanipulator: harmonyMethod);
                        break;
                    }
            }
        }

        public static void CreatePatch(Action method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);
        public static void CreatePatch<T>(Action<T> method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);
        public static void CreatePatch<T, T1>(Action<T, T1> method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);
        public static void CreatePatch<T, T1, T2>(Action<T, T1, T2> method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);
        public static void CreatePatch<T, T1, T2, T3>(Action<T, T1, T2, T3> method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);

        public static void CreatePatch(Action method, PatchType patchType, PrefixMethod prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T>(Action<T> method, PatchType patchType, PrefixMethod prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1>(Action<T, T1> method, PatchType patchType, PrefixMethod prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);

        public static void CreatePatch<P>(Action method, PatchType patchType, PrefixMethod<P> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<P, P1>(Action method, PatchType patchType, PrefixMethod<P, P1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<P, P1, P2>(Action method, PatchType patchType, PrefixMethod<P, P1, P2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<P, P1, P2, P3>(Action method, PatchType patchType, PrefixMethod<P, P1, P2, P3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        
        public static void CreatePatch<T, P>(Action<T> method, PatchType patchType, PrefixMethod<P> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, P, P1>(Action<T> method, PatchType patchType, PrefixMethod<P, P1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, P, P1, P2>(Action<T> method, PatchType patchType, PrefixMethod<P, P1, P2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, P, P1, P2, P3>(Action<T> method, PatchType patchType, PrefixMethod<P, P1, P2, P3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        
        public static void CreatePatch<T, T1, P>(Action<T, T1> method, PatchType patchType, PrefixMethod<P> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, P, P1>(Action<T, T1> method, PatchType patchType, PrefixMethod<P, P1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, P, P1, P2>(Action<T, T1> method, PatchType patchType, PrefixMethod<P, P1, P2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, P, P1, P2, P3>(Action<T, T1> method, PatchType patchType, PrefixMethod<P, P1, P2, P3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        
        public static void CreatePatch<T, T1, T2, P>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod<P> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, P, P1>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod<P, P1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, P, P1, P2>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod<P, P1, P2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, P, P1, P2, P3>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod<P, P1, P2, P3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        
        public static void CreatePatch<T, T1, T2, T3, P>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod<P> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3, P, P1>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod<P, P1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3, P, P1, P2>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod<P, P1, P2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3, P, P1, P2, P3>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod<P, P1, P2, P3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        
        public static void CreatePatch<T>(Action<T> method, PatchType patchType, PrefixMethod<T> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1>(Action<T, T1> method, PatchType patchType, PrefixMethod<T, T1> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2>(Action<T, T1, T2> method, PatchType patchType, PrefixMethod<T, T1, T2> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3>(Action<T, T1, T2, T3> method, PatchType patchType, PrefixMethod<T, T1, T2, T3> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);
        public static void CreatePatch<T, T1, T2, T3, T4>(Action<T, T1, T2, T3, T4> method, PatchType patchType, PrefixMethod<T, T1, T2, T3, T4> prefixMethod) => CreatePatch(method.Method, patchType, prefixMethod.Method);

        public static void CreatePatch(Delegate method, PatchType patchType, Delegate action) => CreatePatch(method.Method, patchType, action);

        public static void CreatePatch(MethodBase method, PatchType patchType, Delegate action) => CreatePatch(method, patchType, action.Method);

        public static void CreatePatch(MethodBase method, PatchType patchType, MethodInfo patcher)
        {
            var harmonyMethod = new HarmonyMethod(patcher);

            switch (patchType)
            {
                case PatchType.Prefix:
                    {
                        LegacyPlugin.harmony.Patch(method, prefix: harmonyMethod);
                        break;
                    }
                case PatchType.Postfix:
                    {
                        LegacyPlugin.harmony.Patch(method, postfix: harmonyMethod);
                        break;
                    }
                case PatchType.Transpiler:
                    {
                        LegacyPlugin.harmony.Patch(method, transpiler: harmonyMethod);
                        break;
                    }
                case PatchType.Finalizer:
                    {
                        LegacyPlugin.harmony.Patch(method, finalizer: harmonyMethod);
                        break;
                    }
                case PatchType.ILManipulator:
                    {
                        LegacyPlugin.harmony.Patch(method, ilmanipulator: harmonyMethod);
                        break;
                    }
            }
        }
    }
}
