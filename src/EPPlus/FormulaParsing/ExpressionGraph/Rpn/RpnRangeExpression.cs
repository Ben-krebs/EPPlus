﻿using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using OfficeOpenXml.FormulaParsing.ExpressionGraph.Rpn;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Diagnostics;
using Operators = OfficeOpenXml.FormulaParsing.Excel.Operators.Operators;

namespace OfficeOpenXml.FormulaParsing.ExpressionGraph
{
    [DebuggerDisplay("CellAddressExpression: {ExpressionString}")]
    internal class RpnRangeExpression : RpnExpression
    {
        IRangeInfo _range;
        bool _negate=false;
        public RpnRangeExpression(IRangeInfo address, ParsingContext ctx) : base(ctx)
        {
            _range = address;
        }
        internal override ExpressionType ExpressionType => ExpressionType.ExcelRange;
        CompileResult _result;
        public override CompileResult Compile()
        {
            if (_result == null)
            {
                _result = new AddressCompileResult(_range, DataType.ExcelRange, _range.Address);
            }
            return _result;
        }
        public override void Negate()
        {
            _negate = !_negate;
        }
    }
}
