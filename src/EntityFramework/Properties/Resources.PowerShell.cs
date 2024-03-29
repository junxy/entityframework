// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Resources
{
    using System.CodeDom.Compiler;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Globalization;
    using System.IO;
    using System.Resources;
    using System.Threading;

    /// <summary>
    ///     Strongly-typed and parameterized string resources.
    /// </summary>
    [GeneratedCode("Resources.PowerShell.tt", "1.0.0.0")]
    internal static class Strings
    {
        /// <summary>
        ///     A string like "Specify the '-Verbose' flag to view the SQL statements being applied to the target database."
        /// </summary>
        internal static string UpdateDatabaseCommand_VerboseInstructions
        {
            get { return EntityRes.GetString(EntityRes.UpdateDatabaseCommand_VerboseInstructions); }
        }

        /// <summary>
        ///     A string like "The '-SourceMigration' parameter is only valid when using '-Script'."
        /// </summary>
        internal static string UpdateDatabaseCommand_SourceMigrationWithoutScript
        {
            get { return EntityRes.GetString(EntityRes.UpdateDatabaseCommand_SourceMigrationWithoutScript); }
        }

        /// <summary>
        ///     A string like "No migrations have been applied to the target database."
        /// </summary>
        internal static string GetMigrationsCommand_NoHistory
        {
            get { return EntityRes.GetString(EntityRes.GetMigrationsCommand_NoHistory); }
        }

        /// <summary>
        ///     A string like "Scaffolding migration '{0}'."
        /// </summary>
        internal static string LoggingGenerate(object p0)
        {
            return EntityRes.GetString(EntityRes.LoggingGenerate, p0);
        }

        /// <summary>
        ///     A string like "Only the Designer Code for migration '{0}' was re-scaffolded. To re-scaffold the entire migration, use the -Force parameter."
        /// </summary>
        internal static string RescaffoldNoForce(object p0)
        {
            return EntityRes.GetString(EntityRes.RescaffoldNoForce, p0);
        }

        /// <summary>
        ///     A string like "The Designer Code for this migration file includes a snapshot of your current Code First model. This snapshot is used to calculate the changes to your model when you scaffold the next migration. If you make additional changes to your model that you want to include in this migration, then you can re-scaffold it by running 'Add-Migration {0}' again."
        /// </summary>
        internal static string SnapshotBehindWarning(object p0)
        {
            return EntityRes.GetString(EntityRes.SnapshotBehindWarning, p0);
        }

        /// <summary>
        ///     A string like "Unable to generate an explicit migration because the following explicit migrations are pending: [{0}]. Apply the pending explicit migrations before attempting to generate a new explicit migration."
        /// </summary>
        internal static string MigrationsPendingException(object p0)
        {
            return EntityRes.GetString(EntityRes.MigrationsPendingException, p0);
        }

        /// <summary>
        ///     A string like "You can use the Add-Migration command to write the pending model changes to a code-based migration."
        /// </summary>
        internal static string AutomaticMigrationDisabledInfo
        {
            get { return EntityRes.GetString(EntityRes.AutomaticMigrationDisabledInfo); }
        }

        /// <summary>
        ///     A string like "Edit the generated Configuration class to specify the context to enable migrations for."
        /// </summary>
        internal static string EnableMigrations_ManuallyEnterContext
        {
            get { return EntityRes.GetString(EntityRes.EnableMigrations_ManuallyEnterContext); }
        }

        /// <summary>
        ///     A string like "No classes deriving from DbContext found in the current project."
        /// </summary>
        internal static string EnableMigrations_NoContexts
        {
            get { return EntityRes.GetString(EntityRes.EnableMigrations_NoContexts); }
        }

        /// <summary>
        ///     A string like "More than one class deriving from DbContext found in the current project."
        /// </summary>
        internal static string EnableMigrations_MultipleContexts
        {
            get { return EntityRes.GetString(EntityRes.EnableMigrations_MultipleContexts); }
        }

        /// <summary>
        ///     A string like "Error while searching for context type (specify -Verbose to see exception details)."
        /// </summary>
        internal static string EnableMigrations_ErrorFindingContexts
        {
            get { return EntityRes.GetString(EntityRes.EnableMigrations_ErrorFindingContexts); }
        }

        /// <summary>
        ///     A string like "Code First Migrations enabled for project {0}."
        /// </summary>
        internal static string EnableMigrations_Success(object p0)
        {
            return EntityRes.GetString(EntityRes.EnableMigrations_Success, p0);
        }

        /// <summary>
        ///     A string like "Checking if the context targets an existing database..."
        /// </summary>
        internal static string EnableMigrations_BeginInitialScaffold
        {
            get { return EntityRes.GetString(EntityRes.EnableMigrations_BeginInitialScaffold); }
        }

        /// <summary>
        ///     A string like "Detected database created with a database initializer. Scaffolded migration '{0}' corresponding to existing database. To use an automatic migration instead, delete the Migrations folder and re-run Enable-Migrations specifying the -EnableAutomaticMigrations parameter."
        /// </summary>
        internal static string EnableMigrations_InitialScaffold(object p0)
        {
            return EntityRes.GetString(EntityRes.EnableMigrations_InitialScaffold, p0);
        }

        /// <summary>
        ///     A string like "Code First Migrations does not support web site projects."
        /// </summary>
        internal static string WebSiteProjectNotSupported
        {
            get { return EntityRes.GetString(EntityRes.WebSiteProjectNotSupported); }
        }

        /// <summary>
        ///     A string like "Retrieving migrations that have been applied to the target database."
        /// </summary>
        internal static string GetMigrationsCommand_Intro
        {
            get { return EntityRes.GetString(EntityRes.GetMigrationsCommand_Intro); }
        }

        /// <summary>
        ///     A string like "Migrations have already been enabled in project '{0}'. To overwrite the existing migrations configuration, use the -Force parameter."
        /// </summary>
        internal static string MigrationsAlreadyEnabled(object p0)
        {
            return EntityRes.GetString(EntityRes.MigrationsAlreadyEnabled, p0);
        }

        /// <summary>
        ///     A string like "Failed to add the Entity Framework 'defaultConnectionFactory' entry to the .config file '{0}' in the current project. The default SqlConnectionFactory configured for '.\\SQLEXPRESS' will be used unless you either add the 'defaultConnectionFactory' entry to the .config file manually or specify connection strings in code. See inner exception for details."
        /// </summary>
        internal static string SaveConnectionFactoryInConfigFailed(object p0)
        {
            return EntityRes.GetString(EntityRes.SaveConnectionFactoryInConfigFailed, p0);
        }
    }

    /// <summary>
    ///     Strongly-typed and parameterized exception factory.
    /// </summary>
    [GeneratedCode("Resources.PowerShell.tt", "1.0.0.0")]
    internal static class Error
    {
        /// <summary>
        ///     Migrations.MigrationsPendingException with message like "Unable to generate an explicit migration because the following explicit migrations are pending: [{0}]. Apply the pending explicit migrations before attempting to generate a new explicit migration."
        /// </summary>
        internal static Exception MigrationsPendingException(object p0)
        {
            return new MigrationsPendingException(Strings.MigrationsPendingException(p0));
        }

        /// <summary>
        ///     Migrations.ProjectTypeNotSupportedException with message like "Code First Migrations does not support web site projects."
        /// </summary>
        internal static Exception WebSiteProjectNotSupported()
        {
            return new ProjectTypeNotSupportedException(Strings.WebSiteProjectNotSupported);
        }

        /// <summary>
        ///     Migrations.Infrastructure.MigrationsException with message like "Migrations have already been enabled in project '{0}'. To overwrite the existing migrations configuration, use the -Force parameter."
        /// </summary>
        internal static Exception MigrationsAlreadyEnabled(object p0)
        {
            return new MigrationsException(Strings.MigrationsAlreadyEnabled(p0));
        }

        /// <summary>
        ///     System.IO.IOException with message like "Failed to add the Entity Framework 'defaultConnectionFactory' entry to the .config file '{0}' in the current project. The default SqlConnectionFactory configured for '.\\SQLEXPRESS' will be used unless you either add the 'defaultConnectionFactory' entry to the .config file manually or specify connection strings in code. See inner exception for details."
        /// </summary>
        internal static Exception SaveConnectionFactoryInConfigFailed(object p0)
        {
            return new IOException(Strings.SaveConnectionFactoryInConfigFailed(p0));
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
    [GeneratedCode("Resources.PowerShell.tt", "1.0.0.0")]
    internal sealed class EntityRes
    {
        internal const string UpdateDatabaseCommand_VerboseInstructions = "UpdateDatabaseCommand_VerboseInstructions";
        internal const string UpdateDatabaseCommand_SourceMigrationWithoutScript = "UpdateDatabaseCommand_SourceMigrationWithoutScript";
        internal const string GetMigrationsCommand_NoHistory = "GetMigrationsCommand_NoHistory";
        internal const string LoggingGenerate = "LoggingGenerate";
        internal const string RescaffoldNoForce = "RescaffoldNoForce";
        internal const string SnapshotBehindWarning = "SnapshotBehindWarning";
        internal const string MigrationsPendingException = "MigrationsPendingException";
        internal const string AutomaticMigrationDisabledInfo = "AutomaticMigrationDisabledInfo";
        internal const string EnableMigrations_ManuallyEnterContext = "EnableMigrations_ManuallyEnterContext";
        internal const string EnableMigrations_NoContexts = "EnableMigrations_NoContexts";
        internal const string EnableMigrations_MultipleContexts = "EnableMigrations_MultipleContexts";
        internal const string EnableMigrations_ErrorFindingContexts = "EnableMigrations_ErrorFindingContexts";
        internal const string EnableMigrations_Success = "EnableMigrations_Success";
        internal const string EnableMigrations_BeginInitialScaffold = "EnableMigrations_BeginInitialScaffold";
        internal const string EnableMigrations_InitialScaffold = "EnableMigrations_InitialScaffold";
        internal const string WebSiteProjectNotSupported = "WebSiteProjectNotSupported";
        internal const string GetMigrationsCommand_Intro = "GetMigrationsCommand_Intro";
        internal const string MigrationsAlreadyEnabled = "MigrationsAlreadyEnabled";
        internal const string SaveConnectionFactoryInConfigFailed = "SaveConnectionFactoryInConfigFailed";

        private static EntityRes loader;
        private readonly ResourceManager resources;

        private EntityRes()
        {
            resources = new ResourceManager("System.Data.Entity.Properties.Resources.PowerShell", typeof(DbContext).Assembly);
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
