// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityModel.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    ///     class representing the Schema element in the schema
    /// </summary>
    [DebuggerDisplay("Namespace={Namespace}, PublicKeyToken={PublicKeyToken}, Version={Version}")]
    internal class Schema : SchemaElement
    {
        #region Instance Fields

        private const int RootDepth = 2;
        // if adding properties also add to InitializeObject()!
        private List<EdmSchemaError> _errors = new List<EdmSchemaError>();
        // We need to keep track of functions seperately, since we can't deduce the strong name of the function, 
        // until we have resolved the parameter names. Hence we keep track of functions seperately and add them
        // to the schema types list, in the validate phase
        private List<Function> _functions;

        private AliasResolver _aliasResolver;
        private string _location;
        protected string _namespaceName;
        private List<SchemaType> _schemaTypes;

        private int _depth; // recursion depth in Parse used by *Handlers to know which hander set to set
        private double _schemaVersion = XmlConstants.UndefinedVersion;
        private readonly SchemaManager _schemaManager;

        private bool? _useStrongSpatialTypes;

        #endregion

        #region Public Methods

        public Schema(SchemaManager schemaManager)
            : base(null)
        {
            Debug.Assert(schemaManager != null, "SchemaManager parameter should never be null");
            _schemaManager = schemaManager;
            _errors = new List<EdmSchemaError>();
        }

        internal IList<EdmSchemaError> Resolve()
        {
            ResolveTopLevelNames();
            if (_errors.Count != 0)
            {
                return ResetErrors();
            }
            ResolveSecondLevelNames();
            return ResetErrors();
        }

        internal IList<EdmSchemaError> ValidateSchema()
        {
            Validate();
            return ResetErrors();
        }

        internal void AddError(EdmSchemaError error)
        {
            _errors.Add(error);
        }

        /// <summary>
        ///     Populate the schema object from a schema
        /// </summary>
        /// <param name="sourceReader"> TextReader containing the schema xml definition </param>
        /// <param name="source"> Uri containing path to a schema file (may be null) </param>
        /// <returns> list of errors </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal IList<EdmSchemaError> Parse(XmlReader sourceReader, string sourceLocation)
        {
            // We don't Assert (sourceReader != null) here any more because third-party
            // providers that extend XmlEnabledProvidermanifest could hit this code. The
            // following code eventually detects the anomaly and a ProviderIncompatible
            // exception is thrown (which is the right thing to do in such cases).

            try
            {
                // user specified a stream to read from, read from it.
                // The Uri is just used to identify the stream in errors.
                var readerSettings = CreateXmlReaderSettings();
                var wrappedReader = XmlReader.Create(sourceReader, readerSettings);
                return InternalParse(wrappedReader, sourceLocation);
            }
            catch (IOException ex)
            {
                AddError(ErrorCode.IOException, EdmSchemaErrorSeverity.Error, sourceReader, ex);
            }

            // do not close the reader here (SQLBUDT 522950)

            return ResetErrors();
        }

        /// <summary>
        ///     Populate the schema object from a schema
        /// </summary>
        /// <param name="sourceReader"> TextReader containing the schema xml definition </param>
        /// <param name="source"> Uri containing path to a schema file (may be null) </param>
        /// <returns> list of errors </returns>
        private IList<EdmSchemaError> InternalParse(XmlReader sourceReader, string sourceLocation)
        {
            Debug.Assert(sourceReader != null, "sourceReader parameter is null");

            // these need to be set before any calls to AddError are made.
            Schema = this;
            Location = sourceLocation;

            try
            {
                // to make life simpler, we skip down to the first/root element, unless we're
                // already there
                if (sourceReader.NodeType
                    != XmlNodeType.Element)
                {
                    while (sourceReader.Read()
                           && sourceReader.NodeType != XmlNodeType.Element)
                    {
                    }
                }
                GetPositionInfo(sourceReader);

                var expectedNamespaces = SomSchemaSetHelper.GetPrimarySchemaNamespaces(DataModel);

                // the root element needs to be either TDL or Schema in our namespace
                if (sourceReader.EOF)
                {
                    if (sourceLocation != null)
                    {
                        AddError(ErrorCode.EmptyFile, EdmSchemaErrorSeverity.Error, Strings.EmptyFile(sourceLocation));
                    }
                    else
                    {
                        AddError(ErrorCode.EmptyFile, EdmSchemaErrorSeverity.Error, Strings.EmptySchemaTextReader);
                    }
                }
                else if (!expectedNamespaces.Contains(sourceReader.NamespaceURI))
                {
                    Func<object, object, object, string> messageFormat = Strings.UnexpectedRootElement;
                    if (string.IsNullOrEmpty(sourceReader.NamespaceURI))
                    {
                        messageFormat = Strings.UnexpectedRootElementNoNamespace;
                    }
                    var expectedNamespacesString = Helper.GetCommaDelimitedString(expectedNamespaces);
                    AddError(
                        ErrorCode.UnexpectedXmlElement, EdmSchemaErrorSeverity.Error,
                        messageFormat(sourceReader.NamespaceURI, sourceReader.LocalName, expectedNamespacesString));
                }
                else
                {
                    SchemaXmlNamespace = sourceReader.NamespaceURI;
                    if (DataModel == SchemaDataModelOption.EntityDataModel)
                    {
                        if (SchemaXmlNamespace == XmlConstants.ModelNamespace_1)
                        {
                            SchemaVersion = XmlConstants.EdmVersionForV1;
                        }
                        else if (SchemaXmlNamespace == XmlConstants.ModelNamespace_1_1)
                        {
                            SchemaVersion = XmlConstants.EdmVersionForV1_1;
                        }
                        else if (SchemaXmlNamespace == XmlConstants.ModelNamespace_2)
                        {
                            SchemaVersion = XmlConstants.EdmVersionForV2;
                        }
                        else
                        {
                            Debug.Assert(SchemaXmlNamespace == XmlConstants.ModelNamespace_3, "Unknown namespace in CSDL");
                            SchemaVersion = XmlConstants.EdmVersionForV3;
                        }
                    }
                    else if (DataModel == SchemaDataModelOption.ProviderDataModel)
                    {
                        if (SchemaXmlNamespace == XmlConstants.TargetNamespace_1)
                        {
                            SchemaVersion = XmlConstants.StoreVersionForV1;
                        }
                        else if (SchemaXmlNamespace == XmlConstants.TargetNamespace_2)
                        {
                            SchemaVersion = XmlConstants.StoreVersionForV2;
                        }
                        else
                        {
                            Debug.Assert(SchemaXmlNamespace == XmlConstants.TargetNamespace_3, "Unknown namespace in SSDL");
                            SchemaVersion = XmlConstants.StoreVersionForV3;
                        }
                    }

                    switch (sourceReader.LocalName)
                    {
                        case "Schema":
                        case "ProviderManifest":
                            HandleTopLevelSchemaElement(sourceReader);
                            // this forces the reader to look beyond this top
                            // level element, and complain if there is another one.
                            sourceReader.Read();
                            break;
                        default:
                            AddError(
                                ErrorCode.UnexpectedXmlElement, EdmSchemaErrorSeverity.Error,
                                Strings.UnexpectedRootElement(sourceReader.NamespaceURI, sourceReader.LocalName, SchemaXmlNamespace));
                            break;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                AddError(ErrorCode.InternalError, EdmSchemaErrorSeverity.Error, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                AddError(ErrorCode.UnauthorizedAccessException, EdmSchemaErrorSeverity.Error, sourceReader, ex);
            }
            catch (IOException ex)
            {
                AddError(ErrorCode.IOException, EdmSchemaErrorSeverity.Error, sourceReader, ex);
            }
            catch (SecurityException ex)
            {
                AddError(ErrorCode.SecurityError, EdmSchemaErrorSeverity.Error, sourceReader, ex);
            }
            catch (XmlException ex)
            {
                AddError(ErrorCode.XmlError, EdmSchemaErrorSeverity.Error, sourceReader, ex);
            }

            return ResetErrors();
        }

        internal static XmlReaderSettings CreateEdmStandardXmlReaderSettings()
        {
            var readerSettings = new XmlReaderSettings();

            readerSettings.CheckCharacters = true;
            readerSettings.CloseInput = false;
            readerSettings.IgnoreWhitespace = true;
            readerSettings.ConformanceLevel = ConformanceLevel.Auto;
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.DtdProcessing = DtdProcessing.Prohibit;

            // remove flags
            // the ProcessInlineSchema, and ProcessSchemaLocation flags must be removed for the same
            // xsd schema to be used on multiple threads
            readerSettings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessIdentityConstraints;
            readerSettings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessSchemaLocation;
            readerSettings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessInlineSchema;

            return readerSettings;
        }

        private XmlReaderSettings CreateXmlReaderSettings()
        {
            var readerSettings = CreateEdmStandardXmlReaderSettings();

            // add flags
            readerSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

            readerSettings.ValidationEventHandler += OnSchemaValidationEvent;
            readerSettings.ValidationType = ValidationType.Schema;

            var schemaSet = SomSchemaSetHelper.GetSchemaSet(DataModel);

            // Do not use readerSetting.Schemas.Add(schemaSet)
            // you must use the line below for this to work in 
            // a multithread environment
            readerSettings.Schemas = schemaSet;

            return readerSettings;
        }

        /// <summary>
        ///     Called by the validating reader when the schema is xsd invalid
        /// </summary>
        /// <param name="sender"> the validating reader </param>
        /// <param name="e"> information about the validation error </param>
        internal void OnSchemaValidationEvent(object sender, ValidationEventArgs e)
        {
            Debug.Assert(e != null);
            var reader = sender as XmlReader;
            if (reader != null
                && !IsValidateableXmlNamespace(reader.NamespaceURI, reader.NodeType == XmlNodeType.Attribute))
            {
                //For V1 Schemas, we never returned errors for elements in custom namespaces.
                //But the behavior is not totally correct since the error might have occured inside a known namespace
                //even though the element that the reader pointing to is in a custom namespace. But if we fix that, it would
                //cause lot of breaking changes for V1 customers since we can not change the xsd for them.
                //For attributes, we can ignore the errors always since attributes are unordered and custom attributes should always be allowed.
                if ((SchemaVersion == XmlConstants.EdmVersionForV1)
                    || (SchemaVersion == XmlConstants.EdmVersionForV1_1))
                {
                    return;
                }
                // For V2 Schemas that have custom namespaces, the only thing we would not catch are warnings.
                //We also need to ignore any errors reported on custom namespace since they would become annotations.
                Debug.Assert(
                    SchemaVersion >= XmlConstants.EdmVersionForV2 || SchemaVersion == XmlConstants.UndefinedVersion,
                    "Have you added a new Edm Version?");
                if ((reader.NodeType == XmlNodeType.Attribute)
                    || (e.Severity == XmlSeverityType.Warning))
                {
                    return;
                }
            }

            //Ignore the warnings for attributes in V2 since we would see warnings for undeclared attributes in empty namespace
            //that are on elements in custom namespace. For undeclared attributes in known namespace, we would see errors.
            if ((SchemaVersion >= XmlConstants.EdmVersionForV2) && (reader.NodeType == XmlNodeType.Attribute)
                && (e.Severity == XmlSeverityType.Warning))
            {
                return;
            }

            var severity = EdmSchemaErrorSeverity.Error;
            if (e.Severity
                == XmlSeverityType.Warning)
            {
                severity = EdmSchemaErrorSeverity.Warning;
            }
            AddError(ErrorCode.XmlError, severity, e.Exception.LineNumber, e.Exception.LinePosition, e.Message);
        }

        public bool IsParseableXmlNamespace(string xmlNamespaceUri, bool isAttribute)
        {
            if (string.IsNullOrEmpty(xmlNamespaceUri) && isAttribute)
            {
                // we own the empty namespace for attributes
                return true;
            }

            if (_parseableXmlNamespaces == null)
            {
                _parseableXmlNamespaces = new HashSet<string>();
                foreach (var schemaResource in XmlSchemaResource.GetMetadataSchemaResourceMap(SchemaVersion).Values)
                {
                    _parseableXmlNamespaces.Add(schemaResource.NamespaceUri);
                }
            }

            return _parseableXmlNamespaces.Contains(xmlNamespaceUri);
        }

        private HashSet<string> _validatableXmlNamespaces;
        private HashSet<string> _parseableXmlNamespaces;

        public bool IsValidateableXmlNamespace(string xmlNamespaceUri, bool isAttribute)
        {
            if (string.IsNullOrEmpty(xmlNamespaceUri) && isAttribute)
            {
                // we own the empty namespace for attributes
                return true;
            }

            if (_validatableXmlNamespaces == null)
            {
                var validatableXmlNamespaces = new HashSet<string>();
                var schemaVersion = SchemaVersion == XmlConstants.UndefinedVersion ? XmlConstants.SchemaVersionLatest : SchemaVersion;
                foreach (var schemaResource in XmlSchemaResource.GetMetadataSchemaResourceMap(schemaVersion).Values)
                {
                    AddAllSchemaResourceNamespaceNames(validatableXmlNamespaces, schemaResource);
                }

                if (SchemaVersion == XmlConstants.UndefinedVersion)
                {
                    // we are getting called before the version is set
                    return validatableXmlNamespaces.Contains(xmlNamespaceUri);
                }
                _validatableXmlNamespaces = validatableXmlNamespaces;
            }

            return _validatableXmlNamespaces.Contains(xmlNamespaceUri);
        }

        private static void AddAllSchemaResourceNamespaceNames(HashSet<string> hashSet, XmlSchemaResource schemaResource)
        {
            hashSet.Add(schemaResource.NamespaceUri);
            foreach (var import in schemaResource.ImportedSchemas)
            {
                AddAllSchemaResourceNamespaceNames(hashSet, import);
            }
        }

        internal override void ResolveTopLevelNames()
        {
            base.ResolveTopLevelNames();

            // Resolve all the referenced namespace to make sure that this namespace is valid
            AliasResolver.ResolveNamespaces();

            foreach (SchemaElement element in SchemaTypes)
            {
                element.ResolveTopLevelNames();
            }

            foreach (var function in Functions)
            {
                function.ResolveTopLevelNames();
            }
        }

        internal override void ResolveSecondLevelNames()
        {
            base.ResolveSecondLevelNames();
            foreach (SchemaElement element in SchemaTypes)
            {
                element.ResolveSecondLevelNames();
            }

            foreach (var function in Functions)
            {
                function.ResolveSecondLevelNames();
            }
        }

        /// <summary>
        ///     Vaidate the schema.
        /// </summary>
        /// <returns> list of errors </returns>
        internal override void Validate()
        {
            if (String.IsNullOrEmpty(Namespace))
            {
                AddError(ErrorCode.MissingNamespaceAttribute, EdmSchemaErrorSeverity.Error, Strings.MissingNamespaceAttribute);
                return;
            }

            // Also check for alias to be system namespace
            if (!String.IsNullOrEmpty(Alias)
                && EdmItemCollection.IsSystemNamespace(ProviderManifest, Alias))
            {
                AddError(
                    ErrorCode.CannotUseSystemNamespaceAsAlias, EdmSchemaErrorSeverity.Error,
                    Strings.CannotUseSystemNamespaceAsAlias(Alias));
            }

            // Check whether the schema namespace is a system namespace. We set the provider manifest to edm provider manifest
            // if we need to check for system namespace. Otherwise, it will be set to null (if we are loading edm provider manifest)
            if (ProviderManifest != null
                &&
                EdmItemCollection.IsSystemNamespace(ProviderManifest, Namespace))
            {
                AddError(ErrorCode.SystemNamespace, EdmSchemaErrorSeverity.Error, Strings.SystemNamespaceEncountered(Namespace));
            }

            foreach (SchemaElement schemaType in SchemaTypes)
            {
                schemaType.Validate();
            }

            foreach (var function in Functions)
            {
                AddFunctionType(function);
                function.Validate();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The namespaceUri of the winfs xml namespace
        /// </summary>
        internal string SchemaXmlNamespace { get; private set; }

        internal DbProviderManifest ProviderManifest
        {
            get
            {
                return
                    _schemaManager.GetProviderManifest(
                        (string message, ErrorCode code, EdmSchemaErrorSeverity severity) => AddError(code, severity, message));
            }
        }

        /// <summary>
        ///     Version of the EDM that this schema represents.
        /// </summary>
        internal double SchemaVersion
        {
            get { return _schemaVersion; }
            set { _schemaVersion = value; }
        }

        /// <summary>
        ///     Alias for the schema (null if none)
        /// </summary>
        internal virtual string Alias { get; private set; }

        /// <summary>
        ///     Namespace of the schema
        /// </summary>
        internal virtual string Namespace
        {
            get { return _namespaceName; }
            private set { _namespaceName = value; }
        }

        /// <summary>
        ///     Uri containing the file that defines the schema
        /// </summary>
        internal string Location
        {
            get { return _location; }
            private set { _location = value; }
        }

        private MetadataProperty _schemaSourceProperty;

        internal MetadataProperty SchemaSource
        {
            get
            {
                if (_schemaSourceProperty == null)
                {
                    // create the System MetadataProperty for the SchemaSource
                    _schemaSourceProperty = new MetadataProperty(
                        "SchemaSource",
                        EdmProviderManifest.Instance.GetPrimitiveType(PrimitiveTypeKind.String),
                        false, // IsCollection
                        _location != null ? _location : string.Empty);
                }

                return _schemaSourceProperty;
            }
        }

        /// <summary>
        ///     List of all types defined in the schema
        /// </summary>
        internal List<SchemaType> SchemaTypes
        {
            get
            {
                if (_schemaTypes == null)
                {
                    _schemaTypes = new List<SchemaType>();
                }
                return _schemaTypes;
            }
        }

        /// <summary>
        ///     Fully qualified name of the schema (same as the namespace name)
        /// </summary>
        public override string FQName
        {
            get { return Namespace; }
        }

        private List<Function> Functions
        {
            get
            {
                if (_functions == null)
                {
                    _functions = new List<Function>();
                }
                return _functions;
            }
        }

        #endregion

        #region Protected Properties

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.EntityType))
            {
                HandleEntityTypeElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.ComplexType))
            {
                HandleInlineTypeElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.Association))
            {
                HandleAssociationElement(reader);
                return true;
            }

            // These elements are only supported in EntityDataModel
            if (DataModel == SchemaDataModelOption.EntityDataModel)
            {
                if (CanHandleElement(reader, XmlConstants.Using))
                {
                    HandleUsingElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.Function))
                {
                    HandleModelFunctionElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.EnumType))
                {
                    HandleEnumTypeElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.ValueTerm))
                {
                    // EF does not support this EDM 3.0 element, so ignore it.
                    SkipElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.Annotations))
                {
                    // EF does not support this EDM 3.0 element, so ignore it.
                    SkipElement(reader);
                    return true;
                }
            }

            if (DataModel == SchemaDataModelOption.EntityDataModel
                ||
                DataModel == SchemaDataModelOption.ProviderDataModel)
            {
                if (CanHandleElement(reader, XmlConstants.EntityContainer))
                {
                    HandleEntityContainerTypeElement(reader);
                    return true;
                }
                else if (DataModel == SchemaDataModelOption.ProviderDataModel)
                {
                    if (CanHandleElement(reader, XmlConstants.Function))
                    {
                        HandleFunctionElement(reader);
                        return true;
                    }
                }
            }
            else
            {
                Debug.Assert(DataModel == SchemaDataModelOption.ProviderManifestModel, "Did you add a new option?");
                if (CanHandleElement(reader, XmlConstants.TypesElement))
                {
                    SkipThroughElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.FunctionsElement))
                {
                    SkipThroughElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.Function))
                {
                    HandleFunctionElement(reader);
                    return true;
                }
                else if (CanHandleElement(reader, XmlConstants.TypeElement))
                {
                    HandleTypeInformationElement(reader);
                    return true;
                }
            }

            return false;
        }

        protected override bool ProhibitAttribute(string namespaceUri, string localName)
        {
            if (base.ProhibitAttribute(namespaceUri, localName))
            {
                return true;
            }

            if (namespaceUri == null
                && localName == XmlConstants.Name)
            {
                return false;
            }
            return false;
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            Debug.Assert(_depth > 0);
            if (_depth == 1)
            {
                return false;
            }
            else
            {
                if (base.HandleAttribute(reader))
                {
                    return true;
                }
                else if (CanHandleAttribute(reader, XmlConstants.Alias))
                {
                    HandleAliasAttribute(reader);
                    return true;
                }
                else if (CanHandleAttribute(reader, XmlConstants.Namespace))
                {
                    HandleNamespaceAttribute(reader);
                    return true;
                }
                else if (CanHandleAttribute(reader, XmlConstants.Provider))
                {
                    HandleProviderAttribute(reader);
                    return true;
                }
                else if (CanHandleAttribute(reader, XmlConstants.ProviderManifestToken))
                {
                    HandleProviderManifestTokenAttribute(reader);
                    return true;
                }
                else if (reader.NamespaceURI == XmlConstants.AnnotationNamespace
                         && reader.LocalName == XmlConstants.UseStrongSpatialTypes)
                {
                    HandleUseStrongSpatialTypesAnnotation(reader);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Called when all attributes for the schema element have been handled
        /// </summary>
        protected override void HandleAttributesComplete()
        {
            if (_depth < RootDepth)
            {
                return;
            }
            else if (_depth == RootDepth)
            {
                // only call when done with the root element
                _schemaManager.EnsurePrimitiveSchemaIsLoaded(SchemaVersion);
            }

            base.HandleAttributesComplete();
        }

        protected override void SkipThroughElement(XmlReader reader)
        {
            try
            {
                _depth++;
                base.SkipThroughElement(reader);
            }
            finally
            {
                _depth--;
            }
        }

        /// <summary>
        ///     Look up a fully qualified type name reference.
        /// </summary>
        /// <param name="usingElement"> element containing the reference </param>
        /// <param name="typeName"> the fully qualified type name </param>
        /// <param name="type"> the referenced schema type </param>
        /// <returns> false if there was an error </returns>
        internal bool ResolveTypeName(SchemaElement usingElement, string typeName, out SchemaType type)
        {
            Debug.Assert(usingElement != null);
            Debug.Assert(typeName != null);

            type = null;

            // get the schema(s) that match the namespace/alias
            string actualQualification;
            string unqualifiedTypeName;
            Utils.ExtractNamespaceAndName(typeName, out actualQualification, out unqualifiedTypeName);
            var definingQualification = actualQualification;

            if (definingQualification == null)
            {
                definingQualification = ProviderManifest == null ? _namespaceName : ProviderManifest.NamespaceName;
            }

            string namespaceName;
            // First check if there is an alias defined by this name. For primitive type namespace, we do not need to resolve
            // any alias, since that's a reserved keyword and we don't allow alias with that name
            if (actualQualification == null
                || !AliasResolver.TryResolveAlias(definingQualification, out namespaceName))
            {
                namespaceName = definingQualification;
            }

            // Resolve the type name
            if (!SchemaManager.TryResolveType(namespaceName, unqualifiedTypeName, out type))
            {
                // it must be an undefined type.
                if (actualQualification == null)
                {
                    // Every type except the primitive type must be qualified
                    usingElement.AddError(ErrorCode.NotInNamespace, EdmSchemaErrorSeverity.Error, Strings.NotNamespaceQualified(typeName));
                }
                else if (!SchemaManager.IsValidNamespaceName(namespaceName))
                {
                    usingElement.AddError(
                        ErrorCode.BadNamespace, EdmSchemaErrorSeverity.Error, Strings.BadNamespaceOrAlias(actualQualification));
                }
                else
                {
                    // if the type name was alias qualified
                    if (namespaceName != definingQualification)
                    {
                        usingElement.AddError(
                            ErrorCode.NotInNamespace, EdmSchemaErrorSeverity.Error,
                            Strings.NotInNamespaceAlias(unqualifiedTypeName, namespaceName, definingQualification));
                    }
                    else
                    {
                        usingElement.AddError(
                            ErrorCode.NotInNamespace, EdmSchemaErrorSeverity.Error,
                            Strings.NotInNamespaceNoAlias(unqualifiedTypeName, namespaceName));
                    }
                }
                return false;
            }
                // For ssdl and provider manifest, make sure that the type is present in this schema or primitive schema
            else if (DataModel != SchemaDataModelOption.EntityDataModel && type.Schema != this
                     && type.Schema != SchemaManager.PrimitiveSchema)
            {
                Debug.Assert(type.Namespace != Namespace, "Using element is not allowed in the schema of ssdl and provider manifest");
                usingElement.AddError(
                    ErrorCode.InvalidNamespaceOrAliasSpecified, EdmSchemaErrorSeverity.Error,
                    Strings.InvalidNamespaceOrAliasSpecified(actualQualification));
                return false;
            }

            return true;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        ///     List containing the current schema and all referenced schemas. Used for alias and namespace lookup.
        /// </summary>
        internal AliasResolver AliasResolver
        {
            get
            {
                if (_aliasResolver == null)
                {
                    _aliasResolver = new AliasResolver(this);
                }

                return _aliasResolver;
            }
        }

        /// <summary>
        ///     The schema data model
        /// </summary>
        internal SchemaDataModelOption DataModel
        {
            get { return SchemaManager.DataModel; }
        }

        /// <summary>
        ///     The schema data model
        /// </summary>
        internal SchemaManager SchemaManager
        {
            get { return _schemaManager; }
        }

        internal bool UseStrongSpatialTypes
        {
            get { return _useStrongSpatialTypes ?? true; }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Handler for the Namespace attribute
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at Namespace attribute </param>
        private void HandleNamespaceAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var returnValue = HandleDottedNameAttribute(reader, Namespace);
            if (!returnValue.Succeeded)
            {
                return;
            }

            Namespace = returnValue.Value;
        }

        /// <summary>
        ///     Handler for the Alias attribute
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at Alias attribute </param>
        private void HandleAliasAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);

            Alias = HandleUndottedNameAttribute(reader, Alias);
        }

        /// <summary>
        ///     Handler for the Provider attribute
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at Provider attribute </param>
        private void HandleProviderAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var provider = reader.Value;
            _schemaManager.ProviderNotification(
                provider,
                (string message, ErrorCode code, EdmSchemaErrorSeverity severity) => AddError(code, severity, reader, message));
        }

        /// <summary>
        ///     Handler for the ProviderManifestToken attribute
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at ProviderManifestToken attribute </param>
        private void HandleProviderManifestTokenAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var providerManifestToken = reader.Value;
            _schemaManager.ProviderManifestTokenNotification(
                providerManifestToken,
                (string message, ErrorCode code, EdmSchemaErrorSeverity severity) => AddError(code, severity, reader, message));
        }

        private void HandleUseStrongSpatialTypesAnnotation(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var isStrict = false;
            if (HandleBoolAttribute(reader, ref isStrict))
            {
                _useStrongSpatialTypes = isStrict;
            }
        }

        /// <summary>
        ///     Handler for the using element
        /// </summary>
        /// <param name="reader"> </param>
        private void HandleUsingElement(XmlReader reader)
        {
            var referencedNamespace = new UsingElement(this);
            referencedNamespace.Parse(reader);
            AliasResolver.Add(referencedNamespace);
        }

        /// <summary>
        ///     Handler for the EnumType element.
        /// </summary>
        /// <param name="reader"> Source xml reader currently positioned on the EnumType element. </param>
        private void HandleEnumTypeElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var enumType = new SchemaEnumType(this);
            enumType.Parse(reader);

            TryAddType(enumType, doNotAddErrorForEmptyName: true);
        }

        /// <summary>
        ///     Handler for the top level element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at top level element </param>
        private void HandleTopLevelSchemaElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            try
            {
                _depth += RootDepth;
                Parse(reader);
            }
            finally
            {
                _depth -= RootDepth;
            }
        }

        /// <summary>
        ///     Handler for the EntityType element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at EntityType element </param>
        private void HandleEntityTypeElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var itemType = new SchemaEntityType(this);

            itemType.Parse(reader);

            TryAddType(itemType, true /*doNotAddErrorForEmptyName*/);
        }

        /// <summary>
        ///     Handler for the TypeInformation element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at EntityType element </param>
        private void HandleTypeInformationElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var type = new TypeElement(this);

            type.Parse(reader);

            TryAddType(type, true /*doNotAddErrorForEmptyName*/);
        }

        /// <summary>
        ///     Handler for the Function element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at EntityType element </param>
        private void HandleFunctionElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var function = new Function(this);

            function.Parse(reader);

            Functions.Add(function);
        }

        private void HandleModelFunctionElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var function = new ModelFunction(this);

            function.Parse(reader);

            Functions.Add(function);
        }

        /// <summary>
        ///     Handler for the Association element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at Association element </param>
        private void HandleAssociationElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var relationship = new Relationship(this, RelationshipKind.Association);

            relationship.Parse(reader);

            TryAddType(relationship, true /*doNotAddErrorForEmptyName*/);
        }

        /// <summary>
        ///     Handler for the InlineType element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at InlineType element </param>
        private void HandleInlineTypeElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var complexType = new SchemaComplexType(this);

            complexType.Parse(reader);

            TryAddType(complexType, true /*doNotAddErrorForEmptyName*/);
        }

        /// <summary>
        ///     Handler for the EntityContainer element
        /// </summary>
        /// <param name="reader"> xml reader currently positioned at EntityContainer element </param>
        private void HandleEntityContainerTypeElement(XmlReader reader)
        {
            Debug.Assert(reader != null);

            var type = new EntityContainer(this);
            type.Parse(reader);
            TryAddContainer(type, true /*doNotAddErrorForEmptyName*/);
        }

        /// <summary>
        ///     reset the error collection
        /// </summary>
        /// <returns> old error list </returns>
        private List<EdmSchemaError> ResetErrors()
        {
            var errors = _errors;
            _errors = new List<EdmSchemaError>();

            return errors;
        }

        protected void TryAddType(SchemaType schemaType, bool doNotAddErrorForEmptyName)
        {
            SchemaManager.SchemaTypes.Add(
                schemaType, doNotAddErrorForEmptyName,
                Strings.TypeNameAlreadyDefinedDuplicate);
            SchemaTypes.Add(schemaType);
        }

        protected void TryAddContainer(SchemaType schemaType, bool doNotAddErrorForEmptyName)
        {
            SchemaManager.SchemaTypes.Add(
                schemaType, doNotAddErrorForEmptyName,
                Strings.EntityContainerAlreadyExists);
            SchemaTypes.Add(schemaType);
        }

        protected void AddFunctionType(Function function)
        {
            var space = DataModel == SchemaDataModelOption.EntityDataModel ? "Conceptual" : "Storage";

            if (SchemaVersion >= XmlConstants.EdmVersionForV2
                && SchemaManager.SchemaTypes.ContainsKey(function.FQName))
            {
                function.AddError(
                    ErrorCode.AlreadyDefined, EdmSchemaErrorSeverity.Error,
                    Strings.AmbiguousFunctionAndType(function.FQName, space));
            }
            else
            {
                var error = SchemaManager.SchemaTypes.TryAdd(function);
                Debug.Assert(error != AddErrorKind.MissingNameError, "Function identity can never be null while adding global functions");

                if (error != AddErrorKind.Succeeded)
                {
                    function.AddError(
                        ErrorCode.AlreadyDefined, EdmSchemaErrorSeverity.Error,
                        Strings.AmbiguousFunctionOverload(function.FQName, space));
                }
                else
                {
                    SchemaTypes.Add(function);
                }
            }
        }

        #endregion

        #region Private Properties

        #endregion

        private static class SomSchemaSetHelper
        {
            private static readonly Memoizer<SchemaDataModelOption, XmlSchemaSet> _cachedSchemaSets =
                new Memoizer<SchemaDataModelOption, XmlSchemaSet>(ComputeSchemaSet, EqualityComparer<SchemaDataModelOption>.Default);

            internal static List<string> GetPrimarySchemaNamespaces(SchemaDataModelOption dataModel)
            {
                var namespaces = new List<string>();
                if (dataModel == SchemaDataModelOption.EntityDataModel)
                {
                    namespaces.Add(XmlConstants.ModelNamespace_1);
                    namespaces.Add(XmlConstants.ModelNamespace_1_1);
                    namespaces.Add(XmlConstants.ModelNamespace_2);
                    namespaces.Add(XmlConstants.ModelNamespace_3);
                }
                else if (dataModel == SchemaDataModelOption.ProviderDataModel)
                {
                    namespaces.Add(XmlConstants.TargetNamespace_1);
                    namespaces.Add(XmlConstants.TargetNamespace_2);
                    namespaces.Add(XmlConstants.TargetNamespace_3);
                }
                else
                {
                    Debug.Assert(dataModel == SchemaDataModelOption.ProviderManifestModel, "Unknown SchemaDataModelOption did you add one?");
                    namespaces.Add(XmlConstants.ProviderManifestNamespace);
                }
                return namespaces;
            }

            internal static XmlSchemaSet GetSchemaSet(SchemaDataModelOption dataModel)
            {
                return _cachedSchemaSets.Evaluate(dataModel);
            }

            private static XmlSchemaSet ComputeSchemaSet(SchemaDataModelOption dataModel)
            {
                var namespaceNames = GetPrimarySchemaNamespaces(dataModel);
                Debug.Assert(namespaceNames.Count > 0, "Unknown Datamodel");

                var schemaSet = new XmlSchemaSet();
                // remove the default XmlResolver which will look on 
                // disk for the referenced schemas that we already provided
                schemaSet.XmlResolver = null;
                var schemaResourceMap = XmlSchemaResource.GetMetadataSchemaResourceMap(XmlConstants.SchemaVersionLatest);
                var schemasAlreadyAdded = new HashSet<string>();
                foreach (var namespaceName in namespaceNames)
                {
                    Debug.Assert(schemaResourceMap.ContainsKey(namespaceName), "the namespace name is not one we have a schema set for");
                    var schemaResource = schemaResourceMap[namespaceName];
                    AddXmlSchemaToSet(schemaSet, schemaResource, schemasAlreadyAdded);
                }
                schemaSet.Compile();

                return schemaSet;
            }

            private static void AddXmlSchemaToSet(
                XmlSchemaSet schemaSet, XmlSchemaResource schemaResource, HashSet<string> schemasAlreadyAdded)
            {
                // loop through the children to do a depth first load
                foreach (var import in schemaResource.ImportedSchemas)
                {
                    AddXmlSchemaToSet(schemaSet, import, schemasAlreadyAdded);
                }

                if (!schemasAlreadyAdded.Contains(schemaResource.NamespaceUri))
                {
                    var xsdStream = GetResourceStream(schemaResource.ResourceName);
                    var schema = XmlSchema.Read(xsdStream, null);
                    schemaSet.Add(schema);
                    schemasAlreadyAdded.Add(schemaResource.NamespaceUri);
                }
            }

            private static Stream GetResourceStream(string resourceName)
            {
                Debug.Assert(resourceName != null, "resourceName cannot be null");

                Stream resourceStream = null;
                var executingAssembly = Assembly.GetExecutingAssembly();
                if (executingAssembly != null)
                {
                    resourceStream = executingAssembly.GetManifestResourceStream(resourceName);
                }

                Debug.Assert(
                    resourceStream != null,
                    string.Format(CultureInfo.CurrentCulture, "Unable to load the resource {0} from assembly resources.", resourceName));

                return resourceStream;
            }
        }
    }
}
