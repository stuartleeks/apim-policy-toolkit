﻿using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Mielek.Azure.ApiManagement.PolicyToolkit.Builders.Expressions;
using Mielek.Azure.ApiManagement.PolicyToolkit.Builders.Policies;
using Mielek.Azure.ApiManagement.PolicyToolkit.CodeContext;
using Mielek.Azure.ApiManagement.PolicyToolkit.Compilation.Policy;
using Mielek.Azure.ApiManagement.PolicyToolkit.Compilation.Syntax;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.Compilation;

public class CSharpPolicyCompiler
{
    private readonly ClassDeclarationSyntax _document;

    private readonly BlockCompiler _blockCompiler;
    
    public CSharpPolicyCompiler(ClassDeclarationSyntax document)
    {
        _document = document;
        var invStatement = new InvocationExpressionCompiler(new IMethodPolicyHandler[]
        {
            new BaseCompiler(),
            new SetHeaderCompiler(),
            new SetBodyCompiler(),
            new AuthenticationBasicCompiler()
        });
        var loc = new LocalDeclarationStatementCompiler(new IReturnValueMethodPolicyHandler[]
        {
            new AuthenticationManageIdentityCompiler()
        });
        _blockCompiler = new(new ISyntaxCompiler[]
        {
            invStatement,
            loc
        });
        _blockCompiler.AddCompiler(new IfStatementCompiler(this._blockCompiler));
    }
    
    public XElement Compile()
    {
        var methods = _document.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();
        var policyDocument = new XElement("policies");
        
        foreach (var method in methods)
        {
            var sectionName = method.Identifier.ValueText switch
            {
                nameof(ICodeDocument.Inbound) => "inbound",
                nameof(ICodeDocument.Outbound) => "outbound",
                nameof(ICodeDocument.Backend) => "inbound",
                nameof(ICodeDocument.OnError) => "on-error",
                _ => throw new InvalidOperationException("Invalid section")
            };

            var section = CompileSection(sectionName, method.Body);
            policyDocument.Add(section);
        }

        return policyDocument;
    }


    private XElement CompileSection(string section, BlockSyntax block)
    {
        var sectionElement = new XElement(section);
        var context = new CompilationContext(_document, sectionElement);
        _blockCompiler.Compile(context, block);
        // foreach (var statement in block.Statements)
        // {
        //     switch (statement)
        //     {
        //         case LocalDeclarationStatementSyntax syntax:
        //             ProcessLocalDeclaration(syntax, sectionElement);
        //             break;
        //         case ExpressionStatementSyntax syntax:
        //             ProcessExpression(syntax, sectionElement);
        //             break;
        //         case IfStatementSyntax syntax:
        //             ProcessIf(syntax, sectionElement);
        //             break;
        //     }
        // }

        return sectionElement;
    }

    private void ProcessIf(IfStatementSyntax syntax, XElement sectionElement)
    {
        var choose = new XElement("choose");
        sectionElement.Add(choose);

        IfStatementSyntax? nextIf = syntax;
        IfStatementSyntax currentIf;
        do
        {
            currentIf = nextIf;
            var whenSection = CompileSection("when", currentIf.Statement as BlockSyntax);
            choose.Add(whenSection);

            whenSection.Add(new XAttribute("condition", FindCode(currentIf.Condition as InvocationExpressionSyntax)));

            nextIf = currentIf.Else?.Statement as IfStatementSyntax;
        } while (nextIf != null);
        
        
        if(currentIf.Else != null)
        {
            var otherwiseSection = CompileSection("otherwise", currentIf.Else.Statement as BlockSyntax);
            choose.Add(otherwiseSection);
        }
    }

