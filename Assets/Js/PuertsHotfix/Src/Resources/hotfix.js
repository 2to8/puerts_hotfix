var global = global || (function () { return this; }());
(function (global) {
    "use strict";
    var hotfix = {};
    try { hotfix.map = require("hotfix_map"); } catch { console.log("not found hotfix_map"); }
    const CS = require('csharp');
    const CSHotfix = CS.Puerts.Hotfix;

    function hasPatch(className, methodName, methodId) {
        
    }

    function callPatch(className, methodName, methodId, obj, args) {
        
    }

    CSHotfix.jsHasPatch = hasPatch;
    CSHotfix.jsCallPatch = callPatch;

    global.puerts.hotfix = hotfix;
}(global));
