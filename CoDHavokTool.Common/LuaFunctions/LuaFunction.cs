using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common.LuaFunctions
{
    public abstract class LuaFunction : ILuaFunction
    {
        // properties
        public ILuaFile LuaFile { get; }
        public FunctionHeader Header { get; private set; }
        public FunctionFooter Footer { get; private set; }
        public long FunctionPos { get; private set; }
        public long FunctionLength { get; private set; }
        public IList<Instruction> Instructions { get; } = new List<Instruction>();
        public IList<ILuaConstant> Constants { get; private set; }
        public IList<ILuaFunction> ChildFunctions { get; private set; }
        public IList<Upvalue> Upvalues { get; set; } = new List<Upvalue>();
        public IList<Local> Locals { get; set; } = new List<Local>();
        public Dictionary<int, List<Local>> LocalMap { get; set; } = new Dictionary<int, List<Local>>();            // should be private?

        // private variables
        protected readonly BinaryReader Reader;

        protected LuaFunction(ILuaFile luaFile, BinaryReader reader)
        {
            LuaFile = luaFile;
            Reader = reader;

            Parse();
        }

        private void Parse()
        {
            FunctionPos = Reader.BaseStream.Position;
            Header = ReadFunctionHeader();

            for (var i = 0; i < Header.InstructionCount; i++)
            {
                Instructions.Add(ReadInstruction());
            }

            Constants = ReadConstants();
            Footer = ReadFunctionFooter();
            ChildFunctions = ReadChildFunctions();
        }

        protected abstract FunctionHeader ReadFunctionHeader();
        protected abstract FunctionFooter ReadFunctionFooter();
        protected abstract Instruction ReadInstruction();
        protected abstract IList<ILuaConstant> ReadConstants();
        protected abstract IList<ILuaFunction> ReadChildFunctions();

        public List<Local> LocalsAt(int i)
        {
            if (LocalMap.ContainsKey(i))
            {
                return LocalMap[i];
            }
            
            return null;
        }
    }
}
