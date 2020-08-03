using System;
using System.Collections.Generic;
using System.Linq;

using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Abstractions;
using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Attributes;
using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Enums;
using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Extensions;
using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Tests.Fakes;
using Aliencube.AzureFunctions.Extensions.OpenApi.Core.Visitors;

using FluentAssertions;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Serialization;

namespace Aliencube.AzureFunctions.Extensions.OpenApi.Core.Tests.Visitors
{
    [TestClass]
    public class DictionaryObjectTypeVisitorTests
    {
        private IVisitor _visitor;
        private NamingStrategy _strategy;

        [TestInitialize]
        public void Init()
        {
            this._visitor = new DictionaryObjectTypeVisitor();
            this._strategy = new CamelCaseNamingStrategy();
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), false)]
        public void Given_Type_When_IsNavigatable_Invoked_Then_It_Should_Return_Result(Type type, bool expected)
        {
            var result = this._visitor.IsNavigatable(type);

            result.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), true)]
        [DataRow(typeof(IDictionary<string, string>), true)]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), true)]
        [DataRow(typeof(KeyValuePair<string, string>), true)]
        [DataRow(typeof(int), false)]
        public void Given_Type_When_IsVisitable_Invoked_Then_It_Should_Return_Result(Type type, bool expected)
        {
            var result = this._visitor.IsVisitable(type);

            result.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), false)]
        [DataRow(typeof(IDictionary<string, string>), false)]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), false)]
        [DataRow(typeof(KeyValuePair<string, string>), false)]
        [DataRow(typeof(int), false)]
        public void Given_Type_When_IsParameterVisitable_Invoked_Then_It_Should_Return_Result(Type type, bool expected)
        {
            var result = this._visitor.IsParameterVisitable(type);

            result.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), true)]
        [DataRow(typeof(IDictionary<string, string>), true)]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), true)]
        [DataRow(typeof(KeyValuePair<string, string>), true)]
        [DataRow(typeof(int), false)]
        public void Given_Type_When_IsPayloadVisitable_Invoked_Then_It_Should_Return_Result(Type type, bool expected)
        {
            var result = this._visitor.IsPayloadVisitable(type);

            result.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), "object", null, "string", false, "string", 0)]
        [DataRow(typeof(IDictionary<string, string>), "object", null, "string", false, "string", 0)]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), "object", null, "string", false, "string", 0)]
        [DataRow(typeof(KeyValuePair<string, string>), "object", null, "string", false, "string", 0)]
        [DataRow(typeof(Dictionary<string, FakeModel>), "object", null, "object", true, "fakeModel", 1)]
        [DataRow(typeof(IDictionary<string, FakeModel>), "object", null, "object", true, "fakeModel", 1)]
        [DataRow(typeof(IReadOnlyDictionary<string, FakeModel>), "object", null, "object", true, "fakeModel", 1)]
        [DataRow(typeof(KeyValuePair<string, FakeModel>), "object", null, "object", true, "fakeModel", 1)]
        public void Given_Type_When_Visit_Invoked_Then_It_Should_Return_Result(Type dictionaryType, string dataType, string dataFormat, string additionalPropertyType, bool isReferential, string referenceId, int expected)
        {
            var name = "hello";
            var acceptor = new OpenApiSchemaAcceptor();
            var type = new KeyValuePair<string, Type>(name, dictionaryType);

            this._visitor.Visit(acceptor, type, this._strategy);

            acceptor.Schemas.Should().ContainKey(name);
            acceptor.Schemas[name].Type.Should().Be(dataType);
            acceptor.Schemas[name].Format.Should().Be(dataFormat);

            acceptor.Schemas[name].AdditionalProperties.Should().NotBeNull();
            acceptor.Schemas[name].AdditionalProperties.Type.Should().Be(additionalPropertyType);

            if (isReferential)
            {
                acceptor.Schemas[name].AdditionalProperties.Reference.Type.Should().Be(ReferenceType.Schema);
                acceptor.Schemas[name].AdditionalProperties.Reference.Id.Should().Be(referenceId);
            }

            acceptor.RootSchemas.Count(p => p.Key == referenceId).Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(OpenApiVisibilityType.Advanced)]
        [DataRow(OpenApiVisibilityType.Important)]
        [DataRow(OpenApiVisibilityType.Internal)]
        public void Given_Attribute_When_Visit_Invoked_Then_It_Should_Return_Result(OpenApiVisibilityType visibility)
        {
            var name = "hello";
            var acceptor = new OpenApiSchemaAcceptor();
            var type = new KeyValuePair<string, Type>(name, typeof(Dictionary<string, string>));
            var attribute = new OpenApiSchemaVisibilityAttribute(visibility);

            this._visitor.Visit(acceptor, type, this._strategy, attribute);

            acceptor.Schemas[name].Extensions.Should().ContainKey("x-ms-visibility");
            acceptor.Schemas[name].Extensions["x-ms-visibility"].Should().BeOfType<OpenApiString>();
            (acceptor.Schemas[name].Extensions["x-ms-visibility"] as OpenApiString).Value.Should().Be(visibility.ToDisplayName(this._strategy));
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), "object", null, null)]
        [DataRow(typeof(IDictionary<string, string>), "object", null, null)]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), "object", null, null)]
        [DataRow(typeof(KeyValuePair<string, string>), "object", null, null)]
        public void Given_Type_When_ParameterVisit_Invoked_Then_It_Should_Return_Result(Type dictionaryType, string dataType, string dataFormat, OpenApiSchema expected)
        {
            var result = this._visitor.ParameterVisit(dictionaryType, this._strategy);

            result.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow(typeof(Dictionary<string, string>), "object", null, "string", "string")]
        [DataRow(typeof(IDictionary<string, string>), "object", null, "string", "string")]
        [DataRow(typeof(IReadOnlyDictionary<string, string>), "object", null, "string", "string")]
        [DataRow(typeof(KeyValuePair<string, string>), "object", null, "string", "string")]
        [DataRow(typeof(Dictionary<string, FakeModel>), "object", null, "object", "fakeModel")]
        [DataRow(typeof(IDictionary<string, FakeModel>), "object", null, "object", "fakeModel")]
        [DataRow(typeof(IReadOnlyDictionary<string, FakeModel>), "object", null, "object", "fakeModel")]
        [DataRow(typeof(KeyValuePair<string, FakeModel>), "object", null, "object", "fakeModel")]
        public void Given_Type_When_PayloadVisit_Invoked_Then_It_Should_Return_Result(Type dictionaryType, string dataType, string dataFormat, string additionalPropertyType, string referenceId)
        {
            var result = this._visitor.PayloadVisit(dictionaryType, this._strategy);

            result.Type.Should().Be(dataType);
            result.Format.Should().Be(dataFormat);

            result.AdditionalProperties.Should().NotBeNull();
            result.AdditionalProperties.Type.Should().Be(additionalPropertyType);

            result.AdditionalProperties.Reference.Type.Should().Be(ReferenceType.Schema);
            result.AdditionalProperties.Reference.Id.Should().Be(referenceId);
        }
    }
}
