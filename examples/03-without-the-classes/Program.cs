// Example 03: without the classes.
// Shows:  reading XML into an interface, with the object made by the Factory, so no class is written or named.
// Needs:  BeneficialStrategies.Iso20022.Validation.Xml and BeneficialStrategies.Iso20022.Factory.
// Prints: the party read through its interface, and whether the object is of a class we wrote (it is not).
using System.Xml.Linq;
using BeneficialStrategies.Iso20022.Factory;
using BeneficialStrategies.Iso20022.Validation.Xml;
using FluentValidation;
using BeneficialStrategies.Iso20022.Codesets;
using BeneficialStrategies.Iso20022.Framework;
using BeneficialStrategies.Iso20022.SimpleTypes;
using MyCompany.Examples;

IValidator<IMyParty> validator = new MyPartyRules();
var factory = new IsoDataFactory();

// <snippet read>
var xml = XElement.Parse("<Party><Name>Acme Corp</Name><ChargeBearer>SHAR</ChargeBearer></Party>");

var result = validator.Validate(xml, factory, out IMyParty? party);       // an object of a class built for the interface

Console.WriteLine($"{result.IsValid} {party!.Name} pays {party.ChargeBearer}");   // True Acme Corp pays Shared
// </snippet>

namespace MyCompany.Examples
{
    // <snippet interface>
    public interface IMyParty
    {
        [IsoXmlTag("Name")] Max35Text Name { get; }
        [IsoXmlTag("ChargeBearer")] ChargeBearerType1Code ChargeBearer { get; }
    }

    public class MyPartyRules : AbstractValidator<IMyParty> { }
    // </snippet>
}
