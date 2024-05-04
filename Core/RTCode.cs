using BetterLegacy.Configs;
using Mono.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterLegacy.Core
{

    //Based on UnityExplorer at https://github.com/sinai-dev/UnityExplorer and RuntimeUnityEditor at https://github.com/ManlyMarco/RuntimeUnityEditor
    public static class RTCode
    {
        public static string className = "[<color=#F5501B>RTCode</color>] \n";

        public static ScriptEvaluator Evaluator { get; private set; }

        static readonly StringBuilder _sb = new StringBuilder();

        public static Dictionary<string, string> REPLColors = new Dictionary<string, string>
        {
            // 569CD6
            { "DefaultType", "569CD6" },
            // 57A64A
            { "Comment", "57A64A" },
            // D8A0DF
            { "ExtraFunc", "DE83EA" },
            // DCDCAA
            { "Method", "DE5B16" },
            // 4EC9B0
            { "Type", "4CB59C" },
            // D69D85
            { "String", "E08F59" },
        };

        static HashSet<string> usingDirectives;

        static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection",
            "UnityEngine",
            "RTFunctions",
            "RTFunctions.Functions",
            "RTFunctions.Functions.Components",
            "RTFunctions.Functions.Managers",
            "RTFunctions.Functions.Animation",
            "RTFunctions.Functions.Animation.Keyframe",
            "RTFunctions.Functions.Optimization",
            "RTFunctions.Functions.Optimization.Objects",
            "RTFunctions.Functions.Optimization.Objects.Visual",
        };

        static readonly List<string> DefaultTypes = new List<string>
        {
            "float",
            "int",
            "string",
            "var",
            "byte",
            "public",
            "private",
            "virtual",
            "internal",
            "static",
            "readonly",
            "struct",
            "void",
            "class",
            "using",
            "new",
            "namespace",
            "nameof",
            "typeof",
        };

        static readonly List<string> ExtraFuncs = new List<string>
        {
            "if",
            "try",
            "catch",
            "finally",
            "for",
            "foreach",
        };

        public static List<string> Types = new List<string>();
        public static Dictionary<string, List<string>> Methods = new Dictionary<string, List<string>>();

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
            foreach (string use in DefaultUsing)
                AddUsing(use);

            //foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    foreach (var t in a.GetTypes())
            //    {
            //        string str = t.ToString();
            //        if (!string.IsNullOrEmpty(str))
            //        {
            //            //if (str.Contains("."))
            //            //    str = str.Replace(str.Substring(0, str.LastIndexOf('.') + 1), "");

            //            if (!Methods.ContainsKey(str))
            //            {
            //                var list = new List<string>();
            //                foreach (var m in t.GetMethods())
            //                {
            //                    var mstr = m.ToString();
            //                    if (mstr.Contains(" "))
            //                        mstr = mstr.Replace(mstr.Substring(0, mstr.LastIndexOf(' ') + 1), "").Replace("[T]", "");

            //                    var regex = new Regex(@"\((.*?)\)");
            //                    var match = regex.Match(mstr);
            //                    if (match.Success)
            //                        mstr = mstr.Replace($"({match.Groups[1]})", "");

            //                    list.Add(mstr);
            //                }
            //                Methods.Add(str, list);
            //            }

            //            Types.Add(str);
            //        }
            //    }
            //}

            //Types = Types.OrderByDescending(x => x.Length).ToList();
        }

        public static void Evaluate(string input)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl != null)
                {
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
                    }
                }
            }
            catch (Exception ex)
            {
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

                if (repl != null)
                {
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
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                return null;
            }

            return null;
        }

        public static IEnumerator IEvaluate(string input)
        {
            if (Evaluator == null)
                Init();

            try
            {
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl != null)
                {
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
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);
                yield break;
            }

            yield break;
        }

        public static string ConvertREPLTest(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var lines = input.GetLines();

            string result = "";

            foreach (var line in lines)
            {
                string a = line;

                var split = line.Split(new string[] { "//" }, StringSplitOptions.None).ToList();

                string first = split[0];

                for (int i = 0; i < DefaultTypes.Count; i++)
                    first = first.Replace(DefaultTypes[i], $"<color=#{REPLColors["DefaultType"]}>{DefaultTypes[i]}</color>");

                for (int i = 0; i < ExtraFuncs.Count; i++)
                {
                    var length = ExtraFuncs[i].Length;
                    var spaceless = first.Replace(" ", "");

                    if (spaceless.Length >= length && spaceless.Substring(0, length) == ExtraFuncs[i])
                        first = first.Replace(ExtraFuncs[i], $"<color=#{REPLColors["ExtraFunc"]}>{ExtraFuncs[i]}</color>");
                }

                var regexString = new Regex("\"(.*?)\"");
                var matchString = regexString.Match(first);
                if (matchString.Success)
                {
                    first = first.Replace($"\"{matchString.Groups[1]}\"", $"<color=#{REPLColors["String"]}>\"{matchString.Groups[1].ToString().Replace($"<color=#{REPLColors["DefaultType"]}>", "").Replace($"<color=#{REPLColors["ExtraFunc"]}>", "").Replace($"<color=#{REPLColors["Type"]}>", "").Replace($"<color=#{REPLColors["Method"]}>", "").Replace("</color>", "")}\"</color>");
                }

                //var dots = first.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).ToList();
                //string d = "";

                //for (int i = 0; i < dots.Count; i++)
                //{
                //    if (Types.Contains(dots[i]))
                //        d += dots[i];
                //}

                if (split.Count > 1)
                {
                    a = first + "//" + $"<color=#{REPLColors["Comment"]}>{split[1]}";
                }

                result += a + Environment.NewLine;
            }

            return result;
        }

        public static string ConvertREPL(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var lines = GetLines(input, 200);
            string result = "";
            foreach (var line in lines)
            {
                var regex = new Regex(@"//(.*?[a-zA-Z0-9.,/ _-]+)");
                var match = regex.Match(line);
                if (match.Success)
                {
                    result += line.Replace("//" + match.Groups[1].ToString(), $"<color=#{REPLColors["Comment"]}>//{match.Groups[1]}</color>") + Environment.NewLine;
                }
                else
                {
                    string a = line;

                    for (int i = 0; i < DefaultTypes.Count; i++)
                    {
                        a = a.Replace(DefaultTypes[i], $"<color=#{REPLColors["DefaultType"]}>{DefaultTypes[i]}</color>");
                    }

                    for (int i = 0; i < ExtraFuncs.Count; i++)
                    {
                        var length = ExtraFuncs[i].Length;
                        var spaceless = a.Replace(" ", "");

                        if (spaceless.Length >= length && spaceless.Substring(0, length) == ExtraFuncs[i])
                            a = a.Replace(ExtraFuncs[i], $"<color=#{REPLColors["ExtraFunc"]}>{ExtraFuncs[i]}</color>");
                    }

                    //for (int i = 0; i < Types.Count; i++)
                    //{
                    //    var str = Types[i];
                    //    if (str.Contains("."))
                    //        str = str.Replace(str.Substring(0, str.LastIndexOf('.') + 1), "");

                    //    a = a.Replace(str, $"<color=#{REPLColors["Type"]}>{str}</color>");

                    //    if (Methods.ContainsKey(Types[i]) && a.Contains(Types[i]) && Methods[Types[i]].Find(x => a.Contains(x)) != null)
                    //    {
                    //        var method = Methods[Types[i]].Find(x => a.Contains(x));
                    //        a = a.Replace(method, $"<color=#{REPLColors["Method"]}>{method}</color>");
                    //    }
                    //}

                    //var regexMethod = new Regex(@"([a-zA-Z0-9_]+)\(");
                    //var matchMethod = regexMethod.Match(a);
                    //if (matchMethod.Success)
                    //{
                    //    a = a.Replace(matchMethod.Groups[1].ToString(), $"<color=#{REPLColors["Method"]}>{matchMethod.Groups[1]}</color>");
                    //}

                    var regexString = new Regex("\"(.*?)\"");
                    var matchString = regexString.Match(a);
                    if (matchString.Success)
                    {
                        a = a.Replace($"\"{matchString.Groups[1]}\"", $"<color=#{REPLColors["String"]}>\"{matchString.Groups[1].ToString().Replace($"<color=#{REPLColors["DefaultType"]}>", "").Replace($"<color=#{REPLColors["ExtraFunc"]}>", "").Replace($"<color=#{REPLColors["Type"]}>", "").Replace($"<color=#{REPLColors["Method"]}>", "").Replace("</color>", "")}\"</color>");
                    }

                    result += a + Environment.NewLine;
                }
            }

            return result;
        }

        public static IEnumerator ConvertStringToREPL(string input, Action<string> output)
        {
            var lines = GetLines(input, 200);
            string result = "";
            foreach (var line in lines)
            {
                var regex = new Regex(@"//(.*?[a-zA-Z0-9.,/ _-]+)");
                var match = regex.Match(line);
                if (match.Success)
                {
                    result += line.Replace("//" + match.Groups[1].ToString(), $"<color=#{REPLColors["Comment"]}>//{match.Groups[1]}</color>") + Environment.NewLine;
                }
                else
                {
                    string a = line;

                    for (int i = 0; i < DefaultTypes.Count; i++)
                    {
                        a = a.Replace(DefaultTypes[i], $"<color=#{REPLColors["DefaultType"]}>{DefaultTypes[i]}</color>");
                    }

                    for (int i = 0; i < ExtraFuncs.Count; i++)
                    {
                        var length = ExtraFuncs[i].Length;
                        var spaceless = a.Replace(" ", "");

                        if (spaceless.Length >= length && spaceless.Substring(0, length) == ExtraFuncs[i])
                            a = a.Replace(ExtraFuncs[i], $"<color=#{REPLColors["ExtraFunc"]}>{ExtraFuncs[i]}</color>");
                    }

                    //for (int i = 0; i < Types.Count; i++)
                    //{
                    //    var str = Types[i];
                    //    if (str.Contains("."))
                    //        str = str.Replace(str.Substring(0, str.LastIndexOf('.') + 1), "");

                    //    a = a.Replace(str, $"<color=#{REPLColors["Type"]}>{str}</color>");

                    //    if (Methods.ContainsKey(Types[i]) && a.Contains(Types[i]) && Methods[Types[i]].Find(x => a.Contains(x)) != null)
                    //    {
                    //        var method = Methods[Types[i]].Find(x => a.Contains(x));
                    //        a = a.Replace(method, $"<color=#{REPLColors["Method"]}>{method}</color>");
                    //    }
                    //}

                    //var regexMethod = new Regex(@"([a-zA-Z0-9_]+)\(");
                    //var matchMethod = regexMethod.Match(a);
                    //if (matchMethod.Success)
                    //{
                    //    a = a.Replace(matchMethod.Groups[1].ToString(), $"<color=#{REPLColors["Method"]}>{matchMethod.Groups[1]}</color>");
                    //}

                    var regexString = new Regex("\"(.*?)\"");
                    var matchString = regexString.Match(a);
                    if (matchString.Success)
                    {
                        a = a.Replace($"\"{matchString.Groups[1]}\"", $"<color=#{REPLColors["String"]}>\"{matchString.Groups[1].ToString().Replace($"<color=#{REPLColors["DefaultType"]}>", "").Replace($"<color=#{REPLColors["ExtraFunc"]}>", "").Replace($"<color=#{REPLColors["Type"]}>", "").Replace($"<color=#{REPLColors["Method"]}>", "").Replace("</color>", "")}\"</color>");
                    }

                    result += a + Environment.NewLine;
                }
            }

            output(result);

            yield break;
        }

        public static List<string> GetLines(string value, int lineLength)
        {
            List<string> list = value.Split(new string[]
            {
                "\n",
                "\r\n",
                "\r"
            }, StringSplitOptions.None).ToList();
            for (int i = 0; i < list.Count(); i++)
            {
                string text = list[i];
                if (text.Length >= lineLength)
                {
                    list[i] = text.Substring(0, lineLength);
                    list.Insert(i + 1, text.Substring(lineLength, text.Length - lineLength));
                }
            }
            return list;
        }

        public static void LogREPL(string rep, EditorManager.NotificationType notificationType = EditorManager.NotificationType.Info)
        {
            if (!CoreConfig.Instance.NotifyREPL.Value)
                return;

            if (EditorManager.inst && EditorManager.inst.isEditing)
            {
                EditorManager.inst.DisplayNotification(rep, 1f, notificationType);
            }
            else
            {
                switch (notificationType)
                {
                    case EditorManager.NotificationType.Info:
                    case EditorManager.NotificationType.Success:
                        {
                            UnityEngine.Debug.Log(rep);
                            break;
                        }
                    case EditorManager.NotificationType.Warning:
                        {
                            UnityEngine.Debug.LogWarning(rep);
                            break;
                        }
                    case EditorManager.NotificationType.Error:
                        {
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

                if (repl != null)
                {
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
            }
            catch (Exception ex)
            {
                LogREPL($"{className}Exception invoking REPL: {ex}", EditorManager.NotificationType.Warning);

                return null;
            }

            return null;
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
