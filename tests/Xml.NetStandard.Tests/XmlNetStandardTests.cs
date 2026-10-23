// Copyright 2026 Jeff Ward, Beneficial Strategies. Usage subject to license of enclosing library.

namespace BeneficialStrategies.Iso20022.Validation.Xml.NetStandardTests;

/// <summary>
/// The netstandard2.0 build of the XML validation, run on .NET Framework 4.8 as well as on .NET 8 and 10, with messages of the tests' own
/// and no message record.
/// </summary>
public class XmlNetStandardTests
{
    public interface ITinyMessage : IOuterRecord
    {
        Max35Text Identification { get; }
    }

    public sealed record TinyMessage : ITinyMessage
    {
        public const string IsoIdentifier = "tiny.001.001.01";
        public const string XmlTag = "Tiny";
        public const string DocumentNamespace = "urn:test:tiny";
        public const string DocumentElementName = "Document";

        [IsoXmlTag("Id")]
        public Max35Text Identification { get; init; }
    }

    [IsoMessage("tiny.002.001.01", "Tiny", "urn:test:tiny2")]
    public interface ITinyAttributed : IOuterRecord
    {
        [IsoXmlTag("Id")]
        Max35Text Identification { get; }
    }

    /// <summary>No constants at all: how to read and write it comes from the interface.</summary>
    public sealed record TinyAttributed : ITinyAttributed
    {
        [IsoXmlTag("Id")]
        public Max35Text Identification { get; init; }
    }

    [Fact]
    public void AMessageWithoutConstants_IsWrittenAndRead_FromTheAttributeOfItsInterface()
    {
        var xml = Iso20022XmlSerializer.SerializeToString(new TinyAttributed { Identification = "ATTR-1" });

        Assert.Contains("urn:test:tiny2", xml);
        var read = Iso20022XmlSerializer.TryRead<TinyAttributed>(xml);
        Assert.True(read.Succeeded, string.Join("; ", read.Issues.Select(i => i.Reason)));
        Assert.Equal("ATTR-1", read.Message!.Identification.Value);
    }

    [Fact]
    public void AMessageInterface_IsReadIntoAClassBuiltAtRunTime()
    {
        var xml = Iso20022XmlSerializer.SerializeToString(new TinyAttributed { Identification = "RUN-1" });

        var read = Iso20022XmlSerializer.TryRead<ITinyAttributed>(xml, new Factory.IsoDataFactory());

        Assert.True(read.Succeeded, string.Join("; ", read.Issues.Select(i => i.Reason)));
        Assert.IsNotType<TinyAttributed>(read.Message);
        Assert.Equal("RUN-1", read.Message!.Identification.Value);

        Assert.Equal(xml, Iso20022XmlSerializer.SerializeToString(read.Message));
    }

    [Fact]
    public void XmlExtension_ValidatesAMessageInterface_WithTheFactory_OnEveryTarget()
    {
        var rules = new InlineValidator<ITinyAttributed>();
        rules.RuleFor(x => x.Identification).Must(id => id.Value != "BAD").WithMessage("BAD is not allowed.");
        IValidator<ITinyAttributed> sut = rules;
        var factory = new Factory.IsoDataFactory();
        string Xml(string id) => Iso20022XmlSerializer.SerializeToString(new TinyAttributed { Identification = id });

        var good = sut.Validate(Xml("OK"), factory, out ITinyAttributed? message);
        var bad = sut.Validate(Xml("BAD"), factory, out ITinyAttributed? rejected);
        var broken = sut.Validate("<not-xml", factory, out ITinyAttributed? none);

        Assert.True(good.IsValid);
        Assert.Equal("OK", message!.Identification.Value);
        Assert.False(bad.IsValid);
        Assert.NotNull(rejected);
        Assert.False(broken.IsValid);
        Assert.Null(none);
    }

    private static InlineValidator<ITinyMessage> TinyRules()
    {
        var rules = new InlineValidator<ITinyMessage>();
        rules.RuleFor(x => x.Identification).Must(id => id.Value != "BAD").WithMessage("BAD is not allowed.");
        return rules;
    }

    private static string TinyXml(string id) => $"<Document xmlns=\"urn:test:tiny\"><Tiny><Id>{id}</Id></Tiny></Document>";

    [Fact]
    public void XmlExtension_ReadsAndValidatesAMessageOfTheTestsOwn()
    {
        IValidator<ITinyMessage> sut = TinyRules();

        var good = sut.Validate(TinyXml("OK"), out TinyMessage? message);
        var bad = sut.Validate(TinyXml("BAD"), out TinyMessage? rejected);
        var malformed = sut.Validate("<not-xml", out TinyMessage? none);
        var wrongRoot = sut.Validate("<Other xmlns=\"urn:not-iso20022\"/>", out TinyMessage? alsoNone);

        Assert.True(good.IsValid);
        Assert.Equal("OK", message!.Identification.Value);
        Assert.False(bad.IsValid);
        Assert.NotNull(rejected); // read, but a rule failed: the record is still returned
        Assert.Equal("Xml." + IssueCode.MalformedXml, Assert.Single(malformed.Errors).ErrorCode);
        Assert.Null(none);
        Assert.Contains(wrongRoot.Errors, e => e.ErrorCode == "Xml." + IssueCode.WrongRoot);
        Assert.Null(alsoNone);
    }
}
