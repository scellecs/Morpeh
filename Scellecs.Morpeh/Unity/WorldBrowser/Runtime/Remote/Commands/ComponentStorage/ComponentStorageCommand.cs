﻿#if UNITY_EDITOR && MORPEH_REMOTE_BROWSER || DEVELOPMENT_BUILD && MORPEH_REMOTE_BROWSER
namespace Scellecs.Morpeh.WorldBrowser.Remote.Commands {
    internal static class ComponentStorageCommand {
        internal const byte FetchRequest = 1;
        internal const byte FetchResponse = 2;
    }
}
#endif