using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp;

public static class TextDocumentSelectorExtensions
{
    extension(TextDocumentSelector _)
    {
        public static TextDocumentSelector ForAxlAndTaxl 
            => TextDocumentSelector.ForLanguage(TextDocumentSelector.AxlLanguageId,
                TextDocumentSelector.TaxlLanguageId);

        public static string AxlLanguageId => "axl";
        public static string TaxlLanguageId => "taxl";
    }
}