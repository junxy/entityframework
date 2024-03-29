// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Console.Resources
{
    using System.CodeDom.Compiler;
    using System.Globalization;
    using System.Resources;
    using System.Threading;
    using CmdLine;

    /// <summary>
    ///     Strongly-typed and parameterized string resources.
    /// </summary>
    [GeneratedCode("Resources.Migrate.tt", "1.0.0.0")]
    internal static class Strings
    {
        /// <summary>
        ///     A string like "Code First Migrations Command Line Utility"
        /// </summary>
        internal static string MigrateTitle
        {
            get { return EntityRes.GetString(EntityRes.MigrateTitle); }
        }

        /// <summary>
        ///     A string like "Applies any pending migrations to the database."
        /// </summary>
        internal static string MigrateDescription
        {
            get { return EntityRes.GetString(EntityRes.MigrateDescription); }
        }

        /// <summary>
        ///     A string like "ERROR: {0}"
        /// </summary>
        internal static string ErrorMessage(object p0)
        {
            return EntityRes.GetString(EntityRes.ErrorMessage, p0);
        }

        /// <summary>
        ///     A string like "WARNING: {0}"
        /// </summary>
        internal static string WarningMessage(object p0)
        {
            return EntityRes.GetString(EntityRes.WarningMessage, p0);
        }

        /// <summary>
        ///     A string like "VERBOSE: {0}"
        /// </summary>
        internal static string VerboseMessage(object p0)
        {
            return EntityRes.GetString(EntityRes.VerboseMessage, p0);
        }

        /// <summary>
        ///     A string like "Duplicate Command "{0}""
        /// </summary>
        internal static string DuplicateCommand(object p0)
        {
            return EntityRes.GetString(EntityRes.DuplicateCommand, p0);
        }

        /// <summary>
        ///     A string like "Duplicate Parameter Index [{0}] on Property "{1}""
        /// </summary>
        internal static string DuplicateParameterIndex(object p0, object p1)
        {
            return EntityRes.GetString(EntityRes.DuplicateParameterIndex, p0, p1);
        }

        /// <summary>
        ///     A string like "Out of order parameter "{0}" should have be at parameter index {1} but was found at {2}"
        /// </summary>
        internal static string ParameterOutOfOrder(object p0, object p1, object p2)
        {
            return EntityRes.GetString(EntityRes.ParameterOutOfOrder, p0, p1, p2);
        }

        /// <summary>
        ///     A string like ""{0}" is not a valid choice, valid keys are "{1}""
        /// </summary>
        internal static string InvalidKey(object p0, object p1)
        {
            return EntityRes.GetString(EntityRes.InvalidKey, p0, p1);
        }

        /// <summary>
        ///     A string like "Press any key to continue..."
        /// </summary>
        internal static string PressAnyKey
        {
            get { return EntityRes.GetString(EntityRes.PressAnyKey); }
        }

        /// <summary>
        ///     A string like "Unsupported property type {0}"
        /// </summary>
        internal static string UnsupportedPropertyType(object p0)
        {
            return EntityRes.GetString(EntityRes.UnsupportedPropertyType, p0);
        }

        /// <summary>
        ///     A string like "Invalid ParameterIndex value on property "{0}""
        /// </summary>
        internal static string InvalidPropertyParameterIndexValue(object p0)
        {
            return EntityRes.GetString(EntityRes.InvalidPropertyParameterIndexValue, p0);
        }

        /// <summary>
        ///     A string like "assembly"
        /// </summary>
        internal static string AssemblyNameArgument
        {
            get { return EntityRes.GetString(EntityRes.AssemblyNameArgument); }
        }

        /// <summary>
        ///     A string like "configurationType"
        /// </summary>
        internal static string ConfigurationTypeNameArgument
        {
            get { return EntityRes.GetString(EntityRes.ConfigurationTypeNameArgument); }
        }

        /// <summary>
        ///     A string like "Specifies the name of the assembly that contains the migrations configuration type."
        /// </summary>
        internal static string AssemblyNameDescription
        {
            get { return EntityRes.GetString(EntityRes.AssemblyNameDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the name of the migrations configuration type. If omitted, Code First Migrations will attempt to locate a single migrations configuration type in the specified assembly."
        /// </summary>
        internal static string ConfigurationTypeNameDescription
        {
            get { return EntityRes.GetString(EntityRes.ConfigurationTypeNameDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the name of a particular migration to update the database to. If omitted, the current model will be used."
        /// </summary>
        internal static string TargetMigrationDescription
        {
            get { return EntityRes.GetString(EntityRes.TargetMigrationDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the working directory of your application."
        /// </summary>
        internal static string WorkingDirectoryDescription
        {
            get { return EntityRes.GetString(EntityRes.WorkingDirectoryDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the Web.config or App.config file of your application."
        /// </summary>
        internal static string ConfigurationFileDescription
        {
            get { return EntityRes.GetString(EntityRes.ConfigurationFileDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the directory to use when resolving connection strings containing the |DataDirectory| substitution string."
        /// </summary>
        internal static string DataDirectoryDescription
        {
            get { return EntityRes.GetString(EntityRes.DataDirectoryDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the name of the connection string to use from the specified configuration file. If omitted, the context's default connection will be used."
        /// </summary>
        internal static string ConnectionStringNameDescription
        {
            get { return EntityRes.GetString(EntityRes.ConnectionStringNameDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the the connection string to use. If omitted, the context's default connection will be used."
        /// </summary>
        internal static string ConnectionStringDescription
        {
            get { return EntityRes.GetString(EntityRes.ConnectionStringDescription); }
        }

        /// <summary>
        ///     A string like "Specifies the provider invariant name of the connection string."
        /// </summary>
        internal static string ConnectionProviderNameDescription
        {
            get { return EntityRes.GetString(EntityRes.ConnectionProviderNameDescription); }
        }

        /// <summary>
        ///     A string like "Indicates that automatic migrations which might incur data loss should be allowed."
        /// </summary>
        internal static string ForceDescription
        {
            get { return EntityRes.GetString(EntityRes.ForceDescription); }
        }

        /// <summary>
        ///     A string like "Indicates that the executing SQL and additional diagnostic information should be output to the console window."
        /// </summary>
        internal static string VerboseDescription
        {
            get { return EntityRes.GetString(EntityRes.VerboseDescription); }
        }

        /// <summary>
        ///     A string like "Display this help message."
        /// </summary>
        internal static string HelpDescription
        {
            get { return EntityRes.GetString(EntityRes.HelpDescription); }
        }

        /// <summary>
        ///     A string like "Only one of '{0}' and '{1}' can be assigned to."
        /// </summary>
        internal static string AmbiguousAttributeValues(object p0, object p1)
        {
            return EntityRes.GetString(EntityRes.AmbiguousAttributeValues, p0, p1);
        }

        /// <summary>
        ///     A string like "Only one of /connectionStringName or /connectionString can be specified."
        /// </summary>
        internal static string AmbiguousConnectionString
        {
            get { return EntityRes.GetString(EntityRes.AmbiguousConnectionString); }
        }

        /// <summary>
        ///     A string like "/connectionString and /connectionProviderName must be specified together."
        /// </summary>
        internal static string MissingConnectionInfo
        {
            get { return EntityRes.GetString(EntityRes.MissingConnectionInfo); }
        }

        /// <summary>
        ///     A string like "Invalid ParameterIndex value"
        /// </summary>
        internal static string InvalidParameterIndexValue
        {
            get { return EntityRes.GetString(EntityRes.InvalidParameterIndexValue); }
        }
    }

    /// <summary>
    ///     Strongly-typed and parameterized exception factory.
    /// </summary>
    [GeneratedCode("Resources.Migrate.tt", "1.0.0.0")]
    internal static class Error
    {
        /// <summary>
        ///     InvalidOperationException with message like "Only one of '{0}' and '{1}' can be assigned to."
        /// </summary>
        internal static Exception AmbiguousAttributeValues(object p0, object p1)
        {
            return new InvalidOperationException(Strings.AmbiguousAttributeValues(p0, p1));
        }

        /// <summary>
        ///     CmdLine.CommandLineException with message like "Only one of /connectionStringName or /connectionString can be specified."
        /// </summary>
        internal static Exception AmbiguousConnectionString()
        {
            return new CommandLineException(Strings.AmbiguousConnectionString);
        }

        /// <summary>
        ///     CmdLine.CommandLineException with message like "/connectionString and /connectionProviderName must be specified together."
        /// </summary>
        internal static Exception MissingConnectionInfo()
        {
            return new CommandLineException(Strings.MissingConnectionInfo);
        }

        /// <summary>
        ///     CmdLine.CommandLineException with message like "Invalid ParameterIndex value"
        /// </summary>
        internal static Exception InvalidParameterIndexValue()
        {
            return new CommandLineException(Strings.InvalidParameterIndexValue);
        }

        /// <summary>
        ///     The exception that is thrown when a null reference (Nothing in Visual Basic) is passed to a method that does not accept it as a valid argument.
        /// </summary>
        internal static Exception ArgumentNull(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        /// <summary>
        ///     The exception that is thrown when the value of an argument is outside the allowable range of values as defined by the invoked method.
        /// </summary>
        internal static Exception ArgumentOutOfRange(string paramName)
        {
            return new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        ///     The exception that is thrown when the author has yet to implement the logic at this point in the program. This can act as an exception based TODO tag.
        /// </summary>
        internal static Exception NotImplemented()
        {
            return new NotImplementedException();
        }

        /// <summary>
        ///     The exception that is thrown when an invoked method is not supported, or when there is an attempt to read, seek, or write to a stream that does not support the invoked functionality.
        /// </summary>
        internal static Exception NotSupported()
        {
            return new NotSupportedException();
        }
    }

    ///<summary>
    ///    AutoGenerated resource class. Usage:
    ///
    ///    string s = EntityRes.GetString(EntityRes.MyIdenfitier);
    ///</summary>
    [GeneratedCode("Resources.Migrate.tt", "1.0.0.0")]
    internal sealed class EntityRes
    {
        internal const string MigrateTitle = "MigrateTitle";
        internal const string MigrateDescription = "MigrateDescription";
        internal const string ErrorMessage = "ErrorMessage";
        internal const string WarningMessage = "WarningMessage";
        internal const string VerboseMessage = "VerboseMessage";
        internal const string DuplicateCommand = "DuplicateCommand";
        internal const string DuplicateParameterIndex = "DuplicateParameterIndex";
        internal const string ParameterOutOfOrder = "ParameterOutOfOrder";
        internal const string InvalidKey = "InvalidKey";
        internal const string PressAnyKey = "PressAnyKey";
        internal const string UnsupportedPropertyType = "UnsupportedPropertyType";
        internal const string InvalidPropertyParameterIndexValue = "InvalidPropertyParameterIndexValue";
        internal const string AssemblyNameArgument = "AssemblyNameArgument";
        internal const string ConfigurationTypeNameArgument = "ConfigurationTypeNameArgument";
        internal const string AssemblyNameDescription = "AssemblyNameDescription";
        internal const string ConfigurationTypeNameDescription = "ConfigurationTypeNameDescription";
        internal const string TargetMigrationDescription = "TargetMigrationDescription";
        internal const string WorkingDirectoryDescription = "WorkingDirectoryDescription";
        internal const string ConfigurationFileDescription = "ConfigurationFileDescription";
        internal const string DataDirectoryDescription = "DataDirectoryDescription";
        internal const string ConnectionStringNameDescription = "ConnectionStringNameDescription";
        internal const string ConnectionStringDescription = "ConnectionStringDescription";
        internal const string ConnectionProviderNameDescription = "ConnectionProviderNameDescription";
        internal const string ForceDescription = "ForceDescription";
        internal const string VerboseDescription = "VerboseDescription";
        internal const string HelpDescription = "HelpDescription";
        internal const string AmbiguousAttributeValues = "AmbiguousAttributeValues";
        internal const string AmbiguousConnectionString = "AmbiguousConnectionString";
        internal const string MissingConnectionInfo = "MissingConnectionInfo";
        internal const string InvalidParameterIndexValue = "InvalidParameterIndexValue";

        private static EntityRes loader;
        private readonly ResourceManager resources;

        private EntityRes()
        {
            resources = new ResourceManager("System.Data.Entity.Properties.Resources.Migrate", typeof(DbContext).Assembly);
        }

        private static EntityRes GetLoader()
        {
            if (loader == null)
            {
                var sr = new EntityRes();
                Interlocked.CompareExchange(ref loader, sr, null);
            }
            return loader;
        }

        private static CultureInfo Culture
        {
            get { return null /*use ResourceManager default, CultureInfo.CurrentUICulture*/; }
        }

        public static ResourceManager Resources
        {
            get { return GetLoader().resources; }
        }

        public static string GetString(string name, params object[] args)
        {
            var sys = GetLoader();
            if (sys == null)
            {
                return null;
            }
            var res = sys.resources.GetString(name, Culture);

            if (args != null
                && args.Length > 0)
            {
                for (var i = 0; i < args.Length; i ++)
                {
                    var value = args[i] as String;
                    if (value != null
                        && value.Length > 1024)
                    {
                        args[i] = value.Substring(0, 1024 - 3) + "...";
                    }
                }
                return String.Format(CultureInfo.CurrentCulture, res, args);
            }
            else
            {
                return res;
            }
        }

        public static string GetString(string name)
        {
            var sys = GetLoader();
            if (sys == null)
            {
                return null;
            }
            return sys.resources.GetString(name, Culture);
        }

        public static string GetString(string name, out bool usedFallback)
        {
            // always false for this version of gensr
            usedFallback = false;
            return GetString(name);
        }

        public static object GetObject(string name)
        {
            var sys = GetLoader();
            if (sys == null)
            {
                return null;
            }
            return sys.resources.GetObject(name, Culture);
        }
    }
}
