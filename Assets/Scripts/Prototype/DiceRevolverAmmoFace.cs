using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverAmmoFace : MonoBehaviour
    {
        [SerializeField] private int faceValue = 1;

        public int FaceValue
        {
            get => faceValue;
            set => faceValue = Mathf.Clamp(value, 1, 6);
        }
    }
}
