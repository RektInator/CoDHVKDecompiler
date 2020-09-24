using System;
using System.Collections.Generic;
using System.Linq;
using CoDHavokTool.Common;
using CoDHavokTool.Common.Structures;
using CoDHavokTool.LuaDecompiler.IR;

namespace CoDHavokTool.LuaDecompiler
{
    public class Decompiler : IDecompiler
    {
        public Decompiler()
        {

        }

        public string Decompile(ILuaFile luaFile)
        {
            var function = new IR.Function();

            GenerateIRHKS(function, luaFile.MainFunction);

            return function.ToString();
        }

        public static SymbolTable SymbolTable = new SymbolTable();

        private static Expression RKIR(ILuaFunction fun, uint val)
        {
            if (val < 250)
            {
                return new IR.IdentifierReference(SymbolTable.GetRegister(val));
            }
            else
            {
                return ToConstantIR(fun.Constants[(int) (val - 250)]);
            }
        }

        private static Expression RKIRHKS(ILuaFunction fun, uint val, bool szero)
        {
            if (val >= 0 && !szero)
            {
                return new IR.IdentifierReference(SymbolTable.GetRegister((uint)val));
            }
            else if (szero)
            {
                return ToConstantIR(fun.Constants[(int) val]);
            }
            else
            {
                return ToConstantIR(fun.Constants[(int) -val]);
            }
        }

        private static IR.IdentifierReference Register(uint reg)
        {
            return new IR.IdentifierReference(SymbolTable.GetRegister((uint)reg));
        }

        private static IR.Constant ToConstantIR(ILuaConstant con)
        {
            if (con.Type == ConstantType.TNumber)
                return new IR.Constant(con.NumberValue);
            if (con.Type == ConstantType.TString)
                return new IR.Constant(con.StringValue);
            if (con.Type == ConstantType.TBoolean)
                return new IR.Constant(con.BoolValue);
            if (con.Type == ConstantType.THash)
                return new IR.Constant(con.HashValue);
            return new IR.Constant(IR.Constant.ConstantType.ConstNil);
        }

        private static void CheckLocal(IR.Assignment a, ILuaFunction fun, int index)
        {
            a.LocalAssignments = fun.LocalsAt(index + 1);
        }

