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
            jsEnv.Eval(@"const CS = require('csharp');

                         // 重载方法
                         puerts.hotfix.patchId(CS.PuertsTest.JsHotfixTest, 'HotfixTest', 0, (self)=>{
                            console.log('Js的方法 :: void HotfixTest(); ---- self : ' + self + ' ----');
                         });
                         puerts.hotfix.patchId(CS.PuertsTest.JsHotfixTest, 'HotfixTest', 1, (self, hotfixTest)=>{
                            console.log('Js的方法 :: void HotfixTest(int hotfixTest); - ' + hotfixTest);
                         });

                         // 普通方法
                         puerts.hotfix.patch(CS.PuertsTest.JsHotfixTest, 'HotfixTest1', (self)=>{
                            let result = 1;
                            console.log('Js的方法 :: double HotfixTest1(); - ' + result);
                            return result;
                         });
                       ");
        }
        private void Start()
        {
            HotfixTest();
            HotfixTest(2);
            HotfixTest1();
            NoHotfixTest();
        }
        private void HotfixTest()
        {
            Debug.Log("C#的方法 :: void HotfixTest();");
        }
        private void HotfixTest(int hotfixTest)
        {
            Debug.Log("C#的方法 :: void HotfixTest(int hotfixTest); - " + hotfixTest);
        }
        public double HotfixTest1()
        {
            var result = 0;
            Debug.Log("C#的方法 :: double HotfixTest1(); - " + result);
            return result;
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
