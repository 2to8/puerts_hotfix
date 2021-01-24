var global = global || (function () { return this; }());
(function (global) {
    "use strict";
    var hotfix = {};
    var patchList = {};
    const patchKey = 'patch';
    try { hotfix.map = require('hotfix_map'); } catch { console.error("not found hotfix_map"); }
    const CS = require('csharp');
    const puerts = require('puerts');
    const CSHotfix = CS.Puerts.Hotfix;
    const injectFlag = CS.System.Type.GetType("__PUERTS_GEN.__PUERTS_INJECT_FLAG");
    hotfix.isInject = injectFlag != null; // 是否已注入

    function hasPatch(className, methodName, methodId) {
        if (patchList[className] != null) {
            if (patchList[className][methodName] != null) {
                if (patchList[className][methodName][methodId] != null) {
                    return true;
                } else if (patchList[className][methodName][patchKey] != null) {
                    return true;
                }
            }
        }
        return false;
    }

    function callPatch(className, methodName, methodId, args) {
        let fun = patchList[className][methodName][methodId];
        if (fun == null) fun = patchList[className][methodName][patchKey];
        if (fun != null) {
            if (args != null) {
                let length = args.Length;
                if (length > 1) {
                    let self = args.GetValue(0);
                    let arg = args.GetValue(1);
                    let argLength = arg == null ? 0 : arg.Length;
                    let jsArg = [];
                    for (let i = 0; i < argLength; i++) {
                        const a = arg.GetValue(i);
                        jsArg.push(a);
                    }
                    if (self == null) {
                        return fun(...jsArg);
                    } else {
                        return fun(self, ...jsArg);
                    }
                } else {
                    return fun();
                }
            } else {
                return fun();
            }
        }
    }

    CSHotfix.jsHasPatch = hasPatch;
    CSHotfix.jsCallPatch = callPatch;

    hotfix.patch = function (type, methodName, patchFun) {
        hotfix.patchId(type, methodName, patchKey, patchFun);
    }

    hotfix.patchId = function (type, methodName, methodId, patchFun) {
        if (!hotfix.isInject) {
            console.error("not inject");
            return;
        }
        let t = puerts.$typeof(type);

        if (hotfix.map[t] == null || hotfix.map[t][methodName] == null) {
            console.error("not found : " + t + "." + methodName);
            return;
        }
        if (methodId != patchKey) {
            if (hotfix.map[t][methodName][methodId] == null) {
                console.error("not found : " + t + "." + methodName + "(id:" + methodId + ")");
                return;
            }
        }

        if (patchList[t] == null) {
            patchList[t] = {};
        }
        if (patchList[t][methodName] == null) {
            patchList[t][methodName] = {};
        }
        patchList[t][methodName][methodId] = patchFun;
    }

    global.puerts.hotfix = hotfix;
}(global));
