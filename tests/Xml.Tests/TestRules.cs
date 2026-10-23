// Copyright 2026 Jeff Ward, Beneficial Strategies. Usage subject to license of enclosing library.

namespace BeneficialStrategies.Iso20022.Validation.Xml.Tests;

// The rules that run after a document is read. They are a few of the real message's rules, written here so that this package is
// tested without the validators of the message library: what is tested is the reading, the reporting and the call, not the rules.

/// <summary>A group header needs its message identification.</summary>
internal sealed class GroupHeaderRules : AbstractValidator<IGroupHeader104>
{
    public GroupHeaderRules()
    {
        RuleFor(x => x.MessageIdentification.Value).NotEmpty().WithName("MessageIdentification");
    }
}

/// <summary>A settlement request message needs at least one request, and as many as its header says.</summary>
internal sealed class SettlementRequestRules : AbstractValidator<IMultilateralSettlementRequestV02>
{
    public SettlementRequestRules()
    {
        RuleFor(x => x.GroupHeader).SetValidator(new GroupHeaderRules());
        RuleFor(x => x.SettlementRequest).NotEmpty();
        RuleFor(x => x.SettlementRequest.Count().ToString())
            .Equal(x => x.GroupHeader.NumberOfSettlementRequests.Value)
            .WithName("SettlementRequest");
    }
}
