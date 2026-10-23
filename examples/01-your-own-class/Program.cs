// Example 01: your own class.
// Shows:  reading XML into a class of your own and validating it in one call; every problem is reported with its path.
// Needs:  only BeneficialStrategies.Iso20022.Validation.Xml (which brings Primitives and FluentValidation).
// Prints: the party read from good XML, then the problems found in two bad ones.
using System.Xml.Linq;
using BeneficialStrategies.Iso20022.Codesets;
using BeneficialStrategies.Iso20022.Framework;
using BeneficialStrategies.Iso20022.SimpleTypes;
using BeneficialStrategies.Iso20022.Validation.Xml;
using FluentValidation;
using FluentValidation.Results;
using MyCompany.Examples;

IValidator<MyParty> validator = new MyPartyValidator();

// <snippet read>
var xml = XElement.Parse("<Party><Name>Acme Corp</Name><ChargeBearer>SHAR</ChargeBearer></Party>");

ValidationResult result = validator.Validate(xml, out MyParty? mine);

if (result.IsValid)
    Console.WriteLine($"{mine!.Name} pays {mine.ChargeBearer}");            // Acme Corp pays Shared
else
    foreach (var failure in result.Errors)
        Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
// </snippet>

// <snippet bad>
var tooLong = XElement.Parse("<Party><Name>A name that is a good deal longer than thirty-five characters</Name><ChargeBearer>FREE</ChargeBearer></Party>");
var problems = validator.Validate(tooLong, out MyParty? none);
foreach (var failure in problems.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
Console.WriteLine(none is null);                                            // True: nothing is built from XML that cannot be read
// </snippet>

// <snippet missing>
var missing = XElement.Parse("<Party><Name>Acme Corp</Name></Party>");
var absent = validator.Validate(missing, out MyParty? alsoNone);
foreach (var failure in absent.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
// </snippet>

namespace MyCompany.Examples
{
    // <snippet class>
    public class MyParty
    {
        [IsoXmlTag("Name")]
        public required Max35Text Name { get; init; }                         // a primitive type: at most 35 characters

        [IsoXmlTag("ChargeBearer")]
        public required ChargeBearerType1Code ChargeBearer { get; init; }     // a code set: DEBT, CRED, SHAR or SLEV
    }

    // An ordinary FluentValidation validator. Put rules of your own in it; the ISO rules of the types are enforced as the XML is read.
    public class MyPartyValidator : AbstractValidator<MyParty> { }
    // </snippet>
}
