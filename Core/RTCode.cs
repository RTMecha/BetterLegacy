using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Mono.CSharp;

using BetterLegacy.Configs;

namespace BetterLegacy.Core
{

    //Based on UnityExplorer at https://github.com/sinai-dev/UnityExplorer and RuntimeUnityEditor at https://github.com/ManlyMarco/RuntimeUnityEditor
    public static class RTCode
    {
        public static string className = "[<color=#F5501B>RTCode</color>] \n";

        public static ScriptEvaluator Evaluator { get; private set; }

        static readonly StringBuilder _sb = new StringBuilder();

        static HashSet<string> usingDirectives;

        static readonly string[] defaultUsings = new string[]
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection",
            "UnityEngine",
            "BetterLegacy",
            "BetterLegacy.Components",
            "BetterLegacy.Configs",
            "BetterLegacy.Core",
            "BetterLegacy.Core.Animation",
            "BetterLegacy.Core.Animation.Keyframe",
            "BetterLegacy.Core.Data",
            "BetterLegacy.Core.Managers",
            "BetterLegacy.Core.Optimization",
            "BetterLegacy.Core.Optimization.Objects",
            "BetterLegacy.Core.Optimization.Objects.Visual",
            "BetterLegacy.Core.Prefabs",
            "BetterLegacy.Editor",
            "BetterLegacy.Editor.Managers",
        };

        public static void AddUsing(string assemblyName)
        {
            if (!usingDirectives.Contains(assemblyName))
            {
                Evaluate($"using {assemblyName};");
                usingDirectives.Add(assemblyName);
            }
        }

        public static void Init()
        {
            Evaluator = new ScriptEvaluator(new StringWriter(_sb));
            usingDirectives = new HashSet<string>();
            foreach (string use in defaultUsings)
                AddUsing(use);
        }

        public static void Evaluate(string input, Action<Exception> onError = null)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl == null)
                    return;

                try
                {
                    object ret = null;
                    repl.Invoke(ref ret);
                    string result = ret?.ToString();
                    if (!string.IsNullOrEmpty(result))
                        LogREPL($"{className}Invoked REPL, result: {ret}", EditorManager.NotificationType.Success);
                    else
                        LogREPL($"{className}Invoked REPL (no return value)", EditorManager.NotificationType.Success);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                    LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
            }
        }

        public static object EvaluateWithReturn(string input)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl == null)
                    return null;

                try
                {
                    object ret = null;
                    repl.Invoke(ref ret);
                    string result = ret?.ToString();
                    if (!string.IsNullOrEmpty(result))
                        LogREPL($"{className}Invoked REPL, result: {ret}", EditorManager.NotificationType.Success);
                    else
                        LogREPL($"{className}Invoked REPL (no return value)", EditorManager.NotificationType.Success);
                    return ret;
                }
                catch (Exception ex)
                {
                    LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                return null;
            }
        }

        public static IEnumerator IEvaluate(string input)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl == null)
                    yield break;

                try
                {
                    object ret = null;
                    repl.Invoke(ref ret);
                    string result = ret?.ToString();
                    if (!string.IsNullOrEmpty(result))
                        LogREPL($"{className}Invoked REPL, result: {ret}", EditorManager.NotificationType.Success);
                    else
                        LogREPL($"{className}Invoked REPL (no return value)", EditorManager.NotificationType.Success);
                }
                catch (Exception ex)
                {
                    LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                    yield break;
                }
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                yield break;
            }
        }

        public static void LogREPL(string rep, EditorManager.NotificationType notificationType = EditorManager.NotificationType.Info)
        {
            if (!CoreConfig.Instance.NotifyREPL.Value)
                return;

            if (ProjectArrhythmia.State.InEditor && EditorManager.inst.isEditing)
            {
                EditorManager.inst.DisplayNotification(rep, 1f, notificationType);
            }
            else
            {
                switch (notificationType)
                {
                    case EditorManager.NotificationType.Info:
                    case EditorManager.NotificationType.Success: {
                            UnityEngine.Debug.Log(rep);
                            break;
                        }
                    case EditorManager.NotificationType.Warning: {
                            UnityEngine.Debug.LogWarning(rep);
                            break;
                        }
                    case EditorManager.NotificationType.Error: {
                            UnityEngine.Debug.LogError(rep);
                            break;
                        }
                }
            }
        }

        public static Action ConvertToAction(string input)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl == null)
                    return null;

                try
                {
                    return repl.InvokeEmpty;
                }
                catch (Exception ex)
                {
                    LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);

                    return null;
                }
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);

                return null;
            }
        }

        public static void InvokeEmpty(this CompiledMethod compiledMethod)
        {
            object ret = null;
            compiledMethod.Invoke(ref ret);
        }

        public static bool Validate(string str) =>
                CoreConfig.Instance.EvaluateCode.Value && !str.Contains("File.") && !str.Contains("FileManager") && !str.Contains("System.IO") && !str.Contains("WebClient") && !str.Contains("HttpClient");
    }

    public class ScriptEvaluator : Evaluator, IDisposable
    {
        static readonly HashSet<string> StdLib = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "mscorlib", "System.Core", "System", "System.Xml" };

        readonly TextWriter _logger;

        public ScriptEvaluator(TextWriter logger) : base(BuildContext(logger))
        {
            _logger = logger;

            ImportAppdomainAssemblies(ReferenceAssembly);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            _logger.Dispose();
        }

        void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string name = args.LoadedAssembly.GetName().Name;
            if (StdLib.Contains(name))
                return;
            ReferenceAssembly(args.LoadedAssembly);
        }

        static CompilerContext BuildContext(TextWriter tw)
        {
            var reporter = new StreamReportPrinter(tw);

            var settings = new CompilerSettings
            {
                Version = LanguageVersion.Experimental,
                GenerateDebugInfo = false,
                StdLib = true,
                Target = Target.Library,
                WarningLevel = 0,
                EnhancedWarnings = false
            };

            return new CompilerContext(settings, reporter);
        }

        static void ImportAppdomainAssemblies(Action<Assembly> import)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (StdLib.Contains(name))
                    continue;
                import(assembly);
            }
        }
    }
}
