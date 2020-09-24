using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common.LuaFiles
{
    public abstract class LuaFile : ILuaFile
    {
        // properties
        public string FilePath { get; }
        public FileHeader Header { get; private set; }
        public IList<ILuaConstant> Constants { get; private set; }
        public ILuaFunction MainFunction { get; private set; }

        // private variables
        protected readonly BinaryReader Reader;

        protected LuaFile(string filePath, BinaryReader reader)
        {
            FilePath = filePath;
            Reader = reader;

            Parse();
        }

        private void Parse()
        {
            Header = ReadHeader();
            Constants = ReadConstants();
            MainFunction = ReadFunctions();
        }

        protected abstract FileHeader ReadHeader();
        protected abstract IList<ILuaConstant> ReadConstants();
        protected abstract ILuaFunction ReadFunctions();
    }
}
