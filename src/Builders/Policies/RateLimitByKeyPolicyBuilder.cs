namespace Mielek.Builders.Policies
{
    using System.Collections.Immutable;
    using System.Xml.Linq;

    using Mielek.Builders.Expressions;
    using Mielek.Generators.Attributes;

    [GenerateBuilderSetters]
    public partial class RateLimitByKeyPolicyBuilder
    {
        private uint? _calls;
        private uint? _renewalPeriod;
        private IExpression<string>? _counterKey;
        private IExpression<bool>? _incrementCondition;
        private uint? _incrementCount;
        private string? _retryAfterHeaderName;
        private string? _retryAfterVariableName;
        private string? _remainingCallsHeaderName;
        private string? _remainingCallsVariableName;
        private string? _totalCallsHeaderName;

        public XElement Build()
        {
            if (_calls == null) throw new NullReferenceException();
            if (_renewalPeriod == null) throw new NullReferenceException();
            if (_counterKey == null) throw new NullReferenceException();

            var children = ImmutableArray.CreateBuilder<object>();
            children.Add(new XAttribute("calls", _calls));
            children.Add(new XAttribute("renewal-period", _renewalPeriod));
            children.Add(new XAttribute("counter-key", _counterKey.GetXText()));

            if (_incrementCondition != null)
            {
                children.Add(new XAttribute("increment-condition", _incrementCondition.GetXText()));
            }
            if (_incrementCount != null)
            {
                children.Add(new XAttribute("increment-count", _incrementCount));
            }
            if (_retryAfterHeaderName != null)
            {
                children.Add(new XAttribute("retry-after-header-name", _retryAfterHeaderName));
            }
            if (_retryAfterVariableName != null)
            {
                children.Add(new XAttribute("retry-after-variable-name", _retryAfterVariableName));
            }
            if (_remainingCallsHeaderName != null)
            {
                children.Add(new XAttribute("remaining-calls-header-name", _remainingCallsHeaderName));
            }
            if (_remainingCallsVariableName != null)
            {
                children.Add(new XAttribute("remaining-calls-variable-name", _remainingCallsVariableName));
            }
            if (_totalCallsHeaderName != null)
            {
                children.Add(new XAttribute("total-calls-header-name", _totalCallsHeaderName));
            }

            return new XElement("rate-limit-by-key", children.ToArray());
        }
    }
}

namespace Mielek.Builders
{
    using Mielek.Builders.Policies;

    public partial class PolicySectionBuilder
    {
        public PolicySectionBuilder RateLimitByKey(Action<RateLimitByKeyPolicyBuilder> configurator)
        {
            var builder = new RateLimitByKeyPolicyBuilder();
            configurator(builder);
            this._sectionPolicies.Add(builder.Build());
            return this;
        }
    }
}