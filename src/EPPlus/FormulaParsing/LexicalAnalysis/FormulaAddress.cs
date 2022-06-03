﻿using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Text;
using static OfficeOpenXml.ExcelAddressBase;

namespace OfficeOpenXml.FormulaParsing.LexicalAnalysis
{
    internal class Formula
    {
        internal ExcelWorksheet _ws;
        internal int StartRow, StartCol;
        internal static ISourceCodeTokenizer _tokenizer = OptimizedSourceCodeTokenizer.Default;
        internal IList<Token> Tokens;
        internal ExpressionGraph.ExpressionGraph _graph;
        public Formula(ExcelWorksheet ws, string formula)
        {
            _ws = ws;
            Init(ws, formula);
        }

        private void Init(ExcelWorksheet ws, string formula)
        {
            Tokens = _tokenizer.Tokenize(formula);
            SetTokenInfos();
            var ctx = ParsingContext.Create(ws._package);
            ctx.ExcelDataProvider = new EpplusExcelDataProvider(ws._package, ctx);
            var graphBuilder = new ExpressionGraphBuilder(ctx.ExcelDataProvider, ctx);
            _graph = graphBuilder.Build(Tokens);
            using (var s = ctx.Scopes.NewScope(new FormulaRangeAddress(ctx){ FromCol=StartCol, FromRow=StartRow, ToCol=StartCol, ToRow=StartRow, WorksheetIx=(short)ws.PositionId}))
            {
                var compiler = new ExpressionCompiler(ctx);
                var result = compiler.Compile(_graph.Expressions);
            }
        }

        public Formula(ExcelWorksheet ws, int row, int col, string formula)
        {
            _ws = ws;
            StartRow = row;
            StartCol = col;
            Init(ws, formula);            
        }
        internal void SetRowCol(int row, int col)
        {
            StartRow = row;
            StartCol = col;
        }
        internal Dictionary<int, TokenInfo> TokenInfos;
        private void SetTokenInfos()
        {
            TokenInfos = new Dictionary<int, TokenInfo>();
            string er = "", ws = "";
            short startToken = -1;
            for (short i = 0; i < Tokens.Count; i++)
            {
                var t = Tokens[i];
                switch (t.TokenType)
                {

                    case TokenType.CellAddress:
                        var fa = new FormulaCellAddress(i, t.Value, (short)_ws.PositionId);
                        TokenInfos.Add(i, fa);
                        er = ws = "";
                        break;
                    case TokenType.NameValue:
                        AddNameInfo(startToken == -1 ? i : startToken, i, er, ws);
                        er = ws = "";
                        break;
                    case TokenType.TableName:
                        AddTableAddress(i);
                        er = ws = "";
                        break;
                    case TokenType.WorksheetNameContent:
                        if (startToken == -1)
                        {
                            startToken = i;
                        }
                        ws = t.Value;
                        break;
                    case TokenType.ExternalReference:
                        er = t.Value;
                        break;
                    case TokenType.OpeningBracket:
                        if (startToken == -1)
                        {
                            startToken = i;
                        }
                        break;
                }
            }
        }
        private void AddTableAddress(short pos)
        {
            short i = pos;
            var t = Tokens[i];
            TokenInfos = new Dictionary<int, TokenInfo>();
            var table = _ws.Workbook.GetTable(t.Value);
            if (table != null)
            {
                if (Tokens[++i].TokenType == TokenType.OpeningBracket)
                {
                    int fromRow = 0, toRow = 0, fromCol = 0, toCol = 0;
                    FixedFlag fixedFlag = FixedFlag.All;
                    bool lastColon = false;
                    var bc = 1;
                    i++;
                    while (bc > 0 && i < Tokens.Count)
                    {
                        switch (Tokens[i].TokenType)
                        {
                            case TokenType.OpeningBracket:
                                bc++;
                                break;
                            case TokenType.ClosingBracket:
                                bc--;
                                break;
                            case TokenType.TablePart:
                                SetRowFromTablePart(Tokens[i].Value, table, ref fromRow, ref toRow, ref fixedFlag);
                                break;
                            case TokenType.TableColumn:
                                SetColFromTablePart(Tokens[i].Value, table, ref fromCol, ref toCol, lastColon);
                                break;
                            case TokenType.Colon:
                                lastColon = true;
                                break;
                            default:
                                lastColon = false;
                                break;
                        }
                        i++;
                    }
                    if (bc == 0)
                    {
                        if (fromRow == 0)
                        {
                            fromRow = table.DataRange._fromRow;
                            toRow = table.DataRange._toRow;
                        }

                        if (fromCol == 0)
                        {
                            fromCol = table.DataRange._fromCol;
                            toCol = table.DataRange._toCol;
                        }

                        i--;
                        TokenInfos.Add(pos, new FormulaRange(pos, i, fromRow, fromCol, toRow, toCol, fixedFlag));
                    }
                }
                else
                {
                    TokenInfos.Add(pos, new FormulaRange(pos, i, table.DataRange._fromRow, table.DataRange._fromCol, table.DataRange._toRow, table.DataRange._toCol, 0));
                }
            }
        }

