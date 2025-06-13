using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace UHigh.Syntax
{
    public static class UHighClassificationTypes
    {
        public const string Keyword = "uhigh.keyword";
        public const string Comment = "uhigh.comment";
        public const string String = "uhigh.string";
        public const string Number = "uhigh.number";
        public const string Type = "uhigh.type";
        public const string Function = "uhigh.function";
        public const string Operator = "uhigh.operator";
    }

    internal static class UHighClassificationDefinitions
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Keyword)]
        internal static ClassificationTypeDefinition UHighKeyword = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Comment)]
        internal static ClassificationTypeDefinition UHighComment = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.String)]
        internal static ClassificationTypeDefinition UHighString = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Number)]
        internal static ClassificationTypeDefinition UHighNumber = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Type)]
        internal static ClassificationTypeDefinition UHighType = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Function)]
        internal static ClassificationTypeDefinition UHighFunction = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(UHighClassificationTypes.Operator)]
        internal static ClassificationTypeDefinition UHighOperator = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = UHighClassificationTypes.Keyword)]
    [Name("UHigh Keyword")]
    internal sealed class UHighKeywordFormat : ClassificationFormatDefinition
    {
        public UHighKeywordFormat()
        {
            ForegroundColor = Colors.Blue;
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = UHighClassificationTypes.Comment)]
    [Name("UHigh Comment")]
    internal sealed class UHighCommentFormat : ClassificationFormatDefinition
    {
        public UHighCommentFormat()
        {
            ForegroundColor = Colors.Green;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = UHighClassificationTypes.String)]
    [Name("UHigh String")]
    internal sealed class UHighStringFormat : ClassificationFormatDefinition
    {
        public UHighStringFormat()
        {
            ForegroundColor = Colors.Red;
        }
    }
}
