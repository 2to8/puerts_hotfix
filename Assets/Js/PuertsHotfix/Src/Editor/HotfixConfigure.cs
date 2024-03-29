﻿using System;

namespace Puerts
{
    /// <summary> 获取类型的配置 </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HotfixConfigAttribute : Attribute { }
    /// <summary> 获取hotfix列表 </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class HotfixListAttribute : Attribute { }
    /// <summary> 获取方法是否过滤 </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HotfixFilterAttribute : Attribute { }
    /// <summary> hotfix生成的js代码目录 -- 默认Resources下 </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class HotfixCodeDirectoryAttribute : Attribute { }

    public struct HotfixConfig
    {
        /// <summary> 是否忽略非public方法 </summary>
        public bool ignoreNotPublic;

        public static HotfixConfig GetDefault()
        {
            return new HotfixConfig { ignoreNotPublic = false };
        }
    }
}