        private void SetColFromTablePart(string value, ExcelTable table, ref int fromCol, ref int toCol, bool lastColon)
        {
            var col = table.Columns[value];
            if (col == null) return;
            if (lastColon)
            {
                toCol = table.Range._fromCol + col.Position;
            }
            else
            {
                fromCol = toCol = table.Range._fromCol + col.Position;
            }
        }
        private void SetRowFromTablePart(string value, ExcelTable table, ref int fromRow, ref int toRow, ref FixedFlag fixedFlag)
        {
            switch (value.ToLower())
            {
                case "#all":
                    fromRow = table.Address._fromRow;
                    toRow = table.Address._toRow;
                    break;
                case "#headers":
                    if (table.ShowHeader)
                    {
                        fromRow = table.Address._fromRow;
                        if (toRow == 0)
                        {
                            toRow = table.Address._fromRow;
                        }
                    }
                    else if (fromRow == 0)
                    {
                        fromRow = toRow = -1;
                    }
                    break;
                case "#data":
                    if (fromRow == 0 || table.DataRange._fromRow < fromRow)
                    {
                        fromRow = table.DataRange._fromRow;
                    }
                    if (table.DataRange._toRow > toRow)
                    {
                        toRow = table.DataRange._toRow;
                    }
                    break;
                case "#totals":
                    if (table.ShowTotal)
                    {
                        if (fromRow == 0)
                            fromRow = table.Range._toRow;
                        toRow = table.Range._toRow;
                    }
                    else if (fromRow == 0)
                    {
                        fromRow = toRow = -1;
                    }
                    break;
                case "#this row":
                    var dr = table.DataRange;
                    if (_ws != table.WorkSheet || StartRow < dr._fromRow || StartRow > dr._toRow)
                    {
                        fromRow = toRow = -1;
                    }
                    else
                    {
                        fromRow = StartRow;
                        toRow = StartRow;
                        fixedFlag = FixedFlag.FromColFixed | FixedFlag.ToColFixed;
                    }
                    break;
            }
        }
        private void AddNameInfo(short startPos, short namePos, string er, string ws)
        {
            var t = Tokens[namePos];
            if (string.IsNullOrEmpty(er))    //TODO: add support for external refrence
            {
                ExcelNamedRange n = null;
                if (string.IsNullOrEmpty(ws))
                {
                    if (_ws.Names.ContainsKey(t.Value))
                    {
                        n = _ws.Names[t.Value];
                    }
                    else if (_ws.Workbook.Names.ContainsKey(t.Value))
                    {
                        n = _ws.Workbook.Names[t.Value];
                    }
                }
                else
                {
                    var wsRef = _ws.Workbook.Worksheets[ws];
                    if (wsRef != null && wsRef.Names.ContainsKey(t.Value))
                    {                        
                        n = wsRef.Names[t.Value];
                    }
                }
                if (n == null)
                {
                    //The name is a table.
                    var tbl = _ws.Workbook.GetTable(t.Value);
                    if (tbl != null)
                    {
                        var fr = new FormulaRange(startPos, namePos, tbl.DataRange);
                        fr.Ranges[0].FixedFlag = FixedFlag.All; //a Tables data range is allways fixed.
                        TokenInfos.Add(startPos, fr);
                    }
                }
                else
                {
                    if (n.NameValue != null)
                    {
                        TokenInfos.Add(startPos, new FormulaFixedValue(startPos, namePos, n.NameValue));
                    }
                    else if (n.Formula != null)
                    {
                        TokenInfos.Add(startPos, new FormulaNamedFormula(startPos, namePos, n.NameFormula));
                    }
                    else
                    {
                        TokenInfos.Add(startPos, new FormulaRange(startPos, namePos, n));
                    }
                }
            }
        }
    }
    internal class SharedFormula : Formula
    {        
        internal int EndRow, EndCol;
        int _rowOffset = 0, _colOffset = 0;
        public SharedFormula(ExcelWorksheet ws, string address, string formula) : base(ws, formula)
        {
            _ws = ws;
            Formula = formula;
            ExcelCellBase.GetRowColFromAddress(address, out StartRow, out StartCol, out EndRow, out EndCol);
        }

