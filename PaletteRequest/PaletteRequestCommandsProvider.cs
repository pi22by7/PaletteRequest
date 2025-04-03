// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PaletteRequest;

public partial class PaletteRequestCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public PaletteRequestCommandsProvider()
    {
        DisplayName = "PaletteRequest API Tester";
        Icon = new IconInfo("\uE774"); // Web icon
        _commands = [
            new CommandItem(new PaletteRequestPage()) { Title = "Open API Tester" }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
