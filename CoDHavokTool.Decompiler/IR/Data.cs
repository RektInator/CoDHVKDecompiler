﻿using System.Collections.Generic;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.LuaDecompiler.IR
{
    /// <summary>
    /// Data instruction. Don't know what it does but doesn't seem to do anything important for decompilation purposes.
    /// Basically have this so the label generation algorithm doesn't break
    /// </summary>
    public class Data : IInstruction
    {
        /// <summary>
        /// Lua locals are often defined at the last data instruction
        /// </summary>
        public List<Local> Locals = null;

        public Instruction Instruction { get; set; }
        
    }
}