        public SharedFormula(ExcelRangeBase range, string formula) : this(range.Worksheet, range._fromRow, range._fromCol, range._toRow, range._toCol, formula)
        {
        }

        public SharedFormula(ExcelWorksheet ws, int fromRow, int fromCol, int toRow, int toCol, string formula) : base(ws, fromRow, fromCol, formula)
        {
            EndRow = toRow;
            EndCol = toCol;
            Formula = formula;
        }

        internal int Index { get; set; }
        internal string Formula { get; set; }
        internal bool IsArray { get; set; }
        internal string Address
        {
            get
            {
                return ExcelCellBase.GetAddress(StartRow, StartCol, EndRow, EndCol);
            }
            set
            {
                ExcelCellBase.GetRowColFromAddress(value, out StartRow, out StartCol, out EndRow, out EndCol);
            }
        }
        internal void SetOffset(string wsName, int rowOffset, int colOffset)
        {
            SetOffset(rowOffset, colOffset);
        }
        internal void SetOffset(int rowOffset, int colOffset)
        {
            var changeRowOffset = rowOffset - _rowOffset;
            var changeColOffset = colOffset - _colOffset;
            foreach (var t in TokenInfos.Values)
            {
                switch(t.Type)
                {
                    case FormulaType.CellAddress:
                    case FormulaType.FormulaRange:
                        t.SetOffset(changeRowOffset, changeColOffset);
                        break;                    
                }
            }
            _rowOffset = rowOffset;
            _colOffset = colOffset;
        }

