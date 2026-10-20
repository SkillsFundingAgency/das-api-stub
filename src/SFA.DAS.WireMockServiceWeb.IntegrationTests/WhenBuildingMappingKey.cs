using SFA.DAS.WireMockService.Data;
using System;
using FluentAssertions;
using NUnit.Framework;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenBuildingMappingKey
    {
        [Test]
        public void method_case_is_ignored()
        {
            MappingKey.For("get", "/data").Should().Be(MappingKey.For("GET", "/data"));
        }

        [Test]
        public void url_case_is_ignored()
        {
            MappingKey.For("Get", "/Approvals/ABC?Ukprn=1").Should().Be(MappingKey.For("Get", "/approvals/abc?ukprn=1"));
        }

        [Test]
        public void leading_slash_is_optional()
        {
            MappingKey.For("Get", "data").Should().Be(MappingKey.For("Get", "/data"));
        }

        [Test]
        public void trailing_question_mark_is_ignored()
        {
            MappingKey.For("Get", "/data?").Should().Be(MappingKey.For("Get", "/data"));
        }

        [Test]
        public void query_parameter_order_is_ignored()
        {
            MappingKey.For("Get", "/data?b=2&a=1").Should().Be(MappingKey.For("Get", "/data?a=1&b=2"));
        }

        [Test]
        public void encoded_and_decoded_urls_are_equivalent()
        {
            MappingKey.For("Get", "/data%20set?name=a%20b").Should().Be(MappingKey.For("Get", "/data set?name=a b"));
        }

        [Test]
        public void a_fully_escaped_url_is_equivalent_to_the_unescaped_url()
        {
            MappingKey.For("Get", Uri.EscapeDataString("/data?b=2&a=1")).Should().Be(MappingKey.For("Get", "/data?a=1&b=2"));
        }

        [Test]
        public void different_methods_give_different_keys()
        {
            MappingKey.For("Get", "/data").Should().NotBe(MappingKey.For("Put", "/data"));
        }

        [Test]
        public void different_paths_give_different_keys()
        {
            MappingKey.For("Get", "/data/1").Should().NotBe(MappingKey.For("Get", "/data/2"));
        }

        [Test]
        public void different_query_values_give_different_keys()
        {
            MappingKey.For("Get", "/data?a=1").Should().NotBe(MappingKey.For("Get", "/data?a=2"));
        }

        [Test]
        public void extra_query_parameters_give_different_keys()
        {
            MappingKey.For("Get", "/data?a=1&b=2").Should().NotBe(MappingKey.For("Get", "/data?a=1"));
        }

        [Test]
        public void key_contains_no_characters_table_storage_forbids_in_row_keys()
        {
            MappingKey.For("Get", @"/data/1?a=1#frag\x").Should().NotContainAny("/", "\\", "#", "?");
        }
    }
}
