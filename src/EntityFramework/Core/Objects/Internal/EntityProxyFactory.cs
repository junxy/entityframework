﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Xml.Serialization;

    /// <summary>
    ///     Factory for creating proxy classes that can intercept calls to a class' members.
    /// </summary>
    internal class EntityProxyFactory
    {
        private const string ProxyTypeNameFormat = "System.Data.Entity.DynamicProxies.{0}_{1}";
        internal const string ResetFKSetterFlagFieldName = "_resetFKSetterFlag";
        internal const string CompareByteArraysFieldName = "_compareByteArrays";

        /// <summary>
        ///     A hook such that test code can change the AssemblyBuilderAccess of the
        ///     proxy assembly through reflection into the EntityProxyFactory.
        /// </summary>
        private static AssemblyBuilderAccess s_ProxyAssemblyBuilderAccess = AssemblyBuilderAccess.Run;

        /// <summary>
        ///     Dictionary of proxy class type information, keyed by the pair of the CLR type and EntityType CSpaceName of the type being proxied.
        ///     A null value for a particular EntityType name key records the fact that 
        ///     no proxy Type could be created for the specified type.
        /// </summary>
        private static readonly Dictionary<Tuple<Type, string>, EntityProxyTypeInfo> _proxyNameMap =
            new Dictionary<Tuple<Type, string>, EntityProxyTypeInfo>();

        /// <summary>
        ///     Dictionary of proxy class type information, keyed by the proxy type
        /// </summary>
        private static readonly Dictionary<Type, EntityProxyTypeInfo> _proxyTypeMap = new Dictionary<Type, EntityProxyTypeInfo>();

        private static readonly Dictionary<Assembly, ModuleBuilder> _moduleBuilders = new Dictionary<Assembly, ModuleBuilder>();
        private static readonly ReaderWriterLockSlim _typeMapLock = new ReaderWriterLockSlim();

        /// <summary>
        ///     The runtime assembly of the proxy types.
        ///     This is not the same as the AssemblyBuilder used to create proxy types.
        /// </summary>
        private static readonly HashSet<Assembly> _proxyRuntimeAssemblies = new HashSet<Assembly>();

        private static ModuleBuilder GetDynamicModule(EntityType ospaceEntityType)
        {
            var assembly = ospaceEntityType.ClrType.Assembly;
            ModuleBuilder moduleBuilder;
            if (!_moduleBuilders.TryGetValue(assembly, out moduleBuilder))
            {
                var assemblyName =
                    new AssemblyName(String.Format(CultureInfo.InvariantCulture, "EntityFrameworkDynamicProxies-{0}", assembly.FullName));
                assemblyName.Version = new Version(1, 0, 0, 0);

                // Mark assembly as security transparent, meaning it cannot cause an elevation of privilege.
                // This also means the assembly cannot satisfy a link demand. Instead link demands become full demands.
                var securityTransparentAttributeConstructor = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes);

                // Mark assembly with [SecurityRules(SecurityRuleSet.Level1)]. In memory, the assembly will inherit
                // this automatically from SDE, but when persisted it needs this attribute to be considered Level1.
                var securityRulesAttributeConstructor = typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) });

                var attributeBuilders = new[]
                                            {
                                                new CustomAttributeBuilder(securityTransparentAttributeConstructor, new object[0]),
                                                new CustomAttributeBuilder(
                                                    securityRulesAttributeConstructor, new object[1] { SecurityRuleSet.Level1 })
                                            };

                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    assemblyName, s_ProxyAssemblyBuilderAccess, attributeBuilders);

                if (s_ProxyAssemblyBuilderAccess == AssemblyBuilderAccess.RunAndSave)
                {
                    // Make the module persistable if the AssemblyBuilderAccess is changed to be RunAndSave.
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("EntityProxyModule", "EntityProxyModule.dll");
                }
                else
                {
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("EntityProxyModule");
                }

                _moduleBuilders.Add(assembly, moduleBuilder);
            }
            return moduleBuilder;
        }

        internal static bool TryGetProxyType(Type clrType, string entityTypeName, out EntityProxyTypeInfo proxyTypeInfo)
        {
            _typeMapLock.EnterReadLock();
            try
            {
                return _proxyNameMap.TryGetValue(new Tuple<Type, string>(clrType, entityTypeName), out proxyTypeInfo);
            }
            finally
            {
                _typeMapLock.ExitReadLock();
            }
        }

        internal static bool TryGetProxyType(Type proxyType, out EntityProxyTypeInfo proxyTypeInfo)
        {
            _typeMapLock.EnterReadLock();
            try
            {
                return _proxyTypeMap.TryGetValue(proxyType, out proxyTypeInfo);
            }
            finally
            {
                _typeMapLock.ExitReadLock();
            }
        }

        internal static bool TryGetProxyWrapper(object instance, out IEntityWrapper wrapper)
        {
            Debug.Assert(instance != null, "the instance should not be null");
            wrapper = null;
            EntityProxyTypeInfo proxyTypeInfo;
            if (IsProxyType(instance.GetType())
                &&
                TryGetProxyType(instance.GetType(), out proxyTypeInfo))
            {
                wrapper = proxyTypeInfo.GetEntityWrapper(instance);
            }
            return wrapper != null;
        }

        /// <summary>
        ///     Return proxy type information for the specified O-Space EntityType.
        /// </summary>
        /// <param name="ospaceEntityType"> EntityType in O-Space that represents the CLR type to be proxied. Must not be null. </param>
        /// <returns> A non-null EntityProxyTypeInfo instance that contains information about the type of proxy for the specified O-Space EntityType; or null if no proxy can be created for the specified type. </returns>
        internal static EntityProxyTypeInfo GetProxyType(ClrEntityType ospaceEntityType)
        {
            Debug.Assert(ospaceEntityType != null, "ospaceEntityType must be non-null");
            Debug.Assert(ospaceEntityType.DataSpace == DataSpace.OSpace, "ospaceEntityType.DataSpace must be OSpace");

            EntityProxyTypeInfo proxyTypeInfo = null;

            // Check if an entry for the proxy type already exists.
            if (TryGetProxyType(ospaceEntityType.ClrType, ospaceEntityType.CSpaceTypeName, out proxyTypeInfo))
            {
                if (proxyTypeInfo != null)
                {
                    proxyTypeInfo.ValidateType(ospaceEntityType);
                }
                return proxyTypeInfo;
            }

            // No entry found, may need to create one.
            // Acquire an upgradeable read lock so that:
            // 1. Other readers aren't blocked while the second existence check is performed.
            // 2. Other threads that may have also detected the absence of an entry block while the first thread handles proxy type creation.

            _typeMapLock.EnterUpgradeableReadLock();
            try
            {
                return TryCreateProxyType(ospaceEntityType);
            }
            finally
            {
                _typeMapLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     A mechanism to lookup AssociationType metadata for proxies for a given entity and association information
        /// </summary>
        /// <param name="wrappedEntity"> The entity instance used to lookup the proxy type </param>
        /// <param name="relationshipName"> The name of the relationship (FullName or Name) </param>
        /// <param name="targetRoleName"> Target role of the relationship </param>
        /// <param name="associationType"> The AssociationType for that property </param>
        /// <returns> True if an AssociationType is found in proxy metadata, false otherwise </returns>
        internal static bool TryGetAssociationTypeFromProxyInfo(
            IEntityWrapper wrappedEntity, string relationshipName, string targetRoleName, out AssociationType associationType)
        {
            EntityProxyTypeInfo proxyInfo = null;
            associationType = null;
            return (TryGetProxyType(wrappedEntity.Entity.GetType(), out proxyInfo) && proxyInfo != null &&
                    proxyInfo.TryGetNavigationPropertyAssociationType(relationshipName, targetRoleName, out associationType));
        }

        /// <summary>
        ///     Enumerate list of supplied O-Space EntityTypes, 
        ///     and generate a proxy type for each EntityType (if possible for the particular type).
        /// </summary>
        /// <param name="ospaceEntityType"> Enumeration of O-Space EntityType objects. Must not be null. In addition, the elements of the enumeration must not be null. </param>
        internal static void TryCreateProxyTypes(IEnumerable<EntityType> ospaceEntityTypes)
        {
            Debug.Assert(ospaceEntityTypes != null, "ospaceEntityTypes must be non-null");

            // Acquire an upgradeable read lock for the duration of the enumeration so that:
            // 1. Other readers aren't blocked while existence checks are performed.
            // 2. Other threads that may have detected the absence of an entry block while the first thread handles proxy type creation.

            _typeMapLock.EnterUpgradeableReadLock();
            try
            {
                foreach (var ospaceEntityType in ospaceEntityTypes)
                {
                    Debug.Assert(ospaceEntityType != null, "Null EntityType element reference present in enumeration.");
                    TryCreateProxyType(ospaceEntityType);
                }
            }
            finally
            {
                _typeMapLock.ExitUpgradeableReadLock();
            }
        }

        private static EntityProxyTypeInfo TryCreateProxyType(EntityType ospaceEntityType)
        {
            Debug.Assert(
                _typeMapLock.IsUpgradeableReadLockHeld,
                "EntityProxyTypeInfo.TryCreateProxyType method was called without first acquiring an upgradeable read lock from _typeMapLock.");

            EntityProxyTypeInfo proxyTypeInfo;
            var clrEntityType = (ClrEntityType)ospaceEntityType;

            var proxyIdentiy = new Tuple<Type, string>(clrEntityType.ClrType, clrEntityType.HashedDescription);

            if (!_proxyNameMap.TryGetValue(proxyIdentiy, out proxyTypeInfo)
                && CanProxyType(ospaceEntityType))
            {
                var moduleBuilder = GetDynamicModule(ospaceEntityType);
                proxyTypeInfo = BuildType(moduleBuilder, clrEntityType);

                _typeMapLock.EnterWriteLock();
                try
                {
                    _proxyNameMap[proxyIdentiy] = proxyTypeInfo;
                    if (proxyTypeInfo != null)
                    {
                        // If there is a proxy type, create the reverse lookup
                        _proxyTypeMap[proxyTypeInfo.ProxyType] = proxyTypeInfo;
                    }
                }
                finally
                {
                    _typeMapLock.ExitWriteLock();
                }
            }

            return proxyTypeInfo;
        }

        /// <summary>
        ///     Determine if the specified type represents a known proxy type.
        /// </summary>
        /// <param name="type"> The Type to be examined. </param>
        /// <returns> True if the type is a known proxy type; otherwise false. </returns>
        internal static bool IsProxyType(Type type)
        {
            Debug.Assert(type != null, "type is null, was this intended?");
            return type != null && _proxyRuntimeAssemblies.Contains(type.Assembly);
        }

        /// <summary>
        ///     Return an enumerable of the current set of CLR proxy types.
        /// </summary>
        /// <returns> Enumerable of the current set of CLR proxy types. This value will never be null. </returns>
        /// <remarks>
        ///     The enumerable is based on a shapshot of the current list of types.
        /// </remarks>
        internal static IEnumerable<Type> GetKnownProxyTypes()
        {
            _typeMapLock.EnterReadLock();
            try
            {
                var proxyTypes = from info in _proxyNameMap.Values
                                 where info != null
                                 select info.ProxyType;
                return proxyTypes.ToArray();
            }
            finally
            {
                _typeMapLock.ExitReadLock();
            }
        }

        public virtual Func<object, object> CreateBaseGetter(Type declaringType, PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null, "Null propertyInfo");

            var Object_Parameter = Expression.Parameter(typeof(object), "instance");
            var nonProxyGetter = Expression.Lambda<Func<object, object>>(
                Expression.PropertyOrField(
                    Expression.Convert(Object_Parameter, declaringType),
                    propertyInfo.Name),
                Object_Parameter).Compile();

            var propertyName = propertyInfo.Name;
            return (entity) =>
                       {
                           var type = entity.GetType();
                           if (IsProxyType(type))
                           {
                               object value;
                               if (TryGetBasePropertyValue(type, propertyName, entity, out value))
                               {
                                   return value;
                               }
                           }
                           return nonProxyGetter(entity);
                       };
        }

        private static bool TryGetBasePropertyValue(Type proxyType, string propertyName, object entity, out object value)
        {
            EntityProxyTypeInfo typeInfo;
            value = null;
            if (TryGetProxyType(proxyType, out typeInfo)
                && typeInfo.ContainsBaseGetter(propertyName))
            {
                value = typeInfo.BaseGetter(entity, propertyName);
                return true;
            }
            return false;
        }

        public virtual Action<object, object> CreateBaseSetter(Type declaringType, PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null, "Null propertyInfo");

            var nonProxySetter = LightweightCodeGenerator.CreateNavigationPropertySetter(declaringType, propertyInfo);

            var propertyName = propertyInfo.Name;
            return (entity, value) =>
                       {
                           var type = entity.GetType();
                           if (IsProxyType(type))
                           {
                               if (TrySetBasePropertyValue(type, propertyName, entity, value))
                               {
                                   return;
                               }
                           }
                           nonProxySetter(entity, value);
                       };
        }

        private static bool TrySetBasePropertyValue(Type proxyType, string propertyName, object entity, object value)
        {
            EntityProxyTypeInfo typeInfo;
            if (TryGetProxyType(proxyType, out typeInfo)
                && typeInfo.ContainsBaseSetter(propertyName))
            {
                typeInfo.BaseSetter(entity, propertyName, value);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Build a CLR proxy type for the supplied EntityType.
        /// </summary>
        /// <param name="ospaceEntityType"> EntityType in O-Space that represents the CLR type to be proxied. </param>
        /// <returns> EntityProxyTypeInfo object that contains the constructed proxy type, along with any behaviors associated with that type; or null if a proxy type cannot be constructed for the specified EntityType. </returns>
        private static EntityProxyTypeInfo BuildType(ModuleBuilder moduleBuilder, ClrEntityType ospaceEntityType)
        {
            Debug.Assert(
                _typeMapLock.IsUpgradeableReadLockHeld,
                "EntityProxyTypeInfo.BuildType method was called without first acquiring an upgradeable read lock from _typeMapLock.");

            EntityProxyTypeInfo proxyTypeInfo;

            var proxyTypeBuilder = new ProxyTypeBuilder(ospaceEntityType);
            var proxyType = proxyTypeBuilder.CreateType(moduleBuilder);

            if (proxyType != null)
            {
                // Set the runtime assembly of the proxy types if it hasn't already been set.
                // This is used by the IsProxyType method.
                var typeAssembly = proxyType.Assembly;
                if (!_proxyRuntimeAssemblies.Contains(typeAssembly))
                {
                    _proxyRuntimeAssemblies.Add(typeAssembly);
                    AddAssemblyToResolveList(typeAssembly);
                }

                proxyTypeInfo = new EntityProxyTypeInfo(
                    proxyType, ospaceEntityType,
                    proxyTypeBuilder.CreateInitalizeCollectionMethod(proxyType),
                    proxyTypeBuilder.BaseGetters, proxyTypeBuilder.BaseSetters);

                foreach (var member in proxyTypeBuilder.LazyLoadMembers)
                {
                    InterceptMember(member, proxyType, proxyTypeInfo);
                }

                SetResetFKSetterFlagDelegate(proxyType, proxyTypeInfo);
                SetCompareByteArraysDelegate(proxyType);
            }
            else
            {
                proxyTypeInfo = null;
            }

            return proxyTypeInfo;
        }

        /// <summary>
        ///     In order for deserialization of proxy objects to succeed in this AppDomain,
        ///     an assembly resolve handler must be added to the AppDomain to resolve the dynamic assembly,
        ///     since it is not present in a location discoverable by fusion.
        /// </summary>
        /// <param name="assembly"> Proxy assembly to be resolved. </param>
        [SecuritySafeCritical]
        private static void AddAssemblyToResolveList(Assembly assembly)
        {
            if (_proxyRuntimeAssemblies.Contains(assembly)) // If the assembly is not a known proxy assembly, ignore it.
            {
                ResolveEventHandler resolveHandler = (sender, args) => args.Name == assembly.FullName ? assembly : null;
                AppDomain.CurrentDomain.AssemblyResolve += resolveHandler;
            }
        }

        /// <summary>
        ///     Construct an interception delegate for the specified proxy member.
        /// </summary>
        /// <param name="member"> EdmMember that specifies the member to be intercepted. </param>
        /// <param name="proxyType"> Type of the proxy. </param>
        /// <param name="lazyLoadBehavior"> LazyLoadBehavior object that supplies the behavior to load related ends. </param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void InterceptMember(EdmMember member, Type proxyType, EntityProxyTypeInfo proxyTypeInfo)
        {
            var property = EntityUtil.GetTopProperty(proxyType, member.Name);
            Debug.Assert(
                property != null,
                String.Format(
                    CultureInfo.CurrentCulture, "Expected property {0} to be defined on proxy type {1}", member.Name, proxyType.FullName));

            var interceptorField = proxyType.GetField(
                LazyLoadImplementor.GetInterceptorFieldName(member.Name),
                BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(
                interceptorField != null,
                String.Format(
                    CultureInfo.CurrentCulture, "Expected interceptor field for property {0} to be defined on proxy type {1}", member.Name,
                    proxyType.FullName));

            var interceptorDelegate =
                typeof(LazyLoadBehavior).GetMethod("GetInterceptorDelegate", BindingFlags.NonPublic | BindingFlags.Static).
                    MakeGenericMethod(proxyType, property.PropertyType).
                    Invoke(null, new object[] { member, proxyTypeInfo.EntityWrapperDelegate }) as Delegate;

            AssignInterceptionDelegate(interceptorDelegate, interceptorField);
        }

        /// <summary>
        ///     Set the interceptor on a proxy member.
        /// </summary>
        /// <param name="interceptorDelegate"> Delegate to be set </param>
        /// <param name="interceptorField"> Field define on the proxy type to store the reference to the interception delegate. </param>
        [SuppressMessage("Microsoft.Security", "CA2128")]
        [SecuritySafeCritical]
        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void AssignInterceptionDelegate(Delegate interceptorDelegate, FieldInfo interceptorField)
        {
            interceptorField.SetValue(null, interceptorDelegate);
        }

        /// <summary>
        ///     Sets a delegate onto the _resetFKSetterFlag field such that it can be executed to make
        ///     a call into the state manager to reset the InFKSetter flag.
        /// </summary>
        private static void SetResetFKSetterFlagDelegate(Type proxyType, EntityProxyTypeInfo proxyTypeInfo)
        {
            var resetFKSetterFlagField = proxyType.GetField(
                ResetFKSetterFlagFieldName, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(resetFKSetterFlagField != null, "Expected resetFKSetterFlagField to be defined on the proxy type.");

            var resetFKSetterFlagDelegate = GetResetFKSetterFlagDelegate(proxyTypeInfo.EntityWrapperDelegate);

            AssignInterceptionDelegate(resetFKSetterFlagDelegate, resetFKSetterFlagField);
        }

        /// <summary>
        ///     Returns the delegate that takes a proxy instance and uses it to reset the InFKSetter flag maintained
        ///     by the state manager of the context associated with the proxy instance.
        /// </summary>
        private static Action<object> GetResetFKSetterFlagDelegate(Func<object, object> getEntityWrapperDelegate)
        {
            return (proxy) =>
                       {
                           Debug.Assert(getEntityWrapperDelegate != null, "entityWrapperDelegate must not be null");

                           ResetFKSetterFlag(getEntityWrapperDelegate(proxy));
                       };
        }

        /// <summary>
        ///     Called in the finally clause of each overridden property setter to ensure that the flag
        ///     indicating that we are in an FK setter is cleared.  Note that the wrapped entity is passed as
        ///     an obejct becayse IEntityWrapper is an internal type and is therefore not accessable to
        ///     the proxy type.  Once we're in the framework it is cast back to an IEntityWrapper.
        /// </summary>
        private static void ResetFKSetterFlag(object wrappedEntityAsObject)
        {
            Debug.Assert(
                wrappedEntityAsObject == null || wrappedEntityAsObject is IEntityWrapper, "wrappedEntityAsObject must be an IEntityWrapper");
            var wrappedEntity = (IEntityWrapper)wrappedEntityAsObject; // We want an exception if the cast fails.
            if (wrappedEntity != null
                && wrappedEntity.Context != null)
            {
                wrappedEntity.Context.ObjectStateManager.EntityInvokingFKSetter = null;
            }
        }

        /// <summary>
        ///     Sets a delegate onto the _compareByteArrays field such that it can be executed to check
        ///     whether two byte arrays are the same by value comparison.
        /// </summary>
        private static void SetCompareByteArraysDelegate(Type proxyType)
        {
            var compareByteArraysField = proxyType.GetField(
                CompareByteArraysFieldName, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(compareByteArraysField != null, "Expected compareByteArraysField to be defined on the proxy type.");

            AssignInterceptionDelegate(new Func<object, object, bool>(ByValueEqualityComparer.Default.Equals), compareByteArraysField);
        }

        /// <summary>
        ///     Return boolean that specifies if the specified type can be proxied.
        /// </summary>
        /// <param name="ospaceEntityType"> O-space EntityType </param>
        /// <returns> True if the class is not abstract or sealed, does not implement IEntityWithRelationships, and has a public or protected default constructor; otherwise false. </returns>
        /// <remarks>
        ///     While it is technically possible to derive from an abstract type
        ///     in order to create a proxy, we avoid this so that the proxy type 
        ///     has the same "concreteness" of the type being proxied.
        ///     The check for IEntityWithRelationships ensures that codegen'ed
        ///     entities that derive from EntityObject as well as properly
        ///     constructed IPOCO entities will not be proxied.
        /// </remarks>
        private static bool CanProxyType(EntityType ospaceEntityType)
        {
            var access = ospaceEntityType.ClrType.Attributes & TypeAttributes.VisibilityMask;

            var ctor =
                ospaceEntityType.ClrType.GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null,
                    Type.EmptyTypes, null);
            var accessableCtor = ctor != null && (((ctor.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public) ||
                                                  ((ctor.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family) ||
                                                  ((ctor.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem));

            return (!(ospaceEntityType.Abstract ||
                      ospaceEntityType.ClrType.IsSealed ||
                      typeof(IEntityWithRelationships).IsAssignableFrom(ospaceEntityType.ClrType) ||
                      !accessableCtor) &&
                    access == TypeAttributes.Public);
        }

        private static bool CanProxyMethod(MethodInfo method)
        {
            var result = false;

            if (method != null)
            {
                var access = method.Attributes & MethodAttributes.MemberAccessMask;
                result = method.IsVirtual &&
                         !method.IsFinal &&
                         (access == MethodAttributes.Public ||
                          access == MethodAttributes.Family ||
                          access == MethodAttributes.FamORAssem);
            }

            return result;
        }

        internal static bool CanProxyGetter(PropertyInfo clrProperty)
        {
            Debug.Assert(clrProperty != null, "clrProperty should have a value");
            return CanProxyMethod(clrProperty.GetGetMethod(true));
        }

        internal static bool CanProxySetter(PropertyInfo clrProperty)
        {
            Debug.Assert(clrProperty != null, "clrProperty should have a value");
            return CanProxyMethod(clrProperty.GetSetMethod(true));
        }

        private class ProxyTypeBuilder
        {
            private TypeBuilder _typeBuilder;
            private readonly BaseProxyImplementor _baseImplementor;
            private readonly IPOCOImplementor _ipocoImplementor;
            private readonly LazyLoadImplementor _lazyLoadImplementor;
            private readonly DataContractImplementor _dataContractImplementor;
            private readonly SerializableImplementor _iserializableImplementor;
            private readonly ClrEntityType _ospaceEntityType;
            private ModuleBuilder _moduleBuilder;
            private readonly List<FieldBuilder> _serializedFields = new List<FieldBuilder>(3);

            public ProxyTypeBuilder(ClrEntityType ospaceEntityType)
            {
                _ospaceEntityType = ospaceEntityType;
                _baseImplementor = new BaseProxyImplementor();
                _ipocoImplementor = new IPOCOImplementor(ospaceEntityType);
                _lazyLoadImplementor = new LazyLoadImplementor(ospaceEntityType);
                _dataContractImplementor = new DataContractImplementor(ospaceEntityType);
                _iserializableImplementor = new SerializableImplementor(ospaceEntityType);
            }

            public Type BaseType
            {
                get { return _ospaceEntityType.ClrType; }
            }

            public DynamicMethod CreateInitalizeCollectionMethod(Type proxyType)
            {
                return _ipocoImplementor.CreateInitalizeCollectionMethod(proxyType);
            }

            public List<PropertyInfo> BaseGetters
            {
                get { return _baseImplementor.BaseGetters; }
            }

            public List<PropertyInfo> BaseSetters
            {
                get { return _baseImplementor.BaseSetters; }
            }

            public IEnumerable<EdmMember> LazyLoadMembers
            {
                get { return _lazyLoadImplementor.Members; }
            }

            public Type CreateType(ModuleBuilder moduleBuilder)
            {
                _moduleBuilder = moduleBuilder;
                var hadProxyProperties = false;

                if (_iserializableImplementor.TypeIsSuitable)
                {
                    foreach (var member in _ospaceEntityType.Members)
                    {
                        if (_ipocoImplementor.CanProxyMember(member)
                            ||
                            _lazyLoadImplementor.CanProxyMember(member))
                        {
                            var baseProperty = EntityUtil.GetTopProperty(BaseType, member.Name);
                            var propertyBuilder = TypeBuilder.DefineProperty(
                                member.Name, PropertyAttributes.None, baseProperty.PropertyType, Type.EmptyTypes);

                            if (!_ipocoImplementor.EmitMember(TypeBuilder, member, propertyBuilder, baseProperty, _baseImplementor))
                            {
                                EmitBaseSetter(TypeBuilder, propertyBuilder, baseProperty);
                            }
                            if (!_lazyLoadImplementor.EmitMember(TypeBuilder, member, propertyBuilder, baseProperty, _baseImplementor))
                            {
                                EmitBaseGetter(TypeBuilder, propertyBuilder, baseProperty);
                            }

                            hadProxyProperties = true;
                        }
                    }

                    if (_typeBuilder != null)
                    {
                        _baseImplementor.Implement(TypeBuilder);
                        _iserializableImplementor.Implement(TypeBuilder, _serializedFields);
                    }
                }

                return hadProxyProperties ? TypeBuilder.CreateType() : null;
            }

            private TypeBuilder TypeBuilder
            {
                get
                {
                    if (_typeBuilder == null)
                    {
                        var proxyTypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
                        if ((BaseType.Attributes & TypeAttributes.Serializable)
                            == TypeAttributes.Serializable)
                        {
                            proxyTypeAttributes |= TypeAttributes.Serializable;
                        }

                        // If the type as a long name, then use only the first part of it so that there is no chance that the generated
                        // name will be too long.  Note that the full name always gets used to compute the hash.
                        var baseName = BaseType.Name.Length <= 20 ? BaseType.Name : BaseType.Name.Substring(0, 20);
                        var proxyTypeName = String.Format(
                            CultureInfo.InvariantCulture, ProxyTypeNameFormat, baseName, _ospaceEntityType.HashedDescription);

                        _typeBuilder = _moduleBuilder.DefineType(proxyTypeName, proxyTypeAttributes, BaseType, _ipocoImplementor.Interfaces);
                        _typeBuilder.DefineDefaultConstructor(
                            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName
                            | MethodAttributes.SpecialName);

                        Action<FieldBuilder, bool> registerField = RegisterInstanceField;
                        _ipocoImplementor.Implement(_typeBuilder, registerField);
                        _lazyLoadImplementor.Implement(_typeBuilder, registerField);

                        // WCF data contract serialization is not compatible with types that implement ISerializable.
                        if (!_iserializableImplementor.TypeImplementsISerializable)
                        {
                            _dataContractImplementor.Implement(_typeBuilder);
                        }
                    }
                    return _typeBuilder;
                }
            }

            private static void EmitBaseGetter(TypeBuilder typeBuilder, PropertyBuilder propertyBuilder, PropertyInfo baseProperty)
            {
                if (CanProxyGetter(baseProperty))
                {
                    var baseGetter = baseProperty.GetGetMethod(true);
                    const MethodAttributes getterAttributes =
                        MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
                    var getterAccess = baseGetter.Attributes & MethodAttributes.MemberAccessMask;

                    // Define a property getter override in the proxy type
                    var getterBuilder = typeBuilder.DefineMethod(
                        "get_" + baseProperty.Name, getterAccess | getterAttributes, baseProperty.PropertyType, Type.EmptyTypes);
                    var gen = getterBuilder.GetILGenerator();

                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Call, baseGetter);
                    gen.Emit(OpCodes.Ret);

                    propertyBuilder.SetGetMethod(getterBuilder);
                }
            }

            private static void EmitBaseSetter(TypeBuilder typeBuilder, PropertyBuilder propertyBuilder, PropertyInfo baseProperty)
            {
                if (CanProxySetter(baseProperty))
                {
                    var baseSetter = baseProperty.GetSetMethod(true);
                    ;
                    const MethodAttributes methodAttributes =
                        MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
                    var methodAccess = baseSetter.Attributes & MethodAttributes.MemberAccessMask;

                    var setterBuilder = typeBuilder.DefineMethod(
                        "set_" + baseProperty.Name, methodAccess | methodAttributes, null, new[] { baseProperty.PropertyType });
                    var generator = setterBuilder.GetILGenerator();
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Call, baseSetter);
                    generator.Emit(OpCodes.Ret);
                    propertyBuilder.SetSetMethod(setterBuilder);
                }
            }

            private void RegisterInstanceField(FieldBuilder field, bool serializable)
            {
                if (serializable)
                {
                    _serializedFields.Add(field);
                }
                else
                {
                    MarkAsNotSerializable(field);
                }
            }

            private static readonly ConstructorInfo _nonSerializedAttributeConstructor =
                typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes);

            private static readonly ConstructorInfo _ignoreDataMemberAttributeConstructor =
                typeof(IgnoreDataMemberAttribute).GetConstructor(Type.EmptyTypes);

            private static readonly ConstructorInfo _xmlIgnoreAttributeConstructor =
                typeof(XmlIgnoreAttribute).GetConstructor(Type.EmptyTypes);

            private static void MarkAsNotSerializable(FieldBuilder field)
            {
                var emptyArray = new object[0];

                field.SetCustomAttribute(new CustomAttributeBuilder(_nonSerializedAttributeConstructor, emptyArray));

                if (field.IsPublic)
                {
                    field.SetCustomAttribute(new CustomAttributeBuilder(_ignoreDataMemberAttributeConstructor, emptyArray));
                    field.SetCustomAttribute(new CustomAttributeBuilder(_xmlIgnoreAttributeConstructor, emptyArray));
                }
            }
        }
    }
}