    private void ProcessExpression(ExpressionStatementSyntax syntax, XElement section)
    {
        var invocation = syntax.Expression as InvocationExpressionSyntax;
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        switch (memberAccess.Name.ToString())
        {
            case "SetHeader":
                ProcessSetHeader(section, invocation);
                break;
            case "RemoveHeader":
                ProcessRemoveHeader(section, invocation);
                break;
            case "SetBody":
                ProcessSetBody(section, invocation);
                break;
            case "Base":
                section.Add(new XElement("base"));
                break;
            case "AuthenticationBasic":
                ProcessAuthenticationBasic(section, invocation);
                break;
            case "AuthenticationManagedIdentity":
                var resource = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
                section.Add(new AuthenticationManagedIdentityPolicyBuilder()
                    .Resource(resource)
                    .Build());
                break;
        }
    }

    private void ProcessAuthenticationBasic(XElement section, InvocationExpressionSyntax invocation)
    {
        var username = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
        var password = ProcessParameter(invocation.ArgumentList.Arguments[1].Expression);
        section.Add(new AuthenticationBasicPolicyBuilder()
            .Username(username)
            .Password(password)
            .Build());
    }

    private void ProcessSetBody(XElement section, InvocationExpressionSyntax invocation)
    {
        var value = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
        section.Add(new SetBodyPolicyBuilder()
            .Body(value)
            .Build());
    }

    private void ProcessRemoveHeader(XElement section, InvocationExpressionSyntax invocation)
    {
        var headerName = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
        section.Add(new SetHeaderPolicyBuilder()
            .Name(headerName)
            .ExistsAction(SetHeaderPolicyBuilder.ExistsActionType.Delete)
            .Build());
    }

    private void ProcessSetHeader(XElement section, InvocationExpressionSyntax invocation)
    {
        var headerName = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
        var headerValue = ProcessParameter(invocation.ArgumentList.Arguments[1].Expression);
        section.Add(new SetHeaderPolicyBuilder()
            .Name(headerName)
            .ExistsAction(SetHeaderPolicyBuilder.ExistsActionType.Override)
            .Value(headerValue)
            .Build());
    }

    private void ProcessLocalDeclaration(LocalDeclarationStatementSyntax syntax, XElement section)
    {
        var variable = syntax.Declaration.Variables[0];
        var invocation = variable.Initializer.Value as InvocationExpressionSyntax;
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        switch (memberAccess.Name.ToString())
        {
            case "AuthenticationManagedIdentity":
                var resource = ProcessParameter(invocation.ArgumentList.Arguments[0].Expression);
                section.Add(new AuthenticationManagedIdentityPolicyBuilder()
                    .Resource(resource)
                    .OutputTokenVariableName(variable.Identifier.ValueText)
                    .Build());
                break;
        }

    }

    private string ProcessParameter(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax syntax:
                return syntax.Token.ValueText;
            case InterpolatedStringExpressionSyntax syntax:
                var interpolationParts = syntax.Contents.Select(c => c switch
                {
                    InterpolatedStringTextSyntax text => text.TextToken.ValueText,
                    InterpolationSyntax interpolation =>
                        $"{{context.Variables[\"{interpolation.Expression.ToString()}\"]}}",
                    _ => ""
                });
                return new LambdaExpression<string>($"context => $\"{string.Join("", interpolationParts)}\"").Source;
            case AnonymousFunctionExpressionSyntax syntax:
                return new LambdaExpression<string>(syntax.ToString()).Source;
            case InvocationExpressionSyntax syntax:
                return FindCode(syntax);
        }

        return "";
    }

    private string FindCode(InvocationExpressionSyntax syntax)
    {
        var methodIdentifier = (syntax.Expression as IdentifierNameSyntax).Identifier.ValueText;
        var expressionMethod = _document.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == methodIdentifier);

        if (expressionMethod.Body != null)
        {
            return new LambdaExpression<bool>($"context => {expressionMethod.Body}").Source;
        }
        else if (expressionMethod.ExpressionBody != null)
        {
            return new LambdaExpression<bool>($"context => {expressionMethod.ExpressionBody.Expression}").Source;
        }
        else
        {
            throw new InvalidOperationException("Invalid expression");
        }
    }
}
