﻿using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Swifter.Reflection
{
    /// <summary>
    /// XPropertyInfo 属性信息。
    /// 此属性信息提供的读写方法比 .Net 自带的 PropertyInfo 属性信息快很多。
    /// </summary>
    public abstract class XPropertyInfo
    {
        /// <summary>
        /// 创建 XPropertyInfo 属性信息。
        /// </summary>
        /// <param name="propertyInfo">.Net 自带的 PropertyInfo 属性</param>
        /// <param name="flags">绑定标识</param>
        /// <returns>返回 XPropertyInfo 属性信息。</returns>
        public static XPropertyInfo Create(PropertyInfo propertyInfo, XBindingFlags flags)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var getMethod = propertyInfo.GetGetMethod((flags & XBindingFlags.NonPublic) != 0);
            var setMethod = propertyInfo.GetSetMethod((flags & XBindingFlags.NonPublic) != 0);

            var isStatic = (getMethod == null || getMethod.IsStatic) && (setMethod == null || setMethod.IsStatic);
            var declaringType = propertyInfo.DeclaringType;
            var propertyType = propertyInfo.PropertyType;

            if (propertyType.IsPointer)
            {
                propertyType = typeof(IntPtr);
            }
            else if (propertyType.IsByRef)
            {
                propertyType = propertyType.GetElementType();
            }

            Type targetType;

            if (propertyType.IsByRefLike())
            {
                targetType = typeof(XDefaultPropertyInfo);
            }
            else if (isStatic)
            {
                targetType = typeof(XStaticPropertyInfo<>).MakeGenericType(propertyType);
            }
            else if (declaringType.IsValueType)
            {
                targetType = typeof(XStructPropertyInfo<,>).MakeGenericType(declaringType, propertyType);
            }
            else
            {
                targetType = typeof(XClassPropertyInfo<>).MakeGenericType(propertyType);
            }

            var result = (XPropertyInfo)Activator.CreateInstance(targetType);

            result.Initialize(propertyInfo, flags);

            return result;
        }
        
        internal string name;
        internal XBindingFlags flags;
        internal PropertyInfo propertyInfo;

        internal void Initialize(PropertyInfo propertyInfo, XBindingFlags flags)
        {
            if (propertyInfo.PropertyType.IsByRef)
            {
                InitializeByRef(propertyInfo, flags);
            }
            else
            {
                InitializeByValue(propertyInfo, flags);
            }
        }

        private protected virtual void InitializeByValue(PropertyInfo propertyInfo, XBindingFlags flags)
        {
            this.propertyInfo = propertyInfo;

            name = propertyInfo.Name;
            this.flags = flags;
        }

        private protected virtual void InitializeByRef(PropertyInfo propertyInfo, XBindingFlags flags)
        {
            this.propertyInfo = propertyInfo;

            name = propertyInfo.Name;
            this.flags = flags;
        }

        [MethodImpl(VersionDifferences.AggressiveInlining)]
        internal void Assert(bool err, string name)
        {
            if (!err)
            {
                Throw();
            }

            void Throw()
            {
                throw new MissingMethodException($"Property '{PropertyInfo.DeclaringType.Name}.{PropertyInfo.Name}' No define '{name}' method or cannot access.");
            }
        }

        /// <summary>
        /// 获取 .Net 自带的 PropertyInfo 属性
        /// </summary>
        public PropertyInfo PropertyInfo => propertyInfo;

        /// <summary>
        /// 获取此属性的名称。
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 获取该实例属性的值。
        /// </summary>
        /// <param name="obj">类型的实例</param>
        /// <returns>返回该属性的值</returns>
        public virtual object GetValue(object obj) => throw new InvalidOperationException("Is not an instance property.");

        /// <summary>
        /// 设置该实例属性的值。
        /// </summary>
        /// <param name="obj">类型的实例</param>
        /// <param name="value">值</param>
        public virtual void SetValue(object obj, object value) => throw new InvalidOperationException("Is not an instance property.");

        /// <summary>
        /// 获取该静态属性的值。
        /// </summary>
        /// <returns>返回该属性的值</returns>
        public virtual object GetValue() => throw new InvalidOperationException("Is not an static property.");

        /// <summary>
        /// 设置该静态属性的值。
        /// </summary>
        /// <param name="value">值</param>
        public virtual void SetValue(object value) => throw new InvalidOperationException("Is not an static property.");
    }
}