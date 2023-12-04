using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Mielek.Azure.ApiManagement.PolicyToolkit.Expressions.Context;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.Builders.Expressions;

public class ExpressionBuilder<T>
{
    public static ExpressionBuilder<T> Builder => new();

    public static IExpression<T> BuildFromConfiguration(Action<ExpressionBuilder<T>> configurator)
    {
        var builder = ExpressionBuilder<T>.Builder;
        configurator(builder);
        return builder.Build();
    }

    private IExpression<T>? _expression;

    internal ExpressionBuilder() { }

    public ExpressionBuilder<T> Constant(T value)
    {
        _expression = new ConstantExpression<T>(value);
        return this;
    }

    public ExpressionBuilder<T> Inline(Expression<Func<IContext, T>> expression)
    {
        _expression = new InlineExpression<T>(expression.Body.ToString());
        return this;
    }

    public ExpressionBuilder<T> Lambda(Func<IContext, T> lambda, [CallerArgumentExpression(nameof(lambda))] string? lambdaCode = null)
    {
        if (lambdaCode == null)
        {
            throw new Exception("Compiler services are not enabled");
        }

        var info = lambda.GetMethodInfo();
        ValidateParameters(info);
        _expression = new LambdaExpression<T>(info, lambdaCode);
        return this;
    }

    public ExpressionBuilder<T> Method(Func<IContext, T> method, [CallerFilePath] string? sourceFilePath = null)
    {
        if (sourceFilePath == null)
        {
            throw new Exception("Compiler services are not enabled");
        }

        var info = method.GetMethodInfo();
        ValidateParameters(info);
        _expression = new MethodExpression<T>(info, sourceFilePath);
        return this;
    }

    public ExpressionBuilder<T> Function(Func<IContext, T> func, [CallerArgumentExpression(nameof(func))] string? code = null, [CallerFilePath] string? sourceFilePath = null)
    {
        if (code == null || sourceFilePath == null)
        {
            throw new Exception("Compiler services are not enabled");
        }

        if (Regex.Match(code, @"\(?\s*[^)]*\s*\)?\s*=>\s*.+", RegexOptions.Singleline).Success)
        {
            return Lambda(func, code);
        }
        else
        {
            return Method(func, sourceFilePath);
        }
    }

    public IExpression<T> Build()
    {
        return _expression ?? throw new NullReferenceException();
    }

    private void ValidateParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters == null || parameters.Length > 1)
        {
            throw new Exception($"Expression should have only one parameter but have 0 or more then one");
        }

        var parameter = parameters[0];
        if (parameter.ParameterType != typeof(IContext))
        {
            throw new Exception($"Parameter should be of type \"IContext\" but is \"{parameter.ParameterType}\"");
        }

        if (parameter.Name != "context")
        {
            throw new Exception($"Context parameter should be named \"context\" but is \"{parameter.Name}\"");
        }
    }
}