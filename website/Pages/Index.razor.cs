using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace website.Pages
{
    public partial class Index : ComponentBase
    {
        private readonly string commandText = "dotnet publish -c Release -o ./portfolio";
        private string displayedText = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AnimateTyping();
            }
        }

        private async Task AnimateTyping()
        {
            foreach (char c in commandText)
            {
                displayedText += c;
                StateHasChanged();
                await Task.Delay(75); // Adjust typing speed here
            }
        }
    }
}
