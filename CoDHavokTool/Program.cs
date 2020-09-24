using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoDHavokTool.Common;
using CoDHavokTool.LuaDecompiler;

namespace CoDHavokTool
{
    public class Program
    {
        private readonly IDecompiler _decompiler;

        public Program(IDecompiler decompiler)
        {
            _decompiler = decompiler;
        }

        private void HandleFile(string filePath)
        {
            try
            {
                // parse lua file
                var file = LuaFileFactory.Create(filePath);

                // decompile file
                var output = _decompiler.Decompile(file);

                // replace extension
                var outFileName = Path.ChangeExtension(filePath, ".dec.lua");

                // save output
                File.WriteAllText(outFileName, output);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static List<string> ParseFilesFromArgs(IEnumerable<string> args)
        {
            var files = new List<string>();

            foreach (var arg in args)
            {
                var attr = File.GetAttributes(arg);
                // determine if we're a directory first
                // if so only includes file that are of ".lua" or ".luac" extension
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    files.AddRange(Directory.GetFiles(arg, "*.lua*", SearchOption.AllDirectories).ToList());
                }
                else if (Path.GetExtension(arg).Contains(".lua"))
                {
                    files.Add(arg);
                }
                else
                {
                    Console.WriteLine($"Invalid argument passed {arg} | {File.GetAttributes(arg)}!");
                }
            }
            
            // make sure to remove duplicates
            files = files.Distinct().ToList();

            // also remove any already dumped files
            files.RemoveAll(elem => elem.EndsWith(".dec.lua"));
            files.RemoveAll(elem => elem.EndsWith(".luadec"));

            return files;
        }

        public void Main(string[] args)
        {
            // parse files from arguments
            var files = ParseFilesFromArgs(args);
            
            Console.WriteLine($"Total of {files.Count} to process.");

            // 
            files.ForEach(HandleFile);
        }
    }
}
