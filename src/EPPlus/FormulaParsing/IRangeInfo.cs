﻿using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using OfficeOpenXml.FormulaParsing.Ranges;
using System;
using System.Collections.Generic;
/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  05/16/2020         EPPlus Software AB           EPPlus 6
 *************************************************************************************************/
using System.Linq;
using System.Text;

namespace OfficeOpenXml.FormulaParsing
{
    public interface IRangeInfo : IAddressInfo, IEnumerator<ICellInfo>, IEnumerable<ICellInfo>
    {
        /// <summary>
        /// If the range is empty
        /// </summary>
        bool IsEmpty { get; }
        /// <summary>
        /// If the contains more than one cell  with a value.
        /// </summary>
        bool IsMulti { get; }

        /// <summary>
        /// Returns true if the range is not referring to the cell store, but rather keeps the data in memory.
        /// </summary>
        bool IsInMemoryRange { get; }
        /// <summary>
        /// Get number of cells
        /// </summary>
        /// <returns>Number of cells</returns>
        int GetNCells();

        /// <summary>
        /// Size of the range, i.e. number of Cols and number of Rows
        /// </summary>
        RangeDefinition Size { get; }
        /// <summary>
        /// Get the value from a cell
        /// </summary>
        /// <param name="row">The Row</param>
        /// <param name="col">The Column</param>
        /// <returns></returns>
        object GetValue(int row, int col);
        /// <summary>
        /// Gets
        /// </summary>
        /// <param name="rowOffset"></param>
        /// <param name="colOffset"></param>
        /// <returns></returns>
        object GetOffset(int rowOffset, int colOffset);
        /// <summary>
        /// The worksheet 
        /// </summary>
        ExcelWorksheet Worksheet { get; }
    }
    public interface IAddressInfo
    {
        /// <summary>
        /// The address.
        /// </summary>
        //ExcelAddressBase Address { get; }
        FormulaRangeAddress Address { get; }
    }
}
