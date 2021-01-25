using Puerts;
using UnityEngine;

namespace PuertsTest
{
    public class JsHotfixTest : MonoBehaviour
    {
        private JsEnv jsEnv;
        private void Awake()
        {
            jsEnv = new JsEnv();
            Hotfix.Init(jsEnv);
            jsEnv.Eval(@"require('test_hotfix');");
        }
        private void Start()
        {
            HotfixTest();
            HotfixTest(2, TestHotfixEnum.Test, this.gameObject);
            Debug.Log("HotfixTest1 : " + HotfixTest1());
            Debug.Log("HotfixTest2 : " + HotfixTest2());
            NoHotfixTest();
            StaticMethod();
        }
        private void HotfixTest()
        {
            Debug.Log("C# :: HotfixTest();");
        }
        private void HotfixTest(int hotfixTest, TestHotfixEnum testEnum, GameObject go)
        {
            Debug.Log("C# :: HotfixTest(); :: " + hotfixTest + " " + testEnum + " " + go.name);
        }
        public double HotfixTest1()
        {
            return 1;
        }
        public GameObject HotfixTest2()
        {
            return this.gameObject;
        }
        private void NoHotfixTest()
        {
            Debug.Log("C# :: NoHotfixTest");
        }
        public static void StaticMethod()
        {
            Debug.Log("C# :: StaticMethod");
        }

        private void OnDestroy()
        {
            jsEnv.Dispose();
        }

        public enum TestHotfixEnum
        {
            Test = 0,
        }
    }
}
