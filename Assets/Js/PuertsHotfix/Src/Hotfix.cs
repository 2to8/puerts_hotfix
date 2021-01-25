namespace Puerts
{
    public delegate bool HotfixHasPatch(string className, string methodName, int methodId);
    public delegate object HotfixCallPatch(string className, string methodName, int methodId, object[] args);
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
            jsEnv.UsingFunc<string, string, int, bool>();
            jsEnv.UsingFunc<string, string, int, object[], object>();
            jsEnv.Eval(@"require('hotfix')");
        }
        public static bool HasPatch(string className, string methodName, int methodId)
        {
            if (!isInit || jsHasPatch == null) return false;
            return jsHasPatch.Invoke(className, methodName, methodId);
        }
        public static object CallPatch(string className, string methodName, int methodId, params object[] args)
        {
            if (jsCallPatch == null) return null;
            if(args.Length > 0)
            {
                UnityEngine.Debug.Log(args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    UnityEngine.Debug.Log(args[i]);
                }
            }
            return jsCallPatch.Invoke(className, methodName, methodId, args);
        }
    }
}
