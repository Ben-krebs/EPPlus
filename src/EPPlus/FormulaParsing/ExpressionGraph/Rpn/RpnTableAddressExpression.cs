﻿using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Diagnostics;

namespace OfficeOpenXml.FormulaParsing.ExpressionGraph.Rpn
{
    [DebuggerDisplay("TableAddressExpression: {_addressInfo}")]
    internal class RpnTableAddressExpression : RpnExpression
    {
        readonly FormulaTableAddress _addressInfo;
        private bool _negate;

        public RpnTableAddressExpression(FormulaTableAddress addressInfo, ParsingContext ctx) : base(ctx)
        {
            _addressInfo = addressInfo;
        }
        internal override ExpressionType ExpressionType => ExpressionType.TableAddress;

        public override CompileResult Compile()
        {
            if(_addressInfo.ExternalReferenceIx > 0)
            {
                return new CompileResult(eErrorType.Ref);
            }
            else
            {
                _addressInfo.SetTableAddress(Context.Package);
                if(_addressInfo.FromRow < 1)
                {
                    return new CompileResult(eErrorType.Ref);
                }
            }
            var ri = Context.ExcelDataProvider.GetRange(_addressInfo);
            if (ri.IsMulti)
            {
                return new AddressCompileResult(ri, DataType.ExcelRange, _addressInfo);
            }
            else
            {
                return CompileResultFactory.Create(ri.GetOffset(0,0), _addressInfo);
            }
        }

        public override void Negate()
        {
            _negate = !_negate;
        }
        internal override RpnExpressionStatus Status
        {
            get;
            set;
        } = RpnExpressionStatus.CanCompile;
        public override FormulaRangeAddress GetAddress() 
        { 
            return _addressInfo.Clone();
        }
    }
}