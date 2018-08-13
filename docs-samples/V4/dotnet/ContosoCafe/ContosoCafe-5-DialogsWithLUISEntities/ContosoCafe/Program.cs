// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Contains the entry point for the app.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point for the app.
        /// </summary>
        /// <param name="args">The command line args.</param>
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        /// <summary>
        /// Creates the web host for the app.
        /// </summary>
        /// <param name="args">The command line args.</param>
        /// <returns>A configured web host.</returns>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
