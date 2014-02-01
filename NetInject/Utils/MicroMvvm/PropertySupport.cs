//  Original author - Josh Smith - http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030
namespace NetInject.Utils.MicroMvvm {
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Properties;
    public static class PropertySupport {
        public static String ExtractPropertyName<T>(Expression<Func<T>> propertyExpresssion) {
            if (propertyExpresssion == null)
                throw new ArgumentNullException("propertyExpresssion");
            var memberExpression = propertyExpresssion.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException(Resources.PropertySupport_ExtractPropertyName_The_expression_is_not_a_member_access_expression_, "propertyExpresssion");
            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
                throw new ArgumentException(Resources.PropertySupport_ExtractPropertyName_The_member_access_expression_does_not_access_a_property_, "propertyExpresssion");
            MethodInfo getMethod = property.GetGetMethod(true);
            if (getMethod.IsStatic)
                throw new ArgumentException(Resources.PropertySupport_ExtractPropertyName_The_referenced_property_is_a_static_property_, "propertyExpresssion");
            return memberExpression.Member.Name;
        }
    }
}