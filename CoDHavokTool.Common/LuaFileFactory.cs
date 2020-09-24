using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoDHavokTool.Common.LuaFiles;

namespace CoDHavokTool.Common
{
    public class LuaFileFactory
    {
        public static ILuaFile Create(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File {filePath} not found!", filePath);
            }

            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            var bytes = reader.ReadBytes(13);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            if (bytes[0] != 0x1B || bytes[1] != 0x4C || bytes[2] != 0x75 || bytes[3] != 0x61)
            {
                throw new Exception("Invalid file magic");
            }
            
            // Check the .LJ magic in IW8 lua
            if (bytes[0] == 0x1B && bytes[1] == 0x4C && bytes[2] == 0x4A)
            {
                throw new NotImplementedException("Modern Warfare lua isn't implemented yet");
            }
            
            // Check if lua version is 5.0
            if (bytes[4] == 0x50)
            {
                throw new NotImplementedException("5.0 lua isn't implemented yet");
            }
            
            // Check compiler version
            if (bytes[5] == 0x0D)
            {
                return new LuaFileT6(filePath, reader);
            }
            
            // Check if they use big endian
            //if(bytes[6] == 0x00)
            //    return new LuaFileDS(filePath, reader);
            
            //if(bytes[12] == 0x03)
            //    return new LuaFileIW(filePath, reader);

            return null;
        }
    }
}
