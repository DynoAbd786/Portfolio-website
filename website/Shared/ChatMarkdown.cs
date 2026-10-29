using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace website.Shared;

// The assistant's replies as HTML. They show inside the chat panel's frame, so a link to the site
// opens in the main window and a link elsewhere in a new tab, never inside the frame.
public static class ChatMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        var doc = Markdown.Parse(markdown, Pipeline);
        foreach (var link in doc.Descendants<LinkInline>().Where(l => !l.IsImage))
        {
            var attrs = link.GetAttributes();
            if (link.Url is { } url && (url.StartsWith('/') || url.StartsWith('#')))
                attrs.AddPropertyIfNotExist("target", "_top");
            else
            {
                attrs.AddPropertyIfNotExist("target", "_blank");
                attrs.AddPropertyIfNotExist("rel", "noopener");
            }
        }
        return doc.ToHtml(Pipeline);
    }
}
