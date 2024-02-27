using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.Compilation.SyntaxCompiler;

public class BlockCompiler : ISyntaxCompiler
{
    private IReadOnlyDictionary<SyntaxKind, ISyntaxCompiler> compilers;
    
    public SyntaxKind IsCompiling => SyntaxKind.Block;

    public void Compile(ICompilationContext context, SyntaxNode node)
    {
        var block = node as BlockSyntax ?? throw new Exception();
        
        foreach (var statement in block.Statements)
        {
            if (compilers.TryGetValue(statement.Kind(), out var compiler))
            {
                compiler.Compile(context, statement);
            }
        }
        
        
    }
}