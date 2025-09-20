using UnityEngine;

namespace Character.Collider
{
    [RequireComponent(typeof(UnityEngine.Collider))]
    public class CharacterCollider: MonoBehaviour
    {
        public UnityEngine.Collider Collider => GetComponent<UnityEngine.Collider>();
        
        [SerializeField] private CharacterObject owner;
        public CharacterObject Owner => owner;
    }
}