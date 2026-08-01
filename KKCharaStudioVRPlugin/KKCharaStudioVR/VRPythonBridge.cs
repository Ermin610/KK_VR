using System;
using System.Reflection;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal static class VRPythonBridge
{
    private static object _mainEngine;
    private static MethodInfo _createScriptSourceMethod;

    public static bool TryExecute(string code, string operation, out string error)
    {
        error = null;
        if (!TryResolveEngine(out error))
        {
            VRLog.Warn(operation + " failed: " + error);
            return false;
        }

        try
        {
            object scriptSource = _createScriptSourceMethod.Invoke(_mainEngine, new object[] { code });
            if (scriptSource == null)
                throw new InvalidOperationException("Python script source was not created.");

            MethodInfo compileMethod = scriptSource.GetType().GetMethod("Compile", Type.EmptyTypes);
            if (compileMethod == null)
                throw new MissingMethodException(scriptSource.GetType().FullName, "Compile");

            object compiledCode = compileMethod.Invoke(scriptSource, null);
            if (compiledCode == null)
                throw new InvalidOperationException("Python code was not compiled.");

            MethodInfo executeMethod = compiledCode.GetType().GetMethod("Execute", Type.EmptyTypes);
            if (executeMethod == null)
                throw new MissingMethodException(compiledCode.GetType().FullName, "Execute");

            executeMethod.Invoke(compiledCode, null);
            VRLog.Info(operation + " completed.");
            return true;
        }
        catch (Exception ex)
        {
            Exception root = Unwrap(ex);
            error = string.IsNullOrEmpty(root.Message)
                ? root.GetType().Name
                : root.GetType().Name + ": " + root.Message;
            _mainEngine = null;
            _createScriptSourceMethod = null;
            VRLog.Error(operation + " failed: " + root);
            return false;
        }
    }

    private static bool TryResolveEngine(out string error)
    {
        error = null;
        if (_mainEngine != null && _createScriptSourceMethod != null)
            return true;

        try
        {
            Assembly consoleAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Unity.Console")
                {
                    consoleAssembly = assembly;
                    break;
                }
            }

            if (consoleAssembly == null)
            {
                error = "Unity.Console is not loaded. VNGE/MMDD may still be starting.";
                return false;
            }

            Type programType = consoleAssembly.GetType("Unity.Console.Program");
            MethodInfo getMainEngine = programType?.GetMethod(
                "get_MainEngine",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _mainEngine = getMainEngine?.Invoke(null, null);
            _createScriptSourceMethod = _mainEngine?.GetType().GetMethod(
                "CreateScriptSourceFromString",
                new Type[] { typeof(string) });

            if (_mainEngine == null || _createScriptSourceMethod == null)
            {
                error = "VNGE Python engine is not ready.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = Unwrap(ex).Message;
            _mainEngine = null;
            _createScriptSourceMethod = null;
            return false;
        }
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is TargetInvocationException && ex.InnerException != null)
            ex = ex.InnerException;
        return ex;
    }
}
