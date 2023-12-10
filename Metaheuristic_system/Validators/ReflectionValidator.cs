using System.Reflection;

namespace Metaheuristic_system.Validators
{
    public static class ReflectionValidator
    {
       public static bool ImplementsInterface(Type targetType, Type interfaceType)
       {
            if (targetType.Name == interfaceType.Name)
            {
                var interfaceMethods = interfaceType.GetMethods();
                var interfaceProperties = interfaceType.GetProperties();

                var targetMethods = targetType.GetMethods();
                var targetProperties = targetType.GetProperties();

                bool methodsPresent = interfaceMethods.All(interfaceMethod =>
                    targetMethods.Any(targetMethod =>
                        targetMethod.Name == interfaceMethod.Name &&
                        targetMethod.ReturnType.Name == interfaceMethod.ReturnType.Name &&
                        targetMethod.GetParameters().Length == interfaceMethod.GetParameters().Length &&
                        interfaceMethod.GetParameters().All(parameter =>
                            targetMethod.GetParameters().Any(param =>
                            param.ParameterType.Name == parameter.ParameterType.Name &&
                            param.Name == parameter.Name))
                        ));

                bool propertiesPresent = interfaceProperties.All(interfaceProperty =>
                    targetProperties.Any(targetProperty =>
                        targetProperty.Name == interfaceProperty.Name && targetProperty.PropertyType.Name == interfaceProperty.PropertyType.Name));

                return methodsPresent && propertiesPresent;
            }
            return false;
       }
    }
}
