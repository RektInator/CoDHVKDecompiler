using System;
using System.Linq;
using Autofac;
using CoDHavokTool.Common;
using CoDHavokTool.LuaDecompiler;

namespace CoDHavokTool
{
    internal class Startup
    {
        private static void Main(string[] args)
        {
            //
            Console.WriteLine("CoD Havok Decompiler made from katalash's DSLuaDecompiler");

            // setup dependency injection
            var builder = new ContainerBuilder();

            // CoDHavokTool.Common
            builder.Register((context, parameters) =>
            {
                if (parameters.Count() != 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return LuaFileFactory.Create(parameters.Positional<string>(0));
            }).As<ILuaFile>().InstancePerLifetimeScope();
            
            // CoDHavokTool.LuaDecompiler
            builder.RegisterType<Decompiler>().As<IDecompiler>().SingleInstance();

            // CodHavokTool
            builder.RegisterType<Program>().SingleInstance();


            var container = builder.Build();
            container.Resolve<Program>().Main(args);
        }
    }
}
