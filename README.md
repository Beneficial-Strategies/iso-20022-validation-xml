![Beneficial Strategies](https://raw.githubusercontent.com/Beneficial-Strategies/iso-20022-validation-xml/main/.beneficial/logo.png)

# BeneficialStrategies.Iso20022.Validation.Xml

Validate ISO 20022 XML straight from text, a document or an element: every problem is reported with its path, then the FluentValidation rules run on what was read.

**Read ISO 20022 XML into an object and validate it in one call.** You get your object, populated, **or** every problem found, in the same `ValidationResult` FluentValidation always gives you, each with the path of the XML element it is about.

The package is built so that you take only what you need. It needs [BeneficialStrategies.Iso20022.Primitives](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Primitives) (the ISO primitive types, the code sets and the XML reader) and [FluentValidation](https://docs.fluentvalidation.net/), nothing else: not the message definitions, not the records. Three examples follow, each adding one thing.

## Install

```
dotnet add package BeneficialStrategies.Iso20022.Validation.Xml --prerelease
```

The package is in alpha, so the command asks for a prerelease version.

## How to use

### Your own class

You do not need the ISO 20022 message library to use this. You need a class of your own whose members are ISO 20022 primitive types, and the XML to read into it.

<!-- snippet: examples/01-your-own-class/Program.cs#class -->
```csharp
public class MyParty
{
    [IsoXmlTag("Name")]
    public required Max35Text Name { get; init; }                         // a primitive type: at most 35 characters

    [IsoXmlTag("ChargeBearer")]
    public required ChargeBearerType1Code ChargeBearer { get; init; }     // a code set: DEBT, CRED, SHAR or SLEV
}

// An ordinary FluentValidation validator. Put rules of your own in it; the ISO rules of the types are enforced as the XML is read.
public class MyPartyValidator : AbstractValidator<MyParty> { }
```
<!-- /snippet -->

Read some XML into it:

<!-- snippet: examples/01-your-own-class/Program.cs#read -->
```csharp
var xml = XElement.Parse("<Party><Name>Acme Corp</Name><ChargeBearer>SHAR</ChargeBearer></Party>");

ValidationResult result = validator.Validate(xml, out MyParty? mine);

if (result.IsValid)
    Console.WriteLine($"{mine!.Name} pays {mine.ChargeBearer}");            // Acme Corp pays Shared
else
    foreach (var failure in result.Errors)
        Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
```
<!-- /snippet -->

With bad XML, `mine` is `null` and the result lists **every** problem, not only the first:

<!-- snippet: examples/01-your-own-class/Program.cs#bad -->
```csharp
var tooLong = XElement.Parse("<Party><Name>A name that is a good deal longer than thirty-five characters</Name><ChargeBearer>FREE</ChargeBearer></Party>");
var problems = validator.Validate(tooLong, out MyParty? none);
foreach (var failure in problems.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
Console.WriteLine(none is null);                                            // True: nothing is built from XML that cannot be read
```
<!-- /snippet -->

```
Xml.FacetViolation Party/Name: 'Max35Text' allows at most 35 character(s) but received 61: 'A name that is a good deal longer than thirty-f...'.
Xml.BadValue Party/ChargeBearer: 'FREE' is not a value of ChargeBearerType1Code. Expected one of: DEBT, CRED, SHAR, SLEV.
True
```

A missing element is a problem too:

<!-- snippet: examples/01-your-own-class/Program.cs#missing -->
```csharp
var missing = XElement.Parse("<Party><Name>Acme Corp</Name></Party>");
var absent = validator.Validate(missing, out MyParty? alsoNone);
foreach (var failure in absent.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}: {failure.ErrorMessage}");
```
<!-- /snippet -->

```
Xml.RequiredMissing Party/ChargeBearer: Required element <ChargeBearer> is missing.
```

What made that work: `[IsoXmlTag]` says which element a member is read from; `required` makes a missing element an error; and the types `Max35Text` and `ChargeBearerType1Code` carry their ISO format rules with them, so they are checked while reading. Declare the `out` variable **with its type** (`out MyParty? mine`; `out var mine` does not compile): that is how the reader knows what to build.

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-validation-xml/tree/main/examples/01-your-own-class) yourself.

### With the message records

You would rather not write the classes: ISO 20022 has thousands of them. [BeneficialStrategies.Iso20022.Messages.Records](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Messages.Records) has every message, component and choice as a ready-made class. Read an element of a message into its record, or a whole document into the record of the message:

<!-- snippet: examples/02-with-the-message-records/Program.cs#read -->
```csharp
var header = XElement.Parse(
    "<GrpHdr><MsgId>MSG-001</MsgId><CreDtTm>2026-01-01T00:00:00</CreDtTm><NbOfSttlmReqs>1</NbOfSttlmReqs></GrpHdr>");

var result = validator.Validate(header, out GroupHeader104? read);       // the record, named by the out variable
Console.WriteLine($"{result.IsValid} {read!.MessageIdentification.Value}");   // True MSG-001
```
<!-- /snippet -->

Your rules are written for the **interface** of the type (`IGroupHeader104`), not for a class, so they work on whatever implements it:

<!-- snippet: examples/02-with-the-message-records/Program.cs#rules -->
```csharp
// A rule of your own, written for the interface, so it works on a record, on an object of the Factory or on a class of yours.
public class MyGroupHeaderRules : AbstractValidator<IGroupHeader104>
{
    public MyGroupHeaderRules() =>
        RuleFor(x => x.NumberOfSettlementRequests.Value).Equal("1").WithMessage("This integration sends one request per message.");
}
```
<!-- /snippet -->

An element that cannot be read is reported, with its path, before any rule runs:

<!-- snippet: examples/02-with-the-message-records/Program.cs#bad -->
```csharp
var bad = XElement.Parse(
    "<GrpHdr><MsgId>A message identification of well over thirty-five characters</MsgId><CreDtTm>2026-01-01T00:00:00</CreDtTm><NbOfSttlmReqs>1</NbOfSttlmReqs></GrpHdr>");

var problems = validator.Validate(bad, out GroupHeader104? unread);
foreach (var failure in problems.Errors)
    Console.WriteLine($"{failure.ErrorCode} {failure.PropertyName}");
```
<!-- /snippet -->

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-validation-xml/tree/main/examples/02-with-the-message-records) yourself.

