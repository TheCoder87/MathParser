﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GuiLabs.MathParser
{
    public class Binder
    {
        private List<MethodInfo> methods = new List<MethodInfo>();
        private Dictionary<string, Func<double>> variables = new Dictionary<string, Func<double>>();
        private Dictionary<string, ParameterExpression> parameters = new Dictionary<string, ParameterExpression>();

        public static Binder Default { get; } = CreateDefaultBinder();

        public Binder(bool includeMathMethods = true)
        {
            if (includeMathMethods)
            {
                RegisterStaticMethods(typeof(Math));
            }
        }

        private static Binder CreateDefaultBinder()
        {
            var binder = new Binder();
            return binder;
        }

        public void RegisterStaticMethods<T>() => RegisterStaticMethods(typeof(T));

        public void RegisterStaticMethods(Type type)
        {
            foreach (var methodInfo in type.GetRuntimeMethods())
            {
                methods.Add(methodInfo);
            }
        }

        public void RegisterParameter(ParameterExpression parameter)
        {
            parameters[parameter.Name] = parameter;
        }

        public void RegisterVariable(string variableName, Func<double> valueGetter)
        {
            variables[variableName] = valueGetter;
        }

        ParameterExpression ResolveParameter(string parameterName)
        {
            if (parameters.TryGetValue(parameterName, out var parameter))
            {
                return parameter;
            }

            return null;
        }

        Expression ResolveConstant(string identifier)
        {
            if (identifier.Equals("pi", StringComparison.OrdinalIgnoreCase))
            {
                return Expression.Constant(Math.PI);
            }
            else if (identifier.Equals("e", StringComparison.OrdinalIgnoreCase))
            {
                return Expression.Constant(Math.E);
            }

            return null;
        }

        Expression ResolveVariable(string identifier)
        {
            if (variables.TryGetValue(identifier, out var variable))
            {
                return Expression.Call(Expression.Constant(variable.Target), variable.GetMethodInfo());
            }

            return null;
        }

        public Expression Resolve(string identifier)
        {
            var result = ResolveConstant(identifier)
                ?? ResolveVariable(identifier)
                ?? ResolveParameter(identifier);
            if (result != null)
            {
                return result;
            }

            var method = ResolveMethod(identifier, 0);
            if (method != null)
            {
                return Expression.Call(method);
            }

            return null;
        }

        public MethodInfo ResolveMethod(string functionName, int parameterCount)
        {
            foreach (var methodInfo in methods)
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length != parameterCount)
                {
                    continue;
                }

                if (methodInfo.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase)
                    && methodInfo.IsStatic
                    && parameters.All(p => p.ParameterType == typeof(double))
                    && methodInfo.ReturnType == typeof(double))
                {
                    return methodInfo;
                }
            }

            return null;
        }
    }
}
