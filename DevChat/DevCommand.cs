using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;

namespace DevChat
{
    public class DevCommand
    {
        private async Task NotifyWorking(CommandContext ctx, string job)
        {
            await ctx.TriggerTypingAsync();
            await ctx.Client.UpdateStatusAsync(game: new Game(job));
        }

        [Command("create"), RequireOwner]
        [Description("create {Name}\nCreate a project.")]
        public async Task Create(CommandContext ctx, string name)
        {
            await NotifyWorking(ctx, "Create " + name);
        }

        [Command("remove"), RequireOwner]
        [Description("remove {Name}\nRemove a project.")]
        public async Task Remove(CommandContext ctx, string name)
        {
            await NotifyWorking(ctx, "Remove " + name);
        }

        [Command("build"), RequireOwner]
        [Description("build {Name} {Script}\nBuild {Name} with script file at {Script}.")]
        public async Task Build(CommandContext ctx, string name, string scriptPath)
        {
            await NotifyWorking(ctx, "Build " + name);
        }

        [Command("run"), RequireOwner]
        [Description("run {Name} {File}\nExecute {Name}\'s file at {Script}.")]
        public async Task Run(CommandContext ctx, string name, string filePath)
        {
            await NotifyWorking(ctx, "Run " + name);
        }
    }
}
