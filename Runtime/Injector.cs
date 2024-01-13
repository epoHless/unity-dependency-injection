using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace epoHless.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class InjectAttribute : PropertyAttribute
    { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProvideAttribute : PropertyAttribute
    { }
    
    public interface IDependencyProvider {}
    
    [DefaultExecutionOrder(-1000)]
    public class Injector : MonoBehaviour
    {
        private const BindingFlags k_bindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();

        protected void Awake()
        {
            var providers = FindMonoBehaviours().OfType<IDependencyProvider>();

            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }

            var injectables = FindMonoBehaviours().Where(IsInjectable);

            foreach (var injectable in injectables)
            {
                Inject(injectable);
            }
        }

        private void Inject(object instance)
        {
            var type = instance.GetType();
            
            InjectFields(instance, type);
            InjectProperties(instance, type);
            InjectMethods(instance, type);
        }

        private void InjectMethods(object instance, Type type)
        {
            var injectableMethods = type.GetMethods(k_bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

            foreach (var injectableMethod in injectableMethods)
            {
                var requiredParameters = injectableMethod.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                var resolvedInstances = requiredParameters.Select(Resolve).ToArray();

                if (resolvedInstances.Any(resolvedInstance => resolvedInstance == null))
                {
                    throw new Exception();
                }

                injectableMethod.Invoke(instance, resolvedInstances);
            }
        }

        private void InjectFields(object instance, Type type)
        {
            var injectableFields = type.GetFields(k_bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

            foreach (var injectableField in injectableFields)
            {
                if (injectableField.GetValue(instance) != null)
                {
                    Debug.LogWarning($"[Injector] Field '{injectableField.Name}' of class '{type.Name}' is already injected.");
                    continue;
                }
                
                var fieldType = injectableField.FieldType;
                var resolvedInstance = Resolve(fieldType);

                if (resolvedInstance == null)
                {
                    throw new Exception();
                }
                
                injectableField.SetValue(instance, resolvedInstance);
                Debug.Log($"Injected {fieldType.Name} into {injectableField.Name}");
            }
        }
        
        private void InjectProperties(object instance, Type type)
        {
            var injectableProperties = type.GetProperties(k_bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

            foreach (var injectableProperty in injectableProperties)
            {
                var propertyType = injectableProperty.PropertyType;
                var resolvedInstance = Resolve(propertyType);

                if (resolvedInstance == null)
                {
                    throw new Exception();
                }
                
                injectableProperty.SetValue(instance, resolvedInstance);
                Debug.Log($"Injected {propertyType.Name} into {injectableProperty.Name}");
            }
        }

        object Resolve(Type type)
        {
            registry.TryGetValue(type, out var resolvedInstance);
            return resolvedInstance;
        }

        static bool IsInjectable(MonoBehaviour obj)
        {
            var members = obj.GetType().GetMembers(k_bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }

        private void RegisterProvider(IDependencyProvider provider)
        { 
            var methods = provider.GetType().GetMethods(k_bindingFlags);

            foreach (var method in methods)
            {
                if(!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;

                var returnType = method.ReturnType;
                var providedInstance = method.Invoke(provider, null);

                if (providedInstance != null)
                {
                    registry.Add(returnType, providedInstance);
                    Debug.Log($"Provided: {providedInstance}");
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        static MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID);
        }
    }
}
