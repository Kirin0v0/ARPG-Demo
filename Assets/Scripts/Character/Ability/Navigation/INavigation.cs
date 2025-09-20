using UnityEngine;

namespace Character.Ability.Navigation
{
    public interface INavigationWalk
    {
        void StartWalkNavigation(Vector3 destination, float speed, float angularSpeed, float stoppingDistance = 0.1f);
        void StopWalkNavigation();
    }

    public interface INavigationFly
    {
        void StartFlyNavigation(Vector3 destination, float height, float horizontalSpeed, float verticalSpeed,
            float angularSpeed, float horizontalStoppingDistance = 0.1f, float verticalStoppingDistance = 0.1f);

        void StopFlyNavigation();
    }
}