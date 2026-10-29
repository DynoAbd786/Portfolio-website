using website.Shared;

namespace website.tests;

// The assistant's replies render inside the chat panel's frame: links must leave the frame
public class ChatMarkdownTests
{
    [Fact]
    public void Site_links_open_in_the_main_window()
    {
        var html = ChatMarkdown.ToHtml("See [HireForge](/projects/hireforge) or [get in touch](/#contact).");
        Assert.Contains("<a href=\"/projects/hireforge\" target=\"_top\">", html);
        Assert.Contains("<a href=\"/#contact\" target=\"_top\">", html);
    }

    [Fact]
    public void Outside_links_open_in_a_new_tab()
    {
        var html = ChatMarkdown.ToHtml("[GitHub](https://github.com/DynoAbd786)");
        Assert.Contains("<a href=\"https://github.com/DynoAbd786\" target=\"_blank\" rel=\"noopener\">", html);
    }

    [Fact]
    public void Markdown_still_renders()
    {
        Assert.Contains("<strong>bold</strong>", ChatMarkdown.ToHtml("**bold**"));
    }
}
