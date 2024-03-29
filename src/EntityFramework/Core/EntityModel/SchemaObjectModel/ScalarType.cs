// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityModel.SchemaObjectModel
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     This is an adapter to make PrimitiveTypeKindData fit in the Schema Object Model tree
    /// </summary>
    internal sealed class ScalarType : SchemaType
    {
        internal const string DateTimeFormat = @"yyyy-MM-dd HH\:mm\:ss.fffZ";
        internal const string TimeFormat = @"HH\:mm\:ss.fffffffZ";
        internal const string DateTimeOffsetFormat = @"yyyy-MM-dd HH\:mm\:ss.fffffffz";
        private static readonly Regex _binaryValueValidator = new Regex("^0[xX][0-9a-fA-F]+$", RegexOptions.Compiled);

        private static readonly Regex _guidValueValidator = new Regex(
            "[0-9a-fA-F]{8,8}(-[0-9a-fA-F]{4,4}){3,3}-[0-9a-fA-F]{12,12}", RegexOptions.Compiled);

        private readonly PrimitiveType _primitiveType;

        /// <summary>
        ///     Construct an internal (not from schema) CDM scalar type
        /// </summary>
        /// <param name="parentElement"> the owning schema </param>
        /// <param name="typeName"> the naem of the type </param>
        /// <param name="primitiveType"> the PrimitiveTypeKind of the type </param>
        internal ScalarType(Schema parentElement, string typeName, PrimitiveType primitiveType)
            : base(parentElement)
        {
            Name = typeName;
            _primitiveType = primitiveType;
        }

        /// <summary>
        ///     try to parse a string
        /// </summary>
        /// <param name="text"> the string to parse </param>
        /// <param name="value"> the value of the string </param>
        /// <returns> true if the value is a valid value, false otherwise </returns>
        public bool TryParse(string text, out object value)
        {
            switch (_primitiveType.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    return TryParseBinary(text, out value);
                case PrimitiveTypeKind.Boolean:
                    return TryParseBoolean(text, out value);
                case PrimitiveTypeKind.Byte:
                    return TryParseByte(text, out value);
                case PrimitiveTypeKind.DateTime:
                    return TryParseDateTime(text, out value);
                case PrimitiveTypeKind.Time:
                    return TryParseTime(text, out value);
                case PrimitiveTypeKind.DateTimeOffset:
                    return TryParseDateTimeOffset(text, out value);
                case PrimitiveTypeKind.Decimal:
                    return TryParseDecimal(text, out value);
                case PrimitiveTypeKind.Double:
                    return TryParseDouble(text, out value);
                case PrimitiveTypeKind.Guid:
                    return TryParseGuid(text, out value);
                case PrimitiveTypeKind.Int16:
                    return TryParseInt16(text, out value);
                case PrimitiveTypeKind.Int32:
                    return TryParseInt32(text, out value);
                case PrimitiveTypeKind.Int64:
                    return TryParseInt64(text, out value);
                case PrimitiveTypeKind.Single:
                    return TryParseSingle(text, out value);
                case PrimitiveTypeKind.String:
                    return TryParseString(text, out value);
                case PrimitiveTypeKind.SByte:
                    return TryParseSByte(text, out value);
                default:
                    throw new NotSupportedException(_primitiveType.FullName);
            }
        }

        /// <summary>
        ///     The type kind of this type.
        /// </summary>
        public PrimitiveTypeKind TypeKind
        {
            get { return _primitiveType.PrimitiveTypeKind; }
        }

        /// <summary>
        ///     Returns the PrimitiveType of the scalar type.
        /// </summary>
        public PrimitiveType Type
        {
            get { return _primitiveType; }
        }

        private static bool TryParseBoolean(string text, out object value)
        {
            Boolean temp;
            if (!Boolean.TryParse(text, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseByte(string text, out object value)
        {
            Byte temp;
            if (!Byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseSByte(string text, out object value)
        {
            SByte temp;
            if (!SByte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseInt16(string text, out object value)
        {
            Int16 temp;
            if (!Int16.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseInt32(string text, out object value)
        {
            Int32 temp;
            if (!Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseInt64(string text, out object value)
        {
            Int64 temp;
            if (!Int64.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseDouble(string text, out object value)
        {
            Double temp;
            if (!Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseDecimal(string text, out object value)
        {
            Decimal temp;
            if (!Decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseDateTime(string text, out object value)
        {
            DateTime temp;
            if (
                !DateTime.TryParseExact(
                    text, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out temp))
            {
                value = null;
                return false;
            }

            value = temp;
            return true;
        }

        /// <summary>
        ///     Parses the default value for Edm Type Time based on the DateTime format "HH:mm:ss.fffffffz".
        ///     The value is first converted to DateTime value and then converted to TimeSpan.
        /// </summary>
        /// <param name="text"> </param>
        /// <param name="value"> </param>
        /// <returns> </returns>
        private static bool TryParseTime(string text, out object value)
        {
            DateTime temp;
            if (
                !DateTime.TryParseExact(
                    text, TimeFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal | DateTimeStyles.NoCurrentDateDefault, out temp))
            {
                value = null;
                return false;
            }
            value = new TimeSpan(temp.Ticks);
            return true;
        }

        private static bool TryParseDateTimeOffset(string text, out object value)
        {
            DateTimeOffset temp;
            if (!DateTimeOffset.TryParse(text, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }

        private static bool TryParseGuid(string text, out object value)
        {
            if (!_guidValueValidator.IsMatch(text))
            {
                value = null;
                return false;
            }
            value = new Guid(text);
            return true;
        }

        private static bool TryParseString(string text, out object value)
        {
            value = text;
            return true;
        }

        private static bool TryParseBinary(string text, out object value)
        {
            //value must look like 0xddddd...
            if (!_binaryValueValidator.IsMatch(text))
            {
                value = null;
                return false;
            }

            // strip off the 0x
            var binaryPart = text.Substring(2);

            value = ConvertToByteArray(binaryPart);

            return true;
        }

        internal static byte[] ConvertToByteArray(string text)
        {
            var inc = 2;
            var numBytes = (text.Length) / 2;

            // adjust for case where we have 1F7 instead of 01F7
            if (text.Length % 2 == 1)
            {
                inc = 1;
                numBytes++;
            }

            var bytes = new byte[numBytes];
            for (int index = 0, iByte = 0; index < text.Length; index += inc, inc = 2, ++iByte)
            {
                bytes[iByte] = byte.Parse(text.Substring(index, inc), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return bytes;
        }

        private static bool TryParseSingle(string text, out object value)
        {
            Single temp;
            if (!Single.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
            {
                value = null;
                return false;
            }
            value = temp;
            return true;
        }
    }
}
