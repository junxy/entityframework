﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityModel.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;

    internal struct XmlSchemaResource
    {
        private static readonly XmlSchemaResource[] _emptyImportList = new XmlSchemaResource[0];

        public XmlSchemaResource(string namespaceUri, string resourceName, XmlSchemaResource[] importedSchemas)
        {
            Debug.Assert(!string.IsNullOrEmpty(namespaceUri), "namespaceUri is null or empty");
            Debug.Assert(!string.IsNullOrEmpty(resourceName), "resourceName is null or empty");
            Debug.Assert(importedSchemas != null, "importedSchemas is null");
            NamespaceUri = namespaceUri;
            ResourceName = resourceName;
            ImportedSchemas = importedSchemas;
        }

        public XmlSchemaResource(string namespaceUri, string resourceName)
        {
            Debug.Assert(!string.IsNullOrEmpty(namespaceUri), "namespaceUri is null or empty");
            Debug.Assert(!string.IsNullOrEmpty(resourceName), "resourceName is null or empty");
            NamespaceUri = namespaceUri;
            ResourceName = resourceName;
            ImportedSchemas = _emptyImportList;
        }

        internal string NamespaceUri;
        internal string ResourceName;
        internal XmlSchemaResource[] ImportedSchemas;

        /// <summary>
        ///     Builds a dictionary from XmlNamespace to XmlSchemaResource of both C and S space schemas
        /// </summary>
        /// <returns> The built XmlNamespace to XmlSchemaResource dictionary. </returns>
        internal static Dictionary<string, XmlSchemaResource> GetMetadataSchemaResourceMap(double schemaVersion)
        {
            var schemaResourceMap = new Dictionary<string, XmlSchemaResource>(StringComparer.Ordinal);
            AddEdmSchemaResourceMapEntries(schemaResourceMap, schemaVersion);
            AddStoreSchemaResourceMapEntries(schemaResourceMap, schemaVersion);
            return schemaResourceMap;
        }

        /// <summary>
        ///     Adds Store schema resource entries to the given XmlNamespace to XmlSchemaResoure map
        /// </summary>
        /// <param name="schemaResourceMap"> The XmlNamespace to XmlSchemaResource map to add entries to. </param>
        internal static void AddStoreSchemaResourceMapEntries(Dictionary<string, XmlSchemaResource> schemaResourceMap, double schemaVersion)
        {
            XmlSchemaResource[] ssdlImports =
                {
                    new XmlSchemaResource(
                        XmlConstants.EntityStoreSchemaGeneratorNamespace,
                        "System.Data.Resources.EntityStoreSchemaGenerator.xsd")
                };

            var ssdlSchema = new XmlSchemaResource(XmlConstants.TargetNamespace_1, "System.Data.Resources.SSDLSchema.xsd", ssdlImports);
            schemaResourceMap.Add(ssdlSchema.NamespaceUri, ssdlSchema);

            if (schemaVersion >= XmlConstants.StoreVersionForV2)
            {
                var ssdlSchema2 = new XmlSchemaResource(
                    XmlConstants.TargetNamespace_2, "System.Data.Resources.SSDLSchema_2.xsd", ssdlImports);
                schemaResourceMap.Add(ssdlSchema2.NamespaceUri, ssdlSchema2);
            }

            if (schemaVersion >= XmlConstants.StoreVersionForV3)
            {
                Debug.Assert(XmlConstants.SchemaVersionLatest == XmlConstants.StoreVersionForV3, "Did you add a new schema version");

                var ssdlSchema3 = new XmlSchemaResource(
                    XmlConstants.TargetNamespace_3, "System.Data.Resources.SSDLSchema_3.xsd", ssdlImports);
                schemaResourceMap.Add(ssdlSchema3.NamespaceUri, ssdlSchema3);
            }

            var providerManifest = new XmlSchemaResource(
                XmlConstants.ProviderManifestNamespace, "System.Data.Resources.ProviderServices.ProviderManifest.xsd");
            schemaResourceMap.Add(providerManifest.NamespaceUri, providerManifest);
        }

        /// <summary>
        ///     Adds Mapping schema resource entries to the given XmlNamespace to XmlSchemaResoure map
        /// </summary>
        /// <param name="schemaResourceMap"> The XmlNamespace to XmlSchemaResource map to add entries to. </param>
        internal static void AddMappingSchemaResourceMapEntries(
            Dictionary<string, XmlSchemaResource> schemaResourceMap, double schemaVersion)
        {
            var msl1 = new XmlSchemaResource(StorageMslConstructs.NamespaceUriV1, StorageMslConstructs.ResourceXsdNameV1);
            schemaResourceMap.Add(msl1.NamespaceUri, msl1);

            if (schemaVersion >= XmlConstants.EdmVersionForV2)
            {
                var msl2 = new XmlSchemaResource(StorageMslConstructs.NamespaceUriV2, StorageMslConstructs.ResourceXsdNameV2);
                schemaResourceMap.Add(msl2.NamespaceUri, msl2);
            }

            if (schemaVersion >= XmlConstants.EdmVersionForV3)
            {
                Debug.Assert(XmlConstants.SchemaVersionLatest == XmlConstants.EdmVersionForV3, "Did you add a new schema version");
                var msl3 = new XmlSchemaResource(StorageMslConstructs.NamespaceUriV3, StorageMslConstructs.ResourceXsdNameV3);
                schemaResourceMap.Add(msl3.NamespaceUri, msl3);
            }
        }

        /// <summary>
        ///     Adds Edm schema resource entries to the given XmlNamespace to XmlSchemaResoure map,
        ///     when calling from SomSchemaSetHelper.ComputeSchemaSet(), all the imported xsd will be included
        /// </summary>
        /// <param name="schemaResourceMap"> The XmlNamespace to XmlSchemaResource map to add entries to. </param>
        internal static void AddEdmSchemaResourceMapEntries(Dictionary<string, XmlSchemaResource> schemaResourceMap, double schemaVersion)
        {
            XmlSchemaResource[] csdlImports =
                {
                    new XmlSchemaResource(
                        XmlConstants.CodeGenerationSchemaNamespace,
                        "System.Data.Resources.CodeGenerationSchema.xsd")
                };

            XmlSchemaResource[] csdl2Imports =
                {
                    new XmlSchemaResource(
                        XmlConstants.CodeGenerationSchemaNamespace,
                        "System.Data.Resources.CodeGenerationSchema.xsd"),
                    new XmlSchemaResource(
                        XmlConstants.AnnotationNamespace, "System.Data.Resources.AnnotationSchema.xsd")
                };

            XmlSchemaResource[] csdl3Imports =
                {
                    new XmlSchemaResource(
                        XmlConstants.CodeGenerationSchemaNamespace,
                        "System.Data.Resources.CodeGenerationSchema.xsd"),
                    new XmlSchemaResource(
                        XmlConstants.AnnotationNamespace, "System.Data.Resources.AnnotationSchema.xsd")
                };

            var csdlSchema_1 = new XmlSchemaResource(XmlConstants.ModelNamespace_1, "System.Data.Resources.CSDLSchema_1.xsd", csdlImports);
            schemaResourceMap.Add(csdlSchema_1.NamespaceUri, csdlSchema_1);

            var csdlSchema_1_1 = new XmlSchemaResource(
                XmlConstants.ModelNamespace_1_1, "System.Data.Resources.CSDLSchema_1_1.xsd", csdlImports);
            schemaResourceMap.Add(csdlSchema_1_1.NamespaceUri, csdlSchema_1_1);

            if (schemaVersion >= XmlConstants.EdmVersionForV2)
            {
                var csdlSchema_2 = new XmlSchemaResource(
                    XmlConstants.ModelNamespace_2, "System.Data.Resources.CSDLSchema_2.xsd", csdl2Imports);
                schemaResourceMap.Add(csdlSchema_2.NamespaceUri, csdlSchema_2);
            }

            if (schemaVersion >= XmlConstants.EdmVersionForV3)
            {
                Debug.Assert(XmlConstants.SchemaVersionLatest == XmlConstants.EdmVersionForV3, "Did you add a new schema version");

                var csdlSchema_3 = new XmlSchemaResource(
                    XmlConstants.ModelNamespace_3, "System.Data.Resources.CSDLSchema_3.xsd", csdl3Imports);
                schemaResourceMap.Add(csdlSchema_3.NamespaceUri, csdlSchema_3);
            }
        }
    }
}
