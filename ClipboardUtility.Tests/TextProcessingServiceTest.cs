using System.Globalization;
using System.Text;
using ClipboardUtility.src.Services;

namespace ClipboardUtility.Tests
{
    public class TextProcessingServiceTests
    {
        private readonly TextProcessingService _service;

        public TextProcessingServiceTests()
        {
            _service = new TextProcessingService();
        }

        #region CountCharacters Tests

        [Fact]
        public void CountCharacters_WithNormalString_ReturnsCorrectCount()
        {
            // Arrange
            var input = "Hello, World!";

            // Act
            var result = _service.CountCharacters(input);

            // Assert
            Assert.Equal(13, result);
        }

        [Fact]
        public void CountCharacters_WithNull_ReturnsZero()
        {
            // Act
            var result = _service.CountCharacters(null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CountCharacters_WithEmptyString_ReturnsZero()
        {
            // Act
            var result = _service.CountCharacters(string.Empty);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region RemoveLineBreaks Tests

        [Fact]
        public void RemoveLineBreaks_WithCRLF_ReplacesWithSpace()
        {
            // Arrange
            var input = "Line1\r\nLine2\r\nLine3";

            // Act
            var result = _service.RemoveLineBreaks(input);

            // Assert
            Assert.Equal("Line1 Line2 Line3", result);
        }

        [Fact]
        public void RemoveLineBreaks_WithLF_ReplacesWithSpace()
        {
            // Arrange
            var input = "Line1\nLine2\nLine3";

            // Act
            var result = _service.RemoveLineBreaks(input);

            // Assert
            Assert.Equal("Line1 Line2 Line3", result);
        }

        [Fact]
        public void RemoveLineBreaks_WithNull_ReturnsEmpty()
        {
            // Act
            var result = _service.RemoveLineBreaks(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region NormalizeWhitespace Tests

        [Fact]
        public void NormalizeWhitespace_WithMultipleSpaces_CollapsesToSingle()
        {
            // Arrange
            var input = "Hello    World";

            // Act
            var result = _service.NormalizeWhitespace(input);

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void NormalizeWhitespace_WithTabsAndNewlines_NormalizesAll()
        {
            // Arrange
            var input = "  Hello\t\tWorld\r\n  Test  ";

            // Act
            var result = _service.NormalizeWhitespace(input);

            // Assert
            Assert.Equal("Hello World Test", result);
        }

        #endregion

        #region ConvertTabsToSpaces Tests

        [Fact]
        public void ConvertTabsToSpaces_WithDefaultSize_Converts()
        {
            // Arrange
            var input = "Hello\tWorld";

            // Act
            var result = _service.ConvertTabsToSpaces(input);

            // Assert
            Assert.Equal("Hello    World", result);
        }

        [Fact]
        public void ConvertTabsToSpaces_WithCustomSize_Converts()
        {
            // Arrange
            var input = "Hello\tWorld";

            // Act
            var result = _service.ConvertTabsToSpaces(input, 2);

            // Assert
            Assert.Equal("Hello  World", result);
        }

        #endregion

        #region Trim Tests

        [Fact]
        public void Trim_WithLeadingAndTrailingSpaces_RemovesThem()
        {
            // Arrange
            var input = "  Hello World  ";

            // Act
            var result = _service.Trim(input);

            // Assert
            Assert.Equal("Hello World", result);
        }

        #endregion

        #region ToUpper/ToLower Tests

        [Fact]
        public void ToUpper_WithLowerCase_ConvertsToUpper()
        {
            // Arrange
            var input = "hello world";

            // Act
            var result = _service.ToUpper(input);

            // Assert
            Assert.Equal("HELLO WORLD", result);
        }

        [Fact]
        public void ToLower_WithUpperCase_ConvertsToLower()
        {
            // Arrange
            var input = "HELLO WORLD";

            // Act
            var result = _service.ToLower(input);

            // Assert
            Assert.Equal("hello world", result);
        }

        #endregion

        #region RemovePunctuation Tests

        [Fact]
        public void RemovePunctuation_WithPunctuation_RemovesIt()
        {
            // Arrange
            var input = "Hello, World! How are you?";

            // Act
            var result = _service.RemovePunctuation(input);

            // Assert
            Assert.Equal("Hello World How are you", result);
        }

        #endregion

        #region RemoveControlCharacters Tests

        [Fact]
        public void RemoveControlCharacters_WithControlChars_RemovesThem()
        {
            // Arrange
            var input = "Hello\x00\x01World\x1F";

            // Act
            var result = _service.RemoveControlCharacters(input);

            // Assert
            Assert.Equal("HelloWorld", result);
        }

        #endregion

        #region RemoveUrls Tests

        [Fact]
        public void RemoveUrls_WithHttpUrl_RemovesIt()
        {
            // Arrange
            var input = "Check out https://example.com for more info";

            // Act
            var result = _service.RemoveUrls(input);

            // Assert
            Assert.Equal("Check out  for more info", result);
        }

        [Fact]
        public void RemoveUrls_WithWwwUrl_RemovesIt()
        {
            // Arrange
            var input = "Visit www.example.com for details";

            // Act
            var result = _service.RemoveUrls(input);

            // Assert
            Assert.Equal("Visit  for details", result);
        }

        #endregion

        #region RemoveEmails Tests

        [Fact]
        public void RemoveEmails_WithEmail_RemovesIt()
        {
            // Arrange
            var input = "Contact me at test@example.com for info";

            // Act
            var result = _service.RemoveEmails(input);

            // Assert
            Assert.Equal("Contact me at  for info", result);
        }

        #endregion

        #region RemoveHtmlTags Tests

        [Fact]
        public void RemoveHtmlTags_WithTags_RemovesThem()
        {
            // Arrange
            var input = "<p>Hello <strong>World</strong></p>";

            // Act
            var result = _service.RemoveHtmlTags(input);

            // Assert
            Assert.Equal("Hello World", result);
        }

        #endregion

        #region StripMarkdownLinks Tests

        [Fact]
        public void StripMarkdownLinks_WithMarkdownLink_ExtractsText()
        {
            // Arrange
            var input = "Check [this link](https://example.com) out";

            // Act
            var result = _service.StripMarkdownLinks(input);

            // Assert
            Assert.Equal("Check this link out", result);
        }

        #endregion

        #region NormalizeUnicode Tests

        [Fact]
        public void NormalizeUnicode_WithUnicodeChars_Normalizes()
        {
            // Arrange
            var input = "café"; // Different representations possible

            // Act
            var result = _service.NormalizeUnicode(input);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("caf", result);
        }

        #endregion

        #region RemoveDiacritics Tests

        [Fact]
        public void RemoveDiacritics_WithCombiningDiacritics_RemovesThem()
        {
            // Arrange - Using combining diacritics that decompose properly
            var input = "e\u0301"; // é composed as e + combining acute accent

            // Act
            var result = _service.RemoveDiacritics(input);

            // Assert
            Assert.Equal("e", result);
        }

        [Fact]
        public void RemoveDiacritics_WithNullOrEmpty_ReturnsEmpty()
        {
            // Act & Assert
            Assert.Equal(string.Empty, _service.RemoveDiacritics(null));
            Assert.Equal(string.Empty, _service.RemoveDiacritics(string.Empty));
        }

        [Fact]
        public void RemoveDiacritics_WithNormalText_ReturnsUnchanged()
        {
            // Arrange
            var input = "Hello World";

            // Act
            var result = _service.RemoveDiacritics(input);

            // Assert
            Assert.Equal("Hello World", result);
        }

        #endregion

        #region Truncate Tests

        [Fact]
        public void Truncate_WithLongString_TruncatesWithSuffix()
        {
            // Arrange
            var input = "This is a very long string that needs to be truncated";

            // Act
            var result = _service.Truncate(input, 20, "...");

            // Assert
            Assert.Equal(20, result.Length);
            Assert.EndsWith("...", result);
        }

        [Fact]
        public void Truncate_WithShortString_ReturnsOriginal()
        {
            // Arrange
            var input = "Short";

            // Act
            var result = _service.Truncate(input, 20);

            // Assert
            Assert.Equal("Short", result);
        }

        #endregion

        #region JoinLinesWithSpace Tests

        [Fact]
        public void JoinLinesWithSpace_WithMultipleLines_JoinsThem()
        {
            // Arrange
            var input = "Line1\r\nLine2\nLine3";

            // Act
            var result = _service.JoinLinesWithSpace(input);

            // Assert
            Assert.Equal("Line1 Line2 Line3", result);
        }

        #endregion

        #region RemoveDuplicateLines Tests

        [Fact]
        public void RemoveDuplicateLines_WithConsecutiveDuplicates_RemovesThem()
        {
            // Arrange
            var input = "Line1\r\nLine1\r\nLine2\r\nLine2\r\nLine3";

            // Act
            var result = _service.RemoveDuplicateLines(input);

            // Assert
            var lines = result.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void RemoveDuplicateLines_WithNonConsecutiveDuplicates_KeepsThem()
        {
            // Arrange
            var input = "Line1\r\nLine2\r\nLine1";

            // Act
            var result = _service.RemoveDuplicateLines(input);

            // Assert
            var lines = result.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.Equal(3, lines.Length);
        }

        #endregion

        #region ToPascalCase Tests

        [Fact]
        public void ToPascalCase_WithSpaceSeparated_ConvertsToPascal()
        {
            // Arrange
            var input = "hello world test";

            // Act
            var result = _service.ToPascalCase(input);

            // Assert
            Assert.Equal("HelloWorldTest", result);
        }

        [Fact]
        public void ToPascalCase_WithHyphenated_ConvertsToPascal()
        {
            // Arrange
            var input = "hello-world-test";

            // Act
            var result = _service.ToPascalCase(input);

            // Assert
            Assert.Equal("HelloWorldTest", result);
        }

        [Fact]
        public void ToPascalCase_WithUnderscored_ConvertsToPascal()
        {
            // Arrange
            var input = "hello_world_test";

            // Act
            var result = _service.ToPascalCase(input);

            // Assert
            Assert.Equal("HelloWorldTest", result);
        }

        #endregion

        #region ToCamelCase Tests

        [Fact]
        public void ToCamelCase_WithSpaceSeparated_ConvertsToCamel()
        {
            // Arrange
            var input = "hello world test";

            // Act
            var result = _service.ToCamelCase(input);

            // Assert
            Assert.Equal("helloWorldTest", result);
        }

        #endregion

        #region ToTitleCase Tests

        [Fact]
        public void ToTitleCase_WithLowerCase_ConvertsToTitle()
        {
            // Arrange
            var input = "hello world";

            // Act
            var result = _service.ToTitleCase(input);

            // Assert
            Assert.Equal("Hello World", result);
        }

        #endregion

        #region Process Method Tests

        [Fact]
        public void Process_WithNoneMode_ReturnsOriginal()
        {
            // Arrange
            var input = "Hello World";

            // Act
            var result = _service.Process(input, ProcessingMode.None);

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void Process_WithRemoveLineBreaksMode_RemovesLineBreaks()
        {
            // Arrange
            var input = "Line1\r\nLine2";

            // Act
            var result = _service.Process(input, ProcessingMode.RemoveLineBreaks);

            // Assert
            Assert.Equal("Line1 Line2", result);
        }

        [Fact]
        public void Process_WithTrimMode_TrimsWhitespace()
        {
            // Arrange
            var input = "  Hello  ";

            // Act
            var result = _service.Process(input, ProcessingMode.Trim);

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Process_WithCollapseWhitespaceMode_RemovesAllWhitespace()
        {
            // Arrange
            var input = "Hello World Test";

            // Act
            var result = _service.Process(input, ProcessingMode.CollapseWhitespaceAll);

            // Assert
            Assert.Equal("HelloWorldTest", result);
        }

        [Fact]
        public void Process_WithOptions_UsesTabSize()
        {
            // Arrange
            var input = "Hello\tWorld";
            var options = new TextProcessingOptions { TabSize = 2 };

            // Act
            var result = _service.Process(input, ProcessingMode.ConvertTabsToSpaces, options);

            // Assert
            Assert.Equal("Hello  World", result);
        }

        [Fact]
        public void Process_WithOptions_UsesTruncateSettings()
        {
            // Arrange
            var input = "This is a long string";
            var options = new TextProcessingOptions { MaxLength = 10, TruncateSuffix = "..." };

            // Act
            var result = _service.Process(input, ProcessingMode.Truncate, options);

            // Assert
            Assert.Equal(10, result.Length);
            Assert.EndsWith("...", result);
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AllMethods_WithNullOrEmpty_ReturnEmptyOrZero(string? input)
        {
            // Act & Assert
            Assert.Equal(string.Empty, _service.RemoveLineBreaks(input));
            Assert.Equal(string.Empty, _service.NormalizeWhitespace(input));
            Assert.Equal(string.Empty, _service.ConvertTabsToSpaces(input));
            Assert.Equal(string.Empty, _service.Trim(input));
            Assert.Equal(string.Empty, _service.ToUpper(input));
            Assert.Equal(string.Empty, _service.ToLower(input));
            Assert.Equal(string.Empty, _service.RemovePunctuation(input));
            Assert.Equal(string.Empty, _service.RemoveControlCharacters(input));
            Assert.Equal(string.Empty, _service.RemoveUrls(input));
            Assert.Equal(string.Empty, _service.RemoveEmails(input));
            Assert.Equal(string.Empty, _service.RemoveHtmlTags(input));
            Assert.Equal(string.Empty, _service.StripMarkdownLinks(input));
            Assert.Equal(string.Empty, _service.NormalizeUnicode(input));
            Assert.Equal(string.Empty, _service.RemoveDiacritics(input));
            Assert.Equal(string.Empty, _service.JoinLinesWithSpace(input));
            Assert.Equal(string.Empty, _service.RemoveDuplicateLines(input));
            Assert.Equal(string.Empty, _service.ToPascalCase(input));
            Assert.Equal(string.Empty, _service.ToCamelCase(input));
            Assert.Equal(string.Empty, _service.ToTitleCase(input));
            Assert.Equal(0, _service.CountCharacters(input));
        }

        [Fact]
        public void Process_WithNullInput_ReturnsEmpty()
        {
            // Act
            var result = _service.Process(null, ProcessingMode.RemoveLineBreaks);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region Japanese Text Tests

        [Fact]
        public void RemoveLineBreaks_WithJapaneseText_WorksCorrectly()
        {
            // Arrange
            var input = "こんにちは\r\n世界";

            // Act
            var result = _service.RemoveLineBreaks(input);

            // Assert
            Assert.Equal("こんにちは 世界", result);
        }


        #endregion
    }
}
