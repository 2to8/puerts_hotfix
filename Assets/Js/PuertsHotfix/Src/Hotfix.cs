namespace Puerts
{
    public delegate bool HotfixHasPatch(string className, string methodName, int methodId);
    public delegate object HotfixCallPatch(string className, string methodName, int methodId, object obj, params object[] args);
    public class Hotfix
    {
        public static HotfixHasPatch jsHasPatch;
        public static HotfixCallPatch jsCallPatch;
        private static bool isInit;
        private static JsEnv jsEnv;
        public static void Init(JsEnv env)
        {
            if (isInit) return;
            isInit = true;
            jsEnv = env;
            jsEnv.Eval(@"require('hotfix')");
        }
        public static bool HasPatch(string className, string methodName, int methodId)
        {
            if (!isInit || jsHasPatch == null) return false;
            return jsHasPatch.Invoke(className, methodName, methodId);
        }
        public static object CallPatch(string className, string methodName, int methodId, object obj, params object[] args)
        {
            if (jsCallPatch == null) return null;
            return jsCallPatch.Invoke(className, methodName, methodId, obj, args);
        }
    }
}
