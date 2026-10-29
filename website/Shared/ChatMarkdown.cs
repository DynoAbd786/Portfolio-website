using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace website.Shared;

// The assistant's replies as HTML. They show inside the chat panel's frame, so a link to the site
// opens in the main window and a link elsewhere in a new tab, never inside the frame. A bare address
// on the site ("mkkai.dev/#contact") becomes such a link too.
public static class ChatMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    // mkkai.dev with an optional path, not part of an email address or a longer word; a sentence's
    // closing punctuation stays out of the path
    private static readonly Regex BareSiteAddress = new(
        @"(?<![\w.@/-])(?:https?://)?(?:www\.)?mkkai\.dev(?<path>/(?:[^\s)\]]*[^\s)\].,;:!?'""])?)?(?![\w-])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SiteUrl = new(@"^https?://(?:www\.)?mkkai\.dev(?<path>/.*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string ToHtml(string markdown)
    {
        var doc = Markdown.Parse(markdown, Pipeline);
        LinkBareSiteAddresses(doc);
        foreach (var link in doc.Descendants<LinkInline>().Where(l => !l.IsImage))
        {
            if (link.Url is { } full && SiteUrl.Match(full) is { Success: true } site)
                link.Url = site.Groups["path"].Success ? site.Groups["path"].Value : "/";
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

    private static void LinkBareSiteAddresses(MarkdownDocument doc)
    {
        foreach (var literal in doc.Descendants<LiteralInline>().ToList())
        {
            if (InsideLink(literal)) continue;
            var text = literal.Content.ToString();
            var matches = BareSiteAddress.Matches(text);
            if (matches.Count == 0) continue;

            Inline cursor = literal;
            var last = 0;
            foreach (Match m in matches)
            {
                if (m.Index > last) cursor = After(cursor, new LiteralInline(text[last..m.Index]));
                var path = m.Groups["path"].Success && m.Groups["path"].Length > 0 ? m.Groups["path"].Value : "/";
                var link = new LinkInline(path, "");
                link.AppendChild(new LiteralInline(m.Value));
                cursor = After(cursor, link);
                last = m.Index + m.Length;
            }
            if (last < text.Length) After(cursor, new LiteralInline(text[last..]));
            literal.Remove();
        }
    }

    private static Inline After(Inline cursor, Inline next)
    {
        cursor.InsertAfter(next);
        return next;
    }

    private static bool InsideLink(Inline inline)
    {
        for (var p = inline.Parent; p is not null; p = p.Parent)
            if (p is LinkInline) return true;
        return false;
    }
}
