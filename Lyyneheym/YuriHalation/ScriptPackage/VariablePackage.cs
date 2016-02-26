﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YuriHalation.ScriptPackage
{
    /// <summary>
    /// 变量包装类
    /// </summary>
    [Serializable]
    class VariablePackage
    {
        /// <summary>
        /// 变量名
        /// </summary>
        public string varName = "";

        /// <summary>
        /// 是否全局变量
        /// </summary>
        public bool isGlobal = false;

        /// <summary>
        /// 最先作为左值的行
        /// </summary>
        public int firstLeftValueLine = Int32.MaxValue;

        /// <summary>
        /// 被引用次数
        /// </summary>
        public int referenceCount = 0;
    }
}
