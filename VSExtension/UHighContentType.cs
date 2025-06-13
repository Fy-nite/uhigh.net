using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace UHigh.Syntax
{
    internal static class UHighContentTypeDefinitions
    {
        [Export]
        [Name("uhigh")]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition UHighContentTypeDefinition;

        [Export]
        [FileExtension(".uh")]
        [ContentType("uhigh")]
        internal static FileExtensionToContentTypeDefinition UHighFileExtensionDefinition;
    }
}
