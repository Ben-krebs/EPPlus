/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB       Initial release EPPlus 5
 *************************************************************************************************/
using OfficeOpenXml.DataValidation.Contracts;
using OfficeOpenXml.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace OfficeOpenXml.DataValidation
{
    /// <summary>
    /// <para>
    /// Collection of <see cref="ExcelDataValidation"/>. This class is providing the API for EPPlus data validation.
    /// </para>
    /// <para>
    /// The public methods of this class (Add[...]Validation) will create a datavalidation entry in the worksheet. When this
    /// validation has been created changes to the properties will affect the workbook immediately.
    /// </para>
    /// <para>
    /// Each type of validation has either a formula or a typed value/values, except for custom validation which has a formula only.
    /// </para>
    /// <code>
    /// // Add a date time validation
    /// var validation = worksheet.DataValidation.AddDateTimeValidation("A1");
    /// // set validation properties
    /// validation.ShowErrorMessage = true;
    /// validation.ErrorTitle = "An invalid date was entered";
    /// validation.Error = "The date must be between 2011-01-31 and 2011-12-31";
    /// validation.Prompt = "Enter date here";
    /// validation.Formula.Value = DateTime.Parse("2011-01-01");
    /// validation.Formula2.Value = DateTime.Parse("2011-12-31");
    /// validation.Operator = ExcelDataValidationOperator.between;
    /// </code>
    /// </summary>
    public class ExcelDataValidationCollection : IEnumerable<ExcelDataValidation>
    {
        private List<ExcelDataValidation> _validations = new List<ExcelDataValidation>();
        private ExcelWorksheet _worksheet = null;

        private const string DataValidationPath = "//d:dataValidations";
        private readonly string DataValidationItemsPath = string.Format("{0}/d:dataValidation", DataValidationPath);

        internal ExcelDataValidationCollection(ExcelWorksheet worksheet)
        {
            _worksheet = worksheet;
        }

        internal ExcelDataValidationCollection(XmlReader xr, ExcelWorksheet worksheet)
            : this(worksheet)
        {
            ReadDataValidations(xr);
        }

        public void ReadDataValidations(XmlReader xr)
        {
            while (xr.Read())
            {
                if (xr.LocalName != "dataValidation")
                {
                    xr.Read(); //Read beyond the end element
                    break;
                }

                if (xr.NodeType == XmlNodeType.Element)
                {
                    var validation = ExcelDataValidationFactory.Create(xr);
                    _validations.Add(validation);
                }
            }
        }

        internal bool HasValidationType(InternalValidationType type)
        {
            if (Count != 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (_validations[i].InternalValidationType == type)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        int GetCount(InternalValidationType type)
        {
            int validationCount = 0;
            for (int i = 0; i < Count; i++)
            {
                if (_validations[i].InternalValidationType == type)
                {
                    validationCount++;
                }
            }
            return validationCount;
        }

        internal int GetNonExtLstCount()
        {
            return GetCount(InternalValidationType.DataValidation);
        }


        internal int GetExtLstCount()
        {
            return GetCount(InternalValidationType.ExtLst);
        }

        private void OnValidationCountChanged()
        {

        }

        /// <summary>
        /// Validates address - not empty, collisions
        /// </summary>
        /// <param name="address"></param>
        /// <param name="validatingValidation"></param>
        private void ValidateAddress(string address, IExcelDataValidation validatingValidation)
        {
            Require.Argument(address).IsNotNullOrEmpty("address");

            if (!InternalValidationEnabled) return;

            // ensure that the new address does not collide with an existing validation.
            var newAddress = new ExcelAddress(address);
            if (_validations.Count > 0)
            {
                foreach (var validation in _validations)
                {
                    if (validatingValidation != null && validatingValidation == validation)
                    {
                        continue;
                    }
                    var result = validation.Address.Collide(newAddress);
                    if (result != ExcelAddressBase.eAddressCollition.No)
                    {
                        throw new InvalidOperationException(string.Format("The address ({0}) collides with an existing validation ({1})", address, validation.Address.Address));
                    }
                }
            }
        }

        private void ValidateAddress(string address)
        {
            ValidateAddress(address, null);
        }

        /// <summary>
        /// Validates all data validations.
        /// </summary>
        internal void ValidateAll()
        {
            if (!InternalValidationEnabled) return;

            foreach (var validation in _validations)
            {
                validation.Validate();
            }
            //if (_extLstValidations != null)
            //{
            //    foreach (var extValidation in _extLstValidations)
            //    {
            //        extValidation.Validate();
            //    }
            //}
        }

        internal void AddCopyOfDataValidation(string address, ExcelDataValidation dv)
        {
            var validation = ExcelDataValidationFactory.Create(dv, address, ExcelDataValidation.NewId(), _worksheet.Name);

            _validations.Add(validation);
        }



        public IExcelDataValidationAny AddAnyValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationAny(ExcelDataValidation.NewId(), address);
            _validations.Add(item);
            return item;
        }

        public IExcelDataValidationInt AddIntegerValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationInt(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }

        public IExcelDataValidationInt AddTextLengthValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationInt(ExcelDataValidation.NewId(), address, _worksheet.Name, true);
            _validations.Add(item);
            return item;
        }

        public IExcelDataValidationDecimal AddDecimalValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationDecimal(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }

        public IExcelDataValidationList AddListValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationList(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }

        public IExcelDataValidationTime AddTimeValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationTime(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }
        public IExcelDataValidationDateTime AddDateTimeValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationDateTime(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }
        public IExcelDataValidationCustom AddCustomValidation(string address)
        {
            ValidateAddress(address);
            var item = new ExcelDataValidationCustom(ExcelDataValidation.NewId(), address, _worksheet.Name);
            _validations.Add(item);
            return item;
        }


        /// <summary>
        /// Number of validations
        /// </summary>
        public int Count
        {
            get { return GetValidations().Count; }
        }

        /// <summary>
        /// Epplus validates that all data validations are consistend and valid
        /// when they are added and when a workbook is saved. Since this takes some
        /// resources, it can be disabled for improve performance. 
        /// </summary>
        public bool InternalValidationEnabled
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Index operator, returns by 0-based index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ExcelDataValidation this[int index]
        {
            get { return GetValidations()[index]; }
            set { GetValidations()[index] = value; }
        }

        /// <summary>
        /// Index operator, returns a data validation which address partly or exactly matches the searched address.
        /// </summary>
        /// <param name="address">A cell address or range</param>
        /// <returns>A <see cref="ExcelDataValidation"/> or null if no match</returns>
        public IExcelDataValidation this[string address]
        {
            get
            {
                var searchedAddress = new ExcelAddress(address);
                return GetValidations().Find(x => x.Address.Collide(searchedAddress) != ExcelAddressBase.eAddressCollition.No);
            }
        }

        /// <summary>
        /// Returns all validations that matches the supplied predicate <paramref name="match"/>.
        /// </summary>
        /// <param name="match">predicate to filter out matching validations</param>
        /// <returns></returns>
        public IEnumerable<ExcelDataValidation> FindAll(Predicate<ExcelDataValidation> match)
        {
            return GetValidations().FindAll(match);
        }

        /// <summary>
        /// Removes an <see cref="ExcelDataValidation"/> from the collection.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if remove succeeds, otherwise false</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="item"/> is null</exception>
        public bool Remove(ExcelDataValidation item)
        {
            Require.Argument(item).IsNotNull("item");
            if (!(item is ExcelDataValidation))
            {
                throw new InvalidCastException("The supplied item must inherit OfficeOpenXml.DataValidation.ExcelDataValidation");
            }

            var retVal = _validations.Remove(item);
            if (retVal) OnValidationCountChanged();
            return retVal;
        }

        /// <summary>
        /// Returns the first matching validation.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public ExcelDataValidation Find(Predicate<ExcelDataValidation> match)
        {
            return GetValidations().Find(match);
        }

        /// <summary>
        /// Removes all validations from the collection.
        /// </summary>
        public void Clear()
        {
        }

        /// <summary>
        /// Removes the validations that matches the predicate
        /// </summary>
        /// <param name="match"></param>
        public void RemoveAll(Predicate<IExcelDataValidation> match)
        {
        }


        IEnumerator<ExcelDataValidation> IEnumerable<ExcelDataValidation>.GetEnumerator()
        {
            return GetValidations().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetValidations().GetEnumerator();
        }

        private List<ExcelDataValidation> GetValidations()
        {
            return _validations;
        }
    }
}
