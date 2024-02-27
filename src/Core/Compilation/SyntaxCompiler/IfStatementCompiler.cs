using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.Compilation.SyntaxCompiler;

public class IfStatementCompiler : ISyntaxCompiler
{
    public SyntaxKind IsCompiling => SyntaxKind.IfStatement;

    public void Compile(ICompilationContext context, SyntaxNode node)
    {
        var ifStatement = node as IfStatementSyntax ?? throw new Exception();
        
        var choose = new XElement("choose");
        // IfStatementSyntax? nextIf = ifStatement;
        // IfStatementSyntax currentIf;
        // do
        // {
        //     currentIf = nextIf;
        //     var whenSection = CompileSection("when", currentIf.Statement as BlockSyntax);
        //     choose.Add(whenSection);
        //
        //     whenSection.Add(new XAttribute("condition", FindCode(currentIf.Condition as InvocationExpressionSyntax)));
        //
        //     nextIf = currentIf.Else?.Statement as IfStatementSyntax;
        // } while (nextIf != null);
        //
        //
        // if(currentIf.Else != null)
        // {
        //     var otherwiseSection = CompileSection("otherwise", currentIf.Else.Statement as BlockSyntax);
        //     choose.Add(otherwiseSection);
        // }

        return choose;
    }
}