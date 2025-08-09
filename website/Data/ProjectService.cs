using System.Collections.Generic;
using System.Threading.Tasks;

namespace website.Data
{
    public class ProjectService
    {
        public Task<List<Project>> GetProjectsAsync()
        {
            
            var projects = new List<Project>
            {
                new Project
                {
                    Title = "E-Commerce Platform",
                    Description = "A full-featured e-commerce site built with ASP.NET Core, EF Core, and Blazor for the admin dashboard. Includes payment gateway integration.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=E-Commerce",
                    ProjectUrl = "#",
                    Tags = new[] { "ASP.NET Core", "Blazor", "EF Core", "Stripe" }
                },
                new Project
                {
                    Title = "Real-Time Chat Application",
                    Description = "A chat app using SignalR for real-time communication. Features user authentication, private messaging, and group chats.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Chat+App",
                    ProjectUrl = "#",
                    Tags = new[] { "SignalR", ".NET", "Blazor", "Real-Time" }
                },
                new Project
                {
                    Title = "Cloud-Native API",
                    Description = "A serverless REST API built with Azure Functions and Cosmos DB. Designed for high scalability and low latency.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Serverless+API",
                    ProjectUrl = "#",
                    Tags = new[] { "Azure Functions", "Cosmos DB", "Serverless", "C#" }
                },
                new Project
                {
                    Title = "Cross-Platform Mobile App",
                    Description = "A mobile application for iOS and Android built with .NET MAUI, sharing over 95% of the codebase.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=.NET+MAUI+App",
                    ProjectUrl = "#",
                    Tags = new[] { ".NET MAUI", "XAML", "C#", "Mobile" }
                }
            };


            return Task.FromResult(projects);
        }
    }
}
