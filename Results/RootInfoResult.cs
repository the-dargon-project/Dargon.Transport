using Dargon.Games;

namespace Dargon.IO.DSP.Results
{
   public class RootInfoResult
   {
      /// <summary>
      /// The dargon game associated with the data request
      /// </summary>
      public DargonGame Game { get; private set; }

      /// <summary>
      /// The node id of the root node queried
      /// </summary>
      public uint RootNodeID { get; private set; }

      /// <summary>
      /// The tree id of the root node queried
      /// </summary>
      public uint TreeID { get; private set; }

      /// <summary>
      /// The friendly name of the game associated with the Game property.
      /// </summary>
      public string GameName { get; private set; }

      /// <summary>
      /// Initializes a new instance of a RootInfoResult
      /// </summary>
      /// <param name="game"></param>
      /// <param name="rootNodeId"></param>
      /// <param name="treeId"></param>
      /// <param name="gameName"></param>
      public RootInfoResult(
         DargonGame game,
         uint rootNodeId,
         uint treeId,
         string gameName)
      {
         Game = game;
         RootNodeID = rootNodeId;
         TreeID = treeId;
         GameName = gameName;
      }
   }
}
