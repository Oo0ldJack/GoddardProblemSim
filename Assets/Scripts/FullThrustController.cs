using UnityEngine;

namespace Goddard.Stand
{
    public class FullThrustController : MonoBehaviour, IThrustSource
    {
        public float GetU(float t, float vModel) => (float)Goddard.Core.Params.P.UMax;
    }

}