        internal SharedFormula Clone()
        {
            return new SharedFormula(_ws, StartRow, StartCol, EndRow, EndCol, Formula)
            {
                Index = Index,
                IsArray = IsArray,
                Tokens = Tokens,
                TokenInfos = TokenInfos,
                _ws = _ws,
            };
        }
        internal string GetFormula(int row, int column, string worksheet)
        {
            if (StartRow == row && StartCol == column)
            {
                return Formula;
            }

            SetTokens(worksheet);
            string f = "";
            foreach (var token in Tokens)
            {
                if (token.TokenTypeIsSet(TokenType.ExcelAddress))
                {
                    var a = new ExcelFormulaAddress(token.Value, (ExcelWorksheet)null);
                    if (a.IsFullColumn)
                    {
                        if (a.IsFullRow)
                        {
                            f += token.Value;
                        }
                        else
                        {
                            f += a.GetOffset(0, column - StartCol, true);
                        }
                    }
                    else if (a.IsFullRow)
                    {
                        f += a.GetOffset(row - StartRow, 0, true);
                    }
                    else
                    {
                        if (a.Table != null)
                        {
                            f += token.Value;
                        }
                        else
                        {
                            f += a.GetOffset(row - StartRow, column - StartCol, true);
                        }
                    }
                }
                else
                {
                    if (token.TokenTypeIsSet(TokenType.StringContent))
                    {
                        f += "\"" + token.Value.Replace("\"", "\"\"") + "\"";
                    }
                    else
                    {
                        f += token.Value;
                    }
                }
            }
            return f;
        }
        internal void SetTokens(string worksheet)
        {
            if (Tokens == null)
            {
                Tokens = _tokenizer.Tokenize(Formula, worksheet);
            }
        }
    }
    internal enum FormulaType
    {
        CellAddress,
        FormulaRange,
        FixedValue,
        Formula
    }
    [Flags]
    internal enum FixedFlag : byte
    {
        None = 0,
        FromRowFixed = 0x1,
        FromColFixed = 0x2,
        ToRowFixed = 0x4,
        ToColFixed = 0x8,
        All = 0xF,
    }
    internal abstract class TokenInfo
    {
        internal FormulaType Type;
        internal short TokenStartPosition;
        internal short TokenEndPosition;
        internal virtual void SetOffset(int rowOffset, int colOffset) { }

        internal abstract string GetValue();

