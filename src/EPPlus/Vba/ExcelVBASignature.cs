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
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using OfficeOpenXml.Utils;
using System.IO;
using OfficeOpenXml.Utils.CompundDocument;
using System.Security.Cryptography.Pkcs;
using OfficeOpenXml.Packaging;
using OfficeOpenXml.Constants;
using OfficeOpenXml.Vba.ContentHash;
using System.Security.Cryptography;
using OfficeOpenXml.VBA.ContentHash;
using System.Text;
using OfficeOpenXml.VBA.Signatures;

namespace OfficeOpenXml.VBA
{
    /// <summary>
    /// The code signature properties of the project
    /// </summary>
    public class ExcelVbaSignature
    {
        internal ExcelVbaSignature(Packaging.ZipPackagePart vbaPart)
        {
            _vbaPart = vbaPart;
            _legacySignature = new EPPlusVbaSignatureLegacy(vbaPart);
            _agileSignature = new EPPlusVbaSignatureAgile(vbaPart);
            ReadSignatures();
        }

        private readonly ZipPackagePart _vbaPart = null;
        private readonly EPPlusVbaSignature _legacySignature;
        private readonly EPPlusVbaSignature _agileSignature;

        /// <summary>
        /// The certificate to sign the VBA project.
        /// <remarks>
        /// This certificate must have a private key.
        /// There is no validation that the certificate is valid for codesigning, so make sure it's valid to sign Excel files (Excel 2010 is more strict that prior versions).
        /// </remarks>
        /// </summary>
        public X509Certificate2 Certificate { get; set; }
        /// <summary>
        /// The verifier (legacy format)
        /// </summary>
        public SignedCms Verifier { get; internal set; }
        /// <summary>
        /// The verifier (agile format)
        /// </summary>
        public SignedCms VerifierAgile { get; internal set; }
        /// <summary>
        /// The verifier (V3 format)
        /// </summary>
        public SignedCms VerifierV3 { get; internal set; }
        internal CompoundDocument Signature { get; set; }
        internal Packaging.ZipPackagePart Part { get; set; }
        internal Packaging.ZipPackagePart PartAgile { get; set; }
        internal Packaging.ZipPackagePart PartV3 { get; set; }
        private void ReadSignatures()
        {
            if (_vbaPart == null) return;

            // Legacy signature
            _legacySignature.ReadSignature();
            Part = _legacySignature.Part;
            Certificate = _legacySignature.Certificate;
            Verifier = _legacySignature.Verifier;

            // Agile signature
            _agileSignature.ReadSignature();
            PartAgile = _agileSignature.Part;
            if (Certificate == null)
            {
                Certificate = _agileSignature.Certificate;
            }
            VerifierAgile = _agileSignature.Verifier;
        }

        internal void Save(ExcelVbaProject proj)
        {
            _legacySignature.CreateSignature(proj);
            _agileSignature.CreateSignature(proj);
        }

        private void CreateProjectSignatures(ExcelVbaProject proj)
        {
            _legacySignature.CreateSignature(proj);
            _agileSignature.CreateSignature(proj);
        }
    }
}
