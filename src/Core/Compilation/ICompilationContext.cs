using System.Xml.Linq;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.Compilation;

public interface ICompilationContext
{
    void AddPolicy(XElement element);
    void ReportError(string message);
}