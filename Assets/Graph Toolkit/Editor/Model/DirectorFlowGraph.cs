using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace SBK.FlowGraph
{
    [Graph(AssetExtension)] 
    [Serializable]
    class DirectorFlowGraph : Graph
    {
        // In Unity, the extension is used to select the right importer, so it must be unique.
        internal const string AssetExtension = "sbkgraph";

        [MenuItem("SBK/Graph/Create Director Flow Graph", false)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DirectorFlowGraph>();
        }
    }
}
