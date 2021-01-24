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

            // jsEnv.Eval("console.log(puerts.hotfix == null)");
        }
        private void Start()
        {
            HotfixTest();
            HotfixTest(2);
            NoHotfixTest();
        }
        private void HotfixTest()
        {
            Debug.Log("C#的方法 :: HotfixTest");
        }
        private void HotfixTest(int hotfixTest2)
        {
            Debug.Log("C#的方法 :: HotfixTest2 - " + hotfixTest2);
        }
        private void NoHotfixTest()
        {
            Debug.Log("C#的方法 :: NoHotfixTest");
        }

        private void OnDestroy()
        {
            jsEnv.Dispose();
        }
    }
}
