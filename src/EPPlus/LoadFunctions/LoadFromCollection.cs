﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  07/16/2020         EPPlus Software AB       EPPlus 5.2.1
 *************************************************************************************************/
using OfficeOpenXml.Compatibility;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using OfficeOpenXml.LoadFunctions.Params;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using OfficeOpenXml.Attributes;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.LoadFunctions
{
    internal class LoadFromCollection<T> : LoadFunctionBase
    {
        public LoadFromCollection(ExcelRangeBase range, IEnumerable<T> items, LoadFromCollectionParams parameters) : base(range, parameters)
        {
            _items = items;
            _bindingFlags = parameters.BindingFlags;
            _headerParsingType = parameters.HeaderParsingType;
            var type = typeof(T);
            if (parameters.Members == null)
            {
                var members = type.GetProperties(_bindingFlags);
                var sortedMemberInfos = new List<ColumnInfo>();
                if(type.HasMemberWithPropertyOfType<EpplusTableColumnAttribute>())
                {
                    var index = 0;
                    foreach(var member in members)
                    {
                        if(member.HasPropertyOfType<EpplusIgnore>())
                        {
                            continue;
                        }
                        var sortOrder = -1;
                        var numberFormat = string.Empty;
                        var epplusColumnAttr = member.GetFirstAttributeOfType<EpplusTableColumnAttribute>();
                        if(epplusColumnAttr != null)
                        {
                            sortOrder = epplusColumnAttr.ColumnOrder;
                            numberFormat = epplusColumnAttr.ColumnNumberFormat;
                        }
                        sortedMemberInfos.Add(new ColumnInfo { SortOrder = sortOrder, MemberInfo = member, NumberFormat =  numberFormat});
                    }
                    sortedMemberInfos.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
                    sortedMemberInfos.ForEach(x => x.Index = index++);
                    _members = sortedMemberInfos.ToArray();
                }
                else
                {
                    var index = 0;
                    _members = members.Select(x => new ColumnInfo { Index = index++, MemberInfo = x }).ToArray();
                }
                var formulaColumns = type.FindAttributesOfType<EpplusFormulaTableColumnAttribute>();
                if(formulaColumns != null && formulaColumns.Any())
                {
                    var formulaColumnList = new List<ColumnInfo>(_members);
                    foreach(var column in formulaColumns)
                    {
                        formulaColumnList.Add(new ColumnInfo { SortOrder = column.ColumnOrder, Header = column.ColumnHeader, Formula = column.Formula, FormulaR1C1 = column.FormulaR1C1, NumberFormat = column.ColumnNumberFormat });
                    }
                    formulaColumnList.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
                    var index = 0;
                    formulaColumnList.ForEach(x => x.Index = index++);
                    _members = formulaColumnList.ToArray();
                   
                }
            }
            else
            {
                _members = parameters.Members.Select(x => new ColumnInfo { MemberInfo = x }).ToArray();
                if (_members.Length == 0)   //Fixes issue 15555
                {
                    throw (new ArgumentException("Parameter Members must have at least one property. Length is zero"));
                }
                foreach (var columnInfo in _members)
                {
                    if (columnInfo.MemberInfo == null) continue;
                    var member = columnInfo.MemberInfo;
                    if (member.DeclaringType != null && member.DeclaringType != type)
                    {
                        _isSameType = false;
                    }

                    //Fixing inverted check for IsSubclassOf / Pullrequest from tomdam
                    if (member.DeclaringType != null && member.DeclaringType != type && !TypeCompat.IsSubclassOf(type, member.DeclaringType) && !TypeCompat.IsSubclassOf(member.DeclaringType, type))
                    {
                        throw new InvalidCastException("Supplied properties in parameter Properties must be of the same type as T (or an assignable type from T)");
                    }
                }
            }
        }

        private readonly BindingFlags _bindingFlags;
        private readonly ColumnInfo[] _members;
        private readonly HeaderParsingTypes _headerParsingType;
        private readonly IEnumerable<T> _items;
        private readonly bool _isSameType = true;

        protected override int GetNumberOfColumns()
        {
            return _members.Length == 0 ? 1 : _members.Length;
        }

        protected override int GetNumberOfRows()
        {
            if (_items == null) return 0;
            return _items.Count();
        }


        protected override void LoadInternal(object[,] values, out Dictionary<int, FormulaCell> formulaCells, out Dictionary<int, string> columnFormats)
        {

            int col = 0, row = 0;
            columnFormats = new Dictionary<int, string>();
            formulaCells = new Dictionary<int, FormulaCell>();
            if (_members.Length > 0 && PrintHeaders)
            {
                foreach (var colInfo in _members)
                {
                    var header = string.Empty;
                    if(colInfo.MemberInfo != null)
                    {
                        var member = colInfo.MemberInfo;
                        var epplusColumnAttribute = member.GetFirstAttributeOfType<EpplusTableColumnAttribute>();
                        if (epplusColumnAttribute != null)
                        {
                            if (!string.IsNullOrEmpty(epplusColumnAttribute.ColumnHeader))
                            {
                                header = epplusColumnAttribute.ColumnHeader;
                            }
                            else
                            {
                                header = ParseHeader(member.Name);
                            }
                            if (!string.IsNullOrEmpty(epplusColumnAttribute.ColumnNumberFormat))
                            {
                                columnFormats.Add(col, epplusColumnAttribute.ColumnNumberFormat);
                            }
                        }
                        else
                        {
                            var descriptionAttribute = member.GetFirstAttributeOfType<DescriptionAttribute>();
                            if (descriptionAttribute != null)
                            {
                                header = descriptionAttribute.Description;
                            }
                            else
                            {
                                var displayNameAttribute = member.GetFirstAttributeOfType<DisplayNameAttribute>();
                                if (displayNameAttribute != null)
                                {
                                    header = displayNameAttribute.DisplayName;
                                }
                                else
                                {
                                    header = ParseHeader(member.Name);
                                }
                            }
                        }
                    }
                    else
                    {
                        // column is a FormulaColumn
                        header = colInfo.Header;
                        columnFormats.Add(colInfo.Index, colInfo.NumberFormat);
                    }
                    
                    values[row, col++] = header;
                }
                row++;
            }

            if (!_items.Any() && (_members.Length == 0 || PrintHeaders == false))
            {
                return;
            }

            var nMembers = GetNumberOfColumns();
            foreach (var item in _items)
            {
                if (item == null)
                {
                    col = GetNumberOfColumns();
                }
                else
                {
                    col = 0;
                    if (item is string || item is decimal || item is DateTime || TypeCompat.IsPrimitive(item))
                    {
                        values[row, col++] = item;
                    }
                    else
                    {
                        foreach (var colInfo in _members)
                        {
                            if(colInfo.MemberInfo != null)
                            {
                                var member = colInfo.MemberInfo;
                                if (_isSameType == false && item.GetType().GetMember(member.Name, _bindingFlags).Length == 0)
                                {
                                    col++;
                                    continue; //Check if the property exists if and inherited class is used
                                }
                                else if (member is PropertyInfo)
                                {
                                    values[row, col++] = ((PropertyInfo)member).GetValue(item, null);
                                }
                                else if (member is FieldInfo)
                                {
                                    values[row, col++] = ((FieldInfo)member).GetValue(item);
                                }
                                else if (member is MethodInfo)
                                {
                                    values[row, col++] = ((MethodInfo)member).Invoke(item, null);
                                }
                            }
                            else if(!string.IsNullOrEmpty(colInfo.Formula))
                            {
                                formulaCells[colInfo.Index] = new FormulaCell { Formula = colInfo.Formula };
                            }
                            else if(!string.IsNullOrEmpty(colInfo.Formula))
                            {
                                formulaCells[colInfo.Index] = new FormulaCell { FormulaR1C1 = colInfo.FormulaR1C1 };
                            }
                        }
                    }
                }
                row++;
            }
        }

        private string ParseHeader(string header)
        {
            switch(_headerParsingType)
            {
                case HeaderParsingTypes.Preserve:
                    return header;
                case HeaderParsingTypes.UnderscoreToSpace:
                    return header.Replace("_", " ");
                case HeaderParsingTypes.CamelCaseToSpace:
                    return Regex.Replace(header, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
                case HeaderParsingTypes.UnderscoreAndCamelCaseToSpace:
                    header = Regex.Replace(header, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
                    return header.Replace("_ ", "_").Replace("_", " ");
                default:
                    return header;
            }
        }
    }
}