        public static void GenerateIRHKS(IR.Function irfun, ILuaFunction fun)
        {
            // First register closures for all the children
            for (int i = 0; i < fun.ChildFunctions.Count; i++)
            {
                var cfun = new IR.Function();
                // Upval count needs to be set for child functions for analysis to be correct
                cfun.UpvalCount = fun.ChildFunctions[i].Upvalues.Count;
                irfun.AddClosure(cfun);
            }

            SymbolTable.BeginScope();
            var parameters = new List<IR.Identifier>();
            for (uint i = 0; i < fun.Header.ParameterCount; i++)
            {
                parameters.Add(SymbolTable.GetRegister(i));
            }
            irfun.SetParameters(parameters);

            for (int i = 0; i < fun.Instructions.Count * 4; i += 4)
            {
                var instruction = fun.Instructions[i / 4];
                //uint opcode = instruction & 0x3F;
                // Uhhh thanks again hork
                var opcode = instruction.OpCode;
                var a = instruction.A;
                var c = instruction.C;
                var b = instruction.B;
                var szero = instruction.ExtraCBit;

                var bx = instruction.Bx;
                var sbx = instruction.SBx;
                uint addr;
                var pc = i / 4;
                
                List<IR.Expression> args = null;
                List<IR.IdentifierReference> rets = null;
                List<IR.IInstruction> instructions = new List<IR.IInstruction>();
                IR.Assignment assn;
                switch (opcode)
                {
                    case LuaOpCode.HKS_OPCODE_MOVE:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), Register((uint)b));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_LOADK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), ToConstantIR(fun.Constants[(int) bx]));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_LOADBOOL:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.Constant(b == 1));
                        assn.NilAssignmentReg = a;
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        if (c > 0)
                        {
                            instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2))));
                        }
                        break;
                    case LuaOpCode.HKS_OPCODE_LOADNIL:
                        var nlist = new List<IR.IdentifierReference>();
                        for (int arg = (int)a; arg <= b; arg++)
                        {
                            nlist.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                        }
                        assn = new IR.Assignment(nlist, new IR.Constant(IR.Constant.ConstantType.ConstNil));
                        assn.NilAssignmentReg = a;
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_GETUPVAL:
                        if (b >= irfun.UpvalueBindings.Count)
                        {
                            //throw new Exception("Reference to unbound upvalue");
                        }

                        Identifier up = irfun.UpvalueBindings[(int) b];
                        up.IsClosureBound = true;
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(up)));
                        break;
                    case LuaOpCode.HKS_OPCODE_SETUPVAL:
                    case LuaOpCode.HKS_OPCODE_SETUPVAL_R1:
                        up = SymbolTable.GetUpvalue((uint)b);
                        if (fun.Upvalues.Any() && !up.UpvalueResolved)
                        {
                            up.Name = fun.Upvalues[(int) b].Name;
                            up.UpvalueResolved = true;
                        }
                        instructions.Add(new IR.Assignment(up, new IR.IdentifierReference(SymbolTable.GetRegister(a))));
                        break;
                    case LuaOpCode.HKS_OPCODE_GETGLOBAL_MEM:
                    case LuaOpCode.HKS_OPCODE_GETGLOBAL:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(SymbolTable.GetGlobal(fun.Constants[(int) bx].ToString())));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_GETTABLE_S:
                    case LuaOpCode.HKS_OPCODE_GETTABLE:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(SymbolTable.GetRegister((uint)b), RKIRHKS(fun, c, szero)));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SETGLOBAL:
                        instructions.Add(new IR.Assignment(SymbolTable.GetGlobal(fun.Constants[(int) bx].ToString()), new IR.IdentifierReference(SymbolTable.GetRegister(a))));
                        break;
                    
                    case LuaOpCode.HKS_OPCODE_NEWTABLE:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.InitializerList(new List<IR.Expression>()));
                        assn.VarargAssignmentReg = a;
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SELF:
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a + 1), Register((uint)b)));
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(SymbolTable.GetRegister((uint)b), RKIRHKS(fun, c, szero))));
                        break;
                    case LuaOpCode.HKS_OPCODE_ADD:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpAdd));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_ADD_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpAdd));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SUB:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpSub));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SUB_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpSub));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_MUL:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpMul));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_MUL_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpMul));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_DIV:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpDiv));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_DIV_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpDiv));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_MOD:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpMod));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_MOD_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpMod));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_POW:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpPow));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_POW_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpPow));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_UNM:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a),
                            new IR.UnaryOp(new IR.IdentifierReference(SymbolTable.GetRegister((uint)b)), IR.UnaryOp.OperationType.OpNegate));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_NOT:
                    case LuaOpCode.HKS_OPCODE_NOT_R1:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a),
                            new IR.UnaryOp(new IR.IdentifierReference(SymbolTable.GetRegister((uint)b)), IR.UnaryOp.OperationType.OpNot));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_LEN:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a),
                            new IR.UnaryOp(new IR.IdentifierReference(SymbolTable.GetRegister((uint)b)), IR.UnaryOp.OperationType.OpLength));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SHIFT_LEFT:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpShiftLeft));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SHIFT_LEFT_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpShiftLeft));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SHIFT_RIGHT:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpShiftRight));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_SHIFT_RIGHT_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpShiftRight));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_BITWISE_AND:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpBAnd));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_BITWISE_AND_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpBAnd));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_BITWISE_OR:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), IR.BinOp.OperationType.OpBOr));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_BITWISE_OR_BK:
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), IR.BinOp.OperationType.OpBOr));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_CONCAT:
                        args = new List<IR.Expression>();
                        for (int arg = (int)b; arg <= c; arg++)
                        {
                            args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                        }
                        assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.Concat(args));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_JMP:
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint) (pc + 1 + sbx))));
                        break;
                    case LuaOpCode.HKS_OPCODE_EQ:
                        var operation = IR.BinOp.OperationType.OpEqual;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpNotEqual;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_EQ_BK:
                        operation = IR.BinOp.OperationType.OpEqual;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpNotEqual;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_LT:
                        operation = IR.BinOp.OperationType.OpLessThan;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpGreaterEqual;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_LT_BK:
                        operation = IR.BinOp.OperationType.OpLessThan;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpGreaterEqual;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_LE:
                        operation = IR.BinOp.OperationType.OpLessEqual;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpGreaterThan;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(Register((uint)b), RKIRHKS(fun, c, szero), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_LE_BK:
                        operation = IR.BinOp.OperationType.OpLessEqual;
                        if (a == 1)
                            operation = IR.BinOp.OperationType.OpGreaterThan;
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)(pc + 2)), new IR.BinOp(ToConstantIR(fun.Constants[(int) b]), Register((uint)c), operation)));
                        break;
                    case LuaOpCode.HKS_OPCODE_TEST:
                    case LuaOpCode.HKS_OPCODE_TEST_R1:
                        // This op is weird
                        if (c == 0)
                        {
                            instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2)), Register((uint)a)));
                        }
                        else
                        {
                            instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2)), new IR.UnaryOp(Register((uint)a), IR.UnaryOp.OperationType.OpNot)));
                        }
                        break;
                    case LuaOpCode.HKS_OPCODE_TESTSET:
                        // This op is weird
                        if (c == 0)
                        {
                            instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2)), new IR.BinOp(RKIR(fun, b), new IR.Constant(0.0), IR.BinOp.OperationType.OpNotEqual)));
                        }
                        else
                        {
                            instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2)), new IR.BinOp(RKIR(fun, b), new IR.Constant(0.0), IR.BinOp.OperationType.OpEqual)));
                        }
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(SymbolTable.GetRegister(b))));
                        break;
                    case LuaOpCode.HKS_OPCODE_SETTABLE:
                    case LuaOpCode.HKS_OPCODE_SETTABLE_S:
                        instructions.Add(new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a), Register(b)), RKIRHKS(fun, c, szero)));
                        break;
                    case LuaOpCode.HKS_OPCODE_TAILCALL:
                    case LuaOpCode.HKS_OPCODE_TAILCALL_I:
                    case LuaOpCode.HKS_OPCODE_TAILCALL_I_R1:
                        args = new List<IR.Expression>();
                        for (int arg = (int)a + 1; arg < a + b; arg++)
                        {
                            args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                        }
                        var funCall = new IR.FunctionCall(new IR.IdentifierReference(SymbolTable.GetRegister(a)), args);
                        funCall.IsIndeterminantArgumentCount = (b == 0);
                        funCall.BeginArg = a + 1;
                        instructions.Add(new IR.Return(funCall));
                        break;
                    case LuaOpCode.HKS_OPCODE_SETTABLE_S_BK:
                        instructions.Add(new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a), ToConstantIR(fun.Constants[(int) b])), RKIRHKS(fun, c, szero)));
                        break;
                    case LuaOpCode.HKS_OPCODE_CALL_I:
                    case LuaOpCode.HKS_OPCODE_CALL_I_R1:
                    case LuaOpCode.HKS_OPCODE_CALL:
                        args = new List<IR.Expression>();
                        rets = new List<IR.IdentifierReference>();
                        for (int arg = (int)a + 1; arg < a + b; arg++)
                        {
                            args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                        }
                        for (int r = (int)a + 1; r < a + c; r++)
                        {
                            rets.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)r - 1)));
                        }
                        if (c == 0)
                        {
                            rets.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)a)));
                        }
                        funCall = new IR.FunctionCall(new IR.IdentifierReference(SymbolTable.GetRegister(a)), args);
                        funCall.IsIndeterminantArgumentCount = (b == 0);
                        funCall.IsIndeterminantReturnCount = (c == 0);
                        funCall.BeginArg = a + 1;
                        assn = new IR.Assignment(rets, funCall);
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_RETURN:
                        args = new List<IR.Expression>();
                        if (b != 0)
                        {
                            for (int arg = (int)a; arg < a + b - 1; arg++)
                            {
                                args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                            }
                        }
                        var ret = new IR.Return(args);
                        if (b == 0)
                        {
                            ret.BeginRet = a;
                            ret.IsIndeterminantReturnCount = true;
                        }
                        instructions.Add(ret);
                        break;
                    case LuaOpCode.HKS_OPCODE_FORLOOP:
                        instructions.Add(new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a)), new IR.BinOp(new IR.IdentifierReference(SymbolTable.GetRegister(a)),
                            new IR.IdentifierReference(SymbolTable.GetRegister(a + 2)), IR.BinOp.OperationType.OpAdd)));
                        var jmp = new IR.Jump(irfun.GetLabel((uint) (pc + 1 + sbx)), new IR.BinOp(new IR.IdentifierReference(SymbolTable.GetRegister(a)),
                            new IR.IdentifierReference(SymbolTable.GetRegister(a + 1)), IR.BinOp.OperationType.OpLoopCompare));
                        var pta = new IR.Assignment(SymbolTable.GetRegister(a + 3), Register((uint)a));
                        pta.PropogateAlways = true;
                        jmp.PostTakenAssignment = pta;
                        instructions.Add(jmp);
                        break;
                    case LuaOpCode.HKS_OPCODE_TFORLOOP:
                        args = new List<IR.Expression>();
                        rets = new List<IR.IdentifierReference>();
                        args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)a + 1)));
                        args.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)a + 2)));
                        if (c == 0)
                        {
                            rets.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)a + 3)));
                        }
                        else
                        {
                            for (int r = (int)a + 3; r <= a + c + 2; r++)
                            {
                                rets.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)r)));
                            }
                        }
                        var fcall = new IR.FunctionCall(new IR.IdentifierReference(SymbolTable.GetRegister(a)), args);
                        fcall.IsIndeterminantReturnCount = (c == 0);
                        assn = new IR.Assignment(rets, fcall);
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint)((i / 4) + 2)), new IR.BinOp(Register((uint)a + 3), new IR.Constant(IR.Constant.ConstantType.ConstNil), IR.BinOp.OperationType.OpEqual)));
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a + 2), new IR.IdentifierReference(SymbolTable.GetRegister(a + 3))));
                        break;
                    case LuaOpCode.HKS_OPCODE_FORPREP:
                        // The VM technically does a subtract, but we don't actually emit it since it simplifies things to map better to the high level Lua
                        //instructions.Add(new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a)), new IR.BinOp(new IR.IdentifierReference(SymbolTable.GetRegister(a)),
                        //    new IR.IdentifierReference(SymbolTable.GetRegister(a + 2)), IR.BinOp.OperationType.OpSub)));
                        instructions.Add(new IR.Jump(irfun.GetLabel((uint) (pc + 1 + sbx))));
                        break;
                    case LuaOpCode.HKS_OPCODE_SETLIST:
                        if (b == 0)
                        {
                            if (c == 1)
                            {
                                assn = new IR.Assignment(SymbolTable.GetRegister(a), Register(a + 1));
                                assn.VarargAssignmentReg = a;
                                assn.IsIndeterminantVararg = true;
                                CheckLocal(assn, fun, pc);
                                instructions.Add(assn);
                            }
                        }
                        else
                        {
                            for (int j = 1; j <= b; j++)
                            {
                                assn = new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a), new IR.Constant((double)(c - 1) * 32 + j)),
                                    new IR.IdentifierReference(SymbolTable.GetRegister(a + (uint)j)));
                                CheckLocal(assn, fun, pc);
                                instructions.Add(assn);
                            }
                        }
                        break;
                    case LuaOpCode.HKS_OPCODE_CLOSURE:
                        instructions.Add(new IR.Assignment(SymbolTable.GetRegister(a), new IR.Closure(irfun.LookupClosure(bx))));
                        break;
                    case LuaOpCode.HKS_OPCODE_GETFIELD:
                    case LuaOpCode.HKS_OPCODE_GETFIELD_R1:
                        assn = new IR.Assignment(Register((uint)a), new IR.IdentifierReference(SymbolTable.GetRegister((uint)b), new IR.Constant(fun.Constants[(int) c].ToString())));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_DATA:
                        if (a != 0)
                        {
                            IR.Function closureFunc = null;
                            int index = pc;

                            while (index >= 0)
                            {
                                if (fun.Instructions[index].OpCode == LuaOpCode.HKS_OPCODE_CLOSURE)
                                {
                                    closureFunc = irfun.LookupClosure(fun.Instructions[index].Bx);
                                    break;
                                }
                                index--;
                            }

                            if (closureFunc == null)
                            {
                                continue;
                            }

                            if (a == 1)
                            {
                                closureFunc.UpvalueBindings.Add(SymbolTable.GetRegister(c));
                            }
                            else if (a == 2)
                            {
                                closureFunc.UpvalueBindings.Add(irfun.UpvalueBindings[(int) c]);
                            }
                        }
                        else
                        {
                            instructions.Add(new Data());
                        }
                        break;
                    case LuaOpCode.HKS_OPCODE_SETFIELD:
                    case LuaOpCode.HKS_OPCODE_SETFIELD_R1:
                        assn = new IR.Assignment(new IR.IdentifierReference(SymbolTable.GetRegister(a), new IR.Constant(fun.Constants[(int) b].ToString())), RKIRHKS(fun, c, szero));
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        break;
                    case LuaOpCode.HKS_OPCODE_VARARG:
                        var vargs = new List<IR.IdentifierReference>();
                        for (int arg = (int)a; arg <= a + b - 1; arg++)
                        {
                            vargs.Add(new IR.IdentifierReference(SymbolTable.GetRegister((uint)arg)));
                        }
                        if (b != 0)
                        {
                            assn = new IR.Assignment(vargs, new IR.IdentifierReference(SymbolTable.GetVarargs()));
                        }
                        else
                        {
                            assn = new IR.Assignment(SymbolTable.GetRegister(a), new IR.IdentifierReference(SymbolTable.GetVarargs()));
                            assn.IsIndeterminantVararg = true;
                            assn.VarargAssignmentReg = a;
                        }
                        CheckLocal(assn, fun, pc);
                        instructions.Add(assn);
                        irfun.IsVarargs = true;
                        break;
                    case LuaOpCode.HKS_OPCODE_CLOSE:
                        // LUA source : close all variables in the stack up to (>=) R(A)
                        // Let's ignore this for now, doesn't print anything and don't know if it affects SSA
                        instructions.Add(new Close());
                        break;
                    default:
                        Console.WriteLine($@"Missing op: {opcode} {a} {b} {c}");
                        instructions.Add(new IR.PlaceholderInstruction(($@"{opcode} {a} {b} {c}")));
                        break;
                }
                foreach (var inst in instructions)
                {
                    inst.OpLocation = i / 4;
                    irfun.AddInstruction(inst);
                }
            }
            irfun.ApplyLabels();

            // Simple post-ir and idiom recognition analysis passes
            irfun.ResolveVarargListAssignment(SymbolTable);
            irfun.MergeMultiBoolAssignment();
            irfun.EliminateRedundantAssignments();
            irfun.MergeConditionalJumps();
            irfun.MergeConditionalAssignments();
            //irfun.PeepholeOptimize();
            irfun.CheckControlFlowIntegrity();
            irfun.RemoveUnusedLabels();
            irfun.ClearDataInstructions();

            // Control flow graph construction and SSA conversion
            irfun.ConstructControlFlowGraph();
            irfun.ResolveIndeterminantArguments(SymbolTable);
            irfun.CompleteLua51Loops();
            
            // Upval resolution
            irfun.RegisterClosureUpvalues53(SymbolTable.GetAllRegistersInScope());
            
            irfun.ConvertToSSA(SymbolTable.GetAllRegistersInScope());

            // Data flow passes
            irfun.EliminateDeadAssignments(true);
            irfun.PerformExpressionPropogation();
            irfun.DetectListInitializers();

            // CFG passes
            irfun.StructureCompoundConditionals();
            irfun.DetectLoops();
            irfun.DetectLoopConditionals();
            irfun.DetectTwoWayConditionals();
            irfun.SimplifyIfElseFollowChain();
            
            irfun.EliminateDeadAssignments(true);
            irfun.PerformExpressionPropogation();
            irfun.VerifyLivenessNoInterference();

            // Convert out of SSA and rename variables
            irfun.DropSSADropSubscripts();
            irfun.AnnotateLocalDeclarations();
            
            irfun.RenameVariables();
            irfun.Parenthesize();
            irfun.AddEmptyLines();
            irfun.SearchInlineClosures();

            irfun.RemoveUnnecessaryReturns();

            // Convert to AST
            irfun.ConvertToAST(true);

            // Now generate IR for all the child closures
            for (int i = 0; i < fun.ChildFunctions.Count; i++)
            {
                GenerateIRHKS(irfun.LookupClosure((uint)i), fun.ChildFunctions[i]);
            }
            SymbolTable.EndScope();
        }
    }
}