        internal virtual bool IsFixed { get { return true; } }
    }
    internal class FormulaCellAddress : TokenInfo
    {
        internal FormulaCellAddress(short pos, string cellAddress, short worksheetIx)
        {
            WorksheetIx = worksheetIx;
            Type = FormulaType.CellAddress; 
            TokenStartPosition = TokenEndPosition = pos;
            ExcelCellBase.GetRowColFromAddress(cellAddress, out Row, out Col, out FixedRow, out FixedCol);
        }
        internal short ExternalReferenceIx, WorksheetIx;
        internal int Row, Col;
        internal bool FixedRow, FixedCol;
        internal override void SetOffset(int rowOffset, int colOffset)
        {
            if (!FixedRow) Row += rowOffset;
            if (!FixedCol) Col += colOffset;
        }
        internal override bool IsFixed { get { return FixedRow & FixedCol; } }
        internal override string GetValue()
        {
            return ExcelCellBase.GetAddress(Row, FixedRow, Col, FixedCol);
        }
    }
    internal class FormulaFixedValue : TokenInfo
    {
        public FormulaFixedValue(short startPos, short endPos, object v)
        {
            Type = FormulaType.FixedValue;
            TokenStartPosition = startPos;
            TokenEndPosition = endPos;
            Value = v;
        }
        internal object Value;
        internal override string GetValue()
        {
            return Value.ToString();
        }
    }
    internal class FormulaNamedFormula : TokenInfo
    {
        public FormulaNamedFormula(short startPos, short endPos, string f)
        {
            Type = FormulaType.Formula;
            TokenStartPosition = startPos;
            TokenEndPosition = endPos;
            Formula = f;
        }
        internal string Formula;
        internal override bool IsFixed { get { return false; } } //TODO: Check here if we can us fixed from the actual formula in  later stage.
        internal override string GetValue()
        {
            return Formula;
        }
    }
    internal class FormulaRange : TokenInfo
    {
        ParsingContext _ctx;
        public FormulaRange(ParsingContext ctx)
        {
            _ctx = ctx;
        }
        internal override void SetOffset(int rowOffset, int colOffset)
        { 
            for(int i=0;i < Ranges.Count;i++)
            {
                var r=Ranges[i];
                if ((r.FixedFlag & FixedFlag.FromRowFixed) == FixedFlag.None) r.FromRow += rowOffset;
                if ((r.FixedFlag & FixedFlag.ToRowFixed) == FixedFlag.None) r.ToRow += rowOffset;
                if ((r.FixedFlag & FixedFlag.FromColFixed) == FixedFlag.None) r.FromCol += colOffset;
                if ((r.FixedFlag & FixedFlag.ToColFixed) == FixedFlag.None) r.ToCol += colOffset;
            }
        }
        internal override bool IsFixed 
        {
            get
            {
                foreach(var r in Ranges)
                {
                    if(r.FixedFlag != FixedFlag.All)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        internal List<FormulaRangeAddress> Ranges;
        internal FormulaRange(short startPos, short endPos, int fromRow, int fromCol, int toRow, int toCol, FixedFlag fixedFlag)
        {
            Type = FormulaType.FormulaRange;
            TokenStartPosition = startPos;
            TokenEndPosition = endPos;
            Ranges = new List<FormulaRangeAddress>();
            Ranges.Add(
                new FormulaRangeAddress(_ctx)
                {
                    FromRow = fromRow,
                    FromCol = fromCol,
                    ToRow = toRow,
                    ToCol = toCol,
                    FixedFlag = fixedFlag
                });
        }
        internal FormulaRange(short startPos, short endPos, ExcelRangeBase range)
        {
            Type = FormulaType.FormulaRange;
            TokenStartPosition = startPos;
            TokenEndPosition = endPos;
            Ranges = new List<FormulaRangeAddress>();
            if (range.Addresses == null)
            {
                Ranges.Add(
                    new FormulaRangeAddress(_ctx)
                    {
                        ExternalReferenceIx = (short)(string.IsNullOrEmpty(range._wb) ? 0 : range._workbook.ExternalLinks.GetExternalLink(range._wb)),
                        WorksheetIx = (short)range.Worksheet.PositionId,
                        FromRow = range._fromRow,
                        FromCol = range._fromCol,
                        ToRow = range._toRow,
                        ToCol = range._toCol,

                        FixedFlag = (range._fromRowFixed ? FixedFlag.FromRowFixed : 0) |
                                    (range._fromColFixed ? FixedFlag.FromColFixed : 0) |
                                    (range._toRowFixed ? FixedFlag.ToRowFixed : 0) |
                                    (range._toColFixed ? FixedFlag.ToColFixed : 0)
                    }); 
            }
            else
            {
                foreach (var a in range.Addresses)
                {
                    Ranges.Add(
                        new FormulaRangeAddress(_ctx)
                        {
                            ExternalReferenceIx = (short)(string.IsNullOrEmpty(a._wb) ? -1 : range._workbook.ExternalLinks.GetExternalLink(a._wb)),
                            WorksheetIx = (short)(string.IsNullOrEmpty(a.WorkSheetName) ? range.Worksheet.PositionId : (range._workbook.Worksheets[a.WorkSheetName]==null ? -1 : range._workbook.Worksheets[a.WorkSheetName].PositionId)),
                            FromRow = a._fromRow,
                            FromCol = a._fromCol,
                            ToRow = a._toRow,
                            ToCol = a._toCol,
                            FixedFlag = (a._fromRowFixed ? FixedFlag.FromRowFixed : 0) |
                                        (a._fromColFixed ? FixedFlag.FromColFixed : 0) |
                                        (a._toRowFixed ? FixedFlag.ToRowFixed : 0) |
                                        (a._toColFixed ? FixedFlag.ToColFixed : 0) 

                        });
                }
            }
        }
        internal override string GetValue()
        {
            var sb=new StringBuilder();
            foreach(var r in Ranges)
            {
                sb.Append(ExcelCellBase.GetAddress(r.FromRow, r.FromCol, r.ToRow, r.ToCol,
                    (r.FixedFlag & FixedFlag.FromRowFixed) > 0,
                    (r.FixedFlag & FixedFlag.FromColFixed) > 0,
                    (r.FixedFlag & FixedFlag.ToRowFixed) > 0,
                    (r.FixedFlag & FixedFlag.ToColFixed) > 0));
                sb.Append(':');
            }
            return sb.ToString(0, sb.Length - 1);
        }
    }
    public class FormulaRangeAddress
    {
        public ParsingContext _context;
        internal FormulaRangeAddress()
        {

        }
        internal FormulaRangeAddress(ParsingContext ctx)
        {            
            _context = ctx;
        }
        public short ExternalReferenceIx;
        /// <summary>
        /// Worksheet index in the package.
        /// -1             - Non-existing worksheet
        /// short.MinValue - Not set. 
        /// </summary>
        public short WorksheetIx = short.MinValue;
        public int FromRow, FromCol, ToRow, ToCol;
        internal FixedFlag FixedFlag;

        public static FormulaRangeAddress Empty
        {
            get { return new FormulaRangeAddress(); }
        }

        internal eAddressCollition CollidesWith(FormulaRangeAddress other)
        {
            var util = new ExcelAddressCollideUtility(this, _context);
            return util.Collide(other, _context);
        }

        /// <summary>
        /// ToString() returns the full address as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var ws = WorksheetName;
            if(!string.IsNullOrEmpty(ws))
            {
                return new ExcelAddress(ws, FromRow, FromCol, ToRow, ToCol).FullAddress;
            }
            return new ExcelAddress(FromRow, FromCol, ToRow, ToCol).FullAddress;
        }

        /// <summary>
        /// Address of the range on the worksheet (i.e. worksheet name is excluded).
        /// </summary>
        public string WorksheetAddress
        {
            get
            {
                return new ExcelAddress(FromRow, FromCol, ToRow, ToCol).Address;
            }
        }

        /// <summary>
        /// Worksheet name of the address
        /// </summary>
        public string WorksheetName
        {
            get
            {
                if(WorksheetIx > -1 && _context != null && _context.Package != null)
                {
                    if(_context.Package.Workbook.Worksheets[WorksheetIx] != null)
                    {
                        return _context.Package.Workbook.Worksheets[WorksheetIx].Name;
                    }
                }
                return string.Empty;
            }
        }

        internal FormulaRangeAddress Intersect(FormulaRangeAddress address)
        {
            if (address.FromRow > ToRow || ToRow < address.FromRow ||
               address.FromCol > ToCol || ToCol < address.FromCol ||
               address.WorksheetIx != WorksheetIx)
            {
                return null;
            }

            var fromRow = Math.Max(address.FromRow, FromRow);
            var toRow = Math.Min(address.ToRow, ToRow);
            var fromCol = Math.Max(address.FromCol, FromCol);
            var toCol = Math.Min(address.ToCol, ToCol);

            return new FormulaRangeAddress(_context)
            {
                WorksheetIx = WorksheetIx,
                FromRow = fromRow,
                FromCol = fromCol,
                ToRow = toRow,
                ToCol = toCol
            };
        }

        /// <summary>
        /// Returns this address as a <see cref="ExcelAddressBase"/>
        /// </summary>
        /// <returns></returns>
        internal ExcelAddressBase ToExcelAddressBase()
        {
            if(ExternalReferenceIx > 0)
            {
                return new ExcelAddressBase(ExternalReferenceIx, WorksheetName, FromRow, FromCol, ToRow, ToCol);
            }
            return new ExcelAddressBase(WorksheetName, FromRow, FromCol, ToRow, ToCol);
        }

        internal ExcelCellAddress _start = null;
        /// <summary>
        /// Gets the row and column of the top left cell.
        /// </summary>
        /// <value>The start row column.</value>
        public ExcelCellAddress Start
        {
            get
            {
                if (_start == null)
                {
                    _start = new ExcelCellAddress(FromRow, FromCol);
                }
                return _start;
            }
        }
        internal ExcelCellAddress _end = null;
        /// <summary>
        /// Gets the row and column of the bottom right cell.
        /// </summary>
        /// <value>The end row column.</value>
        public ExcelCellAddress End
        {
            get
            {
                if (_end == null)
                {
                    _end = new ExcelCellAddress(ToRow, ToCol);
                }
                return _end;
            }
        }
    }
    public class FormulaTableAddress : FormulaRangeAddress
    {
        public string TableName = "", ColumnName1 = "", ColumnName2 = "", TablePart1 = "", TablePart2="";
        internal void SetTableAddress(ExcelPackage package)
        {
            ExcelTable table;
            if (WorksheetIx >= 0)
            {
                if(WorksheetIx< package.Workbook.Worksheets.Count)
                {
                    table = package.Workbook.Worksheets[WorksheetIx].Tables[TableName];
                }
                else
                {
                    table = null;
                }
            }
            else if(WorksheetIx == short.MinValue)
            {
                table = package.Workbook.GetTable(TableName);
            }
            else
            {
                table = null;
            }

            if (table != null && ExternalReferenceIx <= 0)
            {
                FixedFlag = FixedFlag.All;
                SetRowFromTablePart(TablePart1, table, ref FromRow, ref ToRow, ref FixedFlag);
                if(string.IsNullOrEmpty(TablePart2)==false) SetRowFromTablePart(TablePart2, table, ref FromRow, ref ToRow, ref FixedFlag);
                
                SetColFromTablePart(ColumnName1, table, ref FromCol, ref ToCol, false);
                if (string.IsNullOrEmpty(ColumnName2) == false) SetColFromTablePart(ColumnName2, table, ref FromCol, ref ToCol, true);
            }
        }
        private void SetColFromTablePart(string value, ExcelTable table, ref int fromCol, ref int toCol, bool lastColon)
        {            
            var col = table.Columns[value];
            if (col == null)
            {
                if(value.StartsWith("'#"))
                {
                    col = table.Columns[value.Substring(1)];
                }
                if (col == null)
                    return;
            }
            if (lastColon)
            {
                toCol = table.Range._fromCol + col.Position;
            }
            else
            {
                fromCol = toCol = table.Range._fromCol + col.Position;
            }
        }
        private void SetRowFromTablePart(string value, ExcelTable table, ref int fromRow, ref int toRow, ref FixedFlag fixedFlag)
        {
            switch (value.ToLower())
            {
                case "#all":
                    fromRow = table.Address._fromRow;
                    toRow = table.Address._toRow;
                    break;
                case "#headers":
                    if (table.ShowHeader)
                    {
                        fromRow = table.Address._fromRow;
                        if (toRow == 0)
                        {
                            toRow = table.Address._fromRow;
                        }
                    }
                    else if (fromRow == 0)
                    {
                        fromRow = toRow = -1;
                    }
                    break;
                case "#data":
                    if (fromRow == 0 || table.DataRange._fromRow < fromRow)
                    {
                        fromRow = table.DataRange._fromRow;
                    }
                    if (table.DataRange._toRow > toRow)
                    {
                        toRow = table.DataRange._toRow;
                    }
                    break;
                case "#totals":
                    if (table.ShowTotal)
                    {
                        if (fromRow == 0)
                            fromRow = table.Range._toRow;
                        toRow = table.Range._toRow;
                    }
                    else if (fromRow == 0)
                    {
                        fromRow = toRow = -1;
                    }
                    break;
                case "#this row":
                    var dr = table.DataRange;
                    if (WorksheetIx != table.WorkSheet.PositionId || FromRow < dr._fromRow || FromRow > dr._toRow)
                    {
                        fromRow = toRow = -1;
                    }
                    else
                    {
                        fromRow = FromRow;
                        toRow = FromRow;
                        fixedFlag = FixedFlag.FromColFixed | FixedFlag.ToColFixed;
                    }
                    break;
                default:
                    FromCol = table.Address._fromCol;
                    ToCol = table.Address._toCol;
                    fromRow = table.ShowHeader ? table.Address._fromRow + 1 : table.Address._fromRow;
                    toRow = table.ShowTotal ? table.Address._toRow - 1 : table.Address._toRow;
                    break;
            }
        }
    }
}
