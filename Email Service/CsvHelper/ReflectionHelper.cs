using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper.Configuration;

namespace CsvHelper
{
    /// <summary>
    ///     Common reflection tasks.
    /// </summary>
    internal static class ReflectionHelper
    {
        /// <summary>
        ///     Creates an instance of type T.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="args">The constructor arguments.</param>
        /// <returns>A new instance of type T.</returns>
        public static T CreateInstance<T>(params object[] args)
        {
            return (T) CreateInstance(typeof(T), args);
        }

        /// <summary>
        ///     Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type of instance to create.</param>
        /// <param name="args">The constructor arguments.</param>
        /// <returns>A new instance of the specified type.</returns>
        public static object CreateInstance(Type type, params object[] args)
        {
            object obj;
            var array = (
                from a in (IEnumerable<object>) args
                select a.GetType()).ToArray();
            var parameterExpressionArray =
                array.Select((t, i) => Expression.Parameter(t, string.Concat("var", i))).ToArray();
            var @delegate =
                Expression.Lambda(Expression.New(type.GetConstructor(array), parameterExpressionArray),
                    parameterExpressionArray).Compile();
            try
            {
                obj = @delegate.DynamicInvoke(args);
            }
            catch (TargetInvocationException targetInvocationException)
            {
                throw targetInvocationException.InnerException;
            }
            return obj;
        }

        /// <summary>
        ///     Gets the first attribute of type T on property.
        /// </summary>
        /// <typeparam name="T">Type of attribute to get.</typeparam>
        /// <param name="property">The <see cref="T:System.Reflection.PropertyInfo" /> to get the attribute from.</param>
        /// <param name="inherit">True to search inheritance tree, otherwise false.</param>
        /// <returns>The first attribute of type T, otherwise null.</returns>
        public static T GetAttribute<T>(PropertyInfo property, bool inherit)
            where T : Attribute
        {
            var item = default(T);
            var list = property.GetCustomAttributes(typeof(T), inherit).ToList();
            if (list.Count > 0)
                item = list[0] as T;
            return item;
        }

        /// <summary>
        ///     Gets the attributes of type T on property.
        /// </summary>
        /// <typeparam name="T">Type of attribute to get.</typeparam>
        /// <param name="property">The <see cref="T:System.Reflection.PropertyInfo" /> to get the attribute from.</param>
        /// <param name="inherit">True to search inheritance tree, otherwise false.</param>
        /// <returns>The attributes of type T.</returns>
        public static T[] GetAttributes<T>(PropertyInfo property, bool inherit)
            where T : Attribute
        {
            return property.GetCustomAttributes(typeof(T), inherit).Cast<T>().ToArray();
        }

        /// <summary>
        ///     Gets the constructor <see cref="T:System.Linq.Expressions.NewExpression" /> from the give
        ///     <see cref="T:System.Linq.Expressions.Expression" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the object that will be constructed.</typeparam>
        /// <param name="expression">The constructor <see cref="T:System.Linq.Expressions.Expression" />.</param>
        /// <returns>A constructor <see cref="T:System.Linq.Expressions.NewExpression" />.</returns>
        /// <exception cref="T:System.ArgumentException">Not a constructor expression.;expression</exception>
        public static NewExpression GetConstructor<T>(Expression<Func<T>> expression)
        {
            var body = expression.Body as NewExpression;
            if (body == null)
                throw new ArgumentException("Not a constructor expression.", "expression");
            return body;
        }

        /// <summary>
        ///     Gets the member expression.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        private static MemberExpression GetMemberExpression<TModel, T>(Expression<Func<TModel, T>> expression)
        {
            MemberExpression operand = null;
            if (expression.Body.NodeType == ExpressionType.Convert)
                operand = ((UnaryExpression) expression.Body).Operand as MemberExpression;
            else if (expression.Body.NodeType == ExpressionType.MemberAccess)
                operand = expression.Body as MemberExpression;
            if (operand == null)
                throw new ArgumentException("Not a member access", "expression");
            return operand;
        }

        /// <summary>
        ///     Gets the property from the expression.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>The <see cref="T:System.Reflection.PropertyInfo" /> for the expression.</returns>
        public static PropertyInfo GetProperty<TModel>(Expression<Func<TModel, object>> expression)
        {
            var member = GetMemberExpression(expression).Member;
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo == null)
                throw new CsvConfigurationException(
                    string.Format("'{0}' is not a property. Did you try to map a field by accident?", member.Name));
            return propertyInfo;
        }
    }
}