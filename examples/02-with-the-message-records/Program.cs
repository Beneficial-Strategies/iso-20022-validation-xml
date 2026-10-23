// Example 02: with the message records.
// Shows:  reading one element of an ISO 20022 message into its ready-made record, and running rules of your own on it.
// Needs:  BeneficialStrategies.Iso20022.Validation.Xml and BeneficialStrategies.Iso20022.Messages.Records.
// Prints: the group header read from good XML, then the problems found when its message identification is too long.
using System.Xml.Linq;
using BeneficialStrategies.Iso20022.Components;
using BeneficialStrategies.Iso20022.Validation.Xml;
using FluentValidation;
using MyCompany.Examples;

IValidator<IGroupHeader104> validator = new MyGroupHeaderRules();

// <snippet read>
var header = XElement.Parse(
    "<GrpHdr><MsgId>MSG-001</MsgId><CreDtTm>2026-01-01T00:00:00</CreDtTm><NbOfSttlmReqs>1</NbOfSttlmReqs></GrpHdr>");

var result = validator.Validate(header, out GroupHeader104? read);       // the record, named by the out variable
Console.WriteLine($"{result.IsValid} {read!.MessageIdentification.Value}");   // True MSG-001
// </snippet>

// <snippet bad>
var bad = XElement.Parse(
    "<GrpHdr><MsgId>A message identification of well over thirty-five characters</MsgId><CreDtTm>2026-01-01T00:00:00</CreDtTm><NbOfSttlmReqs>1</NbOfSttlmReqs></GrpHdr>");

var problems = validator.Validate(bad, out GroupHeader104? unread);
foreach (var failure in problems.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}");
// </snippet>

namespace MyCompany.Examples
{
    // <snippet rules>
    // A rule of your own, written for the interface, so it works on a record, on an object of the Factory or on a class of yours.
    public class MyGroupHeaderRules : AbstractValidator<IGroupHeader104>
    {
        public MyGroupHeaderRules() =>
            RuleFor(x => x.NumberOfSettlementRequests.Value).Equal("1").WithMessage("This integration sends one request per message.");
    }
    // </snippet>
}
