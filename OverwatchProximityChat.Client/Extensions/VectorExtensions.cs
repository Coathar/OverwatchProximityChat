using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchProximityChat.Client.Extensions
{
    public static class VectorExtensions
    {
        public static TeamSpeak.Sdk.Vector ToTSVector(this Vector3 vector)
        {
            return new TeamSpeak.Sdk.Vector(vector.X, vector.Y, vector.Z);
        }
    }
}
