using UnityEngine;

namespace MusicalFountains
{
    public abstract class FountainButton : MonoBehaviour
    {
        public abstract void SetPressed(int owner, bool pressed);
        public abstract void SetHovered(int owner, bool hovered);
    }
}
