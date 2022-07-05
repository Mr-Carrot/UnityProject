using System.Collections.Generic;

namespace Core
{
    public class WhichAndPart
    {
        public static Dictionary<MeshPart, List<string>> PartWhichComparison = new Dictionary<MeshPart, List<string>>()
        {
            { MeshPart.Hair, new List<string>() },
            { MeshPart.Top, new List<string>() { WhichItem.Body } },
            { MeshPart.Bottom, new List<string>() { WhichItem.Leg } },
            { MeshPart.Shoe, new List<string>() { WhichItem.Foot } },
            { MeshPart.Suit, new List<string>() { WhichItem.Body, WhichItem.Leg } },
        };
    }
}