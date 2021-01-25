const CS = require('csharp');

function Hotfix() {
    if (!puerts.hotfix.isInject) {
        console.warn('Please Click "Puerts/Inject Editor"');
        return;
    }

    // 普通方法
    puerts.hotfix.patch(CS.PuertsTest.JsHotfixTest, 'HotfixTest1', (self) => {
        return 0.2;
    });
    puerts.hotfix.patch(CS.PuertsTest.JsHotfixTest, 'HotfixTest2', (self) => {
        return new CS.UnityEngine.GameObject("Js HotfixTest2");
    });

    // 第三个参数为方法id, 会生成在hotfix_map.js.txt中
    // 重载方法
    puerts.hotfix.patchId(CS.PuertsTest.JsHotfixTest, 'HotfixTest', 0, (self) => {
        console.log('Js的方法 :: HotfixTest();');
    });
    puerts.hotfix.patchId(CS.PuertsTest.JsHotfixTest, 'HotfixTest', 1, (self, hotfixTest, testEnum, go) => {
        console.log('Js的方法 :: HotfixTest(); :: ' + hotfixTest + " " + testEnum + " " + go.name);
    });

    // 静态方法
    puerts.hotfix.patch(CS.PuertsTest.JsHotfixTest, 'StaticMethod', () => {
        console.log('Js的方法 :: StaticMethod');
    });
}
Hotfix();