### Without the classes: the Factory

You want the message's shape, not 3,339 implementations of it. Give the reader a [Factory](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Factory) and read into the interface; the object is made for you:

<!-- snippet: examples/03-without-the-classes/Program.cs#interface -->
```csharp
public interface IMyParty
{
    [IsoXmlTag("Name")] Max35Text Name { get; }
    [IsoXmlTag("ChargeBearer")] ChargeBearerType1Code ChargeBearer { get; }
}

public class MyPartyRules : AbstractValidator<IMyParty> { }
```
<!-- /snippet -->

<!-- snippet: examples/03-without-the-classes/Program.cs#read -->
```csharp
var xml = XElement.Parse("<Party><Name>Acme Corp</Name><ChargeBearer>SHAR</ChargeBearer></Party>");

var result = validator.Validate(xml, factory, out IMyParty? party);       // an object of a class built for the interface

Console.WriteLine($"{result.IsValid} {party!.Name} pays {party.ChargeBearer}");   // True Acme Corp pays Shared
```
<!-- /snippet -->

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-validation-xml/tree/main/examples/03-without-the-classes) yourself.

## More

* Documentation, release notes and issues: https://github.com/Beneficial-Strategies/iso-20022-validation-xml
* License: MIT. Status: alpha.

### Every call has an async form and takes a document, an element or text

`Validate` and `ValidateAsync` read from a string, an `XDocument` or an `XElement`. An element from inside a larger document is read with a `ReadContext` (its namespace and the path prefix to put in front of the problems).

### The rules of ISO's own messages

The validators for ISO's own messages, written for the interfaces, are a separate package: [BeneficialStrategies.Iso20022.Messages.Validation](https://github.com/Beneficial-Strategies/iso-20022-messages-validation). They are `IValidator<T>`, so they work with the calls above. This package does not depend on them.

<!-- end of package readme -->

## Building and contributing

```
dotnet build -c Release
dotnet test -c Release --no-build
tools/run-examples.sh              # packs the package, then builds and runs every example against it
python3 tools/sync-readme.py       # fills the code blocks above from the examples
```

A bug report or a change starts with an issue; a bug fix names the test that failed before it. See CONTRIBUTING.md.
